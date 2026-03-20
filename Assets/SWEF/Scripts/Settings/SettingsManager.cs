using System;
using UnityEngine;
using SWEF.Flight;

namespace SWEF.Settings
{
    /// <summary>
    /// PlayerPrefs-based persistent settings for SWEF.
    /// Manages MasterVolume, SfxVolume, ComfortMode, TouchSensitivity, and MaxSpeed.
    /// Applies values to FlightController and TouchInputRouter on load/change.
    /// When <see cref="SWEF.Core.SaveManager"/> is available, settings are also read from
    /// and written to the JSON save file.
    /// Key prefix: "SWEF_"
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        // ── Defaults ────────────────────────────────────────────────────────
        public const float DefaultMasterVolume    = 0.7f;
        public const float DefaultSfxVolume       = 1.0f;
        public const bool  DefaultComfortMode     = true;
        public const float DefaultTouchSensitivity = 1.4f;
        public const float DefaultMaxSpeed        = 250f;
        public const bool  DefaultNotificationsEnabled = true;

        // ── Current values ───────────────────────────────────────────────────────
        public float MasterVolume         { get; private set; } = DefaultMasterVolume;
        public float SfxVolume            { get; private set; } = DefaultSfxVolume;
        public bool  ComfortMode          { get; private set; } = DefaultComfortMode;
        public float TouchSensitivity     { get; private set; } = DefaultTouchSensitivity;
        public float MaxSpeed             { get; private set; } = DefaultMaxSpeed;
        /// <summary>Whether local/push notifications are enabled for this device.</summary>
        public bool  NotificationsEnabled { get; private set; } = DefaultNotificationsEnabled;

        // ── Phase 16 — Haptics ───────────────────────────────────────────────────
        public const bool  DefaultHapticsEnabled  = true;
        public const float DefaultHapticIntensity = 1.0f;

        private const string KeyHapticsEnabled  = "SWEF_HapticsEnabled";
        private const string KeyHapticIntensity = "SWEF_HapticIntensity";

        /// <summary>Whether haptic feedback is enabled.</summary>
        public bool  HapticsEnabled  { get; private set; } = DefaultHapticsEnabled;

        /// <summary>Haptic intensity multiplier (0–1).</summary>
        public float HapticIntensity { get; private set; } = DefaultHapticIntensity;

        /// <summary>Raised when the haptics-enabled setting changes.</summary>
        public static event Action<bool>  OnHapticsSettingChanged;

        /// <summary>Raised when the haptic intensity setting changes.</summary>
        public static event Action<float> OnHapticIntensityChanged;

        // ── Phase 27 — Terrain settings ──────────────────────────────────────────
        public const bool  DefaultTerrainEnabled       = true;
        public const int   DefaultTerrainLODQuality    = 1;
        public const float DefaultTerrainRenderDistance = 20000f;

        private const string KeyTerrainEnabled         = "SWEF_TerrainEnabled";
        private const string KeyTerrainLODQuality      = "SWEF_TerrainLODQuality";
        private const string KeyTerrainRenderDistance  = "SWEF_TerrainRenderDistance";

        /// <summary>Whether procedural terrain generation is enabled.</summary>
        public bool  TerrainEnabled        { get; private set; } = DefaultTerrainEnabled;

        /// <summary>Terrain LOD quality preset (0 = lowest … 3 = highest).</summary>
        public int   TerrainLODQuality     { get; private set; } = DefaultTerrainLODQuality;

        /// <summary>Maximum terrain render distance in metres.</summary>
        public float TerrainRenderDistance { get; private set; } = DefaultTerrainRenderDistance;

        // ── Phase 28 — Spatial Audio settings ───────────────────────────────────
        public const bool DefaultSpatialAudioEnabled = true;
        public const bool DefaultDopplerEnabled      = true;
        public const bool DefaultReverbEnabled       = true;
        public const int  DefaultAudioQuality        = 1; // 0=Low, 1=Medium, 2=High

        private const string KeySpatialAudioEnabled = "SWEF_SpatialAudioEnabled";
        private const string KeyDopplerEnabled      = "SWEF_DopplerEnabled";
        private const string KeyReverbEnabled       = "SWEF_ReverbEnabled";
        private const string KeyAudioQuality        = "SWEF_AudioQuality";

        /// <summary>Whether 3D spatial audio is enabled.</summary>
        public bool SpatialAudioEnabled { get; private set; } = DefaultSpatialAudioEnabled;

