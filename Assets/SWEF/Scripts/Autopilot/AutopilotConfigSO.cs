// Phase 72 — Autopilot & Cruise Control System
// Assets/SWEF/Scripts/Autopilot/AutopilotConfigSO.cs
using UnityEngine;

namespace SWEF.Autopilot
{
    /// <summary>
    /// Designer-tunable ScriptableObject that stores all autopilot PID gains,
    /// limit values, approach parameters, and smoothing settings.
    /// Assign to <see cref="AutopilotController.configAsset"/> in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Autopilot/AutopilotConfig", fileName = "AutopilotConfig")]
    public class AutopilotConfigSO : ScriptableObject
    {
        [Header("Altitude Hold — PID Gains")]
        [Tooltip("Proportional gain for altitude PID.")]
        public float altitudeKp = 0.5f;
        [Tooltip("Integral gain for altitude PID (anti-windup active).")]
        public float altitudeKi = 0.05f;
        [Tooltip("Derivative gain for altitude PID.")]
        public float altitudeKd = 0.2f;

        [Header("Heading Hold — PID Gains")]
        [Tooltip("Proportional gain for heading PID.")]
        public float headingKp = 1.0f;
        [Tooltip("Integral gain for heading PID.")]
        public float headingKi = 0.01f;
        [Tooltip("Derivative gain for heading PID.")]
        public float headingKd = 0.3f;

        [Header("Speed Hold — PID Gains")]
        [Tooltip("Proportional gain for speed PID.")]
        public float speedKp = 0.8f;
        [Tooltip("Integral gain for speed PID.")]
        public float speedKi = 0.1f;
        [Tooltip("Derivative gain for speed PID.")]
        public float speedKd = 0.15f;

        [Header("Limits")]
        [Tooltip("Maximum pitch correction angle (degrees).")]
        public float maxPitchAngle = 30f;
        [Tooltip("Maximum bank angle for heading turns (degrees).")]
        public float maxBankAngle = 45f;
        [Tooltip("Maximum throttle change rate per second (0–1).")]
        public float maxThrottleRate = 0.5f;
        [Tooltip("Altitude tolerance before PID engages (metres).")]
        public float altitudeTolerance = 10f;
        [Tooltip("Heading tolerance before PID engages (degrees).")]
        public float headingTolerance = 2f;
        [Tooltip("Speed tolerance before PID engages (km/h).")]
        public float speedTolerance = 5f;

        [Header("Approach Parameters")]
        [Tooltip("Glideslope descent angle (degrees).")]
        public float glideslopeAngle = 3f;
        [Tooltip("Height AGL at which to transition from Glideslope to Flare (metres).")]
        public float flareAltitude = 15f;
        [Tooltip("Target approach speed (km/h).")]
        public float approachSpeed = 120f;

        [Header("Smoothing")]
        [Tooltip("Input smooth time (seconds).")]
        public float inputSmoothTime = 0.3f;
        [Tooltip("Mode transition blend time (seconds).")]
        public float modeTransitionTime = 1.0f;

        [Header("Safety Thresholds")]
        [Tooltip("Autopilot auto-disengages below this altitude AGL (metres).")]
        public float safeAltitudeMinAgl = 50f;
        [Tooltip("Speed below which stall protection activates (km/h).")]
        public float stallSpeedKmh = 150f;
        [Tooltip("Fuel level fraction at which low-fuel warning is triggered (0–1).")]
        public float fuelLowThreshold = 0.1f;

        /// <summary>
        /// Converts this ScriptableObject into a plain <see cref="AutopilotConfig"/> instance.
        /// Used by <see cref="AutopilotController"/> to apply designer values at runtime.
        /// </summary>
        public AutopilotConfig ToRuntimeConfig()
        {
            return new AutopilotConfig
            {
                altitudeKp        = altitudeKp,
                altitudeKi        = altitudeKi,
                altitudeKd        = altitudeKd,
                headingKp         = headingKp,
                headingKi         = headingKi,
                headingKd         = headingKd,
                speedKp           = speedKp,
                speedKi           = speedKi,
                speedKd           = speedKd,
                maxPitchAngle     = maxPitchAngle,
                maxBankAngle      = maxBankAngle,
                maxThrottleRate   = maxThrottleRate,
                altitudeTolerance = altitudeTolerance,
                headingTolerance  = headingTolerance,
                speedTolerance    = speedTolerance,
                glideslopeAngle   = glideslopeAngle,
                flareAltitude     = flareAltitude,
                approachSpeed     = approachSpeed,
                inputSmoothTime   = inputSmoothTime,
                modeTransitionTime = modeTransitionTime,
                safeAltitudeMinAgl = safeAltitudeMinAgl,
                stallSpeedKmh     = stallSpeedKmh,
                fuelLowThreshold  = fuelLowThreshold
            };
        }

        /// <summary>
        /// Factory method: creates a new <see cref="AutopilotConfigSO"/> with all default values.
        /// Used as a runtime fallback when no asset is assigned.
        /// </summary>
        public static AutopilotConfigSO CreateDefault()
        {
            return CreateInstance<AutopilotConfigSO>();
        }
    }
}
