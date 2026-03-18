using UnityEngine;

namespace SWEF.Settings
{
    /// <summary>
    /// Persists and exposes all user-configurable weather system settings.
    ///
    /// <para>Settings are stored in <see cref="PlayerPrefs"/> under keys prefixed with
    /// <c>SWEF_Weather_</c> and are compatible with the existing <see cref="SettingsManager"/>
    /// save/load pattern.</para>
    ///
    /// <para>Attach to the same persistent GameObject as <see cref="SettingsManager"/>
    /// or any object that survives scene loads.</para>
    /// </summary>
    public class WeatherSettings : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherSettings Instance { get; private set; }

        // ── Defaults ──────────────────────────────────────────────────────────────
        public const bool DefaultWeatherEnabled        = true;
        public const int  DefaultEffectsQuality        = 2;   // 0=Low, 1=Med, 2=High, 3=Ultra
        public const bool DefaultWeatherPhysicsEnabled = true;
        public const bool DefaultWeatherAudioEnabled   = true;
        public const bool DefaultManualOverrideEnabled = false;
        public const int  DefaultManualCondition       = 0;   // WeatherCondition.Clear

        // ── PlayerPrefs keys ──────────────────────────────────────────────────────
        private const string KeyEnabled          = "SWEF_Weather_Enabled";
        private const string KeyEffectsQuality   = "SWEF_Weather_EffectsQuality";
        private const string KeyPhysicsEnabled   = "SWEF_Weather_PhysicsEnabled";
        private const string KeyAudioEnabled     = "SWEF_Weather_AudioEnabled";
        private const string KeyManualOverride   = "SWEF_Weather_ManualOverride";
        private const string KeyManualCondition  = "SWEF_Weather_ManualCondition";

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Weather System")]
        [Tooltip("Enable or disable the entire weather system.")]
        [SerializeField] private bool weatherEnabled = DefaultWeatherEnabled;

        [Header("Effects Quality")]
        [Tooltip("Particle and visual effect quality level. 0=Low, 1=Medium, 2=High, 3=Ultra.")]
        [Range(0, 3)]
        [SerializeField] private int effectsQuality = DefaultEffectsQuality;

        [Header("Sub-systems")]
        [Tooltip("Apply wind, turbulence, icing, and thermals to flight physics.")]
        [SerializeField] private bool weatherPhysicsEnabled = DefaultWeatherPhysicsEnabled;

        [Tooltip("Play ambient weather audio (rain, wind, thunder, etc.).")]
        [SerializeField] private bool weatherAudioEnabled = DefaultWeatherAudioEnabled;

        [Header("Manual Override")]
        [Tooltip("When true, use ManualCondition instead of fetched/procedural weather.")]
        [SerializeField] private bool manualOverrideEnabled = DefaultManualOverrideEnabled;

        [Tooltip("The weather condition to use when manual override is active.")]
        [SerializeField] private SWEF.Weather.WeatherCondition manualCondition =
            SWEF.Weather.WeatherCondition.Clear;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when any weather setting changes.</summary>
        public static event System.Action OnWeatherSettingsChanged;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Whether the weather system is active.</summary>
        public bool WeatherEnabled        { get; private set; } = DefaultWeatherEnabled;

        /// <summary>Particle effect quality (0=Low … 3=Ultra).</summary>
        public int  EffectsQuality        { get; private set; } = DefaultEffectsQuality;

        /// <summary>Whether flight-physics modifiers (wind, turbulence, icing) are applied.</summary>
        public bool WeatherPhysicsEnabled { get; private set; } = DefaultWeatherPhysicsEnabled;

        /// <summary>Whether weather audio loops and SFX are active.</summary>
        public bool WeatherAudioEnabled   { get; private set; } = DefaultWeatherAudioEnabled;

        /// <summary>When <c>true</c> the manual condition override is used instead of real/procedural data.</summary>
        public bool ManualOverrideEnabled { get; private set; } = DefaultManualOverrideEnabled;

