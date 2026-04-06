// SpeedAltitudeGraph.cs — Phase 116: Flight Analytics Dashboard
// Speed vs altitude graph: real-time during flight, historical review.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Builds <see cref="ChartData"/> for a speed-vs-altitude scatter
    /// plot or dual-axis line graph.  Can operate in real-time (streaming) or
    /// post-flight (historical) mode.
    /// </summary>
    public class SpeedAltitudeGraph : MonoBehaviour
    {
        // ── State ─────────────────────────────────────────────────────────────────

        private readonly List<float> _speeds    = new List<float>();
        private readonly List<float> _altitudes = new List<float>();
        private readonly List<float> _times     = new List<float>();

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>Number of data points currently buffered.</summary>
        public int SampleCount => _speeds.Count;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Add a real-time sample (call from Update or a coroutine).</summary>
        public void AddSample(float speedKnots, float altitudeM, float timestamp)
        {
            _speeds.Add(speedKnots);
            _altitudes.Add(altitudeM);
            _times.Add(timestamp);
        }

        /// <summary>Load all samples from a completed session record.</summary>
        public void LoadSession(FlightSessionRecord session)
        {
            Clear();
            if (session?.dataPoints == null) return;
            foreach (var p in session.dataPoints)
                AddSample(p.speedKnots, p.altitude, p.timestamp);
        }

        /// <summary>Clear all buffered samples.</summary>
        public void Clear()
        {
            _speeds.Clear();
            _altitudes.Clear();
            _times.Clear();
        }

        /// <summary>Build a dual-axis line chart with speed and altitude series.</summary>
        public ChartData BuildDualAxisChart()
        {
            var speedSeries = new ChartSeries
            {
                label  = "Speed (kts)",
                color  = new Color(0.3f, 0.7f, 1f),
                values = new List<float>(_speeds)
            };

            var altSeries = new ChartSeries
            {
                label  = "Altitude (m)",
                color  = new Color(1f, 0.5f, 0.2f),
                values = new List<float>(_altitudes)
            };

            return new ChartData
            {
                title      = "Speed & Altitude",
                chartType  = ChartType.Line,
                series     = new List<ChartSeries> { speedSeries, altSeries },
                xAxisLabel = "Time (s)",
                yAxisLabel = "Value"
            };
        }

        /// <summary>Build a scatter plot of speed vs altitude.</summary>
        public ChartData BuildScatterChart()
        {
            var series = new ChartSeries
            {
                label  = "Speed vs Altitude",
                color  = Color.cyan,
                values = new List<float>(_speeds),   // Y-axis: speed
                xLabels = new List<string>()
            };
            foreach (float a in _altitudes)
                series.xLabels.Add(a.ToString("F0"));

            return new ChartData
            {
                title      = "Speed vs Altitude",
                chartType  = ChartType.Scatter,
                series     = new List<ChartSeries> { series },
                xAxisLabel = "Altitude (m)",
                yAxisLabel = "Speed (kts)"
            };
        }
    }
}
