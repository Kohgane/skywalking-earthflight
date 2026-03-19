using System;
using UnityEngine;

namespace SWEF.Flight
{
    /// <summary>
    /// Main integration point for advanced flight physics.
    /// Sits alongside <see cref="FlightController"/> and enhances it with
    /// aerodynamic drag, lift, gravity, and orbital mechanics each FixedUpdate.
    ///
    /// Set <see cref="enableAdvancedPhysics"/> to <c>false</c> or
    /// <see cref="physicsBlendFactor"/> to 0 to revert to the original kinematic behaviour.
    /// </summary>
    [RequireComponent(typeof(FlightController))]
    public class FlightPhysicsIntegrator : MonoBehaviour
    {
        // ── Inspector references ─────────────────────────────────────────────
        [Header("Dependencies")]
        [SerializeField] private AltitudeController altitudeController;
        [SerializeField] private AeroPhysicsModel   aeroModel;
        [SerializeField] private OrbitalMechanics   orbitalMechanics;
        [SerializeField] private StallWarningSystem  stallWarning;

        [Header("Advanced Physics Toggle")]
        [Tooltip("Disable to fall back to the original kinematic flight behaviour.")]
        [SerializeField] private bool enableAdvancedPhysics = true;
        [Tooltip("0 = pure kinematic, 1 = full physics. Allows a gradual transition.")]
        [SerializeField] [Range(0f, 1f)] private float physicsBlendFactor = 1f;

        [Header("Speed Limits")]
        [Tooltip("Maximum speed while in atmosphere / sub-orbital (m/s).")]
        [SerializeField] private float maxAtmosphericSpeed = 8000f;
        [Tooltip("Maximum speed at / beyond escape velocity (m/s).")]
        [SerializeField] private float maxEscapeSpeed      = 11200f;

        // ── Cached component reference ───────────────────────────────────────
        private FlightController _flight;

        // ── Runtime state ────────────────────────────────────────────────────
        private Vector3    _prevVelocity;
        private AeroState  _lastAero;
        private OrbitState _lastOrbit = OrbitState.Atmospheric;

        // ── Constants ────────────────────────────────────────────────────────
        private const float G0 = 9.81f;

        // ── Public event ─────────────────────────────────────────────────────
        /// <summary>
        /// Fired every physics tick with a full snapshot of the vehicle's physics state.
        /// Subscribe for HUD updates or telemetry recording.
        /// </summary>
        public event Action<FlightPhysicsSnapshot> OnPhysicsSnapshot;

        // ─────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _flight = GetComponent<FlightController>();

            // Auto-locate dependencies on the same GameObject if not wired in Inspector
            if (aeroModel        == null) aeroModel        = GetComponent<AeroPhysicsModel>();
            if (orbitalMechanics == null) orbitalMechanics = GetComponent<OrbitalMechanics>();
            if (stallWarning     == null) stallWarning      = GetComponent<StallWarningSystem>();
            if (altitudeController == null)
                altitudeController = GetComponentInChildren<AltitudeController>();
        }

        private void FixedUpdate()
        {
            if (!enableAdvancedPhysics || physicsBlendFactor <= 0f) return;
            if (_flight == null) return;

            float dt  = Time.fixedDeltaTime;
            float alt = altitudeController != null
                ? altitudeController.CurrentAltitudeMeters
                : transform.position.y;

            Vector3 velocity = _flight.Velocity;
            Vector3 forward  = _flight.Forward;
            float   speed    = velocity.magnitude;

            // ── 1. Evaluate atmosphere ────────────────────────────────────────
            _lastAero = aeroModel != null
                ? aeroModel.Evaluate(velocity, forward, alt)
                : new AeroState(0f, G0, 0f, 0f, 0f, true, false, false, 340f, alt);

            // ── 2. Evaluate orbital state ─────────────────────────────────────
            if (orbitalMechanics != null)
            {
                orbitalMechanics.Evaluate(alt, speed);
                _lastOrbit = orbitalMechanics.CurrentOrbitState;
            }

            // ── 3. Calculate net accelerations (m/s²) ────────────────────────
            float aoa = AeroPhysicsModel.CalculateAoA(velocity, forward);

            // CalculateDrag/CalculateLift return force in Newtons.
            // We treat the craft mass as 1 kg (a deliberate game-feel design choice
            // so forces map directly to m/s² without introducing a mass parameter
            // that would require careful tuning).
            Vector3 dragAccel    = aeroModel != null
                ? aeroModel.CalculateDrag(velocity, alt)
                : Vector3.zero;
            Vector3 liftAccel    = aeroModel != null
                ? aeroModel.CalculateLift(velocity, alt, aoa)
                : Vector3.zero;
            Vector3 gravityAccel = _lastAero.IsInAtmosphere
                ? Vector3.down * _lastAero.Gravity
                : (orbitalMechanics != null
                    ? orbitalMechanics.GetGravitationalAcceleration(alt)
                    : Vector3.down * _lastAero.Gravity);

            Vector3 netAcceleration = dragAccel + liftAccel + gravityAccel;

            // ── 4. Blend and apply ────────────────────────────────────────────
            _flight.ApplyExternalAcceleration(netAcceleration * physicsBlendFactor);

            // ── 5. Smooth speed clamping ──────────────────────────────────────
            // Apply a damping deceleration when speed exceeds the limit rather
            // than an instantaneous velocity clamp to keep behaviour stable.
            float maxSpd = (_lastOrbit == OrbitState.Escape)
                ? maxEscapeSpeed
                : maxAtmosphericSpeed;
            Vector3 vel = _flight.Velocity;
            float   spd = vel.magnitude;
            if (spd > maxSpd && spd > 0f)
            {
                // Decelerate toward the limit smoothly over one physics step
                float excess = spd - maxSpd;
                _flight.ApplyExternalAcceleration(-vel.normalized * (excess / dt));
            }

            // ── 6. Compute G-force ────────────────────────────────────────────
            float gForce = ComputeGForce(velocity, _prevVelocity, dt);
            _prevVelocity = velocity;

            // ── 7. Stall warning ──────────────────────────────────────────────
            if (stallWarning != null)
                stallWarning.Evaluate(_lastAero, gForce, speed);

            // ── 8. Emit snapshot ──────────────────────────────────────────────
            if (OnPhysicsSnapshot != null)
            {
                float thrustPct  = aeroModel != null
                    ? (_flight.Throttle01 * aeroModel.GetMaxThrust(alt)) /
                      Mathf.Max(aeroModel.GetMaxThrust(0f), 1f)
                    : _flight.Throttle01;

                float weight = _lastAero.Gravity;   // m/s² (per-unit-mass)
                float liftW  = weight > 0f ? liftAccel.magnitude / weight : 0f;

                var snapshot = new FlightPhysicsSnapshot(
                    aero:             _lastAero,
                    orbit:            _lastOrbit,
                    netForce:         netAcceleration,   // N·kg⁻¹ = m/s²
                    velocity:         velocity,
                    thrustPercent:    thrustPct,
                    liftToWeightRatio: liftW,
                    gForce:           gForce
                );
                OnPhysicsSnapshot.Invoke(snapshot);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static float ComputeGForce(Vector3 current, Vector3 prev, float dt)
        {
            if (dt <= 0f) return 1f;
            Vector3 accel = (current - prev) / dt;
            // Include 1G felt from resisting gravity
            float netAccel = (accel + Vector3.up * G0).magnitude;
            return netAccel / G0;
        }
    }
}
