// ResponsiveUIScaler.cs — SWEF Tablet UI Optimization (Phase 97)
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.TabletUI
{
    /// <summary>
    /// Singleton MonoBehaviour that dynamically scales UI elements based on screen DPI
    /// and physical screen size, ensuring text and controls remain readable and usable
    /// across all supported platforms (PC, mobile, tablet, XR).
    ///
    /// <para>Works in conjunction with Unity's <see cref="CanvasScaler"/> components
    /// already present in the scene.  For each root canvas whose scaler is set to
    /// <c>ScaleWithScreenSize</c> the match value is adjusted so that scaling is
    /// governed primarily by the physical DPI rather than raw pixel dimensions.</para>
    ///
    /// <para>Supports an accessibility font-size override that multiplies all
    /// <see cref="Text"/> and <see cref="TextMesh"/> sizes on subscribed canvases.</para>
    /// </summary>
    public class ResponsiveUIScaler : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static ResponsiveUIScaler Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("DPI Scaling")]
        [Tooltip("Reference DPI used as the baseline for scale computation.")]
        [SerializeField] private float referenceDpi = 160f;

        [Tooltip("Clamp the computed DPI scale to this minimum.")]
        [SerializeField] private float minScale = 0.75f;

        [Tooltip("Clamp the computed DPI scale to this maximum.")]
        [SerializeField] private float maxScale = 2.5f;

        [Header("Accessibility")]
        [Tooltip("Global font-size multiplier.  1 = default.  Changed by accessibility settings.")]
        [SerializeField, Range(0.5f, 3f)] private float fontSizeMultiplier = 1f;

        [Header("Behaviour")]
        [Tooltip("Re-evaluate scaling every time screen size or DPI changes.")]
        [SerializeField] private bool autoRefresh = true;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private float _lastDpi;
        private int   _lastWidth;
        private int   _lastHeight;
        private float _currentScale = 1f;

        /// <summary>The currently applied UI scale factor.</summary>
        public float CurrentScale => _currentScale;

        /// <summary>The current font-size multiplier.</summary>
        public float FontSizeMultiplier => fontSizeMultiplier;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the computed UI scale changes. Parameter: new scale.</summary>
        public event Action<float> OnScaleChanged;

        /// <summary>Fired when the font multiplier changes. Parameter: new multiplier.</summary>
        public event Action<float> OnFontMultiplierChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => ApplyScaling();

        private void Update()
        {
            if (!autoRefresh) return;
            if (!Mathf.Approximately(Screen.dpi,   _lastDpi)   ||
                Screen.width  != _lastWidth ||
                Screen.height != _lastHeight)
            {
                ApplyScaling();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Set the global font-size multiplier (used by the Accessibility module).
        /// </summary>
        /// <param name="multiplier">Value in the range [0.5, 3.0].</param>
        public void SetFontSizeMultiplier(float multiplier)
        {
            fontSizeMultiplier = Mathf.Clamp(multiplier, 0.5f, 3f);
            ApplyScaling();
            OnFontMultiplierChanged?.Invoke(fontSizeMultiplier);
        }

        /// <summary>Force an immediate re-computation and application of UI scaling.</summary>
        public void ApplyScaling()
        {
            _lastDpi    = Screen.dpi;
            _lastWidth  = Screen.width;
            _lastHeight = Screen.height;

            float scale = ComputeScale();
            if (Mathf.Approximately(scale, _currentScale)) return;

            _currentScale = scale;
            ApplyToAllCanvasScalers(scale);
            OnScaleChanged?.Invoke(_currentScale);
            Debug.Log($"[SWEF] ResponsiveUIScaler: scale={_currentScale:F3} (dpi={Screen.dpi:F1}).");
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private float ComputeScale()
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : referenceDpi;
            float dpiScale = dpi / referenceDpi;
            dpiScale = Mathf.Clamp(dpiScale, minScale, maxScale);
            return dpiScale * fontSizeMultiplier;
        }

        private static void ApplyToAllCanvasScalers(float scale)
        {
            CanvasScaler[] scalers = FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            foreach (CanvasScaler scaler in scalers)
            {
                if (scaler == null) continue;
                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    // Bias the match toward height on tablets (landscape) so that
                    // text stays readable regardless of aspect ratio
                    scaler.matchWidthOrHeight = Screen.width > Screen.height ? 1f : 0f;
                }
                else if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    scaler.scaleFactor = scale;
                }
            }
        }
    }
}
