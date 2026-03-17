using UnityEngine;
using UnityEngine.UI;
using SWEF.Flight;

namespace SWEF.UI
{
    /// <summary>
    /// Minimap overlay showing current latitude, longitude, altitude, and a simple coordinate display.
    /// Adapts display based on altitude range.
    /// </summary>
    public class MiniMap : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text coordText;
        [SerializeField] private Text altRangeText;
        [SerializeField] private GameObject miniMapPanel;

        [Header("References")]
        [SerializeField] private AltitudeController altitudeSource;

        [Header("Toggle")]
        [SerializeField] private Button toggleButton;

        private bool _visible = true;

        private void Start()
        {
            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();

            if (altitudeSource == null)
                Debug.LogWarning("[SWEF] MiniMap — AltitudeController not found.");

            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleVisibility);

            SetPanelVisible(_visible);
        }

        private void Update()
        {
            if (!_visible) return;

            double lat = SWEFSession.Lat;
            double lon = SWEFSession.Lon;

            float alt = altitudeSource != null
                ? altitudeSource.CurrentAltitudeMeters
                : (float)SWEFSession.Alt;

            if (coordText != null)
                coordText.text = $"{lat:F4}°, {lon:F4}°";

            UpdateAltitudeDisplay(alt);
        }

        private void UpdateAltitudeDisplay(float alt)
        {
            if (altRangeText == null && coordText == null) return;

            string altStr;
            if (alt < 1000f)
                altStr = $"ALT {alt:0} m";
            else if (alt < 100_000f)
                altStr = $"ALT {alt / 1000f:0.0} km";
            else
                altStr = $"ALT {alt / 1000f:0} km";

            string rangeLabel = GetAltitudeRangeLabel(alt);

            // Append altitude string to coord text if there is no separate altRangeText
            if (altRangeText != null)
            {
                altRangeText.text = $"{altStr}  {rangeLabel}";
            }
            else if (coordText != null)
            {
                double lat = SWEFSession.Lat;
                double lon = SWEFSession.Lon;
                coordText.text = $"{lat:F4}°, {lon:F4}°\n{altStr}  {rangeLabel}";
            }
        }

        private static string GetAltitudeRangeLabel(float alt)
        {
            if (alt < 2_000f)   return "🏙️ City Level";
            if (alt < 20_000f)  return "✈️ Cruising Altitude";
            if (alt < 80_000f)  return "🌍 Stratosphere";
            if (alt < 120_000f) return "🔥 Kármán Line";
            return "🚀 Space";
        }

        /// <summary>Toggle the minimap panel visibility.</summary>
        public void ToggleVisibility()
        {
            _visible = !_visible;
            SetPanelVisible(_visible);
        }

        private void SetPanelVisible(bool visible)
        {
            if (miniMapPanel != null)
                miniMapPanel.SetActive(visible);
        }
    }
}
