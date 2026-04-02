// CompetitiveRacingConfig.cs — SWEF Competitive Racing & Time Trial System (Phase 88)

namespace SWEF.CompetitiveRacing
{
    /// <summary>
    /// Compile-time constants shared across the Competitive Racing system.
    /// All distances are in metres, all times in seconds.
    /// </summary>
    public static class CompetitiveRacingConfig
    {
        // ── Race Session ──────────────────────────────────────────────────────────

        /// <summary>Default countdown duration (seconds) before race start.</summary>
        public const float DefaultCountdownDuration = 3f;

        /// <summary>Maximum checkpoints allowed per course.</summary>
        public const int MaxCheckpointsPerCourse = 200;

        /// <summary>Default checkpoint trigger capture radius (metres).</summary>
        public const float DefaultCheckpointTriggerRadius = 150f;

        // ── Race Modes ────────────────────────────────────────────────────────────

        /// <summary>Interval (seconds) between elimination events in Elimination mode.</summary>
        public const float EliminationInterval = 30f;

        /// <summary>
        /// Minimum valid lap time (seconds).  Results faster than this are flagged
        /// as potentially cheated.
        /// </summary>
        public const float MinimumLapTimeAntiCheat = 10f;

        // ── Season ────────────────────────────────────────────────────────────────

        /// <summary>Duration of one competition season in days.</summary>
        public const int SeasonDurationDays = 90;

        // ── Leaderboard ───────────────────────────────────────────────────────────

        /// <summary>Number of entries returned per leaderboard page.</summary>
        public const int LeaderboardPageSize = 50;

        /// <summary>Maximum number of ghost replays stored locally per course.</summary>
        public const int MaxGhostReplaysPerCourse = 3;

        // ── Course Validation ─────────────────────────────────────────────────────

        /// <summary>Minimum number of checkpoints required for a valid course.</summary>
        public const int MinCheckpointsRequired = 3;

        /// <summary>Minimum total course distance (metres).</summary>
        public const float MinCourseDistanceMeters = 500f;

        /// <summary>Maximum total course distance (metres).</summary>
        public const float MaxCourseDistanceMeters = 500000f;

        /// <summary>
        /// Distance threshold (metres) within which the last checkpoint is
        /// considered to connect back to the first (loop detection).
        /// </summary>
        public const float LoopDetectionThreshold = 300f;

        /// <summary>Minimum distance (metres) between two checkpoint gates.</summary>
        public const float MinCheckpointSpacing = 50f;

        // ── Medal Time Estimation ─────────────────────────────────────────────────

        /// <summary>Gold medal threshold as a fraction of estimated finish time.</summary>
        public const float GoldTimeMultiplier   = 0.85f;

        /// <summary>Silver medal threshold as a fraction of estimated finish time.</summary>
        public const float SilverTimeMultiplier = 1.00f;

        /// <summary>Bronze medal threshold as a fraction of estimated finish time.</summary>
        public const float BronzeTimeMultiplier = 1.20f;

        // ── Ghost Racing ──────────────────────────────────────────────────────────

        /// <summary>Maximum number of ghosts that can race simultaneously.</summary>
        public const int MaxSimultaneousGhosts = 3;

        // ── Wrong-Way Detection ───────────────────────────────────────────────────

        /// <summary>
        /// Dot product threshold below which the player's velocity is considered
        /// "wrong way" relative to the course direction.
        /// </summary>
        public const float WrongWayDotThreshold = -0.5f;

        // ── HUD ───────────────────────────────────────────────────────────────────

        /// <summary>Duration (seconds) for which a race alert banner is displayed.</summary>
        public const float AlertBannerDuration = 3f;

        /// <summary>Duration (seconds) the race HUD remains visible after the race finishes before auto-hiding.</summary>
        public const float RaceFinishHUDLingerDuration = 5f;
    }
}
