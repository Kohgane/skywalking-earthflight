// CityInfoHUD.cs — Phase 113: Procedural City & Airport Generation
// HUD overlay showing current city name, population, nearby airports, POIs.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Heads-up display overlay that shows the name, population, and nearby
    /// airports of the procedural city closest to the player.
    /// </summary>
    public class CityInfoHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("UI References")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private Text cityNameText;
        [SerializeField] private Text populationText;
        [SerializeField] private Text nearbyAirportsText;
        [SerializeField] private Text poiText;

        [Header("Settings")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float updateInterval = 3f;
        [SerializeField] private float maxCityRange = 5000f;

        // ── Private state ─────────────────────────────────────────────────────────
        private float _nextUpdate;
        private CityDescription _currentCity;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            if (Time.time < _nextUpdate) return;
            _nextUpdate = Time.time + updateInterval;
            RefreshHUD();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the HUD overlay.</summary>
        public void Show() { if (hudRoot != null) hudRoot.SetActive(true); }

        /// <summary>Hides the HUD overlay.</summary>
        public void Hide() { if (hudRoot != null) hudRoot.SetActive(false); }

        /// <summary>Forces an immediate HUD refresh.</summary>
        public void ForceRefresh() => RefreshHUD();

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void RefreshHUD()
        {
            var mgr = ProceduralWorldManager.Instance;
            if (mgr == null || playerTransform == null) return;

            _currentCity = FindNearestCity(mgr.ActiveCities, playerTransform.position);
            if (_currentCity == null)
            {
                if (hudRoot != null) hudRoot.SetActive(false);
                return;
            }

            if (hudRoot != null) hudRoot.SetActive(true);
            if (cityNameText != null) cityNameText.text = _currentCity.cityName;
            if (populationText != null) populationText.text = $"Pop: {_currentCity.population:N0}";

            // Nearby airports
            var airports = mgr.ActiveAirports;
            var nearbyICAOs = new List<string>();
            foreach (var ap in airports)
            {
                if (Vector3.Distance(ap.referencePoint, playerTransform.position) < maxCityRange)
                    nearbyICAOs.Add(ap.icaoCode);
            }
            if (nearbyAirportsText != null)
                nearbyAirportsText.text = nearbyICAOs.Count > 0
                    ? "Airports: " + string.Join(", ", nearbyICAOs)
                    : "No airports nearby";

            if (poiText != null)
                poiText.text = $"Type: {_currentCity.cityType}";
        }

        private static CityDescription FindNearestCity(
            IReadOnlyList<CityDescription> cities, Vector3 pos)
        {
            CityDescription nearest = null;
            float minDist = float.MaxValue;
            foreach (var city in cities)
            {
                float d = Vector3.Distance(city.centre, pos);
                if (d < minDist && d <= city.radiusMetres * 2f)
                {
                    minDist = d;
                    nearest = city;
                }
            }
            return nearest;
        }
    }
}
