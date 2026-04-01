// TerrainSurveyAnalytics.cs — SWEF Terrain Scanning & Geological Survey System
using System.Collections.Generic;
using UnityEngine;

// Optional dependency guard — TelemetryDispatcher
#if SWEF_ANALYTICS_AVAILABLE
using SWEF.Analytics;
#endif

namespace SWEF.TerrainSurvey
{
    /// <summary>
    /// Collects telemetry events for the Terrain Survey system and dispatches them
    /// via <c>TelemetryDispatcher</c> (null-safe, compile-guarded).
    /// Also flushes a session summary on application quit.
    /// </summary>
    public class TerrainSurveyAnalytics : MonoBehaviour
    {
        // ── Session counters ──────────────────────────────────────────────────────
        private static int   _sessionScans        = 0;
        private static int   _sessionPOIs         = 0;
        private static float _sessionAreaCovered  = 0f;

        private static readonly HashSet<GeologicalFeatureType> _uniqueFeaturesSession =
            new HashSet<GeologicalFeatureType>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (TerrainScannerController.Instance != null)
            {
                TerrainScannerController.Instance.OnScanStarted   += OnScanStarted;
                TerrainScannerController.Instance.OnScanCompleted += OnScanCompleted;
            }

            if (SurveyPOIManager.Instance != null)
                SurveyPOIManager.Instance.OnPOIDiscovered += OnPOIDiscovered;
        }

        private void OnDisable()
        {
            if (TerrainScannerController.Instance != null)
            {
                TerrainScannerController.Instance.OnScanStarted   -= OnScanStarted;
                TerrainScannerController.Instance.OnScanCompleted -= OnScanCompleted;
            }

            if (SurveyPOIManager.Instance != null)
                SurveyPOIManager.Instance.OnPOIDiscovered -= OnPOIDiscovered;
        }

        private void OnApplicationQuit()
        {
            FlushSessionSummary();
        }

        // ── Static event trackers (called from other components) ──────────────────

        /// <summary>Called by <see cref="TerrainSurveyHUD"/> when the user changes survey mode.</summary>
        public static void TrackModeChanged(SurveyMode mode)
        {
            Dispatch("survey_mode_changed", new Dictionary<string, object>
            {
                { "mode", mode.ToString() }
            });
        }

        /// <summary>Called by <see cref="TerrainSurveyUI"/> when the catalog is opened.</summary>
        public static void TrackCatalogOpened()
        {
            Dispatch("survey_catalog_opened", null);
        }

        /// <summary>Called by <see cref="TerrainSurveyUI"/> when the user navigates to a POI.</summary>
        public static void TrackNavigateToPOI(SurveyPOI poi)
        {
            if (poi == null) return;
            Dispatch("survey_navigate_to_poi", new Dictionary<string, object>
            {
                { "poi_id",       poi.id },
                { "feature_type", poi.featureType.ToString() },
            });
        }

        // ── Private event handlers ────────────────────────────────────────────────

        private void OnScanStarted()
        {
            _sessionScans++;
            Dispatch("survey_scan_started", new Dictionary<string, object>
            {
                { "session_scan_count", _sessionScans }
            });
        }

        private void OnScanCompleted(SurveySample[] samples)
        {
            if (samples == null) return;
            // Approximate area covered by the scan grid (scanRadius² * π)
            // We use the bounding box of the sample set as a proxy.
            if (samples.Length > 1)
            {
                float minX = samples[0].position.x, maxX = samples[0].position.x;
                float minZ = samples[0].position.z, maxZ = samples[0].position.z;
                foreach (var s in samples)
                {
                    if (s.position.x < minX) minX = s.position.x;
                    if (s.position.x > maxX) maxX = s.position.x;
                    if (s.position.z < minZ) minZ = s.position.z;
                    if (s.position.z > maxZ) maxZ = s.position.z;
                }
                _sessionAreaCovered += (maxX - minX) * (maxZ - minZ);
            }

            foreach (var s in samples)
                _uniqueFeaturesSession.Add(s.featureType);
        }

        private void OnPOIDiscovered(SurveyPOI poi)
        {
            if (poi == null) return;
            _sessionPOIs++;

            Dispatch("survey_poi_discovered", new Dictionary<string, object>
            {
                { "poi_id",             poi.id },
                { "feature_type",       poi.featureType.ToString() },
                { "altitude",           poi.position.y },
                { "session_poi_count",  _sessionPOIs },
            });
        }

        private void FlushSessionSummary()
        {
            Dispatch("survey_session_summary", new Dictionary<string, object>
            {
                { "total_scans",       _sessionScans },
                { "unique_features",   _uniqueFeaturesSession.Count },
                { "pois_discovered",   _sessionPOIs },
                { "area_covered_m2",   _sessionAreaCovered },
            });
        }

        // ── Dispatch helper ───────────────────────────────────────────────────────

        private static void Dispatch(string eventName, Dictionary<string, object> properties)
        {
#if SWEF_ANALYTICS_AVAILABLE
            if (TelemetryDispatcher.Instance == null) return;
            TelemetryDispatcher.Instance.Track(eventName, properties);
#else
            string propsStr = properties != null
                ? string.Join(", ", properties)
                : "(none)";
            Debug.Log($"[SurveyAnalytics] {eventName} — {propsStr}");
#endif
        }
    }
}
