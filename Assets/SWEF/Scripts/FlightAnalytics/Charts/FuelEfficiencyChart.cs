// FuelEfficiencyChart.cs — Phase 116: Flight Analytics Dashboard
// Fuel consumption analysis: consumption rate over time, cross-aircraft comparison.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Builds <see cref="ChartData"/> objects for fuel consumption and
    /// efficiency analysis, including per-flight trend lines and aircraft comparisons.
    /// </summary>
    public class FuelEfficiencyChart : MonoBehaviour
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Build a line chart of fuel-efficiency scores over ordered sessions.
        /// </summary>
        public ChartData BuildEfficiencyTrendChart(IList<FlightSessionRecord> sessions)
        {
            var series = new ChartSeries
            {
                label = "Fuel Efficiency Score",
                color = new Color(0.4f, 1f, 0.4f)
            };

            foreach (var s in sessions)
            {
                series.values.Add(s.fuelEfficiencyScore);
                series.xLabels.Add(FormatDate(s));
            }

            return new ChartData
            {
                title      = "Fuel Efficiency Trend",
                chartType  = ChartType.Line,
                series     = new List<ChartSeries> { series },
                xAxisLabel = "Flight",
                yAxisLabel = "Score (0–100)"
            };
        }

        /// <summary>
        /// Build a line chart of instantaneous fuel-normalised values over a single session.
        /// Useful for identifying heavy fuel-burn phases.
        /// </summary>
        public ChartData BuildConsumptionRateChart(FlightSessionRecord session)
        {
            var series = new ChartSeries
            {
                label = "Fuel Remaining",
                color = new Color(1f, 0.8f, 0.2f)
            };

            if (session?.dataPoints != null)
            {
                foreach (var p in session.dataPoints)
                {
                    series.values.Add(p.fuelNormalised * 100f);
                    series.xLabels.Add(p.timestamp.ToString("F0") + "s");
                }
            }

            return new ChartData
            {
                title      = "Fuel Consumption",
                chartType  = ChartType.Line,
                series     = new List<ChartSeries> { series },
                xAxisLabel = "Time (s)",
                yAxisLabel = "Fuel (%)"
            };
        }

        /// <summary>
        /// Build a bar chart comparing average fuel efficiency across different aircraft.
        /// <paramref name="perAircraft"/> maps aircraftId → average efficiency score.
        /// </summary>
        public ChartData BuildAircraftComparisonChart(Dictionary<string, float> perAircraft)
        {
            var series = new ChartSeries
            {
                label = "Avg Fuel Efficiency",
                color = new Color(0.6f, 0.4f, 1f)
            };

            if (perAircraft != null)
            {
                foreach (var kv in perAircraft)
                {
                    series.values.Add(kv.Value);
                    series.xLabels.Add(kv.Key);
                }
            }

            return new ChartData
            {
                title      = "Fuel Efficiency by Aircraft",
                chartType  = ChartType.Bar,
                series     = new List<ChartSeries> { series },
                xAxisLabel = "Aircraft",
                yAxisLabel = "Score (0–100)"
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string FormatDate(FlightSessionRecord s)
        {
            return System.DateTimeOffset.FromUnixTimeSeconds(s.startTimeUtc)
                         .LocalDateTime.ToString("MM/dd");
        }
    }
}
