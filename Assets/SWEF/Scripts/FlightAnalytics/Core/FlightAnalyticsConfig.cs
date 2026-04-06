// FlightAnalyticsConfig.cs — Phase 116: Flight Analytics Dashboard
// ScriptableObject configuration for the flight analytics system.
// Namespace: SWEF.FlightAnalytics

using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — ScriptableObject that controls all tuneable parameters for the
    /// Flight Analytics Dashboard: data retention, sampling rates, heatmap resolution,
    /// export preferences, and privacy settings.
    ///
    /// <para>Create via <em>Assets → Create → SWEF/FlightAnalytics/Analytics Config</em>.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/FlightAnalytics/Analytics Config", fileName = "FlightAnalyticsConfig")]
    public class FlightAnalyticsConfig : ScriptableObject
    {
        // ── Data Retention ────────────────────────────────────────────────────────

        [Header("Data Retention")]
        [Tooltip("Number of days to keep recorded flight sessions. 0 = keep forever.")]
        [Range(0, 365)]
        public int dataRetentionDays = 90;

        [Tooltip("Maximum number of flight sessions to store. Oldest are purged first. 0 = unlimited.")]
        [Range(0, 10000)]
        public int maxStoredSessions = 500;

        // ── Sampling ──────────────────────────────────────────────────────────────

        [Header("Sampling")]
        [Tooltip("How many times per second flight data is sampled during a session.")]
        [Range(0.1f, 60f)]
        public float samplingRateHz = 2f;

        [Tooltip("Minimum distance (metres) the aircraft must move before a new data point is recorded.")]
        [Range(0f, 1000f)]
        public float minSampleDistanceM = 50f;

        // ── Heatmap ───────────────────────────────────────────────────────────────

        [Header("Heatmap")]
        [Tooltip("Number of cells along each axis of the world-space heatmap grid.")]
        [Range(16, 512)]
        public int heatmapResolution = 128;

        [Tooltip("World-space size (metres) covered by the full heatmap grid.")]
        public float heatmapWorldSize = 200000f;

        [Tooltip("Number of cells along each axis of the landing-zone heatmap.")]
        [Range(16, 256)]
        public int landingHeatmapResolution = 64;

        // ── Export ────────────────────────────────────────────────────────────────

        [Header("Export")]
        [Tooltip("Default format when the player exports an analytics report.")]
        public ExportFormat defaultExportFormat = ExportFormat.JSON;

        [Tooltip("Subfolder under Application.persistentDataPath where exports are written.")]
        public string exportSubfolder = "FlightAnalytics/Exports";

        // ── Privacy ───────────────────────────────────────────────────────────────

        [Header("Privacy")]
        [Tooltip("When true, absolute GPS/world positions are omitted from exports.")]
        public bool anonymisePositions = false;

        [Tooltip("When true, flight data is not submitted to community leaderboards.")]
        public bool optOutOfLeaderboards = false;

        [Tooltip("When true, aggregate usage data about the analytics dashboard itself is collected.")]
        public bool allowDashboardTelemetry = true;

        // ── Performance Scoring ───────────────────────────────────────────────────

        [Header("Performance Scoring")]
        [Tooltip("Weight of landing quality in the overall performance score (0–1).")]
        [Range(0f, 1f)]
        public float landingScoreWeight = 0.35f;

        [Tooltip("Weight of fuel efficiency in the overall performance score (0–1).")]
        [Range(0f, 1f)]
        public float fuelScoreWeight = 0.25f;

        [Tooltip("Weight of navigation accuracy in the overall performance score (0–1).")]
        [Range(0f, 1f)]
        public float navigationScoreWeight = 0.25f;

        [Tooltip("Weight of flight smoothness in the overall performance score (0–1).")]
        [Range(0f, 1f)]
        public float smoothnessScoreWeight = 0.15f;
    }
}
