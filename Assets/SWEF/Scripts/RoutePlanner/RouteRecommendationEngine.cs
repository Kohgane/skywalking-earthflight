using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Intelligent route suggestion engine.  Evaluates the player's current
    /// location, time of day, weather, discovered landmarks, and flight history to
    /// surface relevant route recommendations.
    /// </summary>
    public class RouteRecommendationEngine : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static RouteRecommendationEngine Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Nearby Routes")]
        [Tooltip("Radius in km within which a route's start point is considered 'nearby'.")]
        [SerializeField] private float _nearbyRadiusKm = 50f;

        [Header("Route of the Day")]
        [Tooltip("Hour of the day (UTC) when the featured route rotates.")]
        [SerializeField] private int _rotationHourUtc = 0;

        #endregion

        #region Private State

        // Cached date of the last "route of the day" calculation
        private DateTime _lastRotationDate = DateTime.MinValue;
        private FlightRoute _cachedRouteOfTheDay;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns up to <paramref name="count"/> route recommendations ordered by relevance.
        /// </summary>
        /// <param name="count">Maximum number of routes to return.</param>
        public List<FlightRoute> GetRecommendations(int count)
        {
            var all     = GetAllRoutesFromStorage();
            var scored  = new List<(FlightRoute route, float score)>();

            foreach (var r in all)
                scored.Add((r, ScoreRoute(r)));

            scored.Sort((a, b) => b.score.CompareTo(a.score));

            var result = new List<FlightRoute>(count);
            for (int i = 0; i < Mathf.Min(count, scored.Count); i++)
                result.Add(scored[i].route);

            return result;
        }

        /// <summary>
        /// Returns the curated "Route of the Day". The selection rotates daily and is
        /// deterministically derived from the current UTC date.
        /// </summary>
        public FlightRoute GetRouteOfTheDay()
        {
            DateTime today = DateTime.UtcNow.Date;

            if (_cachedRouteOfTheDay != null && _lastRotationDate == today)
                return _cachedRouteOfTheDay;

            var all = GetAllRoutesFromStorage();
            if (all.Count == 0) return null;

            // Deterministic index based on day-of-year
            int idx = today.DayOfYear % all.Count;
            _cachedRouteOfTheDay = all[idx];
            _lastRotationDate    = today;

            return _cachedRouteOfTheDay;
        }

        /// <summary>
        /// Returns routes whose start point is within <paramref name="radiusKm"/> km of
        /// the supplied coordinates.
        /// </summary>
        /// <param name="lat">Observer latitude.</param>
        /// <param name="lon">Observer longitude.</param>
        /// <param name="radiusKm">Search radius in kilometres.</param>
        public List<FlightRoute> GetNearbyRoutes(double lat, double lon, float radiusKm)
        {
            float radius = radiusKm > 0f ? radiusKm : _nearbyRadiusKm;
            var result   = new List<FlightRoute>();

            foreach (var r in GetAllRoutesFromStorage())
            {
                double dist = HaversineKm(lat, lon, r.startLatitude, r.startLongitude);
                if (dist <= radius) result.Add(r);
            }

            result.Sort((a, b) =>
            {
                double da = HaversineKm(lat, lon, a.startLatitude, a.startLongitude);
                double db = HaversineKm(lat, lon, b.startLatitude, b.startLongitude);
                return da.CompareTo(db);
            });

            return result;
        }

        /// <summary>
        /// Returns the most-downloaded routes across the library, sorted descending.
        /// </summary>
        /// <param name="count">Maximum number of trending routes to return.</param>
        public List<FlightRoute> GetTrendingRoutes(int count)
        {
            var all = GetAllRoutesFromStorage();
            all.Sort((a, b) => b.downloadCount.CompareTo(a.downloadCount));

            if (all.Count > count) all.RemoveRange(count, all.Count - count);
            return all;
        }

        /// <summary>
        /// Returns routes recommended for the current time of day and weather.
        /// </summary>
        public List<FlightRoute> GetContextualRecommendations(int count)
        {
            int hour = DateTime.Now.Hour;
            string timeContext = hour >= 5 && hour < 10   ? "sunrise"
                               : hour >= 17 && hour < 21  ? "sunset"
                               : hour >= 21 || hour < 5   ? "night"
                               : "day";

            var all    = GetAllRoutesFromStorage();
            var result = new List<FlightRoute>();

            foreach (var r in all)
            {
                string rec = r.timeOfDayRecommendation?.ToLower() ?? string.Empty;
                if (rec.Contains(timeContext))
                    result.Add(r);
            }

            // Fill remaining slots with scored recommendations
            if (result.Count < count)
            {
                var extras = GetRecommendations(count);
                foreach (var r in extras)
                    if (!result.Contains(r) && result.Count < count)
                        result.Add(r);
            }

            return result;
        }

        #endregion

        #region Private — Scoring

        /// <summary>
        /// Computes a relevance score for a route based on rating, completion count,
        /// and personal factors.
        /// </summary>
        private float ScoreRoute(FlightRoute route)
        {
            float score = 0f;

            // Community quality
            score += route.rating * 10f;
            score += Mathf.Log10(Mathf.Max(1, route.completionCount)) * 5f;
            score += Mathf.Log10(Mathf.Max(1, route.downloadCount))   * 2f;

            // Difficulty preference (assume beginner-friendly is more accessible)
            float diffPenalty = Mathf.Abs(route.difficulty - 2);
            score -= diffPenalty;

            // Freshness: newer routes get a small boost
            if (DateTime.TryParse(route.updatedAt, out DateTime updated))
            {
                double daysSinceUpdate = (DateTime.UtcNow - updated).TotalDays;
                score += Mathf.Max(0f, 10f - (float)daysSinceUpdate * 0.1f);
            }

            return score;
        }

        #endregion

        #region Private — Helpers

        private static List<FlightRoute> GetAllRoutesFromStorage()
        {
            if (RouteStorageManager.Instance != null)
                return RouteStorageManager.Instance.GetAllRoutes();
            if (RoutePlannerManager.Instance != null)
                return RoutePlannerManager.Instance.GetAllRoutes();
            return new List<FlightRoute>();
        }

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a    = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                        + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                        * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        #endregion
    }
}
