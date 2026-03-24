// Phase 72 — Autopilot & Cruise Control System
// Assets/SWEF/Scripts/Autopilot/AutopilotController.cs
using System;
using UnityEngine;
using SWEF.Flight;
using SWEF.Landing;
using SWEF.Fuel;
using SWEF.RoutePlanner;
using SWEF.GuidedTour;

namespace SWEF.Autopilot
{
    /// <summary>
    /// Central autopilot manager: altitude hold, heading hold, speed hold,
    /// route-follow, approach-assist, and full-autopilot modes.
    /// Singleton — persists across scenes.
    /// </summary>
    [DisallowMultipleComponent]
    public class AutopilotController : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared autopilot instance.</summary>
        public static AutopilotController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitialisePIDs();
            LoadConfig();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Inspector
        [Header("Configuration")]
        [SerializeField] private AutopilotConfigSO configAsset;

        [Header("Scene References (optional — auto-located if null)")]
        [SerializeField] private FlightController flightController;
        [SerializeField] private AltitudeController altitudeController;
        [SerializeField] private WaypointNavigator waypointNavigator;
        #endregion

        #region Public State
        /// <summary>Runtime configuration (loaded from <see cref="configAsset"/> or PlayerPrefs).</summary>
        public AutopilotConfig config = new AutopilotConfig();

        /// <summary>Currently engaged autopilot mode.</summary>
        public AutopilotMode CurrentMode { get; private set; } = AutopilotMode.Off;

        /// <summary>Master on/off flag.</summary>
        public bool IsEngaged { get; private set; }

        /// <summary>Altitude target in metres.</summary>
        public float TargetAltitude { get; private set; }

        /// <summary>Heading target in degrees (0–360).</summary>
        public float TargetHeading { get; private set; }

        /// <summary>Speed target in km/h.</summary>
        public float TargetSpeed { get; private set; }

        /// <summary>Current approach phase when in ApproachAssist mode.</summary>
        public ApproachPhase CurrentApproachPhase { get; private set; } = ApproachPhase.None;

        /// <summary>Cruise-control sub-state (used when speed-hold is active).</summary>
        public CruiseControlState CruiseState { get; private set; } = CruiseControlState.Disabled;
        #endregion

        #region Events
        /// <summary>Fired when the active mode changes.</summary>
        public event Action<AutopilotMode> OnModeChanged;

        /// <summary>Fired when autopilot is engaged or disengaged.</summary>
        public event Action<bool> OnEngagementChanged;

        /// <summary>Fired as the approach phase progresses.</summary>
        public event Action<ApproachPhase> OnApproachPhaseChanged;

        /// <summary>Fired when a safety warning is generated.</summary>
        public event Action<string> OnAutopilotWarning;
        #endregion

        #region Private — PID controllers
        private PIDController _altitudePID;
        private PIDController _headingPID;
        private PIDController _speedPID;
        #endregion

        #region Private — Approach state
        private AirportData _approachAirport;
        private RunwayData _approachRunway;
        #endregion

        #region Private — Safety
        // Approximate world-space metres per degree at the equator
        private const float MetresPerDegreeLongitude = 111320f;
        private const float MetresPerDegreeLatitude  = 110540f;
        #endregion

        #region Initialise
        private void InitialisePIDs()
        {
            _altitudePID = new PIDController(config.altitudeKp, config.altitudeKi, config.altitudeKd)
            {
                OutputMin = -1f, OutputMax = 1f, IntegralMax = 10f
            };
            _headingPID = new PIDController(config.headingKp, config.headingKi, config.headingKd)
            {
                OutputMin = -1f, OutputMax = 1f, IntegralMax = 10f
            };
            _speedPID = new PIDController(config.speedKp, config.speedKi, config.speedKd)
            {
                OutputMin = -1f, OutputMax = 1f, IntegralMax = 10f
            };
        }

        private void Start()
        {
            // Auto-locate scene components if not wired in inspector
            if (flightController == null)
                flightController = FindObjectOfType<FlightController>();
            if (altitudeController == null)
                altitudeController = FindObjectOfType<AltitudeController>();
            if (waypointNavigator == null)
                waypointNavigator = FindObjectOfType<WaypointNavigator>();
        }
        #endregion

        #region Update
        private void Update()
        {
            if (!IsEngaged) return;

            float dt = Time.deltaTime;
            CheckSafety();

            switch (CurrentMode)
            {
                case AutopilotMode.AltitudeHold:   UpdateAltitudeHold(dt);  break;
                case AutopilotMode.HeadingHold:    UpdateHeadingHold(dt);   break;
                case AutopilotMode.SpeedHold:      UpdateSpeedHold(dt);     break;
                case AutopilotMode.RouteFollow:    UpdateRouteFollow(dt);   break;
                case AutopilotMode.ApproachAssist: UpdateApproach(dt);      break;
                case AutopilotMode.FullAutopilot:
                    UpdateAltitudeHold(dt);
                    UpdateHeadingHold(dt);
                    UpdateSpeedHold(dt);
                    break;
            }
        }
        #endregion

