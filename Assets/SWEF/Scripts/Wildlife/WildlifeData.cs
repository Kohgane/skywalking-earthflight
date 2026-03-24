using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    // ─── Enumerations ────────────────────────────────────────────────────────────

    /// <summary>Classification of wildlife species into ecological groups.</summary>
    public enum WildlifeCategory
    {
        Bird,
        Raptor,
        Seabird,
        Waterfowl,
        MigratoryBird,
        MarineMammal,
        Fish,
        LandMammal,
        Insect,
        Mythical
    }

    /// <summary>Behavior state for wildlife AI.</summary>
    public enum WildlifeBehavior
    {
        Idle,
        Roaming,
        Feeding,
        Migrating,
        Fleeing,
        Flocking,
        Circling,
        Diving,
        Surfacing,
        Sleeping
    }

    /// <summary>Threat level from aircraft proximity.</summary>
    public enum WildlifeThreatLevel
    {
        None,
        Aware,
        Alarmed,
        Fleeing,
        Panicked
    }

    /// <summary>Spawn biome preference for species placement.</summary>
    public enum SpawnBiome
    {
        Ocean,
        Coast,
        Lake,
        River,
        Forest,
        Grassland,
        Desert,
        Mountain,
        Arctic,
        Tropical,
        Urban,
        Wetland
    }

    /// <summary>Circadian activity pattern of a species.</summary>
    public enum ActivityPattern
    {
        Diurnal,
        Nocturnal,
        Crepuscular,
        AllDay
    }

    /// <summary>Formation type used by bird flocks.</summary>
    public enum FormationType
    {
        VFormation,
        Murmuration,
        SoaringCircle,
        LineFormation,
        Scatter
    }

    // ─── Data Classes ─────────────────────────────────────────────────────────────

    /// <summary>Design-time specification for a single wildlife species.</summary>
    [Serializable]
    public class WildlifeSpecies
    {
        [Tooltip("Unique identifier, e.g. \"bald_eagle\".")]
        public string speciesId = string.Empty;

        [Tooltip("Localization key for display name.")]
        public string displayNameKey = string.Empty;

        [Tooltip("Localization key for journal description.")]
        public string descriptionKey = string.Empty;

        [Tooltip("Ecological category of this species.")]
        public WildlifeCategory category = WildlifeCategory.Bird;

        [Tooltip("Biomes where this species can spawn.")]
        public SpawnBiome[] preferredBiomes = new SpawnBiome[0];

        [Tooltip("Circadian activity pattern.")]
        public ActivityPattern activityPattern = ActivityPattern.Diurnal;

        [Tooltip("Minimum altitude (metres AGL) for spawning.")]
        public float minAltitude = 0f;

        [Tooltip("Maximum altitude (metres AGL) for spawning.")]
        public float maxAltitude = 500f;

        [Tooltip("Cruising speed in m/s.")]
        public float baseSpeed = 10f;

        [Tooltip("Flee speed in m/s when evading aircraft.")]
        public float fleeSpeed = 20f;

        [Tooltip("Distance in metres at which this species starts fleeing.")]
        public float fleeDistance = 150f;

        [Tooltip("Distance in metres at which this species becomes aware of the aircraft.")]
        public float awareDistance = 400f;

        [Tooltip("Minimum group size at spawn.")]
        public int minGroupSize = 1;

        [Tooltip("Maximum group size at spawn.")]
        public int maxGroupSize = 10;

        [Tooltip("Relative spawn probability weighting.")]
        public float spawnWeight = 1f;

        [Tooltip("Whether this species follows seasonal migration paths.")]
        public bool isMigratory = false;

        [Tooltip("Month indices (0–11) during which migration is active.")]
        public int[] migratorySeason = new int[0];

        [Tooltip("Rarity tier: 0 = common, 1 = rare, 2 = legendary.")]
        public float rarityTier = 0f;
    }

    /// <summary>Runtime state for an active wildlife group.</summary>
    [Serializable]
    public class WildlifeGroupState
    {
        [Tooltip("Unique runtime identifier for this group.")]
        public string groupId = string.Empty;

        [Tooltip("Species that makes up this group.")]
        public WildlifeSpecies species;

        [Tooltip("World-space centre of the group.")]
        public Vector3 centerPosition;

        [Tooltip("Current group velocity vector.")]
        public Vector3 groupVelocity;

        [Tooltip("Active behavior state.")]
        public WildlifeBehavior currentBehavior = WildlifeBehavior.Roaming;

        [Tooltip("Current threat level from nearby aircraft.")]
        public WildlifeThreatLevel threatLevel = WildlifeThreatLevel.None;

        [Tooltip("Number of individuals in the group.")]
        public int memberCount = 1;

        [Tooltip("Game time at which this group was spawned (Time.time).")]
        public float spawnTime;

        [Tooltip("Maximum lifetime in seconds before this group despawns.")]
        public float lifetime = 300f;

        [Tooltip("Whether the player has already discovered this group.")]
        public bool isDiscovered = false;
    }

    /// <summary>Immutable record of a single wildlife encounter logged by the player.</summary>
    [Serializable]
    public class WildlifeEncounterRecord
    {
        [Tooltip("Species identifier.")]
        public string speciesId = string.Empty;

        [Tooltip("Group identifier.")]
        public string groupId = string.Empty;

        [Tooltip("World position of the encounter.")]
        public Vector3 encounterPosition;

        [Tooltip("Aircraft altitude at encounter time (metres AGL).")]
        public float encounterAltitude;

        [Tooltip("Game time of the encounter (Time.time).")]
        public float encounterTime;

        [Tooltip("Category of the encountered species.")]
        public WildlifeCategory category;

        [Tooltip("Number of individuals in the encountered group.")]
        public int groupSize;

        [Tooltip("Whether the player photographed this encounter.")]
        public bool wasPhotographed;

        [Tooltip("Closest approach distance in metres.")]
        public float closestApproach;
    }

    /// <summary>Runtime performance and tuning configuration for the wildlife system.</summary>
    [Serializable]
    public class WildlifeConfig
    {
        [Tooltip("Maximum number of simultaneously active wildlife groups.")]
        [Range(1, 50)]
        public int maxActiveGroups = 15;

        [Tooltip("Maximum total individual wildlife entities visible at once.")]
        [Range(1, 500)]
        public int maxIndividualsTotal = 200;

        [Tooltip("Spawn ring radius in metres from the player.")]
        public float spawnRadius = 2000f;

        [Tooltip("Despawn distance in metres from the player.")]
        public float despawnRadius = 3000f;

        [Tooltip("Seconds between spawn attempts.")]
        public float spawnInterval = 5f;

        [Tooltip("Seconds between flock AI ticks.")]
        public float flockUpdateRate = 0.1f;

        [Tooltip("Collision distance in metres for bird strike detection.")]
        public float birdStrikeDistance = 3f;

        [Tooltip("Minimum seconds between journal encounter entries for the same species.")]
        public float detectionReportCooldown = 10f;

        [Tooltip("Whether bird strike collisions are enabled.")]
        public bool enableBirdStrikes = true;

        [Tooltip("Whether seasonal migration patterns are enabled.")]
        public bool enableMigrationPatterns = true;

        [Tooltip("Quality scale multiplier — reduce on low-end hardware.")]
        [Range(0.1f, 1f)]
        public float qualityScaleMultiplier = 1f;
    }

    /// <summary>Tunable boid weights and radii for flocking simulation.</summary>
    [Serializable]
    public class FlockParameters
    {
        [Tooltip("Weight for separation rule (avoid crowding).")]
        public float separationWeight = 1.5f;

        [Tooltip("Weight for alignment rule (match velocity).")]
        public float alignmentWeight = 1.0f;

        [Tooltip("Weight for cohesion rule (move toward center).")]
        public float cohesionWeight = 1.0f;

        [Tooltip("Radius for separation rule in metres.")]
        public float separationRadius = 5f;

        [Tooltip("Radius for alignment rule in metres.")]
        public float alignmentRadius = 15f;

        [Tooltip("Radius for cohesion rule in metres.")]
        public float cohesionRadius = 25f;

        [Tooltip("Maximum steering force magnitude.")]
        public float maxSteerForce = 3f;

        [Tooltip("Weight for obstacle avoidance steering.")]
        public float obstacleAvoidanceWeight = 2f;

        [Tooltip("Weight for terrain following.")]
        public float terrainFollowWeight = 0.5f;
    }
}
