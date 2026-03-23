using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    #region Enumerations

    /// <summary>Biological kingdom classification for an animal species.</summary>
    public enum AnimalKingdom
    {
        Mammal,
        Bird,
        Fish,
        Reptile,
        Amphibian,
        Insect
    }

    /// <summary>Current behavioral state of an animal or group.</summary>
    public enum AnimalBehavior
    {
        Grazing,
        Hunting,
        Migrating,
        Resting,
        Flying,
        Swimming,
        Schooling,
        Nesting,
        Fleeing
    }

    /// <summary>Biome or habitat zone in which an animal can naturally appear.</summary>
    public enum BiomeHabitat
    {
        Savanna,
        Forest,
        Desert,
        Arctic,
        Ocean,
        River,
        Mountain,
        Jungle,
        Wetland,
        Grassland,
        Coral,
        DeepSea,
        Coast,
        Urban
    }

    /// <summary>Body-size tier from insect to blue whale.</summary>
    public enum AnimalSize
    {
        Tiny,
        Small,
        Medium,
        Large,
        Huge,
        Colossal
    }

    /// <summary>Spatial formation used by a flying flock or swimming school.</summary>
    public enum FlockFormation
    {
        Vee,
        Line,
        Cluster,
        Spiral,
        Random,
        Circle
    }

    /// <summary>Circadian activity pattern of a species.</summary>
    public enum TimeActivity
    {
        Diurnal,
        Nocturnal,
        Crepuscular,
        AllDay
    }

    /// <summary>Rarity tier that controls spawn probability weighting.</summary>
    public enum AnimalRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    /// <summary>Seasonal period used for migration route activation.</summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    #endregion

    #region Species & Group Definitions

    /// <summary>
    /// Design-time specification for a single animal species.
    /// </summary>
    [Serializable]
    public class AnimalSpecies
    {
        [Tooltip("Display name of the species.")]
        public string speciesName = "Unknown Animal";

        [Tooltip("Biological kingdom this species belongs to.")]
        public AnimalKingdom kingdom = AnimalKingdom.Mammal;

        [Tooltip("Body-size tier for LOD and audio distance scaling.")]
        public AnimalSize size = AnimalSize.Medium;

        [Tooltip("List of biomes/habitats where this species naturally appears.")]
        public List<BiomeHabitat> habitats = new List<BiomeHabitat>();

        [Tooltip("Circadian activity pattern.")]
        public TimeActivity activityPattern = TimeActivity.Diurnal;

        [Tooltip("Base movement speed in world units per second.")]
        public float baseSpeed = 5f;

        [Tooltip("Minimum number of individuals per spawned group.")]
        public int minGroupSize = 1;

        [Tooltip("Maximum number of individuals per spawned group.")]
        public int maxGroupSize = 10;

        [Tooltip("Whether this species is capable of sustained flight.")]
        public bool flightCapable = false;

        [Tooltip("Whether this species can swim.")]
        public bool swimCapable = false;

        [Tooltip("Minimum altitude (world units) at which this species appears.")]
        public float minAltitude = 0f;

        [Tooltip("Maximum altitude (world units) at which this species appears.")]
        public float maxAltitude = 500f;

        [Tooltip("AudioClip reference key used by WildlifeAudioController.")]
        public string soundClipKey = string.Empty;

        [Tooltip("LOD distances: [0]=full anim, [1]=simplified, [2]=static/off.")]
        public float[] lodDistances = { 100f, 500f, 1500f };

        [Tooltip("Prefab reference path (resolved at runtime by WildlifeSpawnSystem).")]
        public string prefabKey = string.Empty;

        [Tooltip("Animator controller reference key.")]
        public string animatorControllerKey = string.Empty;

        [Tooltip("Spawn probability weighting.")]
        public AnimalRarity rarity = AnimalRarity.Common;

        [Tooltip("Short description shown in the Wildlife Codex.")]
        [TextArea(2, 4)]
        public string description = string.Empty;
    }

    /// <summary>
    /// Runtime state for a group of animals moving and behaving together.
    /// </summary>
    [Serializable]
    public class AnimalGroup
    {
        [Tooltip("Species specification shared by all members of this group.")]
        public AnimalSpecies species;

        [Tooltip("Number of active members in this group.")]
        public int memberCount = 1;

        [Tooltip("World-space center of the group's bounding area.")]
        public Vector3 centerPosition;

        [Tooltip("Current normalised movement direction.")]
        public Vector3 movementDirection;

        [Tooltip("Current behavioral state of the group.")]
        public AnimalBehavior currentBehavior = AnimalBehavior.Grazing;

        [Tooltip("Spatial formation currently adopted by the group.")]
        public FlockFormation formation = FlockFormation.Random;

        [Tooltip("Radius of the group's bounding sphere in world units.")]
        public float groupRadius = 10f;

        [Tooltip("World time at which this group was spawned.")]
        public float spawnTime;
    }

    /// <summary>
    /// Defines a seasonal migration path taken by a species.
    /// </summary>
    [Serializable]
    public class MigrationRoute
    {
        [Tooltip("Species that follows this route.")]
        public AnimalSpecies species;

        [Tooltip("Ordered list of world-space waypoints defining the migration path.")]
        public List<Vector3> waypoints = new List<Vector3>();

        [Tooltip("Season during which this route is active.")]
        public Season activeSeason = Season.Spring;

        [Tooltip("Real-world duration of the migration in in-game days.")]
        public float durationDays = 7f;

        [Tooltip("Whether this migration is currently running.")]
        public bool isActive = false;
    }

    /// <summary>
    /// Immutable record of a single wildlife sighting logged by the player.
    /// </summary>
    [Serializable]
    public class WildlifeEncounter
    {
        [Tooltip("Common name of the encountered species.")]
        public string speciesName = string.Empty;

        [Tooltip("World position where the encounter occurred.")]
        public Vector3 position;

        [Tooltip("In-game timestamp of the encounter (Time.time).")]
        public float timestamp;

        [Tooltip("Whether the player photographed this animal during the encounter.")]
        public bool wasPhotographed;

        [Tooltip("Distance between the player and the animal at time of encounter.")]
        public float distanceFromPlayer;
    }

    #endregion

    #region Settings

    /// <summary>
    /// Runtime-configurable parameters for the entire wildlife system.
    /// </summary>
    [Serializable]
    public class WildlifeSettings
    {
        [Tooltip("Maximum number of simultaneously visible individual animals.")]
        [Range(0, 500)]
        public int maxAnimalsVisible = 150;

        [Tooltip("Distance from the player at which animals can spawn.")]
        public float spawnRadius = 2000f;

        [Tooltip("Distance from the player beyond which animals are despawned.")]
        public float despawnRadius = 2500f;

        [Tooltip("Per-LOD distances: [0]=full, [1]=simplified, [2]=static.")]
        public float[] lodDistances = { 150f, 600f, 1800f };

        [Tooltip("Whether marine life is enabled.")]
        public bool enableMarineLife = true;

        [Tooltip("Whether birds are enabled.")]
        public bool enableBirds = true;

        [Tooltip("Whether land animals are enabled.")]
        public bool enableLandAnimals = true;

        [Tooltip("Whether insects and small critters are enabled.")]
        public bool enableInsects = true;

        [Tooltip("Maximum distance at which animal sounds are audible.")]
        public float soundDistance = 300f;

        [Tooltip("Whether encounters are written to the encounter log.")]
        public bool encounterLogEnabled = true;
    }

    #endregion
}
