using UnityEngine;

namespace SWEF.LOD
{
    /// <summary>
    /// Lightweight runtime occlusion helper for terrain chunks.
    /// Combines frustum culling, distance culling, and altitude-based culling
    /// to determine chunk visibility without Unity's built-in occlusion baking.
    /// </summary>
    public class OcclusionCullingHelper : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Culling Distances")]
        [SerializeField] private float maxRenderDistance   = 20000f;
        [SerializeField] private float altitudeCullAlt     = 50000f;  // player altitude above which distant chunks are culled
        [SerializeField] private float altitudeCullFraction = 0.3f;   // fraction of maxRenderDistance applied at high altitude

        // ── Stats ────────────────────────────────────────────────────────────────
        /// <summary>Chunks determined visible this cycle.</summary>
        public int VisibleChunks       { get; private set; }

        /// <summary>Total chunks tested this cycle (visible + culled).</summary>
        public int CulledChunks        { get; private set; }

        /// <summary>Chunks removed by frustum test this cycle.</summary>
        public int FrustumCulledCount  { get; private set; }

        // ── Internal state ───────────────────────────────────────────────────────
        private Plane[] _frustumPlanes = new Plane[6];
        private Camera  _mainCamera;

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>
        /// Updates the frustum planes from <see cref="Camera.main"/>.
        /// Should be called once per LOD update cycle before batch IsVisible queries.
        /// </summary>
        public void RefreshCullingPlanes()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera != null)
                GeometryUtility.CalculateFrustumPlanes(_mainCamera, _frustumPlanes);
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="bounds"/> should be rendered
        /// given the player's current distance and altitude.
        /// </summary>
        public bool IsVisible(Bounds bounds, float distance, float playerAltitude)
        {
            // Distance culling
            float effectiveMax = maxRenderDistance;
            if (playerAltitude > altitudeCullAlt)
                effectiveMax *= altitudeCullFraction;

            if (distance > effectiveMax)
            {
                CulledChunks++;
                return false;
            }

            // Frustum culling
            if (_frustumPlanes != null && !GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds))
            {
                FrustumCulledCount++;
                CulledChunks++;
                return false;
            }

            VisibleChunks++;
            return true;
        }

        /// <summary>Resets per-cycle stats counters.</summary>
        public void ResetStats()
        {
            VisibleChunks      = 0;
            CulledChunks       = 0;
            FrustumCulledCount = 0;
        }
    }
}
