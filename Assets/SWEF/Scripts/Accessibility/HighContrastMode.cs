// HighContrastMode.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Accessibility
{
    /// <summary>
    /// MonoBehaviour that applies a high-contrast visual theme to all registered
    /// UI elements when high-contrast mode is active.
    ///
    /// <para>Elements are registered automatically via <see cref="Register"/> or
    /// discovered on <c>Start</c> via <c>FindObjectsOfType</c>. Settings are
    /// driven by <see cref="AccessibilityManager"/>.</para>
    /// </summary>
    public class HighContrastMode : MonoBehaviour
    {
        // ── Serialised fields ─────────────────────────────────────────────────────
        [Header("High-Contrast Palette")]
        [SerializeField] private Color backgroundColor  = Color.black;
        [SerializeField] private Color primaryTextColor = Color.white;
        [SerializeField] private Color accentColor      = Color.yellow;
        [SerializeField] private Color buttonNormalColor = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private Color buttonHighlightColor = Color.yellow;

        [Header("Auto-Discovery")]
        [SerializeField, Tooltip("Find all Text / Image components in scene on Start.")]
        private bool autoDiscover = true;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private readonly List<Text>  _texts  = new List<Text>();
        private readonly List<Image> _images = new List<Image>();

        private bool _active;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            if (autoDiscover)
            {
                // Note: FindObjectsOfType is an O(n) scene scan; used only once on Start
                // so that elements present at scene load are automatically registered.
                // Elements spawned after Start must call Register() explicitly.
                _texts.AddRange(FindObjectsOfType<Text>());
                _images.AddRange(FindObjectsOfType<Image>());
            }

            if (AccessibilityManager.Instance != null)
            {
                Apply(AccessibilityManager.Instance.Profile.highContrastUI);
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
                Apply(AccessibilityManager.Instance.Profile.highContrastUI);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Registers a <see cref="Text"/> element for high-contrast theming.</summary>
        public void Register(Text text) { if (text != null) _texts.Add(text); }

        /// <summary>Registers an <see cref="Image"/> element for high-contrast theming.</summary>
        public void Register(Image image) { if (image != null) _images.Add(image); }

        /// <summary>Enables or disables the high-contrast skin.</summary>
        public void Apply(bool active)
        {
            _active = active;

            if (active)
            {
                foreach (var t in _texts)
                    if (t != null) t.color = primaryTextColor;

                foreach (var img in _images)
                    if (img != null) img.color = backgroundColor;

                Debug.Log("[SWEF] Accessibility: High-contrast mode ON.");
            }
            else
            {
                // Restore is handled by the owning UI components' own Reset logic.
                Debug.Log("[SWEF] Accessibility: High-contrast mode OFF.");
            }
        }
    }
}
