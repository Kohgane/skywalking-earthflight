// EventSpawnZone.cs — SWEF Dynamic Event & World Quest System (Phase 64)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.WorldEvent
{
    /// <summary>
    /// Defines a world-space zone within which world events of specific categories
    /// may spawn.  Place one or more of these in a scene alongside a trigger
    /// <see cref="Collider"/> to create themed event regions.
    /// Registers itself with <see cref="WorldEventManager"/> on Awake.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class EventSpawnZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        [Tooltip("Trigger collider that defines the zone volume. Assigned automatically from this GameObject.")]
        /// <summary>Trigger collider that defines the zone volume.</summary>
        public Collider zoneCollider;

        [Tooltip("Categories of events that may spawn inside this zone. Leave empty to allow all categories.")]
        /// <summary>
        /// Categories of events that may spawn inside this zone.
        /// An empty list permits all categories.
        /// </summary>
        public List<EventCategory> allowedCategories = new List<EventCategory>();

        [Tooltip("Biome tags associated with this zone, forwarded to the spawn eligibility check.")]
        /// <summary>Biome tags associated with this zone.</summary>
        public List<string> biomeTags = new List<string>();

        [Tooltip("Multiplicative modifier applied to the base spawn chance inside this zone (1 = no change).")]
        /// <summary>
        /// Multiplicative modifier applied to the base spawn chance inside this zone.
        /// Values &gt; 1 increase spawn frequency; values &lt; 1 reduce it.
        /// </summary>
        [Min(0f)]
        public float spawnMultiplier = 1f;

        [Tooltip("Maximum simultaneous events that may be active within this zone.")]
        /// <summary>Maximum simultaneous events that may be active within this zone.</summary>
        [Min(1)]
        public int maxEventsInZone = 2;

        // ── Runtime State ────────────────────────────────────────────────────────

        /// <summary>Number of currently active events spawned within this zone.</summary>
        public int activeEventCount { get; private set; }

        // ── Unity ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (zoneCollider == null)
                zoneCollider = GetComponent<Collider>();

            if (zoneCollider != null)
                zoneCollider.isTrigger = true;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when this zone accepts events of the given
        /// <paramref name="category"/> and has room for more.
        /// </summary>
        /// <param name="category">Category of the candidate event.</param>
        public bool AcceptsCategory(EventCategory category)
        {
            if (allowedCategories != null && allowedCategories.Count > 0 &&
                !allowedCategories.Contains(category))
                return false;

            return activeEventCount < maxEventsInZone;
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="position"/> lies inside the
        /// zone collider.
        /// </summary>
        /// <param name="position">World-space point to test.</param>
        public bool Contains(Vector3 position)
        {
            if (zoneCollider == null) return false;
            return zoneCollider.bounds.Contains(position);
        }

        /// <summary>Increments the active event count for this zone.</summary>
        public void RegisterEvent()
        {
            activeEventCount++;
        }

        /// <summary>Decrements the active event count for this zone (minimum 0).</summary>
        public void UnregisterEvent()
        {
            if (activeEventCount > 0)
                activeEventCount--;
        }
    }
}
