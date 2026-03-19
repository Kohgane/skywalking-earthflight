using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Terrain
{
    /// <summary>
    /// Object pool dedicated to <see cref="TerrainChunk"/> GameObjects.
    /// Pre-instantiates <see cref="poolSize"/> chunks at startup and auto-expands
    /// with a warning when the pool is exhausted.
    /// </summary>
    public class TerrainChunkPool : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Pool Settings")]
        [SerializeField] private int    poolSize       = 50;
        [SerializeField] private string chunkLayerName = "Terrain";

        // ── Internal state ───────────────────────────────────────────────────────
        private readonly Queue<TerrainChunk> _available = new Queue<TerrainChunk>();
        private int _totalCreated;

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            PreWarm(poolSize);
        }

        // ── API ──────────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns an inactive <see cref="TerrainChunk"/> from the pool.
        /// If the pool is empty, a new chunk is created and a warning is logged.
        /// </summary>
        public TerrainChunk Get()
        {
            if (_available.Count == 0)
            {
                Debug.LogWarning("[SWEF] TerrainChunkPool exhausted — expanding pool. Consider increasing poolSize.");
                CreateChunk();
            }

            TerrainChunk chunk = _available.Dequeue();
            chunk.gameObject.SetActive(true);
            return chunk;
        }

        /// <summary>
        /// Returns a <see cref="TerrainChunk"/> to the pool.
        /// Calls <see cref="TerrainChunk.Recycle"/> to reset state.
        /// </summary>
        public void Return(TerrainChunk chunk)
        {
            if (chunk == null) return;
            chunk.Recycle();
            _available.Enqueue(chunk);
        }

        /// <summary>Number of chunks currently available in the pool.</summary>
        public int AvailableCount => _available.Count;

        /// <summary>Total chunks ever created by this pool.</summary>
        public int TotalCreated => _totalCreated;

        // ── Internal helpers ─────────────────────────────────────────────────────
        private void PreWarm(int count)
        {
            for (int i = 0; i < count; i++)
                CreateChunk();
        }

        private void CreateChunk()
        {
            var go = new GameObject($"TerrainChunk_{_totalCreated:D4}");
            go.transform.SetParent(transform, false);

            // Assign layer if available
            int layer = LayerMask.NameToLayer(chunkLayerName);
            if (layer >= 0)
                go.layer = layer;
            else if (!string.IsNullOrEmpty(chunkLayerName))
                Debug.LogWarning($"[SWEF] TerrainChunkPool: Layer '{chunkLayerName}' not found — chunk will use Default layer. Create it in Project Settings → Tags & Layers.");

            // Ensure required components are present
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();

            var chunk = go.AddComponent<TerrainChunk>();
            go.SetActive(false);

            _available.Enqueue(chunk);
            _totalCreated++;
        }
    }
}
