using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.UI
{
    /// <summary>
    /// Enhances the existing <see cref="AccessibilityManager"/> with additional Phase 16 features:
    /// colorblind-safe palettes, dynamic text scaling, screen-reader announcements, and reduced-motion
    /// propagation to visual subsystems.
    /// All settings are persisted in PlayerPrefs and applied at boot via
    /// <see cref="ApplySavedSettings"/>.
    /// </summary>
    public class AccessibilityController : MonoBehaviour
    {
        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyColorblindMode = "SWEF_ColorblindMode";
        private const string KeyTextScale      = "SWEF_TextScale";
        private const string KeyReducedMotion  = "SWEF_ReducedMotion";
        private const string KeyScreenReader   = "SWEF_ScreenReaderEnabled";

        // ── Defaults ─────────────────────────────────────────────────────────────
        public const float DefaultTextScale = 1.0f;

        // ── Properties ───────────────────────────────────────────────────────────
        /// <summary>Active colorblind correction mode.</summary>
        public ColorblindMode ActiveColorblindMode { get; private set; } = ColorblindMode.Normal;

        /// <summary>Current dynamic-text scale multiplier (0.8–2.0).</summary>
        public float TextScaleMultiplier { get; private set; } = DefaultTextScale;

        /// <summary>Whether reduced-motion mode is active.</summary>
        public bool ReducedMotionEnabled { get; private set; }

        /// <summary>Whether screen-reader announcements are enabled.</summary>
        public bool ScreenReaderEnabled { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Raised whenever the dynamic text scale changes.</summary>
        public event Action<float> OnTextScaleChanged;

        /// <summary>Raised whenever the colorblind mode changes.</summary>
        public event Action<ColorblindMode> OnColorblindModeChanged;

        // ── Original font-size caches ────────────────────────────────────────────
        private readonly Dictionary<Text, int>      _origTextSizes  = new Dictionary<Text, int>();
#if TEXTMESHPRO_PRESENT || UNITY_TEXTMESHPRO
        private readonly Dictionary<TMPro.TMP_Text, float> _origTmpSizes = new Dictionary<TMPro.TMP_Text, float>();
#endif

        // ── Colorblind-safe colour tables ────────────────────────────────────────
        // Maps common game colours (red/green/blue) to accessible alternatives per mode.
        // Keys are approximate hue categories; values are the replacement colours.
        private static readonly Dictionary<ColorblindMode, (Color dangerous, Color safe)[]> _cbMaps
            = new Dictionary<ColorblindMode, (Color, Color)[]>
        {
            [ColorblindMode.Protanopia]   = new[] { (Color.red, new Color(0f, 0.45f, 0.7f)), (Color.green, new Color(0.9f, 0.6f, 0f)) },
            [ColorblindMode.Deuteranopia] = new[] { (Color.red, new Color(0f, 0.45f, 0.7f)), (Color.green, new Color(0.9f, 0.6f, 0f)) },
            [ColorblindMode.Tritanopia]   = new[] { (Color.blue, new Color(0.9f, 0.6f, 0f)), (new Color(0f, 1f, 1f), new Color(1f, 0.4f, 0.7f)) },
            [ColorblindMode.Achromatopsia]= new[] { (Color.red, new Color(0.3f, 0.3f, 0.3f)), (Color.green, new Color(0.6f, 0.6f, 0.6f)), (Color.blue, new Color(0.7f, 0.7f, 0.7f)) },
        };

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            ApplySavedSettings();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Reads all persisted preferences and applies them. Called by BootManager.</summary>
        public void ApplySavedSettings()
        {
            int cbm = PlayerPrefs.GetInt(KeyColorblindMode, (int)ColorblindMode.Normal);
            ActiveColorblindMode = (ColorblindMode)cbm;

            TextScaleMultiplier  = PlayerPrefs.GetFloat(KeyTextScale, DefaultTextScale);
            TextScaleMultiplier  = Mathf.Clamp(TextScaleMultiplier, 0.8f, 2.0f);

            ReducedMotionEnabled = PlayerPrefs.GetInt(KeyReducedMotion, 0) == 1;
            ScreenReaderEnabled  = PlayerPrefs.GetInt(KeyScreenReader,  0) == 1;

            ApplyTextScale(TextScaleMultiplier);
            // Sync reduced-motion flag to the singleton manager
            AccessibilityManager.Instance?.SetReducedMotion(ReducedMotionEnabled);

            Debug.Log($"[SWEF] AccessibilityController: saved settings applied " +
                      $"(colorblind={ActiveColorblindMode}, scale={TextScaleMultiplier:F1}, " +
                      $"reducedMotion={ReducedMotionEnabled})");
        }

        // ── Colorblind support ────────────────────────────────────────────────────

        /// <summary>Applies a colorblind correction mode and persists the choice.</summary>
        public void SetColorblindMode(ColorblindMode mode)
        {
            ActiveColorblindMode = mode;
            PlayerPrefs.SetInt(KeyColorblindMode, (int)mode);
            PlayerPrefs.Save();
            OnColorblindModeChanged?.Invoke(mode);
            Debug.Log($"[SWEF] Colorblind mode: {mode}");
        }

        /// <summary>
        /// Returns a colorblind-safe replacement for <paramref name="original"/>
        /// based on the active <see cref="ColorblindMode"/>.
        /// Returns <paramref name="original"/> unchanged when mode is <see cref="ColorblindMode.Normal"/>.
        /// </summary>
        public Color GetAccessibleColor(Color original)
        {
            if (ActiveColorblindMode == ColorblindMode.Normal) return original;
            if (!_cbMaps.TryGetValue(ActiveColorblindMode, out var pairs)) return original;

            foreach (var (dangerous, safe) in pairs)
            {
                if (ColorsApproximatelyEqual(original, dangerous))
                    return safe;
            }
            return original;
        }

        // ── Dynamic text scaling ──────────────────────────────────────────────────

        /// <summary>
        /// Applies a text-scale multiplier (0.8–2.0) to all Text and TMP components
        /// found in the scene. Stores originals on first call to prevent cumulative drift.
        /// </summary>
        public void SetTextScale(float scale)
        {
            scale               = Mathf.Clamp(scale, 0.8f, 2.0f);
            TextScaleMultiplier = scale;
            PlayerPrefs.SetFloat(KeyTextScale, scale);
            PlayerPrefs.Save();

            ApplyTextScale(scale);
            OnTextScaleChanged?.Invoke(scale);
        }

        // ── Screen reader support ────────────────────────────────────────────────

        /// <summary>
        /// Queues an accessibility announcement for the system screen reader.
        /// Logs locally; platform TTS integration is stubbed for future implementation.
        /// </summary>
        public void Announce(string message)
        {
            if (!ScreenReaderEnabled || string.IsNullOrEmpty(message)) return;

            Debug.Log($"[SWEF] Accessibility announce: {message}");

#if UNITY_IOS && !UNITY_EDITOR
            // TODO: Integrate iOS UIAccessibility.PostNotification via native bridge
            // Example: UIAccessibilityPostNotification(UIAccessibilityAnnouncementNotification, message);
#elif UNITY_ANDROID && !UNITY_ANDROID_EDITOR
            // TODO: Integrate Android AccessibilityManager.sendAccessibilityEvent via AndroidJavaObject
#endif
        }

        /// <summary>Enables or disables screen-reader announcements and persists the setting.</summary>
        public void SetScreenReaderEnabled(bool enabled)
        {
            ScreenReaderEnabled = enabled;
            PlayerPrefs.SetInt(KeyScreenReader, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ── Reduced motion ───────────────────────────────────────────────────────

        /// <summary>
        /// Enables or disables the reduced-motion flag.
        /// Propagates to <see cref="AccessibilityManager"/> and persists the setting.
        /// </summary>
        public void SetReducedMotion(bool enabled)
        {
            ReducedMotionEnabled = enabled;
            PlayerPrefs.SetInt(KeyReducedMotion, enabled ? 1 : 0);
            PlayerPrefs.Save();

            AccessibilityManager.Instance?.SetReducedMotion(enabled);
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        private void ApplyTextScale(float scale)
        {
            Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in texts)
            {
                if (!_origTextSizes.ContainsKey(t))
                    _origTextSizes[t] = t.fontSize;
                t.fontSize = Mathf.RoundToInt(_origTextSizes[t] * scale);
            }

#if TEXTMESHPRO_PRESENT || UNITY_TEXTMESHPRO
            var tmpTexts = FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in tmpTexts)
            {
                if (!_origTmpSizes.ContainsKey(t))
                    _origTmpSizes[t] = t.fontSize;
                t.fontSize = _origTmpSizes[t] * scale;
            }
#endif
        }

        private static bool ColorsApproximatelyEqual(Color a, Color b, float tolerance = 0.15f)
        {
            return Mathf.Abs(a.r - b.r) < tolerance
                && Mathf.Abs(a.g - b.g) < tolerance
                && Mathf.Abs(a.b - b.b) < tolerance;
        }
    }
}
