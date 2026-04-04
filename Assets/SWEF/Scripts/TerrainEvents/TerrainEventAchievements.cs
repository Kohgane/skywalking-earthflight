// TerrainEventAchievements.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — Tracks achievement progress tied to witnessing, surviving, or
    /// interacting with dynamic terrain events.
    ///
    /// <para>Achievement keys emitted here follow the pattern
    /// <c>terrain_event_{eventId}_{missionType}</c> and are forwarded to the SWEF
    /// Achievement system via the <c>SWEF_ACHIEVEMENT_AVAILABLE</c> compile guard.</para>
    /// </summary>
    public sealed class TerrainEventAchievements : MonoBehaviour
    {
        // ── Achievement Keys (public constants for external use) ──────────────────

        public const string WitnessVolcano     = "terrain_witness_volcanic_eruption";
        public const string FlyThroughAurora   = "terrain_flythrough_aurora";
        public const string SurviveEarthquake  = "terrain_survive_earthquake";
        public const string PhotographTsunami  = "terrain_photograph_tsunami";
        public const string WitnessGeyser      = "terrain_witness_geyser";
        public const string AllEventsWitnessed = "terrain_all_events_witnessed";

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Achievement Settings")]
        [Tooltip("Minimum distance in metres for a 'witnessed' credit.")]
        [Min(100f)]
        public float witnessDistance = 5000f;

        [Tooltip("Player transform. Auto-found if null.")]
        [SerializeField] private Transform _playerTransform;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when an achievement key is unlocked. Parameter is the achievement key.</summary>
        public event Action<string> OnAchievementUnlocked;

        // ── Private State ─────────────────────────────────────────────────────────

        private readonly HashSet<string> _unlockedKeys = new HashSet<string>();
        private readonly HashSet<TerrainEventType> _witnessedTypes = new HashSet<TerrainEventType>();

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
                TerrainEventManager.Instance.OnEventPhaseChanged += OnEventPhaseChanged;
                TerrainEventManager.Instance.OnEventSpawned      += OnEventSpawned;
            }
        }

        private void OnDestroy()
        {
            if (TerrainEventManager.Instance != null)
            {
                TerrainEventManager.Instance.OnEventPhaseChanged -= OnEventPhaseChanged;
                TerrainEventManager.Instance.OnEventSpawned      -= OnEventSpawned;
            }
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void OnEventSpawned(TerrainEvent ev) { /* reserved for future early-spawn achievements */ }

        private void OnEventPhaseChanged(TerrainEvent ev)
        {
            if (ev.phase != TerrainEventPhase.Active && ev.phase != TerrainEventPhase.Peak) return;
            if (_playerTransform == null || ev.config == null) return;

            float dist = Vector3.Distance(_playerTransform.position, ev.origin);
            if (dist > witnessDistance) return;

            CheckWitnessAchievement(ev);
            CheckTypeSpecificAchievements(ev);
            CheckAllWitnessedAchievement();
        }

        // ── Achievement Checks ────────────────────────────────────────────────────

        private void CheckWitnessAchievement(TerrainEvent ev)
        {
            if (ev.config.witnessAchievementKeys == null) return;
            foreach (string key in ev.config.witnessAchievementKeys)
                Unlock(key);

            _witnessedTypes.Add(ev.config.eventType);
        }

        private void CheckTypeSpecificAchievements(TerrainEvent ev)
        {
            switch (ev.config.eventType)
            {
                case TerrainEventType.VolcanicEruption:
                    Unlock(WitnessVolcano);
                    break;

                case TerrainEventType.Aurora:
                    if (_playerTransform != null && _playerTransform.position.y >= 5000f)
                        Unlock(FlyThroughAurora);
                    break;

                case TerrainEventType.Earthquake:
                    if (ev.ContainsPosition(_playerTransform.position))
                        Unlock(SurviveEarthquake);
                    break;

                case TerrainEventType.Tsunami:
                    Unlock(PhotographTsunami);
                    break;

                case TerrainEventType.Geyser:
                    Unlock(WitnessGeyser);
                    break;
            }
        }

        private void CheckAllWitnessedAchievement()
        {
            // Award if all five main event types have been witnessed
            if (_witnessedTypes.Contains(TerrainEventType.VolcanicEruption) &&
                _witnessedTypes.Contains(TerrainEventType.Earthquake)       &&
                _witnessedTypes.Contains(TerrainEventType.Aurora)           &&
                _witnessedTypes.Contains(TerrainEventType.Tsunami)          &&
                _witnessedTypes.Contains(TerrainEventType.Geyser))
            {
                Unlock(AllEventsWitnessed);
            }
        }

        private void Unlock(string key)
        {
            if (string.IsNullOrEmpty(key) || _unlockedKeys.Contains(key)) return;
            _unlockedKeys.Add(key);
            Debug.Log($"[SWEF] TerrainEventAchievements: unlocked '{key}'.");
            OnAchievementUnlocked?.Invoke(key);

#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.Unlock(key);
#endif
        }

        /// <summary>Returns <c>true</c> if the achievement with <paramref name="key"/> is unlocked in this session.</summary>
        public bool IsUnlocked(string key) => _unlockedKeys.Contains(key);

        /// <summary>Returns all achievement keys that have been unlocked this session.</summary>
        public IReadOnlyCollection<string> GetUnlockedKeys() => _unlockedKeys;
    }
}
