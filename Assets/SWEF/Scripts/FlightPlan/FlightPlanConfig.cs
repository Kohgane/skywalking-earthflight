// FlightPlanConfig.cs — SWEF Advanced Navigation & Flight Plan System (Phase 87)

namespace SWEF.FlightPlan
{
    /// <summary>
    /// Compile-time constants shared across the Flight Plan system.
    /// All altitudes are in feet, speeds in knots, distances in nautical miles,
    /// fuel in kilograms, and times in minutes unless otherwise noted.
    /// </summary>
    public static class FlightPlanConfig
    {
        // ── Default Cruise Parameters ─────────────────────────────────────────────
        /// <summary>Default cruise altitude in feet when none is specified.</summary>
        public const float DefaultCruiseAltitudeFt      = 35000f;

        /// <summary>Default cruise speed in knots (IAS).</summary>
        public const float DefaultCruiseSpeedKts        = 450f;

        /// <summary>Default climb rate in feet per minute.</summary>
        public const float DefaultClimbRateFpm          = 2000f;

        /// <summary>Default descent rate in feet per minute.</summary>
        public const float DefaultDescentRateFpm        = 1800f;

        /// <summary>Standard initial climb rate used in SID segments (ft/min).</summary>
        public const float SIDClimbRateFpm              = 1500f;

        // ── Fuel Consumption Rates (kg/nm) per altitude band ─────────────────────
        /// <summary>Fuel burn at sea level (kg per nautical mile).</summary>
        public const float FuelBurnSeaLevelKgPerNm      = 4.5f;

        /// <summary>Fuel burn at FL100 (10 000 ft) band (kg/nm).</summary>
        public const float FuelBurnFL100KgPerNm         = 3.8f;

        /// <summary>Fuel burn at FL200 (20 000 ft) band (kg/nm).</summary>
        public const float FuelBurnFL200KgPerNm         = 3.2f;

        /// <summary>Fuel burn at FL300 (30 000 ft) band (kg/nm).</summary>
        public const float FuelBurnFL300KgPerNm         = 2.8f;

        /// <summary>Fuel burn at FL350+ optimal cruise band (kg/nm).</summary>
        public const float FuelBurnFL350KgPerNm         = 2.5f;

        /// <summary>Baseline fuel flow rate at sea level (kg/hour).</summary>
        public const float BaseFuelFlowKgPerHour        = 2500f;

        // ── Waypoint Capture & Sequencing ─────────────────────────────────────────
        /// <summary>Radius in nautical miles within which a waypoint is considered captured.</summary>
        public const float WaypointCaptureRadiusNm      = 0.5f;

        /// <summary>
        /// Distance in nautical miles before a waypoint at which turn anticipation begins.
        /// </summary>
        public const float TurnAnticipationDistanceNm   = 1.5f;

        /// <summary>
        /// Distance in nautical miles before a waypoint at which the approaching alert fires.
        /// </summary>
        public const float WaypointApproachingAlertNm   = 3.0f;

        // ── VNAV Path Parameters ──────────────────────────────────────────────────
        /// <summary>Default VNAV glide-path angle in degrees (3° = standard ILS).</summary>
        public const float DefaultVNAVPathAngleDeg      = 3.0f;

        /// <summary>Altitude deviation threshold (ft) before VNAV issues a correction.</summary>
        public const float VNAVAltitudeDeviationFt      = 200f;

        /// <summary>Vertical speed threshold (ft/min) for level-off detection.</summary>
        public const float VNAVLevelOffThresholdFpm     = 100f;

        // ── Speed Constraint Thresholds ───────────────────────────────────────────
        /// <summary>Maximum speed (kts) below FL100 for terminal area operations.</summary>
        public const float SpeedLimitBelowFL100Kts      = 250f;

        /// <summary>Speed tolerance (kts) — deviation before FMS corrects speed.</summary>
        public const float SpeedConstraintToleranceKts  = 10f;

        // ── FMS Update Rates ──────────────────────────────────────────────────────
        /// <summary>FMS guidance update interval in seconds.</summary>
        public const float FMSUpdateIntervalSec         = 0.1f;

        /// <summary>
        /// Distance threshold (nm) of off-track deviation that triggers a route
        /// recalculation in LNAV mode.
        /// </summary>
        public const float RouteRecalcThresholdNm       = 5.0f;

        /// <summary>Proximity check coroutine interval in seconds (FlightPlanManager).</summary>
        public const float WaypointCheckIntervalSec     = 1.0f;

        /// <summary>ETA/fuel recalculation interval in seconds.</summary>
        public const float ETARecalcIntervalSec         = 30.0f;

        // ── ATC Flight Plan Submission ────────────────────────────────────────────
        /// <summary>ICAO flight plan format version used when filing with ATC.</summary>
        public const string ATCSubmissionFormatVersion  = "ICAO-2012";

        /// <summary>Minimum fuel reserve ratio (fraction of total fuel required).</summary>
        public const float MinFuelReserveRatio          = 0.10f;

        // ── Cross-Track Error Limits ──────────────────────────────────────────────
        /// <summary>Cross-track error (nm) considered acceptable for LNAV guidance.</summary>
        public const float XTKErrorAcceptableNm         = 0.3f;

        /// <summary>Cross-track error (nm) above which LNAV issues a full-correction heading.</summary>
        public const float XTKErrorMaxCorrectionNm      = 2.0f;

        // ── Holding Pattern Defaults ──────────────────────────────────────────────
        /// <summary>Default holding leg time in minutes.</summary>
        public const float DefaultHoldLegTimeMin        = 1.0f;

        /// <summary>Standard holding pattern bank angle limit in degrees.</summary>
        public const float HoldingBankAngleDeg          = 25.0f;

        // ── Fuel Warning ──────────────────────────────────────────────────────────
        /// <summary>Fuel remaining (kg) below which a FuelWarning alert is raised.</summary>
        public const float FuelWarningThresholdKg       = 500f;
    }
}
