// LandingDetector.cs — SWEF Landing & Airport System (Phase 68)
using System;
using UnityEngine;

namespace SWEF.Landing
{
    /// <summary>
    /// Phase 68 — Detects and evaluates landing attempts.
    ///
    /// <para>Drives a state machine through
    /// <c>InFlight → Approaching → OnFinal → Flaring → Touchdown → Rolling → Stopped</c>,
    /// performs ground-contact raycasts, scores the landing, and fires events that
    /// <see cref="LandingUI"/> and external systems can subscribe to.</para>
    /// </summary>
    public class LandingDetector : MonoBehaviour
    {
        #region Inspector

        [Header("Landing Detector — Detection")]
        [Tooltip("Vertical speed threshold (m/s) used to register a touchdown.")]
        [SerializeField] private float touchdownSpeedThreshold = 5f;

        [Tooltip("Vertical speed (m/s) above which a landing is classified as hard.")]
        [SerializeField] private float maxSafeTouchdownSpeed = LandingConfig.MaxSafeTouchdownSpeed;

        [Tooltip("Vertical speed (m/s) above which a crash is recorded.")]
        [SerializeField] private float maxSurvivableTouchdownSpeed = LandingConfig.MaxSurvivableTouchdownSpeed;

        [Tooltip("Downward raycast distance (m) for ground contact detection.")]
        [SerializeField] private float groundContactDistance = 2f;

        [Tooltip("Layer mask for surfaces that count as ground/runway.")]
        [SerializeField] private LayerMask groundLayers = ~0;

        [Tooltip("Maximum lateral deviation (m) from the runway centreline to count as on-runway.")]
        [SerializeField] private float lateralDeviationMax = 15f;

        [Header("Landing Detector — Approach Thresholds")]
        [Tooltip("Distance (m) from the airport at which the Approaching state activates.")]
        [SerializeField] private float approachRange = 5000f;

        [Tooltip("AGL altitude (m) below which the Flaring state activates.")]
        [SerializeField] private float flareAltitude = LandingConfig.FlareAltitude;

        #endregion

        #region Public State

        /// <summary>Current phase of the landing sequence.</summary>
        public LandingState CurrentState { get; private set; } = LandingState.InFlight;

        /// <summary><c>true</c> when the aircraft is confirmed on a runway surface.</summary>
        public bool IsOnRunway { get; private set; }

        /// <summary>The runway being approached or landed on; <c>null</c> when not set.</summary>
        public RunwayData ActiveRunway { get; private set; }

        /// <summary>Composite landing score 0–100, computed at touchdown.</summary>
        public float LandingScore { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired every time <see cref="CurrentState"/> changes.</summary>
        public event Action<LandingState> OnLandingStateChanged;

        /// <summary>Fired at touchdown; passes the downward vertical speed (m/s, positive value).</summary>
        public event Action<float> OnTouchdown;

        /// <summary>Fired once the landing score is calculated; passes score (0–100) and grade string.</summary>
        public event Action<float, string> OnLandingScored;

        #endregion

        #region Private State

        private Rigidbody _rb;
        private float _touchdownVerticalSpeed;
        private float _lateralDeviationAtTouchdown;
        private float _smoothnessAccumulator;
        private float _smoothnessSamples;
        private float _previousVerticalSpeed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            UpdateGroundContact();
            UpdateStateMachine();
            AccumulateSmoothness();
        }

        #endregion

        #region Public API

        /// <summary>Assigns a runway as the active landing target.</summary>
        /// <param name="runway">The runway the pilot is approaching.</param>
        public void SetActiveRunway(RunwayData runway)
        {
            ActiveRunway = runway;
        }

        /// <summary>Clears the active runway and resets to <see cref="LandingState.InFlight"/>.</summary>
        public void ClearActiveRunway()
        {
            ActiveRunway = null;
            TransitionTo(LandingState.InFlight);
        }

        /// <summary>
        /// Returns the grade string for a given landing score.
        /// </summary>
        /// <param name="score">Score in the range 0–100.</param>
        /// <returns>"Perfect", "Good", "Acceptable", "Hard", or "Crash".</returns>
        public string GetLandingGrade(float score)
        {
            if (score >= LandingConfig.PerfectThreshold)    return "Perfect";
            if (score >= LandingConfig.GoodThreshold)       return "Good";
            if (score >= LandingConfig.AcceptableThreshold) return "Acceptable";
            if (_touchdownVerticalSpeed <= LandingConfig.MaxSurvivableTouchdownSpeed) return "Hard";
            return "Crash";
        }

        #endregion

        #region State Machine

        private void UpdateStateMachine()
        {
            switch (CurrentState)
            {
                case LandingState.InFlight:
                    CheckForApproach();
                    break;

                case LandingState.Approaching:
                    CheckForFinal();
                    break;

                case LandingState.OnFinal:
                    CheckForFlare();
                    break;

                case LandingState.Flaring:
                    CheckForTouchdown();
                    break;

                case LandingState.Touchdown:
                    TransitionTo(LandingState.Rolling);
                    break;

                case LandingState.Rolling:
                    CheckForStop();
                    break;
            }
        }

