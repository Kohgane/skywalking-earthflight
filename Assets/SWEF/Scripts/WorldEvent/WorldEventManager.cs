// WorldEventManager.cs — SWEF Dynamic Event & World Quest System (Phase 64)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.WorldEvent
{
    /// <summary>
    /// Singleton MonoBehaviour that manages the full lifecycle of world events:
    /// eligibility checks, spawning, cooldown tracking, and completion notifications.
    /// Attach to a persistent scene object (DontDestroyOnLoad).
    /// </summary>
    public sealed class WorldEventManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────

        /// <summary>Shared singleton instance.</summary>
        public static WorldEventManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Event Pool")]
        [Tooltip("All WorldEventData assets that can be spawned at runtime.")]
        /// <summary>All WorldEventData assets that can be spawned at runtime.</summary>
        public List<WorldEventData> eventPool = new List<WorldEventData>();

        [Header("Prefabs")]
        [Tooltip("Prefab instantiated for each active world event. Must have an ActiveWorldEvent component.")]
        /// <summary>Prefab instantiated for each active world event.</summary>
        [SerializeField] private GameObject _eventPrefab;

        [Header("Spawn Settings")]
        [Tooltip("Maximum number of simultaneously active events.")]
        /// <summary>Maximum number of simultaneously active events.</summary>
        [Min(1)]
        public int maxConcurrentEvents = WorldEventConfig.MaxConcurrentEvents;

        [Tooltip("Seconds between each automatic spawn check.")]
        /// <summary>Seconds between each automatic spawn check.</summary>
        [Min(1f)]
        public float eventCheckInterval = WorldEventConfig.EventCheckInterval;

        [Tooltip("Probability [0, 1] that each spawn check produces a new event.")]
        /// <summary>Probability [0, 1] that each spawn check produces a new event.</summary>
        [Range(0f, 1f)]
        public float baseSpawnChance = WorldEventConfig.BaseSpawnChance;

        [Header("Player Reference")]
        [Tooltip("Transform tracked to determine spawn positions and altitude.")]
        /// <summary>Transform tracked to determine spawn positions and altitude.</summary>
        [SerializeField] private Transform _playerTransform;

        [Tooltip("Current player progression level used to gate events.")]
        /// <summary>Current player progression level used to gate events.</summary>
        public int playerLevel = 1;

        [Tooltip("Current biome tag at the player's location.")]
        /// <summary>Current biome tag at the player's location.</summary>
        public string currentBiome = string.Empty;

        // ── Runtime State ────────────────────────────────────────────────────────

        /// <summary>All currently running event instances.</summary>
        public IReadOnlyList<ActiveWorldEvent> activeEvents => _activeEvents;
        private readonly List<ActiveWorldEvent> _activeEvents = new List<ActiveWorldEvent>();

        private readonly Dictionary<string, float> _cooldownTimestamps = new Dictionary<string, float>();

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Raised when a new event is spawned in the world.</summary>
        public event Action<ActiveWorldEvent> OnEventSpawned;

        /// <summary>Raised when an event is completed successfully.</summary>
        public event Action<ActiveWorldEvent> OnEventCompleted;

        /// <summary>Raised when an event is failed.</summary>
        public event Action<ActiveWorldEvent> OnEventFailed;

        /// <summary>Raised when an event expires without completion or failure.</summary>
        public event Action<ActiveWorldEvent> OnEventExpired;

        // ── Unity ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_playerTransform == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                    _playerTransform = playerObj.transform;
            }

            StartCoroutine(SpawnCheckLoop());
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Immediately spawns the given event at <paramref name="position"/>, bypassing
        /// all eligibility checks.  Use for scripted triggers and tutorials.
        /// </summary>
        /// <param name="data">Event template to spawn.</param>
        /// <param name="position">World-space position at which to place the event.</param>
        /// <returns>The spawned <see cref="ActiveWorldEvent"/>, or <c>null</c> if spawning failed.</returns>
        public ActiveWorldEvent ForceSpawnEvent(WorldEventData data, Vector3 position)
        {
            if (data == null) return null;
            return SpawnEvent(data, position);
        }

        /// <summary>
        /// Cancels the first active event whose template matches <paramref name="eventId"/>.
        /// </summary>
        /// <param name="eventId">Template event ID to match.</param>
        public void CancelEvent(string eventId)
        {
            for (int i = _activeEvents.Count - 1; i >= 0; i--)
            {
                var ev = _activeEvents[i];
                if (ev != null && ev.eventData != null && ev.eventData.eventId == eventId)
                {
                    ev.Fail();
                    return;
                }
            }
        }

        /// <summary>
        /// Returns the active event whose world position is nearest to
        /// <paramref name="position"/>, or <c>null</c> if no events are active.
        /// </summary>
        /// <param name="position">Reference world-space position.</param>
        public ActiveWorldEvent GetNearestEvent(Vector3 position)
        {
            ActiveWorldEvent nearest = null;
            float best = float.MaxValue;

            foreach (var ev in _activeEvents)
            {
                if (ev == null) continue;
                float d = Vector3.Distance(ev.worldPosition, position);
                if (d < best)
                {
                    best = d;
                    nearest = ev;
                }
            }

            return nearest;
        }

        // ── Internal notifications (called by ActiveWorldEvent) ───────────────────

        /// <summary>Called by <see cref="ActiveWorldEvent.Complete"/> to notify the manager.</summary>
        internal void NotifyEventCompleted(ActiveWorldEvent ev)
        {
            _activeEvents.Remove(ev);
            RecordCooldown(ev);
            OnEventCompleted?.Invoke(ev);
        }

        /// <summary>Called by <see cref="ActiveWorldEvent.Fail"/> to notify the manager.</summary>
        internal void NotifyEventFailed(ActiveWorldEvent ev)
        {
            _activeEvents.Remove(ev);
            RecordCooldown(ev);
            OnEventFailed?.Invoke(ev);
        }

        /// <summary>Called by <see cref="ActiveWorldEvent.Expire"/> to notify the manager.</summary>
        internal void NotifyEventExpired(ActiveWorldEvent ev)
        {
            _activeEvents.Remove(ev);
            RecordCooldown(ev);
            OnEventExpired?.Invoke(ev);
        }

        // ── Spawn Loop ───────────────────────────────────────────────────────────

        private IEnumerator SpawnCheckLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(eventCheckInterval);
                TrySpawnRandomEvent();
            }
        }

        private void TrySpawnRandomEvent()
        {
            if (_activeEvents.Count >= maxConcurrentEvents) return;
            if (UnityEngine.Random.value > baseSpawnChance) return;
            if (_playerTransform == null) return;

            var candidates = BuildCandidateList();
            if (candidates.Count == 0) return;

            var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            var position = PickSpawnPosition(chosen);
            SpawnEvent(chosen, position);
        }

        private List<WorldEventData> BuildCandidateList()
        {
            var result = new List<WorldEventData>();
            float now = Time.time;
            float playerAlt = _playerTransform != null ? _playerTransform.position.y : 0f;

            foreach (var data in eventPool)
            {
                if (data == null) continue;
                if (playerAlt < data.minAltitude || playerAlt > data.maxAltitude) continue;
                if (playerLevel < data.minPlayerLevel) continue;
                if (data.requiredBiomes != null && data.requiredBiomes.Count > 0 &&
                    !data.requiredBiomes.Contains(currentBiome)) continue;
                if (IsOnCooldown(data, now)) continue;

                result.Add(data);
            }

            return result;
        }

        private bool IsOnCooldown(WorldEventData data, float now)
        {
            if (_cooldownTimestamps.TryGetValue(data.eventId, out float last))
                return (now - last) < data.cooldown;
            return false;
        }

        private Vector3 PickSpawnPosition(WorldEventData data)
        {
            Vector3 playerPos = _playerTransform != null ? _playerTransform.position : Vector3.zero;
            float maxDist = Mathf.Min(data.spawnRadius, WorldEventConfig.MaxEventDistance);
            Vector2 rand2D = UnityEngine.Random.insideUnitCircle.normalized *
                             UnityEngine.Random.Range(WorldEventConfig.MinEventDistance, maxDist);
            return new Vector3(playerPos.x + rand2D.x, playerPos.y, playerPos.z + rand2D.y);
        }

        private ActiveWorldEvent SpawnEvent(WorldEventData data, Vector3 position)
        {
            GameObject go;
            if (_eventPrefab != null)
            {
                go = Instantiate(_eventPrefab, position, Quaternion.identity);
            }
            else
            {
                go = new GameObject($"WorldEvent_{data.eventId}");
                go.transform.position = position;
            }

            var ev = go.GetComponent<ActiveWorldEvent>() ?? go.AddComponent<ActiveWorldEvent>();
            ev.eventData = data;

            _activeEvents.Add(ev);
            OnEventSpawned?.Invoke(ev);
            return ev;
        }

        private void RecordCooldown(ActiveWorldEvent ev)
        {
            if (ev?.eventData != null && !string.IsNullOrEmpty(ev.eventData.eventId))
                _cooldownTimestamps[ev.eventData.eventId] = Time.time;
        }
    }
}
