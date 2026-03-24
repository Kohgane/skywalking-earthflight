// MissionConfig.cs — SWEF Mission Briefing & Objective System (Phase 70)

namespace SWEF.Mission
{
    /// <summary>
    /// Phase 70 — Static configuration constants for the Mission Briefing &amp; Objective system.
    ///
    /// <para>All tunable defaults live here so individual components stay consistent
    /// without duplicating magic numbers.</para>
    /// </summary>
    public static class MissionConfig
    {
        // ── Mission Limits ────────────────────────────────────────────────────

        /// <summary>Maximum number of simultaneously active missions (always 1 in SWEF).</summary>
        public const int MaxActiveMissions = 1;

        // ── Checkpoint ────────────────────────────────────────────────────────

        /// <summary>Default trigger radius in metres for a checkpoint.</summary>
        public const float CheckpointDefaultRadius = 30f;

        /// <summary>Distance in metres beyond which checkpoint ring fades out.</summary>
        public const float CheckpointRingFadeDistance = 2000f;

        // ── Rating Thresholds (0 – 1) ─────────────────────────────────────────

        /// <summary>Minimum score ratio for an S rating.</summary>
        public const float SThreshold = 0.95f;

        /// <summary>Minimum score ratio for an A rating.</summary>
        public const float AThreshold = 0.85f;

        /// <summary>Minimum score ratio for a B rating.</summary>
        public const float BThreshold = 0.70f;

        /// <summary>Minimum score ratio for a C rating.</summary>
        public const float CThreshold = 0.55f;

        /// <summary>Minimum score ratio for a D rating.</summary>
        public const float DThreshold = 0.40f;

        // ── Rating Reward Multipliers ─────────────────────────────────────────

        /// <summary>Reward multiplier for an S rating.</summary>
        public const float SMultiplier = 2f;

        /// <summary>Reward multiplier for an A rating.</summary>
        public const float AMultiplier = 1.5f;

        /// <summary>Reward multiplier for a B rating.</summary>
        public const float BMultiplier = 1.2f;

        /// <summary>Reward multiplier for a C rating.</summary>
        public const float CMultiplier = 1f;

        /// <summary>Reward multiplier for a D rating.</summary>
        public const float DMultiplier = 0.8f;

        /// <summary>Reward multiplier for an F rating.</summary>
        public const float FMultiplier = 0.5f;

        // ── Scoring ───────────────────────────────────────────────────────────

        /// <summary>Score bonus granted per second the player finishes under the par time.</summary>
        public const int TimeBonusPerSecond = 10;

        /// <summary>Flat score bonus awarded for each completed optional objective.</summary>
        public const int OptionalObjectiveBonus = 500;

        // ── Briefing UI ───────────────────────────────────────────────────────

        /// <summary>Characters revealed per second during the briefing typewriter effect.</summary>
        public const float BriefingTypewriterSpeed = 30f;
    }
}
