// ApproachGuidance.cs — SWEF Landing & Airport System (Phase 68)
using System;
using UnityEngine;

namespace SWEF.Landing
{
    /// <summary>
    /// Phase 68 — ILS-style approach guidance system.
    ///
    /// <para>Computes localizer deviation (lateral) and glide slope deviation (vertical)
    /// from the aircraft's current position relative to the active runway and ILS beam.
    /// Provides recommended approach speed and altitude for each distance segment.</para>
    /// </summary>
    public class ApproachGuidance : MonoBehaviour
    {
        #region Inspector

        [Header("Approach Guidance — Configuration")]
        [Tooltip("Type of approach guidance to provide.")]
        [SerializeField] private ApproachType approachType = ApproachType.ILS;

        [Tooltip("Half-width (normalized, 0–1) deviation tolerance for localizer established.")]
        [SerializeField] private float localizerTolerance = 0.3f;

        [Tooltip("Half-width (normalized, 0–1) deviation tolerance for glide slope established.")]
        [SerializeField] private float glideSlopeTolerance = 0.3f;

        [Tooltip("Reference stall speed (m/s) used to calculate recommended approach speed.")]
        [SerializeField] private float stallSpeed = 40f;

        #endregion

        #region Public State

        /// <summary>Runway currently being approached; <c>null</c> if no approach is active.</summary>
        public RunwayData TargetRunway { get; private set; }

        /// <summary>Approach type currently in use.</summary>
        public ApproachType CurrentApproachType => approachType;

        /// <summary>Lateral deviation from the extended centreline, −1 (full left) to +1 (full right), 0 = centred.</summary>
        public float LocalizerDeviation { get; private set; }

        /// <summary>Vertical deviation from the ideal glide slope, −1 (below slope) to +1 (above slope), 0 = on slope.</summary>
        public float GlideSlopeDeviation { get; private set; }

        /// <summary>Horizontal distance to the runway threshold in meters.</summary>
        public float DistanceToThreshold { get; private set; }

        /// <summary>Recommended approach speed (m/s) for the current distance.</summary>
        public float RecommendedSpeed { get; private set; }

        /// <summary>Target altitude (m ASL) on the glide slope at the current distance.</summary>
        public float RecommendedAltitude { get; private set; }

        /// <summary><c>true</c> when both localizer and glide slope are within their respective tolerances.</summary>
        public bool IsEstablished { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired once when both localizer and glide slope come within tolerance.</summary>
        public event Action OnApproachEstablished;

        /// <summary>Fired when the aircraft drifts outside localizer or glide slope tolerances after being established.</summary>
        public event Action OnApproachDeviation;

        #endregion

        #region Private State

        private bool _wasEstablished;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (TargetRunway == null) return;
            ComputeGuidance();
            EvaluateEstablished();
        }

        #endregion

        #region Public API

        /// <summary>Begins approach guidance to the specified runway.</summary>
        /// <param name="runway">The runway to approach.</param>
        public void SetTargetRunway(RunwayData runway)
        {
            TargetRunway = runway;
            _wasEstablished = false;
            IsEstablished   = false;
        }

        /// <summary>Disengages approach guidance and clears all computed values.</summary>
        public void CancelApproach()
        {
            TargetRunway        = null;
            LocalizerDeviation  = 0f;
            GlideSlopeDeviation = 0f;
            DistanceToThreshold = 0f;
            RecommendedSpeed    = 0f;
            RecommendedAltitude = 0f;
            IsEstablished       = false;
            _wasEstablished     = false;
        }

        #endregion

        #region Guidance Computation

        private void ComputeGuidance()
        {
            Vector3 aircraftPos = transform.position;
            Vector3 threshold   = TargetRunway.thresholdPosition;
            Vector3 runwayDir   = TargetRunway.GetRunwayDirection();

            Vector3 toAircraft  = aircraftPos - threshold;

            // ── Distance along runway axis ────────────────────────────────────
            // 'along' is negative when the aircraft is in front of the threshold (on approach).
            // DistanceToThreshold is always a non-negative value.
            float along = Vector3.Dot(toAircraft, runwayDir);
            DistanceToThreshold = Mathf.Max(0f, -along);

            // ── Localizer deviation ───────────────────────────────────────────
            // Cross-track distance; normalised to ±1 over ±localizer beam width (half = 2.5° at 10 NM ≈ 460 m)
            Vector3 lateral      = toAircraft - along * runwayDir;
            float   lateralDist  = Vector3.Dot(lateral, Vector3.Cross(runwayDir, Vector3.up).normalized);
            float   beamHalfWidth = Mathf.Max(1f, DistanceToThreshold * Mathf.Tan(2.5f * Mathf.Deg2Rad));
            LocalizerDeviation   = Mathf.Clamp(lateralDist / beamHalfWidth, -1f, 1f);

            // ── Glide slope deviation ─────────────────────────────────────────
            // Ideal altitude at current distance along glide slope
            float gsAngleRad     = TargetRunway.glideSlopeAngle * Mathf.Deg2Rad;
            float idealAltitude  = threshold.y + DistanceToThreshold * Mathf.Tan(gsAngleRad);
            float actualAltitude = aircraftPos.y;
            float altError       = actualAltitude - idealAltitude;
            float gsBeamHalf     = Mathf.Max(1f, DistanceToThreshold * Mathf.Tan(0.7f * Mathf.Deg2Rad));
            GlideSlopeDeviation  = Mathf.Clamp(altError / gsBeamHalf, -1f, 1f);

            // ── Recommended values ────────────────────────────────────────────
            RecommendedAltitude = idealAltitude;
            RecommendedSpeed    = stallSpeed * LandingConfig.ApproachSpeedFactor;
        }

        private void EvaluateEstablished()
        {
            bool nowEstablished = Mathf.Abs(LocalizerDeviation)  <= localizerTolerance
                               && Mathf.Abs(GlideSlopeDeviation) <= glideSlopeTolerance;

            if (nowEstablished && !_wasEstablished)
            {
                IsEstablished   = true;
                _wasEstablished = true;
                OnApproachEstablished?.Invoke();
            }
            else if (!nowEstablished && _wasEstablished)
            {
                IsEstablished   = false;
                _wasEstablished = false;
                OnApproachDeviation?.Invoke();
            }
            else
            {
                IsEstablished = nowEstablished;
            }
        }

        #endregion
    }
}
