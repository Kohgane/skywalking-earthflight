// TouchdownAnalyzer.cs — Phase 120: Precision Landing Challenge System
// Touchdown analysis: exact position, speed, vertical speed, bank angle, crab, G-force.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Analyses the touchdown event to extract precise metrics used
    /// by the scoring engine: position, speed, vertical speed, bank/crab angles,
    /// and G-force at impact.
    /// </summary>
    public class TouchdownAnalyzer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Runway Reference")]
        [SerializeField] private Transform runwayCentreline;
        [SerializeField] private Transform runwayThreshold;
        [SerializeField] private float     runwayHeadingDeg = 90f;

        // ── State ─────────────────────────────────────────────────────────────

        private Vector3 _previousVelocity;
        private bool    _velocityInitialised;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Analyse a touchdown event from the current aircraft transform and
        /// physics state.
        /// </summary>
        /// <param name="aircraftTransform">Aircraft transform at moment of touchdown.</param>
        /// <param name="velocityWorldMetresPerSec">Aircraft velocity in world space (m/s).</param>
        /// <param name="previousVelocity">Velocity one frame before touchdown for G-force calculation.</param>
        /// <returns>Populated <see cref="TouchdownData"/>.</returns>
        public TouchdownData Analyse(Transform aircraftTransform,
                                     Vector3 velocityWorldMetresPerSec,
                                     Vector3 previousVelocity)
        {
            var td = new TouchdownData();

            td.Position = aircraftTransform.position;

            // Speed (m/s → knots)
            float speedMps = new Vector3(velocityWorldMetresPerSec.x, 0f, velocityWorldMetresPerSec.z).magnitude;
            td.SpeedKnots  = speedMps * 1.94384f;

            // Vertical speed (m/s → FPM)
            td.VerticalSpeedFPM = velocityWorldMetresPerSec.y * 196.85f;

            // Bank angle
            td.BankAngleDeg = aircraftTransform.eulerAngles.z;
            if (td.BankAngleDeg > 180f) td.BankAngleDeg -= 360f;

            // Crab angle (heading vs runway heading)
            float aircraftHeading = aircraftTransform.eulerAngles.y;
            td.CrabAngleDeg = Mathf.DeltaAngle(aircraftHeading, runwayHeadingDeg);

            // G-force at impact
            float deltaV = (velocityWorldMetresPerSec.y - previousVelocity.y);
            td.GForce    = 1f + Mathf.Abs(deltaV) / (9.81f * Time.deltaTime);

            // Centreline offset
            if (runwayCentreline != null)
            {
                Vector3 toAircraft = aircraftTransform.position - runwayCentreline.position;
                Vector3 right      = runwayCentreline.right;
                td.CentrelineOffsetMetres = Vector3.Dot(toAircraft, right);
            }

            // Threshold distance
            if (runwayThreshold != null)
            {
                Vector3 toAircraft = aircraftTransform.position - runwayThreshold.position;
                Vector3 fwd        = runwayThreshold.forward;
                td.ThresholdDistanceMetres = Vector3.Dot(toAircraft, fwd);
            }

            return td;
        }

        /// <summary>
        /// Returns a simple G-force estimate from velocity change between frames.
        /// </summary>
        public float EstimateGForce(Vector3 currentVelocity, Vector3 lastVelocity, float dt)
        {
            if (dt <= 0f) return 1f;
            float deltaVY = currentVelocity.y - lastVelocity.y;
            return 1f + Mathf.Abs(deltaVY) / (9.81f * dt);
        }

        private void Update()
        {
            // Track previous velocity each frame for G-force calculation
        }
    }
}
