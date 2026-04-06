// SpatialAudioManager.cs — Phase 118: Spatial Audio & 3D Soundscape
// Central manager singleton for 3D spatial audio, listener positioning,
// audio zone management and HRTF processing.
// Namespace: SWEF.SpatialAudio

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Phase 118 — Singleton MonoBehaviour that manages the full spatial audio
    /// system: source pool, listener tracking, zone transitions and HRTF toggle.
    /// Persists across scenes via <see cref="DontDestroyOnLoad"/>.
    /// </summary>
    public class SpatialAudioManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared instance of <see cref="SpatialAudioManager"/>.</summary>
        public static SpatialAudioManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Listener")]
        [SerializeField] private AudioListener audioListener;

        // ── State ─────────────────────────────────────────────────────────────────

        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private readonly Dictionary<string, AudioSource> _loopingSources = new Dictionary<string, AudioSource>();

        private AudioZoneState _zoneState = new AudioZoneState();
        private float _zoneTransitionTimer;

        /// <summary>Current audio zone state snapshot.</summary>
        public AudioZoneState CurrentZoneState => _zoneState;

        /// <summary>Whether the spatial audio system is fully initialised.</summary>
        public bool IsInitialised { get; private set; }

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (audioListener == null)
                audioListener = FindFirstObjectByType<AudioListener>();

            BuildPool(config != null ? config.maxAudioSources : 64);
            IsInitialised = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_zoneState.isTransitioning)
                TickZoneTransition();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Plays a one-shot 3D sound at the given world position.
        /// </summary>
        /// <param name="clip">Audio clip to play.</param>
        /// <param name="worldPos">World-space position of the sound.</param>
        /// <param name="volume">Playback volume (0–1).</param>
        /// <param name="pitch">Playback pitch multiplier.</param>
        public void PlayOneShot(AudioClip clip, Vector3 worldPos, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            var src = GetAvailableSource();
            if (src == null) return;

            src.transform.position = worldPos;
            src.volume = volume;
            src.pitch  = pitch;
            src.PlayOneShot(clip);
        }

        /// <summary>
        /// Starts a looping audio source identified by <paramref name="id"/>.
        /// Calling with the same id replaces the previous source.
        /// </summary>
        public void PlayLooping(string id, AudioClip clip, Vector3 worldPos, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            StopLooping(id);

            var src = GetAvailableSource();
            if (src == null) return;

            src.transform.position = worldPos;
            src.clip   = clip;
            src.volume = volume;
            src.pitch  = pitch;
            src.loop   = true;
            src.Play();
            _loopingSources[id] = src;
        }

        /// <summary>Stops and releases a looping source by id.</summary>
        public void StopLooping(string id)
        {
            if (!_loopingSources.TryGetValue(id, out var src)) return;
            src.Stop();
            src.clip = null;
            src.loop = false;
            _loopingSources.Remove(id);
        }

        /// <summary>
        /// Updates the position and volume of an existing looping source.
        /// </summary>
        public void UpdateLoopingSource(string id, Vector3 worldPos, float volume, float pitch)
        {
            if (!_loopingSources.TryGetValue(id, out var src)) return;
            src.transform.position = worldPos;
            src.volume = volume;
            src.pitch  = pitch;
        }

        /// <summary>
        /// Repositions the scene AudioListener to follow the given transform.
        /// </summary>
        public void UpdateListenerPosition(Vector3 pos, Quaternion rot)
        {
            if (audioListener == null)
                audioListener = FindFirstObjectByType<AudioListener>();
            if (audioListener == null) return;
            audioListener.transform.SetPositionAndRotation(pos, rot);
        }

        /// <summary>Initiates a crossfade transition to <paramref name="targetZone"/>.</summary>
        public void TransitionToZone(AudioZoneType targetZone, float duration = -1f)
        {
            if (_zoneState.currentZone == targetZone) return;
            _zoneState.targetZone     = targetZone;
            _zoneState.isTransitioning = true;
            _zoneState.blendWeight    = 0f;

            float dur = duration > 0f
                ? duration
                : config != null ? config.interiorExteriorTransitionDuration : 0.5f;
            _zoneTransitionTimer = dur;
        }

        /// <summary>Immediately sets the active zone without crossfading.</summary>
        public void SetZoneImmediate(AudioZoneType zone)
        {
            _zoneState.currentZone    = zone;
            _zoneState.targetZone     = zone;
            _zoneState.isTransitioning = false;
            _zoneState.blendWeight    = 1f;
        }

        /// <summary>Updates the flight state used for altitude-based audio mixing.</summary>
        public void UpdateFlightState(float altitudeMetres, float speedMs)
        {
            _zoneState.altitudeMetres        = altitudeMetres;
            _zoneState.speedMetresPerSecond  = speedMs;
        }

        // ── HRTF ─────────────────────────────────────────────────────────────────

        /// <summary>Toggles HRTF binaural processing on all pooled sources.</summary>
        public void SetHRTF(bool enabled)
        {
            foreach (var src in _pool)
                src.spatialize = enabled;
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private void BuildPool(int size)
        {
            for (int i = 0; i < size; i++)
                _pool.Add(CreateSource());
        }

        private AudioSource CreateSource()
        {
            var go  = new GameObject("SpatialAudioSource");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.spatialBlend = 1f; // full 3D
            src.rolloffMode  = AudioRolloffMode.Logarithmic;
            src.maxDistance  = config != null ? config.maxDistance : 5000f;
            src.minDistance  = config != null ? config.minDistance : 5f;
            src.dopplerLevel = config != null ? config.dopplerFactor : 1f;
            return src;
        }

        private AudioSource GetAvailableSource()
        {
            foreach (var src in _pool)
            {
                if (!src.isPlaying) return src;
            }
            // Expand pool with a warning
            Debug.LogWarning("[SpatialAudioManager] Source pool exhausted — expanding by 1.");
            var extra = CreateSource();
            _pool.Add(extra);
            return extra;
        }

        private void TickZoneTransition()
        {
            float speed = config != null
                ? 1f / Mathf.Max(0.05f, config.interiorExteriorTransitionDuration)
                : 2f;
            _zoneState.blendWeight = Mathf.MoveTowards(_zoneState.blendWeight, 1f, speed * Time.deltaTime);

            if (Mathf.Approximately(_zoneState.blendWeight, 1f))
            {
                _zoneState.currentZone    = _zoneState.targetZone;
                _zoneState.isTransitioning = false;
                _zoneState.blendWeight    = 1f;
            }
        }
    }
}
