// Phase 73 — Flight Formation Display & Airshow System
// Assets/SWEF/Scripts/Airshow/AirshowEnums.cs
namespace SWEF.Airshow
{
    /// <summary>The type of airshow performance.</summary>
    public enum AirshowType
    {
        FreeStyle,          // No script, pure improv
        Choreographed,      // Follows a scripted routine
        FormationDisplay,   // Focus on tight formation flying
        AeroBatic,          // Solo or group aerobatic maneuvers
        SmokeTrailArt,      // Drawing shapes with smoke
        Flyby               // Low-pass flybys over a venue
    }

    /// <summary>Individual maneuver types available to performers.</summary>
    public enum ManeuverType
    {
        StraightAndLevel,
        BarrelRoll,
        LoopTheLoop,
        Immelmann,
        SplitS,
        CubanEight,
        Hammerhead,
        TailSlide,
        KnifeEdge,
        FormationBreak,     // Break and rejoin
        DeltaPass,          // Delta formation flyby
        BombBurst,          // Starburst break from center
        CrossOver,          // Two groups cross paths
        HeartShape,         // Smoke trail heart
        SpiralClimb,
        InvertedFlight,
        SynchroMirror,      // Mirrored maneuvers
        DiamondRoll         // Rolling while in diamond formation
    }

    /// <summary>State of an airshow performance lifecycle.</summary>
    public enum AirshowState
    {
        Idle,
        Briefing,       // Pre-show preparation
        Countdown,      // 3-2-1 countdown
        Performing,     // Active performance
        Intermission,   // Break between acts
        Finale,         // Final sequence
        Completed,      // Show finished
        Aborted         // Emergency cancel
    }

    /// <summary>Smoke trail color presets for performers.</summary>
    public enum SmokeColor
    {
        White,
        Red,
        Blue,
        Green,
        Yellow,
        Orange,
        Purple,
        Pink,
        Black,
        Custom          // Player-defined RGB
    }

    /// <summary>Spectator camera mode options.</summary>
    public enum SpectatorCameraMode
    {
        GroundLevel,        // From audience perspective
        TowerCam,           // High vantage point
        ChaseCamera,        // Following the formation
        CockpitCam,         // From inside a performer
        BirdsEye,           // Top-down view
        Cinematic,          // Auto-switching dramatic angles
        FreeRoam,           // Player-controlled spectator
        SlowMotion          // Slow-mo replay with dramatic angles
    }

    /// <summary>Score rating for performance quality.</summary>
    public enum PerformanceRating
    {
        Perfect,        // 95-100%
        Excellent,      // 85-94%
        Great,          // 70-84%
        Good,           // 50-69%
        NeedsWork       // Below 50%
    }

    /// <summary>Runtime-tunable configuration for the airshow system.</summary>
    [System.Serializable]
    public class AirshowConfig
    {
        public float countdownDuration = 5f;            // seconds
        public float intermissionDuration = 15f;        // seconds
        public float maxShowDuration = 600f;            // 10 minutes max
        public float maneuverTimingTolerance = 1.5f;    // seconds tolerance for timing score
        public float positionTolerance = 50f;           // metres tolerance for position score
        public float smokeDensity = 1.0f;               // 0-2 multiplier
        public float smokeTrailLifetime = 30f;          // seconds before fade
        public float audienceReactionDelay = 0.5f;      // seconds delay for crowd response
        public int maxPerformers = 8;                   // max aircraft in airshow
        public float venueRadius = 2000f;               // metres, airshow venue area
    }
}
