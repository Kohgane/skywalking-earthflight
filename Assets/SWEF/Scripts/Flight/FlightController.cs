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
            Vector3 targetVel = transform.forward * (maxSpeed * Throttle01);
            _vel = ExpSmoothing.ExpLerp(_vel, targetVel, accelSmoothing, dt);
            transform.position += _vel * dt;

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
