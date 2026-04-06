// SatelliteRenderer.cs — Phase 114: Satellite & Space Debris Tracking
// Satellite 3D model rendering: solar panel orientation, antenna pointing, scale-appropriate display.
// Namespace: SWEF.SatelliteTracking

using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Renders a single satellite's 3D model, orienting solar panels toward the Sun
    /// and pointing the primary antenna toward Earth, with altitude-appropriate scaling.
    /// </summary>
    public class SatelliteRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Model Parts")]
        [Tooltip("Root transform of the satellite's 3D model.")]
        [SerializeField] private Transform modelRoot;

        [Tooltip("Solar panel pivot transforms.")]
        [SerializeField] private Transform[] solarPanelPivots;

        [Tooltip("Primary antenna pivot transform.")]
        [SerializeField] private Transform antennaPivot;

        [Header("Scale")]
        [Tooltip("Actual satellite size in metres (used for LOD scaling).")]
        [SerializeField] private float actualSizeM = 10f;

        [Tooltip("Minimum on-screen size (world-units) before the model is replaced by a sprite.")]
        [SerializeField] private float minVisibleSize = 0.01f;

        [Tooltip("Kilometres per Unity world unit.")]
        [SerializeField] private float kmPerWorldUnit = 10f;

        [Header("Orbit Data")]
        [Tooltip("The satellite record this renderer belongs to.")]
        [SerializeField] private SatelliteRecord record;

        // ── Private state ─────────────────────────────────────────────────────────
        private Camera _cam;
        private float _baseScale;
        private Vector3 _sunDirection = new Vector3(1f, 0.3f, 0.2f); // Approximate solar direction

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _cam = Camera.main;
            _baseScale = actualSizeM / (1000f * kmPerWorldUnit);
            if (modelRoot != null) modelRoot.localScale = Vector3.one * _baseScale;
        }

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;
            UpdateSolarPanels();
            UpdateAntennaPointing();
            UpdateLODScale();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Binds this renderer to a satellite record.</summary>
        public void SetRecord(SatelliteRecord sat) => record = sat;

        /// <summary>Updates the approximate solar direction (normalised).</summary>
        public void SetSunDirection(Vector3 dir) => _sunDirection = dir.normalized;

        // ── Private helpers ───────────────────────────────────────────────────────

        private void UpdateSolarPanels()
        {
            if (solarPanelPivots == null) return;
            foreach (var pivot in solarPanelPivots)
            {
                if (pivot == null) continue;
                // Track the Sun: rotate panel normal toward sun direction
                pivot.rotation = Quaternion.LookRotation(_sunDirection, Vector3.up);
            }
        }

        private void UpdateAntennaPointing()
        {
            if (antennaPivot == null) return;
            // Point antenna toward Earth centre (world origin)
            var toEarth = (Vector3.zero - transform.position).normalized;
            antennaPivot.rotation = Quaternion.LookRotation(toEarth, Vector3.up);
        }

        private void UpdateLODScale()
        {
            if (_cam == null || modelRoot == null) return;
            float dist = Vector3.Distance(_cam.transform.position, transform.position);
            // Scale up at distance to maintain minimum angular size
            float worldSize = Mathf.Max(_baseScale, minVisibleSize * dist * 0.01f);
            modelRoot.localScale = Vector3.one * worldSize;
        }
    }
}
