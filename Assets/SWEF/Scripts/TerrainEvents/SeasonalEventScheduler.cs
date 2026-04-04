// SeasonalEventScheduler.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — MonoBehaviour that schedules terrain events based on the current
    /// in-game season and the player's geographic region.
    ///
    /// <para>Works alongside <see cref="TerrainEventManager"/> — it selects <em>which</em> event
    /// to spawn and <em>where</em>, then delegates actual spawning to the manager.</para>
    /// </summary>
    public sealed class SeasonalEventScheduler : MonoBehaviour
    {
        #region Inspector

        [Header("Season Data")]
        [Tooltip("All season definitions. The scheduler picks the one that is currently active.")]
        public SeasonDefinition[] seasons = new SeasonDefinition[0];

        [Header("Regional Profiles")]
        [Tooltip("All regional profiles available for event scheduling.")]
        public List<RegionalEventProfile> regions = new List<RegionalEventProfile>();

        [Header("Scheduling Settings")]
        [Tooltip("How often (seconds) the scheduler checks whether to spawn an event.")]
        [Min(30f)]
        public float checkInterval = 120f;

        [Tooltip("Player transform used for region detection. Auto-found if null.")]
        [SerializeField] private Transform _playerTransform;

        #endregion

        #region Events

        /// <summary>Raised when the scheduler decides to spawn an event.</summary>
        public event Action<TerrainEventConfig, Vector3> OnScheduledSpawn;

        #endregion

        #region Private State

        private Coroutine _scheduleCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_playerTransform == null)
            {
                var fc = FindFirstObjectByType<Flight.FlightController>();
                if (fc != null) _playerTransform = fc.transform;
            }

            _scheduleCoroutine = StartCoroutine(ScheduleLoop());
        }

        private void OnDestroy()
        {
            if (_scheduleCoroutine != null)
                StopCoroutine(_scheduleCoroutine);
        }

        #endregion

        #region Schedule Loop

        private IEnumerator ScheduleLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkInterval);

                if (TerrainEventManager.Instance == null) continue;
                if (TerrainEventManager.Instance.activeEvents.Count >= TerrainEventManager.Instance.maxConcurrentEvents) continue;

                TryScheduleEvent();
            }
        }

        private void TryScheduleEvent()
        {
            SeasonDefinition currentSeason = SeasonDefinition.GetCurrentSeason(seasons);
            RegionalEventProfile region    = GetPlayerRegion();

            if (region == null) return;

            TerrainEventConfig cfg = PickConfig(region, currentSeason);
            if (cfg == null) return;

            // Spawn inside the region's bounding box near the player
            Vector3 spawnPos = PickPositionInRegion(region);

            OnScheduledSpawn?.Invoke(cfg, spawnPos);
            TerrainEventManager.Instance.SpawnEvent(cfg, spawnPos);

            Debug.Log($"[SWEF] SeasonalEventScheduler: scheduled '{cfg.eventName}' " +
                      $"in region '{region.regionName}' " +
                      $"(season: {currentSeason?.seasonName ?? "none"}).");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private RegionalEventProfile GetPlayerRegion()
        {
            if (_playerTransform == null || regions.Count == 0) return null;

            // Weighted random pick among regions that contain the player
            var candidates = new List<RegionalEventProfile>();
            foreach (var r in regions)
                if (r != null && r.ContainsPosition(_playerTransform.position))
                    candidates.Add(r);

            if (candidates.Count == 0)
            {
                // Fallback: pick any region by weight
                candidates.AddRange(regions);
            }

            return PickWeighted(candidates);
        }

        private static RegionalEventProfile PickWeighted(List<RegionalEventProfile> list)
        {
            float totalWeight = 0f;
            foreach (var r in list) totalWeight += r.spawnWeight;
            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;
            foreach (var r in list)
            {
                cumulative += r.spawnWeight;
                if (roll <= cumulative) return r;
            }
            return list[list.Count - 1];
        }

        private static TerrainEventConfig PickConfig(RegionalEventProfile region, SeasonDefinition season)
        {
            if (region.allowedEventConfigs.Count == 0) return null;

            // Apply seasonal probability multipliers via weighted random
            float total = 0f;
            foreach (var cfg in region.allowedEventConfigs)
            {
                if (cfg == null) continue;
                float weight = season != null ? season.GetProbabilityMultiplier(cfg.eventType) : 1f;
                total += weight;
            }

            if (total <= 0f) return region.GetRandomConfig();

            float r = UnityEngine.Random.value * total;
            float acc = 0f;
            foreach (var cfg in region.allowedEventConfigs)
            {
                if (cfg == null) continue;
                acc += season != null ? season.GetProbabilityMultiplier(cfg.eventType) : 1f;
                if (r <= acc) return cfg;
            }

            return region.GetRandomConfig();
        }

        private Vector3 PickPositionInRegion(RegionalEventProfile region)
        {
            float x = UnityEngine.Random.Range(region.boundsMin.x, region.boundsMax.x);
            float z = UnityEngine.Random.Range(region.boundsMin.y, region.boundsMax.y);
            return new Vector3(x, 0f, z);
        }

        #endregion
    }
}
