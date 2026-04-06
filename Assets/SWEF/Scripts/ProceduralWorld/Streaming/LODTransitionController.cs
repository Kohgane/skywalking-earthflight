// LODTransitionController.cs — Phase 113: Procedural City & Airport Generation
// Smooth LOD transitions: cross-fade, pop-in prevention, distance-based quality scaling.
// Namespace: SWEF.ProceduralWorld

using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Manages smooth transitions between LOD levels for a procedural city chunk.
    /// Uses alpha cross-fading to prevent visual pop-in during LOD switches.
    /// </summary>
    public class LODTransitionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Transition")]
        [Tooltip("Duration of a cross-fade LOD transition in seconds.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float fadeDuration = 0.4f;

        [Tooltip("Hysteresis distance added to thresholds to prevent oscillation.")]
        [Range(0f, 200f)]
        [SerializeField] private float hysteresisMetres = 50f;

        // ── Private state ─────────────────────────────────────────────────────────
        private LODLevel _targetLOD = LODLevel.LOD0;
        private LODLevel _currentLOD = LODLevel.LOD0;
        private float _fadeProgress;
        private bool _fading;
        private Renderer[] _renderers;
        private Camera _camera;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _camera = Camera.main;
        }

        private void Update()
        {
            UpdateTargetLOD();
            if (_fading) AdvanceFade();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Forces an immediate LOD transition without fading.</summary>
        public void ForceSetLOD(LODLevel level)
        {
            _currentLOD = level;
            _targetLOD = level;
            _fading = false;
            _fadeProgress = 1f;
            SetOpacity(1f);
        }

        /// <summary>Current LOD level of this controller.</summary>
        public LODLevel CurrentLOD => _currentLOD;

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void UpdateTargetLOD()
        {
            if (_camera == null) return;
            float dist = Vector3.Distance(transform.position, _camera.transform.position);
            var cfg = ProceduralWorldManager.Instance?.Config;
            float h = hysteresisMetres;

            LODLevel desired;
            if (cfg == null)
            {
                desired = dist < 500f ? LODLevel.LOD0
                        : dist < 2000f ? LODLevel.LOD1
                        : dist < 8000f ? LODLevel.LOD2
                        : LODLevel.LOD3;
            }
            else
            {
                desired = dist < cfg.lod1Distance - h ? LODLevel.LOD0
                        : dist < cfg.lod2Distance - h ? LODLevel.LOD1
                        : dist < cfg.lod3Distance - h ? LODLevel.LOD2
                        : LODLevel.LOD3;
            }

            if (desired != _targetLOD)
            {
                _targetLOD = desired;
                _fading = true;
                _fadeProgress = 0f;
            }
        }

        private void AdvanceFade()
        {
            _fadeProgress += Time.deltaTime / Mathf.Max(0.001f, fadeDuration);
            SetOpacity(_fadeProgress);

            if (_fadeProgress >= 1f)
            {
                _currentLOD = _targetLOD;
                _fading = false;
                SetOpacity(1f);
            }
        }

        private void SetOpacity(float alpha)
        {
            if (_renderers == null) return;
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        var c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                }
            }
        }
    }
}