        #region Public API
        /// <summary>Engage autopilot in the specified mode.</summary>
        public void Engage(AutopilotMode mode)
        {
            if (mode == AutopilotMode.Off)
            {
                Disengage();
                return;
            }

            // Seed targets from current flight state on first engagement
            if (!IsEngaged)
            {
                SeedTargetsFromCurrentState();
                IsEngaged = true;
                OnEngagementChanged?.Invoke(true);
            }

            AutopilotMode previous = CurrentMode;
            CurrentMode = mode;

            // Reset the relevant PID(s) on mode change
            if (previous != mode)
            {
                ResetPIDsForMode(mode);
                OnModeChanged?.Invoke(mode);
            }

            if (mode == AutopilotMode.SpeedHold || mode == AutopilotMode.FullAutopilot)
                CruiseState = CruiseControlState.Maintaining;
        }

        /// <summary>Disengage autopilot completely.</summary>
        public void Disengage()
        {
            if (!IsEngaged && CurrentMode == AutopilotMode.Off) return;
            IsEngaged  = false;
            CurrentMode = AutopilotMode.Off;
            CruiseState = CruiseControlState.Disabled;
            SetApproachPhase(ApproachPhase.None);
            _altitudePID.Reset();
            _headingPID.Reset();
            _speedPID.Reset();
            OnEngagementChanged?.Invoke(false);
            OnModeChanged?.Invoke(AutopilotMode.Off);
        }

        /// <summary>Set the altitude hold target.</summary>
        public void SetTargetAltitude(float metres)
        {
            TargetAltitude = Mathf.Max(0f, metres);
        }

        /// <summary>Set the heading hold target (0–360 degrees).</summary>
        public void SetTargetHeading(float degrees)
        {
            TargetHeading = (degrees % 360f + 360f) % 360f;
        }

        /// <summary>Set the speed hold target in km/h.</summary>
        public void SetTargetSpeed(float kmh)
        {
            TargetSpeed = Mathf.Max(0f, kmh);
        }

        /// <summary>Begin an approach to the given airport (nearest runway selected automatically).</summary>
        public void StartApproach(AirportData airport)
        {
            if (airport == null) return;

            _approachAirport = airport;
            _approachRunway  = AirportRegistry.Instance != null
                ? AirportRegistry.Instance.GetBestRunway(airport, 0f)
                : (airport.runways != null && airport.runways.Count > 0 ? airport.runways[0] : null);

            if (_approachRunway == null)
            {
                OnAutopilotWarning?.Invoke("ap_warning_no_runway");
                return;
            }

            // Set approach speed
            TargetSpeed = config.approachSpeed;

            Engage(AutopilotMode.ApproachAssist);
            SetApproachPhase(ApproachPhase.Intercept);

            AutopilotAnalytics.Instance?.TrackApproachStarted(airport.airportName);
        }

        /// <summary>Returns the current autopilot mode.</summary>
        public AutopilotMode GetCurrentMode() => CurrentMode;
        #endregion

        #region Altitude Hold
        private void UpdateAltitudeHold(float dt)
        {
            if (altitudeController == null) return;

            float current = altitudeController.CurrentAltitudeMeters;
            float error   = TargetAltitude - current;

            // Within tolerance — no correction needed
            if (Mathf.Abs(error) < config.altitudeTolerance) return;

            float signal = _altitudePID.Update(error, dt);
            // Map PID output to a pitch adjustment (positive = nose up = climb)
            float pitchAdjust = Mathf.Clamp(signal * config.maxPitchAngle, -config.maxPitchAngle, config.maxPitchAngle);
            float normalised  = pitchAdjust / config.maxPitchAngle; // -1..+1

            flightController?.Step(0f, normalised, 0f);
        }
        #endregion

        #region Heading Hold
        private void UpdateHeadingHold(float dt)
        {
            if (flightController == null) return;

            float current = GetCurrentHeadingDeg();
            float error   = ShortestHeadingError(current, TargetHeading);

            if (Mathf.Abs(error) < config.headingTolerance) return;

            float signal  = _headingPID.Update(error, dt);
            // Map to yaw/roll. 90° is used as the full-deflection reference for roll normalisation
            // so that a heading error requiring maximum bank still stays within -1..+1
            float yaw  = Mathf.Clamp(signal, -1f, 1f);
            float roll = Mathf.Clamp(signal * config.maxBankAngle / 90f, -1f, 1f);

            flightController.Step(yaw, 0f, roll);
        }

