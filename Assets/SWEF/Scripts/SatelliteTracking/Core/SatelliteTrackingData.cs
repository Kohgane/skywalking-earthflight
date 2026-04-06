// SatelliteTrackingData.cs — Phase 114: Satellite & Space Debris Tracking
// Enums and data models for the satellite tracking system.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    // ── Satellite Type ────────────────────────────────────────────────────────────

    /// <summary>Functional classification of an orbital object.</summary>
    public enum SatelliteType
    {
        /// <summary>Commercial or government communication relay satellite.</summary>
        Communication,
        /// <summary>Global navigation system satellite (GPS, GLONASS, Galileo, BeiDou).</summary>
        Navigation,
        /// <summary>Earth observation and meteorological satellite.</summary>
        Weather,
        /// <summary>Research and scientific payload satellite.</summary>
        Science,
        /// <summary>Military or classified reconnaissance satellite.</summary>
        Military,
        /// <summary>Crewed or uncrewed space station.</summary>
        SpaceStation,
        /// <summary>Non-functional orbital debris object.</summary>
        Debris
    }

    // ── Orbit Type ────────────────────────────────────────────────────────────────

    /// <summary>Orbital regime classification.</summary>
    public enum OrbitType
    {
        /// <summary>Low Earth Orbit — 160–2 000 km altitude.</summary>
        LEO,
        /// <summary>Medium Earth Orbit — 2 000–35 786 km altitude.</summary>
        MEO,
        /// <summary>Geostationary Orbit — ~35 786 km altitude.</summary>
        GEO,
        /// <summary>Highly Elliptical Orbit (e.g. Molniya).</summary>
        HEO,
        /// <summary>Sun-Synchronous Orbit — near-polar retrograde LEO.</summary>
        SSO,
        /// <summary>Polar orbit — inclination near 90°.</summary>
        Polar
    }

    // ── Satellite Status ──────────────────────────────────────────────────────────

    /// <summary>Operational status of a satellite.</summary>
    public enum SatelliteStatus
    {
        /// <summary>Satellite is fully operational.</summary>
        Active,
        /// <summary>Satellite is in standby or partial operation.</summary>
        Standby,
        /// <summary>Satellite has failed and is non-functional.</summary>
        Failed,
        /// <summary>Satellite is decaying and re-entering the atmosphere.</summary>
        Decaying,
        /// <summary>Satellite has re-entered or been deorbited.</summary>
        Deorbited
    }

    // ── Debris Size ───────────────────────────────────────────────────────────────

    /// <summary>Size classification of a space debris fragment.</summary>
    public enum DebrisSize
    {
        /// <summary>Large object (&gt;10 cm) — trackable from ground radar.</summary>
        Large,
        /// <summary>Medium object (1–10 cm) — partially trackable.</summary>
        Medium,
        /// <summary>Small object (1 mm–1 cm) — not individually tracked.</summary>
        Small,
        /// <summary>Micro debris (&lt;1 mm) — paint flecks, shrapnel.</summary>
        Micro
    }

    // ── Docking State ─────────────────────────────────────────────────────────────

    /// <summary>Phase of an ISS docking approach sequence.</summary>
    public enum DockingState
    {
        /// <summary>No docking operation active.</summary>
        Idle,
        /// <summary>Long-range approach; matching orbital plane.</summary>
        FarApproach,
        /// <summary>Close-range approach within 1 km.</summary>
        NearApproach,
        /// <summary>Final alignment within 100 m.</summary>
        FinalApproach,
        /// <summary>Contact interface has made contact.</summary>
        Contact,
        /// <summary>Docking port capture ring has engaged.</summary>
        Capture,
        /// <summary>Hard-dock; vestibule pressurisation in progress.</summary>
        HardDock,
        /// <summary>Undocking and separation burn.</summary>
        Undocking
    }

    // ── Tracking Mode ─────────────────────────────────────────────────────────────

    /// <summary>Active tracking mode of the satellite tracking manager.</summary>
    public enum TrackingMode
    {
        /// <summary>Tracking is disabled.</summary>
        Off,
        /// <summary>Passive tracking — updates position from TLE data only.</summary>
        Passive,
        /// <summary>Active tracking — real-time API data, full prediction.</summary>
        Active,
        /// <summary>Locked on a single satellite or station.</summary>
        Locked,
        /// <summary>Debris radar mode — focus on nearby hazards.</summary>
        DebrisRadar
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Data classes
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Two-Line Element set parsed from NORAD TLE format.
    /// </summary>
    [Serializable]
    public class TLEData
    {
        /// <summary>Human-readable satellite name (line 0).</summary>
        public string name;
        /// <summary>NORAD catalogue number.</summary>
        public int noradId;
        /// <summary>International designator (launch year + piece).</summary>
        public string internationalDesignator;
        /// <summary>TLE epoch as Julian date.</summary>
        public double epochJulian;
        /// <summary>Mean motion first derivative (revolutions/day²).</summary>
        public double meanMotionDot;
        /// <summary>Mean motion second derivative (revolutions/day³).</summary>
        public double meanMotionDDot;
        /// <summary>BSTAR drag term.</summary>
        public double bstar;
        /// <summary>Inclination in degrees.</summary>
        public double inclinationDeg;
        /// <summary>Right Ascension of the Ascending Node in degrees.</summary>
        public double raanDeg;
        /// <summary>Orbital eccentricity (0 = circular, &lt;1 = elliptical).</summary>
        public double eccentricity;
        /// <summary>Argument of perigee in degrees.</summary>
        public double argOfPerigeeDeg;
        /// <summary>Mean anomaly in degrees.</summary>
        public double meanAnomalyDeg;
        /// <summary>Mean motion in revolutions per day.</summary>
        public double meanMotionRevPerDay;
        /// <summary>Revolution number at epoch.</summary>
        public int revNumberAtEpoch;
    }

    /// <summary>
    /// Orbital state vector representing satellite position and velocity.
    /// </summary>
    [Serializable]
    public class OrbitalState
    {
        /// <summary>Position in Earth-Centred Inertial (ECI) frame (km).</summary>
        public Vector3 positionECI;
        /// <summary>Velocity in ECI frame (km/s).</summary>
        public Vector3 velocityECI;
        /// <summary>UTC timestamp of this state.</summary>
        public DateTime utcTime;
        /// <summary>Altitude above Earth surface (km).</summary>
        public float altitudeKm;
        /// <summary>Geographic latitude (degrees).</summary>
        public float latitudeDeg;
        /// <summary>Geographic longitude (degrees).</summary>
        public float longitudeDeg;
    }

    /// <summary>
    /// Complete satellite record in the tracking database.
    /// </summary>
    [Serializable]
    public class SatelliteRecord
    {
        /// <summary>Human-readable satellite name.</summary>
        public string name;
        /// <summary>NORAD catalogue ID.</summary>
        public int noradId;
        /// <summary>Functional type of this satellite.</summary>
        public SatelliteType satelliteType;
        /// <summary>Orbital regime.</summary>
        public OrbitType orbitType;
        /// <summary>Operational status.</summary>
        public SatelliteStatus status;
        /// <summary>Owning country or agency (ISO alpha-2 or name).</summary>
        public string country;
        /// <summary>Launch date (UTC).</summary>
        public DateTime launchDate;
        /// <summary>Most recent TLE data.</summary>
        public TLEData tle;
        /// <summary>Current orbital state (updated each frame).</summary>
        public OrbitalState currentState;
        /// <summary>Whether this satellite is in the user's favourites list.</summary>
        public bool isFavourite;
        /// <summary>Visual magnitude for pass prediction (lower = brighter).</summary>
        public float visualMagnitude;
    }

    /// <summary>
    /// A predicted visible pass of a satellite over an observer location.
    /// </summary>
    [Serializable]
    public class SatellitePass
    {
        /// <summary>NORAD ID of the passing satellite.</summary>
        public int noradId;
        /// <summary>UTC time when the satellite rises above the horizon.</summary>
        public DateTime riseTime;
        /// <summary>UTC time of closest approach / maximum elevation.</summary>
        public DateTime maxElevationTime;
        /// <summary>UTC time when the satellite sets below the horizon.</summary>
        public DateTime setTime;
        /// <summary>Maximum elevation angle above horizon (degrees).</summary>
        public float maxElevationDeg;
        /// <summary>Azimuth at rise (degrees from north).</summary>
        public float riseAzimuthDeg;
        /// <summary>Azimuth at set (degrees from north).</summary>
        public float setAzimuthDeg;
        /// <summary>Predicted visual magnitude at maximum elevation.</summary>
        public float peakMagnitude;
        /// <summary>True if the pass occurs during astronomical twilight (visible).</summary>
        public bool isVisibleNight;
    }

    /// <summary>
    /// A space debris fragment tracked by the debris manager.
    /// </summary>
    [Serializable]
    public class DebrisObject
    {
        /// <summary>Internal tracking identifier.</summary>
        public int debrisId;
        /// <summary>Physical size classification.</summary>
        public DebrisSize size;
        /// <summary>NORAD ID if officially catalogued (0 = uncatalogued).</summary>
        public int noradId;
        /// <summary>Estimated cross-sectional area (m²).</summary>
        public float crossSectionM2;
        /// <summary>Tumble angular velocity (degrees/second).</summary>
        public float tumbleRateDegPerSec;
        /// <summary>Surface albedo [0..1].</summary>
        public float albedo;
        /// <summary>Current position in ECI frame (km).</summary>
        public Vector3 positionECI;
        /// <summary>Current velocity in ECI frame (km/s).</summary>
        public Vector3 velocityECI;
        /// <summary>Altitude above Earth surface (km).</summary>
        public float altitudeKm;
        /// <summary>Origin event (e.g. rocket body, fragmentation event).</summary>
        public string originEvent;
    }

    /// <summary>
    /// Result of a conjunction (close-approach) analysis between two objects.
    /// </summary>
    [Serializable]
    public class ConjunctionData
    {
        /// <summary>Primary object NORAD ID.</summary>
        public int primaryNoradId;
        /// <summary>Secondary object NORAD ID (0 = uncatalogued debris).</summary>
        public int secondaryNoradId;
        /// <summary>UTC time of time of closest approach (TCA).</summary>
        public DateTime tcaUtc;
        /// <summary>Miss distance at TCA (km).</summary>
        public float missDistanceKm;
        /// <summary>Collision probability estimate.</summary>
        public float collisionProbability;
        /// <summary>Recommended avoidance maneuver delta-V (m/s).</summary>
        public float avoidanceDeltaVms;
        /// <summary>Urgency level 0–3 (0 = informational, 3 = emergency).</summary>
        public int urgencyLevel;
    }

    /// <summary>
    /// Docking corridor definition for approach guidance.
    /// </summary>
    [Serializable]
    public class DockingCorridor
    {
        /// <summary>Name of the docking port (e.g. "PMA-2 Forward").</summary>
        public string portName;
        /// <summary>Corridor entry waypoint in local station frame (m).</summary>
        public Vector3 entryPointLocal;
        /// <summary>Docking port position in local station frame (m).</summary>
        public Vector3 portPositionLocal;
        /// <summary>Docking port approach axis (unit vector in local frame).</summary>
        public Vector3 approachAxisLocal;
        /// <summary>Maximum allowable approach speed at each range (m/s).</summary>
        public AnimationCurve maxSpeedVsRange;
        /// <summary>Maximum allowable lateral offset at each range (m).</summary>
        public AnimationCurve maxOffsetVsRange;
    }
}
