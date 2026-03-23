// FPSCounter.cs — SWEF Performance Profiler & Debug Overlay
using System;
using UnityEngine;

namespace SWEF.DebugOverlay
{
    /// <summary>
    /// MonoBehaviour that calculates and exposes FPS statistics including rolling
    /// average, min/max, 1% low, 0.1% low, and a history buffer for graph rendering.
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        #region Inspector Fields

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [Header("FPS Counter Configuration")]
        [Tooltip("Number of frames to include in the rolling average window.")]
        [SerializeField] private int rollingWindowSize = 60;

        [Tooltip("FPS threshold below which the state becomes Warning.")]
        [SerializeField] private int warningThreshold = 30;

        [Tooltip("FPS threshold below which the state becomes Critical.")]
        [SerializeField] private int criticalThreshold = 20;

        [Header("Graph")]
        [Tooltip("Maximum number of FPS samples stored for the graph.")]
        [SerializeField] private int maxGraphSamples = 128;
#endif

        #endregion

        #region Private State

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        // Rolling window
        private float[] _rollingBuffer;
        private int     _rollingIndex;
        private int     _rollingCount;

        // Session extremes
        private float _minFPS = float.MaxValue;
        private float _maxFPS = float.MinValue;

        // Percentile lows (capped ring buffer, sorted on demand)
        private float[] _sessionFrameTimes;
        private int     _sessionFrameIndex;
        private int     _sessionFrameCount;
        private const int MaxSessionFrameSamples = 18000; // ~5 min @ 60 fps

        // Graph history (ring buffer)
        private float[] _graphBuffer;
        private int     _graphIndex;
        private int     _graphCount;

        // Current frame values
        private float _currentFPS;
        private float _frameTimeMs;
#endif

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _rollingBuffer      = new float[Mathf.Max(1, rollingWindowSize)];
            _graphBuffer        = new float[Mathf.Max(1, maxGraphSamples)];
            _sessionFrameTimes  = new float[MaxSessionFrameSamples];
#endif
        }

        private void Update()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            _frameTimeMs = dt * 1000f;
            _currentFPS  = 1f / dt;

            // Rolling window
            _rollingBuffer[_rollingIndex] = _currentFPS;
            _rollingIndex = (_rollingIndex + 1) % _rollingBuffer.Length;
            if (_rollingCount < _rollingBuffer.Length) _rollingCount++;

            // Session min/max
            if (_currentFPS < _minFPS) _minFPS = _currentFPS;
            if (_currentFPS > _maxFPS) _maxFPS = _currentFPS;

            // Percentile tracking (capped ring buffer)
            _sessionFrameTimes[_sessionFrameIndex] = _frameTimeMs;
            _sessionFrameIndex = (_sessionFrameIndex + 1) % MaxSessionFrameSamples;
            if (_sessionFrameCount < MaxSessionFrameSamples) _sessionFrameCount++;

            // Graph buffer (ring)
            _graphBuffer[_graphIndex] = _currentFPS;
            _graphIndex = (_graphIndex + 1) % _graphBuffer.Length;
            if (_graphCount < _graphBuffer.Length) _graphCount++;
#endif
        }

        #endregion

        #region Public API

        /// <summary>Returns the instantaneous FPS for the current frame.</summary>
        public float GetCurrentFPS()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            return _currentFPS;
#else
            return 0f;
#endif
        }

        /// <summary>Returns the rolling average FPS over the configured sample window.</summary>
        public float GetAverageFPS()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_rollingCount == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < _rollingCount; i++)
                sum += _rollingBuffer[i];
            return sum / _rollingCount;
#else
            return 0f;
#endif
        }

        /// <summary>Returns the minimum FPS recorded since tracking began.</summary>
        public float GetMinFPS()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            return _minFPS == float.MaxValue ? 0f : _minFPS;
#else
            return 0f;
#endif
        }

        /// <summary>Returns the maximum FPS recorded since tracking began.</summary>
        public float GetMaxFPS()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            return _maxFPS == float.MinValue ? 0f : _maxFPS;
#else
            return 0f;
#endif
        }

        /// <summary>Returns the current frame time in milliseconds.</summary>
        public float GetFrameTimeMs()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            return _frameTimeMs;
#else
            return 0f;
#endif
        }

        /// <summary>
        /// Returns the 1% low FPS — the average of the slowest 1% of frames this session.
        /// </summary>
        public float GetOnePercentLow()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            return ComputePercentileLow(0.01f);
#else
            return 0f;
#endif
        }

        /// <summary>
        /// Returns the 0.1% low FPS — the average of the slowest 0.1% of frames this session.
        /// </summary>
        public float GetPointOnePercentLow()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            return ComputePercentileLow(0.001f);
#else
            return 0f;
#endif
        }

        /// <summary>
        /// Returns the current <see cref="PerformanceThreshold"/> based on
        /// the average FPS versus the configured thresholds.
        /// </summary>
        public PerformanceThreshold GetCurrentState()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            float avg = GetAverageFPS();
            if (avg < criticalThreshold) return PerformanceThreshold.Critical;
            if (avg < warningThreshold)  return PerformanceThreshold.Warning;
            return PerformanceThreshold.Good;
#else
            return PerformanceThreshold.Good;
#endif
        }

        /// <summary>
        /// Fills <paramref name="sampleCount"/> most-recent FPS values into a new array
        /// suitable for graph rendering (oldest → newest order).
        /// </summary>
        /// <param name="sampleCount">Number of samples to return (clamped to history size).</param>
        public float[] GetFPSHistory(int sampleCount)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            int count = Mathf.Clamp(sampleCount, 1, _graphCount);
            float[] result = new float[count];
            int bufLen = _graphBuffer.Length;
            // Walk backwards from the current write-head
            int start = (_graphIndex - _graphCount + bufLen) % bufLen;
            int skip  = Mathf.Max(0, _graphCount - count);
            for (int i = 0; i < count; i++)
                result[i] = _graphBuffer[(start + skip + i) % bufLen];
            return result;
#else
            return Array.Empty<float>();
#endif
        }

        /// <summary>Resets all session statistics (min/max, percentile lists, graph).</summary>
        public void ResetStats()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _minFPS = float.MaxValue;
            _maxFPS = float.MinValue;
            _sessionFrameCount = 0;
            _sessionFrameIndex = 0;
            _rollingCount = 0;
            _rollingIndex = 0;
            _graphCount   = 0;
            _graphIndex   = 0;
#endif
        }

        #endregion

        #region Private Helpers

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private float ComputePercentileLow(float fraction)
        {
            if (_sessionFrameCount == 0) return 0f;
            // Copy valid entries into a temp array and sort
            var temp = new float[_sessionFrameCount];
            int start = (_sessionFrameIndex - _sessionFrameCount + MaxSessionFrameSamples) % MaxSessionFrameSamples;
            for (int i = 0; i < _sessionFrameCount; i++)
                temp[i] = _sessionFrameTimes[(start + i) % MaxSessionFrameSamples];
            Array.Sort(temp); // ascending frame-time
            int takeCount = Mathf.Max(1, Mathf.RoundToInt(temp.Length * fraction));
            // Take the top (highest) frame-times → slowest frames
            float sum = 0f;
            int startIdx = temp.Length - takeCount;
            for (int i = startIdx; i < temp.Length; i++)
                sum += temp[i];
            float avgFrameTimeMs = sum / takeCount;
            return avgFrameTimeMs > 0f ? 1000f / avgFrameTimeMs : 0f;
        }
#endif

        #endregion
    }
}