        /// <summary>Whether Doppler effect processing is enabled.</summary>
        public bool DopplerEnabled      { get; private set; } = DefaultDopplerEnabled;

        /// <summary>Whether environment reverb is enabled.</summary>
        public bool ReverbEnabled       { get; private set; } = DefaultReverbEnabled;

        /// <summary>Audio quality preset (0 = Low / 8 sources, 1 = Medium / 16, 2 = High / 32).</summary>
        public int  AudioQuality        { get; private set; } = DefaultAudioQuality;

        // ── Phase 29 — Cloud Rendering settings ─────────────────────────────────
        public const bool   DefaultCloudRenderingEnabled = false;
        public const int    DefaultCloudQualityLevel     = 0; // 0=Auto
        public const string DefaultCloudServerRegion     = "auto";

        private const string KeyCloudRenderingEnabled = "SWEF_CloudRendering";
        private const string KeyCloudQualityLevel     = "SWEF_CloudQuality";
        private const string KeyCloudServerRegion     = "SWEF_CloudRegion";

        /// <summary>Whether cloud rendering / remote streaming is enabled.</summary>
        public bool   CloudRenderingEnabled { get; private set; } = DefaultCloudRenderingEnabled;

        /// <summary>Cloud streaming quality preset (0=Auto, 1=Ultra, 2=High, 3=Medium, 4=Low).</summary>
        public int    CloudQualityLevel     { get; private set; } = DefaultCloudQualityLevel;

        /// <summary>Preferred cloud server region (e.g. "auto", "US-East").</summary>
        public string CloudServerRegion     { get; private set; } = DefaultCloudServerRegion;

        // ── Phase 26 — Performance settings ─────────────────────────────────────
        public const bool DefaultAdaptiveQuality = true;
        public const bool DefaultDiagnosticsHUD  = false;

        private const string KeyAdaptiveQuality = "SWEF_AdaptiveQuality";
        private const string KeyDiagnosticsHUD  = "SWEF_DiagnosticsHUD";

        /// <summary>Whether adaptive quality adjustment is enabled.</summary>
        public bool AdaptiveQuality { get; private set; } = DefaultAdaptiveQuality;

        /// <summary>Whether the runtime diagnostics HUD is enabled.</summary>
        public bool DiagnosticsHUD  { get; private set; } = DefaultDiagnosticsHUD;

        // ── References (optional — auto-found via FindFirstObjectByType if not set) ──
        [Header("Refs")]
        [SerializeField] private FlightController    flightController;
        [SerializeField] private TouchInputRouter    touchInputRouter;

        /// <summary>Raised whenever settings are saved/applied.</summary>
        public event Action OnSettingsChanged;

        /// <summary>Raised when the notifications-enabled setting changes, passing the new value.</summary>
        public event Action<bool> OnNotificationSettingChanged;

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyMasterVolume          = "SWEF_MasterVolume";
        private const string KeySfxVolume             = "SWEF_SfxVolume";
        private const string KeyComfortMode           = "SWEF_ComfortMode";
        private const string KeyTouchSensitivity      = "SWEF_TouchSensitivity";
        private const string KeyMaxSpeed              = "SWEF_MaxSpeed";
        private const string KeyNotificationsEnabled  = "SWEF_NotificationsEnabled";

        // ── Phase 10 — SaveManager integration ──────────────────────────────
        private SWEF.Core.SaveManager _saveManager;

        private void Awake()
        {
            if (flightController == null)
                flightController = FindFirstObjectByType<FlightController>();
            if (touchInputRouter == null)
                touchInputRouter = FindFirstObjectByType<TouchInputRouter>();

            // Phase 10 — resolve SaveManager
            _saveManager = FindFirstObjectByType<SWEF.Core.SaveManager>();

            Load();
        }

