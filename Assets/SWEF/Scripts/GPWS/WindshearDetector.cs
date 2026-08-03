using UnityEngine;

#if SWEF_WEATHER_AVAILABLE
using SWEF.Weather;
#endif

#if SWEF_COCKPITHUD_AVAILABLE
using SWEF.CockpitHUD;
#endif

namespace SWEF.GPWS
{
    /// <summary>
    /// Phase 121 — Reactive windshear and microburst detector.
    ///
    /// <para>Monitors airspeed and altitude-rate changes frame-to-frame.
    /// When sudden deviations exceed thresholds, triggers a windshear warning
    /// via <see cref="GPWSManager"/>. Provides escape guidance (pitch-up target,
    /// max thrust) for the flight director. Reads wind data from
    /// <c>SWEF.Weather.WindSystem</c> when available.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class WindshearDetector : MonoBehaviour
    {
        #region Inspector

        [Header("Detection Thresholds")]
        [Tooltip("Airspeed change in knots/second that constitutes windshear.")]
        [SerializeField] private float airspeedRateThresholdKtsPerSec = 15f;

        [Tooltip("Vertical speed change in fpm/second threshold.")]
        [SerializeField] private float vsRateThresholdFpmPerSec = 500f;

        [Header("Microburst Detection")]
        [Tooltip("Wind-layer altitude difference in metres for microburst sampling.")]
        [SerializeField] private float microburstSampleAltDeltaM = 100f;

        [Tooltip("Horizontal wind shear in m/s between layers to flag microburst.")]
        [SerializeField] private float microburstThresholdMps = 7.5f;

        [Header("Escape Guidance")]
        [Tooltip("Recommended pitch-up angle in degrees during windshear escape.")]
        [SerializeField] private float escapePitchDeg = 15f;

        #endregion

        #region Public Properties

        /// <summary>True while a windshear condition is active.</summary>
        public bool WindshearActive { get; private set; }

        /// <summary>Recommended escape pitch target in degrees.</summary>
        public float EscapePitchTarget => escapePitchDeg;

        /// <summary>Detected windshear intensity in knots.</summary>
        public float CurrentShearKnots { get; private set; }

        /// <summary>True if a microburst was detected in the wind layers.</summary>
        public bool MicroburstDetected { get; private set; }

        #endregion

        #region Private State

        private GPWSManager _mgr;
        private float _prevAirspeedKnots;
        private float _prevVerticalSpeedFpm;
        private float _shearCooldown;

        private const float CooldownDuration = 5f;
        private const float MpsToKnots = 1.94384f;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _mgr = GPWSManager.Instance;
        }

        private void Update()
        {
            if (_mgr == null || !_mgr.IsEnabled || !_mgr.Config.enableMode7) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float currentAirspeed = _mgr.AirspeedKnots;
            float currentVs = _mgr.VerticalSpeedFpm;

            float airspeedRate = (currentAirspeed - _prevAirspeedKnots) / dt;
            float vsRate = (currentVs - _prevVerticalSpeedFpm) / dt;

            _prevAirspeedKnots = currentAirspeed;
            _prevVerticalSpeedFpm = currentVs;

            bool reactiveShear = Mathf.Abs(airspeedRate) > airspeedRateThresholdKtsPerSec
                                 || Mathf.Abs(vsRate) > vsRateThresholdFpmPerSec;

            DetectMicroburst();

            if ((reactiveShear || MicroburstDetected) && _shearCooldown <= 0f)
            {
                CurrentShearKnots = Mathf.Abs(airspeedRate);
                WindshearActive = true;
                _shearCooldown = CooldownDuration;

                _mgr.RaiseAlert(GPWSMode.Mode7_WindshearWarning, GPWSAlertLevel.Warning, "gpws_windshear");
                _mgr.TriggerWindshearAlert(CurrentShearKnots);
            }
            else if (WindshearActive && _shearCooldown > 0f)
            {
                _shearCooldown -= dt;
                if (_shearCooldown <= 0f)
                {
                    WindshearActive = false;
                    MicroburstDetected = false;
                    CurrentShearKnots = 0f;
                    _mgr.ClearAlert(GPWSMode.Mode7_WindshearWarning);
                }
            }
        }

        #endregion

        #region Microburst Detection

        private void DetectMicroburst()
        {
#if SWEF_WEATHER_AVAILABLE
            var ws = WindSystem.Instance;
            if (ws == null) return;

            float altM = _mgr.AircraftAltitudeAglFt / 3.28084f;
            Vector3 windHere = ws.GetWindAtAltitude(altM);
            Vector3 windAbove = ws.GetWindAtAltitude(altM + microburstSampleAltDeltaM);
            Vector3 windBelow = ws.GetWindAtAltitude(Mathf.Max(0f, altM - microburstSampleAltDeltaM));

            float shearAbove = (windAbove - windHere).magnitude;
            float shearBelow = (windHere - windBelow).magnitude;

            MicroburstDetected = shearAbove > microburstThresholdMps || shearBelow > microburstThresholdMps;
#else
            MicroburstDetected = false;
#endif
        }

        #endregion
    }
}
