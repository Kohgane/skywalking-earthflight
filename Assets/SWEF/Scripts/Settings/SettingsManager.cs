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

        // ── Current values ───────────────────────────────────────────────────────
        public float MasterVolume     { get; private set; } = DefaultMasterVolume;
        public float SfxVolume        { get; private set; } = DefaultSfxVolume;
        public bool  ComfortMode      { get; private set; } = DefaultComfortMode;
        public float TouchSensitivity { get; private set; } = DefaultTouchSensitivity;
        public float MaxSpeed         { get; private set; } = DefaultMaxSpeed;

        // ── References (optional — auto-found via FindFirstObjectByType if not set) ──
        [Header("Refs")]
        [SerializeField] private FlightController    flightController;
        [SerializeField] private TouchInputRouter    touchInputRouter;

        /// <summary>Raised whenever settings are saved/applied.</summary>
        public event Action OnSettingsChanged;

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyMasterVolume     = "SWEF_MasterVolume";
        private const string KeySfxVolume        = "SWEF_SfxVolume";
        private const string KeyComfortMode      = "SWEF_ComfortMode";
        private const string KeyTouchSensitivity = "SWEF_TouchSensitivity";
        private const string KeyMaxSpeed         = "SWEF_MaxSpeed";

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
            MasterVolume     = PlayerPrefs.GetFloat(KeyMasterVolume,     DefaultMasterVolume);
            SfxVolume        = PlayerPrefs.GetFloat(KeySfxVolume,        DefaultSfxVolume);
            ComfortMode      = PlayerPrefs.GetInt(KeyComfortMode,        DefaultComfortMode ? 1 : 0) == 1;
            TouchSensitivity = PlayerPrefs.GetFloat(KeyTouchSensitivity, DefaultTouchSensitivity);
            MaxSpeed         = PlayerPrefs.GetFloat(KeyMaxSpeed,         DefaultMaxSpeed);

            // Phase 10 — override with SaveManager values when present
            if (_saveManager != null && _saveManager.HasSaveFile())
            {
                MasterVolume     = _saveManager.GetFloat(KeyMasterVolume,     MasterVolume);
                SfxVolume        = _saveManager.GetFloat(KeySfxVolume,        SfxVolume);
                ComfortMode      = _saveManager.GetInt(KeyComfortMode,        ComfortMode ? 1 : 0) == 1;
                TouchSensitivity = _saveManager.GetFloat(KeyTouchSensitivity, TouchSensitivity);
                MaxSpeed         = _saveManager.GetFloat(KeyMaxSpeed,         MaxSpeed);
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
            PlayerPrefs.Save();

            // Phase 10 — mirror to SaveManager
            if (_saveManager != null)
            {
                _saveManager.SetFloat(KeyMasterVolume,     MasterVolume);
                _saveManager.SetFloat(KeySfxVolume,        SfxVolume);
                _saveManager.SetInt(KeyComfortMode,        ComfortMode ? 1 : 0);
                _saveManager.SetFloat(KeyTouchSensitivity, TouchSensitivity);
                _saveManager.SetFloat(KeyMaxSpeed,         MaxSpeed);
                _saveManager.Save();
            }

            ApplyAll();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>Resets all settings to their default values and saves.</summary>
        public void ResetToDefaults()
        {
            MasterVolume     = DefaultMasterVolume;
            SfxVolume        = DefaultSfxVolume;
            ComfortMode      = DefaultComfortMode;
            TouchSensitivity = DefaultTouchSensitivity;
            MaxSpeed         = DefaultMaxSpeed;
            Save();
        }

        // ── Setters ──────────────────────────────────────────────────────────
        public void SetMasterVolume(float v)     { MasterVolume     = Mathf.Clamp01(v); }
        public void SetSfxVolume(float v)        { SfxVolume        = Mathf.Clamp01(v); }
        public void SetComfortMode(bool b)       { ComfortMode      = b; }
        public void SetTouchSensitivity(float v) { TouchSensitivity = Mathf.Clamp(v, 0.5f, 3.0f); }
        public void SetMaxSpeed(float v)         { MaxSpeed         = Mathf.Clamp(v, 50f, 500f); }

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