        /// <summary>Loads all settings from PlayerPrefs (and SaveManager when available) and applies them.</summary>
        public void Load()
        {
            MasterVolume         = PlayerPrefs.GetFloat(KeyMasterVolume,    DefaultMasterVolume);
            SfxVolume            = PlayerPrefs.GetFloat(KeySfxVolume,       DefaultSfxVolume);
            ComfortMode          = PlayerPrefs.GetInt(KeyComfortMode,       DefaultComfortMode ? 1 : 0) == 1;
            TouchSensitivity     = PlayerPrefs.GetFloat(KeyTouchSensitivity, DefaultTouchSensitivity);
            MaxSpeed             = PlayerPrefs.GetFloat(KeyMaxSpeed,        DefaultMaxSpeed);
            NotificationsEnabled = PlayerPrefs.GetInt(KeyNotificationsEnabled,
                DefaultNotificationsEnabled ? 1 : 0) == 1;
            HapticsEnabled       = PlayerPrefs.GetInt(KeyHapticsEnabled,  DefaultHapticsEnabled  ? 1 : 0) == 1;
            HapticIntensity      = PlayerPrefs.GetFloat(KeyHapticIntensity, DefaultHapticIntensity);
            AdaptiveQuality      = PlayerPrefs.GetInt(KeyAdaptiveQuality, DefaultAdaptiveQuality ? 1 : 0) == 1;
            DiagnosticsHUD       = PlayerPrefs.GetInt(KeyDiagnosticsHUD,  DefaultDiagnosticsHUD  ? 1 : 0) == 1;
            TerrainEnabled       = PlayerPrefs.GetInt(KeyTerrainEnabled,       DefaultTerrainEnabled       ? 1 : 0) == 1;
            TerrainLODQuality    = PlayerPrefs.GetInt(KeyTerrainLODQuality,    DefaultTerrainLODQuality);
            TerrainRenderDistance = PlayerPrefs.GetFloat(KeyTerrainRenderDistance, DefaultTerrainRenderDistance);
            SpatialAudioEnabled  = PlayerPrefs.GetInt(KeySpatialAudioEnabled, DefaultSpatialAudioEnabled ? 1 : 0) == 1;
            DopplerEnabled       = PlayerPrefs.GetInt(KeyDopplerEnabled,      DefaultDopplerEnabled      ? 1 : 0) == 1;
            ReverbEnabled        = PlayerPrefs.GetInt(KeyReverbEnabled,       DefaultReverbEnabled       ? 1 : 0) == 1;
            AudioQuality         = PlayerPrefs.GetInt(KeyAudioQuality,        DefaultAudioQuality);
            CloudRenderingEnabled = PlayerPrefs.GetInt(KeyCloudRenderingEnabled, DefaultCloudRenderingEnabled ? 1 : 0) == 1;
            CloudQualityLevel    = PlayerPrefs.GetInt(KeyCloudQualityLevel,   DefaultCloudQualityLevel);
            CloudServerRegion    = PlayerPrefs.GetString(KeyCloudServerRegion, DefaultCloudServerRegion);

            // Phase 10 — override with SaveManager values when present
            if (_saveManager != null && _saveManager.HasSaveFile())
            {
                MasterVolume         = _saveManager.GetFloat(KeyMasterVolume,     MasterVolume);
                SfxVolume            = _saveManager.GetFloat(KeySfxVolume,        SfxVolume);
                ComfortMode          = _saveManager.GetInt(KeyComfortMode,        ComfortMode ? 1 : 0) == 1;
                TouchSensitivity     = _saveManager.GetFloat(KeyTouchSensitivity, TouchSensitivity);
                MaxSpeed             = _saveManager.GetFloat(KeyMaxSpeed,         MaxSpeed);
                NotificationsEnabled = _saveManager.GetInt(KeyNotificationsEnabled,
                    NotificationsEnabled ? 1 : 0) == 1;
            }

            ApplyAll();
        }

