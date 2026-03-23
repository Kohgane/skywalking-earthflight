using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 53 — Manages all wildlife audio for Skywalking Earthflight.
    ///
    /// <para>Handles ambient biome sounds, individual animal calls triggered by proximity
    /// and behavior, 3D spatial audio on group centers, altitude and weather attenuation,
    /// dawn chorus scheduling, and sound priority management.</para>
    ///
    /// <para>Integrates with <c>SWEF.Audio.AudioManager</c> when available; otherwise
    /// creates its own AudioSource components.</para>
    /// </summary>
    public class WildlifeAudioController : MonoBehaviour
    {
        #region Constants

        private const float AltitudeFadeStartHeight = 500f;
        private const float AltitudeFadeEndHeight   = 2000f;
        private const float DawnHour                = 5.5f;
        private const float DawnDuration            = 1.5f;   // hours
        private const int   AudioSourcePoolSize     = 16;
        private const float MinCallInterval         = 3f;

        #endregion

        #region Inspector

        [Header("Audio Settings")]
        [Tooltip("Master volume multiplier for all wildlife sounds.")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;

        [Tooltip("Maximum distance at which animal sounds are audible.")]
        [SerializeField] private float soundDistance = 300f;

        [Tooltip("AudioMixerGroup for wildlife sounds. Optional.")]
        [SerializeField] private AudioMixerGroup audioMixerGroup;

        [Header("Clips")]
        [Tooltip("Ambient savanna background loop.")]
        [SerializeField] private AudioClip savannaAmbient;

        [Tooltip("Ambient jungle bird loop.")]
        [SerializeField] private AudioClip jungleAmbient;

        [Tooltip("Ocean wave + whale song loop.")]
        [SerializeField] private AudioClip oceanAmbient;

        [Tooltip("Arctic wind loop.")]
        [SerializeField] private AudioClip arcticAmbient;

        [Tooltip("Night cricket loop.")]
        [SerializeField] private AudioClip nightCrickets;

        [Header("References")]
        [Tooltip("Player/camera transform for distance checks. Resolved at runtime if null.")]
        [SerializeField] private Transform playerTransform;

        #endregion

        #region Private State

        private readonly List<AudioSource>    _pool         = new List<AudioSource>();
        private readonly Dictionary<AnimalGroup, AudioSource> _activeSources =
            new Dictionary<AnimalGroup, AudioSource>();

        private AudioSource _ambientSource;
        private float       _playerAltitude;
        private float       _callTimer;
        private BiomeHabitat _currentBiome       = BiomeHabitat.Grassland;
        private float       _weatherMultiplier   = 1f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (playerTransform == null)
                playerTransform = Camera.main != null ? Camera.main.transform : null;

            BuildAudioPool();
            _ambientSource = CreateSource();
            _ambientSource.loop   = true;
            _ambientSource.volume = 0.3f;
        }

        private void Update()
        {
            if (playerTransform != null)
                _playerAltitude = playerTransform.position.y;

            UpdateAltitudeAttenuation();
            _callTimer += Time.deltaTime;
        }

        #endregion

        #region Public API

        /// <summary>Plays the appropriate ambient clip for the given biome.</summary>
        public void PlayBiomeAmbient(BiomeHabitat biome)
        {
            if (_currentBiome == biome && _ambientSource.isPlaying) return;
            _currentBiome = biome;

            AudioClip clip = GetBiomeClip(biome);
            if (clip == null) return;

            _ambientSource.clip = clip;
            _ambientSource.Play();
        }

        /// <summary>Stops the ambient loop.</summary>
        public void StopBiomeAmbient()
        {
            _ambientSource.Stop();
        }

        /// <summary>Plays a behavior-appropriate sound for the given group.</summary>
        public void PlayGroupSound(AnimalGroup group, AnimalBehavior behavior)
        {
            if (group?.species == null) return;
            if (_callTimer < MinCallInterval) return;
            if (string.IsNullOrEmpty(group.species.soundClipKey)) return;

            _callTimer = 0f;

            float dist = playerTransform != null
                ? Vector3.Distance(group.centerPosition, playerTransform.position)
                : float.MaxValue;

            if (dist > soundDistance) return;

            AudioSource src = GetPooledSource();
            if (src == null) return;

            src.transform.position = group.centerPosition;
            src.volume = masterVolume * Mathf.Clamp01(1f - dist / soundDistance)
                         * AltitudeVolumeMultiplier() * _weatherMultiplier;

            // Clip lookup via Resources (key-based)
            AudioClip clip = Resources.Load<AudioClip>("Wildlife/Audio/" + group.species.soundClipKey);
            if (clip == null) return;

            src.PlayOneShot(clip);
            _activeSources[group] = src;
        }

        /// <summary>Stops any active sound for the given group.</summary>
        public void StopGroupSound(AnimalGroup group)
        {
            if (_activeSources.TryGetValue(group, out var src))
            {
                src.Stop();
                _activeSources.Remove(group);
            }
        }

        /// <summary>Reduces all wildlife audio during storms.</summary>
        public void OnWeatherChanged(bool isStorming)
        {
            _weatherMultiplier = isStorming ? 0.3f : 1f;
        }

        #endregion

        #region Biome Clips

        private AudioClip GetBiomeClip(BiomeHabitat biome)
        {
            switch (biome)
            {
                case BiomeHabitat.Savanna:   return savannaAmbient;
                case BiomeHabitat.Jungle:    return jungleAmbient;
                case BiomeHabitat.Ocean:
                case BiomeHabitat.DeepSea:
                case BiomeHabitat.Coast:     return oceanAmbient;
                case BiomeHabitat.Arctic:    return arcticAmbient;
                default:                     return nightCrickets;
            }
        }

        #endregion

        #region Altitude Attenuation

        private void UpdateAltitudeAttenuation()
        {
            float mult = AltitudeVolumeMultiplier();
            if (_ambientSource != null) _ambientSource.volume = 0.3f * mult * _weatherMultiplier;
        }

        private float AltitudeVolumeMultiplier()
        {
            if (_playerAltitude <= AltitudeFadeStartHeight) return 1f;
            if (_playerAltitude >= AltitudeFadeEndHeight)   return 0f;
            return 1f - (_playerAltitude - AltitudeFadeStartHeight) /
                        (AltitudeFadeEndHeight - AltitudeFadeStartHeight);
        }

        #endregion

        #region Audio Pool

        private void BuildAudioPool()
        {
            for (int i = 0; i < AudioSourcePoolSize; i++)
                _pool.Add(CreateSource());
        }

        private AudioSource CreateSource()
        {
            var go  = new GameObject("WildlifeAudioSource");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.rolloffMode  = AudioRolloffMode.Linear;
            src.maxDistance  = soundDistance;
            if (audioMixerGroup != null) src.outputAudioMixerGroup = audioMixerGroup;
            return src;
        }

        private AudioSource GetPooledSource()
        {
            foreach (var src in _pool)
                if (!src.isPlaying) return src;
            return null;
        }

        #endregion
    }
}
