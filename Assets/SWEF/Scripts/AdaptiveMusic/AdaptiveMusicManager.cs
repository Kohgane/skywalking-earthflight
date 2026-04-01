// AdaptiveMusicManager.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Central singleton orchestrator for the Adaptive Music System.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    ///
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Polls <see cref="FlightContextAnalyzer"/> on a configurable interval.</item>
    ///   <item>Passes context to <see cref="MoodResolver"/> to get target mood + intensity.</item>
    ///   <item>Delegates mood transitions to <see cref="MusicTransitionController"/>.</item>
    ///   <item>Delegates layer activation to <see cref="IntensityController"/>.</item>
    ///   <item>Persists user preferences via <see cref="PlayerPrefs"/>.</item>
    /// </list>
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class AdaptiveMusicManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static AdaptiveMusicManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Components (auto-found if null)")]
        [SerializeField] private FlightContextAnalyzer   contextAnalyzer;
        [SerializeField] private MusicTransitionController transitionController;
        [SerializeField] private IntensityController     intensityController;
        [SerializeField] private StemMixer               stemMixer;
        [SerializeField] private BeatSyncClock           beatSyncClock;

        [Header("Profile")]
        [Tooltip("Default adaptive music profile (ScriptableObject).")]
        [SerializeField] private AdaptiveMusicProfile    defaultProfile;

        [Header("Polling")]
        [Tooltip("Seconds between flight-context polling ticks.")]
        [SerializeField, Range(0.1f, 2f)] private float contextUpdateInterval = 0.5f;

        // ── PlayerPrefs keys ──────────────────────────────────────────────────────
        private const string PrefEnabled        = "SWEF_Music_AdaptiveEnabled";
        private const string PrefMode           = "SWEF_Music_Mode";
        private const string PrefMasterVolume   = "SWEF_Music_MasterVolume";
        private const string PrefCrossfadeSpeed = "SWEF_Music_CrossfadeSpeed";
        private const string PrefMoodSensitivity = "SWEF_Music_MoodSensitivity";
        private const string PrefDisabledLayers = "SWEF_Music_DisabledLayers";

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired when the active mood changes. Parameters: (previousMood, newMood).</summary>
        public event Action<MusicMood, MusicMood> OnMoodChanged;

        /// <summary>Fired when the intensity value changes significantly.</summary>
        public event Action<float> OnIntensityChanged;

        /// <summary>Fired when a stem layer is activated.</summary>
        public event Action<MusicLayer> OnStemActivated;

        /// <summary>Fired when a stem layer is deactivated.</summary>
        public event Action<MusicLayer> OnStemDeactivated;

        // ── State ─────────────────────────────────────────────────────────────────
        private AdaptiveMusicProfile _activeProfile;
        private MusicMood   _lastMood      = MusicMood.Peaceful;
        private float       _lastIntensity = 0f;
        private bool        _adaptiveEnabled;
        private MusicMode   _musicMode;
        private float       _masterVolume       = 1f;
        private float       _crossfadeSpeed     = 1f;
        private float       _moodSensitivity    = 1f;
        private float       _pollTimer;

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

            AutoFindComponents();
            LoadPreferences();
            ApplyProfile(defaultProfile);
        }

        private void OnEnable()
        {
            if (transitionController != null)
                transitionController.OnTransitionStarted += HandleTransitionStarted;
        }

        private void OnDisable()
        {
            if (transitionController != null)
                transitionController.OnTransitionStarted -= HandleTransitionStarted;
        }

        private void Update()
        {
            if (!_adaptiveEnabled)
                return;

            _pollTimer += Time.deltaTime;
            if (_pollTimer >= contextUpdateInterval)
            {
                _pollTimer = 0f;
                PollContext();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Current active music mood.</summary>
        public MusicMood CurrentMood => transitionController != null
            ? transitionController.CurrentMood
            : _lastMood;

        /// <summary>Current normalised intensity (0–1).</summary>
        public float CurrentIntensity => _lastIntensity;

        /// <summary>Whether the adaptive music system is enabled.</summary>
        public bool IsEnabled => _adaptiveEnabled;

        /// <summary>Current music mode (adaptive / playlist / hybrid).</summary>
        public MusicMode Mode => _musicMode;

        /// <summary>Overrides the current mood immediately.</summary>
        public void SetMood(MusicMood mood)
        {
            transitionController?.RequestMood(mood);
        }

        /// <summary>Sets the intensity override (0–1).</summary>
        public void SetIntensity(float intensity)
        {
            _lastIntensity = Mathf.Clamp01(intensity);
            intensityController?.SetIntensity(_lastIntensity);
        }

        /// <summary>Enables or disables the adaptive music system.</summary>
        public void SetEnabled(bool enabled)
        {
            _adaptiveEnabled = enabled;
            PlayerPrefs.SetInt(PrefEnabled, enabled ? 1 : 0);

            if (!enabled)
                stemMixer?.StopAll();
        }

        /// <summary>Sets the music mode (adaptive / playlist / hybrid).</summary>
        public void SetMode(MusicMode mode)
        {
            _musicMode = mode;
            PlayerPrefs.SetInt(PrefMode, (int)mode);
        }

        /// <summary>Sets the master volume for all adaptive stems (0–1).</summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            stemMixer?.SetMasterVolume(_masterVolume);
            PlayerPrefs.SetFloat(PrefMasterVolume, _masterVolume);
        }

        /// <summary>Fades out all stems over <paramref name="duration"/> seconds.</summary>
        public void FadeOut(float duration = 3f)
        {
            StartCoroutine(FadeOutCoroutine(duration));
        }

        /// <summary>Pauses stem playback (does not fade).</summary>
        public void Pause() => _adaptiveEnabled = false;

        /// <summary>Resumes stem playback.</summary>
        public void Resume() => _adaptiveEnabled = true;

        /// <summary>Switches to the specified <see cref="AdaptiveMusicProfile"/> at runtime.</summary>
        public void ApplyProfile(AdaptiveMusicProfile profile)
        {
            if (profile == null)
                return;

            _activeProfile = profile;
            transitionController?.SetProfile(profile);
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private void AutoFindComponents()
        {
            if (contextAnalyzer      == null) contextAnalyzer      = GetComponent<FlightContextAnalyzer>()   ?? gameObject.AddComponent<FlightContextAnalyzer>();
            if (transitionController == null) transitionController = GetComponent<MusicTransitionController>()  ?? gameObject.AddComponent<MusicTransitionController>();
            if (intensityController  == null) intensityController  = GetComponent<IntensityController>()     ?? gameObject.AddComponent<IntensityController>();
            if (stemMixer            == null) stemMixer            = GetComponent<StemMixer>()               ?? gameObject.AddComponent<StemMixer>();
            if (beatSyncClock        == null) beatSyncClock        = GetComponent<BeatSyncClock>()           ?? gameObject.AddComponent<BeatSyncClock>();
        }

        private void LoadPreferences()
        {
            _adaptiveEnabled  = PlayerPrefs.GetInt(PrefEnabled, 1) == 1;
            _musicMode        = (MusicMode)PlayerPrefs.GetInt(PrefMode, 0);
            _masterVolume     = PlayerPrefs.GetFloat(PrefMasterVolume, 1f);
            _crossfadeSpeed   = PlayerPrefs.GetFloat(PrefCrossfadeSpeed, 1f);
            _moodSensitivity  = PlayerPrefs.GetFloat(PrefMoodSensitivity, 1f);

            stemMixer?.SetMasterVolume(_masterVolume);
        }

        private void PollContext()
        {
            if (contextAnalyzer == null) return;

            FlightMusicContext ctx  = contextAnalyzer.BuildContext();
            MusicMood targetMood    = MoodResolver.ResolveMood(ctx);
            float     targetIntensity = MoodResolver.ResolveIntensity(ctx, targetMood);

            // Request mood change
            transitionController?.RequestMood(targetMood);

            // Update intensity
            float delta = Mathf.Abs(targetIntensity - _lastIntensity);
            if (delta > 0.01f)
            {
                _lastIntensity = targetIntensity;
                intensityController?.SetIntensity(_lastIntensity);
                OnIntensityChanged?.Invoke(_lastIntensity);
            }
        }

        private void HandleTransitionStarted(MusicMood from, MusicMood to, float duration)
        {
            if (from == to) return;

            MusicMood previous = from;
            _lastMood = to;
            OnMoodChanged?.Invoke(previous, to);

            // Crossfade stems for the new mood
            if (_activeProfile == null || stemMixer == null) return;

            foreach (MusicLayer layer in System.Enum.GetValues(typeof(MusicLayer)))
            {
                StemDefinition stem = _activeProfile.GetStem(to, layer);
                if (stem != null && stem.audioClip != null)
                {
                    stemMixer.CrossfadeStem(layer, stem, duration);
                    OnStemActivated?.Invoke(layer);
                }
            }
        }

        private IEnumerator FadeOutCoroutine(float duration)
        {
            float start   = _masterVolume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                stemMixer?.SetMasterVolume(Mathf.Lerp(start, 0f, elapsed / duration));
                yield return null;
            }

            stemMixer?.StopAll();
            stemMixer?.SetMasterVolume(_masterVolume);
        }
    }
}
