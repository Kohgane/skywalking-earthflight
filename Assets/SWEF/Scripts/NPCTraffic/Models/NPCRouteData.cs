// NPCRouteData.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Route and waypoint data models for the NPC Traffic module.
// Namespace: SWEF.NPCTraffic

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    // ════════════════════════════════════════════════════════════════════════════
    // Route waypoint
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>A single point along an NPC flight route.</summary>
    [Serializable]
    public class NPCWaypoint
    {
        /// <summary>Identifier / fix name for this waypoint (e.g. "OKLAB", "RNWY28L").</summary>
        public string Name;

        /// <summary>World-space position of this waypoint.</summary>
        public Vector3 WorldPosition;

        /// <summary>Geographic latitude in decimal degrees.</summary>
        public double Latitude;

        /// <summary>Geographic longitude in decimal degrees.</summary>
        public double Longitude;

        /// <summary>Target altitude at this waypoint in metres MSL.</summary>
        public float AltitudeMetres;

        /// <summary>Speed constraint at this waypoint in knots (0 = no constraint).</summary>
        public float SpeedConstraintKnots;

        /// <summary>Whether the aircraft should slow to approach speed at this point.</summary>
        public bool IsApproachFix;

        /// <summary>Whether this waypoint is an airport threshold.</summary>
        public bool IsRunwayThreshold;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Full route definition
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Complete flight route used by NPC aircraft.</summary>
    [Serializable]
    public class NPCRoute
    {
        /// <summary>Unique route identifier.</summary>
        public string RouteId;

        /// <summary>Human-readable display name (e.g. "EGLL→KJFK").</summary>
        public string DisplayName;

        /// <summary>Broad type of this route.</summary>
        public NPCRouteType RouteType;

        /// <summary>ICAO code of the departure airport (if applicable).</summary>
        public string DepartureICAO;

        /// <summary>ICAO code of the destination airport (if applicable).</summary>
        public string ArrivalICAO;

        /// <summary>Ordered list of waypoints from origin to destination.</summary>
        public List<NPCWaypoint> Waypoints = new List<NPCWaypoint>();

        /// <summary>Index of the waypoint the NPC is currently heading toward.</summary>
        public int CurrentWaypointIndex;

        /// <summary>Total great-circle distance of the route in kilometres.</summary>
        public float TotalDistanceKm;

        /// <summary>Average cruise altitude for this route in metres MSL.</summary>
        public float CruiseAltitudeMetres;

        /// <summary>Aircraft categories permitted to fly this route.</summary>
        public List<NPCAircraftCategory> AllowedCategories = new List<NPCAircraftCategory>();

        /// <summary>Whether this route should loop (e.g. patrol, training circuit).</summary>
        public bool IsLooping;

        /// <summary>Maximum airspeed constraint for the entire route in knots (0 = unrestricted).</summary>
        public float RouteMaxSpeedKnots;

        /// <summary>Whether this route was procedurally generated at runtime.</summary>
        public bool IsGenerated;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Altitude profile segment
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Describes a climb, cruise, or descent segment of a flight.</summary>
    [Serializable]
    public class AltitudeProfileSegment
    {
        /// <summary>Waypoint index at which this segment begins.</summary>
        public int StartWaypointIndex;

        /// <summary>Waypoint index at which this segment ends.</summary>
        public int EndWaypointIndex;

        /// <summary>Target altitude at the end of this segment in metres MSL.</summary>
        public float TargetAltitudeMetres;

        /// <summary>Climb or descent rate for this segment in metres per second.</summary>
        public float VerticalRateMs;
    }
}
