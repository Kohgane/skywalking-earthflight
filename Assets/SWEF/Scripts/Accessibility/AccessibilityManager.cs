using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Accessibility
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Built-in accessibility preset profiles for common needs.
    /// </summary>
    public enum AccessibilityPreset
    {
        /// <summary>All accessibility features disabled; default game settings.</summary>
        Default,
        /// <summary>Large text, high contrast, reduced motion, screen reader on.</summary>
        LowVision,
        /// <summary>Colorblind filter active (Deuteranopia by default).</summary>
        Colorblind,
        /// <summary>One-handed mode, larger touch targets, auto-level assist, sequential input.</summary>
        MotorImpaired,
        /// <summary>Subtitles and closed captions on, audio-to-haptic on.</summary>
        HearingImpaired,
        /// <summary>All assist features enabled at maximum settings.</summary>
        FullAssist
    }

    /// <summary>
    /// Central singleton that owns all accessibility preferences and notifies subsystems
    /// of changes.  Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class AccessibilityManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static AccessibilityManager Instance { get; private set; }

        // ── Persistence ───────────────────────────────────────────────────────────
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "accessibility_settings.json");

        // Legacy PlayerPrefs key (read once for migration, then removed)
        private const string LegacyKeyProfileJson = "SWEF_AccessibilityProfile";

        // ── Serialised fields ─────────────────────────────────────────────────────
        [Header("Defaults")]
        [SerializeField] private AccessibilityPreset defaultPreset = AccessibilityPreset.Default;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private AccessibilityProfile _profile = new AccessibilityProfile();
        private readonly Dictionary<string, bool> _featureFlags = new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>Active accessibility profile (read-only snapshot; use API to modify).</summary>
        public AccessibilityProfile Profile => _profile;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired whenever the active profile changes (any field).</summary>
        public event Action OnProfileChanged;

        /// <summary>Fired when the color-blind mode changes.</summary>
        public event Action<ColorBlindMode> OnColorBlindModeChanged;

        /// <summary>Fired when subtitle settings change.</summary>
        public event Action<bool> OnSubtitleSettingsChanged;

        /// <summary>Fired when an individual feature flag is toggled.</summary>
        public event Action<string, bool> OnFeatureToggled;

        /// <summary>Fired when a preset is applied.</summary>
        public event Action<AccessibilityPreset> OnPresetApplied;

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
            LoadProfile();
        }

        // ── Profile persistence ──────────────────────────────────────────────

        /// <summary>
        /// Persists the current profile to <c>accessibility_settings.json</c>.
        /// </summary>
        public void SaveProfile()
        {
            // Sync feature flags into the profile list before saving
            _profile.enabledFeatureKeys.Clear();
            foreach (var kvp in _featureFlags)
                if (kvp.Value) _profile.enabledFeatureKeys.Add(kvp.Key);

            try
            {
                string json = JsonUtility.ToJson(_profile, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                Debug.Log("[SWEF] Accessibility: Profile saved.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Accessibility: Failed to save profile — {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the profile from <c>accessibility_settings.json</c>; falls back to
        /// legacy PlayerPrefs, then to defaults.
        /// </summary>
        public void LoadProfile()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    _profile = JsonUtility.FromJson<AccessibilityProfile>(json);
                }
                else
                {
                    // Migrate from legacy PlayerPrefs
                    string legacyJson = PlayerPrefs.GetString(LegacyKeyProfileJson, string.Empty);
                    if (!string.IsNullOrEmpty(legacyJson))
                    {
                        _profile = JsonUtility.FromJson<AccessibilityProfile>(legacyJson);
                        PlayerPrefs.DeleteKey(LegacyKeyProfileJson);
                        PlayerPrefs.Save();
                        SaveProfile(); // write to new location
                    }
                    else
                    {
                        ApplyPresetDefaults(defaultPreset, notify: false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Accessibility: Failed to load profile — {ex.Message}. Using defaults.");
                _profile = new AccessibilityProfile();
            }

            // Restore feature flags from profile list
            _featureFlags.Clear();
            foreach (var key in _profile.enabledFeatureKeys)
                _featureFlags[key] = true;

            Debug.Log($"[SWEF] Accessibility: Profile loaded — preset: {_profile.activePreset}");
        }

        // ── Preset system ─────────────────────────────────────────────────────────

        /// <summary>
        /// Applies a named preset, overwriting all profile values, then notifies subsystems.
        /// </summary>
        /// <param name="preset">The preset to apply.</param>
        public void ApplyPreset(AccessibilityPreset preset)
        {
            ApplyPresetDefaults(preset, notify: true);
            SaveProfile();
            OnPresetApplied?.Invoke(preset);
        }

        private void ApplyPresetDefaults(AccessibilityPreset preset, bool notify)
        {
            _profile = new AccessibilityProfile { activePreset = preset };

            switch (preset)
            {
                case AccessibilityPreset.Default:
                    break;

                case AccessibilityPreset.LowVision:
                    _profile.profileName        = "Low Vision";
                    _profile.screenReaderEnabled = true;
                    _profile.highContrastUI      = true;
                    _profile.reducedMotion       = true;
                    _profile.hudScale            = 1.5f;
                    _profile.textScale           = 1.5f;
                    break;

                case AccessibilityPreset.Colorblind:
                    _profile.profileName         = "Color Blind Friendly";
                    _profile.colorBlindMode      = ColorBlindMode.Deuteranopia;
                    _profile.colorBlindIntensity = 1f;
                    break;

                case AccessibilityPreset.MotorImpaired:
                    _profile.profileName      = "Motor Impaired";
                    _profile.oneHandedMode    = true;
                    _profile.autoHoverAssist  = true;
                    _profile.hudScale         = 1.25f;
                    break;

                case AccessibilityPreset.HearingImpaired:
                    _profile.profileName       = "Hearing Impaired";
                    _profile.subtitleEnabled   = true;
                    _profile.audioDescriptions = true;
                    break;

                case AccessibilityPreset.FullAssist:
                    _profile.profileName         = "Full Assist";
                    _profile.screenReaderEnabled  = true;
                    _profile.highContrastUI       = true;
                    _profile.reducedMotion        = true;
                    _profile.hudScale             = 1.5f;
                    _profile.textScale            = 1.5f;
                    _profile.colorBlindMode       = ColorBlindMode.Deuteranopia;
                    _profile.oneHandedMode        = true;
                    _profile.autoHoverAssist      = true;
                    _profile.subtitleEnabled      = true;
                    _profile.audioDescriptions    = true;
                    break;
            }

            if (notify) NotifyAll();
        }

        // ── Feature flag system ──────────────────────────────────────────────────

        /// <summary>
        /// Checks whether a named feature flag is enabled.
        /// </summary>
        /// <param name="featureKey">Unique string key for the feature.</param>
        public bool IsFeatureEnabled(string featureKey)
        {
            return _featureFlags.TryGetValue(featureKey, out bool v) && v;
        }

        /// <summary>
        /// Toggles a named feature flag on or off at runtime.
        /// </summary>
        /// <param name="featureKey">Unique string key for the feature.</param>
        /// <param name="enabled">Desired state.</param>
        public void SetFeature(string featureKey, bool enabled)
        {
            if (string.IsNullOrEmpty(featureKey)) return;
            _featureFlags[featureKey] = enabled;
            OnFeatureToggled?.Invoke(featureKey, enabled);
            SaveProfile();
        }

        // ── Auto-detection hints ─────────────────────────────────────────────────

        /// <summary>
        /// Inspects OS-level accessibility settings and returns a suggested preset.
        /// Currently uses screen DPI and font scale as heuristics.
        /// </summary>
        public AccessibilityPreset SuggestPreset()
        {
            float dpi = Screen.dpi;
            // High DPI small screens (phones ≥ 400 DPI) suggest motor-impaired helpers
            if (dpi >= 400f) return AccessibilityPreset.MotorImpaired;
            // Large display (4K TV) suggests large text
            if (Screen.width >= 3840) return AccessibilityPreset.LowVision;
            return AccessibilityPreset.Default;
        }

        // ── Direct profile mutations ─────────────────────────────────────────────

        /// <summary>
        /// Applies a complete profile, saves it, and notifies all subsystems.
        /// </summary>
        public void ApplyProfile(AccessibilityProfile profile)
        {
            if (profile == null) return;
            _profile = profile;
            SaveProfile();
            NotifyAll();
            AccessibilityBridge.NotifyProfileChanged(_profile);
        }

        /// <summary>Returns the currently active accessibility profile.</summary>
        public AccessibilityProfile GetActiveProfile() => _profile;

        /// <summary>Resets all accessibility settings to the "Default" preset.</summary>
        public void ResetToDefault()
        {
            ApplyPreset(AccessibilityPreset.Default);
        }

        /// <summary>Sets a field on the active profile and broadcasts the change.</summary>
        public void SetProfileValue(Action<AccessibilityProfile> mutator)
        {
            if (mutator == null) return;
            mutator(_profile);
            SaveProfile();
            NotifyAll();
        }

        // ── Internal helpers ─────────────────────────────────────────────────────
        private void NotifyAll()
        {
            OnProfileChanged?.Invoke();
            OnColorBlindModeChanged?.Invoke(_profile.colorBlindMode);
            OnSubtitleSettingsChanged?.Invoke(_profile.subtitleEnabled);
        }
    }
}