        /// <summary>Shortest signed angle from <paramref name="current"/> to <paramref name="target"/> (degrees).</summary>
        private static float ShortestHeadingError(float current, float target)
        {
            float diff = target - current;
            while (diff >  180f) diff -= 360f;
            while (diff < -180f) diff += 360f;
            return diff;
        }

        private float GetCurrentHeadingDeg()
        {
            if (flightController == null) return 0f;
            Vector3 fwd = flightController.transform.forward;
            float heading = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            return (heading + 360f) % 360f;
        }
        #endregion

        #region Speed Hold
        private void UpdateSpeedHold(float dt)
        {
            if (flightController == null) return;

            float currentKmh = flightController.CurrentSpeedMps * 3.6f;
            float error      = TargetSpeed - currentKmh;

            if (Mathf.Abs(error) < config.speedTolerance)
            {
                CruiseState = CruiseControlState.Maintaining;
                return;
            }

            CruiseState = error > 0f ? CruiseControlState.Accelerating : CruiseControlState.Decelerating;

            float signal   = _speedPID.Update(error, dt);
            float newThrottle = Mathf.Clamp(flightController.Throttle01 + signal * config.maxThrottleRate * dt, 0f, 1f);
            flightController.SetThrottle(newThrottle);
        }
        #endregion

        #region Route Follow
        private void UpdateRouteFollow(float dt)
        {
            RoutePlannerManager rpm = RoutePlannerManager.Instance;
            if (rpm == null || !rpm.IsNavigating || rpm.ActiveRoute == null) return;

            RouteWaypoint wp = rpm.NextWaypoint;
            if (wp == null) return;

            // Steer toward next waypoint using bearing from current position
            Vector3 myPos = flightController != null ? flightController.transform.position : Vector3.zero;
            // Approximate world-space position from lat/lon (flat-earth offset in metres from origin)
            var wpWorldApprox = new Vector3((float)wp.longitude * MetresPerDegreeLongitude, 0f, (float)wp.latitude * MetresPerDegreeLatitude);
            Vector3 toWp = wpWorldApprox - new Vector3(myPos.x, 0f, myPos.z);

            if (toWp.sqrMagnitude > 1f)
            {
                float desiredHeading = (Mathf.Atan2(toWp.x, toWp.z) * Mathf.Rad2Deg + 360f) % 360f;
                SetTargetHeading(desiredHeading);
                UpdateHeadingHold(dt);

                // Also notify WaypointNavigator so the HUD arrow updates
                if (wp.altitude > 0f)
                    waypointNavigator?.SetManualTarget(new Vector3(wpWorldApprox.x, wp.altitude, wpWorldApprox.z));
                else
                    waypointNavigator?.SetManualTarget(wpWorldApprox);
            }

            // Altitude gate
            if (wp.requiredAltitude > 0f)
            {
                SetTargetAltitude(wp.requiredAltitude);
                UpdateAltitudeHold(dt);
            }
            else if (wp.altitude > 0f)
            {
                SetTargetAltitude(wp.altitude);
                UpdateAltitudeHold(dt);
            }

            // Speed gate
            if (wp.requiredSpeed > 0f)
            {
                SetTargetSpeed(wp.requiredSpeed);
                UpdateSpeedHold(dt);
            }
        }
        #endregion

        #region Approach Assist
        private void UpdateApproach(float dt)
        {
            if (_approachRunway == null) { Disengage(); return; }

            switch (CurrentApproachPhase)
            {
                case ApproachPhase.Intercept:   UpdateIntercept(dt);   break;
                case ApproachPhase.Glideslope:  UpdateGlideslope(dt);  break;
                case ApproachPhase.Flare:       UpdateFlare(dt);       break;
                case ApproachPhase.Rollout:     UpdateRollout(dt);     break;
            }
        }

        private void UpdateIntercept(float dt)
        {
            SetTargetHeading(_approachRunway.heading);
            UpdateHeadingHold(dt);
            UpdateSpeedHold(dt);

            float headingError = Mathf.Abs(ShortestHeadingError(GetCurrentHeadingDeg(), _approachRunway.heading));
            if (headingError < config.headingTolerance)
                SetApproachPhase(ApproachPhase.Glideslope);
        }

