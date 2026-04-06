// ChartRenderer.cs — Phase 116: Flight Analytics Dashboard
// Chart drawing engine: builds chart data structures for UI rendering.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Assembles <see cref="ChartData"/> objects from raw analytics data.
    /// Actual pixel rendering is delegated to UI components (Unity UI or custom shaders).
    /// </summary>
    public class ChartRenderer : MonoBehaviour
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Build a line chart showing performance scores over the given ordered sessions.
        /// </summary>
        public ChartData BuildPerformanceLineChart(IList<FlightSessionRecord> sessions)
        {
            var series = new ChartSeries
            {
                label = "Performance Score",
                color = new Color(0.2f, 0.8f, 1f)
            };

            foreach (var s in sessions)
            {
                series.values.Add(s.performanceScore);
                series.xLabels.Add(FormatSessionDate(s));
            }

            return new ChartData
            {
                title       = "Performance Over Time",
                chartType   = ChartType.Line,
                series      = new List<ChartSeries> { series },
                xAxisLabel  = "Flight",
                yAxisLabel  = "Score"
            };
        }

        /// <summary>
        /// Build a bar chart comparing multiple sessions across key metrics.
        /// </summary>
        public ChartData BuildComparisonBarChart(IList<FlightSessionRecord> sessions, string metric)
        {
            var series = new ChartSeries
            {
                label = metric,
                color = new Color(1f, 0.6f, 0.2f)
            };

            foreach (var s in sessions)
            {
                float value = metric switch
                {
                    "performanceScore"   => s.performanceScore,
                    "landingScore"       => s.landingScore,
                    "fuelEfficiency"     => s.fuelEfficiencyScore,
                    "distanceNm"         => s.distanceNm,
                    _                    => 0f
                };
                series.values.Add(value);
                series.xLabels.Add(FormatSessionDate(s));
            }

            return new ChartData
            {
                title      = $"{metric} Comparison",
                chartType  = ChartType.Bar,
                series     = new List<ChartSeries> { series },
                xAxisLabel = "Flight",
                yAxisLabel = metric
            };
        }

        /// <summary>
        /// Build a radar chart from a pilot's normalised skill scores.
        /// </summary>
        public ChartData BuildSkillRadarChart(float landing, float fuel, float navigation,
                                               float smoothness, float consistency)
        {
            var series = new ChartSeries
            {
                label  = "Skill Profile",
                color  = new Color(0.5f, 1f, 0.5f),
                values = new List<float> { landing, fuel, navigation, smoothness, consistency },
                xLabels = new List<string> { "Landing", "Fuel", "Navigation", "Smoothness", "Consistency" }
            };

            return new ChartData
            {
                title     = "Skill Radar",
                chartType = ChartType.Radar,
                series    = new List<ChartSeries> { series }
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string FormatSessionDate(FlightSessionRecord session)
        {
            return System.DateTimeOffset.FromUnixTimeSeconds(session.startTimeUtc)
                         .LocalDateTime.ToString("MM/dd");
        }
    }
}
