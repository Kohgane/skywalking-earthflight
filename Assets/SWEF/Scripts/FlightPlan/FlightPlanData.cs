// FlightPlanData.cs — SWEF Advanced Navigation & Flight Plan System (Phase 87)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightPlan
{
    // ─────────────────────────────────────────────────────────────────────────────
    // FlightPlanWaypoint
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single navigation fix within a <see cref="FlightPlanRoute"/>.
    /// Serialised as part of the route JSON and ScriptableObject asset.
    /// </summary>
    [Serializable]
    public class FlightPlanWaypoint
    {
        // ── Identity ──────────────────────────────────────────────────────────
        [Tooltip("ICAO/internal identifier, e.g. 'HELEN', 'RKSI', 'VOR-BKK'.")]
        public string waypointId = string.Empty;

        [Tooltip("Human-readable display name shown in HUD and UI.")]
        public string name = string.Empty;

        [Tooltip("Category of this navigation fix.")]
        public WaypointCategory category = WaypointCategory.GPS;

        // ── Position ──────────────────────────────────────────────────────────
        [Tooltip("Latitude in decimal degrees (WGS-84).")]
        public double latitude;

        [Tooltip("Longitude in decimal degrees (WGS-84).")]
        public double longitude;

        // ── Constraints ───────────────────────────────────────────────────────
        [Tooltip("Target altitude at this waypoint in feet. 0 = no constraint.")]
        public float altitude;

        [Tooltip("Target speed at this waypoint in knots (IAS). 0 = no constraint.")]
        public float speedConstraint;

        // ── Leg Geometry ──────────────────────────────────────────────────────
        [Tooltip("Geometry of the leg arriving at this waypoint.")]
        public LegType legType = LegType.DirectTo;

        [Tooltip("Inbound magnetic course to this waypoint in degrees.")]
        public float course;

        // ── Holding & Procedure ───────────────────────────────────────────────
        [Tooltip("Holding pattern leg duration in minutes. 0 = not a hold.")]
        public float holdingTime;

        [Tooltip("Name of the published procedure this waypoint belongs to, e.g. 'RNAV RWY 28L'.")]
        public string procedureName = string.Empty;

        // ── Fly-By vs Fly-Over ────────────────────────────────────────────────
        [Tooltip("True = the aircraft must fly directly over the fix; false = fly-by turn.")]
        public bool isFlyover;

        [Tooltip("Custom turn radius in nautical miles. 0 = use standard LNAV radius.")]
        public float turnRadius;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // FlightPlanRoute
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A complete flight plan: route data, aircraft performance figures, and
    /// fuel/time estimates.  Serialised for save/load and ATC filing.
    /// </summary>
    [Serializable]
    public class FlightPlanRoute
    {
        // ── Identity ──────────────────────────────────────────────────────────
        [Tooltip("Unique plan identifier (GUID).")]
        public string planId = string.Empty;

        [Tooltip("ATC call-sign (e.g. 'KAL001', 'N12345').")]
        public string callsign = string.Empty;

        // ── Classification ────────────────────────────────────────────────────
        [Tooltip("IFR, VFR, SVFR, or DVFR.")]
        public FlightRuleType flightRule = FlightRuleType.IFR;

        [Tooltip("Current lifecycle state.")]
        public FlightPlanStatus status = FlightPlanStatus.Draft;

        // ── Airports ──────────────────────────────────────────────────────────
        [Tooltip("ICAO code of departure airport (e.g. 'RKSI').")]
        public string departureAirport = string.Empty;

        [Tooltip("ICAO code of destination airport (e.g. 'RJTT').")]
        public string arrivalAirport = string.Empty;

        [Tooltip("ICAO code of alternate airport in case of diversion.")]
        public string alternateAirport = string.Empty;

        // ── Procedures ────────────────────────────────────────────────────────
        [Tooltip("Name of the departure SID (e.g. 'NIKEL2B').")]
        public string departureSID = string.Empty;

        [Tooltip("Name of the arrival STAR (e.g. 'AGRIS1A').")]
        public string arrivalSTAR = string.Empty;

        // ── Waypoints ─────────────────────────────────────────────────────────
        [Tooltip("Ordered list of waypoints from departure to destination.")]
        public List<FlightPlanWaypoint> waypoints = new List<FlightPlanWaypoint>();

        // ── Performance ───────────────────────────────────────────────────────
        [Tooltip("Planned cruise altitude in feet.")]
        public float cruiseAltitude = FlightPlanConfig.DefaultCruiseAltitudeFt;

        [Tooltip("Planned cruise speed in knots IAS.")]
        public float cruiseSpeed = FlightPlanConfig.DefaultCruiseSpeedKts;

        // ── Estimates ─────────────────────────────────────────────────────────
        [Tooltip("Total route distance in nautical miles.")]
        public float totalDistanceNm;

        [Tooltip("Estimated time en-route in minutes.")]
        public float estimatedTimeEnRoute;

        // ── Fuel ──────────────────────────────────────────────────────────────
        [Tooltip("Calculated fuel required for the route in kg (includes reserve).")]
        public float fuelRequired;

        [Tooltip("Actual fuel loaded on board in kg.")]
        public float fuelOnBoard;

        // ── Payload ───────────────────────────────────────────────────────────
        [Tooltip("Number of passengers on board.")]
        public int paxCount;

        [Tooltip("Cargo/freight weight in kg.")]
        public float cargoWeight;

        // ── Remarks ───────────────────────────────────────────────────────────
        [Tooltip("Free-text remarks field filed with ATC.")]
        public string remarks = string.Empty;

        // ── Timing ───────────────────────────────────────────────────────────
        [Tooltip("UTC time when the plan was filed.")]
        public DateTime filedTime;

        [Tooltip("UTC estimated off-block / departure time.")]
        public DateTime estimatedDeparture;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // FlightPlanData ScriptableObject
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Phase 87 — ScriptableObject flight plan template.
    ///
    /// <para>Create via <em>Assets → Create → SWEF/FlightPlan/Flight Plan Data</em>.
    /// Use as a saved template or preset that can be loaded into
    /// <see cref="FlightPlanManager"/> at runtime.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/FlightPlan/Flight Plan Data", fileName = "NewFlightPlan")]
    public class FlightPlanData : ScriptableObject
    {
        [Header("Route")]
        [Tooltip("The flight plan route data stored in this template.")]
        public FlightPlanRoute route = new FlightPlanRoute();

        [Header("Preview")]
        [Tooltip("Optional thumbnail preview of the route shown in the UI plan library.")]
        public Sprite routePreview;
    }
}
