// SpatialAudioAnalytics.cs — Phase 118: Spatial Audio & 3D Soundscape
// Telemetry: audio settings usage, HRTF adoption, quality distribution, performance impact.
// Namespace: SWEF.SpatialAudio

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Collects and reports spatial audio telemetry: HRTF adoption rate, quality preset
    /// distribution, peak audio source count, and average audio CPU cost.
    /// </summary>
    public class SpatialAudioAnalytics : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared instance of <see cref="SpatialAudioAnalytics"/>.</summary>
        public static SpatialAudioAnalytics Instance { get; private set; }

        // ── State ─────────────────────────────────────────────────────────────────

        private readonly List<SpatialAudioAnalyticsRecord> _sessionRecords =
            new List<SpatialAudioAnalyticsRecord>();

        private int   _peakActiveSources;
        private float _totalAudioCpuMs;
        private int   _frameSamples;

        private bool  _hrtfEnabled;
        private string _qualityPreset = "Medium";

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            SampleFrame();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Records that the HRTF setting was changed.</summary>
        public void RecordHRTFToggle(bool enabled)
        {
            _hrtfEnabled = enabled;
            Debug.Log($"[SpatialAudioAnalytics] HRTF toggled: {enabled}");
        }

        /// <summary>Records a quality preset change.</summary>
        public void RecordQualityPreset(string preset)
        {
            _qualityPreset = preset;
        }

        /// <summary>Records the current active source count for peak tracking.</summary>
        public void RecordActiveSourceCount(int count)
        {
            if (count > _peakActiveSources)
                _peakActiveSources = count;
        }

        /// <summary>Finalises and saves the current session analytics record.</summary>
        public SpatialAudioAnalyticsRecord FinaliseSession()
        {
            float avgCpu = _frameSamples > 0 ? _totalAudioCpuMs / _frameSamples : 0f;
            var record = new SpatialAudioAnalyticsRecord
            {
                timestamp        = System.DateTime.UtcNow,
                hrtfEnabled      = _hrtfEnabled,
                qualityPreset    = _qualityPreset,
                peakActiveSources = _peakActiveSources,
                avgAudioCpuMs   = avgCpu
            };
            _sessionRecords.Add(record);
            ResetCounters();
            return record;
        }

        /// <summary>Returns all recorded session analytics records.</summary>
        public IReadOnlyList<SpatialAudioAnalyticsRecord> GetAllRecords() => _sessionRecords;

        // ── Private ───────────────────────────────────────────────────────────────

        private void SampleFrame()
        {
            // Approximate audio CPU cost using Unity's profiler output (placeholder)
            _totalAudioCpuMs += 0.1f;
            _frameSamples++;
        }

        private void ResetCounters()
        {
            _peakActiveSources = 0;
            _totalAudioCpuMs   = 0f;
            _frameSamples      = 0;
        }
    }
}
