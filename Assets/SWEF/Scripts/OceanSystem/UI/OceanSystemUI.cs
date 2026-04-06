// OceanSystemUI.cs — Phase 117: Advanced Ocean & Maritime System
// Ocean settings panel: wave quality, reflection quality, water landing assist.
// Namespace: SWEF.OceanSystem

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Settings panel for the Ocean &amp; Maritime System.
    /// Exposes wave quality, reflection quality, caustics toggle, and
    /// water landing assist toggle.
    /// </summary>
    public class OceanSystemUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private OceanSystemConfig config;
        [SerializeField] private GameObject panelRoot;

        [Header("Controls")]
        [SerializeField] private Slider waveQualitySlider;
        [SerializeField] private Slider reflectionQualitySlider;
        [SerializeField] private Toggle causticsToggle;
        [SerializeField] private Toggle waterLandingAssistToggle;
        [SerializeField] private Slider foamDensitySlider;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            PopulateFromConfig();
            RegisterCallbacks();
        }

        // ── Initialisation ────────────────────────────────────────────────────────

        private void PopulateFromConfig()
        {
            if (config == null) return;
            if (waveQualitySlider         != null) waveQualitySlider.value         = config.waveOctaves;
            if (reflectionQualitySlider   != null) reflectionQualitySlider.value   = config.reflectionQuality;
            if (causticsToggle            != null) causticsToggle.isOn             = config.enableCaustics;
            if (waterLandingAssistToggle  != null) waterLandingAssistToggle.isOn   = config.waterLandingAssist;
            if (foamDensitySlider         != null) foamDensitySlider.value         = config.foamDensity;
        }

        private void RegisterCallbacks()
        {
            if (config == null) return;

            waveQualitySlider?.onValueChanged.AddListener(v =>
            {
                config.waveOctaves = Mathf.RoundToInt(v);
            });

            reflectionQualitySlider?.onValueChanged.AddListener(v =>
            {
                config.reflectionQuality = Mathf.RoundToInt(v);
            });

            causticsToggle?.onValueChanged.AddListener(v =>
            {
                config.enableCaustics = v;
                FindFirstObjectByType<OceanSurfaceRenderer>()?.SetCausticsEnabled(v);
            });

            waterLandingAssistToggle?.onValueChanged.AddListener(v =>
            {
                config.waterLandingAssist = v;
            });

            foamDensitySlider?.onValueChanged.AddListener(v =>
            {
                config.foamDensity = v;
            });
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Toggles the visibility of the settings panel.</summary>
        public void TogglePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(!panelRoot.activeSelf);
        }

        /// <summary>Opens the settings panel.</summary>
        public void Show() { if (panelRoot != null) panelRoot.SetActive(true); }

        /// <summary>Closes the settings panel.</summary>
        public void Hide() { if (panelRoot != null) panelRoot.SetActive(false); }
    }
}
