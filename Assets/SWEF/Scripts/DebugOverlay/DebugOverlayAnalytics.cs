// DebugOverlayAnalytics.cs — SWEF Performance Profiler & Debug Overlay
using System;
using UnityEngine;

namespace SWEF.DebugOverlay
{
    /// <summary>
    /// Static utility class that accumulates session-level performance analytics.
    /// Call <see cref="RecordFrameStats"/> once per frame and
    /// <see cref="RecordMemorySnapshot"/> periodically to build up a
    /// <see cref="PerformanceSummary"/> retrievable at any time.
    /// </summary>
    public static class DebugOverlayAnalytics
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        #region State

        private static long   _totalFrames;
        private static double _fpsSumAccumulator;      // use double to avoid precision loss
        private static float  _lowestFPS        = float.MaxValue;
        private static float  _highestFPS       = float.MinValue;
        private static float  _peakMemoryMB;
        private static int    _totalGCCollections;
        private static float  _sessionStartTime;       // Time.realtimeSinceStartup at first record

        private static bool   _initialized;

        #endregion

        #region Public API

        /// <summary>
        /// Records FPS and frame-time data for the current frame.
        /// Should be called once per rendered frame.
        /// </summary>
        /// <param name="fps">Instantaneous FPS for this frame.</param>
        /// <param name="frameTimeMs">Frame time in milliseconds for this frame.</param>
        public static void RecordFrameStats(float fps, float frameTimeMs)
        {
            if (!_initialized) Initialize();
            if (fps <= 0f) return;

            _totalFrames++;
            _fpsSumAccumulator += fps;

            if (fps < _lowestFPS)  _lowestFPS  = fps;
            if (fps > _highestFPS) _highestFPS = fps;
        }

        /// <summary>
        /// Records a memory snapshot to track peak managed-heap usage and GC events.
        /// </summary>
        /// <param name="snapshot">The <see cref="MemorySnapshot"/> to record.</param>
        public static void RecordMemorySnapshot(MemorySnapshot snapshot)
        {
            if (!_initialized) Initialize();

            if (snapshot.allocatedManagedMB > _peakMemoryMB)
                _peakMemoryMB = snapshot.allocatedManagedMB;

            // Count any new GC collections
            int currentGen0 = GC.CollectionCount(0);
            int currentGen1 = GC.CollectionCount(1);
            int currentGen2 = GC.CollectionCount(2);
            int total = currentGen0 + currentGen1 + currentGen2;

            // _totalGCCollections is reset on Initialize; delta approach would
            // require storing previous counts, so we expose the live total instead.
            _totalGCCollections = total;
        }

        /// <summary>
        /// Returns a <see cref="PerformanceSummary"/> aggregating all data recorded
        /// since the session started (or since the last <see cref="Reset"/> call).
        /// </summary>
        public static PerformanceSummary GetPerformanceSummary()
        {
            if (!_initialized) Initialize();

            float sessionDuration = Time.realtimeSinceStartup - _sessionStartTime;
            float avgFPS = _totalFrames > 0
                ? (float)(_fpsSumAccumulator / _totalFrames)
                : 0f;

            return new PerformanceSummary
            {
                sessionAverageFPS     = avgFPS,
                sessionLowestFPS      = _lowestFPS == float.MaxValue ? 0f : _lowestFPS,
                peakMemoryMB          = _peakMemoryMB,
                totalGCCollections    = _totalGCCollections,
                totalFrames           = _totalFrames,
                sessionDurationSeconds = sessionDuration
            };
        }

        /// <summary>Resets all accumulated analytics data.</summary>
        public static void Reset()
        {
            _initialized        = false;
            Initialize();
        }

        #endregion

        #region Private Helpers

        private static void Initialize()
        {
            _totalFrames          = 0;
            _fpsSumAccumulator    = 0.0;
            _lowestFPS            = float.MaxValue;
            _highestFPS           = float.MinValue;
            _peakMemoryMB         = 0f;
            _totalGCCollections   = 0;
            _sessionStartTime     = Time.realtimeSinceStartup;
            _initialized          = true;
        }

        #endregion
#else
        // Release-build stubs — no overhead in shipping builds.

        /// <summary>No-op in release builds.</summary>
        public static void RecordFrameStats(float fps, float frameTimeMs) { }

        /// <summary>No-op in release builds.</summary>
        public static void RecordMemorySnapshot(MemorySnapshot snapshot) { }

        /// <summary>Returns an empty summary in release builds.</summary>
        public static PerformanceSummary GetPerformanceSummary() => default;

        /// <summary>No-op in release builds.</summary>
        public static void Reset() { }
#endif
    }
}
