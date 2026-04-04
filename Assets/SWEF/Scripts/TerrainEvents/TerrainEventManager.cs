// TerrainEventManager.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — Singleton MonoBehaviour that acts as the central controller for all
    /// dynamic terrain/geological events.  Manages the spawn pool, active event list,
    /// seasonal and regional compatibility checks, and exposes aggregate query APIs
    /// used by <see cref="TerrainEventVFXController"/>, mission triggers, and the
    /// achievement system.
    ///
    /// <para>Attach to a persistent scene object — uses DontDestroyOnLoad.</para>
    /// </summary>
    public sealed class TerrainEventManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static TerrainEventManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Event Pool")]
        [Tooltip("All TerrainEventConfig assets available for spawning.")]
        public List<TerrainEventConfig> eventPool = new List<TerrainEventConfig>();

        [Header("Spawn Settings")]
        [Tooltip("Maximum number of terrain events active at the same time.")]
        [Min(1)]
        public int maxConcurrentEvents = 5;

        [Tooltip("Interval in seconds between automatic spawn-chance checks.")]
        [Min(10f)]
        public float eventCheckInterval = 60f;

        [Tooltip("Base probability (0–1) of spawning an event on each check.")]
        [Range(0f, 1f)]
        public float baseSpawnChance = 0.2f;

        [Header("Player Reference")]
        [Tooltip("Player transform used for distance-based spawn placement. Auto-found if null.")]
        [SerializeField] private Transform _playerTransform;

        #endregion

        #region Public State

        /// <summary>Read-only view of all currently active terrain events.</summary>
        public IReadOnlyList<TerrainEvent> activeEvents => _activeEvents;

        #endregion

        #region Events

        /// <summary>Raised when a new terrain event is spawned.</summary>
        public event Action<TerrainEvent> OnEventSpawned;

        /// <summary>Raised when an active terrain event changes phase.</summary>
        public event Action<TerrainEvent> OnEventPhaseChanged;

        /// <summary>Raised when an active terrain event fully ends.</summary>
        public event Action<TerrainEvent> OnEventEnded;

        #endregion

        #region Private State

        private readonly List<TerrainEvent> _activeEvents = new List<TerrainEvent>();
        private Coroutine _spawnCheckCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SWEF] TerrainEventManager: initialised.");
        }

        private void Start()
        {
            if (_playerTransform == null)
            {
                var fc = FindFirstObjectByType<Flight.FlightController>();
                if (fc != null)
                {
                    _playerTransform = fc.transform;
                    Debug.Log("[SWEF] TerrainEventManager: auto-found FlightController as player transform.");
                }
            }

            _spawnCheckCoroutine = StartCoroutine(SpawnCheckLoop());
        }

        private void OnDestroy()
        {
            if (_spawnCheckCoroutine != null)
                StopCoroutine(_spawnCheckCoroutine);
        }

        #endregion

        #region Spawn Loop

        private IEnumerator SpawnCheckLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(eventCheckInterval);

                if (_activeEvents.Count >= maxConcurrentEvents) continue;
                if (eventPool.Count == 0) continue;
                if (UnityEngine.Random.value > baseSpawnChance) continue;

                TerrainEventConfig candidate = PickCandidate();
                if (candidate == null) continue;

                Vector3 spawnPos = PickSpawnPosition();
                SpawnEventInternal(candidate, spawnPos);
            }
        }

        private TerrainEventConfig PickCandidate()
        {
            // Shuffle-pick a seasonally / biome-compatible config
            var shuffled = new List<TerrainEventConfig>(eventPool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                TerrainEventConfig tmp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = tmp;
            }

            foreach (TerrainEventConfig cfg in shuffled)
                if (IsBiomeCompatible(cfg))
                    return cfg;

            return null;
        }

        private bool IsBiomeCompatible(TerrainEventConfig cfg)
        {
            if (cfg.validBiomes == null || cfg.validBiomes.Length == 0) return true;
            if (_playerTransform == null) return true;

#if SWEF_BIOME_AVAILABLE
            Vector3 pos = _playerTransform.position;
            SWEF.Biome.BiomeType biome = SWEF.Biome.BiomeClassifier.ClassifyBiome(pos.z, pos.x, pos.y);
            string biomeName = biome.ToString();
            foreach (string valid in cfg.validBiomes)
                if (string.Equals(valid, biomeName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
#else
            return true;
#endif
        }

        private Vector3 PickSpawnPosition()
        {
            if (_playerTransform == null) return Vector3.zero;
            float dist  = UnityEngine.Random.Range(5000f, 20000f);
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return _playerTransform.position + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
        }

        #endregion

        #region Public Spawn API

        /// <summary>
        /// Spawns an event defined by <paramref name="config"/> at <paramref name="position"/>.
        /// </summary>
        public TerrainEvent SpawnEvent(TerrainEventConfig config, Vector3 position)
        {
            return SpawnEventInternal(config, position);
        }

        /// <summary>
        /// Force-spawns an event regardless of spawn limits — useful for testing and missions.
        /// </summary>
        public TerrainEvent ForceSpawnEvent(TerrainEventConfig config, Vector3 position)
        {
            if (config == null)
            {
                Debug.LogWarning("[SWEF] TerrainEventManager: ForceSpawnEvent called with null config.");
                return null;
            }
            return SpawnEventInternal(config, position);
        }

        private TerrainEvent SpawnEventInternal(TerrainEventConfig config, Vector3 position)
        {
            if (config == null)
            {
                Debug.LogWarning("[SWEF] TerrainEventManager: SpawnEvent called with null config.");
                return null;
            }

            System.Type eventType = ResolveEventType(config.eventType);
            GameObject go = new GameObject($"TerrainEvent_{config.eventName}_{config.eventId}");
            var ev = (TerrainEvent)go.AddComponent(eventType);
            ev.Initialise(config, position);
            ev.OnEventEnded += HandleEventEnded;

            _activeEvents.Add(ev);
            OnEventSpawned?.Invoke(ev);
            Debug.Log($"[SWEF] TerrainEventManager: spawned '{config.eventName}' at {position}.");
            return ev;
        }

        private static System.Type ResolveEventType(TerrainEventType type)
        {
            switch (type)
            {
                case TerrainEventType.VolcanicEruption: return typeof(VolcanicEruptionEvent);
                case TerrainEventType.Earthquake:       return typeof(EarthquakeEvent);
                case TerrainEventType.Aurora:           return typeof(AuroraEvent);
                case TerrainEventType.Tsunami:          return typeof(TsunamiEvent);
                case TerrainEventType.Geyser:           return typeof(GeyserEvent);
                default:                                return typeof(TerrainEvent);
            }
        }

        #endregion

        #region End Event

        /// <summary>
        /// Terminates <paramref name="ev"/> and removes it from the active list.
        /// Called automatically by <see cref="TerrainEvent"/> when Aftermath ends; can also
        /// be called externally to force-end an event.
        /// </summary>
        public void EndEvent(TerrainEvent ev)
        {
            if (ev == null) return;
            _activeEvents.Remove(ev);
            OnEventEnded?.Invoke(ev);
            if (ev.gameObject != null)
                Destroy(ev.gameObject);
            Debug.Log($"[SWEF] TerrainEventManager: ended event '{ev.config?.eventName}'.");
        }

        private void HandleEventEnded(TerrainEvent ev)
        {
            _activeEvents.Remove(ev);
        }

        #endregion

        #region Phase Change Notification

        /// <summary>
        /// Called by <see cref="TerrainEvent"/> when its phase changes.
        /// </summary>
        internal void NotifyPhaseChanged(TerrainEvent ev)
        {
            OnEventPhaseChanged?.Invoke(ev);
        }

        #endregion

        #region Aggregate Query API

        /// <summary>
        /// Returns the aggregate turbulence multiplier at the given world position and altitude.
        /// </summary>
        public float GetTurbulenceAt(Vector3 pos, float altitude)
        {
            float total = 0f;
            foreach (TerrainEvent ev in _activeEvents)
            {
                if (ev.config == null) continue;
                if (altitude < ev.config.altitudeRange.x || altitude > ev.config.altitudeRange.y) continue;
                total += ev.GetTurbulenceAt(pos);
            }
            return Mathf.Min(total, 5f);
        }

        /// <summary>
        /// Returns the aggregate visibility reduction (0–1) at the given world position and altitude.
        /// </summary>
        public float GetVisibilityReductionAt(Vector3 pos, float altitude)
        {
            float total = 0f;
            foreach (TerrainEvent ev in _activeEvents)
            {
                if (ev.config == null) continue;
                if (altitude < ev.config.altitudeRange.x || altitude > ev.config.altitudeRange.y) continue;
                total += ev.GetVisibilityReductionAt(pos);
            }
            return Mathf.Clamp01(total);
        }

        /// <summary>
        /// Returns all active events whose effect radius includes <paramref name="pos"/>.
        /// </summary>
        public List<TerrainEvent> GetEventsAtPosition(Vector3 pos)
        {
            var result = new List<TerrainEvent>();
            foreach (TerrainEvent ev in _activeEvents)
                if (ev.ContainsPosition(pos))
                    result.Add(ev);
            return result;
        }

        /// <summary>
        /// Returns all active events of the specified type.
        /// </summary>
        public List<TerrainEvent> GetEventsByType(TerrainEventType type)
        {
            var result = new List<TerrainEvent>();
            foreach (TerrainEvent ev in _activeEvents)
                if (ev.config != null && ev.config.eventType == type)
                    result.Add(ev);
            return result;
        }

        #endregion
    }
}
