// TerrainEventMissionTrigger.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — MonoBehaviour that listens for active terrain events and generates
    /// contextual mission prompts (fly through aurora, photograph volcano, etc.).
    ///
    /// <para>Integrates with the SWEF Mission system via the <c>SWEF_MISSION_AVAILABLE</c>
    /// compile guard and the <see cref="OnMissionTriggered"/> event for loose coupling.</para>
    /// </summary>
    public sealed class TerrainEventMissionTrigger : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Mission Trigger Settings")]
        [Tooltip("Minimum seconds between mission trigger evaluations.")]
        [Min(5f)]
        public float evaluationCooldown = 30f;

        [Tooltip("Player transform used to test proximity to events. Auto-found if null.")]
        [SerializeField] private Transform _playerTransform;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when a contextual mission is triggered by a terrain event.
        /// Parameters: (eventConfig, missionType, playerPosition)
        /// </summary>
        public event Action<TerrainEventConfig, TerrainEventMissionType, Vector3> OnMissionTriggered;

        // ── Private State ─────────────────────────────────────────────────────────

        private float _lastEvaluationTime;
        private readonly HashSet<string> _triggeredMissionKeys = new HashSet<string>();

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            if (_playerTransform == null)
            {
                var fc = FindFirstObjectByType<Flight.FlightController>();
                if (fc != null) _playerTransform = fc.transform;
            }

            if (TerrainEventManager.Instance != null)
            {
                TerrainEventManager.Instance.OnEventSpawned      += OnEventSpawned;
                TerrainEventManager.Instance.OnEventPhaseChanged += OnEventPhaseChanged;
            }
        }

        private void OnDestroy()
        {
            if (TerrainEventManager.Instance != null)
            {
                TerrainEventManager.Instance.OnEventSpawned      -= OnEventSpawned;
                TerrainEventManager.Instance.OnEventPhaseChanged -= OnEventPhaseChanged;
            }
        }

        private void Update()
        {
            if (Time.time - _lastEvaluationTime < evaluationCooldown) return;
            _lastEvaluationTime = Time.time;
            EvaluateMissionTriggers();
        }

        // ── Evaluation ────────────────────────────────────────────────────────────

        private void EvaluateMissionTriggers()
        {
            if (TerrainEventManager.Instance == null || _playerTransform == null) return;

            Vector3 playerPos = _playerTransform.position;

            foreach (TerrainEvent ev in TerrainEventManager.Instance.activeEvents)
            {
                if (ev.config == null || !ev.config.canTriggerMission) continue;
                if (ev.phase != TerrainEventPhase.Active && ev.phase != TerrainEventPhase.Peak) continue;

                foreach (TerrainEventMissionType mType in ev.config.availableMissionTypes)
                {
                    string key = $"{ev.config.eventId}_{mType}";
                    if (_triggeredMissionKeys.Contains(key)) continue;

                    if (ShouldTriggerMission(ev, mType, playerPos))
                    {
                        _triggeredMissionKeys.Add(key);
                        TriggerMission(ev.config, mType, playerPos);
                    }
                }
            }
        }

        private bool ShouldTriggerMission(TerrainEvent ev, TerrainEventMissionType mType, Vector3 playerPos)
        {
            float dist = Vector3.Distance(playerPos, ev.origin);

            switch (mType)
            {
                case TerrainEventMissionType.Witness:
                    return dist <= ev.currentRadius * 2f;

                case TerrainEventMissionType.FlyThrough:
                    return dist <= ev.currentRadius;

                case TerrainEventMissionType.Photograph:
                    return dist <= ev.currentRadius * 3f;

                case TerrainEventMissionType.Survive:
                    return dist <= ev.currentRadius;

                case TerrainEventMissionType.Research:
                    return dist <= ev.currentRadius * 4f;

                default:
                    return false;
            }
        }

        private void TriggerMission(TerrainEventConfig cfg, TerrainEventMissionType mType, Vector3 playerPos)
        {
            Debug.Log($"[SWEF] TerrainEventMissionTrigger: triggered mission type '{mType}' for event '{cfg.eventName}'.");
            OnMissionTriggered?.Invoke(cfg, mType, playerPos);

#if SWEF_MISSION_AVAILABLE
            // Forward to the SWEF Mission system when compiled in
            SWEF.Mission.MissionManager.Instance?.TriggerTerrainEventMission(cfg.eventId, mType.ToString());
#endif
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void OnEventSpawned(TerrainEvent ev)
        {
            // Clear old mission keys for this event in case it's a recurring spawn
            if (ev.config != null)
                foreach (TerrainEventMissionType mType in ev.config.availableMissionTypes)
                    _triggeredMissionKeys.Remove($"{ev.config.eventId}_{mType}");
        }

        private void OnEventPhaseChanged(TerrainEvent ev)
        {
            // Re-evaluate immediately when an event reaches peak
            if (ev.phase == TerrainEventPhase.Peak)
                EvaluateMissionTriggers();
        }
    }
}
