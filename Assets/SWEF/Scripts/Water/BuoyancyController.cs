// BuoyancyController.cs — SWEF Ocean & Water Interaction System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Water
{
    /// <summary>
    /// Phase 55 — MonoBehaviour that applies physics-based buoyancy to a Rigidbody
    /// using multi-point sampling against the Gerstner wave surface managed by
    /// <see cref="WaterSurfaceManager"/>.
    ///
    /// <para>Attach to any GameObject that has a <see cref="Rigidbody"/> component and
    /// should float, partially submerge, or sink in water.  Configure the
    /// <see cref="samplePoints"/> array to define the hull sampling positions in
    /// local space.</para>
    ///
    /// <para>Events:
    /// <list type="bullet">
    ///   <item><see cref="OnSubmerged"/> — first frame any sample point crosses below the surface.</item>
    ///   <item><see cref="OnSurfaced"/> — first frame all sample points return above the surface.</item>
    /// </list>
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BuoyancyController : MonoBehaviour
    {
        #region Inspector

        [Header("Buoyancy Profile")]
        [Tooltip("Optional WaterInteractionProfile override. Falls back to WaterSurfaceManager's profile values if null.")]
        [SerializeField] private WaterInteractionProfile profileOverride;

        [Header("Sample Points")]
        [Tooltip("Local-space positions used as buoyancy probe points. Typically placed at hull corners and keel.")]
        [SerializeField] private Vector3[] samplePoints = new Vector3[]
        {
            new Vector3( 1f, 0f,  2f),
            new Vector3(-1f, 0f,  2f),
            new Vector3( 1f, 0f, -2f),
            new Vector3(-1f, 0f, -2f),
        };

        [Header("Air Physics")]
        [Tooltip("Rigidbody drag when out of water; restored when the object surfaces.")]
        [SerializeField] private float airDrag = 0.1f;

        [Tooltip("Rigidbody angular drag when out of water.")]
        [SerializeField] private float airAngularDrag = 0.05f;

        [Header("Debug")]
        [Tooltip("Draw buoyancy probe gizmos in the Scene view.")]
        [SerializeField] private bool showGizmos = true;

        #endregion

        #region Events

        /// <summary>
        /// Fired the frame the object first becomes submerged.
        /// The float parameter is the average submersion depth across sample points (metres).
        /// </summary>
        public event Action<float> OnSubmerged;

        /// <summary>Fired the frame the object fully returns above the water surface.</summary>
        public event Action OnSurfaced;

        #endregion

        #region Private State

        private Rigidbody _rb;
        private bool _wasSubmerged;
        private BuoyancySettings _settings;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            ResolveSettings();
        }

        private void FixedUpdate()
        {
            if (WaterSurfaceManager.Instance == null) return;

            ApplyBuoyancy();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns <c>true</c> if at least one sample point is currently below the water surface.
        /// </summary>
        public bool IsSubmerged => _wasSubmerged;

        /// <summary>
        /// Overrides the buoyancy settings resolved from the profile at runtime.
        /// Useful for dynamic tuning (e.g. loading from a save-game or applying a power-up).
        /// </summary>
        /// <param name="settings">New buoyancy settings to apply immediately.</param>
        public void ApplySettings(BuoyancySettings settings)
        {
            _settings = settings ?? new BuoyancySettings();
        }

        #endregion

        #region Private Helpers

        private void ResolveSettings()
        {
            _settings = profileOverride != null ? profileOverride.buoyancy : new BuoyancySettings();
        }

        private void ApplyBuoyancy()
        {
            int submergedCount = 0;
            float totalDepth = 0f;

            for (int i = 0; i < samplePoints.Length; i++)
            {
                Vector3 worldPoint = transform.TransformPoint(samplePoints[i]);
                float waterY = WaterSurfaceManager.Instance.GetWaterHeightAt(worldPoint);
                float submergeDepth = waterY - worldPoint.y;

                if (submergeDepth > 0f)
                {
                    submergedCount++;
                    float clampedDepth = Mathf.Min(submergeDepth, _settings.submergeDepthThreshold);
                    float forceFraction = clampedDepth / _settings.submergeDepthThreshold;
                    float force = _settings.buoyancyForce * forceFraction;
                    _rb.AddForceAtPosition(Vector3.up * force, worldPoint, ForceMode.Force);
                    totalDepth += submergeDepth;
                }
            }

            bool isSubmerged = submergedCount > 0;

            // Apply water drag when submerged
            _rb.drag        = isSubmerged ? _settings.waterDrag        : airDrag;
            _rb.angularDrag = isSubmerged ? _settings.waterAngularDrag : airAngularDrag;

            // Fire state-change events
            if (isSubmerged && !_wasSubmerged)
            {
                _wasSubmerged = true;
                float avgDepth = submergedCount > 0 ? totalDepth / submergedCount : 0f;
                OnSubmerged?.Invoke(avgDepth);
            }
            else if (!isSubmerged && _wasSubmerged)
            {
                _wasSubmerged = false;
                OnSurfaced?.Invoke();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || samplePoints == null) return;

            Gizmos.color = Color.cyan;
            foreach (Vector3 local in samplePoints)
            {
                Vector3 world = transform.TransformPoint(local);
                Gizmos.DrawWireSphere(world, 0.15f);
            }
        }

        #endregion
    }
}
