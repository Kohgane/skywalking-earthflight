// MaritimeRadarHUD.cs — Phase 117: Advanced Ocean & Maritime System
// Maritime radar overlay: vessel positions, sea state indicator, wind/wave.
// Namespace: SWEF.OceanSystem

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Maritime radar HUD overlay.
    /// Displays vessel blips, sea state indicator, wind direction rose,
    /// and wave height readout.
    /// </summary>
    public class MaritimeRadarHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("HUD Root")]
        [SerializeField] private GameObject hudRoot;

        [Header("Radar")]
        [SerializeField] private RectTransform radarPanel;
        [SerializeField] private GameObject    vesselBlipPrefab;
        [SerializeField] private float         radarRangeMetres = 30000f;
        [SerializeField] private float         radarPanelRadius = 100f;

        [Header("Readouts")]
        [SerializeField] private Text seaStateText;
        [SerializeField] private Text waveHeightText;
        [SerializeField] private Text windText;

        [Header("Wind Rose")]
        [SerializeField] private RectTransform windDirectionArrow;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (hudRoot == null || !hudRoot.activeSelf) return;
            UpdateReadouts();
        }

        // ── Update ────────────────────────────────────────────────────────────────

        private void UpdateReadouts()
        {
            var mgr = OceanSystemManager.Instance;
            if (mgr == null) return;

            var conditions = mgr.CurrentConditions;

            if (seaStateText   != null) seaStateText.text   = "Sea: " + mgr.CurrentSeaState;
            if (waveHeightText != null) waveHeightText.text = $"Wave: {conditions.significantWaveHeight:F1}m";
            if (windText       != null) windText.text       = $"Wind: {conditions.windSpeed:F0}m/s {conditions.windDirection:F0}°";

            // Rotate wind rose arrow
            if (windDirectionArrow != null)
                windDirectionArrow.localEulerAngles = new Vector3(0f, 0f, -conditions.windDirection);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows or hides the maritime radar HUD.</summary>
        public void SetVisible(bool visible)
        {
            if (hudRoot != null) hudRoot.SetActive(visible);
        }
    }
}
