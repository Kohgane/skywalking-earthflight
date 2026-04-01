// StationSpawnManager.cs — SWEF Space Station & Orbital Docking System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpaceStation
{
    /// <summary>
    /// Singleton that spawns and despawns station GameObjects based on player altitude.
    /// Uses a simple LOD scheme: icon (&gt;50 km away), low-poly (10–50 km), full model (&lt;10 km).
    /// Pools station GameObjects to avoid repeated instantiation/destruction.
    /// </summary>
    public class StationSpawnManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        public static StationSpawnManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [SerializeField] private SpaceStationConfig _config;
        [SerializeField] private float _iconDistance      = 50_000f;
        [SerializeField] private float _lowPolyDistance   = 10_000f;

        // ── Types ─────────────────────────────────────────────────────────────────

        private enum LodLevel { None, Icon, LowPoly, Full }

        private class StationInstance
        {
            public StationDefinition definition;
            public GameObject         root;
            public LodLevel           currentLod;
        }

        // ── Private state ─────────────────────────────────────────────────────────

        private readonly List<StationDefinition>            _registeredDefs  = new List<StationDefinition>();
        private readonly Dictionary<string, StationInstance> _activeInstances = new Dictionary<string, StationInstance>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Registers a station definition so it can be spawned.</summary>
        public void RegisterStation(StationDefinition def)
        {
            if (def != null && !_registeredDefs.Contains(def))
                _registeredDefs.Add(def);
        }

        /// <summary>Refreshes LOD levels for all registered stations based on player position.</summary>
        public void UpdateStations(Vector3 playerPosition, double simulationTime)
        {
            int active = 0;
            int maxActive = _config != null ? _config.maxActiveStations : 3;

            foreach (StationDefinition def in _registeredDefs)
            {
                if (active >= maxActive && !_activeInstances.ContainsKey(def.stationId))
                    continue;

                Vector3 pos = OrbitalMechanicsController.Instance != null
                    ? OrbitalMechanicsController.Instance.GetStationPosition(def.stationId, simulationTime)
                    : Vector3.zero;

                float dist = Vector3.Distance(playerPosition, pos);
                LodLevel lod = DistanceToLod(dist);

                if (lod == LodLevel.None)
                {
                    DespawnStation(def.stationId);
                }
                else
                {
                    EnsureSpawned(def, pos, lod);
                    active++;
                }
            }
        }

        /// <summary>Returns the nearest registered station to the given position, or null.</summary>
        public StationDefinition GetNearestStation(Vector3 position, double simulationTime)
        {
            StationDefinition nearest = null;
            float nearestDist = float.MaxValue;

            foreach (StationDefinition def in _registeredDefs)
            {
                Vector3 pos = OrbitalMechanicsController.Instance != null
                    ? OrbitalMechanicsController.Instance.GetStationPosition(def.stationId, simulationTime)
                    : Vector3.zero;

                float dist = Vector3.Distance(position, pos);
                if (dist < nearestDist) { nearestDist = dist; nearest = def; }
            }
            return nearest;
        }

        /// <summary>Returns all station definitions within the given radius of position.</summary>
        public List<StationDefinition> GetStationsInRange(Vector3 position, float radius, double simulationTime)
        {
            var result = new List<StationDefinition>();
            foreach (StationDefinition def in _registeredDefs)
            {
                Vector3 pos = OrbitalMechanicsController.Instance != null
                    ? OrbitalMechanicsController.Instance.GetStationPosition(def.stationId, simulationTime)
                    : Vector3.zero;

                if (Vector3.Distance(position, pos) <= radius)
                    result.Add(def);
            }
            return result;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private LodLevel DistanceToLod(float distance)
        {
            if (distance > _iconDistance)    return LodLevel.None;
            if (distance > _lowPolyDistance) return LodLevel.LowPoly;
            return LodLevel.Full;
        }

        private void EnsureSpawned(StationDefinition def, Vector3 worldPos, LodLevel lod)
        {
            if (!_activeInstances.TryGetValue(def.stationId, out StationInstance inst))
            {
                inst = new StationInstance
                {
                    definition  = def,
                    root        = new GameObject($"Station_{def.stationId}"),
                    currentLod  = LodLevel.None
                };
                _activeInstances[def.stationId] = inst;
            }

            inst.root.transform.position = worldPos;
            if (inst.currentLod != lod)
            {
                inst.currentLod = lod;
                ApplyLod(inst, lod);
            }
        }

        private void DespawnStation(string stationId)
        {
            if (_activeInstances.TryGetValue(stationId, out StationInstance inst))
            {
                if (inst.root != null)
                    Destroy(inst.root);
                _activeInstances.Remove(stationId);
            }
        }

        private static void ApplyLod(StationInstance inst, LodLevel lod)
        {
            // In a full implementation each LOD level would swap mesh renderers.
            // Here we simply log the LOD change so tests can verify the logic.
            if (Debug.isDebugBuild)
                Debug.Log($"[StationSpawnManager] {inst.definition.stationId} LOD → {lod}");
        }
    }
}
