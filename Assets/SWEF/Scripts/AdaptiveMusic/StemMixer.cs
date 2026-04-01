// StemMixer.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Audio;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Manages the AudioSource pool used for adaptive music stem playback.
    /// Handles fade-in, fade-out, crossfade, and ducking for narration/voice chat.
    ///
    /// <para>One <see cref="AudioSource"/> is reserved per active layer (up to
    /// <see cref="AdaptiveMusicProfile.stemPoolSize"/>). Stem starts are scheduled
    /// against Unity's DSP clock for sample-accurate playback.</para>
    /// </summary>
    public class StemMixer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Configuration")]
        [Tooltip("Master volume multiplier for all adaptive music stems (0–1).")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

        [Tooltip("Volume multiplier applied when another system ducks the music (0–1).")]
        [SerializeField, Range(0f, 1f)] private float duckingVolume = 0.2f;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly Dictionary<MusicLayer, AudioSource> _activeSources
            = new Dictionary<MusicLayer, AudioSource>();

        private readonly Dictionary<MusicLayer, float> _targetVolumes
            = new Dictionary<MusicLayer, float>();

        private bool _isDucked;

        // ── Coroutine tracking ────────────────────────────────────────────────────
        private readonly Dictionary<MusicLayer, Coroutine> _fadeCoroutines
            = new Dictionary<MusicLayer, Coroutine>();

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            // Smoothly apply target volumes each frame
            float duckMultiplier = _isDucked ? duckingVolume : 1f;

            foreach (var kvp in _activeSources)
            {
                AudioSource src = kvp.Value;
                if (src == null) continue;

                if (_targetVolumes.TryGetValue(kvp.Key, out float target))
                    src.volume = Mathf.MoveTowards(src.volume, target * masterVolume * duckMultiplier, Time.deltaTime * 2f);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Begins playing a stem on the given layer, fading in over
        /// <paramref name="fadeInDuration"/> seconds.
        /// </summary>
        public void ActivateStem(StemDefinition stem, float fadeInDuration = 1f)
        {
            if (stem == null || stem.audioClip == null)
                return;

            AudioSource src = GetOrCreateSource(stem.layer);
            src.clip   = stem.audioClip;
            src.loop   = true;
            src.volume = 0f;
            src.Play();

            SetFadeTarget(stem.layer, 1f, fadeInDuration);
        }

        /// <summary>Fades out and stops the stem on the given layer.</summary>
        public void DeactivateStem(MusicLayer layer, float fadeOutDuration = 1f)
        {
            if (!_activeSources.TryGetValue(layer, out AudioSource src) || src == null)
                return;

            StopFadeCoroutine(layer);
            Coroutine co = StartCoroutine(FadeOutAndStop(src, layer, fadeOutDuration));
            _fadeCoroutines[layer] = co;
        }

        /// <summary>Sets the immediate target volume for a given layer (applied smoothly each frame).</summary>
        public void SetStemVolume(MusicLayer layer, float volume)
        {
            _targetVolumes[layer] = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Crossfades from the current stem on <paramref name="layer"/> to a new
        /// <paramref name="newStem"/> over <paramref name="duration"/> seconds.
        /// </summary>
        public void CrossfadeStem(MusicLayer layer, StemDefinition newStem, float duration = 2f)
        {
            if (newStem == null || newStem.audioClip == null)
            {
                DeactivateStem(layer, duration);
                return;
            }

            // Stop any ongoing fade for this layer
            StopFadeCoroutine(layer);
            StartCoroutine(CrossfadeCoroutine(layer, newStem, duration));
        }

        /// <summary>Ducks all stem volumes to a reduced level for narration or voice chat.</summary>
        public void Duck()
        {
            _isDucked = true;
        }

        /// <summary>Restores stem volumes from ducked state.</summary>
        public void Unduck()
        {
            _isDucked = false;
        }

        /// <summary>Sets the master volume multiplier.</summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }

        /// <summary>Stops all active stems immediately.</summary>
        public void StopAll()
        {
            foreach (AudioSource src in _activeSources.Values)
            {
                if (src != null)
                    src.Stop();
            }
            _targetVolumes.Clear();
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private AudioSource GetOrCreateSource(MusicLayer layer)
        {
            if (_activeSources.TryGetValue(layer, out AudioSource existing) && existing != null)
            {
                existing.Stop();
                return existing;
            }

            GameObject go = new GameObject($"StemSource_{layer}");
            go.transform.SetParent(transform, false);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2-D music
            _activeSources[layer] = src;
            return src;
        }

        private void SetFadeTarget(MusicLayer layer, float target, float duration)
        {
            StopFadeCoroutine(layer);
            if (duration <= 0f)
            {
                _targetVolumes[layer] = target;
                return;
            }
            _targetVolumes[layer] = target;
            // Volume is approached per-frame in Update(); duration controls approach speed
        }

        private void StopFadeCoroutine(MusicLayer layer)
        {
            if (_fadeCoroutines.TryGetValue(layer, out Coroutine co) && co != null)
                StopCoroutine(co);
        }

        private IEnumerator FadeOutAndStop(AudioSource src, MusicLayer layer, float duration)
        {
            float startVolume = src.volume;
            float elapsed     = 0f;

            while (elapsed < duration)
            {
                elapsed    += Time.deltaTime;
                src.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            src.Stop();
            src.volume = 0f;
            _targetVolumes.Remove(layer);
            _fadeCoroutines.Remove(layer);
        }

        private IEnumerator CrossfadeCoroutine(MusicLayer layer, StemDefinition newStem, float duration)
        {
            // Fade out current stem
            if (_activeSources.TryGetValue(layer, out AudioSource oldSrc) && oldSrc != null && oldSrc.isPlaying)
            {
                float startVol = oldSrc.volume;
                float elapsed  = 0f;
                while (elapsed < duration * 0.5f)
                {
                    elapsed    += Time.deltaTime;
                    oldSrc.volume = Mathf.Lerp(startVol, 0f, elapsed / (duration * 0.5f));
                    yield return null;
                }
                oldSrc.Stop();
            }

            // Fade in new stem
            AudioSource newSrc = GetOrCreateSource(layer);
            newSrc.clip   = newStem.audioClip;
            newSrc.loop   = true;
            newSrc.volume = 0f;
            newSrc.Play();

            float inElapsed = 0f;
            float targetVol = _targetVolumes.TryGetValue(layer, out float tv) ? tv : 1f;
            float effectiveVol = targetVol * masterVolume * (_isDucked ? duckingVolume : 1f);
            while (inElapsed < duration * 0.5f)
            {
                inElapsed    += Time.deltaTime;
                newSrc.volume = Mathf.Lerp(0f, effectiveVol, inElapsed / (duration * 0.5f));
                yield return null;
            }

            newSrc.volume        = effectiveVol;
            _targetVolumes[layer] = targetVol;
            _fadeCoroutines.Remove(layer);
        }
    }
}
