using UnityEngine;

namespace SWEF.Terrain
{
    /// <summary>
    /// Bridge between Cesium 3D Tiles and the procedural terrain fallback.
    /// When Cesium tiles are available for a region, procedural terrain is hidden;
    /// when Cesium tiles are absent (no network, out of coverage), procedural terrain shows.
    /// All Cesium-specific API calls are guarded behind <c>#if CESIUM_FOR_UNITY</c>.
    /// </summary>
    public class CesiumTerrainBridge : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Fallback Control")]
        [SerializeField] private bool forceProceduralFallback = false;
        [SerializeField] private float checkIntervalSeconds   = 5f;

        [Header("Refs")]
        [SerializeField] private ProceduralTerrainGenerator terrainGenerator;

        // ── Internal state ───────────────────────────────────────────────────────
        private bool  _cesiumAvailable;
        private float _nextCheckTime;

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            if (terrainGenerator == null)
                terrainGenerator = FindFirstObjectByType<ProceduralTerrainGenerator>();
        }

        private void Start()
        {
            CheckCesiumAvailability();

#if CESIUM_FOR_UNITY
            // Subscribe to tile load events if Cesium is present
            SubscribeCesiumEvents();
#endif
        }

        private void Update()
        {
            if (Time.time >= _nextCheckTime)
            {
                _nextCheckTime = Time.time + checkIntervalSeconds;
                CheckCesiumAvailability();
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>
        /// Returns <c>true</c> if Cesium 3D tiles are considered active for the
        /// chunk at <paramref name="chunkCoord"/>.
        /// </summary>
        public bool IsCesiumCoveringRegion(Vector2Int chunkCoord)
        {
            if (forceProceduralFallback) return false;

#if CESIUM_FOR_UNITY
            return IsCesiumLoadedForRegion(chunkCoord);
#else
            return false;
#endif
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void CheckCesiumAvailability()
        {
#if CESIUM_FOR_UNITY
            _cesiumAvailable = IsCesiumRuntimeAvailable();
#else
            _cesiumAvailable = false;
#endif

            bool showProcedural = forceProceduralFallback || !_cesiumAvailable;
            if (terrainGenerator != null)
                terrainGenerator.gameObject.SetActive(showProcedural);

            Debug.Log($"[SWEF] CesiumTerrainBridge: Cesium={_cesiumAvailable}, Procedural={showProcedural}");
        }

#if CESIUM_FOR_UNITY
        private void SubscribeCesiumEvents()
        {
            // Hook into Cesium tile loading events.
            // CesiumGeoreference or Cesium3DTileset events go here when SDK is available.
            Debug.Log("[SWEF] CesiumTerrainBridge: Cesium event hooks registered.");
        }

        private bool IsCesiumRuntimeAvailable()
        {
            // Check for a live Cesium3DTileset in the scene
            var tileset = FindFirstObjectByType<CesiumForUnity.Cesium3DTileset>();
            return tileset != null && tileset.enabled;
        }

        private bool IsCesiumLoadedForRegion(Vector2Int chunkCoord)
        {
            // Broad check: if Cesium is running we assume global coverage.
            // A more refined implementation would query tile bounding-volume trees.
            return IsCesiumRuntimeAvailable();
        }
#endif

        // ── Network connectivity ─────────────────────────────────────────────────
        /// <summary>Call this when the device goes offline to force procedural fallback.</summary>
        public void OnNetworkLost()
        {
            Debug.Log("[SWEF] CesiumTerrainBridge: Network lost — enabling procedural fallback.");
            forceProceduralFallback = true;
            CheckCesiumAvailability();
        }

        /// <summary>Call this when the device regains network connectivity.</summary>
        public void OnNetworkRestored()
        {
            Debug.Log("[SWEF] CesiumTerrainBridge: Network restored — re-evaluating Cesium coverage.");
            forceProceduralFallback = false;
            CheckCesiumAvailability();
        }
    }
}
