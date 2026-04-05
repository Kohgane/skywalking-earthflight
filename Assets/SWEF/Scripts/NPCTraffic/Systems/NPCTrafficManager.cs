// NPCTrafficManager.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Central singleton that orchestrates NPC spawning, updates, pooling,
// density control, and cross-system integration.
// Namespace: SWEF.NPCTraffic

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Central singleton managing the full NPC Traffic ecosystem.
    /// Controls spawn/despawn budgets, density scaling, object pooling, and
    /// provides the public API consumed by all other NPC sub-systems.
    /// Attach to a persistent scene object — uses <c>DontDestroyOnLoad</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NPCTrafficManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static NPCTrafficManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Configuration")]
        [Tooltip("NPC Traffic configuration asset.")]
        [SerializeField] private NPCTrafficConfig _config;

        [Header("References")]
        [Tooltip("Transform representing the player (updated at runtime).")]
        [SerializeField] private Transform _playerTransform;

        #endregion

        #region Events

        /// <summary>Fired when an NPC is spawned. Argument is the new NPC data.</summary>
        public event Action<NPCAircraftData> OnNPCSpawned;

        /// <summary>Fired when an NPC is despawned. Argument is the NPC identifier.</summary>
        public event Action<string> OnNPCDespawned;

        /// <summary>Fired when traffic density changes.</summary>
        public event Action<NPCTrafficDensity> OnDensityChanged;

        #endregion

        #region Public State

        /// <summary>Read-only view of all currently active NPC aircraft.</summary>
        public IReadOnlyList<NPCAircraftData> ActiveNPCs => _activeNPCs;

        /// <summary>Current effective traffic density (may differ from config after time-of-day scaling).</summary>
        public NPCTrafficDensity CurrentDensity { get; private set; }

        /// <summary>Maximum active NPC count as adjusted by time-of-day multiplier.</summary>
        public int EffectiveMaxNPCs { get; private set; }

        #endregion

        #region Private State

        private readonly List<NPCAircraftData> _activeNPCs = new List<NPCAircraftData>();
        private readonly Dictionary<string, NPCAircraftData> _npcLookup = new Dictionary<string, NPCAircraftData>();
        private readonly Queue<NPCAircraftData> _pool = new Queue<NPCAircraftData>();

        private NPCSpawnController  _spawnController;
        private NPCRouteGenerator   _routeGenerator;
        private Coroutine           _updateCoroutine;
        private Coroutine           _spawnCoroutine;
        private int                 _idCounter;

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

            if (_config == null)
                _config = ScriptableObject.CreateInstance<NPCTrafficConfig>();
        }

        private void Start()
        {
            ResolveReferences();
            ApplyDensitySettings();

            _updateCoroutine = StartCoroutine(UpdateLoop());
            _spawnCoroutine  = StartCoroutine(SpawnLoop());
        }

        private void OnDestroy()
        {
            if (_updateCoroutine != null) StopCoroutine(_updateCoroutine);
            if (_spawnCoroutine  != null) StopCoroutine(_spawnCoroutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Registers an externally-created NPC with the traffic manager.
        /// </summary>
        /// <param name="data">NPC data to register.</param>
        public void RegisterNPC(NPCAircraftData data)
        {
            if (data == null || _npcLookup.ContainsKey(data.Id))
                return;

            _activeNPCs.Add(data);
            _npcLookup[data.Id] = data;
            OnNPCSpawned?.Invoke(data);
        }

        /// <summary>
        /// Removes and pools an NPC by identifier.
        /// </summary>
        /// <param name="id">NPC unique identifier to remove.</param>
        public void DeregisterNPC(string id)
        {
            if (!_npcLookup.TryGetValue(id, out NPCAircraftData data))
                return;

            _activeNPCs.Remove(data);
            _npcLookup.Remove(id);
            ReturnToPool(data);
            OnNPCDespawned?.Invoke(id);
        }

        /// <summary>
        /// Returns the NPC data for the given identifier, or <c>null</c> if not found.
        /// </summary>
        /// <param name="id">NPC unique identifier.</param>
        public NPCAircraftData GetNPC(string id)
        {
            _npcLookup.TryGetValue(id, out NPCAircraftData data);
            return data;
        }

        /// <summary>
        /// Returns the nearest active NPC to the given world-space position.
        /// </summary>
        /// <param name="origin">World-space reference position.</param>
        /// <param name="maxRangeMetres">Search radius. Pass 0 for unlimited.</param>
        /// <returns>Nearest <see cref="NPCAircraftData"/>, or <c>null</c> if none found.</returns>
        public NPCAircraftData GetNearestNPC(Vector3 origin, float maxRangeMetres = 0f)
        {
            NPCAircraftData nearest      = null;
            float           nearestDistSq = float.MaxValue;

            foreach (NPCAircraftData npc in _activeNPCs)
            {
                float distSq = (npc.WorldPosition - origin).sqrMagnitude;
                if (maxRangeMetres > 0f && distSq > maxRangeMetres * maxRangeMetres)
                    continue;

                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest       = npc;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Overrides the player transform reference used for distance checks.
        /// </summary>
        /// <param name="playerTransform">New player transform.</param>
        public void SetPlayerTransform(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        /// <summary>
        /// Immediately despawns all active NPCs (e.g. on scene unload).
        /// </summary>
        public void ClearAllNPCs()
        {
            for (int i = _activeNPCs.Count - 1; i >= 0; i--)
            {
                string id = _activeNPCs[i].Id;
                _activeNPCs.RemoveAt(i);
                _npcLookup.Remove(id);
                OnNPCDespawned?.Invoke(id);
            }
        }

        #endregion

        #region Private — Coroutines

        private IEnumerator UpdateLoop()
        {
            var wait = new WaitForSeconds(_config.UpdateIntervalSeconds);
            while (true)
            {
                yield return wait;
                DespawnDistantNPCs();
                UpdateTimeOfDayDensity();
            }
        }

        private IEnumerator SpawnLoop()
        {
            var wait = new WaitForSeconds(_config.SpawnIntervalSeconds);
            while (true)
            {
                yield return wait;

                if (_activeNPCs.Count < EffectiveMaxNPCs && _playerTransform != null)
                    TrySpawnNPC();
            }
        }

        #endregion

        #region Private — Density & Spawning

        private void ApplyDensitySettings()
        {
            CurrentDensity = _config.Density;
            EffectiveMaxNPCs = GetBaseMaxForDensity(_config.Density);
            OnDensityChanged?.Invoke(CurrentDensity);
        }

        private void UpdateTimeOfDayDensity()
        {
            int hour = DateTime.Now.Hour;
            float multiplier = 1f;

            if (hour is >= 7 and <= 9 or >= 17 and <= 20)
                multiplier = _config.RushHourMultiplier;
            else if (hour is >= 23 or <= 5)
                multiplier = _config.NightMultiplier;

            EffectiveMaxNPCs = Mathf.Clamp(
                Mathf.RoundToInt(GetBaseMaxForDensity(_config.Density) * multiplier),
                0, _config.MaxActiveNPCs);
        }

        private static int GetBaseMaxForDensity(NPCTrafficDensity density) =>
            density switch
            {
                NPCTrafficDensity.None   => 0,
                NPCTrafficDensity.Sparse => 10,
                NPCTrafficDensity.Normal => 30,
                NPCTrafficDensity.Dense  => 60,
                _                       => 30
            };

        private void TrySpawnNPC()
        {
            if (_spawnController == null)
                return;

            NPCAircraftData npc = _spawnController.SpawnNear(_playerTransform.position, _config.SpawnRadiusMetres);
            if (npc != null)
                RegisterNPC(npc);
        }

        private void DespawnDistantNPCs()
        {
            if (_playerTransform == null)
                return;

            float maxDistSq = _config.DespawnRadiusMetres * _config.DespawnRadiusMetres;
            Vector3 playerPos = _playerTransform.position;

            for (int i = _activeNPCs.Count - 1; i >= 0; i--)
            {
                if ((_activeNPCs[i].WorldPosition - playerPos).sqrMagnitude > maxDistSq)
                    DeregisterNPC(_activeNPCs[i].Id);
            }
        }

        #endregion

        #region Private — Object Pool

        private NPCAircraftData RentFromPool()
        {
            return _pool.Count > 0 ? _pool.Dequeue() : new NPCAircraftData();
        }

        private void ReturnToPool(NPCAircraftData data)
        {
            // Reset mutable fields before pooling
            data.IsVisible    = false;
            data.BehaviorState = NPCBehaviorState.Parked;
            _pool.Enqueue(data);
        }

        #endregion

        #region Private — Reference Resolution

        private void ResolveReferences()
        {
            _spawnController = GetComponentInChildren<NPCSpawnController>();
            _routeGenerator  = GetComponentInChildren<NPCRouteGenerator>();

            if (_playerTransform == null)
            {
                // Attempt soft-resolve via string type name to avoid hard dependency
                Component fc = FindObjectOfType(
                    Type.GetType("SWEF.Flight.FlightController, Assembly-CSharp") ?? typeof(MonoBehaviour)) as Component;
                if (fc != null)
                    _playerTransform = fc.transform;
            }
        }

        #endregion

        #region Internal Helpers

        internal string GenerateId() => $"NPC_{++_idCounter:D6}";

        internal NPCFlightProfile GetProfile(NPCAircraftCategory category)
        {
            return _config.FlightProfiles?.FirstOrDefault(p => p.Category == category)
                ?? new NPCFlightProfile { Category = category, CruiseSpeedKnots = 200f };
        }

        #endregion
    }
}
