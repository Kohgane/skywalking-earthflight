// SeaplanePhysics.cs — Phase 117: Advanced Ocean & Maritime System
// Float physics: pontoon buoyancy, step effect, porpoising prevention.
// Namespace: SWEF.OceanSystem

using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Simulates seaplane / float-plane hydrodynamics.
    /// Applies per-pontoon buoyancy, the hull-step planing effect during takeoff run,
    /// and crosswind drift correction on water.
    /// </summary>
    public class SeaplanePhysics : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Pontoons")]
        [SerializeField] private Transform[] pontoonPoints;
        [SerializeField] private float pontoonBuoyancyForce = 8000f;
        [SerializeField] private float pontoonDraftMetres   = 0.25f;

        [Header("Step Effect")]
        [Tooltip("Speed at which the hull steps up onto the plane (knots).")]
        [SerializeField] private float stepSpeedKnots = 25f;
        [Tooltip("Lift force generated when on the step.")]
        [SerializeField] private float stepLiftForce = 5000f;

        [Header("Porpoising")]
        [Tooltip("Pitch damping force to prevent porpoising oscillation.")]
        [SerializeField] private float porpoisingDamping = 3000f;

        [Header("Crosswind")]
        [SerializeField] private float crosswindCorrectionForce = 2000f;

        // ── Private state ─────────────────────────────────────────────────────────

        private Rigidbody _rb;
        private bool      _isOnStep;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Whether the seaplane is currently on the hull step.</summary>
        public bool IsOnStep => _isOnStep;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            ApplyPontoonBuoyancy();
            ApplyStepEffect();
            ApplyPorpoisingDamping();
            ApplyCrosswindCorrection();
        }

        // ── Physics ───────────────────────────────────────────────────────────────

        private void ApplyPontoonBuoyancy()
        {
            if (pontoonPoints == null || _rb == null) return;
            var mgr = OceanSystemManager.Instance;

            foreach (var pt in pontoonPoints)
            {
                if (pt == null) continue;
                float surfaceY = mgr != null
                    ? mgr.GetSurfaceHeight(new Vector2(pt.position.x, pt.position.z))
                    : 0f;

                float depth = surfaceY - (pt.position.y - pontoonDraftMetres);
                if (depth > 0f)
                {
                    float force = Mathf.Clamp(depth * pontoonBuoyancyForce, 0f, pontoonBuoyancyForce);
                    _rb.AddForceAtPosition(Vector3.up * force, pt.position, ForceMode.Force);
                }
            }
        }

        private void ApplyStepEffect()
        {
            if (_rb == null) return;
            float speedKnots = _rb.linearVelocity.magnitude / 0.5144f;
            _isOnStep        = speedKnots >= stepSpeedKnots;

            if (_isOnStep)
            {
                // Reduce stern buoyancy, increase bow lift — represented as forward-pitched lift
                _rb.AddRelativeForce(new Vector3(0f, stepLiftForce, -stepLiftForce * 0.3f), ForceMode.Force);
            }
        }

        private void ApplyPorpoisingDamping()
        {
            if (_rb == null) return;
            // Damp pitch angular velocity to prevent oscillation
            var angVel      = _rb.angularVelocity;
            float pitchRate = Vector3.Dot(angVel, transform.right); // local pitch axis
            _rb.AddRelativeTorque(new Vector3(-pitchRate * porpoisingDamping, 0f, 0f) * Time.fixedDeltaTime, ForceMode.Force);
        }

        private void ApplyCrosswindCorrection()
        {
            if (_rb == null) return;
            // Project velocity onto local right axis to get lateral drift
            float lateralSpeed = Vector3.Dot(_rb.linearVelocity, transform.right);
            _rb.AddForce(-transform.right * lateralSpeed * crosswindCorrectionForce * Time.fixedDeltaTime, ForceMode.Force);
        }
    }
}
