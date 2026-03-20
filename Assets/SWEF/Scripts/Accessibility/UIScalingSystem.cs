using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Accessibility
{
    /// <summary>
    /// Manages global UI scaling, DPI-aware base scale, large text mode,
    /// reduced motion, and focus highlight for accessibility.
    /// Integrates with all <see cref="CanvasScaler"/> components in the scene.
    /// </summary>
    public class UIScalingSystem : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static UIScalingSystem Instance { get; private set; }

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyScale          = "SWEF_UIScale";
        private const string KeyTextLevel      = "SWEF_UITextLevel";
        private const string KeyReducedMotion  = "SWEF_UIReducedMotion";
        private const string KeySimplified     = "SWEF_UISimplified";
        private const string KeySpacing        = "SWEF_UISpacing";

        // ── Serialised fields ────────────────────────────────────────────────────
        [Header("Global Scale")]
        [SerializeField] [Range(0.5f, 3.0f)] private float globalScale = 1f;

        [Header("Large Text")]
        /// <summary>0=Normal,1=+25%,2=+50%,3=+75%,4=+100%</summary>
        [SerializeField] [Range(0, 4)] private int textSizeLevel = 0;

        [Header("Spacing")]
        [SerializeField] [Range(0f, 2f)] private float spacingMultiplier = 1f;

        [Header("Reduced Motion")]
        [SerializeField] private bool reducedMotion = false;

        [Header("Simplified UI")]
        [SerializeField] private bool simplifiedUI = false;
        [SerializeField] private List<GameObject> nonEssentialElements = new List<GameObject>();

        [Header("Focus Highlight")]
        [SerializeField] private Color  focusOutlineColor    = Color.yellow;
        [SerializeField] [Range(1f, 12f)] private float focusOutlineWidth = 4f;
        [SerializeField] private bool   focusPulseEnabled    = true;
        [SerializeField] [Range(0.3f, 3f)] private float focusPulseSpeed = 1.5f;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private readonly List<CanvasScaler> _scalers = new List<CanvasScaler>();
        private readonly Dictionary<RectTransform, float> _baseScales   = new Dictionary<RectTransform, float>();
        private readonly Dictionary<LayoutGroup, float>   _baseSpacings = new Dictionary<LayoutGroup, float>();
        private Coroutine    _pulseRoutine;
        private GameObject   _focusedObject;

        /// <summary>Current global UI scale factor.</summary>
        public float GlobalScale => globalScale;

        /// <summary>Whether reduced motion is active.</summary>
        public bool ReducedMotion => reducedMotion;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired when the global scale changes.</summary>
        public event Action<float> OnScaleChanged;

        /// <summary>Fired when reduced-motion mode is toggled.</summary>
        public event Action<bool> OnReducedMotionToggled;

        /// <summary>Fired when simplified-UI mode is toggled.</summary>
        public event Action<bool> OnSimplifiedUIToggled;

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

        private void Start()
        {
            RefreshScalers();
            ApplyScale(globalScale);
            ApplyTextSizeLevel(textSizeLevel);
            ApplySimplifiedUI(simplifiedUI);
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the global UI scale factor (0.5–3.0) and applies it to all canvas scalers.
        /// </summary>
        public void SetGlobalScale(float scale)
        {
            globalScale = Mathf.Clamp(scale, 0.5f, 3.0f);
            ApplyScale(globalScale);
            PlayerPrefs.SetFloat(KeyScale, globalScale);
            PlayerPrefs.Save();
            OnScaleChanged?.Invoke(globalScale);
        }

        /// <summary>
        /// Auto-detects device DPI and returns a suggested base scale for comfortable viewing.
        /// </summary>
        public float SuggestScaleForDPI()
        {
            float dpi = Screen.dpi;
            if (dpi <= 0f) return 1f;          // Unknown DPI
            if (dpi < 100f) return 0.75f;       // Large TV / monitor
            if (dpi < 160f) return 1.0f;        // Standard desktop/tablet
            if (dpi < 280f) return 1.25f;       // High-DPI laptop / iPad
            return 1.5f;                         // Phone / ultra-HiDPI
        }

        /// <summary>
        /// Sets the large-text level (0=Normal, 1=+25%, 2=+50%, 3=+75%, 4=+100%).
        /// </summary>
        public void SetTextSizeLevel(int level)
        {
            textSizeLevel = Mathf.Clamp(level, 0, 4);
            ApplyTextSizeLevel(textSizeLevel);
            PlayerPrefs.SetInt(KeyTextLevel, textSizeLevel);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Sets the spacing multiplier for buttons and UI elements (1.0 = default).
        /// </summary>
        public void SetSpacingMultiplier(float multiplier)
        {
            spacingMultiplier = Mathf.Clamp(multiplier, 0f, 2f);
            ApplySpacing(spacingMultiplier);
            PlayerPrefs.SetFloat(KeySpacing, spacingMultiplier);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Enables or disables reduced-motion mode (skips/shortens animations).
        /// </summary>
        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled;
            // Propagate to SWEF.UI.AccessibilityManager if present
            var uiAcc = SWEF.UI.AccessibilityManager.Instance;
            if (uiAcc != null) uiAcc.SetReducedMotion(enabled);

            PlayerPrefs.SetInt(KeyReducedMotion, enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnReducedMotionToggled?.Invoke(enabled);
        }

        /// <summary>
        /// Enables or disables simplified UI (hides non-essential elements).
        /// </summary>
        public void SetSimplifiedUI(bool enabled)
        {
            simplifiedUI = enabled;
            ApplySimplifiedUI(enabled);
            PlayerPrefs.SetInt(KeySimplified, enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnSimplifiedUIToggled?.Invoke(enabled);
        }

        /// <summary>
        /// Highlights the given UI element with a pulsing focus outline.
        /// Pass <c>null</c> to clear the highlight.
        /// </summary>
        public void SetFocus(GameObject target)
        {
            if (_focusedObject == target) return;
            _focusedObject = target;

            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }

            if (target != null && focusPulseEnabled)
                _pulseRoutine = StartCoroutine(PulseFocus(target));
        }

        // ── Internal helpers ─────────────────────────────────────────────────────
        private void RefreshScalers()
        {
            _scalers.Clear();
            _scalers.AddRange(FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None));
        }

        private void ApplyScale(float scale)
        {
            RefreshScalers();
            foreach (var scaler in _scalers)
            {
                if (scaler == null) continue;
                scaler.scaleFactor = scale;
            }
        }

        private void ApplyTextSizeLevel(int level)
        {
            float multiplier = 1f + level * 0.25f;
            foreach (var text in FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                if (text == null) continue;
                if (!_baseScales.TryGetValue(text.rectTransform, out float baseSize))
                {
                    baseSize = text.fontSize;
                    _baseScales[text.rectTransform] = baseSize;
                }
                text.fontSize = Mathf.RoundToInt(baseSize * multiplier);
            }
        }

        private void ApplySpacing(float multiplier)
        {
            foreach (var layout in FindObjectsByType<HorizontalOrVerticalLayoutGroup>(FindObjectsSortMode.None))
            {
                if (layout == null) continue;
                if (!_baseSpacings.TryGetValue(layout, out float baseSpacing))
                {
                    baseSpacing = layout.spacing;
                    _baseSpacings[layout] = baseSpacing;
                }
                layout.spacing = baseSpacing * multiplier;
            }
        }

        private void ApplySimplifiedUI(bool simplified)
        {
            foreach (var go in nonEssentialElements)
            {
                if (go != null) go.SetActive(!simplified);
            }
        }

        private IEnumerator PulseFocus(GameObject target)
        {
            var outline = target.GetComponent<Outline>();
            bool createdOutline = false;
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
                createdOutline = true;
            }
            outline.effectColor     = focusOutlineColor;
            outline.effectDistance  = new Vector2(focusOutlineWidth, focusOutlineWidth);
            outline.enabled         = true;

            float t = 0f;
            while (target != null)
            {
                t += Time.unscaledDeltaTime * focusPulseSpeed;
                float alpha = Mathf.Abs(Mathf.Sin(t * Mathf.PI));
                Color c = focusOutlineColor;
                c.a = Mathf.Lerp(0.4f, 1f, alpha);
                outline.effectColor = c;
                yield return null;
            }

            if (createdOutline && outline != null) Destroy(outline);
        }

        private void LoadPreferences()
        {
            globalScale       = PlayerPrefs.GetFloat(KeyScale, 1f);
            textSizeLevel     = PlayerPrefs.GetInt(KeyTextLevel, 0);
            reducedMotion     = PlayerPrefs.GetInt(KeyReducedMotion, 0) == 1;
            simplifiedUI      = PlayerPrefs.GetInt(KeySimplified, 0) == 1;
            spacingMultiplier = PlayerPrefs.GetFloat(KeySpacing, 1f);
        }
    }
}
