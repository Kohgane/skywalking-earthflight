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

    // ── Serialisable profile ─────────────────────────────────────────────────────

    /// <summary>
    /// Serialisable container for all user accessibility preferences.
    /// Saved to JSON and loaded on startup.
    /// </summary>
    [Serializable]
    public class AccessibilityProfile
    {
        // General
        public AccessibilityPreset activePreset = AccessibilityPreset.Default;

        // Vision
        public bool screenReaderEnabled;
        public bool colorblindFilterEnabled;
        public int  colorblindMode;          // ColorblindMode enum cast to int for serialisation
        public bool highContrastEnabled;
        public float colorblindIntensity = 1f;

        // Motor
        public bool oneHandedModeEnabled;
        public bool sequentialInputEnabled;
        public bool gyroSteeringEnabled;
        public float deadZoneLeft  = 0.1f;
        public float deadZoneRight = 0.1f;

        // Hearing
        public bool subtitlesEnabled;
        public bool closedCaptionsEnabled;
        public bool audioToHapticEnabled;

        // UI
        public float uiScale = 1f;
        public bool  reducedMotion;
        public bool  simplifiedUI;
        public int   textSizeLevel;     // 0=Normal, 1=Large25%, 2=Large50%, 3=Large75%, 4=Large100%

        // Cognitive
        public bool  simplifiedFlightEnabled;
        public float gameSpeed = 1f;
        public int   hudInfoLevel;      // 0=Full, 1=Reduced, 2=Minimal
        public bool  remindersEnabled = true;

        // Haptic
        public float hapticIntensityMultiplier = 1f;
        public bool  hapticEnabled = true;

        // Feature flags (string key → enabled)
        public List<string> enabledFeatureKeys = new List<string>();
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

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyProfileJson = "SWEF_AccessibilityProfile";

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

        // ── Profile persistence ──────────────────────────────────────────────────

        /// <summary>
        /// Persists the current profile to <see cref="PlayerPrefs"/> as JSON.
        /// </summary>
        public void SaveProfile()
        {
            // Sync feature flags into the profile list before saving
            _profile.enabledFeatureKeys.Clear();
            foreach (var kvp in _featureFlags)
                if (kvp.Value) _profile.enabledFeatureKeys.Add(kvp.Key);

            string json = JsonUtility.ToJson(_profile, prettyPrint: true);
            PlayerPrefs.SetString(KeyProfileJson, json);
            PlayerPrefs.Save();
            Debug.Log("[SWEF Accessibility] Profile saved.");
        }

        /// <summary>
        /// Loads the profile from <see cref="PlayerPrefs"/>; falls back to defaults.
        /// </summary>
        public void LoadProfile()
        {
            string json = PlayerPrefs.GetString(KeyProfileJson, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    _profile = JsonUtility.FromJson<AccessibilityProfile>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF Accessibility] Failed to parse profile JSON: {ex.Message}");
                    _profile = new AccessibilityProfile();
                }
            }
            else
            {
                ApplyPresetDefaults(defaultPreset, notify: false);
            }

            // Restore feature flags from profile list
            _featureFlags.Clear();
            foreach (var key in _profile.enabledFeatureKeys)
                _featureFlags[key] = true;

            Debug.Log($"[SWEF Accessibility] Profile loaded — preset: {_profile.activePreset}");
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
                    _profile.screenReaderEnabled  = true;
                    _profile.highContrastEnabled  = true;
                    _profile.reducedMotion        = true;
                    _profile.uiScale              = 1.5f;
                    _profile.textSizeLevel        = 2;
                    break;

                case AccessibilityPreset.Colorblind:
                    _profile.colorblindFilterEnabled = true;
                    _profile.colorblindMode          = (int)ColorblindMode.Deuteranopia;
                    _profile.colorblindIntensity     = 1f;
                    break;

                case AccessibilityPreset.MotorImpaired:
                    _profile.oneHandedModeEnabled  = true;
                    _profile.sequentialInputEnabled = false;
                    _profile.gyroSteeringEnabled   = true;
                    _profile.deadZoneLeft          = 0.15f;
                    _profile.deadZoneRight         = 0.15f;
                    _profile.uiScale               = 1.25f;
                    break;

                case AccessibilityPreset.HearingImpaired:
                    _profile.subtitlesEnabled      = true;
                    _profile.closedCaptionsEnabled = true;
                    _profile.audioToHapticEnabled  = true;
                    break;

                case AccessibilityPreset.FullAssist:
                    _profile.screenReaderEnabled      = true;
                    _profile.highContrastEnabled       = true;
                    _profile.reducedMotion             = true;
                    _profile.uiScale                   = 1.5f;
                    _profile.textSizeLevel             = 2;
                    _profile.colorblindFilterEnabled   = true;
                    _profile.oneHandedModeEnabled      = true;
                    _profile.gyroSteeringEnabled       = true;
                    _profile.subtitlesEnabled          = true;
                    _profile.closedCaptionsEnabled     = true;
                    _profile.audioToHapticEnabled      = true;
                    _profile.simplifiedFlightEnabled   = true;
                    _profile.hudInfoLevel              = 1;
                    _profile.remindersEnabled          = true;
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
        }
    }
}
