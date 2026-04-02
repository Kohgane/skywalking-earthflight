// FMSController.cs — SWEF Advanced Navigation & Flight Plan System (Phase 87)
using System.Collections;
using UnityEngine;

#if SWEF_WEATHER_AVAILABLE
using SWEF.Weather;
#endif

#if SWEF_DISASTER_AVAILABLE
using SWEF.NaturalDisaster;
#endif

namespace SWEF.FlightPlan
{
    /// <summary>
    /// Phase 87 — Flight Management System controller.
    ///
    /// <para>Provides LNAV (lateral) and VNAV (vertical) guidance by computing
    /// desired heading, vertical speed, and target speed from the active
    /// <see cref="FlightPlanManager"/> route, then commanding the
    /// <see cref="Flight.FlightController"/> accordingly.</para>
    ///
    /// <para>Attach to the player aircraft GameObject.  Requires
    /// <see cref="FlightPlanManager"/> to be active in the scene.</para>
    /// </summary>
    public class FMSController : MonoBehaviour
    {
        #region Inspector

        [Header("FMS References")]
        [Tooltip("Aircraft FlightController — auto-found if null.")]
        [SerializeField] private Flight.FlightController _flightController;

        [Header("LNAV")]
        [Tooltip("Proportional gain for cross-track error correction.")]
        [Min(0f)]
        public float lnavKp = 2.0f;

        [Tooltip("Maximum heading correction offset (degrees) LNAV will apply.")]
        [Range(0f, 90f)]
        public float maxLNAVCorrectionDeg = 45f;

        [Header("VNAV")]
        [Tooltip("Proportional gain for altitude deviation correction.")]
        [Min(0f)]
        public float vnavKp = 0.5f;

        [Tooltip("Maximum vertical speed command (ft/min) VNAV will apply.")]
        [Min(0f)]
        public float maxVNAVVerticalSpeedFpm = 3000f;

        #endregion

        #region Public State

        /// <summary>Current FMS guidance mode.</summary>
        public FMSMode currentMode { get; private set; } = FMSMode.Manual;

        /// <summary>Lateral cross-track error in nautical miles (positive = right of track).</summary>
        public float crossTrackErrorNm { get; private set; }

        /// <summary>Vertical deviation from the planned altitude profile in feet.</summary>
        public float verticalDeviationFt { get; private set; }

        /// <summary>FMS-computed desired magnetic heading (degrees).</summary>
        public float desiredHeading { get; private set; }

        /// <summary>FMS-computed desired vertical speed (ft/min).</summary>
        public float desiredVerticalSpeed { get; private set; }

        /// <summary>FMS-computed target indicated airspeed (knots).</summary>
        public float desiredSpeed { get; private set; }

        /// <summary>Distance to the top-of-descent point in nm (negative = already past TOD).</summary>
        public float distanceToTODNm { get; private set; }

        /// <summary>Distance to the top-of-climb point in nm (negative = already past TOC).</summary>
        public float distanceToTOCNm { get; private set; }

        #endregion

        #region Private State

        private bool _lnavEngaged;
        private bool _vnavEngaged;
        private Coroutine _holdingCoroutine;

