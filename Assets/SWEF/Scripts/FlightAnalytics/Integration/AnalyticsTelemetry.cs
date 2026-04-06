// AnalyticsTelemetry.cs — Phase 116: Flight Analytics Dashboard
// Meta-analytics: dashboard usage, popular charts, export frequency.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Collects lightweight meta-analytics about dashboard usage
    /// (which charts are viewed most, how often reports are exported) when the
    /// player has opted in via <see cref="FlightAnalyticsConfig.allowDashboardTelemetry"/>.
    /// </summary>
    public class AnalyticsTelemetry : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [SerializeField] private FlightAnalyticsConfig config;

        // ── State ─────────────────────────────────────────────────────────────────

        private readonly Dictionary<string, int> _chartViewCounts  = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _exportCounts     = new Dictionary<string, int>();
        private int _dashboardOpenCount;

        // ── Properties ────────────────────────────────────────────────────────────

        private bool IsEnabled => config == null || config.allowDashboardTelemetry;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Record that the analytics dashboard was opened.</summary>
        public void TrackDashboardOpen()
        {
            if (!IsEnabled) return;
            _dashboardOpenCount++;
        }

        /// <summary>Record that a specific chart type was viewed.</summary>
        public void TrackChartView(ChartType chartType)
        {
            if (!IsEnabled) return;
            string key = chartType.ToString();
            _chartViewCounts[key] = _chartViewCounts.GetValueOrDefault(key) + 1;
        }

        /// <summary>Record that a report was exported in the given format.</summary>
        public void TrackExport(ExportFormat format)
        {
            if (!IsEnabled) return;
            string key = format.ToString();
            _exportCounts[key] = _exportCounts.GetValueOrDefault(key) + 1;
        }

        /// <summary>Return the most-viewed chart type (null if no data).</summary>
        public string MostViewedChart()
        {
            string best = null;
            int    max  = 0;
            foreach (var kv in _chartViewCounts)
                if (kv.Value > max) { max = kv.Value; best = kv.Key; }
            return best;
        }

        /// <summary>Return a summary string for debugging/logging.</summary>
        public string GetSummary()
        {
            return $"Dashboard opens: {_dashboardOpenCount}, " +
                   $"Most-viewed chart: {MostViewedChart() ?? "none"}";
        }
    }
}
