// StemMixer.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Manages an AudioSource pool (one per active layer, max 8 simultaneous).
    /// Supports fade-in/out, crossfade between stems on the same layer, per-layer
    /// volume control, and ducking for narration or voice chat.
    /// </summary>
    public class StemMixer : MonoBehaviour
    {
        // ── Types ─────────────────────────────────────────────────────────────

        private class StemSlot
        {
            public MusicLayer    layer;
            public AudioSource   source;
            public float         targetVolume;
            public Coroutine     fadeCoroutine;
        }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Volume")]
        [Tooltip("Master volume for all adaptive music stems (0–1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _masterVolume = 1f;

        [Tooltip("Multiplier applied during ducking (e.g. narration/voice chat).")]
        [Range(0f, 1f)]
        [SerializeField] private float _duckVolume = 0.3f;

        // ── Events ────────────────────────────────────────────────────────────

        public event System.Action<MusicLayer> OnStemActivated;
        public event System.Action<MusicLayer> OnStemDeactivated;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly Dictionary<MusicLayer, StemSlot> _slots = new Dictionary<MusicLayer, StemSlot>();
        private bool  _isDucked;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Starts playing a stem for the given layer, fading in over <paramref name="fadeInDuration"/> seconds.</summary>
        public void ActivateStem(StemDefinition stem, float fadeInDuration = 1f)
        {
            if (stem == null) return;

            var clip = Resources.Load<AudioClip>(stem.audioClipResourcePath);
            if (clip == null) return;

            var slot = GetOrCreateSlot(stem.layer);

            if (slot.fadeCoroutine != null) StopCoroutine(slot.fadeCoroutine);

            slot.source.clip = clip;
            slot.source.loop = true;
            slot.source.volume = 0f;
            slot.source.Play();

            float vol = _masterVolume * (_isDucked ? _duckVolume : 1f);
            slot.fadeCoroutine = StartCoroutine(FadeVolume(slot.source, 0f, vol, fadeInDuration));
            slot.targetVolume  = vol;

            OnStemActivated?.Invoke(stem.layer);
        }

        /// <summary>Fades out and stops the stem for the given layer.</summary>
        public void DeactivateStem(MusicLayer layer, float fadeOutDuration = 1f)
        {
            if (!_slots.TryGetValue(layer, out var slot)) return;
            if (!slot.source.isPlaying) return;

            if (slot.fadeCoroutine != null) StopCoroutine(slot.fadeCoroutine);
            slot.fadeCoroutine = StartCoroutine(FadeOutAndStop(slot.source, fadeOutDuration));
            OnStemDeactivated?.Invoke(layer);
        }

        /// <summary>Immediately sets the volume for the given layer's stem.</summary>
        public void SetStemVolume(MusicLayer layer, float volume)
        {
            if (_slots.TryGetValue(layer, out var slot))
            {
                slot.targetVolume  = Mathf.Clamp01(volume);
                slot.source.volume = slot.targetVolume * _masterVolume * (_isDucked ? _duckVolume : 1f);
            }
        }

        /// <summary>
        /// Crossfades the given <paramref name="layer"/> from its current clip to
        /// <paramref name="newStem"/> over <paramref name="duration"/> seconds.
        /// </summary>
        public void CrossfadeStem(MusicLayer layer, StemDefinition newStem, float duration)
        {
            DeactivateStem(layer, duration * 0.5f);
            StartCoroutine(ActivateAfterDelay(newStem, duration * 0.5f, duration * 0.5f));
        }

        /// <summary>Plays a one-shot stinger clip on a temporary AudioSource.</summary>
        public void PlayStinger(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return;
            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, Vector3.zero, _masterVolume);
        }

        /// <summary>Applies ducking: reduces all stem volumes by <see cref="_duckVolume"/> factor.</summary>
        public void Duck()
        {
            _isDucked = true;
            ApplyMasterVolume();
        }

        /// <summary>Releases ducking: restores stem volumes.</summary>
        public void Unduck()
        {
            _isDucked = false;
            ApplyMasterVolume();
        }

        /// <summary>Sets the master volume and re-applies to all active stems.</summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            ApplyMasterVolume();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private StemSlot GetOrCreateSlot(MusicLayer layer)
        {
            if (!_slots.TryGetValue(layer, out var slot))
            {
                var go = new GameObject($"StemMixer_{layer}");
                go.transform.SetParent(transform, false);
                slot = new StemSlot
                {
                    layer  = layer,
                    source = go.AddComponent<AudioSource>(),
                };
                slot.source.playOnAwake = false;
                _slots[layer] = slot;
            }
            return slot;
        }

        private void ApplyMasterVolume()
        {
            float duck = _isDucked ? _duckVolume : 1f;
            foreach (var kv in _slots)
                kv.Value.source.volume = kv.Value.targetVolume * _masterVolume * duck;
        }

        private IEnumerator FadeVolume(AudioSource src, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                src.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            src.volume = to;
        }

        private IEnumerator FadeOutAndStop(AudioSource src, float duration)
        {
            float start = src.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                src.volume = Mathf.Lerp(start, 0f, elapsed / duration);
                yield return null;
            }
            src.Stop();
        }

        private IEnumerator ActivateAfterDelay(StemDefinition stem, float delay, float fadeIn)
        {
            yield return new WaitForSeconds(delay);
            ActivateStem(stem, fadeIn);
        }
    }
}
