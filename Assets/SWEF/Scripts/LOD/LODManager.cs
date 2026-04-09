using System;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Performance;
using SWEF.Terrain;

namespace SWEF.LOD
{
    /// <summary>
    /// MonoBehaviour singleton that manages all LOD decisions for terrain chunks.
    /// Distance thresholds are altitude-aware and can be shifted by
    /// <see cref="AdaptiveQualityController"/> when performance degrades.
    /// </summary>
    public class LODManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static LODManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("LOD Distance Thresholds (metres)")]
        [SerializeField] private float[] lodDistances = { 500f, 2000f, 8000f, 20000f };

        [Header("Update Rate")]
        [SerializeField] private int updateIntervalFrames = 10;

        [Header("Altitude Scaling")]
        [SerializeField] private float altitudeLODScaleBase = 5000f;

        [Header("Refs")]
        [SerializeField] private Transform playerTransform;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired when a chunk's LOD level changes.</summary>
        public event Action<TerrainChunk, TerrainLODLevel, TerrainLODLevel> OnLODChanged;

        // ── Public stats ─────────────────────────────────────────────────────────
        public int TotalActiveChunks  { get; private set; }
        public int CulledChunks       { get; private set; }
        public long MemoryEstimateBytes { get; private set; }

        // ── Internal state ───────────────────────────────────────────────────────
        private int _frameCounter;
        private AdaptiveQualityController _aqc;
        private OcclusionCullingHelper    _occlusion;

        // Registered chunks (ProceduralTerrainGenerator registers via AddChunk/RemoveChunk)
        private readonly List<TerrainChunk> _chunks = new List<TerrainChunk>();

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _aqc       = FindFirstObjectByType<AdaptiveQualityController>();
            _occlusion = FindFirstObjectByType<OcclusionCullingHelper>();
        }

        private void Start()
        {
            if (playerTransform == null)
                playerTransform = Camera.main?.transform;
        }

        private void Update()
        {
            _frameCounter++;
            if (_frameCounter < updateIntervalFrames) return;
            _frameCounter = 0;
            UpdateAllLODs();
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>Registers a chunk so the LODManager tracks it each update cycle.</summary>
        public void AddChunk(TerrainChunk chunk)    { if (!_chunks.Contains(chunk)) _chunks.Add(chunk); }

        /// <summary>Unregisters a chunk.</summary>
        public void RemoveChunk(TerrainChunk chunk) { _chunks.Remove(chunk); }

        /// <summary>
        /// Returns the appropriate <see cref="TerrainLODLevel"/> for the given
        /// world-space distance and player altitude.
        /// </summary>
        public TerrainLODLevel GetLODLevel(float distance, float playerAltitude)
        {
            float[] thresholds = GetScaledThresholds(playerAltitude);
            if (distance < thresholds[0]) return TerrainLODLevel.Full;
            if (distance < thresholds[1]) return TerrainLODLevel.Half;
            if (distance < thresholds[2]) return TerrainLODLevel.Quarter;
            if (distance < thresholds[3]) return TerrainLODLevel.Minimal;
            return TerrainLODLevel.Culled;
        }

        /// <summary>Forces an immediate LOD update for all registered chunks.</summary>
        public void UpdateAllLODs()
        {
            if (playerTransform == null) return;

            float playerAltitude = playerTransform.position.y;
            Vector3 playerPos    = playerTransform.position;

            int culled = 0;
            long memBytes = 0;

            _occlusion?.RefreshCullingPlanes();

            foreach (var chunk in _chunks)
            {
                if (chunk == null) continue;

                float dist = Vector3.Distance(playerPos, chunk.transform.position);
                chunk.DistanceToPlayer = dist;

                TerrainLODLevel prev = chunk.CurrentLOD;

                // Ask occlusion helper first
                bool visible = _occlusion == null || _occlusion.IsVisible(
                    chunk.GetComponent<MeshRenderer>()?.bounds ?? new Bounds(chunk.transform.position, Vector3.one * 256),
                    dist, playerAltitude);

                TerrainLODLevel next = visible
                    ? GetLODLevel(dist, playerAltitude)
                    : TerrainLODLevel.Culled;

                if (next != prev)
                {
                    chunk.UpdateLOD(next);
                    OnLODChanged?.Invoke(chunk, prev, next);
                }

                if (next == TerrainLODLevel.Culled) culled++;
                memBytes += EstimateChunkMemory(next);
            }

            TotalActiveChunks   = _chunks.Count;
            CulledChunks        = culled;
            MemoryEstimateBytes = memBytes;
        }

        // ── Inspector-accessible threshold array ─────────────────────────────────
        /// <summary>Direct access to the raw LOD distance thresholds (for editor tooling).</summary>
        public float[] LODDistances => lodDistances;

        // ── Helpers ──────────────────────────────────────────────────────────────
        private float[] GetScaledThresholds(float playerAltitude)
        {
            // Scale distances proportionally with altitude so distant chunks stay visible
            float altScale = 1f + Mathf.Max(0f, playerAltitude / altitudeLODScaleBase);

            // If adaptive quality is under pressure, tighten thresholds
            float qualScale = 1f;
            if (_aqc != null)
            {
                float score = Mathf.Clamp01(_aqc.CurrentQualityScore / 100f); // 0 = low quality, 1 = ultra
                qualScale = Mathf.Lerp(0.5f, 1f, score);
            }

            float finalScale = altScale * qualScale;
            float[] scaled   = new float[lodDistances.Length];
            for (int i = 0; i < lodDistances.Length; i++)
                scaled[i] = lodDistances[i] * finalScale;
            return scaled;
        }

        private static long EstimateChunkMemory(TerrainLODLevel level)
        {
            // Rough vertex-count estimates per LOD × 32 bytes per vertex
            return level switch
            {
                TerrainLODLevel.Full    => 257L * 257 * 32,
                TerrainLODLevel.Half    => 129L * 129 * 32,
                TerrainLODLevel.Quarter =>  65L *  65 * 32,
                TerrainLODLevel.Minimal =>  33L *  33 * 32,
                _                       => 0L
            };
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
