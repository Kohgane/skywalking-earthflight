using UnityEngine;

namespace SWEF.Util
{
    /// <summary>
    /// Exponential smoothing utilities for frame-rate-independent interpolation.
    /// </summary>
    public static class ExpSmoothing
    {
        /// <summary>
        /// Exponential lerp: approaches target smoothly regardless of frame rate.
        /// Higher k = faster convergence.
        /// </summary>
        public static float ExpLerp(float current, float target, float k, float dt)
        {
            float t = 1f - Mathf.Exp(-k * dt);
            return Mathf.LerpUnclamped(current, target, t);
        }

        /// <summary>
        /// Vector3 version of ExpLerp.
        /// </summary>
        public static Vector3 ExpLerp(Vector3 current, Vector3 target, float k, float dt)
        {
            float t = 1f - Mathf.Exp(-k * dt);
            return Vector3.LerpUnclamped(current, target, t);
        }

        /// <summary>
        /// Convert angle from 0..360 range to -180..180 range.
        /// </summary>
        public static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            return a;
        }
    }
}
