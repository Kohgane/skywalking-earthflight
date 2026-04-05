// NPCRouteGenerator.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Generates flight routes for NPC aircraft: airport-to-airport, patrol loops,
// training circuits, and random GA traffic.
// Namespace: SWEF.NPCTraffic

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Procedurally generates flight routes for NPC aircraft.
    /// Supports airport-to-airport routes, patrol loops, training circuits,
    /// and random GA waypoint routes.  Routes respect the aircraft category's
    /// altitude and speed profile.
    /// </summary>
    public sealed class NPCRouteGenerator : MonoBehaviour
    {
        #region Inspector

        [Header("Route Parameters")]
        [Tooltip("Minimum number of waypoints in a random GA route.")]
        [Range(2, 6)]
        [SerializeField] private int _minGAWaypoints = 3;

        [Tooltip("Maximum number of waypoints in a random GA route.")]
        [Range(4, 12)]
        [SerializeField] private int _maxGAWaypoints = 6;

        [Tooltip("Spread radius in metres for random GA waypoints.")]
        [Range(5000f, 200000f)]
        [SerializeField] private float _gaWaypointSpreadMetres = 80000f;

        [Tooltip("Patrol loop radius in metres.")]
        [Range(2000f, 50000f)]
        [SerializeField] private float _patrolRadiusMetres = 20000f;

        [Tooltip("Training circuit leg length in metres.")]
        [Range(500f, 5000f)]
        [SerializeField] private float _circuitLegMetres = 1500f;

        #endregion

        #region Public API

        /// <summary>
        /// Generates an airport-to-airport route between two ICAO codes.
        /// </summary>
        /// <param name="originICAO">Departure airport ICAO code.</param>
        /// <param name="originPos">World-space position of the departure airport.</param>
        /// <param name="destICAO">Arrival airport ICAO code.</param>
        /// <param name="destPos">World-space position of the arrival airport.</param>
        /// <param name="category">NPC aircraft category (affects altitude/speed).</param>
        /// <returns>A populated <see cref="NPCRoute"/>.</returns>
        public NPCRoute GenerateAirportToAirport(
            string originICAO, Vector3 originPos,
            string destICAO,   Vector3 destPos,
            NPCAircraftCategory category)
        {
            NPCFlightProfile profile = GetProfile(category);
            float cruiseAlt = profile.CruiseAltitudeMetres;

            var waypoints = new List<NPCWaypoint>
            {
                BuildWaypoint($"{originICAO}_DEP", originPos, 0f,        0f, isThreshold: true),
                BuildWaypoint($"{originICAO}_SID", originPos + Vector3.up * cruiseAlt * 0.3f
                              + (destPos - originPos).normalized * 5000f, cruiseAlt * 0.5f, 0f),
                BuildWaypoint("TOC", Vector3.Lerp(originPos, destPos, 0.15f) + Vector3.up * cruiseAlt, cruiseAlt, 0f),
                BuildWaypoint("CRZ_MID", Vector3.Lerp(originPos, destPos, 0.5f)  + Vector3.up * cruiseAlt, cruiseAlt, 0f),
                BuildWaypoint("TOD", Vector3.Lerp(originPos, destPos, 0.85f) + Vector3.up * cruiseAlt, cruiseAlt, 0f),
                BuildWaypoint($"{destICAO}_STAR",  destPos + (originPos - destPos).normalized * 5000f + Vector3.up * cruiseAlt * 0.3f,
                              cruiseAlt * 0.2f, profile.MinSpeedKnots * 1.3f, isApproach: true),
                BuildWaypoint($"{destICAO}_RWY",   destPos, 0f, 0f, isThreshold: true)
            };

            return new NPCRoute
            {
                RouteId              = Guid.NewGuid().ToString("N")[..8],
                DisplayName          = $"{originICAO}→{destICAO}",
                RouteType            = NPCRouteType.AirportToAirport,
                DepartureICAO        = originICAO,
                ArrivalICAO          = destICAO,
                Waypoints            = waypoints,
                CruiseAltitudeMetres = cruiseAlt,
                AllowedCategories    = new List<NPCAircraftCategory> { category },
                IsLooping            = false,
                IsGenerated          = true,
                TotalDistanceKm      = (destPos - originPos).magnitude / 1000f
            };
        }

        /// <summary>
        /// Generates a looping patrol route centred on a given position.
        /// </summary>
        /// <param name="centre">Centre of the patrol area.</param>
        /// <param name="altitudeMetres">Patrol altitude in metres MSL.</param>
        /// <param name="segments">Number of corners in the patrol polygon.</param>
        /// <returns>A looping <see cref="NPCRoute"/>.</returns>
        public NPCRoute GeneratePatrolRoute(Vector3 centre, float altitudeMetres, int segments = 4)
        {
            segments = Mathf.Clamp(segments, 3, 8);
            var waypoints = new List<NPCWaypoint>();

            for (int i = 0; i < segments; i++)
            {
                float angle = (360f / segments) * i * Mathf.Deg2Rad;
                Vector3 pos = centre + new Vector3(
                    Mathf.Sin(angle) * _patrolRadiusMetres,
                    altitudeMetres,
                    Mathf.Cos(angle) * _patrolRadiusMetres);

                waypoints.Add(BuildWaypoint($"PATROL_{i + 1}", pos, altitudeMetres, 0f));
            }

            return new NPCRoute
            {
                RouteId              = Guid.NewGuid().ToString("N")[..8],
                DisplayName          = "PATROL",
                RouteType            = NPCRouteType.PatrolLoop,
                Waypoints            = waypoints,
                CruiseAltitudeMetres = altitudeMetres,
                AllowedCategories    = new List<NPCAircraftCategory> { NPCAircraftCategory.MilitaryAircraft },
                IsLooping            = true,
                IsGenerated          = true
            };
        }

        /// <summary>
        /// Generates a standard rectangular training circuit around an airport.
        /// </summary>
        /// <param name="threshold">World-space position of the active runway threshold.</param>
        /// <param name="runwayHeadingDeg">Magnetic runway heading in degrees.</param>
        /// <returns>A looping training circuit <see cref="NPCRoute"/>.</returns>
        public NPCRoute GenerateTrainingCircuit(Vector3 threshold, float runwayHeadingDeg)
        {
            const float circuitAlt = 350f; // 1000 ft AAL in metres

            float hdgRad = runwayHeadingDeg * Mathf.Deg2Rad;
            Vector3 fwd  = new Vector3(Mathf.Sin(hdgRad), 0f, Mathf.Cos(hdgRad));
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

            Vector3 upwindEnd  = threshold + fwd  * _circuitLegMetres;
            Vector3 crosswind  = upwindEnd  + right * _circuitLegMetres;
            Vector3 downwind   = threshold  + right * _circuitLegMetres;

            var waypoints = new List<NPCWaypoint>
            {
                BuildWaypoint("TAKEOFF",   threshold                          + Vector3.up * circuitAlt, circuitAlt, 0f, isThreshold: true),
                BuildWaypoint("UPWIND",    upwindEnd  + Vector3.up * circuitAlt, circuitAlt, 0f),
                BuildWaypoint("CROSSWIND", crosswind  + Vector3.up * circuitAlt, circuitAlt, 0f),
                BuildWaypoint("DOWNWIND",  downwind   + Vector3.up * circuitAlt, circuitAlt, 0f),
                BuildWaypoint("BASE",      threshold  + right * (_circuitLegMetres * 0.5f) + Vector3.up * (circuitAlt * 0.5f), circuitAlt * 0.5f, 0f, isApproach: true),
                BuildWaypoint("FINAL",     threshold                          + Vector3.up * 30f, 30f, 0f, isThreshold: true)
            };

            return new NPCRoute
            {
                RouteId              = Guid.NewGuid().ToString("N")[..8],
                DisplayName          = "CIRCUIT",
                RouteType            = NPCRouteType.TrainingCircuit,
                Waypoints            = waypoints,
                CruiseAltitudeMetres = circuitAlt,
                AllowedCategories    = new List<NPCAircraftCategory> { NPCAircraftCategory.TrainingAircraft },
                IsLooping            = true,
                IsGenerated          = true
            };
        }

        /// <summary>
        /// Generates a semi-random general aviation route from an origin position.
        /// </summary>
        /// <param name="origin">Starting world-space position.</param>
        /// <param name="category">NPC aircraft category.</param>
        /// <returns>A generated GA <see cref="NPCRoute"/>.</returns>
        public NPCRoute GenerateRandomGARoute(Vector3 origin, NPCAircraftCategory category)
        {
            NPCFlightProfile profile   = GetProfile(category);
            int              count     = UnityEngine.Random.Range(_minGAWaypoints, _maxGAWaypoints + 1);
            var              waypoints = new List<NPCWaypoint>();

            Vector3 current = origin;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = UnityEngine.Random.insideUnitSphere * _gaWaypointSpreadMetres;
                offset.y = 0f;
                current += offset;

                waypoints.Add(BuildWaypoint(
                    $"WPT{i + 1:D2}",
                    current + Vector3.up * profile.CruiseAltitudeMetres,
                    profile.CruiseAltitudeMetres,
                    0f));
            }

            // Return to origin
            waypoints.Add(BuildWaypoint("HOME", origin + Vector3.up * 30f, 30f, 0f, isThreshold: true));

            return new NPCRoute
            {
                RouteId              = Guid.NewGuid().ToString("N")[..8],
                DisplayName          = "GA_RANDOM",
                RouteType            = NPCRouteType.RandomGA,
                Waypoints            = waypoints,
                CruiseAltitudeMetres = profile.CruiseAltitudeMetres,
                AllowedCategories    = new List<NPCAircraftCategory> { category },
                IsLooping            = false,
                IsGenerated          = true
            };
        }

        #endregion

        #region Private Helpers

        private static NPCWaypoint BuildWaypoint(
            string name, Vector3 pos, float alt, float speedConstraint,
            bool isApproach = false, bool isThreshold = false)
        {
            return new NPCWaypoint
            {
                Name                  = name,
                WorldPosition         = pos,
                AltitudeMetres        = alt,
                SpeedConstraintKnots  = speedConstraint,
                IsApproachFix         = isApproach,
                IsRunwayThreshold     = isThreshold
            };
        }

        private static NPCFlightProfile GetProfile(NPCAircraftCategory category)
        {
            if (NPCTrafficManager.Instance != null)
                return NPCTrafficManager.Instance.GetProfile(category);

            return new NPCFlightProfile
            {
                Category             = category,
                CruiseSpeedKnots     = 200f,
                MinSpeedKnots        = 80f,
                CruiseAltitudeMetres = 3000f
            };
        }

        #endregion
    }
}
