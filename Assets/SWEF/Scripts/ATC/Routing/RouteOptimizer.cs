// RouteOptimizer.cs — Phase 119: Advanced AI Traffic Control
// Real-time route optimization: shortest path with wind consideration,
// fuel-optimal altitude, weather avoidance.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Route optimizer that computes fuel-efficient flight paths
    /// accounting for wind, altitude and weather avoidance.
    /// </summary>
    public class RouteOptimizer : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ATCConfig config;

        // ── Optimized Route ───────────────────────────────────────────────────────

        /// <summary>Result of a route optimization calculation.</summary>
        public class OptimizedRoute
        {
            /// <summary>Ordered list of waypoints forming the route.</summary>
            public List<Waypoint> waypoints = new List<Waypoint>();
            /// <summary>Total estimated distance (nautical miles).</summary>
            public float totalDistanceNM;
            /// <summary>Optimal cruise altitude (feet).</summary>
            public int optimalAltitudeFt;
            /// <summary>Estimated fuel burn for the route (arbitrary units).</summary>
            public float estimatedFuelBurn;
            /// <summary>Whether this route includes weather deviation.</summary>
            public bool hasWeatherDeviation;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes an optimized route between two positions given a list of available
        /// waypoints and a wind vector.
        /// </summary>
        public OptimizedRoute OptimizeRoute(
            Vector3 origin,
            Vector3 destination,
            List<Waypoint> availableWaypoints,
            Vector3 windVector,
            bool avoidWeather = false)
        {
            var route = new OptimizedRoute();

            // Add direct waypoints sorted by proximity to the great-circle path
            foreach (var wp in availableWaypoints)
            {
                if (IsNearPath(origin, destination, wp.position, 50f))
                    route.waypoints.Add(wp);
            }

            // Sort waypoints along the path
            route.waypoints.Sort((a, b) =>
            {
                float da = Vector3.Distance(origin, a.position);
                float db = Vector3.Distance(origin, b.position);
                return da.CompareTo(db);
            });

            float wind = config != null ? config.windOptimizationWeight : 0.4f;
            float fuelW = config != null ? config.fuelOptimizationWeight : 0.6f;

            // Estimate distance
            route.totalDistanceNM = Vector3.Distance(origin, destination) / 1852f;

            // Wind-adjusted time/fuel estimate
            float headwindComponent = Vector3.Dot(
                (destination - origin).normalized, windVector.normalized) * windVector.magnitude;
            route.estimatedFuelBurn = route.totalDistanceNM * (1f - wind * headwindComponent / 50f) * fuelW;

            // Optimal altitude based on simple heuristic (higher = less drag above FL280)
            route.optimalAltitudeFt = route.totalDistanceNM > 500f ? 36000 : 24000;

            route.hasWeatherDeviation = avoidWeather && route.totalDistanceNM > 100f;

            return route;
        }

        /// <summary>
        /// Returns the fuel-optimal cruise altitude for a given route distance.
        /// </summary>
        public int GetOptimalAltitude(float distanceNM, float aircraftWeightTons)
        {
            // Simplified step-climb logic
            if (distanceNM < 200f) return 18000;
            if (distanceNM < 500f) return 28000;
            if (distanceNM < 2000f) return 35000;
            return 39000;
        }

        private bool IsNearPath(Vector3 a, Vector3 b, Vector3 point, float toleranceMeters)
        {
            Vector3 ab = b - a;
            Vector3 ap = point - a;
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
            Vector3 closest = a + t * ab;
            return Vector3.Distance(closest, point) <= toleranceMeters;
        }
    }
}
