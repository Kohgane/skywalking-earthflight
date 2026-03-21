using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SWEF.Localization;

namespace SWEF.Journal
{
    /// <summary>
    /// Statistics dashboard panel.
    /// Displays aggregate <see cref="JournalStatistics"/> with animated counter transitions.
    /// </summary>
    public class JournalStatisticsUI : MonoBehaviour
    {
        // ── Inspector — Panel ─────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button     closeButton;

        // ── Inspector — Totals ────────────────────────────────────────────────────
        [Header("Totals")]
        [SerializeField] private TMP_Text totalFlightsText;
        [SerializeField] private TMP_Text totalDistanceText;
        [SerializeField] private TMP_Text totalDurationText;

        // ── Inspector — Records ───────────────────────────────────────────────────
        [Header("Records")]
        [SerializeField] private TMP_Text highestAltitudeText;
        [SerializeField] private TMP_Text fastestSpeedText;
        [SerializeField] private TMP_Text longestFlightText;

        // ── Inspector — Averages ──────────────────────────────────────────────────
        [Header("Averages")]
        [SerializeField] private TMP_Text avgDurationText;
        [SerializeField] private TMP_Text avgAltitudeText;

        // ── Inspector — Streaks ───────────────────────────────────────────────────
        [Header("Streaks")]
        [SerializeField] private TMP_Text currentStreakText;
        [SerializeField] private TMP_Text longestStreakText;

        // ── Inspector — Favourites ────────────────────────────────────────────────
        [Header("Favourites")]
        [SerializeField] private TMP_Text favoriteWeatherText;
        [SerializeField] private TMP_Text mostVisitedText;

        // ── Inspector — Recency ───────────────────────────────────────────────────
        [Header("Recency")]
        [SerializeField] private TMP_Text flightsThisWeekText;
        [SerializeField] private TMP_Text flightsThisMonthText;

        // ── Inspector — Animation ─────────────────────────────────────────────────
        [Header("Animation")]
        [Tooltip("Duration (seconds) of the counter-tick animation when the panel opens.")]
        [SerializeField] private float counterAnimDuration = 0.8f;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the statistics dashboard and populates it with current data.</summary>
        public void Show()
        {
            if (panelRoot != null) panelRoot.SetActive(true);

            if (JournalManager.Instance == null) return;
            var stats = JournalManager.Instance.GetStatistics();
            StartCoroutine(PopulateAnimated(stats));
        }

        /// <summary>Hides the statistics dashboard.</summary>
        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private IEnumerator PopulateAnimated(JournalStatistics stats)
        {
            float elapsed = 0f;

            while (elapsed < counterAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / counterAnimDuration);

                SetText(totalFlightsText,    $"{Mathf.RoundToInt(t * stats.totalFlights)}");
                SetText(totalDistanceText,   $"{(t * stats.totalDistanceKm):F1} km");
                SetText(totalDurationText,   FormatHours(t * stats.totalDurationHours));
                SetText(highestAltitudeText, $"{(t * stats.highestAltitudeEver):F0} m");
                SetText(fastestSpeedText,    $"{(t * stats.fastestSpeedEver):F0} km/h");
                SetText(longestFlightText,   FormatSeconds(t * stats.longestFlightSeconds));
                SetText(avgDurationText,     FormatSeconds(t * stats.averageFlightDuration));
                SetText(avgAltitudeText,     $"{(t * stats.averageAltitude):F0} m");
                SetText(currentStreakText,   $"{Mathf.RoundToInt(t * stats.currentStreak)}");
                SetText(longestStreakText,   $"{Mathf.RoundToInt(t * stats.longestStreak)}");
                SetText(flightsThisWeekText, $"{Mathf.RoundToInt(t * stats.flightsThisWeek)}");
                SetText(flightsThisMonthText,$"{Mathf.RoundToInt(t * stats.flightsThisMonth)}");

                yield return null;
            }

            // Final snap to exact values.
            SetText(totalFlightsText,    $"{stats.totalFlights}");
            SetText(totalDistanceText,   $"{stats.totalDistanceKm:F1} km");
            SetText(totalDurationText,   FormatHours(stats.totalDurationHours));
            SetText(highestAltitudeText, $"{stats.highestAltitudeEver:F0} m");
            SetText(fastestSpeedText,    $"{stats.fastestSpeedEver:F0} km/h");
            SetText(longestFlightText,   FormatSeconds(stats.longestFlightSeconds));
            SetText(avgDurationText,     FormatSeconds(stats.averageFlightDuration));
            SetText(avgAltitudeText,     $"{stats.averageAltitude:F0} m");
            SetText(currentStreakText,   $"{stats.currentStreak}");
            SetText(longestStreakText,   $"{stats.longestStreak}");
            SetText(flightsThisWeekText, $"{stats.flightsThisWeek}");
            SetText(flightsThisMonthText,$"{stats.flightsThisMonth}");
            SetText(favoriteWeatherText, stats.favoriteWeather ?? Localize("journal_stats_unknown"));
            SetText(mostVisitedText,     stats.mostVisitedLocation ?? Localize("journal_stats_unknown"));
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value ?? string.Empty;
        }

        private static string FormatHours(float hours)
        {
            int h = (int)hours;
            int m = (int)((hours - h) * 60f);
            return h > 0 ? $"{h}h {m:D2}m" : $"{m}m";
        }

        private static string FormatSeconds(float seconds)
        {
            var ts = System.TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0
                ? $"{ts.Hours}h {ts.Minutes:D2}m"
                : $"{ts.Minutes}m {ts.Seconds:D2}s";
        }

        private static string Localize(string key)
        {
            var loc = LocalizationManager.Instance;
            return loc != null ? loc.GetText(key) : key;
        }
    }
}