        // Holding-pattern state
        private bool   _inHolding;
        private float  _holdingInboundCourse;
        private float  _holdingLegTime;
        private double _holdLat;
        private double _holdLon;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_flightController == null)
                _flightController = GetComponentInParent<Flight.FlightController>();
            if (_flightController == null)
                _flightController = FindFirstObjectByType<Flight.FlightController>();
        }

        private void Update()
        {
            if (FlightPlanManager.Instance == null) return;
            if (FlightPlanManager.Instance.activePlan == null) return;
            if (FlightPlanManager.Instance.activePlan.status != FlightPlanStatus.Active) return;

            if (_lnavEngaged) UpdateLNAV();
            if (_vnavEngaged) UpdateVNAV();
        }

        #endregion

        #region Public API

        /// <summary>Engages lateral navigation mode.</summary>
        public void EngageLNAV()
        {
            _lnavEngaged = true;
            UpdateMode();
            Debug.Log("[SWEF] FMSController: LNAV engaged.");
        }

        /// <summary>Engages vertical navigation mode.</summary>
        public void EngageVNAV()
        {
            _vnavEngaged = true;
            UpdateMode();
            Debug.Log("[SWEF] FMSController: VNAV engaged.");
        }

        /// <summary>Disengages all FMS guidance modes and returns to Manual.</summary>
        public void Disengage()
        {
            _lnavEngaged = false;
            _vnavEngaged = false;
            _inHolding   = false;
            if (_holdingCoroutine != null)
            {
                StopCoroutine(_holdingCoroutine);
                _holdingCoroutine = null;
            }
            currentMode = FMSMode.Manual;
            Debug.Log("[SWEF] FMSController: disengaged — Manual.");
        }

        /// <summary>
        /// Initiates a holding pattern at the given fix.
        /// LNAV must be engaged for the FMS to fly the hold.
        /// </summary>
        public void SetHoldingPattern(FlightPlanWaypoint wp, float inboundCourse, float legTime)
        {
            if (wp == null) return;
            _holdLat             = wp.latitude;
            _holdLon             = wp.longitude;
            _holdingInboundCourse = inboundCourse;
            _holdingLegTime      = legTime > 0f ? legTime : FlightPlanConfig.DefaultHoldLegTimeMin;
            _inHolding           = true;
            currentMode          = FMSMode.Holding;
            Debug.Log($"[SWEF] FMSController: holding pattern at {wp.name}, inbound {inboundCourse}°, {_holdingLegTime} min.");
        }

        #endregion

        #region LNAV

        private void UpdateLNAV()
        {
            var mgr = FlightPlanManager.Instance;
            var wp  = mgr.ActiveWaypoint;
            if (wp == null) return;

            // Current aircraft position (approx lat/lon from world-space)
            double pLat, pLon;
            GetPlayerLatLon(out pLat, out pLon);

            // Cross-track error to next waypoint
            crossTrackErrorNm = ComputeCrossTrackError(pLat, pLon, wp);

            // Bearing to next waypoint
            float bearingDeg = (float)ComputeBearing(pLat, pLon, wp.latitude, wp.longitude);

            // LNAV correction: offset bearing to correct XTK error
            float correction = Mathf.Clamp(-crossTrackErrorNm * lnavKp,
                                           -maxLNAVCorrectionDeg, maxLNAVCorrectionDeg);
            desiredHeading = (bearingDeg + correction + 360f) % 360f;

            // Wind correction (crab angle)
#if SWEF_WEATHER_AVAILABLE
            if (WeatherManager.Instance != null)
            {
                float windSpeed = WeatherManager.Instance.GetAverageWindSpeed();
                float windDir   = WeatherManager.Instance.GetAverageWindDirection();
                float crab      = ComputeWindCorrectionAngle(windSpeed, windDir, bearingDeg,
                                                             mgr.activePlan.cruiseSpeed);
                desiredHeading = (desiredHeading + crab + 360f) % 360f;
            }
#endif

            // Disaster avoidance alert
#if SWEF_DISASTER_AVAILABLE
            if (_flightController != null && DisasterManager.Instance != null)
            {
                float alt = _flightController.transform.position.y;
                if (DisasterManager.Instance.IsInNoFlyZone(_flightController.transform.position, alt))
                    mgr.OnPlanAlert?.Invoke(FlightPlanAlertType.DisasterHazard);
            }
#endif

            // Issue heading command to FlightController
            // (FlightController heading API is consumed via its own autopilot hooks;
            //  we store the desired value so FMSController can expose it to autopilot/HUD)
        }

        #endregion

        #region VNAV

        private void UpdateVNAV()
        {
            var mgr  = FlightPlanManager.Instance;
            var plan = mgr.activePlan;
            var wp   = mgr.ActiveWaypoint;
            if (wp == null) return;

            float currentAltFt = GetCurrentAltitudeFt();
            float targetAltFt  = GetTargetAltitude(mgr);

            verticalDeviationFt = currentAltFt - targetAltFt;

            // Compute desired VS to correct deviation
            desiredVerticalSpeed = Mathf.Clamp(-verticalDeviationFt * vnavKp,
                                               -maxVNAVVerticalSpeedFpm, maxVNAVVerticalSpeedFpm);

            // Speed management
            float speedLimit = (currentAltFt < FlightPlanConfig.SpeedLimitBelowFL100 * 100f)
                ? FlightPlanConfig.SpeedLimitBelowFL100Kts
                : plan.cruiseSpeed;

            desiredSpeed = wp.speedConstraint > 0f
                ? Mathf.Min(wp.speedConstraint, speedLimit)
                : speedLimit;

            // TOC / TOD computation
            ComputeVerticalProfile(mgr);

            // Alert on TOC/TOD crossing
            if (distanceToTODNm is >= -0.1f and <= FlightPlanConfig.WaypointApproachingAlertNm)
                mgr.OnPlanAlert?.Invoke(FlightPlanAlertType.TopOfDescent);

            if (distanceToTOCNm is >= -0.1f and <= FlightPlanConfig.WaypointApproachingAlertNm)
                mgr.OnPlanAlert?.Invoke(FlightPlanAlertType.TopOfClimb);
        }

        private void ComputeVerticalProfile(FlightPlanManager mgr)
        {
            var plan = mgr.activePlan;

            // TOC: distance needed to climb from departure altitude to cruise altitude
            float departureAlt = plan.waypoints.Count > 0 ? plan.waypoints[0].altitude : 0f;
            float climbDist    = (plan.cruiseAltitude - departureAlt)
                               / FlightPlanConfig.DefaultClimbRateFpm
                               * (plan.cruiseSpeed / 60f); // nm = min × nm/min
            distanceToTOCNm = climbDist - (mgr.activePlan.totalDistanceNm - mgr.GetTotalRemainingNm());

            // TOD: distance needed to descend from cruise to destination altitude
            float destAlt      = plan.waypoints.Count > 0
                ? plan.waypoints[plan.waypoints.Count - 1].altitude
                : 0f;
            float descentDist  = (plan.cruiseAltitude - destAlt)
                               / FlightPlanConfig.DefaultDescentRateFpm
                               * (plan.cruiseSpeed / 60f);
            distanceToTODNm    = mgr.GetTotalRemainingNm() - descentDist;
        }

        private float GetTargetAltitude(FlightPlanManager mgr)
        {
            var wp = mgr.ActiveWaypoint;
            if (wp != null && wp.altitude > 0f)
                return wp.altitude;
            return mgr.activePlan.cruiseAltitude;
        }

        #endregion

        #region Geodesic & Wind Helpers

        private float ComputeCrossTrackError(double pLat, double pLon, FlightPlanWaypoint wp)
        {
            var mgr = FlightPlanManager.Instance;
            int idx = mgr.activeWaypointIndex;
            if (idx <= 0 || mgr.activePlan.waypoints.Count < 2)
                return 0f;

            var prev = mgr.activePlan.waypoints[idx - 1];

            // Compute signed cross-track distance from prev→wp track
            double d13    = NavigationDatabase.HaversineNm(prev.latitude, prev.longitude, pLat, pLon);
            double brg12  = ComputeBearing(prev.latitude, prev.longitude, wp.latitude, wp.longitude);
            double brg13  = ComputeBearing(prev.latitude, prev.longitude, pLat, pLon);
            double dRad   = d13 / 3440.065; // convert nm to radians
            double xtk    = System.Math.Asin(System.Math.Sin(dRad)
                            * System.Math.Sin((brg13 - brg12) * System.Math.PI / 180.0))
                            * 3440.065;
            return (float)xtk;
        }

        private static double ComputeBearing(double lat1, double lon1, double lat2, double lon2)
        {
            double la1  = lat1 * System.Math.PI / 180.0;
            double la2  = lat2 * System.Math.PI / 180.0;
            double dLon = (lon2 - lon1) * System.Math.PI / 180.0;
            double y    = System.Math.Sin(dLon) * System.Math.Cos(la2);
            double x    = System.Math.Cos(la1) * System.Math.Sin(la2)
                        - System.Math.Sin(la1) * System.Math.Cos(la2) * System.Math.Cos(dLon);
            double brg  = System.Math.Atan2(y, x) * 180.0 / System.Math.PI;
            return (brg + 360.0) % 360.0;
        }

        private static float ComputeWindCorrectionAngle(float windSpeed, float windDir,
                                                        float trackDeg, float trueAirspeedKts)
        {
            if (trueAirspeedKts < 1f || windSpeed < 0.1f) return 0f;
            // Wind correction angle (WCA): positive = crab right
            float relDir = (windDir - trackDeg) * Mathf.Deg2Rad;
            return Mathf.Asin(windSpeed * Mathf.Sin(relDir) / trueAirspeedKts) * Mathf.Rad2Deg;
        }

        private void GetPlayerLatLon(out double lat, out double lon)
        {
            if (_flightController != null)
            {
                lat = _flightController.transform.position.z / 111320.0;
                lon = _flightController.transform.position.x / 111320.0;
            }
            else
            {
                lat = 0;
                lon = 0;
            }
        }

        private float GetCurrentAltitudeFt()
        {
            if (_flightController == null) return 0f;
            return _flightController.transform.position.y * 3.28084f; // m → ft
        }

        #endregion

        #region Mode Bookkeeping

        private void UpdateMode()
        {
            if (_inHolding)                 { currentMode = FMSMode.Holding;       return; }
            if (_lnavEngaged && _vnavEngaged) { currentMode = FMSMode.LNAVAndVNAV; return; }
            if (_lnavEngaged)               { currentMode = FMSMode.LNAV;          return; }
            if (_vnavEngaged)               { currentMode = FMSMode.VNAV;          return; }
            currentMode = FMSMode.Manual;
        }

        #endregion
    }
}
