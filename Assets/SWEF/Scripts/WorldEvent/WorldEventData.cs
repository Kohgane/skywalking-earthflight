// WorldEventData.cs — SWEF Dynamic Event & World Quest System (Phase 64)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.WorldEvent
{
    /// <summary>
    /// ScriptableObject template that describes a single world event type.
    /// Create via <c>Assets → Create → SWEF → WorldEvent → World Event Data</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/WorldEvent/World Event Data", fileName = "NewWorldEvent")]
    public class WorldEventData : ScriptableObject
    {
        // ── Identity ─────────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Unique identifier used internally and for cooldown tracking.")]
        /// <summary>Unique identifier used internally and for cooldown tracking.</summary>
        public string eventId;

        [Tooltip("Human-readable display name shown in the UI (may be a localisation key).")]
        /// <summary>Human-readable display name shown in the UI (may be a localisation key).</summary>
        public string eventName;

        [Tooltip("Short descriptive text shown in the event popup.")]
        /// <summary>Short descriptive text shown in the event popup.</summary>
        public string description;

        [Tooltip("Broad category that determines handling and UI presentation.")]
        /// <summary>Broad category that determines handling and UI presentation.</summary>
        public EventCategory category = EventCategory.Discovery;

        [Tooltip("Urgency level; higher priorities interrupt passive notifications.")]
        /// <summary>Urgency level; higher priorities interrupt passive notifications.</summary>
        public EventPriority priority = EventPriority.Medium;

        [Tooltip("Skill requirement classification for this event.")]
        /// <summary>Skill requirement classification for this event.</summary>
        public QuestDifficulty difficulty = QuestDifficulty.Normal;

        [Tooltip("Icon shown in the event popup and on the minimap.")]
        /// <summary>Icon shown in the event popup and on the minimap.</summary>
        public Sprite eventIcon;

        // ── Timing ───────────────────────────────────────────────────────────────

        [Header("Timing")]
        [Tooltip("Seconds before the event expires if the player does not complete it.")]
        /// <summary>Seconds before the event expires if the player does not complete it.</summary>
        public float duration = 180f;

        [Tooltip("Minimum seconds that must elapse before this event can spawn again.")]
        /// <summary>Minimum seconds that must elapse before this event can spawn again.</summary>
        public float cooldown = 600f;

        // ── Spawn Conditions ─────────────────────────────────────────────────────

        [Header("Spawn Conditions")]
        [Tooltip("Minimum altitude (metres) at which this event may spawn.")]
        /// <summary>Minimum altitude (metres) at which this event may spawn.</summary>
        public float minAltitude = 0f;

        [Tooltip("Maximum altitude (metres) at which this event may spawn.")]
        /// <summary>Maximum altitude (metres) at which this event may spawn.</summary>
        public float maxAltitude = 100000f;

        [Tooltip("Maximum world-space distance from the player at which the event can be placed.")]
        /// <summary>Maximum world-space distance from the player at which the event can be placed.</summary>
        public float spawnRadius = 5000f;

        [Tooltip("Minimum player progression level required for this event to appear.")]
        /// <summary>Minimum player progression level required for this event to appear.</summary>
        [Min(1)]
        public int minPlayerLevel = 1;

        [Tooltip("Biome tags required for this event to spawn. Leave empty to allow any biome.")]
        /// <summary>
        /// Biome tags required for this event to spawn.
        /// An empty list means the event may spawn in any biome.
        /// </summary>
        public List<string> requiredBiomes = new List<string>();

        // ── Rewards ───────────────────────────────────────────────────────────────

        [Header("Rewards")]
        [Tooltip("Rewards granted to the player on successful completion.")]
        /// <summary>Rewards granted to the player on successful completion.</summary>
        public RewardData rewards = new RewardData();
    }
}
