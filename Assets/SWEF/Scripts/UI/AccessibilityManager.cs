using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.UI
{
    /// <summary>
    /// Singleton accessibility manager. Survives scene loads via DontDestroyOnLoad.
    /// Provides font scaling, high-contrast mode, and a reduced-motion flag.
    /// All settings are persisted in PlayerPrefs and restored on startup.
    /// </summary>
    public class AccessibilityManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static AccessibilityManager Instance { get; private set; }

        // ── Phase 16 — AccessibilityController integration ────────────────────────
        /// <summary>
        /// Returns the active <see cref="AccessibilityController"/> if one exists in the scene.
        /// </summary>
        public AccessibilityController Controller => FindFirstObjectByType<AccessibilityController>();

        /// <summary>
        /// Convenience wrapper: announces a message via <see cref="AccessibilityController.Announce"/>
        /// when the controller is present and the screen reader is enabled.
        /// </summary>
        public void Announce(string message) => Controller?.Announce(message);

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyFontScale     = "SWEF_FontScale";
        private const string KeyHighContrast  = "SWEF_HighContrast";
        private const string KeyReducedMotion = "SWEF_ReducedMotion";

        // ── Public static flag ───────────────────────────────────────────────────
        /// <summary>
        /// When <c>true</c>, other systems should skip or shorten animations and
        /// transitions. Set via <see cref="SetReducedMotion"/>.
        /// </summary>
        public static bool ReducedMotion { get; private set; }

        // ── Properties ───────────────────────────────────────────────────────────
        /// <summary>Current font scale factor (0.8–2.0).</summary>
        public float FontScale { get; private set; } = 1f;

        /// <summary>Whether high-contrast mode is active.</summary>
        public bool HighContrastEnabled { get; private set; }

        /// <summary>Whether reduced-motion mode is active.</summary>
        public bool ReducedMotionEnabled { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired whenever any accessibility setting changes.</summary>
        public event Action OnAccessibilityChanged;

        // ── Original font size cache ─────────────────────────────────────────────
        // Store original sizes so repeated SetFontScale calls don't compound.
        private readonly Dictionary<Text, int> _originalFontSizes = new Dictionary<Text, int>();
#if TEXTMESHPRO_PRESENT || UNITY_TEXTMESHPRO
        private readonly Dictionary<TMPro.TMP_Text, float> _originalTmpFontSizes = new Dictionary<TMPro.TMP_Text, float>();
#endif

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

            LoadPreferences();
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>
        /// Sets the font scale applied to all <see cref="Text"/> components in the scene.
        /// Clamped to [0.8, 2.0]. Uses stored original sizes to avoid cumulative scaling.
        /// </summary>
        public void SetFontScale(float scale)
        {
            scale     = Mathf.Clamp(scale, 0.8f, 2.0f);
            FontScale = scale;
            PlayerPrefs.SetFloat(KeyFontScale, scale);
            PlayerPrefs.Save();

            ApplyFontScale(scale);
            OnAccessibilityChanged?.Invoke();
        }

        /// <summary>
        /// Enables or disables high-contrast mode.
        /// When enabled, UI panels are set to solid black and all text to white.
        /// </summary>
        public void SetHighContrast(bool enabled)
        {
            HighContrastEnabled = enabled;
            PlayerPrefs.SetInt(KeyHighContrast, enabled ? 1 : 0);
            PlayerPrefs.Save();

            ApplyHighContrast(enabled);
            OnAccessibilityChanged?.Invoke();
        }

        /// <summary>
        /// Enables or disables the reduced-motion flag.
        /// Other systems should check <see cref="ReducedMotion"/> before running animations.
        /// </summary>
        public void SetReducedMotion(bool enabled)
        {
            ReducedMotionEnabled = enabled;
            ReducedMotion        = enabled;
            PlayerPrefs.SetInt(KeyReducedMotion, enabled ? 1 : 0);
            PlayerPrefs.Save();

            OnAccessibilityChanged?.Invoke();
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void LoadPreferences()
        {
            float scale = PlayerPrefs.GetFloat(KeyFontScale, 1f);
            bool  hc    = PlayerPrefs.GetInt(KeyHighContrast,  0) == 1;
            bool  rm    = PlayerPrefs.GetInt(KeyReducedMotion, 0) == 1;

            FontScale            = Mathf.Clamp(scale, 0.8f, 2.0f);
            HighContrastEnabled  = hc;
            ReducedMotionEnabled = rm;
            ReducedMotion        = rm;

            ApplyFontScale(FontScale);
            ApplyHighContrast(HighContrastEnabled);
        }

        private void ApplyFontScale(float scale)
        {
            // Legacy Unity UI Text — use stored original sizes to prevent cumulative scaling
            Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in texts)
            {
                if (!_originalFontSizes.ContainsKey(t))
                    _originalFontSizes[t] = t.fontSize;

                t.fontSize = Mathf.RoundToInt(_originalFontSizes[t] * scale);
            }

            // TextMeshPro (optional — compiled only when TMPro is present)
#if TEXTMESHPRO_PRESENT || UNITY_TEXTMESHPRO
            TMPro.TMP_Text[] tmpTexts = FindObjectsByType<TMPro.TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in tmpTexts)
            {
                if (!_originalTmpFontSizes.ContainsKey(t))
                    _originalTmpFontSizes[t] = t.fontSize;

                t.fontSize = _originalTmpFontSizes[t] * scale;
            }
#endif
        }

        private void ApplyHighContrast(bool enabled)
        {
            // Set all Images on GameObjects tagged "UIPanel" to black (alpha 0.9)
            Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var img in images)
            {
                if (img.gameObject.CompareTag("UIPanel"))
                    img.color = enabled ? new Color(0f, 0f, 0f, 0.9f) : Color.white;
            }

            // Set all Text components to white when high-contrast, default otherwise
            Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in texts)
                t.color = enabled ? Color.white : Color.black;
        }
    }
}
