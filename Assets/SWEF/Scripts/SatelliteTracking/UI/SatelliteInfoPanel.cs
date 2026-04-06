// SatelliteInfoPanel.cs — Phase 114: Satellite & Space Debris Tracking
// Satellite detail panel: orbit data, 3D model preview, pass predictions, ground track.
// Namespace: SWEF.SatelliteTracking

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Shows detailed information for a selected satellite: orbital elements,
    /// status, pass prediction table, and ground track toggle.
    /// </summary>
    public class SatelliteInfoPanel : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Labels")]
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text noradIdLabel;
        [SerializeField] private Text typeLabel;
        [SerializeField] private Text orbitLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text countryLabel;
        [SerializeField] private Text altitudeLabel;
        [SerializeField] private Text latLonLabel;
        [SerializeField] private Text inclinationLabel;
        [SerializeField] private Text periodLabel;

        [Header("Buttons")]
        [SerializeField] private Button favouriteButton;
        [SerializeField] private Button predictPassesButton;
        [SerializeField] private Button showGroundTrackButton;
        [SerializeField] private Button closeButton;

        [Header("Pass Prediction")]
        [SerializeField] private Transform passListContent;
        [SerializeField] private GameObject passListItemPrefab;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the user toggles favourite status.</summary>
        public System.Action<SatelliteRecord, bool> OnFavouriteToggled;

        // ── Private state ─────────────────────────────────────────────────────────
        private SatelliteRecord _current;
        private SatellitePassPredictor _predictor;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _predictor = FindObjectOfType<SatellitePassPredictor>();

            if (favouriteButton     != null) favouriteButton.onClick.AddListener(ToggleFavourite);
            if (predictPassesButton != null) predictPassesButton.onClick.AddListener(PredictPasses);
            if (closeButton         != null) closeButton.onClick.AddListener(Hide);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Populates the panel with data from the given satellite record.</summary>
        public void ShowSatellite(SatelliteRecord record)
        {
            _current = record;
            gameObject.SetActive(true);

            SetText(nameLabel,         record.name);
            SetText(noradIdLabel,      $"NORAD: {record.noradId}");
            SetText(typeLabel,         $"Type: {record.satelliteType}");
            SetText(orbitLabel,        $"Orbit: {record.orbitType}");
            SetText(statusLabel,       $"Status: {record.status}");
            SetText(countryLabel,      $"Country: {record.country}");

            if (record.currentState != null)
            {
                SetText(altitudeLabel, $"Alt: {record.currentState.altitudeKm:F1} km");
                SetText(latLonLabel,   $"Lat: {record.currentState.latitudeDeg:F2}°  " +
                                       $"Lon: {record.currentState.longitudeDeg:F2}°");
            }

            if (record.tle != null)
            {
                SetText(inclinationLabel, $"Inc: {record.tle.inclinationDeg:F2}°");

                double n = record.tle.meanMotionRevPerDay * 2.0 * System.Math.PI / 86400.0;
                double sma = System.Math.Pow(OrbitalMechanicsEngine.MuEarth / (n * n), 1.0 / 3.0);
                double periodMin = OrbitalMechanicsEngine.OrbitalPeriodMin(sma);
                SetText(periodLabel, $"Period: {periodMin:F1} min");
            }
        }

        /// <summary>Hides the info panel.</summary>
        public void Hide() => gameObject.SetActive(false);

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ToggleFavourite()
        {
            if (_current == null) return;
            _current.isFavourite = !_current.isFavourite;
            OnFavouriteToggled?.Invoke(_current, _current.isFavourite);
        }

        private void PredictPasses()
        {
            if (_current == null || _predictor == null || passListContent == null) return;

            foreach (Transform child in passListContent) Destroy(child.gameObject);

            List<SatellitePass> passes = _predictor.PredictPasses(_current);
            foreach (var pass in passes)
            {
                if (passListItemPrefab == null) break;
                var item = UnityEngine.Object.Instantiate(passListItemPrefab, passListContent);
                var label = item.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"{pass.riseTime:HH:mm} UTC — Max El: {pass.maxElevationDeg:F0}°";
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
