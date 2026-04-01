// RCSController.cs — SWEF Space Station & Orbital Docking System
using UnityEngine;

namespace SWEF.SpaceStation
{
    /// <summary>
    /// Reaction Control System (RCS) for 6-DOF maneuvering in zero-G.
    /// Maps input axes to translation and rotation forces on a Rigidbody.
    /// Optionally consumes fuel from <c>FuelManager</c> when
    /// <c>SWEF_FUEL_AVAILABLE</c> is defined.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RCSController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [SerializeField] private SpaceStationConfig _config;

        [Tooltip("Dead-zone applied to all input axes before computing thrust.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _deadZone = 0.05f;

        [Tooltip("Sensitivity multiplier applied after the dead-zone.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _sensitivity = 1f;

        [Tooltip("Fuel consumed per Newton of thrust per second.")]
        [Range(0f, 1f)]
        [SerializeField] private float _fuelConsumptionRate = 0.01f;

        // ── Public read-only ──────────────────────────────────────────────────────

        /// <summary>Total fuel consumed this session (arbitrary units).</summary>
        public float TotalFuelConsumed { get; private set; }

        // ── Private references ────────────────────────────────────────────────────

        private Rigidbody _rb;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies a translational impulse in the given local-space direction.
        /// </summary>
        /// <param name="direction">Normalized direction in local space.</param>
        /// <param name="force">Force magnitude in Newtons.</param>
        public void Translate(Vector3 direction, float force)
        {
            if (_rb == null) return;

            float appliedForce = ApplyDeadZoneAndSensitivity(force);
            Vector3 worldForce = transform.TransformDirection(direction.normalized) * appliedForce;
            _rb.AddForce(worldForce, ForceMode.Force);
            ConsumeFuel(appliedForce * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Applies a rotational torque around the given local-space axis.
        /// </summary>
        /// <param name="axis">Local-space rotation axis (normalized).</param>
        /// <param name="torque">Torque magnitude in Newton-metres.</param>
        public void Rotate(Vector3 axis, float torque)
        {
            if (_rb == null) return;

            float appliedTorque = ApplyDeadZoneAndSensitivity(torque);
            Vector3 worldAxis = transform.TransformDirection(axis.normalized);
            _rb.AddTorque(worldAxis * appliedTorque, ForceMode.Force);
            ConsumeFuel(Mathf.Abs(appliedTorque) * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Processes a full 6-DOF input vector and applies forces/torques accordingly.
        /// </summary>
        /// <param name="translateInput">XYZ translation input (−1 to +1 per axis).</param>
        /// <param name="rotateInput">XYZ rotation input pitch/yaw/roll (−1 to +1 per axis).</param>
        public void ProcessInput(Vector3 translateInput, Vector3 rotateInput)
        {
            float rcsForce = _config != null ? _config.rcsForce : 100f;

            if (translateInput.x != 0f) Translate(Vector3.right,   translateInput.x * rcsForce);
            if (translateInput.y != 0f) Translate(Vector3.up,      translateInput.y * rcsForce);
            if (translateInput.z != 0f) Translate(Vector3.forward,  translateInput.z * rcsForce);

            if (rotateInput.x != 0f) Rotate(Vector3.right,   rotateInput.x * rcsForce);
            if (rotateInput.y != 0f) Rotate(Vector3.up,      rotateInput.y * rcsForce);
            if (rotateInput.z != 0f) Rotate(Vector3.forward, rotateInput.z * rcsForce);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private float ApplyDeadZoneAndSensitivity(float input)
        {
            float abs = Mathf.Abs(input);
            if (abs < _deadZone) return 0f;
            // Remap so dead-zone edge maps to 0
            float remapped = (abs - _deadZone) / (1f - _deadZone);
            return Mathf.Sign(input) * remapped * _sensitivity;
        }

        private void ConsumeFuel(float amount)
        {
            if (amount <= 0f) return;
            float consumed = amount * _fuelConsumptionRate;
            TotalFuelConsumed += consumed;

#if SWEF_FUEL_AVAILABLE
            SWEF.Fuel.FuelManager.Instance?.ConsumeFuel(consumed);
#endif
        }
    }
}
