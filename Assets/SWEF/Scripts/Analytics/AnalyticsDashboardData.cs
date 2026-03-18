using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Analytics
{
    /// <summary>
    /// Persists and exposes aggregated local analytics data for the in-app stats dashboard.
    /// Backed by PlayerPrefs; call <see cref="RefreshFromLocal"/> to recalculate derived stats.
    /// </summary>
    public class AnalyticsDashboardData : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static AnalyticsDashboardData Instance { get; private set; }

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyTotalFlights       = "SWEF_AD_TotalFlights";
        private const string KeyTotalFlightTimeSec = "SWEF_AD_TotalFlightTimeSec";
        private const string KeyMaxAltitudeEver    = "SWEF_AD_MaxAltitude";
        private const string KeyMaxSpeedEver       = "SWEF_AD_MaxSpeed";
        private const string KeyAchievements       = "SWEF_AD_Achievements";
        private const string KeyScreenshots        = "SWEF_AD_Screenshots";
        private const string KeyShares             = "SWEF_AD_Shares";
        private const string KeyFavoriteWeather    = "SWEF_AD_FavWeather";
        private const string KeyLastActiveDay      = "SWEF_AD_LastActiveDay";
        private const string KeyActiveStreak       = "SWEF_AD_Streak";
        private const string KeyWeeklyFlights      = "SWEF_AD_WeeklyFlights";
        private const string KeyFeatureUsage       = "SWEF_AD_FeatureUsage";

        // ── Public properties ────────────────────────────────────────────────────
        public int    TotalFlights             { get; private set; }
        public float  TotalFlightTimeSeconds   { get; private set; }
        public float  MaxAltitudeEver          { get; private set; }
        public float  MaxSpeedEver             { get; private set; }
        public float  AverageFlightDuration    { get; private set; }
        public string FavoriteWeatherCondition { get; private set; }
        public int    AchievementsUnlocked     { get; private set; }
        public int    ScreenshotsTaken         { get; private set; }
        public int    ShareCount               { get; private set; }
        public int    DailyActiveStreak        { get; private set; }
        public int[]  WeeklyFlightCounts       { get; private set; } = new int[7];

        /// <summary>Feature name → usage count.</summary>
        public Dictionary<string, int> FeatureUsageCounts { get; private set; } = new Dictionary<string, int>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RefreshFromLocal();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Reload all stats from PlayerPrefs and recalculate derived values.</summary>
        public void RefreshFromLocal()
        {
            TotalFlights           = PlayerPrefs.GetInt(KeyTotalFlights, 0);
            TotalFlightTimeSeconds = PlayerPrefs.GetFloat(KeyTotalFlightTimeSec, 0f);
            MaxAltitudeEver        = PlayerPrefs.GetFloat(KeyMaxAltitudeEver, 0f);
            MaxSpeedEver           = PlayerPrefs.GetFloat(KeyMaxSpeedEver, 0f);
            AchievementsUnlocked   = PlayerPrefs.GetInt(KeyAchievements, 0);
            ScreenshotsTaken       = PlayerPrefs.GetInt(KeyScreenshots, 0);
            ShareCount             = PlayerPrefs.GetInt(KeyShares, 0);
            FavoriteWeatherCondition = PlayerPrefs.GetString(KeyFavoriteWeather, "Clear");
            DailyActiveStreak      = PlayerPrefs.GetInt(KeyActiveStreak, 0);

            AverageFlightDuration = TotalFlights > 0
                ? TotalFlightTimeSeconds / TotalFlights
                : 0f;

            LoadWeeklyFlights();
            LoadFeatureUsage();
            UpdateDailyStreak();
        }

        /// <summary>
        /// Record a completed flight. Call at the end of each flight session.
        /// </summary>
        public void RecordFlight(float durationSeconds, float maxAlt, float maxSpeed, string weatherCondition)
        {
            TotalFlights++;
            TotalFlightTimeSeconds += durationSeconds;
            if (maxAlt   > MaxAltitudeEver) MaxAltitudeEver = maxAlt;
            if (maxSpeed > MaxSpeedEver)    MaxSpeedEver    = maxSpeed;

            if (!string.IsNullOrEmpty(weatherCondition))
                FavoriteWeatherCondition = weatherCondition; // simplified; real impl would tally

            AverageFlightDuration = TotalFlightTimeSeconds / TotalFlights;

            // Weekly flights — today is index 0
            WeeklyFlightCounts[0]++;

            Persist();
        }

        /// <summary>Increment a feature usage counter.</summary>
        public void RecordFeatureUse(string featureName)
        {
            if (!FeatureUsageCounts.ContainsKey(featureName))
                FeatureUsageCounts[featureName] = 0;
            FeatureUsageCounts[featureName]++;
            SaveFeatureUsage();
        }

        /// <summary>Record a screenshot taken.</summary>
        public void RecordScreenshot() { ScreenshotsTaken++; Persist(); }

        /// <summary>Record a share action.</summary>
        public void RecordShare() { ShareCount++; Persist(); }

        /// <summary>
        /// Returns a display-ready dictionary of stats strings.
        /// </summary>
        public Dictionary<string, string> GetFormattedStats()
        {
            return new Dictionary<string, string>
            {
                { "Total Flights",       TotalFlights.ToString() },
                { "Flight Time",         FormatDuration(TotalFlightTimeSeconds) },
                { "Max Altitude",        FormatAltitude(MaxAltitudeEver) },
                { "Max Speed",           FormatSpeed(MaxSpeedEver) },
                { "Avg Flight Duration", FormatDuration(AverageFlightDuration) },
                { "Fav Weather",         FavoriteWeatherCondition },
                { "Achievements",        AchievementsUnlocked.ToString() },
                { "Screenshots",         ScreenshotsTaken.ToString() },
                { "Shares",              ShareCount.ToString() },
                { "Daily Streak",        $"{DailyActiveStreak} days" },
            };
        }

        // ── Persistence helpers ───────────────────────────────────────────────────

        private void Persist()
        {
            PlayerPrefs.SetInt(KeyTotalFlights,           TotalFlights);
            PlayerPrefs.SetFloat(KeyTotalFlightTimeSec,   TotalFlightTimeSeconds);
            PlayerPrefs.SetFloat(KeyMaxAltitudeEver,      MaxAltitudeEver);
            PlayerPrefs.SetFloat(KeyMaxSpeedEver,         MaxSpeedEver);
            PlayerPrefs.SetInt(KeyAchievements,           AchievementsUnlocked);
            PlayerPrefs.SetInt(KeyScreenshots,            ScreenshotsTaken);
            PlayerPrefs.SetInt(KeyShares,                 ShareCount);
            PlayerPrefs.SetString(KeyFavoriteWeather,     FavoriteWeatherCondition);
            PlayerPrefs.SetInt(KeyActiveStreak,           DailyActiveStreak);
            SaveWeeklyFlights();
            PlayerPrefs.Save();
        }

        private void LoadWeeklyFlights()
        {
            string raw = PlayerPrefs.GetString(KeyWeeklyFlights, "0,0,0,0,0,0,0");
            string[] parts = raw.Split(',');
            for (int i = 0; i < 7; i++)
                WeeklyFlightCounts[i] = (i < parts.Length && int.TryParse(parts[i], out int v)) ? v : 0;
        }

        private void SaveWeeklyFlights()
        {
            PlayerPrefs.SetString(KeyWeeklyFlights, string.Join(",", WeeklyFlightCounts));
        }

        private void LoadFeatureUsage()
        {
            FeatureUsageCounts.Clear();
            string raw = PlayerPrefs.GetString(KeyFeatureUsage, "");
            if (string.IsNullOrEmpty(raw)) return;
            foreach (string entry in raw.Split(';'))
            {
                int idx = entry.IndexOf('=');
                if (idx > 0 && int.TryParse(entry.Substring(idx + 1), out int cnt))
                    FeatureUsageCounts[entry.Substring(0, idx)] = cnt;
            }
        }

        private void SaveFeatureUsage()
        {
            var parts = new List<string>();
            foreach (var kvp in FeatureUsageCounts)
                parts.Add($"{kvp.Key}={kvp.Value}");
            PlayerPrefs.SetString(KeyFeatureUsage, string.Join(";", parts));
        }

        private void UpdateDailyStreak()
        {
            string today   = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string lastDay = PlayerPrefs.GetString(KeyLastActiveDay, "");

            if (lastDay == today) return; // already counted today

            if (!string.IsNullOrEmpty(lastDay) &&
                DateTime.TryParse(lastDay, out DateTime last) &&
                (DateTime.UtcNow.Date - last.Date).Days == 1)
            {
                DailyActiveStreak++;
            }
            else if (string.IsNullOrEmpty(lastDay))
            {
                DailyActiveStreak = 1;
            }
            else
            {
                DailyActiveStreak = 1; // streak broken
            }

            PlayerPrefs.SetString(KeyLastActiveDay, today);
            PlayerPrefs.SetInt(KeyActiveStreak, DailyActiveStreak);
            PlayerPrefs.Save();
        }

        // ── Formatting helpers ────────────────────────────────────────────────────

        private static string FormatDuration(float seconds)
        {
            int h = Mathf.FloorToInt(seconds / 3600f);
            int m = Mathf.FloorToInt((seconds % 3600f) / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return h > 0 ? $"{h}h {m:00}m" : $"{m}m {s:00}s";
        }

        private static string FormatAltitude(float m)
        {
            return m >= 1000f ? $"{m / 1000f:0.0} km" : $"{m:0} m";
        }

        private static string FormatSpeed(float mps)
        {
            float kmh  = mps * 3.6f;
            float mach = mps / 343f;
            return mps >= 343f ? $"{kmh:0} km/h (Mach {mach:0.0})" : $"{kmh:0} km/h";
        }
    }
}
