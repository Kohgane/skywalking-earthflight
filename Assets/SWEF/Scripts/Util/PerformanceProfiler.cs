using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Util
{
    /// <summary>
    /// Runtime performance measurement tool.
    /// Tracks frame times over a configurable sliding window and exposes aggregate metrics.
    /// Attach to any active <see cref="GameObject"/> in the scene.
    /// </summary>
    public class PerformanceProfiler : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [SerializeField] private int frameWindow = 300;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when a <see cref="StartBenchmark"/> run finishes, passing the report string.</summary>
        public event Action<string> OnBenchmarkComplete;

        // ── State ─────────────────────────────────────────────────────────────

        private float[] _frameTimes;
        private int     _head;        // next write index (circular)
        private int     _count;       // number of valid samples

        private bool    _benchmarkRunning;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Average FPS over the current sample window.</summary>
        public float AverageFPS => _count == 0 ? 0f : 1f / AverageFrameTimeMs * 1000f;

        /// <summary>Minimum FPS seen in the current window.</summary>
        public float MinFPS
        {
            get
            {
                float maxTime = MaxFrameTimeInWindow();
                return maxTime <= 0f ? 0f : 1f / maxTime * 1000f;
            }
        }

        /// <summary>Maximum FPS seen in the current window.</summary>
        public float MaxFPS
        {
            get
            {
                float minTime = MinFrameTimeInWindow();
                return minTime <= 0f ? 0f : 1f / minTime * 1000f;
            }
        }

        /// <summary>Mean frame time in milliseconds over the current window.</summary>
        public float AverageFrameTimeMs
        {
            get
            {
                if (_count == 0) return 0f;
                float sum = 0f;
                for (int i = 0; i < _count; i++)
                    sum += _frameTimes[i];
                return sum / _count * 1000f;
            }
        }

        /// <summary>
        /// 99th percentile frame time in milliseconds (worst 1 % of frames).
        /// </summary>
        public float FrameTimeP99
        {
            get
            {
                if (_count == 0) return 0f;

                float[] sorted = new float[_count];
                Array.Copy(_frameTimes, sorted, _count);
                Array.Sort(sorted);

                int idx = Mathf.Clamp((int)(_count * 0.99f), 0, _count - 1);
                return sorted[idx] * 1000f;
            }
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            int size   = Mathf.Max(1, frameWindow);
            _frameTimes = new float[size];
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            _frameTimes[_head] = dt;
            _head = (_head + 1) % _frameTimes.Length;
            if (_count < _frameTimes.Length) _count++;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Runs a benchmark for <paramref name="durationSec"/> seconds, then logs and fires
        /// <see cref="OnBenchmarkComplete"/> with the formatted report.
        /// Only one benchmark can run at a time; subsequent calls are ignored while one is active.
        /// </summary>
        /// <param name="durationSec">Duration of the benchmark in seconds (minimum 0.1 s).</param>
        public void StartBenchmark(float durationSec)
        {
            if (_benchmarkRunning)
            {
                Debug.LogWarning("[SWEF] PerformanceProfiler: benchmark already running.");
                return;
            }
            StartCoroutine(BenchmarkCoroutine(Mathf.Max(0.1f, durationSec)));
        }

        /// <summary>
        /// Returns a formatted multi-line string containing all current metrics.
        /// </summary>
        public string GetReport()
        {
            return $"[SWEF] PerformanceProfiler Report\n" +
                   $"  Window   : {_count} frames\n" +
                   $"  Avg FPS  : {AverageFPS:F1}\n" +
                   $"  Min FPS  : {MinFPS:F1}\n" +
                   $"  Max FPS  : {MaxFPS:F1}\n" +
                   $"  Avg FT   : {AverageFrameTimeMs:F2} ms\n" +
                   $"  P99 FT   : {FrameTimeP99:F2} ms";
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private IEnumerator BenchmarkCoroutine(float durationSec)
        {
            _benchmarkRunning = true;
            Debug.Log($"[SWEF] PerformanceProfiler: benchmark started ({durationSec:F1}s).");

            // Reset buffer for a clean run
            _count = 0;
            _head  = 0;

            yield return new WaitForSecondsRealtime(durationSec);

            string report = GetReport();
            Debug.Log(report);
            OnBenchmarkComplete?.Invoke(report);
            _benchmarkRunning = false;
        }

        private float MinFrameTimeInWindow()
        {
            if (_count == 0) return 0f;
            float min = float.MaxValue;
            for (int i = 0; i < _count; i++)
                if (_frameTimes[i] < min) min = _frameTimes[i];
            return min;
        }

        private float MaxFrameTimeInWindow()
        {
            if (_count == 0) return 0f;
            float max = 0f;
            for (int i = 0; i < _count; i++)
                if (_frameTimes[i] > max) max = _frameTimes[i];
            return max;
        }
    }
}
