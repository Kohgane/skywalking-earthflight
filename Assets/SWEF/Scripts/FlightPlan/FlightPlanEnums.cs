// FlightPlanEnums.cs — SWEF Advanced Navigation & Flight Plan System (Phase 87)

namespace SWEF.FlightPlan
{
    /// <summary>Flight rules classification for the flight plan.</summary>
    public enum FlightRuleType
    {
        /// <summary>Instrument Flight Rules — relies on instruments, ATC separation.</summary>
        IFR,
        /// <summary>Visual Flight Rules — visual meteorological conditions required.</summary>
        VFR,
        /// <summary>Special VFR — controlled-airspace VFR clearance below VMC minima.</summary>
        SVFR,
        /// <summary>Defense VFR — military operations under visual rules.</summary>
        DVFR
    }

    /// <summary>Lifecycle state of a flight plan.</summary>
    public enum FlightPlanStatus
    {
        /// <summary>Plan is being built, not yet submitted.</summary>
        Draft,
        /// <summary>Plan has been filed with ATC.</summary>
        Filed,
        /// <summary>ATC has approved the flight plan.</summary>
        Approved,
        /// <summary>Aircraft is currently following the plan.</summary>
        Active,
        /// <summary>All waypoints have been reached; flight complete.</summary>
        Completed,
        /// <summary>Plan was cancelled before completion.</summary>
        Cancelled,
        /// <summary>Aircraft diverted to an alternate airport.</summary>
        Diverted
    }

    /// <summary>Navigation fix type used to classify a flight plan waypoint.</summary>
    public enum WaypointCategory
    {
        /// <summary>Airport or aerodrome.</summary>
        Airport,
        /// <summary>VHF Omnidirectional Range station.</summary>
        VOR,
        /// <summary>Non-Directional Beacon.</summary>
        NDB,
        /// <summary>Named route intersection (5-letter ICAO fix).</summary>
        Intersection,
        /// <summary>GPS-only latitude/longitude fix.</summary>
        GPS,
        /// <summary>Pilot-defined custom position.</summary>
        UserDefined,
        /// <summary>Standard Instrument Departure waypoint.</summary>
        SID,
        /// <summary>Standard Terminal Arrival Route waypoint.</summary>
        STAR,
        /// <summary>Instrument approach procedure waypoint.</summary>
        Approach,
        /// <summary>Missed approach procedure waypoint.</summary>
        Missed
    }

    /// <summary>Category of published instrument procedure.</summary>
    public enum ProcedureType
    {
        /// <summary>Standard Instrument Departure.</summary>
        SID,
        /// <summary>Standard Terminal Arrival Route.</summary>
        STAR,
        /// <summary>Instrument approach (ILS, RNAV, VOR, etc.).</summary>
        Approach,
        /// <summary>Missed approach segment.</summary>
        MissedApproach,
        /// <summary>Published holding procedure.</summary>
        Holding,
        /// <summary>Circling approach to a non-aligned runway.</summary>
        CirclingApproach
    }

    /// <summary>Active guidance mode of the Flight Management System.</summary>
    public enum FMSMode
    {
        /// <summary>No FMS guidance — hand-flown.</summary>
        Manual,
        /// <summary>Lateral navigation only.</summary>
        LNAV,
        /// <summary>Vertical navigation only.</summary>
        VNAV,
        /// <summary>Both lateral and vertical navigation active.</summary>
        LNAVAndVNAV,
        /// <summary>Precision approach guidance mode.</summary>
        Approach,
        /// <summary>Go-around / missed approach mode.</summary>
        GoAround,
        /// <summary>Published or pilot-entered holding pattern.</summary>
        Holding
    }

    /// <summary>Leg geometry type between two consecutive waypoints.</summary>
    public enum LegType
    {
        /// <summary>Fly direct from current position to the fix.</summary>
        DirectTo,
        /// <summary>Fly a specific magnetic track to the fix.</summary>
        TrackToFix,
        /// <summary>Arc of specified radius centred on a navaid to the fix.</summary>
        RadiusToFix,
        /// <summary>Fly a racetrack holding pattern at the fix.</summary>
        HoldingPattern,
        /// <summary>DME arc at constant distance from a navaid.</summary>
        ArcDME,
        /// <summary>Procedure turn to reverse course before the fix.</summary>
        ProcedureTurn
    }

    /// <summary>Alert categories surfaced by <see cref="FlightPlanManager"/>.</summary>
    public enum FlightPlanAlertType
    {
        /// <summary>Aircraft is approaching the next active waypoint.</summary>
        WaypointApproaching,
        /// <summary>Aircraft has reached the top-of-descent point.</summary>
        TopOfDescent,
        /// <summary>Aircraft has reached the top-of-climb point.</summary>
        TopOfClimb,
        /// <summary>Fuel reserves are below the required minimum.</summary>
        FuelWarning,
        /// <summary>Estimated time en-route has been updated.</summary>
        ETAUpdate,
        /// <summary>Significant weather advisory along the route.</summary>
        WeatherAdvisory,
        /// <summary>Natural disaster hazard detected on the planned route.</summary>
        DisasterHazard,
        /// <summary>Aircraft is about to enter a new airspace zone.</summary>
        AirspaceEntry
    }
}
