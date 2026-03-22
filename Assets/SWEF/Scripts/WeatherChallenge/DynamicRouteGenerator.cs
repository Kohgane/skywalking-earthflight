using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.WeatherChallenge
{
    /// <summary>
    /// Phase 53 — Procedurally generates geo-referenced waypoint routes for weather challenges.
    /// All coordinate math uses haversine / inverse-haversine formulae so that routes are
    /// accurate at any location on the globe.
    /// </summary>
    public class DynamicRouteGenerator : MonoBehaviour
    {
        #region Serialized Fields

        /// <summary>Base radius (metres) within which waypoints are scattered around the origin.</summary>
        [SerializeField] private float baseRouteRadius = 5000f;

        /// <summary>Minimum number of waypoints generated for any route.</summary>
        [SerializeField] private int minWaypoints = 3;

        /// <summary>Maximum number of waypoints that can be generated for a route.</summary>
        [SerializeField] private int maxWaypoints = 12;

        #endregion

        #region Public API

        /// <summary>
        /// Generates a list of <see cref="RouteWaypoint"/> objects arranged around a geographic
        /// origin, then adjusts them for the specified weather and difficulty parameters.
        /// </summary>
        /// <param name="originLat">Origin latitude in decimal degrees.</param>
        /// <param name="originLon">Origin longitude in decimal degrees.</param>
        /// <param name="originAlt">Origin altitude in metres ASL.</param>
        /// <param name="difficulty">Difficulty tier controlling waypoint count and spacing.</param>
        /// <param name="weather">Weather scenario influencing altitude and action assignments.</param>
        /// <returns>Validated list of waypoints, or an empty list if generation fails.</returns>
        public List<RouteWaypoint> GenerateRoute(
            double originLat,
            double originLon,
            float  originAlt,
            ChallengeDifficulty   difficulty,
            ChallengeWeatherType  weather)
        {
            int count = DetermineWaypointCount(difficulty);
            float radius = DetermineRadius(difficulty);

            var route = new List<RouteWaypoint>(count);
            for (int i = 0; i < count; i++)
            {
                float angle    = (360f / count) * i + UnityEngine.Random.Range(-20f, 20f);
                float distance = radius * UnityEngine.Random.Range(0.5f, 1.0f);

                var wp = new RouteWaypoint
                {
                    waypointId   = Guid.NewGuid().ToString(),
                    waypointName = $"WP-{i + 1:D2}",
                    requiredAction = ChooseAction(weather, i),
                    radiusMeters = GetWaypointRadius(difficulty),
                    isOptional   = (i == count - 2) && count > 4 // second-to-last optional = bonus
                };

                CalculateWaypointPosition(originLat, originLon, angle, distance,
                    out wp.latitude, out wp.longitude);
                wp.altitude = originAlt + UnityEngine.Random.Range(-200f, 400f);

                route.Add(wp);
            }

            AdjustForWeather(route, weather);
            AdjustForDifficulty(route, difficulty);
            ValidateRoute(route);

            return route;
        }

        /// <summary>
        /// Computes the destination latitude and longitude after travelling <paramref name="distanceMeters"/>
        /// metres from <paramref name="centerLat"/>, <paramref name="centerLon"/> on bearing
        /// <paramref name="angleDeg"/> (clockwise from North).
        /// </summary>
        /// <param name="centerLat">Origin latitude in decimal degrees.</param>
        /// <param name="centerLon">Origin longitude in decimal degrees.</param>
        /// <param name="angleDeg">Bearing in degrees (0 = North, 90 = East).</param>
        /// <param name="distanceMeters">Distance to travel in metres.</param>
        /// <param name="destLat">Output: computed destination latitude.</param>
        /// <param name="destLon">Output: computed destination longitude.</param>
        public void CalculateWaypointPosition(
            double centerLat,
            double centerLon,
            float  angleDeg,
            float  distanceMeters,
            out double destLat,
            out double destLon)
        {
            DestinationPoint(centerLat, centerLon, angleDeg, distanceMeters,
                out destLat, out destLon);
        }

        /// <summary>
        /// Modifies waypoint altitudes and action types to suit the given weather scenario.
        /// </summary>
        /// <param name="route">The route to adjust in-place.</param>
        /// <param name="weather">Target weather type.</param>
        public void AdjustForWeather(List<RouteWaypoint> route, ChallengeWeatherType weather)
        {
            if (route == null) return;

            foreach (RouteWaypoint wp in route)
            {
                switch (weather)
                {
                    case ChallengeWeatherType.Thermal:
                        // Thermals are exploited by gaining altitude in a spiral
                        wp.altitude    += UnityEngine.Random.Range(200f, 800f);
                        wp.requiredAction = "hold_altitude";
                        break;

                    case ChallengeWeatherType.Fog:
                        // Fog — keep low altitude, tight proximity
                        wp.altitude     = Math.Max(100.0, wp.altitude - 300.0);
                        wp.radiusMeters = Mathf.Max(100f, wp.radiusMeters * 0.8f);
                        break;

                    case ChallengeWeatherType.Thunderstorm:
                        // Avoid the worst cells — route snakes around them
                        wp.requiredAction = "avoid_zone";
                        wp.radiusMeters  *= 1.2f;
                        break;

                    case ChallengeWeatherType.Icing:
                        // Stay below the freezing level
                        wp.altitude       = Math.Min(wp.altitude, 3000.0);
                        wp.requiredAction = "hold_altitude";
                        break;

                    case ChallengeWeatherType.Snow:
                        wp.altitude = Math.Max(200.0, wp.altitude - 150.0);
                        break;

                    case ChallengeWeatherType.Crosswind:
                        // Wider spacing to give room for drift correction
                        wp.radiusMeters *= 1.3f;
                        break;
                }
            }
        }

        /// <summary>
        /// Adjusts waypoint radii, spacing, and count characteristics to match the given difficulty.
        /// Called after <see cref="AdjustForWeather"/>.
        /// </summary>
        /// <param name="route">The route to adjust in-place.</param>
        /// <param name="difficulty">Target difficulty tier.</param>
        public void AdjustForDifficulty(List<RouteWaypoint> route, ChallengeDifficulty difficulty)
        {
            if (route == null) return;

            float radiusMul = 1f;
            switch (difficulty)
            {
                case ChallengeDifficulty.Easy:    radiusMul = 1.5f;  break;
                case ChallengeDifficulty.Hard:    radiusMul = 0.65f; break;
                case ChallengeDifficulty.Extreme: radiusMul = 0.4f;  break;
            }

            foreach (RouteWaypoint wp in route)
                wp.radiusMeters = Mathf.Max(50f, wp.radiusMeters * radiusMul);
        }

        /// <summary>
        /// Validates the route to ensure no two waypoints overlap and that consecutive
        /// waypoints are at least 200 metres apart.  Overlapping waypoints are jittered.
        /// </summary>
        /// <param name="route">The route to validate in-place.</param>
        public void ValidateRoute(List<RouteWaypoint> route)
        {
            if (route == null || route.Count < 2) return;

            const double minSeparationMeters = 200.0;

            for (int i = 0; i < route.Count; i++)
            {
                for (int j = i + 1; j < route.Count; j++)
                {
                    double dist = HaversineMeters(
                        route[i].latitude, route[i].longitude,
                        route[j].latitude, route[j].longitude);

                    if (dist < minSeparationMeters)
                    {
                        // Jitter the later waypoint slightly
                        float jitterAngle = UnityEngine.Random.Range(0f, 360f);
                        DestinationPoint(route[j].latitude, route[j].longitude,
                            jitterAngle, (float)(minSeparationMeters * 1.5),
                            out route[j].latitude, out route[j].longitude);
                    }
                }
            }
        }

        #endregion

        #region Static Math Utilities

        /// <summary>
        /// Computes the haversine great-circle distance in metres between two WGS-84 coordinates.
        /// </summary>
        public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6_371_000.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0)
                     + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                     * Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);
            return R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        }

        /// <summary>
        /// Returns the initial bearing (degrees, clockwise from North) from point 1 to point 2.
        /// </summary>
        public static double Bearing(double lat1, double lon1, double lat2, double lon2)
        {
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double rlat1 = lat1 * Math.PI / 180.0;
            double rlat2 = lat2 * Math.PI / 180.0;
            double y = Math.Sin(dLon) * Math.Cos(rlat2);
            double x = Math.Cos(rlat1) * Math.Sin(rlat2)
                     - Math.Sin(rlat1) * Math.Cos(rlat2) * Math.Cos(dLon);
            return (Math.Atan2(y, x) * 180.0 / Math.PI + 360.0) % 360.0;
        }

        /// <summary>
        /// Computes the destination point given a start coordinate, bearing, and distance.
        /// Uses the spherical-Earth inverse-haversine formula.
        /// </summary>
        /// <param name="lat">Start latitude in decimal degrees.</param>
        /// <param name="lon">Start longitude in decimal degrees.</param>
        /// <param name="bearingDeg">Bearing in degrees (clockwise from North).</param>
        /// <param name="distanceMeters">Distance in metres.</param>
        /// <param name="destLat">Output: destination latitude in decimal degrees.</param>
        /// <param name="destLon">Output: destination longitude in decimal degrees.</param>
        public static void DestinationPoint(
            double lat, double lon,
            double bearingDeg, double distanceMeters,
            out double destLat, out double destLon)
        {
            const double R = 6_371_000.0;
            double delta = distanceMeters / R;
            double theta = bearingDeg * Math.PI / 180.0;
            double rlat  = lat * Math.PI / 180.0;
            double rlon  = lon * Math.PI / 180.0;

            double sinLat2 = Math.Sin(rlat) * Math.Cos(delta)
                           + Math.Cos(rlat) * Math.Sin(delta) * Math.Cos(theta);
            double lat2 = Math.Asin(sinLat2);

            double y   = Math.Sin(theta) * Math.Sin(delta) * Math.Cos(rlat);
            double x   = Math.Cos(delta) - Math.Sin(rlat) * sinLat2;
            double lon2 = rlon + Math.Atan2(y, x);

            destLat = lat2 * 180.0 / Math.PI;
            destLon = ((lon2 * 180.0 / Math.PI) + 540.0) % 360.0 - 180.0;
        }

        #endregion

        #region Private Helpers

        private int DetermineWaypointCount(ChallengeDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ChallengeDifficulty.Easy:    return UnityEngine.Random.Range(minWaypoints, minWaypoints + 2);
                case ChallengeDifficulty.Hard:    return UnityEngine.Random.Range(6, maxWaypoints - 2);
                case ChallengeDifficulty.Extreme: return UnityEngine.Random.Range(9, maxWaypoints);
                default:                          return UnityEngine.Random.Range(4, 8);
            }
        }

        private float DetermineRadius(ChallengeDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ChallengeDifficulty.Easy:    return baseRouteRadius * 0.6f;
                case ChallengeDifficulty.Hard:    return baseRouteRadius * 1.4f;
                case ChallengeDifficulty.Extreme: return baseRouteRadius * 2f;
                default:                          return baseRouteRadius;
            }
        }

        private static float GetWaypointRadius(ChallengeDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ChallengeDifficulty.Easy:    return 400f;
                case ChallengeDifficulty.Hard:    return 150f;
                case ChallengeDifficulty.Extreme: return 80f;
                default:                          return 250f;
            }
        }

        private static string ChooseAction(ChallengeWeatherType weather, int index)
        {
            if (index == 0) return "fly_through";
            switch (weather)
            {
                case ChallengeWeatherType.Thermal:    return index % 2 == 0 ? "hold_altitude" : "fly_through";
                case ChallengeWeatherType.Thunderstorm: return index % 3 == 0 ? "avoid_zone" : "fly_through";
                case ChallengeWeatherType.Icing:      return "hold_altitude";
                default:                              return "fly_through";
            }
        }

        #endregion
    }
}