        /// <summary>The manually selected weather condition (used when <see cref="ManualOverrideEnabled"/> is true).</summary>
        public SWEF.Weather.WeatherCondition ManualCondition { get; private set; } =
            SWEF.Weather.WeatherCondition.Clear;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Load();
            ApplyToSubSystems();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Loads settings from <see cref="PlayerPrefs"/>.</summary>
        public void Load()
        {
            WeatherEnabled        = PlayerPrefs.GetInt(KeyEnabled,        DefaultWeatherEnabled        ? 1 : 0) == 1;
            EffectsQuality        = PlayerPrefs.GetInt(KeyEffectsQuality, DefaultEffectsQuality);
            WeatherPhysicsEnabled = PlayerPrefs.GetInt(KeyPhysicsEnabled, DefaultWeatherPhysicsEnabled ? 1 : 0) == 1;
            WeatherAudioEnabled   = PlayerPrefs.GetInt(KeyAudioEnabled,   DefaultWeatherAudioEnabled   ? 1 : 0) == 1;
            ManualOverrideEnabled = PlayerPrefs.GetInt(KeyManualOverride, DefaultManualOverrideEnabled ? 1 : 0) == 1;
            ManualCondition       = (SWEF.Weather.WeatherCondition)PlayerPrefs.GetInt(KeyManualCondition, DefaultManualCondition);
        }

        /// <summary>Persists current settings to <see cref="PlayerPrefs"/>.</summary>
        public void Save()
        {
            PlayerPrefs.SetInt(KeyEnabled,       WeatherEnabled        ? 1 : 0);
            PlayerPrefs.SetInt(KeyEffectsQuality, EffectsQuality);
            PlayerPrefs.SetInt(KeyPhysicsEnabled, WeatherPhysicsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KeyAudioEnabled,   WeatherAudioEnabled   ? 1 : 0);
            PlayerPrefs.SetInt(KeyManualOverride, ManualOverrideEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KeyManualCondition, (int)ManualCondition);
            PlayerPrefs.Save();

            ApplyToSubSystems();
            OnWeatherSettingsChanged?.Invoke();
        }

        /// <summary>Updates a single bool setting and saves.</summary>
        public void SetWeatherEnabled(bool value)        { WeatherEnabled        = value; Save(); }

        /// <summary>Updates effects quality (0–3) and saves.</summary>
        public void SetEffectsQuality(int value)         { EffectsQuality        = Mathf.Clamp(value, 0, 3); Save(); }

        /// <summary>Enables or disables weather physics modifiers and saves.</summary>
        public void SetWeatherPhysicsEnabled(bool value) { WeatherPhysicsEnabled = value; Save(); }

        /// <summary>Enables or disables weather audio and saves.</summary>
        public void SetWeatherAudioEnabled(bool value)   { WeatherAudioEnabled   = value; Save(); }

        /// <summary>Enables or disables the manual weather override and saves.</summary>
        public void SetManualOverride(bool value)        { ManualOverrideEnabled = value; Save(); }

        /// <summary>Sets the manual override condition and saves.</summary>
        public void SetManualCondition(SWEF.Weather.WeatherCondition cond) { ManualCondition = cond; Save(); }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void ApplyToSubSystems()
        {
            // Manual override: push condition directly into the state manager
            if (ManualOverrideEnabled && SWEF.Weather.WeatherStateManager.Instance != null)
            {
                var overrideData = SWEF.Weather.WeatherData.CreateClear();
                overrideData.condition = ManualCondition;
                SWEF.Weather.WeatherStateManager.Instance.SetTargetWeather(overrideData);
            }

            // Disable weather data service polling when system is off
            if (SWEF.Weather.WeatherDataService.Instance != null)
                SWEF.Weather.WeatherDataService.Instance.enabled = WeatherEnabled;

            // Disable audio controller when audio is off
            if (SWEF.Weather.WeatherAudioController.Instance != null)
                SWEF.Weather.WeatherAudioController.Instance.enabled = WeatherAudioEnabled;

            // Disable flight modifier when physics is off
            if (SWEF.Weather.WeatherFlightModifier.Instance != null)
                SWEF.Weather.WeatherFlightModifier.Instance.enabled = WeatherPhysicsEnabled;
        }
    }
}
