// AdvancedPhotographyConfig.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)

namespace SWEF.AdvancedPhotography
{
    /// <summary>
    /// Compile-time constants shared across the Advanced Photography system.
    /// All distances are in metres, times in seconds, angles in degrees.
    /// </summary>
    public static class AdvancedPhotographyConfig
    {
        // ── Drone Flight ──────────────────────────────────────────────────────────

        /// <summary>Maximum distance (metres) the drone may travel from the player.</summary>
        public const float DroneMaxRange = 500f;

        /// <summary>Maximum altitude (metres AGL) the drone may reach.</summary>
        public const float DroneMaxAltitude = 200f;

        /// <summary>Total battery life (seconds) for a single drone flight.</summary>
        public const float DroneBatteryDuration = 300f;

        /// <summary>Battery percentage below which auto-return is triggered.</summary>
        public const float DroneLowBatteryThreshold = 0.10f;

        /// <summary>Default radius (metres) for Orbit flight mode.</summary>
        public const float DroneOrbitDefaultRadius = 50f;

        /// <summary>Default angular speed (degrees/second) in Orbit mode.</summary>
        public const float DroneOrbitDefaultSpeed = 30f;

        /// <summary>Lookahead distance (metres) for drone collision avoidance raycasts.</summary>
        public const float DroneCollisionLookahead = 5f;

        // ── AI Composition ────────────────────────────────────────────────────────

        /// <summary>Score (0–1) above which the composition is considered good.</summary>
        public const float AICompositionGoodThreshold = 0.65f;

        /// <summary>Score (0–1) above which the composition is considered excellent.</summary>
        public const float AICompositionExcellentThreshold = 0.85f;

        /// <summary>Minimum score delta to trigger a new suggestion update.</summary>
        public const float AICompositionScoreDeltaThreshold = 0.05f;

        /// <summary>Interval (seconds) between automatic composition re-evaluations.</summary>
        public const float AICompositionUpdateInterval = 0.5f;

        // ── Photo Challenges ──────────────────────────────────────────────────────

        /// <summary>Duration (hours) of a Daily challenge window.</summary>
        public const float ChallengeDurationDailyHours = 24f;

        /// <summary>Duration (hours) of a Weekly challenge window.</summary>
        public const float ChallengeDurationWeeklyHours = 168f;

        // ── Panorama ──────────────────────────────────────────────────────────────

        /// <summary>Default resolution (pixels) per cubemap face for Full360 captures.</summary>
        public const int PanoramaDefaultFaceResolution = 2048;

        /// <summary>Overlap percentage between adjacent horizontal/vertical frames.</summary>
        public const float PanoramaOverlapPercent = 30f;

        /// <summary>Number of horizontal steps for a Horizontal panorama sweep.</summary>
        public const int PanoramaHorizontalSteps = 8;

        /// <summary>Number of vertical steps for a Vertical panorama sweep.</summary>
        public const int PanoramaVerticalSteps = 5;

        // ── Timelapse ─────────────────────────────────────────────────────────────

        /// <summary>Minimum interval (seconds) between timelapse frames.</summary>
        public const float TimelapseMinInterval = 0.5f;

        /// <summary>Maximum interval (seconds) between timelapse frames.</summary>
        public const float TimelapseMaxInterval = 300f;

        /// <summary>Default time interval (seconds) between timelapse frames.</summary>
        public const float TimelapseDefaultInterval = 5f;

        /// <summary>Default distance interval (metres) between distance-trigger frames.</summary>
        public const float TimelapseDefaultDistanceInterval = 100f;

        /// <summary>Maximum number of frames stored in a single timelapse buffer.</summary>
        public const int TimelapseMaxFrameCount = 600;

        // ── Photo Spot Discovery ──────────────────────────────────────────────────

        /// <summary>Radius (metres) within which a nearby photo spot is discoverable.</summary>
        public const float PhotoSpotDiscoveryRadius = 5000f;

        /// <summary>Radius (metres) within which the player's position triggers discovery.</summary>
        public const float PhotoSpotTriggerRadius = 200f;

        // ── Contest ───────────────────────────────────────────────────────────────

        /// <summary>Maximum entries returned per contest leaderboard page.</summary>
        public const int ContestLeaderboardPageSize = 50;

        /// <summary>Maximum photos a single player may submit per contest.</summary>
        public const int ContestMaxSubmissionsPerPlayer = 3;
    }
}
