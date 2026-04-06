// OrbitTrailRenderer.cs — Phase 114: Satellite & Space Debris Tracking
// Orbit trail visualization: color-coded by type, fade over time, ground shadow.
// Namespace: SWEF.SatelliteTracking

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Renders a fading trail behind a satellite as it moves along its orbit.
    /// Colour is determined by satellite type. An optional shadow is projected
    /// onto the Earth sphere.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class OrbitTrailRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Trail Settings")]
        [Tooltip("Time in seconds over which the trail fades.")]
        [SerializeField] private float trailTime = 90f;

        [Tooltip("Width of the trail in world units.")]
        [SerializeField] private float trailWidth = 0.3f;

        [Tooltip("Satellite type used to determine trail colour.")]
        [SerializeField] private SatelliteType satelliteType = SatelliteType.Communication;

        [Header("Ground Shadow")]
        [Tooltip("Whether to project a shadow trail onto the Earth sphere.")]
        [SerializeField] private bool showGroundShadow = true;

        [Tooltip("LineRenderer used for the ground shadow.")]
        [SerializeField] private LineRenderer groundShadowRenderer;

        [Tooltip("Earth sphere transform for shadow projection.")]
        [SerializeField] private Transform earthSphere;

        [Tooltip("Earth radius in world units.")]
        [SerializeField] private float earthRadiusWU = 637.1f;

        // ── Private state ─────────────────────────────────────────────────────────
        private TrailRenderer _trail;
        private readonly Queue<Vector3> _trailPositions = new Queue<Vector3>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
            _trail.time       = trailTime;
            _trail.startWidth = trailWidth;
            _trail.endWidth   = 0f;

            ApplyTypeColor();
        }

        private void LateUpdate()
        {
            if (showGroundShadow && groundShadowRenderer != null)
                UpdateGroundShadow();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Updates the satellite type (and recolors the trail).</summary>
        public void SetSatelliteType(SatelliteType type)
        {
            satelliteType = type;
            ApplyTypeColor();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ApplyTypeColor()
        {
            if (_trail == null) return;
            var color = OrbitVisualizer.ColorForType(satelliteType);
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            _trail.colorGradient = grad;
        }

        private void UpdateGroundShadow()
        {
            // Project current trail positions radially onto the Earth sphere
            var trailPositions = new Vector3[_trail.positionCount];
            _trail.GetPositions(trailPositions);

            if (trailPositions.Length < 2) return;

            groundShadowRenderer.positionCount = trailPositions.Length;
            for (int i = 0; i < trailPositions.Length; i++)
            {
                var dir = (trailPositions[i] - (earthSphere != null ? earthSphere.position : Vector3.zero)).normalized;
                groundShadowRenderer.SetPosition(i, dir * earthRadiusWU * 1.001f);
            }
        }
    }
}
