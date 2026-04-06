// WorldStreamer.cs — Phase 113: Procedural City & Airport Generation
// Tile-based world streaming: load/unload city chunks based on player distance, async loading.
// Namespace: SWEF.ProceduralWorld

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Manages tile-based world streaming for procedural cities and airports.
    /// Continuously loads chunks near the player and unloads distant chunks
    /// to keep memory usage bounded.
    /// </summary>
    public class WorldStreamer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Player Reference")]
        [SerializeField] private Transform playerTransform;

        [Header("Streaming")]
        [Tooltip("How often (seconds) the streamer checks for chunk changes.")]
        [SerializeField] private float updateInterval = 2f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when a chunk begins loading.</summary>
        public event System.Action<ChunkCoord> OnChunkLoading;

        /// <summary>Raised when a chunk has been unloaded.</summary>
        public event System.Action<ChunkCoord> OnChunkUnloaded;

        // ── Private state ─────────────────────────────────────────────────────────
        private ProceduralWorldConfig _config;
        private CityChunkManager _chunkManager;
        private readonly HashSet<ChunkCoord> _visibleChunks = new HashSet<ChunkCoord>();
        private float _nextUpdate;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _config = ProceduralWorldManager.Instance != null
                ? ProceduralWorldManager.Instance.Config
                : ScriptableObject.CreateInstance<ProceduralWorldConfig>();
            _chunkManager = GetComponent<CityChunkManager>();
        }

        private void Update()
        {
            if (Time.time < _nextUpdate) return;
            _nextUpdate = Time.time + updateInterval;

            if (playerTransform == null) return;
            StartCoroutine(UpdateChunks());
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the player transform used as the streaming origin.</summary>
        public void SetPlayer(Transform player) => playerTransform = player;

        /// <summary>Returns the chunk coordinate that contains <paramref name="worldPos"/>.</summary>
        public ChunkCoord WorldToChunk(Vector3 worldPos)
        {
            float size = _config != null ? _config.chunkSizeMetres : 2000f;
            return new ChunkCoord(
                Mathf.FloorToInt(worldPos.x / size),
                Mathf.FloorToInt(worldPos.z / size));
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private IEnumerator UpdateChunks()
        {
            float chunkSize = _config != null ? _config.chunkSizeMetres : 2000f;
            float unloadDist = _config != null ? _config.chunkUnloadDistance : 20000f;
            int preload = _config != null ? _config.preloadRadius : 2;

            var playerChunk = WorldToChunk(playerTransform.position);
            var desired = new HashSet<ChunkCoord>();

            for (int dx = -preload; dx <= preload; dx++)
            {
                for (int dz = -preload; dz <= preload; dz++)
                {
                    var coord = new ChunkCoord(playerChunk.x + dx, playerChunk.z + dz);
                    Vector3 chunkCentre = new Vector3(
                        (coord.x + 0.5f) * chunkSize, 0f, (coord.z + 0.5f) * chunkSize);

                    if (Vector3.Distance(playerTransform.position, chunkCentre) <= unloadDist)
                        desired.Add(coord);
                }
            }

            // Load new chunks
            foreach (var coord in desired)
            {
                if (_visibleChunks.Contains(coord)) continue;
                _visibleChunks.Add(coord);
                ProceduralWorldManager.Instance?.RegisterChunk(coord);
                OnChunkLoading?.Invoke(coord);
                _chunkManager?.LoadChunk(coord);
                yield return null;
            }

            // Unload distant chunks
            var toUnload = new List<ChunkCoord>();
            foreach (var coord in _visibleChunks)
                if (!desired.Contains(coord)) toUnload.Add(coord);

            foreach (var coord in toUnload)
            {
                _visibleChunks.Remove(coord);
                ProceduralWorldManager.Instance?.UnregisterChunk(coord);
                _chunkManager?.UnloadChunk(coord);
                OnChunkUnloaded?.Invoke(coord);
            }
        }
    }
}
