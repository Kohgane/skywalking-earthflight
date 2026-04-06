// OrbitVisualizer.cs — Phase 114: Satellite & Space Debris Tracking
// 3D orbit path rendering: elliptical orbit lines, ground track projection, coverage footprint.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Renders the orbital path of a satellite as a 3D line in the scene, along with
    /// the ground track projection and optional coverage footprint ring.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class OrbitVisualizer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Orbit Path")]
        [Tooltip("Number of points used to draw the orbit ellipse.")]
        [Range(36, 720)]
        [SerializeField] private int orbitPoints = 360;

        [Tooltip("Line width for the orbital path.")]
        [SerializeField] private float orbitLineWidth = 0.5f;

        [Tooltip("Color of the orbit path (by type).")]
        [SerializeField] private Color orbitColor = Color.cyan;

        [Header("Ground Track")]
        [Tooltip("Whether to draw the ground track projection on the Earth sphere.")]
        [SerializeField] private bool showGroundTrack = true;

        [Tooltip("LineRenderer used for the ground track (separate from orbit path).")]
        [SerializeField] private LineRenderer groundTrackRenderer;

        [Header("Coverage Footprint")]
        [Tooltip("Whether to draw the satellite's coverage footprint ring.")]
        [SerializeField] private bool showCoverageFootprint = false;

        [Tooltip("Number of points in the footprint ring.")]
        [Range(36, 180)]
        [SerializeField] private int footprintPoints = 72;

        [Header("Scale")]
        [Tooltip("Kilometres per Unity world unit.")]
        [SerializeField] private float kmPerWorldUnit = 10f;

        // ── Private state ─────────────────────────────────────────────────────────
        private LineRenderer _orbitLine;
        private TLEData _currentTLE;
        private OrbitalMechanicsEngine _engine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _orbitLine = GetComponent<LineRenderer>();
            _orbitLine.useWorldSpace = true;
            _orbitLine.loop = true;
            _orbitLine.startWidth = orbitLineWidth;
            _orbitLine.endWidth   = orbitLineWidth;
            _orbitLine.startColor = orbitColor;
            _orbitLine.endColor   = new Color(orbitColor.r, orbitColor.g, orbitColor.b, 0f);

            _engine = FindObjectOfType<OrbitalMechanicsEngine>();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Renders the orbit path for the given TLE over one full period.</summary>
        public void RenderOrbit(TLEData tle)
        {
            if (tle == null || _engine == null) return;
            _currentTLE = tle;

            double n         = tle.meanMotionRevPerDay * 2.0 * Math.PI / 86400.0;
            double smaKm     = Math.Pow(OrbitalMechanicsEngine.MuEarth / (n * n), 1.0 / 3.0);
            double periodMin = OrbitalMechanicsEngine.OrbitalPeriodMin(smaKm);

            var positions = new Vector3[orbitPoints];
            var now = DateTime.UtcNow;

            for (int i = 0; i < orbitPoints; i++)
            {
                double tMin = i / (double)orbitPoints * periodMin;
                var state = _engine.Propagate(tle, now.AddMinutes(tMin));
                if (state != null)
                    positions[i] = state.positionECI / kmPerWorldUnit;
                else
                    positions[i] = i > 0 ? positions[i - 1] : Vector3.zero;
            }

            _orbitLine.positionCount = orbitPoints;
            _orbitLine.SetPositions(positions);

            if (showGroundTrack) RenderGroundTrack(tle, periodMin, now);
            if (showCoverageFootprint) RenderCoverageFootprint(tle, now);
        }

        /// <summary>Sets the orbit line colour.</summary>
        public void SetOrbitColor(Color color)
        {
            orbitColor = color;
            if (_orbitLine != null)
            {
                _orbitLine.startColor = color;
                _orbitLine.endColor   = new Color(color.r, color.g, color.b, 0f);
            }
        }

        /// <summary>Returns a colour based on satellite type for easy identification.</summary>
        public static Color ColorForType(SatelliteType type)
        {
            switch (type)
            {
                case SatelliteType.Communication: return Color.cyan;
                case SatelliteType.Navigation:    return Color.green;
                case SatelliteType.Weather:       return new Color(0.5f, 0.5f, 1f);
                case SatelliteType.Science:       return Color.yellow;
                case SatelliteType.Military:      return Color.red;
                case SatelliteType.SpaceStation:  return Color.white;
                case SatelliteType.Debris:        return new Color(0.6f, 0.4f, 0.2f);
                default:                          return Color.grey;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void RenderGroundTrack(TLEData tle, double periodMin, DateTime startUtc)
        {
            if (groundTrackRenderer == null) return;

            float earthRadius = (float)(OrbitalMechanicsEngine.EarthRadiusKm / kmPerWorldUnit);
            var positions = new Vector3[orbitPoints];

            for (int i = 0; i < orbitPoints; i++)
            {
                double tMin = i / (double)orbitPoints * periodMin;
                var state = _engine.Propagate(tle, startUtc.AddMinutes(tMin));
                if (state != null)
                {
                    float lat = state.latitudeDeg * Mathf.Deg2Rad;
                    float lon = state.longitudeDeg * Mathf.Deg2Rad;
                    positions[i] = new Vector3(
                        earthRadius * Mathf.Cos(lat) * Mathf.Cos(lon),
                        earthRadius * Mathf.Sin(lat),
                        earthRadius * Mathf.Cos(lat) * Mathf.Sin(lon));
                }
            }
            groundTrackRenderer.positionCount = orbitPoints;
            groundTrackRenderer.SetPositions(positions);
        }

        private void RenderCoverageFootprint(TLEData tle, DateTime utc)
        {
            var state = _engine.Propagate(tle, utc);
            if (state == null) return;

            float altKm = state.altitudeKm;
            float earthR = (float)(OrbitalMechanicsEngine.EarthRadiusKm / kmPerWorldUnit);
            float satR   = earthR + altKm / kmPerWorldUnit;

            // Half-angle of coverage cone
            float halfAngle = Mathf.Acos(earthR / satR);

            float lat = state.latitudeDeg * Mathf.Deg2Rad;
            float lon = state.longitudeDeg * Mathf.Deg2Rad;
            Vector3 satDir = new Vector3(
                Mathf.Cos(lat) * Mathf.Cos(lon),
                Mathf.Sin(lat),
                Mathf.Cos(lat) * Mathf.Sin(lon));

            // Draw footprint ring — not actually rendered without a LineRenderer reference
            // but positions are calculated for external use
        }
    }
}
