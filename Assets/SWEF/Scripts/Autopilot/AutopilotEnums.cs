// Phase 72 — Autopilot & Cruise Control System
// Assets/SWEF/Scripts/Autopilot/AutopilotEnums.cs
namespace SWEF.Autopilot
{
    /// <summary>Active autopilot engagement mode.</summary>
    public enum AutopilotMode
    {
        Off,
        AltitudeHold,
        HeadingHold,
        SpeedHold,
        RouteFollow,
        ApproachAssist,
        FullAutopilot   // altitude + heading + speed combined
    }

    /// <summary>Current state of the cruise-control system.</summary>
    public enum CruiseControlState
    {
        Disabled,
        Accelerating,
        Maintaining,
        Decelerating
    }

    /// <summary>Phases of an ILS/visual approach to a runway.</summary>
    public enum ApproachPhase
    {
        None,
        Intercept,      // turning toward runway
        Glideslope,     // descending on approach path
        Flare,          // leveling off just before touchdown
        Rollout         // on runway, decelerating
    }

    /// <summary>Runtime-tunable configuration for the AutopilotController.</summary>
    [System.Serializable]
    public class AutopilotConfig
    {
        // PID gains for altitude hold
        public float altitudeKp = 0.5f;
        public float altitudeKi = 0.05f;
        public float altitudeKd = 0.2f;

        // PID gains for heading hold
        public float headingKp = 1.0f;
        public float headingKi = 0.01f;
        public float headingKd = 0.3f;

        // PID gains for speed hold
        public float speedKp = 0.8f;
        public float speedKi = 0.1f;
        public float speedKd = 0.15f;

        // Limits
        public float maxPitchAngle = 30f;       // degrees
        public float maxBankAngle = 45f;         // degrees
        public float maxThrottleRate = 0.5f;     // per second
        public float altitudeTolerance = 10f;    // metres
        public float headingTolerance = 2f;      // degrees
        public float speedTolerance = 5f;        // km/h

        // Approach
        public float glideslopeAngle = 3f;       // degrees
        public float flareAltitude = 15f;        // metres AGL
        public float approachSpeed = 120f;       // km/h

        // Smoothing
        public float inputSmoothTime = 0.3f;     // seconds
        public float modeTransitionTime = 1.0f;  // seconds for smooth blend

        // Safety
        public float safeAltitudeMinAgl = 50f;   // metres AGL — auto-disengage below this
        public float stallSpeedKmh = 150f;        // km/h — stall protection threshold
        public float fuelLowThreshold = 0.1f;    // 0–1 fraction — fuel warning level
    }
}
