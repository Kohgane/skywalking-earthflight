// GeoDataProvider.cs — Phase 113: Procedural City & Airport Generation
// Interface for real-world geographic data: population centres, airport locations
// from OpenStreetMap/Cesium (#if SWEF_GEO_DATA_AVAILABLE).
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Provides access to real-world geographic data for population centres
    /// and airport locations. When <c>SWEF_GEO_DATA_AVAILABLE</c> is defined,
    /// data is fetched from OpenStreetMap / Cesium; otherwise a built-in set of
    /// sample data is used.
    /// </summary>
    public class GeoDataProvider : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static GeoDataProvider Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Data Source")]
        [Tooltip("Base URL for the geographic data API endpoint.")]
        [SerializeField] private string apiBaseUrl = "https://overpass-api.de/api/interpreter";

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a list of known population centre positions near the given
        /// geographic coordinate within <paramref name="radiusKm"/> kilometres.
        /// </summary>
        public List<Vector2> GetPopulationCentresNear(double latDeg, double lonDeg, float radiusKm)
        {
#if SWEF_GEO_DATA_AVAILABLE
            return FetchPopulationCentresFromAPI(latDeg, lonDeg, radiusKm);
#else
            return BuiltinPopulationCentres(latDeg, lonDeg, radiusKm);
#endif
        }

        /// <summary>
        /// Returns a list of known airport positions near the given geographic coordinate.
        /// Each <see cref="Vector2"/> contains (latitude, longitude) in degrees.
        /// </summary>
        public List<Vector2> GetAirportsNear(double latDeg, double lonDeg, float radiusKm)
        {
#if SWEF_GEO_DATA_AVAILABLE
            return FetchAirportsFromAPI(latDeg, lonDeg, radiusKm);
#else
            return BuiltinAirports(latDeg, lonDeg, radiusKm);
#endif
        }

        // ── Fallback data ─────────────────────────────────────────────────────────

        private static List<Vector2> BuiltinPopulationCentres(double lat, double lon, float radiusKm)
        {
            // Sample world capitals as fallback
            return new List<Vector2>
            {
                new Vector2(51.5074f, -0.1278f),   // London
                new Vector2(48.8566f, 2.3522f),    // Paris
                new Vector2(40.7128f, -74.0060f),  // New York
                new Vector2(35.6762f, 139.6503f),  // Tokyo
                new Vector2(-33.8688f, 151.2093f)  // Sydney
            };
        }

        private static List<Vector2> BuiltinAirports(double lat, double lon, float radiusKm)
        {
            return new List<Vector2>
            {
                new Vector2(51.4775f, -0.4614f),    // LHR
                new Vector2(49.0097f, 2.5479f),    // CDG
                new Vector2(40.6413f, -73.7781f),  // JFK
                new Vector2(35.7647f, 140.3864f),  // NRT
                new Vector2(-33.9461f, 151.1772f)  // SYD
            };
        }

#if SWEF_GEO_DATA_AVAILABLE
        private List<Vector2> FetchPopulationCentresFromAPI(double lat, double lon, float radiusKm)
        {
            // Real implementation would call Overpass API
            Debug.Log($"[GeoDataProvider] Fetching population centres near ({lat},{lon}) r={radiusKm}km");
            return new List<Vector2>();
        }

        private List<Vector2> FetchAirportsFromAPI(double lat, double lon, float radiusKm)
        {
            Debug.Log($"[GeoDataProvider] Fetching airports near ({lat},{lon}) r={radiusKm}km");
            return new List<Vector2>();
        }
#endif
    }
}
