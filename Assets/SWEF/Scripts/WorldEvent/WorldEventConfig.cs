// WorldEventConfig.cs — SWEF Dynamic Event & World Quest System (Phase 64)
namespace SWEF.WorldEvent
{
    /// <summary>
    /// Compile-time constants shared across the Dynamic Event &amp; World Quest system.
    /// Override runtime behaviour by editing <see cref="WorldEventManager"/> inspector
    /// fields; these constants serve as sensible defaults.
    /// </summary>
    public static class WorldEventConfig
    {
        /// <summary>Maximum number of world events that can be active simultaneously.</summary>
        public const int MaxConcurrentEvents = 3;

        /// <summary>Seconds between each automatic spawn-eligibility check.</summary>
        public const float EventCheckInterval = 30f;

        /// <summary>Probability [0, 1] that a spawn attempt produces a new event per check.</summary>
        public const float BaseSpawnChance = 0.4f;

        /// <summary>Minimum world-space distance from the player at which events may spawn.</summary>
        public const float MinEventDistance = 500f;

        /// <summary>Maximum world-space distance from the player at which events may spawn.</summary>
        public const float MaxEventDistance = 10000f;

        /// <summary>Height in world units above the event position at which the visual beacon pillar terminates.</summary>
        public const float EventBeaconHeight = 500f;

        /// <summary>World-space radius within which the player receives a nearby-event notification.</summary>
        public const float NearbyEventNotificationRange = 2000f;
    }
}
