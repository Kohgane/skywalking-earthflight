using System;
using UnityEngine;
using SWEF.Util;

namespace SWEF.Flight
{
    /// <summary>
    /// Core flight physics: kinematic (transform-based, no Rigidbody).
    /// Receives normalized yaw/pitch/roll inputs from TouchInputRouter.
    /// Comfort mode: auto-levels roll, clamps pitch, smooths acceleration.
    /// </summary>
    public class FlightController : MonoBehaviour
    {
        [Header("Speed")]
        [SerializeField] private float maxSpeed = 250f;      // m/s experiential
        [SerializeField] private float accelSmoothing = 6f;

        [Header("Rotation")]
        [SerializeField] private float yawRate = 70f;   // deg/s
        [SerializeField] private float pitchRate = 50f;
        [SerializeField] private float rollRate = 90f;

        [Header("Comfort")]
        public bool comfortMode = true;
        [SerializeField] private float autoLevelStrength = 2.0f;
        [SerializeField] private float pitchClamp = 70f; // degrees

        [Header("Phase 16 — Haptic Thresholds")]
        [SerializeField] private float boostThrottleThreshold = 0.9f;
        [SerializeField] private float stallSpeedThreshold    = 10f;

        public float Throttle01 { get; private set; } = 0.25f;

        // ── Phase 24 — Advanced Physics additions ────────────────────────────
        /// <summary>Current velocity vector in metres per second (world space).</summary>
        public Vector3 Velocity => _vel;

        /// <summary>Convenience accessor for the craft's forward direction.</summary>
        public Vector3 Forward => transform.forward;

        /// <summary>Pending external acceleration (m/s²) to apply on next Step().</summary>
        private Vector3 _externalAccel;

        // ── Phase 32 — Weather additions ─────────────────────────────────────────
        /// <summary>
        /// Pending external position-space force (m/s) applied by weather effects
        /// each frame.  Consumed and reset in <see cref="Step"/>.
        /// </summary>
        private Vector3 _externalForce;

        /// <summary>
        /// Multiplier applied to the effective max speed by weather systems
        /// (e.g. 0.5 = half speed in dense fog).  Default 1.0 = no effect.
        /// Reset to 1.0 when weather clears.
        /// </summary>
        public float ExternalDragMultiplier { get; set; } = 1f;

        private Vector3 _vel;

        // ── Phase 16 — Events ────────────────────────────────────────────────
        /// <summary>Raised when throttle crosses the boost threshold upward.</summary>
        public event Action OnBoostStarted;

        /// <summary>Raised when throttle drops below the boost threshold.</summary>
        public event Action OnBoostEnded;

        /// <summary>Raised when speed drops below the stall threshold.</summary>
        public event Action OnStallWarning;

        private bool _boostActive;
        private bool _stallActive;

        /// <summary>Current speed in meters per second.</summary>
        public float CurrentSpeedMps => _vel.magnitude;

        public void SetThrottle(float t01)
        {
            float prev = Throttle01;
            Throttle01 = Mathf.Clamp01(t01);

            // Phase 16 — boost event
            bool nowBoosting = Throttle01 >= boostThrottleThreshold;
            if (nowBoosting && !_boostActive)
            {
                _boostActive = true;
                OnBoostStarted?.Invoke();
            }
            else if (!nowBoosting && _boostActive)
            {
                _boostActive = false;
                OnBoostEnded?.Invoke();
            }
        }

        public void SetMaxSpeed(float speed) => maxSpeed = Mathf.Clamp(speed, 50f, 500f);

        /// <summary>
        /// Queues an external acceleration (m/s²) to be integrated into velocity
        /// on the next call to <see cref="Step"/>. Called by
        /// <see cref="FlightPhysicsIntegrator"/> each FixedUpdate.
        /// </summary>
        /// <param name="accel">Acceleration vector in world space (m/s²).</param>
        public void ApplyExternalAcceleration(Vector3 accel)
        {
            _externalAccel += accel;
        }

        /// <summary>
        /// Queues a world-space force offset (m/s) to be applied as a direct
        /// position displacement on the next call to <see cref="Step"/>.
        /// Used by <c>WeatherFlightModifier</c> to apply wind push.
        /// </summary>
        /// <param name="force">Force vector in world space (m/s).</param>
        public void ApplyExternalForce(Vector3 force)
        {
            _externalForce += force;
        }

        /// <summary>
        /// Feeds XR controller input directly into the flight system,
        /// bypassing touch input. Called each frame by <c>XRInputAdapter</c>.
        /// </summary>
        /// <param name="throttle">Normalised throttle (0–1).</param>
        /// <param name="yaw">Normalised yaw input (−1 to 1).</param>
        /// <param name="pitch">Normalised pitch input (−1 to 1).</param>
        /// <param name="roll">Normalised roll input (−1 to 1).</param>
        public void SetInputFromXR(float throttle, float yaw, float pitch, float roll)
        {
            SetThrottle(throttle);
            Step(yaw, pitch, roll);
        }

        /// <summary>
        /// Call once per frame with normalized (-1..1) inputs.
        /// </summary>
        public void Step(float yawInput, float pitchInput, float rollInput)
        {
            float dt = Time.deltaTime;

            // --- Rotation ---
            float yaw   = yawInput   * yawRate   * dt;
            float pitch = pitchInput * pitchRate  * dt;
            float roll  = rollInput  * rollRate   * dt;

            transform.Rotate(Vector3.up,      yaw,   Space.Self);
            transform.Rotate(Vector3.right,   pitch, Space.Self);
            transform.Rotate(Vector3.forward, -roll,  Space.Self);

            // Clamp pitch to prevent full loops
            var e = transform.localEulerAngles;
            float x = ExpSmoothing.NormalizeAngle(e.x);
            x = Mathf.Clamp(x, -pitchClamp, pitchClamp);

            // Comfort: auto-level roll toward 0
            float z = ExpSmoothing.NormalizeAngle(e.z);
            if (comfortMode)
                z = ExpSmoothing.ExpLerp(z, 0f, autoLevelStrength, dt);

            transform.localEulerAngles = new Vector3(x, e.y, z);

            // --- Translation ---
            Vector3 targetVel = transform.forward * (maxSpeed * ExternalDragMultiplier * Throttle01);
            _vel = ExpSmoothing.ExpLerp(_vel, targetVel, accelSmoothing, dt);

            // Phase 24 — integrate any external acceleration (aerodynamics, gravity, etc.)
            _vel += _externalAccel * dt;
            _externalAccel = Vector3.zero;

            transform.position += _vel * dt;

            // Phase 32 — apply external wind force as direct position displacement
            transform.position += _externalForce * dt;
            _externalForce      = Vector3.zero;

            // Phase 16 — stall warning
            bool stalling = CurrentSpeedMps < stallSpeedThreshold && Throttle01 > 0.01f;
            if (stalling && !_stallActive)
            {
                _stallActive = true;
                OnStallWarning?.Invoke();
            }
            else if (!stalling)
            {
                _stallActive = false;
            }
        }
    }
}
