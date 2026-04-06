// FlightAnalyticsUI.cs — Phase 116: Flight Analytics Dashboard
// Main analytics dashboard: overview cards, chart area, filter controls.
// Namespace: SWEF.FlightAnalytics

using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Controller for the main Flight Analytics Dashboard panel.
    /// Drives overview stat cards, chart display, date-range filtering, and
    /// category tab navigation. Requires a configured <see cref="FlightAnalyticsManager"/>.
    /// </summary>
    public class FlightAnalyticsUI : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Panel References")]
        [SerializeField] private GameObject overviewPanel;
        [SerializeField] private GameObject chartPanel;
        [SerializeField] private GameObject heatmapPanel;
        [SerializeField] private GameObject leaderboardPanel;

        [Header("Filter State")]
        [SerializeField] private TimeRange selectedRange = TimeRange.Week;
        [SerializeField] private AnalyticsCategory selectedCategory = AnalyticsCategory.FlightPerformance;

        // ── State ─────────────────────────────────────────────────────────────────

        private bool _isOpen;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>Whether the analytics dashboard panel is currently visible.</summary>
        public bool IsOpen => _isOpen;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Open the dashboard and refresh displayed data.</summary>
        public void Open()
        {
            _isOpen = true;
            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>Close the dashboard.</summary>
        public void Close()
        {
            _isOpen = false;
            gameObject.SetActive(false);
        }

        /// <summary>Change the active time-range filter and refresh data.</summary>
        public void SetTimeRange(TimeRange range)
        {
            selectedRange = range;
            if (_isOpen) Refresh();
        }

        /// <summary>Change the active analytics category tab and refresh.</summary>
        public void SetCategory(AnalyticsCategory category)
        {
            selectedCategory = category;
            if (_isOpen) Refresh();
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (FlightAnalyticsManager.Instance == null) return;

            var stats = FlightAnalyticsManager.Instance.GetAggregatedStats(selectedRange);
            UpdateOverviewCards(stats);
        }

        private void UpdateOverviewCards(AggregatedStats stats)
        {
            // Concrete UI wiring (Text/TMP components) goes here.
            // Keeping as a stub to avoid hard TMP dependency at compile time.
            Debug.Log($"[SWEF] FlightAnalyticsUI: Flights={stats.flightCount}, " +
                      $"Hours={stats.totalHours:F1}, Dist={stats.totalDistanceNm:F0}nm");
        }
    }
}
