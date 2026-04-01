// AdaptiveMusicManager.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Singleton MonoBehaviour — central orchestrator for the adaptive music system.
    ///
    /// Polls flight state every <see cref="ContextUpdateInterval"/> seconds,
    /// determines target <see cref="MusicMood"/> and intensity (0–1), manages
    /// stem routing via <see cref="StemMixer"/>, triggers crossfade transitions via
    /// <see cref="MusicTransitionController"/>, and exposes override/pause/resume API.
    ///
    /// User preferences (volume, enabled, enabled stems) are persisted via PlayerPrefs.
    /// </summary>
    public class AdaptiveMusicManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        public static AdaptiveMusicManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private AdaptiveMusicProfile      _profile;
        [SerializeField] private FlightContextAnalyzer     _contextAnalyzer;
        [SerializeField] private StemMixer                 _stemMixer;
        [SerializeField] private MusicTransitionController _transitionController;
        [SerializeField] private IntensityController       _intensityController;
        [SerializeField] private BeatSyncClock             _beatClock;

        [Header("Settings")]
        [Tooltip("How often (seconds) the mood/intensity are re-evaluated.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _contextUpdateInterval = 0.5f;

        [Tooltip("If false, the adaptive music system is muted but not destroyed.")]
        [SerializeField] private bool _enabled = true;

        [Header("Volume")]
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 0.8f;

        // ── PlayerPrefs keys ──────────────────────────────────────────────────

        private const string PrefEnabled = "SWEF.AdaptiveMusic.Enabled";
        private const string PrefVolume  = "SWEF.AdaptiveMusic.Volume";

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired when the active mood changes. Args: (previousMood, newMood).</summary>
        public event Action<MusicMood, MusicMood> OnMoodChanged;

        /// <summary>Fired when the intensity changes by more than 0.05.</summary>
        public event Action<float> OnIntensityChanged;

        /// <summary>Fired when a stem layer is activated.</summary>
        public event Action<MusicLayer> OnStemActivated;

        /// <summary>Fired when a stem layer is deactivated.</summary>
        public event Action<MusicLayer> OnStemDeactivated;

        // ── State ─────────────────────────────────────────────────────────────

        private MusicMood _currentMood      = MusicMood.Peaceful;
        private float     _currentIntensity;
        private float     _nextUpdateTime;
        private bool      _isPaused;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Current active mood.</summary>
        public MusicMood CurrentMood => _currentMood;

        /// <summary>Current intensity (0–1).</summary>
        public float CurrentIntensity => _currentIntensity;

        /// <summary>Whether the adaptive music system is active.</summary>
        public bool IsEnabled => _enabled;

        /// <summary>Update interval in seconds.</summary>
        public float ContextUpdateInterval => _contextUpdateInterval;

        /// <summary>Overrides the current mood immediately.</summary>
        public void SetMood(MusicMood mood)
        {
            var prev = _currentMood;
            _currentMood = mood;
            _transitionController?.ForceTransition(mood);
            if (prev != mood) OnMoodChanged?.Invoke(prev, mood);
        }

        /// <summary>Overrides the current intensity (0–1) immediately.</summary>
        public void SetIntensity(float intensity)
        {
            float clamped = Mathf.Clamp01(intensity);
            _currentIntensity = clamped;
            _intensityController?.SetIntensity(clamped);
            OnIntensityChanged?.Invoke(clamped);
        }

        /// <summary>Pauses all stem playback.</summary>
        public void Pause()
        {
            _isPaused = true;
            _stemMixer?.Duck();
        }

        /// <summary>Resumes all stem playback.</summary>
        public void Resume()
        {
            _isPaused = false;
            _stemMixer?.Unduck();
        }

        /// <summary>Fades out all stems over <paramref name="duration"/> seconds.</summary>
        public void FadeOut(float duration = 3f)
        {
            StartCoroutine(FadeOutCoroutine(duration));
        }

        /// <summary>Enables or disables the system, persisting the preference.</summary>
        public void SetEnabled(bool value)
        {
            _enabled = value;
            PlayerPrefs.SetInt(PrefEnabled, value ? 1 : 0);
            PlayerPrefs.Save();
            if (!value) FadeOut(2f);
        }

        /// <summary>Sets the master volume, persisting the preference.</summary>
        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PrefVolume, _volume);
            PlayerPrefs.Save();
            _stemMixer?.SetMasterVolume(_volume);
        }

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadPreferences();

            if (_stemMixer != null)
            {
                _stemMixer.OnStemActivated   += l => OnStemActivated?.Invoke(l);
                _stemMixer.OnStemDeactivated += l => OnStemDeactivated?.Invoke(l);
            }

            if (_transitionController != null)
                _transitionController.OnTransitionCompleted += OnTransitionComplete;
        }

        private void Update()
        {
            if (!_enabled || _isPaused) return;
            if (Time.time < _nextUpdateTime) return;

            _nextUpdateTime = Time.time + _contextUpdateInterval;
            EvaluateContext();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void EvaluateContext()
        {
            var ctx = _contextAnalyzer != null
                ? _contextAnalyzer.Context
                : FlightMusicContext.Default();

            MusicMood targetMood      = MoodResolver.ResolveMood(ctx);
            float     targetIntensity = MoodResolver.ResolveIntensity(ctx, targetMood);

            if (targetMood != _currentMood)
                _transitionController?.RequestTransition(targetMood);

            float delta = Mathf.Abs(targetIntensity - _currentIntensity);
            if (delta > 0.05f)
            {
                _currentIntensity = targetIntensity;
                _intensityController?.SetIntensity(targetIntensity);
                OnIntensityChanged?.Invoke(targetIntensity);
            }
        }

        private void OnTransitionComplete(MusicMood newMood)
        {
            var prev = _currentMood;
            _currentMood = newMood;
            if (prev != newMood) OnMoodChanged?.Invoke(prev, newMood);
        }

        private void LoadPreferences()
        {
            if (PlayerPrefs.HasKey(PrefEnabled))
                _enabled = PlayerPrefs.GetInt(PrefEnabled) != 0;
            if (PlayerPrefs.HasKey(PrefVolume))
                _volume = PlayerPrefs.GetFloat(PrefVolume);
        }

        private IEnumerator FadeOutCoroutine(float duration)
        {
            float start = _volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _stemMixer?.SetMasterVolume(Mathf.Lerp(start, 0f, elapsed / duration));
                yield return null;
            }
            _stemMixer?.SetMasterVolume(0f);
        }
    }
}
