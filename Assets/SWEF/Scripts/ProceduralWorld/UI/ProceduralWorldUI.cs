// ProceduralWorldUI.cs — Phase 113: Procedural City & Airport Generation
// World generation settings panel: density slider, quality presets, city style preferences.
// Namespace: SWEF.ProceduralWorld

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Settings panel for the procedural world generation system.
    /// Exposes density, quality, and city style controls to the player.
    /// </summary>
    public class ProceduralWorldUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private GameObject panelRoot;

        [Header("Controls")]
        [SerializeField] private Slider densitySlider;
        [SerializeField] private Slider qualitySlider;
        [SerializeField] private Dropdown cityStyleDropdown;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button closeButton;

        // ── Private state ─────────────────────────────────────────────────────────
        private ProceduralWorldConfig _config;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _config = ProceduralWorldManager.Instance != null
                ? ProceduralWorldManager.Instance.Config
                : null;

            if (applyButton != null) applyButton.onClick.AddListener(OnApply);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            RefreshFromConfig();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the settings panel.</summary>
        public void Show()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            RefreshFromConfig();
        }

        /// <summary>Hides the settings panel.</summary>
        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void RefreshFromConfig()
        {
            if (_config == null) return;
            if (densitySlider != null) densitySlider.value = _config.generationDensity;
            if (qualitySlider != null)
            {
                float normDist = Mathf.InverseLerp(100f, 2000f, _config.lod1Distance);
                qualitySlider.value = normDist;
            }
        }

        private void OnApply()
        {
            if (_config == null) return;
            if (densitySlider != null) _config.generationDensity = densitySlider.value;
            if (qualitySlider != null)
                _config.lod1Distance = Mathf.Lerp(100f, 2000f, qualitySlider.value);
        }
    }
}
