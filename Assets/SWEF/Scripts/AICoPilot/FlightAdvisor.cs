// Phase 100 — AI Co-Pilot & Smart Assistant
// Assets/SWEF/Scripts/AICoPilot/FlightAdvisor.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AICoPilot
{
    /// <summary>
    /// Severity level of a flight advisory issued by <see cref="FlightAdvisor"/>.
    /// </summary>
    public enum AdvisoryLevel
    {
        /// <summary>Informational — no immediate action required.</summary>
        Info,

        /// <summary>Caution — pilot awareness recommended.</summary>
        Caution,

        /// <summary>Warning — corrective action recommended soon.</summary>
        Warning,

        /// <summary>Critical — immediate action required.</summary>
        Critical
    }

    /// <summary>
    /// Broad phase of flight used to provide context-aware advice.
    /// </summary>
    public enum FlightPhase
    {
        /// <summary>On the ground, pre-departure.</summary>
        Takeoff,

        /// <summary>Climbing or descending between phases.</summary>
        Climbing,

        /// <summary>Level cruise at target altitude.</summary>
        Cruise,

        /// <summary>Descending toward a destination.</summary>
        Approach,

        /// <summary>Final approach and touchdown.</summary>
        Landing
    }

    /// <summary>
    /// Monitors real-time flight parameters and fires advisory events when thresholds are breached.
    /// Includes a per-advisory cooldown to avoid message spam.
    /// </summary>
    [DisallowMultipleComponent]
    public class FlightAdvisor : MonoBehaviour
    {
        #region Events

        /// <summary>
        /// Fired when a new flight advisory is generated.
        /// </summary>
        /// <param name="level">Severity of the advisory.</param>
        /// <param name="message">Human-readable advisory text.</param>
        public event Action<AdvisoryLevel, string> OnFlightAdvisory;

        #endregion

        #region Inspector

        [Header("Altitude Thresholds")]
        [Tooltip("Altitude AGL (m) below which a low-altitude caution is issued.")]
        [SerializeField] private float _lowAltitudeCautionM = 300f;

        [Tooltip("Altitude AGL (m) below which a terrain warning is issued.")]
        [SerializeField] private float _terrainWarningM = 100f;

        [Header("Speed Thresholds")]
        [Tooltip("Airspeed (m/s) below which a stall risk caution is issued.")]
        [SerializeField] private float _stallSpeedMs = 40f;

        [Tooltip("Airspeed (m/s) above which an overspeed warning is issued.")]
        [SerializeField] private float _overspeedMs = 250f;

        [Header("Attitude Thresholds")]
        [Tooltip("Bank angle (degrees) above which a high-bank caution is issued.")]
        [SerializeField] private float _highBankDeg = 60f;

        [Tooltip("G-force above which a high-g caution is issued.")]
        [SerializeField] private float _highGForce = 3.5f;

        [Header("Cooldowns")]
        [Tooltip("Minimum seconds between repeated advisories of the same type.")]
        [SerializeField] private float _advisoryCooldownSeconds = 15f;

        #endregion

        #region Private State

        private readonly Dictionary<string, float> _cooldownTimestamps = new Dictionary<string, float>();

        private FlightPhase _currentPhase = FlightPhase.Cruise;

        // Simulated flight state — real implementation pulls from SWEF.Flight module.
        private float _altitudeAGL;
        private float _airspeedMs;
        private float _bankAngleDeg;
        private float _gForce;
        private float _verticalSpeedMs;

        #endregion

        #region Public API

        /// <summary>
        /// Evaluates current flight state and fires appropriate advisory events.
        /// Called periodically by <see cref="AICoPilotManager"/>.
        /// </summary>
        public void EvaluateFlightState()
        {
            ReadFlightState();
            UpdateFlightPhase();

            CheckAltitude();
            CheckAirspeed();
            CheckBankAngle();
            CheckGForce();
        }

        /// <summary>
        /// Updates the simulated flight state values (replace with real Flight module bindings).
        /// </summary>
        /// <param name="altitudeAGL">Altitude above ground level in metres.</param>
        /// <param name="airspeedMs">Indicated airspeed in metres per second.</param>
        /// <param name="bankAngleDeg">Bank angle in degrees.</param>
        /// <param name="gForce">Current G-force load factor.</param>
        /// <param name="verticalSpeedMs">Vertical speed in metres per second (positive = climb).</param>
        public void UpdateFlightState(float altitudeAGL, float airspeedMs, float bankAngleDeg,
                                      float gForce, float verticalSpeedMs)
        {
            _altitudeAGL    = altitudeAGL;
            _airspeedMs     = airspeedMs;
            _bankAngleDeg   = Mathf.Abs(bankAngleDeg);
            _gForce         = gForce;
            _verticalSpeedMs = verticalSpeedMs;
        }

        /// <summary>Sets the current flight phase for context-aware advisories.</summary>
        /// <param name="phase">Active flight phase.</param>
        public void SetFlightPhase(FlightPhase phase) => _currentPhase = phase;

        #endregion

        #region Private Methods

        private void ReadFlightState()
        {
            // In production, pull from SWEF.Flight.FlightStateProvider or similar.
            // Values remain at whatever was last set via UpdateFlightState().
        }

        private void UpdateFlightPhase()
        {
            if (_altitudeAGL < 30f)
                _currentPhase = FlightPhase.Takeoff;
            else if (_verticalSpeedMs > 2f)
                _currentPhase = FlightPhase.Climbing;
            else if (_verticalSpeedMs < -2f)
                _currentPhase = FlightPhase.Approach;
            else
                _currentPhase = FlightPhase.Cruise;
        }

        private void CheckAltitude()
        {
            if (_currentPhase == FlightPhase.Takeoff || _currentPhase == FlightPhase.Landing) return;

            if (_altitudeAGL < _terrainWarningM)
                IssueAdvisory("altitude_terrain", AdvisoryLevel.Critical,
                    $"TERRAIN! Pull up! Altitude {_altitudeAGL:F0} metres AGL.");
            else if (_altitudeAGL < _lowAltitudeCautionM)
                IssueAdvisory("altitude_low", AdvisoryLevel.Caution,
                    $"Low altitude — {_altitudeAGL:F0} metres AGL. Check terrain clearance.");
        }

        private void CheckAirspeed()
        {
            if (_airspeedMs < _stallSpeedMs && _currentPhase != FlightPhase.Takeoff && _currentPhase != FlightPhase.Landing)
                IssueAdvisory("speed_stall", AdvisoryLevel.Warning,
                    $"Stall risk — airspeed {_airspeedMs * 1.944f:F0} knots. Increase power or lower nose.");

            if (_airspeedMs > _overspeedMs)
                IssueAdvisory("speed_overspeed", AdvisoryLevel.Warning,
                    $"Overspeed — {_airspeedMs * 1.944f:F0} knots. Reduce throttle and extend spoilers.");
        }

        private void CheckBankAngle()
        {
            if (_bankAngleDeg > _highBankDeg)
                IssueAdvisory("attitude_bank", AdvisoryLevel.Caution,
                    $"High bank angle — {_bankAngleDeg:F0}°. Returning to wings-level recommended.");
        }

        private void CheckGForce()
        {
            if (_gForce > _highGForce)
                IssueAdvisory("attitude_gforce", AdvisoryLevel.Caution,
                    $"High G-load — {_gForce:F1}G. Ease back-pressure.");
        }

        private void IssueAdvisory(string key, AdvisoryLevel level, string message)
        {
            if (!CanIssue(key)) return;
            MarkCooldown(key);
            OnFlightAdvisory?.Invoke(level, message);
        }

        private bool CanIssue(string key)
        {
            if (!_cooldownTimestamps.TryGetValue(key, out float lastTime)) return true;
            return Time.time - lastTime >= _advisoryCooldownSeconds;
        }

        private void MarkCooldown(string key) => _cooldownTimestamps[key] = Time.time;

        #endregion
    }
}
