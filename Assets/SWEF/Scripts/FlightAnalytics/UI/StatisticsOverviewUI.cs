// StatisticsOverviewUI.cs — Phase 116: Flight Analytics Dashboard
// Statistics overview: lifetime stats, records, milestones, progression charts.
// Namespace: SWEF.FlightAnalytics

using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Displays the lifetime statistics overview panel, including
    /// all-time aggregated stats, personal records, and milestone progress.
    /// </summary>
    public class StatisticsOverviewUI : MonoBehaviour
    {
        // ── State ─────────────────────────────────────────────────────────────────

        private AggregatedStats _lifetimeStats;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Refresh the panel using all-time data from the analytics manager.</summary>
        public void Refresh()
        {
            if (FlightAnalyticsManager.Instance == null) return;
            _lifetimeStats = FlightAnalyticsManager.Instance.GetAggregatedStats(TimeRange.AllTime);
            Render();
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private void Render()
        {
            if (_lifetimeStats == null) return;
            Debug.Log($"[SWEF] StatisticsOverviewUI: " +
                      $"Flights={_lifetimeStats.flightCount}, " +
                      $"Hours={_lifetimeStats.totalHours:F1}, " +
                      $"BestScore={_lifetimeStats.bestPerformanceScore:F0}");
        }
    }
}
