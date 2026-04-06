// CityChunkManager.cs — Phase 113: Procedural City & Airport Generation
// Chunk management: persistent seed-based generation (same location = same city), chunk caching.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Manages the lifecycle of city chunks: maintains a cache of loaded
    /// <see cref="CityDescription"/> data keyed by <see cref="ChunkCoord"/>,
    /// ensuring the same seed always produces the same city.
    /// </summary>
    public class CityChunkManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Cache")]
        [Tooltip("Maximum number of chunks kept in the LRU cache.")]
        [SerializeField] private int maxCachedChunks = 64;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly Dictionary<ChunkCoord, CityDescription> _cache =
            new Dictionary<ChunkCoord, CityDescription>();

        private readonly Dictionary<ChunkCoord, GameObject> _chunkRoots =
            new Dictionary<ChunkCoord, GameObject>();

        private readonly LinkedList<ChunkCoord> _lruOrder = new LinkedList<ChunkCoord>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads (or generates) the city chunk at <paramref name="coord"/>
        /// and activates its root GameObject.
        /// </summary>
        public void LoadChunk(ChunkCoord coord)
        {
            if (_chunkRoots.TryGetValue(coord, out var existing))
            {
                if (existing != null) existing.SetActive(true);
                return;
            }

            var city = GetOrGenerate(coord);
            var root = new GameObject($"CityChunk_{coord}");
            root.transform.position = city.centre;
            _chunkRoots[coord] = root;
            TouchLRU(coord);
        }

        /// <summary>
        /// Deactivates the chunk root for <paramref name="coord"/> without destroying it.
        /// </summary>
        public void UnloadChunk(ChunkCoord coord)
        {
            if (_chunkRoots.TryGetValue(coord, out var root))
                if (root != null) root.SetActive(false);
        }

        /// <summary>Returns the cached <see cref="CityDescription"/> for <paramref name="coord"/>, or generates one.</summary>
        public CityDescription GetOrGenerate(ChunkCoord coord)
        {
            if (_cache.TryGetValue(coord, out var cached)) return cached;

            int seed = SeedFromCoord(coord);
            float chunkSize = ProceduralWorldManager.Instance?.Config?.chunkSizeMetres ?? 2000f;
            var centre = new Vector3((coord.x + 0.5f) * chunkSize, 0f, (coord.z + 0.5f) * chunkSize);

            var city = new CityDescription
            {
                seed = seed,
                cityName = $"City_{coord.x}_{coord.z}",
                cityType = CityType.Town,
                centre = centre,
                radiusMetres = chunkSize * 0.4f,
                population = seed % 50000 + 500
            };

            CacheChunk(coord, city);
            return city;
        }

        /// <summary>Returns the number of entries currently in the chunk cache.</summary>
        public int CachedCount => _cache.Count;

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void CacheChunk(ChunkCoord coord, CityDescription city)
        {
            if (_cache.ContainsKey(coord)) return;
            _cache[coord] = city;
            TouchLRU(coord);
            EvictIfNeeded();
        }

        private void TouchLRU(ChunkCoord coord)
        {
            _lruOrder.Remove(coord);
            _lruOrder.AddFirst(coord);
        }

        private void EvictIfNeeded()
        {
            while (_cache.Count > maxCachedChunks && _lruOrder.Count > 0)
            {
                var oldest = _lruOrder.Last.Value;
                _lruOrder.RemoveLast();
                _cache.Remove(oldest);
                if (_chunkRoots.TryGetValue(oldest, out var root))
                {
                    if (root != null) Destroy(root);
                    _chunkRoots.Remove(oldest);
                }
            }
        }

        private static int SeedFromCoord(ChunkCoord coord)
        {
            // Cantor pairing to get a unique integer per coordinate
            int a = coord.x >= 0 ? 2 * coord.x : -2 * coord.x - 1;
            int b = coord.z >= 0 ? 2 * coord.z : -2 * coord.z - 1;
            return (a + b) * (a + b + 1) / 2 + b;
        }
    }
}