        private void UpdateGlideslope(float dt)
        {
            if (altitudeController == null) return;

            Vector3 myPos   = flightController != null ? flightController.transform.position : Vector3.zero;
            Vector3 thresh  = _approachRunway.thresholdPosition;
            float   distH   = Vector2.Distance(new Vector2(myPos.x, myPos.z), new Vector2(thresh.x, thresh.z));

            float desiredAlt = thresh.y + distH * Mathf.Tan(config.glideslopeAngle * Mathf.Deg2Rad);
            SetTargetAltitude(desiredAlt);
            UpdateAltitudeHold(dt);
            UpdateHeadingHold(dt);
            UpdateSpeedHold(dt);

            float aglApprox = altitudeController.CurrentAltitudeMeters - thresh.y;
            if (aglApprox <= config.flareAltitude)
                SetApproachPhase(ApproachPhase.Flare);
        }

        private void UpdateFlare(float dt)
        {
            // Level off — target altitude is the runway threshold height
            SetTargetAltitude(_approachRunway.thresholdPosition.y);
            UpdateAltitudeHold(dt);
            UpdateHeadingHold(dt);

            // Slow toward touchdown speed
            SetTargetSpeed(config.approachSpeed * 0.7f);
            UpdateSpeedHold(dt);

            float aglApprox = altitudeController != null
                ? altitudeController.CurrentAltitudeMeters - _approachRunway.thresholdPosition.y
                : float.MaxValue;

            if (aglApprox <= 1f)
                SetApproachPhase(ApproachPhase.Rollout);
        }

        private void UpdateRollout(float dt)
        {
            // Cut throttle and decelerate
            flightController?.SetThrottle(0f);
            SetTargetSpeed(0f);
            UpdateSpeedHold(dt);

            float spd = flightController != null ? flightController.CurrentSpeedMps : 0f;
            if (spd < 1f)
            {
                AutopilotAnalytics.Instance?.TrackApproachCompleted(_approachAirport?.airportName, success: true);
                Disengage();
            }
        }

        private void SetApproachPhase(ApproachPhase phase)
        {
            if (CurrentApproachPhase == phase) return;
            CurrentApproachPhase = phase;
            OnApproachPhaseChanged?.Invoke(phase);
        }
        #endregion

        #region Safety
        private void CheckSafety()
        {
            if (altitudeController != null && altitudeController.CurrentAltitudeMeters < config.safeAltitudeMinAgl)
            {
                OnAutopilotWarning?.Invoke("ap_warning_terrain");
                Disengage();
                return;
            }

            if (flightController != null)
            {
                float speedKmh = flightController.CurrentSpeedMps * 3.6f;
                if (speedKmh < config.stallSpeedKmh && IsEngaged)
                {
                    OnAutopilotWarning?.Invoke("ap_warning_stall");
                    // Stall protection: increase throttle
                    flightController.SetThrottle(Mathf.Min(flightController.Throttle01 + 0.1f, 1f));
                }
            }

            if (FuelManager.Instance != null && FuelManager.Instance.TotalFuelPercent < config.fuelLowThreshold)
                OnAutopilotWarning?.Invoke("ap_warning_fuel");
        }
        #endregion

        #region Helpers
        private void SeedTargetsFromCurrentState()
        {
            if (altitudeController != null)
                TargetAltitude = altitudeController.CurrentAltitudeMeters;

            TargetHeading = GetCurrentHeadingDeg();

            if (flightController != null)
                TargetSpeed = flightController.CurrentSpeedMps * 3.6f;
        }

        private void ResetPIDsForMode(AutopilotMode mode)
        {
            switch (mode)
            {
                case AutopilotMode.AltitudeHold: _altitudePID.Reset(); break;
                case AutopilotMode.HeadingHold:  _headingPID.Reset();  break;
                case AutopilotMode.SpeedHold:    _speedPID.Reset();    break;
                case AutopilotMode.FullAutopilot:
                case AutopilotMode.RouteFollow:
                case AutopilotMode.ApproachAssist:
                    _altitudePID.Reset();
                    _headingPID.Reset();
                    _speedPID.Reset();
                    break;
            }
        }
        #endregion

        #region Persistence
        private const string PrefKey = "SWEF_AutopilotConfig";

        private void LoadConfig()
        {
            if (configAsset != null)
            {
                config = configAsset.ToRuntimeConfig();
            }
            else if (PlayerPrefs.HasKey(PrefKey))
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PrefKey), config);
                }
                catch { /* keep defaults */ }
            }
            // Re-apply gains to PID controllers
            _altitudePID?.SetGains(config.altitudeKp, config.altitudeKi, config.altitudeKd);
            _headingPID?.SetGains(config.headingKp,  config.headingKi,  config.headingKd);
            _speedPID?.SetGains(config.speedKp,    config.speedKi,    config.speedKd);
        }

        private void OnApplicationQuit()
        {
            PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(config));
            PlayerPrefs.Save();
        }
        #endregion
    }
}
