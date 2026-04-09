using UnityEngine;

namespace SWEF.Terrain
{
    /// <summary>
    /// MonoBehaviour attached to each procedural terrain chunk GameObject.
    /// Manages its own MeshFilter, MeshRenderer, and MeshCollider,
    /// and stores up to four pre-generated LOD meshes.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class TerrainChunk : MonoBehaviour
    {
        // ── Public state ─────────────────────────────────────────────────────────
        /// <summary>Grid coordinate of this chunk in chunk-space.</summary>
        public Vector2Int ChunkCoord        { get; internal set; }

        /// <summary>Raw height samples (length = resolution × resolution).</summary>
        public float[]    HeightData        { get; internal set; }

        /// <summary>Currently active LOD level.</summary>
        public TerrainLODLevel CurrentLOD   { get; private set; } = TerrainLODLevel.Full;

        /// <summary>Whether the chunk renderer is currently visible.</summary>
        public bool        IsVisible        { get; private set; }

        /// <summary>Cached world-space distance to the player this frame.</summary>
        public float       DistanceToPlayer { get; internal set; }

        // ── Component cache ──────────────────────────────────────────────────────
        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        // ── LOD meshes ───────────────────────────────────────────────────────────
        // Index maps directly to (int)TerrainLODLevel (0–3); Culled handled by SetVisible.
        private readonly Mesh[] _lodMeshes = new Mesh[4];

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            _meshFilter   = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
        }

        // ── LOD mesh registration ────────────────────────────────────────────────
        /// <summary>
        /// Stores a pre-built mesh for the given LOD level.
        /// Called by <see cref="ProceduralTerrainGenerator"/> after mesh construction.
        /// </summary>
        public void SetLODMesh(TerrainLODLevel level, Mesh mesh)
        {
            int idx = (int)level;
            if (idx >= 0 && idx < _lodMeshes.Length)
                _lodMeshes[idx] = mesh;
        }

        // ── LOD switching ────────────────────────────────────────────────────────
        /// <summary>
        /// Swaps the active mesh to the specified LOD level.
        /// Culled level is handled by <see cref="SetVisible"/>.
        /// </summary>
        public void UpdateLOD(TerrainLODLevel level)
        {
            if (level == TerrainLODLevel.Culled)
            {
                SetVisible(false);
                CurrentLOD = level;
                return;
            }

            int idx = (int)level;
            Mesh target = (idx >= 0 && idx < _lodMeshes.Length) ? _lodMeshes[idx] : null;
            if (target == null) return;

            _meshFilter.sharedMesh = target;
            if (_meshCollider != null && level == TerrainLODLevel.Full)
                _meshCollider.sharedMesh = target;

            SetVisible(true);
            CurrentLOD = level;
        }

        // ── Visibility ───────────────────────────────────────────────────────────
        /// <summary>Enables or disables the MeshRenderer (and MeshCollider) for this chunk.</summary>
        public void SetVisible(bool visible)
        {
            if (_meshRenderer != null)
                _meshRenderer.enabled = visible;
            if (_meshCollider != null)
                _meshCollider.enabled = visible;
            IsVisible = visible;
        }

        // ── Pooling ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Resets this chunk so it can be returned to the pool and reused.
        /// Does NOT destroy the LOD meshes — they are replaced on next generation.
        /// </summary>
        public void Recycle()
        {
            ChunkCoord       = default;
            HeightData       = null;
            DistanceToPlayer = 0f;
            CurrentLOD       = TerrainLODLevel.Full;
            IsVisible        = false;

            // Clear mesh references but keep Mesh objects alive for GC
            if (_meshFilter != null)
                _meshFilter.sharedMesh = null;
            if (_meshCollider != null)
                _meshCollider.sharedMesh = null;
            if (_meshRenderer != null)
                _meshRenderer.enabled = false;

            for (int i = 0; i < _lodMeshes.Length; i++)
                _lodMeshes[i] = null;

            gameObject.SetActive(false);
        }
    }
}
