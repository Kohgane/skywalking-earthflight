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

        public float Throttle01 { get; private set; } = 0.25f;

        private Vector3 _vel;

        public void SetThrottle(float t01) => Throttle01 = Mathf.Clamp01(t01);
        public void SetMaxSpeed(float speed) => maxSpeed = Mathf.Clamp(speed, 50f, 500f);

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
        }
    }
}