        private void CheckForApproach()
        {
            if (ActiveRunway == null) return;
            float dist = Vector3.Distance(transform.position, ActiveRunway.thresholdPosition);
            if (dist < approachRange)
                TransitionTo(LandingState.Approaching);
        }

        private void CheckForFinal()
        {
            if (ActiveRunway == null) return;
            float agl = GetAGL();
            if (agl < ActiveRunway.decisionAltitude)
                TransitionTo(LandingState.OnFinal);
        }

        private void CheckForFlare()
        {
            float agl = GetAGL();
            if (agl < flareAltitude)
                TransitionTo(LandingState.Flaring);
        }

        private void CheckForTouchdown()
        {
            if (!IsOnRunway) return;
            float vs = _rb != null ? -_rb.linearVelocity.y : 0f;
            if (vs >= touchdownSpeedThreshold) return; // still airborne with high sink
            RecordTouchdown(vs);
            TransitionTo(LandingState.Touchdown);
        }

        private void CheckForStop()
        {
            if (_rb == null) return;
            if (_rb.linearVelocity.magnitude < 0.5f)
                TransitionTo(LandingState.Stopped);
        }

        private void TransitionTo(LandingState next)
        {
            if (CurrentState == next) return;
            CurrentState = next;
            OnLandingStateChanged?.Invoke(CurrentState);
        }

        #endregion

        #region Ground Contact

        private void UpdateGroundContact()
        {
            bool hit = Physics.Raycast(transform.position, Vector3.down,
                groundContactDistance, groundLayers);
            IsOnRunway = hit && IsWithinRunwayCenterline();
        }

        private bool IsWithinRunwayCenterline()
        {
            if (ActiveRunway == null) return false;
            Vector3 toAircraft = transform.position - ActiveRunway.thresholdPosition;
            Vector3 runwayDir  = ActiveRunway.GetRunwayDirection();
            Vector3 lateral    = toAircraft - Vector3.Dot(toAircraft, runwayDir) * runwayDir;
            return lateral.magnitude <= lateralDeviationMax;
        }

        private float GetAGL()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit,
                    500f, groundLayers))
                return hit.distance;
            return float.MaxValue;
        }

        #endregion

        #region Scoring

        private void AccumulateSmoothness()
        {
            if (_rb == null) return;
            float vs = _rb.linearVelocity.y;
            float jerk = Mathf.Abs(vs - _previousVerticalSpeed) / Time.fixedDeltaTime;
            _smoothnessAccumulator += jerk;
            _smoothnessSamples++;
            _previousVerticalSpeed = vs;
        }

        private void RecordTouchdown(float verticalSpeed)
        {
            _touchdownVerticalSpeed = verticalSpeed;
            _lateralDeviationAtTouchdown = ActiveRunway != null
                ? GetLateralDeviation()
                : lateralDeviationMax;

            OnTouchdown?.Invoke(_touchdownVerticalSpeed);
            CalculateAndFireScore();
        }

        private float GetLateralDeviation()
        {
            if (ActiveRunway == null) return lateralDeviationMax;
            Vector3 toAircraft = transform.position - ActiveRunway.thresholdPosition;
            Vector3 runwayDir  = ActiveRunway.GetRunwayDirection();
            Vector3 lateral    = toAircraft - Vector3.Dot(toAircraft, runwayDir) * runwayDir;
            return lateral.magnitude;
        }

        private void CalculateAndFireScore()
        {
            // Centreline score (0–100): perfect = on centreline
            float centerlineScore = Mathf.Clamp01(
                1f - _lateralDeviationAtTouchdown / lateralDeviationMax) * 100f;

            // Vertical speed score (0–100): perfect = below maxSafe
            float vsScore = Mathf.Clamp01(
                1f - (_touchdownVerticalSpeed / maxSurvivableTouchdownSpeed)) * 100f;
            if (_touchdownVerticalSpeed > maxSurvivableTouchdownSpeed) vsScore = 0f;

            // Smoothness score (0–100): lower average jerk = smoother
            float avgJerk   = _smoothnessSamples > 0 ? _smoothnessAccumulator / _smoothnessSamples : 0f;
            float smoothScore = Mathf.Clamp01(1f - avgJerk / 50f) * 100f;

            LandingScore = centerlineScore * LandingConfig.CenterlineWeight
                         + vsScore         * LandingConfig.VerticalSpeedWeight
                         + smoothScore     * LandingConfig.SmoothnessWeight;

            LandingScore = Mathf.Clamp(LandingScore, 0f, 100f);
            string grade = GetLandingGrade(LandingScore);
            OnLandingScored?.Invoke(LandingScore, grade);

            // Reset accumulators for next approach
            _smoothnessAccumulator = 0f;
            _smoothnessSamples     = 0f;
        }

        #endregion
    }
}
