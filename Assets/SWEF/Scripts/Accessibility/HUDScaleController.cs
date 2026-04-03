// HUDScaleController.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Accessibility
{
    /// <summary>
    /// MonoBehaviour that applies <see cref="AccessibilityProfile.hudScale"/> and
    /// <see cref="AccessibilityProfile.textScale"/> globally to all registered
    /// HUD canvases and text elements.
    ///
    /// <para>Attach to a persistent GameObject.  Register canvases via
    /// <see cref="RegisterCanvas"/> and text elements via <see cref="RegisterText"/>
    /// from each HUD component's <c>Awake</c>.</para>
    /// </summary>
    public class HUDScaleController : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static HUDScaleController Instance { get; private set; }

        // ── Runtime state ─────────────────────────────────────────────────────────
        private System.Collections.Generic.List<CanvasScaler> _scalers
            = new System.Collections.Generic.List<CanvasScaler>();

        private System.Collections.Generic.List<Text> _texts
            = new System.Collections.Generic.List<Text>();

        private System.Collections.Generic.Dictionary<Text, float> _baseTextSizes
            = new System.Collections.Generic.Dictionary<Text, float>();

        private float _hudScale  = 1f;
        private float _textScale = 1f;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (AccessibilityManager.Instance != null)
            {
                ApplyProfile(AccessibilityManager.Instance.Profile);
                AccessibilityManager.Instance.OnProfileChanged += OnProfileChanged;
            }
        }

        private void OnDestroy()
        {
            if (AccessibilityManager.Instance != null)
                AccessibilityManager.Instance.OnProfileChanged -= OnProfileChanged;
        }

        private void OnProfileChanged()
        {
            if (AccessibilityManager.Instance != null)
                ApplyProfile(AccessibilityManager.Instance.Profile);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Registers a <see cref="CanvasScaler"/> for HUD-scale adjustment.</summary>
        public void RegisterCanvas(CanvasScaler scaler)
        {
            if (scaler == null || _scalers.Contains(scaler)) return;
            _scalers.Add(scaler);
            ApplyHudScale(scaler, _hudScale);
        }

        /// <summary>Registers a <see cref="Text"/> for text-scale adjustment.</summary>
        public void RegisterText(Text text)
        {
            if (text == null || _texts.Contains(text)) return;
            _texts.Add(text);
            if (!_baseTextSizes.ContainsKey(text))
                _baseTextSizes[text] = text.fontSize;
            ApplyTextScale(text, _textScale);
        }

        /// <summary>Applies scale values from <paramref name="profile"/>.</summary>
        public void ApplyProfile(AccessibilityProfile profile)
        {
            _hudScale  = profile.hudScale;
            _textScale = profile.textScale;

            foreach (var s in _scalers)
                if (s != null) ApplyHudScale(s, _hudScale);

            foreach (var t in _texts)
                if (t != null) ApplyTextScale(t, _textScale);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ApplyHudScale(CanvasScaler scaler, float scale)
        {
            // For constant-pixel-size canvases, multiply scaleFactor
            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
                scaler.scaleFactor = scale;
        }

        private void ApplyTextScale(Text text, float scale)
        {
            if (_baseTextSizes.TryGetValue(text, out float baseSize))
                text.fontSize = Mathf.RoundToInt(baseSize * scale);
        }
    }
}
