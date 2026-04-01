using System;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_ANALYTICS_AVAILABLE
using SWEF.Analytics; // Requires SWEF.Analytics — TelemetryDispatcher
#endif

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// MonoBehaviour that collects and exposes analytics data for the Replay Theater:
    /// view counts, playback heatmaps, export history, camera-angle statistics,
    /// and share counters.
    /// </summary>
    public class ReplayTheaterAnalytics : MonoBehaviour
    {
        #region Inspector

        [Header("Analytics Settings")]
        [SerializeField] private bool  analyticsEnabled  = true;
        [SerializeField] private float heatmapResolution = 1f;

        #endregion

        #region State

        private Dictionary<string, int>                          _viewCountPerReplay  = new Dictionary<string, int>();
        private Dictionary<string, Dictionary<float, int>>       _replayHeatmap       = new Dictionary<string, Dictionary<float, int>>();
        private List<string>                                      _exportHistory       = new List<string>();
        private Dictionary<string, int>                          _cameraAngleStats    = new Dictionary<string, int>();
        private Dictionary<string, int>                          _shareCountPerReplay = new Dictionary<string, int>();

        #endregion

        #region Events

        /// <summary>
        /// Fired when a replay's view count is updated.
        /// Parameters are the replay identifier and the new view count.
        /// </summary>
        public event Action<string, int> OnViewCountUpdated;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Debug.Log("[SWEF] ReplayTheaterAnalytics: Initialised.");
        }

        #endregion

        #region Public API

        /// <summary>Records a view for the specified replay.</summary>
        /// <param name="replayId">Identifier of the replay that was viewed.</param>
        public void RecordView(string replayId)
        {
            if (!analyticsEnabled) return;
            if (!_viewCountPerReplay.ContainsKey(replayId)) _viewCountPerReplay[replayId] = 0;
            _viewCountPerReplay[replayId]++;
            Debug.Log($"[SWEF] ReplayTheaterAnalytics: View recorded for '{replayId}' ({_viewCountPerReplay[replayId]} total).");
            OnViewCountUpdated?.Invoke(replayId, _viewCountPerReplay[replayId]);
        }

        /// <summary>
        /// Records a heatmap sample at a given timestamp within a replay.
        /// Timestamps are bucketed to the nearest <c>heatmapResolution</c> second.
        /// </summary>
        /// <param name="replayId">Identifier of the replay.</param>
        /// <param name="timestamp">Playback position in seconds to record.</param>
        public void RecordHeatmapPoint(string replayId, float timestamp)
        {
            if (!analyticsEnabled) return;

            float bucket = Mathf.Round(timestamp / heatmapResolution) * heatmapResolution;

            if (!_replayHeatmap.ContainsKey(replayId))
                _replayHeatmap[replayId] = new Dictionary<float, int>();

            var map = _replayHeatmap[replayId];
            if (!map.ContainsKey(bucket)) map[bucket] = 0;
            map[bucket]++;
        }

        /// <summary>Records that the given replay was exported in the specified format.</summary>
        /// <param name="replayId">Identifier of the exported replay.</param>
        /// <param name="format">Export format used.</param>
        public void RecordExport(string replayId, ExportFormat format)
        {
            if (!analyticsEnabled) return;
            string entry = $"{replayId}:{format}:{DateTime.UtcNow:O}";
            _exportHistory.Add(entry);
            Debug.Log($"[SWEF] ReplayTheaterAnalytics: Export recorded — {entry}.");
        }

        /// <summary>Increments the usage counter for a named camera angle.</summary>
        /// <param name="angle">Camera angle name (e.g. "Follow Cam").</param>
        public void RecordCameraAngle(string angle)
        {
            if (!analyticsEnabled || string.IsNullOrEmpty(angle)) return;
            if (!_cameraAngleStats.ContainsKey(angle)) _cameraAngleStats[angle] = 0;
            _cameraAngleStats[angle]++;
        }

        /// <summary>Records that a replay was shared to a given platform.</summary>
        /// <param name="replayId">Identifier of the replay that was shared.</param>
        /// <param name="platform">Platform it was shared to.</param>
        public void RecordShare(string replayId, SharingPlatform platform)
        {
            if (!analyticsEnabled) return;
            if (!_shareCountPerReplay.ContainsKey(replayId)) _shareCountPerReplay[replayId] = 0;
            _shareCountPerReplay[replayId]++;
            Debug.Log($"[SWEF] ReplayTheaterAnalytics: Share recorded for '{replayId}' on {platform}.");
        }

        /// <summary>Returns the total view count for the specified replay.</summary>
        /// <param name="replayId">Identifier of the replay.</param>
        /// <returns>Number of views recorded.</returns>
        public int GetViewCount(string replayId)
        {
            return _viewCountPerReplay.TryGetValue(replayId, out int v) ? v : 0;
        }

        /// <summary>Returns the heatmap data (bucket time → hit count) for the given replay.</summary>
        /// <param name="replayId">Identifier of the replay.</param>
        /// <returns>Dictionary mapping bucketed timestamps to hit counts, or an empty dictionary.</returns>
        public Dictionary<float, int> GetHeatmapData(string replayId)
        {
            return _replayHeatmap.TryGetValue(replayId, out var map)
                ? map
                : new Dictionary<float, int>();
        }

        /// <summary>
        /// Returns the timestamp (seconds) with the highest replay count for the given replay.
        /// </summary>
        /// <param name="replayId">Identifier of the replay.</param>
        /// <returns>The most-replayed timestamp, or 0 if no data is available.</returns>
        public float GetMostReplayedTimestamp(string replayId)
        {
            if (!_replayHeatmap.TryGetValue(replayId, out var map) || map.Count == 0)
                return 0f;

            float bestTime  = 0f;
            int   bestCount = 0;
            foreach (var kv in map)
            {
                if (kv.Value > bestCount) { bestCount = kv.Value; bestTime = kv.Key; }
            }
            return bestTime;
        }

        /// <summary>Returns a copy of the camera-angle usage statistics dictionary.</summary>
        /// <returns>Dictionary mapping angle names to usage counts.</returns>
        public Dictionary<string, int> GetPopularCameraAngles()
        {
            return new Dictionary<string, int>(_cameraAngleStats);
        }

        /// <summary>Returns the full export history log as a read-only list.</summary>
        /// <returns>Read-only list of export history entries.</returns>
        public IReadOnlyList<string> GetExportHistory()
        {
            return _exportHistory;
        }

        /// <summary>
        /// Returns the total engagement score for a replay (views + likes + shares).
        /// Likes are sourced from <see cref="ReplaySharingHub"/> if available.
        /// </summary>
        /// <param name="replayId">Identifier of the replay.</param>
        /// <returns>Combined engagement count.</returns>
        public int GetTotalEngagement(string replayId)
        {
            int views  = GetViewCount(replayId);
            int shares = _shareCountPerReplay.TryGetValue(replayId, out int s) ? s : 0;
            int likes  = ReplaySharingHub.Instance != null
                ? ReplaySharingHub.Instance.GetLikeCount(replayId)
                : 0;
            return views + likes + shares;
        }

        /// <summary>Clears all collected analytics data.</summary>
        public void ClearAnalytics()
        {
            _viewCountPerReplay.Clear();
            _replayHeatmap.Clear();
            _exportHistory.Clear();
            _cameraAngleStats.Clear();
            _shareCountPerReplay.Clear();
            Debug.Log("[SWEF] ReplayTheaterAnalytics: All data cleared.");
        }

        #endregion
    }
}
