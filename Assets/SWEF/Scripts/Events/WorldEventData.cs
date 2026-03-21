using System;
using UnityEngine;

namespace SWEF.Events
{
    /// <summary>
    /// Categorises the type of world event.
    /// </summary>
    public enum WorldEventType
    {
        Aurora,
        MeteorShower,
        AirShow,
        Migration,
        RareWeather,
        Festival,
        SpaceDebris,
        Custom
    }

    /// <summary>
    /// Constrains when a recurring event may spawn relative to the in-game season.
    /// </summary>
    public enum SeasonalConstraint
    {
        Any,
        Spring,
        Summer,
        Autumn,
        Winter
    }

    /// <summary>
    /// Defines the geographic region in which a world event may spawn.
    /// </summary>
    [Serializable]
    public struct SpawnRegion
    {
        /// <summary>World-space centre of the spawn area.</summary>
        public Vector3 center;

        /// <summary>Spawn radius in world units around <see cref="center"/>.</summary>
        public float radius;
    }

    /// <summary>
    /// ScriptableObject template that describes a single world event type.
    /// Create via <c>Assets → Create → SWEF → Events → World Event Data</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Events/World Event Data", fileName = "NewWorldEvent")]
    public class WorldEventData : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        [Header("Identity")]
        /// <summary>Unique identifier used internally and for achievement linking.</summary>
        [SerializeField] public string eventId;

        /// <summary>Human-readable display name shown in the UI.</summary>
        [SerializeField] public string eventName;

        /// <summary>Localization key for the event description (SWEF.Localization).</summary>
        [SerializeField] public string descriptionKey;

        /// <summary>Category of this world event.</summary>
        [SerializeField] public WorldEventType eventType = WorldEventType.Custom;

        // ── Duration ──────────────────────────────────────────────────────────────
        [Header("Duration")]
        /// <summary>Minimum time in real-world minutes the event remains active.</summary>
        [SerializeField] public float minDurationMinutes = 5f;

        /// <summary>Maximum time in real-world minutes the event remains active.</summary>
        [SerializeField] public float maxDurationMinutes = 15f;

        // ── Spawn rules ───────────────────────────────────────────────────────────
        [Header("Spawn Rules")]
        /// <summary>Geographic area in which the event instance spawns.</summary>
        [SerializeField] public SpawnRegion spawnRegion;

        /// <summary>Per-evaluation probability [0, 1] that this event is triggered.</summary>
        [SerializeField, Range(0f, 1f)] public float spawnProbability = 0.1f;

        /// <summary>Maximum number of simultaneously active instances of this event.</summary>
        [SerializeField] public int maxConcurrentInstances = 1;

        /// <summary>Altitude range (x = min, y = max) in metres at which the player can participate.</summary>
        [SerializeField] public Vector2 requiredAltitudeRange = new Vector2(0f, 10000f);

        // ── Visuals ───────────────────────────────────────────────────────────────
        [Header("Visuals")]
        /// <summary>Resources-relative path to the prefab used to represent this event visually.</summary>
        [SerializeField] public string visualPrefabPath;

        // ── Rewards ───────────────────────────────────────────────────────────────
        [Header("Rewards")]
        /// <summary>Experience points granted to the player on successful event participation.</summary>
        [SerializeField] public int xpReward = 100;

        /// <summary>Optional achievement ID to unlock when the player completes this event.</summary>
        [SerializeField] public string achievementId;

        // ── Recurrence ────────────────────────────────────────────────────────────
        [Header("Recurrence")]
        /// <summary>Whether this event may fire more than once per session.</summary>
        [SerializeField] public bool isRecurring = true;

        /// <summary>Minimum real-world minutes that must pass between successive spawns of this event.</summary>
        [SerializeField] public float cooldownMinutes = 30f;

        /// <summary>Season during which this event may appear. <see cref="SeasonalConstraint.Any"/> removes the restriction.</summary>
        [SerializeField] public SeasonalConstraint seasonalConstraint = SeasonalConstraint.Any;
    }
}
