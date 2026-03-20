using System.Collections.Generic;
using UnityEngine;
using SWEF.Settings;
using SWEF.Core;

namespace SWEF.Audio
{
    /// <summary>
    /// Singleton that manages a pool of 3D-positioned AudioSources for spatial audio playback.
    /// Pool size defaults to 32 and is configurable. Auto-expands with a warning when exhausted.
    /// Integrates with SettingsManager (master/SFX volume) and PerformanceManager (source count cap).
    /// </summary>
    public class SpatialAudioManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        public static SpatialAudioManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Pool")]
        [SerializeField] private int poolSize = 32;

        [Header("Refs (auto-found if null)")]
        [SerializeField] private SettingsManager settingsManager;
        [SerializeField] private PerformanceManager performanceManager;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly List<AudioSource> _pool       = new List<AudioSource>();
        private readonly Dictionary<string, AudioSource> _looping = new Dictionary<string, AudioSource>();

        private AudioListener _listener;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (settingsManager == null)
                settingsManager = FindFirstObjectByType<SettingsManager>();
            if (performanceManager == null)
                performanceManager = FindFirstObjectByType<PerformanceManager>();

            BuildPool(poolSize);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Plays a one-shot 3D sound at <paramref name="worldPos"/>.</summary>
        public void PlayAtPosition(AudioClip clip, Vector3 worldPos, float volume = 1f, float spatialBlend = 1f)
        {
            if (clip == null) return;
            var src = GetAvailableSource();
            if (src == null) return;

            src.transform.position = worldPos;
            src.clip         = clip;
            src.volume       = volume * MasterSfxVolume();
            src.spatialBlend = spatialBlend;
            src.loop         = false;
            src.Play();
        }

        /// <summary>Starts a looping spatial sound identified by <paramref name="id"/>. Replaces any prior loop with the same id.</summary>
        public AudioSource PlayLooping(AudioClip clip, Vector3 worldPos, string id)
        {
            if (clip == null) return null;

            StopLooping(id);

            var src = GetAvailableSource();
            if (src == null) return null;

            src.transform.position = worldPos;
            src.clip         = clip;
            src.volume       = MasterSfxVolume();
            src.spatialBlend = 1f;
            src.loop         = true;
            src.Play();

            _looping[id] = src;
            return src;
        }

        /// <summary>Stops the looping sound identified by <paramref name="id"/> and returns its source to the pool.</summary>
        public void StopLooping(string id)
        {
            if (!_looping.TryGetValue(id, out var src)) return;
            src.Stop();
            src.clip = null;
            _looping.Remove(id);
        }

        /// <summary>Synchronises the scene AudioListener with the given position and rotation.</summary>
        public void UpdateListenerPosition(Vector3 pos, Quaternion rot)
        {
            if (_listener == null)
                _listener = FindFirstObjectByType<AudioListener>();
            if (_listener == null) return;
            _listener.transform.SetPositionAndRotation(pos, rot);
        }

        /// <summary>Returns the maximum active source count for the given quality level (0=Low, 1=Medium, 2=High).</summary>
        public static int MaxActiveSourcesForQuality(int qualityLevel)
        {
            return qualityLevel switch
            {
                0 => 8,
                1 => 16,
                _ => 32,
            };
        }

        // ── Internal ──────────────────────────────────────────────────────────────

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
            src.spatialBlend = 1f;
            src.rolloffMode  = AudioRolloffMode.Logarithmic;
            src.maxDistance  = 5000f;
            return src;
        }

        private AudioSource GetAvailableSource()
        {
            int cap = ResolveSourceCap();

            // 1. Find a free source within the cap
            int active = 0;
            foreach (var s in _pool)
            {
                if (!s.isPlaying) return s;
                active++;
                if (active >= cap) break;
            }

            // 2. If within total pool size, use next idle source beyond cap
            foreach (var s in _pool)
            {
                if (!s.isPlaying) return s;
            }

            // 3. Expand pool with warning
            Debug.LogWarning("[SpatialAudioManager] Pool exhausted — expanding. Consider increasing poolSize.");
            var newSrc = CreateSource();
            _pool.Add(newSrc);
            return newSrc;
        }

        private int ResolveSourceCap()
        {
            if (performanceManager == null) return poolSize;
            int ql = (int)performanceManager.CurrentQuality; // 0=Low,1=Medium,2=High
            return MaxActiveSourcesForQuality(ql);
        }

        private float MasterSfxVolume()
        {
            if (settingsManager == null) return 1f;
            return settingsManager.MasterVolume * settingsManager.SfxVolume;
        }
    }
}
