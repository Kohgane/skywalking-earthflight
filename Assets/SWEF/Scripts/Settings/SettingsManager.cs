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
        }
    }
}
