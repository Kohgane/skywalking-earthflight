// AdvancedPhotographyEnums.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)

namespace SWEF.AdvancedPhotography
{
    /// <summary>Autonomous flight modes for the drone camera.</summary>
    public enum DroneFlightMode
    {
        /// <summary>Player-controlled free flight.</summary>
        FreeRoam,
        /// <summary>Drone orbits a fixed world-space target point.</summary>
        Orbit,
        /// <summary>Drone performs a straight flyby past the target.</summary>
        Flyby,
        /// <summary>Drone follows the player aircraft with a configurable offset.</summary>
        Follow,
        /// <summary>Drone flies a pre-authored <see cref="DroneFlightPath"/>.</summary>
        Waypoint,
        /// <summary>Drone keeps a subject centred while translating freely.</summary>
        Tracking,
        /// <summary>Slow, cinematic dolly / crane movements.</summary>
        Cinematic,
        /// <summary>Drone autonomously returns to the player's position.</summary>
        ReturnHome
    }

    /// <summary>Photographic composition rules used for AI analysis and guidance overlays.</summary>
    public enum CompositionRule
    {
        /// <summary>Subject placed along the rule-of-thirds grid lines / intersections.</summary>
        RuleOfThirds,
        /// <summary>Subject follows the golden-ratio spiral.</summary>
        GoldenRatio,
        /// <summary>Mirror symmetry across the horizontal or vertical axis.</summary>
        Symmetry,
        /// <summary>Converging lines lead the viewer's eye to the subject.</summary>
        LeadingLines,
        /// <summary>A secondary element frames the primary subject.</summary>
        FrameWithinFrame,
        /// <summary>Key elements placed along diagonal axes.</summary>
        DiagonalMethod,
        /// <summary>Subject centred with equal negative space around it.</summary>
        CenterWeighted
    }

    /// <summary>Primary photographic subject categories used in challenges and metadata tagging.</summary>
    public enum PhotoSubject
    {
        Landscape,
        Landmark,
        Aircraft,
        Wildlife,
        Weather,
        Celestial,
        Urban,
        Nature,
        Action,
        Abstract
    }

    /// <summary>Time-window category for photo challenges.</summary>
    public enum ChallengeCategory
    {
        /// <summary>Resets every 24 hours.</summary>
        Daily,
        /// <summary>Resets every 7 days.</summary>
        Weekly,
        /// <summary>Tied to the current real-world season.</summary>
        Seasonal,
        /// <summary>Limited-time special event.</summary>
        Special,
        /// <summary>Community-created or community-voted challenge.</summary>
        Community
    }

    /// <summary>Star rating applied to a captured photo.</summary>
    public enum PhotoRating
    {
        OneStar   = 1,
        TwoStar   = 2,
        ThreeStar = 3,
        FourStar  = 4,
        FiveStar  = 5
    }

    /// <summary>Panorama capture style.</summary>
    public enum PanoramaType
    {
        /// <summary>Multi-frame horizontal sweep.</summary>
        Horizontal,
        /// <summary>Multi-frame vertical sweep.</summary>
        Vertical,
        /// <summary>6-face cubemap → equirectangular 360° panorama.</summary>
        Full360,
        /// <summary>Stereographic projection from a Full360 capture.</summary>
        LittlePlanet
    }

    /// <summary>Trigger mode for timelapse frame capture.</summary>
    public enum TimelapseMode
    {
        /// <summary>Capture a frame every N seconds.</summary>
        TimeInterval,
        /// <summary>Capture a frame every N metres of player movement.</summary>
        DistanceInterval,
        /// <summary>Capture at fixed solar-angle intervals using TimeOfDayController.</summary>
        SunTracking,
        /// <summary>Capture whenever the weather condition changes.</summary>
        WeatherChange,
        /// <summary>Capture continuously across one full day/night cycle.</summary>
        DayNightCycle
    }

    /// <summary>Level of AI assistance provided by the composition assistant.</summary>
    public enum AIAssistLevel
    {
        /// <summary>No assistance; overlays and suggestions disabled.</summary>
        Off,
        /// <summary>On-screen text suggestions only; no auto-framing.</summary>
        Suggestions,
        /// <summary>Overlay guides shown; camera auto-adjusts FOV only.</summary>
        AutoFrame,
        /// <summary>System automatically repositions camera for best composition.</summary>
        FullAuto
    }
}
