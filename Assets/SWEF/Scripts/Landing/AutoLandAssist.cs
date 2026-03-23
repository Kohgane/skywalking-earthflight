// AutoLandAssist.cs — SWEF Landing & Airport System (Phase 68)
using System;
using UnityEngine;

namespace SWEF.Landing
{
    /// <summary>
    /// Phase 68 — Automatic landing assistance system.
    ///
    /// <para>Supports four modes:
    /// <list type="bullet">
    ///   <item><see cref="AutoLandMode.Off"/> — completely disengaged.</item>
    ///   <item><see cref="AutoLandMode.GuidanceOnly"/> — ILS overlay visible; pilot flies manually.</item>
    ///   <item><see cref="AutoLandMode.SemiAuto"/> — system corrects heading and glide slope; pilot manages throttle.</item>
    ///   <item><see cref="AutoLandMode.FullAuto"/> — system controls pitch, roll, throttle, and flare.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class AutoLandAssist : MonoBehaviour
    {
        // ── Nested Enum ───────────────────────────────────────────────────────

        /// <summary>Auto-land engagement modes.</summary>
        public enum AutoLandMode
        {
            /// <summary>Auto-land disengaged; pilot has full control.</summary>
            Off,
            /// <summary>Approach indicators displayed; pilot flies manually.</summary>
            GuidanceOnly,
            /// <summary>System corrects heading and glide slope; pilot controls throttle.</summary>
            SemiAuto,
            /// <summary>System controls pitch, roll, throttle, and flare.</summary>
            FullAuto
        }

        #region Inspector

        [Header("Auto-Land — Limits")]
        [Tooltip("Maximum crosswind (m/s) that permits auto-land engagement.")]
        [SerializeField] private float maxCrosswind = LandingConfig.MaxAutoLandCrosswind;

        [Tooltip("AGL altitude (m) at which auto-land captures the approach.")]
        [SerializeField] private float captureAltitude = LandingConfig.AutoLandCaptureAltitude;

        [Header("Auto-Land — Control Gains (SemiAuto / FullAuto)")]
        [Tooltip("Roll correction gain for lateral (localizer) tracking.")]
        [SerializeField] private float rollGain = 15f;

        [Tooltip("Pitch correction gain for vertical (glide slope) tracking.")]
        [SerializeField] private float pitchGain = 5f;

        [Tooltip("Throttle reduction rate during the flare manoeuvre.")]
        [SerializeField] private float flareThrottleDecay = 0.5f;

        [Tooltip("Target pitch-up rate (degrees/s) during flare.")]
        [SerializeField] private float flarePitchRate = 3f;

        [Header("Auto-Land — References")]
        [Tooltip("ApproachGuidance component providing ILS deviations. Auto-resolved if null.")]
        [SerializeField] private ApproachGuidance approachGuidance;

        [Tooltip("LandingDetector component tracking state. Auto-resolved if null.")]
        [SerializeField] private LandingDetector landingDetector;

        [Tooltip("Rigidbody of the aircraft. Auto-resolved if null.")]
        [SerializeField] private Rigidbody aircraftRigidbody;

        #endregion

        #region Public State

        /// <summary>Current auto-land engagement mode.</summary>
        public AutoLandMode Mode { get; private set; } = AutoLandMode.Off;

        /// <summary>The runway targeted for auto-land.</summary>
        public RunwayData TargetRunway { get; private set; }

        /// <summary><c>true</c> when auto-land has captured the approach (below capture altitude).</summary>
        public bool IsCaptured { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired when <see cref="Mode"/> changes.</summary>
        public event Action<AutoLandMode> OnAutoLandModeChanged;

        /// <summary>Fired once when auto-land captures the approach.</summary>
        public event Action OnAutoLandCapture;

        /// <summary>Fired when auto-land is disengaged.</summary>
        public event Action OnAutoLandDisengage;

        #endregion

        #region Private State

        private float _currentThrottle = 0.5f;
        private float _currentPitch;
        private float _currentRoll;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (approachGuidance == null) approachGuidance = GetComponent<ApproachGuidance>();
            if (landingDetector  == null) landingDetector  = GetComponent<LandingDetector>();
            if (aircraftRigidbody == null) aircraftRigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (Mode == AutoLandMode.Off || TargetRunway == null) return;
            CheckCapture();
            if (!IsCaptured) return;
            ApplyControl();
        }

        #endregion

        #region Public API

        /// <summary>Engages auto-land toward the specified runway in the given mode.</summary>
        /// <param name="runway">The runway to land on.</param>
        /// <param name="autoMode">The desired auto-land mode.</param>
        public void Engage(RunwayData runway, AutoLandMode autoMode)
        {
            if (autoMode == AutoLandMode.Off)
            {
                Disengage();
                return;
            }
            TargetRunway = runway;
            IsCaptured   = false;
            SetMode(autoMode);
        }

        /// <summary>Disengages auto-land and returns full control to the pilot.</summary>
        public void Disengage()
        {
            TargetRunway = null;
            IsCaptured   = false;
            SetMode(AutoLandMode.Off);
            OnAutoLandDisengage?.Invoke();
        }

        #endregion

        #region Capture

        private void CheckCapture()
        {
            if (IsCaptured) return;
            float agl = GetAGL();
            if (agl > captureAltitude) return;

            // Check crosswind limit
            float crosswind = GetCrosswindComponent();
            if (crosswind > maxCrosswind)
            {
                Debug.LogWarning("[AutoLandAssist] Crosswind exceeds limit — auto-land unavailable.");
                Disengage();
                return;
            }

            IsCaptured = true;
            OnAutoLandCapture?.Invoke();
        }

        #endregion

        #region Control

        private void ApplyControl()
        {
            if (approachGuidance == null) return;

            LandingState state = landingDetector != null
                ? landingDetector.CurrentState
                : LandingState.InFlight;

            bool isFlaring = state == LandingState.Flaring || GetAGL() < LandingConfig.FlareAltitude;

            switch (Mode)
            {
                case AutoLandMode.GuidanceOnly:
                    // No flight control output — guidance display only.
                    break;

                case AutoLandMode.SemiAuto:
                    ApplyHeadingAndGlideSlopeCorrections(isFlaring);
                    break;

                case AutoLandMode.FullAuto:
                    ApplyHeadingAndGlideSlopeCorrections(isFlaring);
                    ApplyThrottle(isFlaring);
                    break;
            }
        }

        private void ApplyHeadingAndGlideSlopeCorrections(bool isFlaring)
        {
            if (aircraftRigidbody == null) return;

            float locDev = approachGuidance.LocalizerDeviation;
            float gsDev  = approachGuidance.GlideSlopeDeviation;

            float targetRoll  = -locDev * rollGain;
            float targetPitch = isFlaring
                ? _currentPitch + flarePitchRate * Time.fixedDeltaTime
                : -gsDev * pitchGain;

            _currentRoll  = Mathf.Lerp(_currentRoll,  targetRoll,  Time.fixedDeltaTime * 2f);
            _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.fixedDeltaTime * 2f);

            Quaternion targetRotation = Quaternion.Euler(_currentPitch, transform.eulerAngles.y, _currentRoll);
            aircraftRigidbody.MoveRotation(
                Quaternion.Slerp(aircraftRigidbody.rotation, targetRotation, Time.fixedDeltaTime * 3f));
        }

