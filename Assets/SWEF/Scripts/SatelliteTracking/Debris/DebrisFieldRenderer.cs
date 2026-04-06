// DebrisFieldRenderer.cs — Phase 114: Satellite & Space Debris Tracking
// Visual debris rendering: point sprites for distant, mesh for close, density cloud for very distant.
// Namespace: SWEF.SatelliteTracking

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Renders space debris using a LOD approach:
    /// <list type="bullet">
    ///   <item>Very distant (&gt;500 WU): volumetric density cloud via particle system</item>
    ///   <item>Mid-range (50–500 WU): point sprites via Graphics.DrawMesh</item>
    ///   <item>Close (&lt;50 WU): individual mesh instances</item>
    /// </list>
    /// </summary>
    public class DebrisFieldRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("LOD Distances (world units)")]
        [Tooltip("Distance below which individual debris meshes are shown.")]
        [SerializeField] private float closeLODDistance   = 50f;

        [Tooltip("Distance below which point sprites are shown.")]
        [SerializeField] private float midLODDistance     = 500f;

        [Header("Meshes & Materials")]
        [Tooltip("Mesh used for close-range debris rendering.")]
        [SerializeField] private Mesh debrisMesh;

        [Tooltip("Material for close-range debris.")]
        [SerializeField] private Material debrisMeshMaterial;

        [Tooltip("Material for mid-range point-sprite debris.")]
        [SerializeField] private Material debrisSpriteMaterial;

        [Header("Particle System")]
        [Tooltip("Particle system used for very distant debris density cloud.")]
        [SerializeField] private ParticleSystem densityCloudParticles;

        [Header("Scale")]
        [Tooltip("Kilometres per Unity world unit.")]
        [SerializeField] private float kmPerWorldUnit = 10f;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<DebrisObject> _visibleDebris = new List<DebrisObject>();
        private Camera _cam;
        private readonly List<Matrix4x4> _closeMeshMatrices  = new List<Matrix4x4>();
        private readonly List<Matrix4x4> _spriteMeshMatrices = new List<Matrix4x4>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Start()
        {
            var debrisMgr = SpaceDebrisManager.Instance;
            if (debrisMgr != null)
            {
                debrisMgr.OnDebrisGenerated += HandleDebrisGenerated;
                if (debrisMgr.GetAllDebris().Count > 0)
                    HandleDebrisGenerated(debrisMgr.GetAllDebris());
            }
        }

        private void OnDestroy()
        {
            var debrisMgr = SpaceDebrisManager.Instance;
            if (debrisMgr != null) debrisMgr.OnDebrisGenerated -= HandleDebrisGenerated;
        }

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;
            RebuildLODBuckets();
            SubmitDrawCalls();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void HandleDebrisGenerated(IReadOnlyList<DebrisObject> debris)
        {
            _visibleDebris.Clear();
            foreach (var d in debris) _visibleDebris.Add(d);
        }

        private void RebuildLODBuckets()
        {
            _closeMeshMatrices.Clear();
            _spriteMeshMatrices.Clear();

            if (_cam == null) return;
            var camPos = _cam.transform.position;

            foreach (var d in _visibleDebris)
            {
                var worldPos = d.positionECI / kmPerWorldUnit;
                float dist = Vector3.Distance(camPos, worldPos);

                if (dist <= closeLODDistance)
                {
                    var scale = d.size == DebrisSize.Large  ? 0.02f
                              : d.size == DebrisSize.Medium ? 0.01f
                              : 0.005f;
                    _closeMeshMatrices.Add(Matrix4x4.TRS(worldPos,
                        Quaternion.Euler(d.tumbleRateDegPerSec * Time.time,
                                         d.tumbleRateDegPerSec * Time.time * 0.7f, 0f),
                        Vector3.one * scale));
                }
                else if (dist <= midLODDistance)
                {
                    _spriteMeshMatrices.Add(Matrix4x4.TRS(worldPos,
                        Quaternion.identity, Vector3.one * 0.005f));
                }
            }
        }

        private void SubmitDrawCalls()
        {
            if (debrisMesh == null) return;

            // Close LOD — instanced mesh
            if (debrisMeshMaterial != null && _closeMeshMatrices.Count > 0)
            {
                int batch = 0;
                var batchArray = new Matrix4x4[Mathf.Min(1023, _closeMeshMatrices.Count)];
                while (batch < _closeMeshMatrices.Count)
                {
                    int count = Mathf.Min(1023, _closeMeshMatrices.Count - batch);
                    for (int i = 0; i < count; i++) batchArray[i] = _closeMeshMatrices[batch + i];
                    Graphics.DrawMeshInstanced(debrisMesh, 0, debrisMeshMaterial, batchArray, count);
                    batch += count;
                }
            }

            // Mid LOD — point sprites
            if (debrisSpriteMaterial != null && _spriteMeshMatrices.Count > 0)
            {
                int batch = 0;
                var batchArray = new Matrix4x4[Mathf.Min(1023, _spriteMeshMatrices.Count)];
                while (batch < _spriteMeshMatrices.Count)
                {
                    int count = Mathf.Min(1023, _spriteMeshMatrices.Count - batch);
                    for (int i = 0; i < count; i++) batchArray[i] = _spriteMeshMatrices[batch + i];
                    Graphics.DrawMeshInstanced(debrisMesh, 0, debrisSpriteMaterial, batchArray, count);
                    batch += count;
                }
            }
        }
    }
}
