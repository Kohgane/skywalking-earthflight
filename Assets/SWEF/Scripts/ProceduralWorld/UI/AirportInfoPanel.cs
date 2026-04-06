// AirportInfoPanel.cs — Phase 113: Procedural City & Airport Generation
// Airport information: ICAO code, runways, frequencies, services, weather.
// Namespace: SWEF.ProceduralWorld

using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Displays detailed information about a procedurally generated airport:
    /// ICAO code, runway data, available services, and simulated COM frequencies.
    /// </summary>
    public class AirportInfoPanel : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text icaoText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text typeText;
        [SerializeField] private Text runwaysText;
        [SerializeField] private Text frequenciesText;
        [SerializeField] private Text servicesText;

        // ── Private state ─────────────────────────────────────────────────────────
        private AirportLayout _currentAirport;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the panel populated with <paramref name="airport"/> data.</summary>
        public void ShowAirport(AirportLayout airport)
        {
            if (airport == null) { Hide(); return; }
            _currentAirport = airport;
            Populate();
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        /// <summary>Hides the panel.</summary>
        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void Populate()
        {
            if (_currentAirport == null) return;
            if (icaoText != null) icaoText.text = _currentAirport.icaoCode;
            if (nameText != null) nameText.text = _currentAirport.airportName;
            if (typeText != null) typeText.text = _currentAirport.airportType.ToString();

            if (runwaysText != null)
            {
                var sb = new StringBuilder();
                foreach (var rwy in _currentAirport.runways)
                {
                    sb.AppendLine($"RWY {rwy.designator}  HDG {rwy.heading:F0}°  " +
                                  $"LEN {rwy.lengthMetres:F0}m  ILS:{(rwy.hasILS ? "YES" : "NO")}");
                }
                runwaysText.text = sb.ToString().TrimEnd();
            }

            if (frequenciesText != null)
            {
                // Deterministic simulated frequencies from ICAO hash
                int hash = Mathf.Abs(_currentAirport.icaoCode.GetHashCode());
                float tower = 118f + (hash % 1000) / 1000f * 18f;
                float ground = 121f + (hash / 1000 % 1000) / 1000f * 8f;
                frequenciesText.text = $"TWR {tower:F3}  GND {ground:F3}";
            }

            if (servicesText != null)
            {
                var svc = new StringBuilder();
                if (_currentAirport.hasControlTower) svc.Append("ATC  ");
                if (_currentAirport.gateCount > 0) svc.Append($"Gates:{_currentAirport.gateCount}  ");
                svc.Append("Fuel  ");
                servicesText.text = svc.ToString().TrimEnd();
            }
        }
    }
}
