// AirportRegistry.cs — SWEF Landing & Airport System (Phase 68)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Landing
{
    /// <summary>
    /// Phase 68 — Singleton registry of all airports in the world.
    ///
    /// <para>Airports are registered at startup (or dynamically) via
    /// <see cref="RegisterAirport"/> and can be queried by position, service,
    /// or ICAO identifier.</para>
    /// </summary>
    public class AirportRegistry : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static AirportRegistry Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Inspector

        [Header("Airport Registry")]
        [Tooltip("Pre-populated list of airports loaded at startup.")]
        [SerializeField] private List<AirportData> airports = new List<AirportData>();

        #endregion

        #region Public State

        /// <summary>Total number of registered airports.</summary>
        public int TotalAirports => airports.Count;

        #endregion

        #region Public API — Registration

        /// <summary>Registers an airport with the registry if not already present.</summary>
        /// <param name="airport">The airport to register.</param>
        public void RegisterAirport(AirportData airport)
        {
            if (airport == null || airports.Contains(airport)) return;
            airports.Add(airport);
        }

        /// <summary>Removes an airport from the registry.</summary>
        /// <param name="airport">The airport to unregister.</param>
        public void UnregisterAirport(AirportData airport)
        {
            airports.Remove(airport);
        }

        #endregion

        #region Public API — Lookup

        /// <summary>Finds the registered airport closest to <paramref name="position"/>.</summary>
        /// <param name="position">World-space query position.</param>
        /// <returns>The nearest <see cref="AirportData"/>, or <c>null</c> if none registered.</returns>
        public AirportData GetNearestAirport(Vector3 position)
        {
            AirportData nearest = null;
            float minDist = float.MaxValue;

            foreach (AirportData a in airports)
            {
                float d = DistanceToAirport(position, a);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = a;
                }
            }
            return nearest;
        }

        /// <summary>
        /// Finds the nearest airport that offers a specific service.
        /// </summary>
        /// <param name="position">World-space query position.</param>
        /// <param name="service">Service tag to match (case-insensitive), e.g. "repair".</param>
        /// <returns>The nearest qualifying <see cref="AirportData"/>, or <c>null</c>.</returns>
        public AirportData GetNearestAirportWithService(Vector3 position, string service)
        {
            AirportData nearest = null;
            float minDist = float.MaxValue;

            foreach (AirportData a in airports)
            {
                if (!HasService(a, service)) continue;
                float d = DistanceToAirport(position, a);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = a;
                }
            }
            return nearest;
        }

        /// <summary>Returns all airports within <paramref name="range"/> meters of <paramref name="position"/>.</summary>
        /// <param name="position">World-space query position.</param>
        /// <param name="range">Search radius in meters.</param>
        /// <returns>List of matching airports (may be empty).</returns>
        public List<AirportData> GetAirportsInRange(Vector3 position, float range)
        {
            var result = new List<AirportData>();
            foreach (AirportData a in airports)
            {
                if (DistanceToAirport(position, a) <= range)
                    result.Add(a);
            }
            return result;
        }

        /// <summary>Looks up an airport by its ICAO-style identifier.</summary>
        /// <param name="airportId">The airport ID to search for.</param>
        /// <returns>The matching <see cref="AirportData"/>, or <c>null</c> if not found.</returns>
        public AirportData GetAirportById(string airportId)
        {
            foreach (AirportData a in airports)
            {
                if (string.Equals(a.airportId, airportId, System.StringComparison.OrdinalIgnoreCase))
                    return a;
            }
            return null;
        }

        /// <summary>
        /// Returns the runway at <paramref name="airport"/> most aligned with
        /// the given <paramref name="windDirection"/> (headwind preferred).
        /// </summary>
        /// <param name="airport">The airport to query.</param>
        /// <param name="windDirection">Wind direction in degrees (meteorological, 0 = from north).</param>
        /// <returns>The best <see cref="RunwayData"/>, or <c>null</c> if no runways exist.</returns>
        public RunwayData GetBestRunway(AirportData airport, float windDirection)
        {
            if (airport == null || airport.runways == null || airport.runways.Count == 0)
                return null;

            RunwayData best       = null;
            float      bestScore  = float.MinValue;

            // Wind comes FROM windDirection; runway heading is the direction you land INTO.
            // Best runway has heading closest to windDirection (headwind landing).
            float windFrom = windDirection;

            foreach (RunwayData rwy in airport.runways)
            {
                float diff  = Mathf.DeltaAngle(rwy.heading, windFrom);
                float score = -Mathf.Abs(diff); // closer to 0 delta = better headwind alignment
                if (score > bestScore)
                {
                    bestScore = score;
                    best      = rwy;
                }
            }
            return best;
        }

        #endregion

        #region Helpers

        private static float DistanceToAirport(Vector3 position, AirportData airport)
        {
            // Use the first runway threshold for distance if available,
            // otherwise fall back to a flat-plane estimate from lat/lon.
            if (airport.runways != null && airport.runways.Count > 0)
                return Vector3.Distance(position, airport.runways[0].thresholdPosition);

            // Approximate world position from geographic coordinates (flat-earth estimate).
            // Longitude scaling is corrected for latitude to improve accuracy at higher latitudes.
            const float MetersPerDegreeLat = 111320f;
            float latRad  = (float)(airport.latitude * System.Math.PI / 180.0);
            float latDiff = (float)(airport.latitude)  * MetersPerDegreeLat;
            float lonDiff = (float)(airport.longitude) * MetersPerDegreeLat * Mathf.Cos(latRad);
            return Vector3.Distance(position, new Vector3(lonDiff, airport.elevation, latDiff));
        }

        private static bool HasService(AirportData airport, string service)
        {
            if (airport.services == null) return false;
            foreach (string s in airport.services)
            {
                if (string.Equals(s, service, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            // Also check built-in capability flags
            if (string.Equals(service, "repair", System.StringComparison.OrdinalIgnoreCase) && airport.hasRepairFacility)
                return true;
            if (string.Equals(service, "fuel",   System.StringComparison.OrdinalIgnoreCase) && airport.hasFuelStation)
                return true;
            return false;
        }

        #endregion
    }
}
