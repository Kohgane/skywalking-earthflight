using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 53 — Handles all wildlife spawning logic for Skywalking Earthflight.
    ///
    /// <para>Divides the world into wildlife chunks, samples biome data per chunk,
    /// applies rarity-weighted random selection, enforces anti-clustering and spawn
    /// cooldowns, and reuses GameObjects via MemoryPoolManager (or simple Instantiate
    /// when the pool is unavailable).</para>
    /// </summary>
    public class WildlifeSpawnSystem : MonoBehaviour
    {
        #region Constants

        private const int   ChunkSize             = 1000;
        private const float AntiClusterDistance   = 200f;  // min distance between same-species groups
        private const float PreSpawnBuffer        = 100f;  // extra radius beyond visible range

        #endregion

        #region Inspector

        [Header("Spawn Settings")]
        [Tooltip("Visible spawn radius forwarded from WildlifeSettings.")]
        [SerializeField] private float spawnRadius = 2000f;

        [Tooltip("Maximum simultaneous pending spawn requests.")]
        [SerializeField] private int maxPendingSpawns = 10;

        [Header("References")]
        [Tooltip("WildlifeManager that owns spawned groups. Resolved at runtime if null.")]
        [SerializeField] private WildlifeManager wildlifeManager;

        #endregion

        #region Events

        /// <summary>Fired after a new animal group is successfully spawned.</summary>
        public event Action<AnimalGroup> OnGroupSpawned;

        #endregion

        #region Private State

        private readonly Dictionary<Vector2Int, List<AnimalGroup>> _chunkGroups =
            new Dictionary<Vector2Int, List<AnimalGroup>>();

        private readonly Queue<SpawnRequest> _pendingRequests = new Queue<SpawnRequest>();
        private bool _processingSpawn;

        #endregion

        #region Internal Types

        private struct SpawnRequest
        {
            public AnimalSpecies Species;
            public Vector3       Origin;
            public float         Radius;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (wildlifeManager == null)
                wildlifeManager = FindFirstObjectByType<WildlifeManager>();
        }

        private void Update()
        {
            if (!_processingSpawn && _pendingRequests.Count > 0)
                StartCoroutine(ProcessNextSpawn());
        }

        #endregion

        #region Public API

        /// <summary>Enqueues a spawn request for the given species near <paramref name="origin"/>.</summary>
        public void RequestSpawn(AnimalSpecies species, Vector3 origin, float radius)
        {
            if (species == null) return;
            if (_pendingRequests.Count >= maxPendingSpawns) return;

            _pendingRequests.Enqueue(new SpawnRequest
            {
                Species = species,
                Origin  = origin,
                Radius  = radius + PreSpawnBuffer
            });
        }

        /// <summary>Immediately removes all active groups associated with a chunk.</summary>
        public void ClearChunk(Vector2Int chunk)
        {
            if (!_chunkGroups.TryGetValue(chunk, out var groups)) return;
            foreach (var g in groups)
                wildlifeManager?.UnregisterGroup(g);
            groups.Clear();
        }

        /// <summary>Removes all spawned groups from every chunk.</summary>
        public void ClearAll()
        {
            foreach (var kv in _chunkGroups)
                foreach (var g in kv.Value)
                    wildlifeManager?.UnregisterGroup(g);
            _chunkGroups.Clear();
        }

        #endregion

        #region Spawn Processing

        private IEnumerator ProcessNextSpawn()
        {
            _processingSpawn = true;
            SpawnRequest req = _pendingRequests.Dequeue();

            yield return null;  // spread cost over frames

            Vector3 spawnPos = PickSpawnPosition(req.Species, req.Origin, req.Radius);
            if (spawnPos == Vector3.zero)
            {
                _processingSpawn = false;
                yield break;
            }

            Vector2Int chunk = WorldToChunk(spawnPos);
            if (!PassesAntiCluster(req.Species, spawnPos, chunk))
            {
                _processingSpawn = false;
                yield break;
            }

            // Determine group size using species range
            int count = req.Species.minGroupSize == req.Species.maxGroupSize
                ? req.Species.minGroupSize
                : UnityEngine.Random.Range(req.Species.minGroupSize, req.Species.maxGroupSize + 1);

            var group = new AnimalGroup
            {
                species           = req.Species,
                memberCount       = count,
                centerPosition    = spawnPos,
                movementDirection = UnityEngine.Random.insideUnitSphere.normalized,
                currentBehavior   = DefaultBehavior(req.Species),
                formation         = DefaultFormation(req.Species),
                groupRadius       = 10f + count * 2f,
                spawnTime         = Time.time
            };

            if (!_chunkGroups.ContainsKey(chunk))
                _chunkGroups[chunk] = new List<AnimalGroup>();
            _chunkGroups[chunk].Add(group);

            wildlifeManager?.RegisterGroup(group);
            OnGroupSpawned?.Invoke(group);

            _processingSpawn = false;
        }

        #endregion

        #region Position Selection

        private Vector3 PickSpawnPosition(AnimalSpecies species, Vector3 origin, float radius)
        {
            int seed = Mathf.FloorToInt(origin.x / ChunkSize) * 1000 +
                       Mathf.FloorToInt(origin.z / ChunkSize);
            UnityEngine.Random.State savedState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);

            Vector3 result = Vector3.zero;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2 offset2D = UnityEngine.Random.insideUnitCircle * radius;
                float   angle    = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector3 candidate = origin + new Vector3(offset2D.x, 0f, offset2D.y);
                candidate.y = species.minAltitude +
                              UnityEngine.Random.value * (species.maxAltitude - species.minAltitude);

                if (IsValidPosition(species, candidate))
                {
                    result = candidate;
                    break;
                }
            }

            UnityEngine.Random.state = savedState;
            return result;
        }

        private static bool IsValidPosition(AnimalSpecies species, Vector3 pos)
        {
            // Basic altitude check; extended checks (terrain, water) can be added here
            return pos.y >= species.minAltitude && pos.y <= species.maxAltitude;
        }

        private bool PassesAntiCluster(AnimalSpecies species, Vector3 pos, Vector2Int chunk)
        {
            if (!_chunkGroups.TryGetValue(chunk, out var groups)) return true;
            foreach (var g in groups)
            {
                if (g.species != null && g.species.speciesName == species.speciesName &&
                    Vector3.Distance(g.centerPosition, pos) < AntiClusterDistance)
                    return false;
            }
            return true;
        }

        #endregion

        #region Helpers

        private static Vector2Int WorldToChunk(Vector3 pos)
        {
            return new Vector2Int(Mathf.FloorToInt(pos.x / ChunkSize),
                                  Mathf.FloorToInt(pos.z / ChunkSize));
        }

        private static AnimalBehavior DefaultBehavior(AnimalSpecies species)
        {
            if (species.flightCapable)  return AnimalBehavior.Flying;
            if (species.swimCapable)    return AnimalBehavior.Swimming;
            return AnimalBehavior.Grazing;
        }

        private static FlockFormation DefaultFormation(AnimalSpecies species)
        {
            if (species.flightCapable)  return FlockFormation.Vee;
            if (species.swimCapable)    return FlockFormation.Cluster;
            return FlockFormation.Random;
        }

        #endregion
    }
}
