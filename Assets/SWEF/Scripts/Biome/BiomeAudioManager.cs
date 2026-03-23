// BiomeAudioManager.cs — SWEF Terrain Detail & Biome System
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Biome
{
    /// <summary>
    /// Singleton MonoBehaviour that manages biome-specific ambient audio,
    /// cross-fades between biomes, and supports dynamic layered audio.
    /// </summary>
    public class BiomeAudioManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static BiomeAudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            InitialiseSources();
        }

        #endregion

        #region Inspector Fields

        [Header("Audio Sources")]
        [Tooltip("Primary ambient audio source (current biome).")]
        [SerializeField] private AudioSource primarySource;

        [Tooltip("Secondary audio source used during cross-fades.")]
        [SerializeField] private AudioSource secondarySource;

        [Header("Biome Clips")]
        [Tooltip("Mapping of BiomeType to ambient AudioClip.")]
        [SerializeField] private BiomeAudioClipEntry[] biomeClips;

        [Header("Volume")]
        [Tooltip("Master ambient volume 0–1.")]
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.6f;

        [Tooltip("Default cross-fade duration in seconds.")]
        [SerializeField] private float defaultCrossFadeDuration = 2f;

        #endregion

        #region Private State

        private BiomeType   _currentBiome;
        private Coroutine   _fadeRoutine;
        private readonly Dictionary<string, AudioSource> _extraLayers = new Dictionary<string, AudioSource>();

        #endregion

        #region Public API

        /// <summary>
        /// Immediately switches ambient audio to the specified biome (no cross-fade).
        /// </summary>
        /// <param name="biome">Target biome.</param>
        public void SetCurrentBiome(BiomeType biome)
        {
            _currentBiome = biome;
            var clip      = GetClipForBiome(biome);
            if (primarySource == null) return;

            primarySource.clip   = clip;
            primarySource.volume = ambientVolume;
            if (clip != null) primarySource.Play();
            else              primarySource.Stop();
        }

        /// <summary>
        /// Cross-fades ambient audio from the current biome to <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Destination biome.</param>
        /// <param name="duration">Cross-fade duration in seconds.</param>
        public void CrossFadeToBiome(BiomeType target, float duration)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(CrossFadeRoutine(target, duration > 0f ? duration : defaultCrossFadeDuration));
        }

        /// <summary>
        /// Sets the master ambient volume for primary and secondary sources.
        /// </summary>
        /// <param name="volume">Volume 0–1.</param>
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            if (primarySource   != null) primarySource.volume   = ambientVolume;
            if (secondarySource != null) secondarySource.volume = 0f; // secondary fades independently
        }

        /// <summary>
        /// Adds a dynamic audio layer (e.g. weather overlay) on a new AudioSource component.
        /// </summary>
        /// <param name="layerId">Unique identifier for this layer.</param>
        /// <param name="clip">Clip to play.</param>
        /// <param name="volume">Playback volume 0–1.</param>
        public void AddAudioLayer(string layerId, AudioClip clip, float volume)
        {
            if (_extraLayers.ContainsKey(layerId)) RemoveAudioLayer(layerId, 0f);

            var src         = gameObject.AddComponent<AudioSource>();
            src.clip        = clip;
            src.volume      = Mathf.Clamp01(volume);
            src.loop        = true;
            src.playOnAwake = false;
            src.Play();

            _extraLayers[layerId] = src;
        }

        /// <summary>
        /// Fades out and removes a previously added audio layer.
        /// </summary>
        /// <param name="layerId">Layer identifier to remove.</param>
        /// <param name="fadeOutDuration">Fade-out time in seconds (0 = instant stop).</param>
        public void RemoveAudioLayer(string layerId, float fadeOutDuration)
        {
            if (!_extraLayers.TryGetValue(layerId, out var src)) return;
            _extraLayers.Remove(layerId);
            StartCoroutine(FadeOutAndDestroyRoutine(src, fadeOutDuration));
        }

        #endregion

        #region Private Methods

        private void InitialiseSources()
        {
            if (primarySource   == null) primarySource   = gameObject.AddComponent<AudioSource>();
            if (secondarySource == null) secondarySource = gameObject.AddComponent<AudioSource>();

            foreach (var src in new[] { primarySource, secondarySource })
            {
                src.loop        = true;
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2-D ambient
            }
        }

        private AudioClip GetClipForBiome(BiomeType biome)
        {
            if (biomeClips == null) return null;
            foreach (var entry in biomeClips)
                if (entry.biomeType == biome) return entry.clip;
            return null;
        }

        private IEnumerator CrossFadeRoutine(BiomeType target, float duration)
        {
            var newClip = GetClipForBiome(target);
            if (secondarySource == null) yield break;

            secondarySource.clip   = newClip;
            secondarySource.volume = 0f;
            if (newClip != null) secondarySource.Play();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / duration);
                if (primarySource   != null) primarySource.volume   = Mathf.Lerp(ambientVolume, 0f, t);
                if (secondarySource != null) secondarySource.volume = Mathf.Lerp(0f, ambientVolume, t);
                yield return null;
            }

            // Swap references
            if (primarySource != null) primarySource.Stop();
            (primarySource, secondarySource) = (secondarySource, primarySource);
            // Reset the new secondary source so it is ready for the next cross-fade
            if (secondarySource != null)
            {
                secondarySource.Stop();
                secondarySource.clip   = null;
                secondarySource.volume = 0f;
            }
            _currentBiome    = target;
            _fadeRoutine     = null;
        }

        private static IEnumerator FadeOutAndDestroyRoutine(AudioSource src, float duration)
        {
            if (src == null) yield break;
            float startVol = src.volume;
            float elapsed  = 0f;
            while (elapsed < duration && src != null)
            {
                elapsed    += Time.deltaTime;
                src.volume  = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            if (src != null)
            {
                src.Stop();
                Destroy(src);
            }
        }

        #endregion
    }

    /// <summary>
    /// Maps a <see cref="BiomeType"/> to an <see cref="AudioClip"/> for serialisation in the Inspector.
    /// </summary>
    [Serializable]
    public struct BiomeAudioClipEntry
    {
        /// <summary>Biome this clip is associated with.</summary>
        [Tooltip("Biome this ambient clip belongs to.")]
        public BiomeType biomeType;

        /// <summary>Audio clip to play for this biome.</summary>
        [Tooltip("Ambient audio clip for this biome.")]
        public AudioClip clip;
    }
}
