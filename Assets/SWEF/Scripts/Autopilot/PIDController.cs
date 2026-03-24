// Phase 72 — Autopilot & Cruise Control System
// Assets/SWEF/Scripts/Autopilot/PIDController.cs
namespace SWEF.Autopilot
{
    /// <summary>
    /// Generic PID controller reusable across all autopilot axes.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class PIDController
    {
        /// <summary>Proportional gain.</summary>
        public float Kp { get; private set; }

        /// <summary>Integral gain.</summary>
        public float Ki { get; private set; }

        /// <summary>Derivative gain.</summary>
        public float Kd { get; private set; }

        /// <summary>Anti-windup clamp on the integral accumulator.</summary>
        public float IntegralMax = 10f;

        /// <summary>Lower clamp on the output signal.</summary>
        public float OutputMin = -1f;

        /// <summary>Upper clamp on the output signal.</summary>
        public float OutputMax = 1f;

        private float _integral;
        private float _previousError;
        private bool _firstUpdate = true;

        /// <param name="kp">Proportional gain.</param>
        /// <param name="ki">Integral gain.</param>
        /// <param name="kd">Derivative gain.</param>
        public PIDController(float kp, float ki, float kd)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
        }

        /// <summary>
        /// Compute the PID control signal for the given error this frame.
        /// Safe when <paramref name="deltaTime"/> is zero (derivative term is skipped).
        /// </summary>
        /// <param name="error">Current error (setpoint − measured).</param>
        /// <param name="deltaTime">Time elapsed since last call (seconds).</param>
        /// <returns>Clamped control output in [OutputMin, OutputMax].</returns>
        public float Update(float error, float deltaTime)
        {
            if (deltaTime <= 0f)
                return UnityEngine.Mathf.Clamp(Kp * error, OutputMin, OutputMax);

            // Integral with anti-windup
            _integral += error * deltaTime;
            _integral = UnityEngine.Mathf.Clamp(_integral, -IntegralMax, IntegralMax);

            // Derivative (skip on first call to avoid spike)
            float derivative = 0f;
            if (!_firstUpdate)
                derivative = (error - _previousError) / deltaTime;

            _firstUpdate = false;
            _previousError = error;

            float output = Kp * error + Ki * _integral + Kd * derivative;
            return UnityEngine.Mathf.Clamp(output, OutputMin, OutputMax);
        }

        /// <summary>Reset the integral accumulator and previous-error state.</summary>
        public void Reset()
        {
            _integral = 0f;
            _previousError = 0f;
            _firstUpdate = true;
        }

        /// <summary>Update the PID gains at runtime (also resets state).</summary>
        public void SetGains(float kp, float ki, float kd)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            Reset();
        }
    }
}
