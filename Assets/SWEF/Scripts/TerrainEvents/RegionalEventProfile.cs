// RegionalEventProfile.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — ScriptableObject that defines which terrain events can occur within a
    /// named geographic region (e.g. "Pacific Ring of Fire", "Polar North").
    ///
    /// <para>Create via <em>Assets → Create → SWEF/TerrainEvents/Regional Event Profile</em>.
    /// Register with <see cref="SeasonalEventScheduler"/> to influence spawn selection.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/TerrainEvents/Regional Event Profile", fileName = "NewRegionalEventProfile")]
    public class RegionalEventProfile : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        [Header("Identity")]

        [Tooltip("Unique region identifier (e.g. \"ring_of_fire\").")]
        public string regionId;

        [Tooltip("Human-readable region name.")]
        public string regionName;

        // ── Geographic Bounds ─────────────────────────────────────────────────────

        [Header("Geographic Bounds (World-space AABB XZ)")]

        [Tooltip("Minimum world-space XZ corner of this region's bounding box.")]
        public Vector2 boundsMin;

        [Tooltip("Maximum world-space XZ corner of this region's bounding box.")]
        public Vector2 boundsMax;

        // ── Allowed Events ────────────────────────────────────────────────────────

        [Header("Allowed Events")]

        [Tooltip("Terrain event configs that are eligible to spawn within this region.")]
        public List<TerrainEventConfig> allowedEventConfigs = new List<TerrainEventConfig>();

        [Tooltip("Spawn probability weight for this region (higher = spawns more often relative to other regions).")]
        [Min(0.01f)]
        public float spawnWeight = 1f;

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> if <paramref name="worldPos"/> falls inside this region's AABB.</summary>
        public bool ContainsPosition(Vector3 worldPos)
        {
            return worldPos.x >= boundsMin.x && worldPos.x <= boundsMax.x &&
                   worldPos.z >= boundsMin.y && worldPos.z <= boundsMax.y;
        }

        /// <summary>Returns a random allowed config, or <c>null</c> if the list is empty.</summary>
        public TerrainEventConfig GetRandomConfig()
        {
            if (allowedEventConfigs == null || allowedEventConfigs.Count == 0) return null;
            return allowedEventConfigs[UnityEngine.Random.Range(0, allowedEventConfigs.Count)];
        }
    }
}