        /// <summary>Writes all current settings to PlayerPrefs and SaveManager (when available).</summary>
        public void Save()
        {
            PlayerPrefs.SetFloat(KeyMasterVolume,     MasterVolume);
            PlayerPrefs.SetFloat(KeySfxVolume,        SfxVolume);
            PlayerPrefs.SetInt(KeyComfortMode,        ComfortMode ? 1 : 0);
            PlayerPrefs.SetFloat(KeyTouchSensitivity, TouchSensitivity);
            PlayerPrefs.SetFloat(KeyMaxSpeed,         MaxSpeed);
            PlayerPrefs.SetInt(KeyNotificationsEnabled, NotificationsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KeyHapticsEnabled,     HapticsEnabled  ? 1 : 0);
            PlayerPrefs.SetFloat(KeyHapticIntensity,  HapticIntensity);
            PlayerPrefs.SetInt(KeyAdaptiveQuality,    AdaptiveQuality ? 1 : 0);
            PlayerPrefs.SetInt(KeyDiagnosticsHUD,     DiagnosticsHUD  ? 1 : 0);
            PlayerPrefs.SetInt(KeyTerrainEnabled,          TerrainEnabled       ? 1 : 0);
            PlayerPrefs.SetInt(KeyTerrainLODQuality,       TerrainLODQuality);
            PlayerPrefs.SetFloat(KeyTerrainRenderDistance, TerrainRenderDistance);
            PlayerPrefs.SetInt(KeySpatialAudioEnabled, SpatialAudioEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KeyDopplerEnabled,      DopplerEnabled      ? 1 : 0);
            PlayerPrefs.SetInt(KeyReverbEnabled,       ReverbEnabled       ? 1 : 0);
            PlayerPrefs.SetInt(KeyAudioQuality,        AudioQuality);
            PlayerPrefs.SetInt(KeyCloudRenderingEnabled, CloudRenderingEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KeyCloudQualityLevel,   CloudQualityLevel);
            PlayerPrefs.SetString(KeyCloudServerRegion, CloudServerRegion);
            PlayerPrefs.Save();

            // Phase 10 — mirror to SaveManager
            if (_saveManager != null)
            {
                _saveManager.SetFloat(KeyMasterVolume,     MasterVolume);
                _saveManager.SetFloat(KeySfxVolume,        SfxVolume);
                _saveManager.SetInt(KeyComfortMode,        ComfortMode ? 1 : 0);
                _saveManager.SetFloat(KeyTouchSensitivity, TouchSensitivity);
                _saveManager.SetFloat(KeyMaxSpeed,         MaxSpeed);
                _saveManager.SetInt(KeyNotificationsEnabled, NotificationsEnabled ? 1 : 0);
                _saveManager.Save();
            }

            ApplyAll();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>Resets all settings to their default values and saves.</summary>
        public void ResetToDefaults()
        {
            MasterVolume         = DefaultMasterVolume;
            SfxVolume            = DefaultSfxVolume;
            ComfortMode          = DefaultComfortMode;
            TouchSensitivity     = DefaultTouchSensitivity;
            MaxSpeed             = DefaultMaxSpeed;
            NotificationsEnabled = DefaultNotificationsEnabled;
            HapticsEnabled       = DefaultHapticsEnabled;
            HapticIntensity      = DefaultHapticIntensity;
            AdaptiveQuality      = DefaultAdaptiveQuality;
            DiagnosticsHUD       = DefaultDiagnosticsHUD;
            TerrainEnabled       = DefaultTerrainEnabled;
            TerrainLODQuality    = DefaultTerrainLODQuality;
            TerrainRenderDistance = DefaultTerrainRenderDistance;
            SpatialAudioEnabled  = DefaultSpatialAudioEnabled;
            DopplerEnabled       = DefaultDopplerEnabled;
            ReverbEnabled        = DefaultReverbEnabled;
            AudioQuality         = DefaultAudioQuality;
            CloudRenderingEnabled = DefaultCloudRenderingEnabled;
            CloudQualityLevel    = DefaultCloudQualityLevel;
            CloudServerRegion    = DefaultCloudServerRegion;
            Save();
        }

        // ── Setters ──────────────────────────────────────────────────────────
        public void SetMasterVolume(float v)     { MasterVolume     = Mathf.Clamp01(v); }
        public void SetSfxVolume(float v)        { SfxVolume        = Mathf.Clamp01(v); }
        public void SetComfortMode(bool b)       { ComfortMode      = b; }
        public void SetTouchSensitivity(float v) { TouchSensitivity = Mathf.Clamp(v, 0.5f, 3.0f); }
        public void SetMaxSpeed(float v)         { MaxSpeed         = Mathf.Clamp(v, 50f, 500f); }

        /// <summary>Sets whether local notifications are enabled and fires <see cref="OnNotificationSettingChanged"/>.</summary>
        public void SetNotificationsEnabled(bool b)
        {
            NotificationsEnabled = b;
            OnNotificationSettingChanged?.Invoke(b);
        }

        /// <summary>Sets whether haptics are enabled and fires <see cref="OnHapticsSettingChanged"/>.</summary>
        public void SetHapticsEnabled(bool b)
        {
            HapticsEnabled = b;
            OnHapticsSettingChanged?.Invoke(b);
        }

        /// <summary>Sets the haptic intensity multiplier and fires <see cref="OnHapticIntensityChanged"/>.</summary>
        public void SetHapticIntensity(float v)
        {
            HapticIntensity = Mathf.Clamp01(v);
            OnHapticIntensityChanged?.Invoke(HapticIntensity);
        }

        // ── Phase 26 setters ─────────────────────────────────────────────────────

        /// <summary>Sets whether adaptive quality is enabled.</summary>
        public void SetAdaptiveQuality(bool b) { AdaptiveQuality = b; }

        /// <summary>Sets whether the diagnostics HUD is enabled.</summary>
        public void SetDiagnosticsHUD(bool b) { DiagnosticsHUD = b; }

        // ── Phase 27 setters ─────────────────────────────────────────────────────

        /// <summary>Sets whether procedural terrain generation is enabled.</summary>
        public void SetTerrainEnabled(bool b) { TerrainEnabled = b; }

        /// <summary>Sets the terrain LOD quality preset (0–3).</summary>
        public void SetTerrainLODQuality(int v) { TerrainLODQuality = Mathf.Clamp(v, 0, 3); }

        /// <summary>Sets the maximum terrain render distance in metres.</summary>
        public void SetTerrainRenderDistance(float v) { TerrainRenderDistance = Mathf.Clamp(v, 1000f, 100000f); }

        // ── Phase 28 setters ─────────────────────────────────────────────────────

        /// <summary>Sets whether 3D spatial audio is enabled.</summary>
        public void SetSpatialAudioEnabled(bool b) { SpatialAudioEnabled = b; }

        /// <summary>Sets whether Doppler effect processing is enabled.</summary>
        public void SetDopplerEnabled(bool b) { DopplerEnabled = b; }

        /// <summary>Sets whether environment reverb is enabled.</summary>
        public void SetReverbEnabled(bool b) { ReverbEnabled = b; }

        /// <summary>Sets the audio quality preset (0 = Low, 1 = Medium, 2 = High).</summary>
        public void SetAudioQuality(int v) { AudioQuality = Mathf.Clamp(v, 0, 2); }

        // ── Phase 29 setters ─────────────────────────────────────────────────────

        /// <summary>Sets whether cloud rendering / remote streaming is enabled.</summary>
        public void SetCloudRenderingEnabled(bool b) { CloudRenderingEnabled = b; }

        /// <summary>Sets the cloud streaming quality preset (0=Auto, 1=Ultra … 4=Low).</summary>
        public void SetCloudQualityLevel(int v) { CloudQualityLevel = Mathf.Clamp(v, 0, 4); }

        /// <summary>Sets the preferred cloud server region.</summary>
        public void SetCloudServerRegion(string region) { CloudServerRegion = region ?? DefaultCloudServerRegion; }

        // ── Internal ─────────────────────────────────────────────────────────
        private void ApplyAll()
        {
            if (flightController != null)
            {
                flightController.comfortMode = ComfortMode;
                flightController.SetMaxSpeed(MaxSpeed);
            }
            if (touchInputRouter != null)
                touchInputRouter.SetSensitivity(TouchSensitivity);

            // Phase 26 — push adaptive quality to controller
            var aq = FindFirstObjectByType<SWEF.Performance.AdaptiveQualityController>();
            if (aq != null)
                aq.AutoAdjustEnabled = AdaptiveQuality;

            // Phase 27 — push terrain settings
            var terrainGen = FindFirstObjectByType<SWEF.Terrain.ProceduralTerrainGenerator>();
            if (terrainGen != null)
                terrainGen.gameObject.SetActive(TerrainEnabled);

            // Phase 28 — push spatial audio settings
            var doppler = FindFirstObjectByType<SWEF.Audio.DopplerEffectController>();
            if (doppler != null) doppler.IsEnabled = DopplerEnabled;

            var reverb = FindFirstObjectByType<SWEF.Audio.EnvironmentReverbController>();
            if (reverb != null) reverb.SetEnabled(ReverbEnabled);

            // Phase 29 — push cloud rendering settings
            var cloudMgr = FindFirstObjectByType<SWEF.CloudRendering.CloudRenderingManager>();
            if (cloudMgr != null)
            {
                if (CloudRenderingEnabled && !cloudMgr.IsCloudMode)
                    cloudMgr.EnableCloudRendering();
                else if (!CloudRenderingEnabled && cloudMgr.IsCloudMode)
                    cloudMgr.DisableCloudRendering();
            }

            var sessionMgr = FindFirstObjectByType<SWEF.CloudRendering.CloudSessionManager>();
            if (sessionMgr != null)
                sessionMgr.Config.region = CloudServerRegion;
        }
    }
}
