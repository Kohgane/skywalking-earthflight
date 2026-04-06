// FlightPathVisualizer.cs — Phase 116: Flight Analytics Dashboard
// 3D flight path replay: colour-coded by altitude/speed/time, with event markers.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Renders a recorded flight path as a 3D polyline in the scene,
    /// colour-coded by altitude, speed, or elapsed time. Requires a <see cref="LineRenderer"/>
    /// component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class FlightPathVisualizer : MonoBehaviour
    {
        // ── Enums ─────────────────────────────────────────────────────────────────

        /// <summary>Attribute used to colour the flight path.</summary>
        public enum ColorMode { Altitude, Speed, Time }

        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Visualization")]
        [SerializeField] private ColorMode colorMode = ColorMode.Altitude;
        [SerializeField] private Gradient  altitudeGradient;
        [SerializeField] private Gradient  speedGradient;
        [SerializeField] private Gradient  timeGradient;
        [SerializeField] [Range(0.1f, 100f)] private float lineWidth = 5f;

        // ── State ─────────────────────────────────────────────────────────────────

        private LineRenderer _lineRenderer;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.startWidth    = lineWidth;
            _lineRenderer.endWidth      = lineWidth;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Render the flight path from a completed session record.</summary>
        public void Visualize(FlightSessionRecord session)
        {
            if (session == null || session.dataPoints == null || session.dataPoints.Count < 2)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            var points = session.dataPoints;
            int count  = points.Count;

            _lineRenderer.positionCount = count;

            // Compute value ranges for normalisation
            float minVal = float.MaxValue, maxVal = float.MinValue;
            foreach (var p in points)
            {
                float v = GetValue(p, count, points.IndexOf(p));
                if (v < minVal) minVal = v;
                if (v > maxVal) maxVal = v;
            }

            var colors = new Color[count];
            for (int i = 0; i < count; i++)
            {
                _lineRenderer.SetPosition(i, points[i].position);
                float t = (maxVal > minVal)
                    ? Mathf.Clamp01((GetValue(points[i], count, i) - minVal) / (maxVal - minVal))
                    : 0f;
                colors[i] = SampleGradient(t);
            }

            _lineRenderer.colorGradient = BuildGradientFromColors(colors);
        }

        /// <summary>Clear the rendered path.</summary>
        public void Clear()
        {
            if (_lineRenderer != null) _lineRenderer.positionCount = 0;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private float GetValue(FlightDataPoint p, int total, int index)
        {
            return colorMode switch
            {
                ColorMode.Altitude => p.altitude,
                ColorMode.Speed    => p.speedKnots,
                ColorMode.Time     => (float)index / Mathf.Max(1, total - 1),
                _                  => p.altitude
            };
        }

        private Color SampleGradient(float t)
        {
            Gradient g = colorMode switch
            {
                ColorMode.Altitude => altitudeGradient,
                ColorMode.Speed    => speedGradient,
                ColorMode.Time     => timeGradient,
                _                  => altitudeGradient
            };
            return g != null ? g.Evaluate(t) : Color.Lerp(Color.blue, Color.red, t);
        }

        private static Gradient BuildGradientFromColors(Color[] colors)
        {
            // Unity Gradient supports up to 8 colour keys; sample evenly
            var g = new Gradient();
            int keyCount = Mathf.Min(8, colors.Length);
            var colorKeys = new GradientColorKey[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                float t = (float)i / Mathf.Max(1, keyCount - 1);
                int idx = Mathf.RoundToInt(t * (colors.Length - 1));
                colorKeys[i] = new GradientColorKey(colors[idx], t);
            }
            g.colorKeys = colorKeys;
            return g;
        }
    }
}