        private void ApplyThrottle(bool isFlaring)
        {
            if (aircraftRigidbody == null) return;
            if (isFlaring)
                _currentThrottle = Mathf.Max(0f, _currentThrottle - flareThrottleDecay * Time.fixedDeltaTime);
            // Throttle value is made available for the flight controller to pick up via
            // a shared FlightData or direct property; output is intentionally advisory here.
        }

        #endregion

        #region Helpers

        private void SetMode(AutoLandMode next)
        {
            if (Mode == next) return;
            Mode = next;
            OnAutoLandModeChanged?.Invoke(Mode);
        }

        private float GetAGL()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2000f))
                return hit.distance;
            return float.MaxValue;
        }

        private float GetCrosswindComponent()
        {
            if (TargetRunway == null) return 0f;
#if SWEF_WEATHER_AVAILABLE
            var weather = FindFirstObjectByType<SWEF.Weather.WeatherManager>();
            if (weather == null) return 0f;
            float windDir   = weather.CurrentWindDirection * Mathf.Deg2Rad;
            Vector3 windVec = new Vector3(Mathf.Sin(windDir), 0f, Mathf.Cos(windDir)) * weather.CurrentWindSpeed;
            Vector3 runDir  = TargetRunway.GetRunwayDirection();
            return Vector3.Cross(windVec, runDir).magnitude;
#else
            return 0f;
#endif
        }

        #endregion
    }
}
