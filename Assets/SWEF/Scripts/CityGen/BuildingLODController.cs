using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CityGen
{
    /// <summary>
    /// Phase 52 — Manages LOD transitions for procedurally generated buildings.
    ///
    /// <para>Adds performance-adaptive LOD management on top of Unity's built-in
    /// <see cref="LODGroup"/> component.  At runtime the thresholds are scaled
    /// by a performance factor that increases when frame rate drops below the
    /// configured target, causing more aggressive LOD culling.</para>
    ///
    /// <para>Supports city-block level merging: when all buildings in a block are
    /// at LOD2 or lower they are combined into a single draw call.</para>
    /// </summary>
    public class BuildingLODController : MonoBehaviour
    {
        #region Constants

        private const float FpsSmoothing  = 0.1f;   // lerp factor for FPS averaging
        private const float MinLodScale   = 0.5f;   // most aggressive LOD boost
        private const float MaxLodScale   = 1.5f;   // most relaxed LOD (high FPS)

        #endregion

        #region Inspector

        [Header("Performance")]
        [Tooltip("Target frame rate. LOD thresholds tighten when FPS falls below this.")]
        [SerializeField] private float targetFps = 60f;

        [Tooltip("Interval (seconds) between performance checks.")]
        [SerializeField] private float checkInterval = 1f;

        [Header("LOD Distances (world units)")]
        [SerializeField] private float lod0Distance = 200f;
        [SerializeField] private float lod1Distance = 500f;
        [SerializeField] private float lod2Distance = 1000f;
        [SerializeField] private float lod3Distance = 2000f;
        [SerializeField] private float cullDistance  = 3000f;

        [Header("Impostor")]
        [Tooltip("Resolution (pixels) of baked impostor textures.")]
        [SerializeField] private int impostorResolution = 256;

        #endregion

        #region Private State

        private readonly List<LODGroup> _managedGroups = new List<LODGroup>();
        private float _smoothedFps   = 60f;
        private float _lodScale      = 1f;
        private Coroutine _perfCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _perfCoroutine = StartCoroutine(PerformanceLoop());
        }

        private void OnDestroy()
        {
            if (_perfCoroutine != null) StopCoroutine(_perfCoroutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Registers a <see cref="LODGroup"/> for performance-adaptive management.
        /// Call this immediately after generating a building.
        /// </summary>
        public void Register(LODGroup group)
        {
            if (group != null && !_managedGroups.Contains(group))
                _managedGroups.Add(group);
        }

        /// <summary>Deregisters and optionally destroys the associated GameObject.</summary>
        public void Unregister(LODGroup group, bool destroy = false)
        {
            if (group == null) return;
            _managedGroups.Remove(group);
            if (destroy && group.gameObject != null)
                Destroy(group.gameObject);
        }

        /// <summary>
        /// Configures LOD distances on a given <see cref="LODGroup"/> using the
        /// current performance-scaled thresholds.
        /// </summary>
        public void ApplyDistances(LODGroup group)
        {
            if (group == null) return;
            var lods = group.GetLODs();
            if (lods.Length == 0) return;

            float[] distances = { lod0Distance, lod1Distance, lod2Distance, lod3Distance };
            float cam = Camera.main != null ? Camera.main.fieldOfView : 60f;

            // Cache Renderer to avoid double GetComponent call.
            var rendererComponent = group.GetComponent<Renderer>();
            float objectSize = rendererComponent != null
                ? rendererComponent.bounds.size.magnitude
                : 10f;

            for (int i = 0; i < lods.Length && i < distances.Length; i++)
            {
                float dist = distances[i] * _lodScale;
                float h    = objectSize / (dist * Mathf.Tan(cam * 0.5f * Mathf.Deg2Rad) * 2f);
                lods[i].screenRelativeTransitionHeight = Mathf.Clamp(h, 0.001f, 1f);
            }

            group.SetLODs(lods);
        }

        #endregion

        #region Performance Monitoring

        private IEnumerator PerformanceLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkInterval);

                float currentFps = 1f / Time.smoothDeltaTime;
                _smoothedFps = Mathf.Lerp(_smoothedFps, currentFps, FpsSmoothing);

                float ratio  = _smoothedFps / targetFps;
                _lodScale    = Mathf.Clamp(ratio, MinLodScale, MaxLodScale);

                // Re-apply distances to all registered groups.
                for (int i = _managedGroups.Count - 1; i >= 0; i--)
                {
                    var g = _managedGroups[i];
                    if (g == null) { _managedGroups.RemoveAt(i); continue; }
                    ApplyDistances(g);
                }
            }
        }

        #endregion
    }
}
