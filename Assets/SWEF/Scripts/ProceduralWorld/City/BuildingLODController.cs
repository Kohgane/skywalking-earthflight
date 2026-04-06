// BuildingLODController.cs — Phase 113: Procedural City & Airport Generation
// Multi-level LOD for buildings: LOD0 (full detail), LOD1 (simplified),
// LOD2 (billboard), LOD3 (merged batch).
// Namespace: SWEF.ProceduralWorld

using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Controls the LOD level of an individual building based on camera distance.
    /// Attach to each spawned building GameObject.
    /// </summary>
    public class BuildingLODController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("LOD Renderers")]
        [Tooltip("Renderer used at LOD0 — full geometric detail.")]
        [SerializeField] private Renderer lod0Renderer;

        [Tooltip("Renderer used at LOD1 — simplified mesh.")]
        [SerializeField] private Renderer lod1Renderer;

        [Tooltip("Renderer used at LOD2 — billboard / impostor.")]
        [SerializeField] private Renderer lod2Renderer;

        [Tooltip("Renderer used at LOD3 — merged instanced batch or point sprite.")]
        [SerializeField] private Renderer lod3Renderer;

        [Header("Distance Thresholds (metres)")]
        [SerializeField] private float lod1Threshold = 500f;
        [SerializeField] private float lod2Threshold = 2000f;
        [SerializeField] private float lod3Threshold = 8000f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current active LOD level.</summary>
        public LODLevel CurrentLOD { get; private set; } = LODLevel.LOD0;

        // ── Private state ─────────────────────────────────────────────────────────
        private Camera _mainCamera;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _mainCamera = Camera.main;
            ApplyThresholds(ProceduralWorldManager.Instance?.Config);
            SetLOD(LODLevel.LOD0);
        }

        private void Update()
        {
            if (_mainCamera == null) return;
            float dist = Vector3.Distance(transform.position, _mainCamera.transform.position);
            LODLevel target = DistanceToLOD(dist);
            if (target != CurrentLOD) SetLOD(target);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Forces the building to a specific LOD level.</summary>
        public void SetLOD(LODLevel level)
        {
            CurrentLOD = level;
            EnableOnly(level);
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void ApplyThresholds(ProceduralWorldConfig cfg)
        {
            if (cfg == null) return;
            lod1Threshold = cfg.lod1Distance;
            lod2Threshold = cfg.lod2Distance;
            lod3Threshold = cfg.lod3Distance;
        }

        private LODLevel DistanceToLOD(float dist)
        {
            if (dist < lod1Threshold) return LODLevel.LOD0;
            if (dist < lod2Threshold) return LODLevel.LOD1;
            if (dist < lod3Threshold) return LODLevel.LOD2;
            return LODLevel.LOD3;
        }

        private void EnableOnly(LODLevel level)
        {
            SetRenderer(lod0Renderer, level == LODLevel.LOD0);
            SetRenderer(lod1Renderer, level == LODLevel.LOD1);
            SetRenderer(lod2Renderer, level == LODLevel.LOD2);
            SetRenderer(lod3Renderer, level == LODLevel.LOD3);
        }

        private static void SetRenderer(Renderer r, bool enabled)
        {
            if (r != null) r.enabled = enabled;
        }
    }
}
