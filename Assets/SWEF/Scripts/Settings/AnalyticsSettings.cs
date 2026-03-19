using UnityEngine;

namespace SWEF.Settings
{
    /// <summary>
    /// PlayerPrefs-backed settings for the Phase 21 analytics pipeline.
    /// Call <see cref="ApplyToSubSystems"/> after changing values to propagate them.
    /// </summary>
    public class AnalyticsSettings : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static AnalyticsSettings Instance { get; private set; }

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyTelemetryEnabled            = "SWEF_AS_TelemetryEnabled";
        private const string KeyConsentLevel                = "SWEF_AS_ConsentLevel";
        private const string KeyDetailedPerformance         = "SWEF_AS_DetailedPerf";
        private const string KeyShareAnonymousUsage         = "SWEF_AS_ShareAnonymous";
        private const string KeyFlightHeatmap               = "SWEF_AS_FlightHeatmap";

        // ── Properties ───────────────────────────────────────────────────────────
        public bool TelemetryEnabled            { get; private set; } = true;
        public int  ConsentLevelInt             { get; private set; } = 1; // Essential
        public bool DetailedPerformanceTracking { get; private set; } = false;
        public bool ShareAnonymousUsageData     { get; private set; } = true;
        public bool FlightHeatmapEnabled        { get; private set; } = true;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void SetTelemetryEnabled(bool value)
        {
            TelemetryEnabled = value;
            PlayerPrefs.SetInt(KeyTelemetryEnabled, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetConsentLevel(int level)
        {
            ConsentLevelInt = Mathf.Clamp(level, 0, 3);
            PlayerPrefs.SetInt(KeyConsentLevel, ConsentLevelInt);
            PlayerPrefs.Save();
        }

        public void SetDetailedPerformanceTracking(bool value)
        {
            DetailedPerformanceTracking = value;
            PlayerPrefs.SetInt(KeyDetailedPerformance, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetShareAnonymousUsageData(bool value)
        {
            ShareAnonymousUsageData = value;
            PlayerPrefs.SetInt(KeyShareAnonymousUsage, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetFlightHeatmapEnabled(bool value)
        {
            FlightHeatmapEnabled = value;
            PlayerPrefs.SetInt(KeyFlightHeatmap, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Push all settings to their respective sub-systems.</summary>
        public void ApplyToSubSystems()
        {
            var dispatcher = Analytics.TelemetryDispatcher.Instance;
            if (dispatcher != null)
                dispatcher.SetTelemetryEnabled(TelemetryEnabled);

            var pcm = Analytics.PrivacyConsentManager.Instance;
            if (pcm != null)
                pcm.SetConsent((Analytics.PrivacyConsentManager.ConsentLevel)ConsentLevelInt);
        }

        /// <summary>Reset all settings to factory defaults and apply.</summary>
        public void ResetToDefaults()
        {
            SetTelemetryEnabled(true);
            SetConsentLevel(1);
            SetDetailedPerformanceTracking(false);
            SetShareAnonymousUsageData(true);
            SetFlightHeatmapEnabled(true);
            ApplyToSubSystems();
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void Load()
        {
            TelemetryEnabled            = PlayerPrefs.GetInt(KeyTelemetryEnabled,    1) == 1;
            ConsentLevelInt             = PlayerPrefs.GetInt(KeyConsentLevel,         1);
            DetailedPerformanceTracking = PlayerPrefs.GetInt(KeyDetailedPerformance, 0) == 1;
            ShareAnonymousUsageData     = PlayerPrefs.GetInt(KeyShareAnonymousUsage, 1) == 1;
            FlightHeatmapEnabled        = PlayerPrefs.GetInt(KeyFlightHeatmap,        1) == 1;
        }
    }
}
