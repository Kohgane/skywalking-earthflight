using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace SWEF.Performance
{
    /// <summary>
    /// Advanced frame-time profiler singleton.
    /// Tracks rolling FPS statistics, 1% / 0.1% lows, frame-time histogram,
    /// and GC collection counts. Automatically takes a <see cref="PerformanceSnapshot"/>
    /// every 5 seconds and keeps the last 60 snapshots (5 minutes of data).
    /// </summary>
    public class PerformanceProfiler : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static PerformanceProfiler Instance { get; private set; }

        // ── Constants ────────────────────────────────────────────────────────────
        private const int   RollingWindowSize   = 60;   // frames for rolling avg
        private const int   HistogramBuckets    = 16;   // 0-2,2-4,...,30-32,32ms+
        private const float BucketWidthMs       = 2f;
        private const float SnapshotIntervalSec = 5f;
        private const int   MaxSnapshotHistory  = 60;

        // ── Inspector ────────────────────────────────────────────────────────────
        [SerializeField] private bool startProfilingOnAwake = true;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired every time a new snapshot is taken (every 5 seconds).</summary>
        public event Action<PerformanceSnapshot> OnSnapshotTaken;

        // ── Public state ─────────────────────────────────────────────────────────
        /// <summary>All recorded snapshots, newest last. Capped at 60 entries.</summary>
        public List<PerformanceSnapshot> History { get; } = new List<PerformanceSnapshot>();

        // ── Rolling window ───────────────────────────────────────────────────────
        private readonly float[] _frameTimes = new float[RollingWindowSize];
        private int   _frameIndex;
        private bool  _windowFull;

        private float _snapshotTimer;
        private int   _lastGcCount;

        // ── Frame-time histogram (counts per bucket) ─────────────────────────────
        private readonly int[] _histogram = new int[HistogramBuckets];

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _lastGcCount = GC.CollectionCount(0);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            RecordFrame(dt);

            _snapshotTimer += dt;
            if (_snapshotTimer >= SnapshotIntervalSec)
            {
                _snapshotTimer = 0f;
                TakeSnapshot();
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>
        /// Records a single frame's delta time into the rolling window and histogram.
        /// Called automatically from <see cref="Update"/>, but can also be called by
        /// external systems (e.g. <c>PerformanceManager</c>).
        /// </summary>
        public void RecordFrame(float deltaTime)
        {
            float ms = deltaTime * 1000f;

            _frameTimes[_frameIndex] = ms;
            _frameIndex = (_frameIndex + 1) % RollingWindowSize;
            if (_frameIndex == 0) _windowFull = true;

            int bucket = Mathf.Clamp(Mathf.FloorToInt(ms / BucketWidthMs), 0, HistogramBuckets - 1);
            _histogram[bucket]++;
        }

        /// <summary>Returns the most recently taken <see cref="PerformanceSnapshot"/>,
        /// or a fresh snapshot built from the current frame if none exist yet.</summary>
        public PerformanceSnapshot GetCurrentSnapshot()
        {
            if (History.Count > 0)
                return History[History.Count - 1];
            return BuildSnapshot();
        }

        /// <summary>Clears all recorded snapshot history.</summary>
        public void ResetHistory()
        {
            History.Clear();
        }

        /// <summary>
        /// Writes all snapshot history to a CSV file under
        /// <c>persistentDataPath/Performance/</c>.
        /// </summary>
        public void ExportReport()
        {
            string dir = Path.Combine(Application.persistentDataPath, "Performance");
            Directory.CreateDirectory(dir);

            string filename = $"perf_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string path     = Path.Combine(dir, filename);

            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,AvgFPS,1%Low,0.1%Low,AvgFrameTimeMs,MaxFrameTimeMs,GCCollects,AllocatedMB,UsedHeapMB");
            foreach (var snap in History)
            {
                sb.AppendLine(
                    $"{snap.timestamp:O},{snap.avgFps:F2},{snap.onePercentLow:F2}," +
                    $"{snap.pointOnePercentLow:F2},{snap.avgFrameTimeMs:F2},{snap.maxFrameTimeMs:F2}," +
                    $"{snap.gcCollectCount},{snap.totalAllocatedMB},{snap.usedHeapMB}");
            }

            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[SWEF] PerformanceProfiler: report exported to {path}");
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void TakeSnapshot()
        {
            var snap = BuildSnapshot();

            History.Add(snap);
            if (History.Count > MaxSnapshotHistory)
                History.RemoveAt(0);

            OnSnapshotTaken?.Invoke(snap);
        }

        private PerformanceSnapshot BuildSnapshot()
        {
            int count = _windowFull ? RollingWindowSize : _frameIndex;
            if (count == 0)
                return new PerformanceSnapshot { timestamp = DateTime.Now };

            // Sort a copy for percentile calculation
            float[] sorted = new float[count];
            Array.Copy(_frameTimes, sorted, count);
            Array.Sort(sorted);

            float sum = 0f;
            float max = 0f;
            for (int i = 0; i < count; i++)
            {
                sum += sorted[i];
                if (sorted[i] > max) max = sorted[i];
            }
            float avgMs = sum / count;
            float avgFps = avgMs > 0f ? 1000f / avgMs : 0f;

            // 1% low: worst 1% of frames → lowest FPS from those
            int onePercentIdx = Mathf.Max(0, Mathf.CeilToInt(count * 0.01f) - 1);
            float onePercentLow = sorted[count - 1 - onePercentIdx] > 0f
                ? 1000f / sorted[count - 1 - onePercentIdx]
                : avgFps;

            // 0.1% low
            int ptOnePercentIdx = Mathf.Max(0, Mathf.CeilToInt(count * 0.001f) - 1);
            float pointOnePercentLow = sorted[count - 1 - ptOnePercentIdx] > 0f
                ? 1000f / sorted[count - 1 - ptOnePercentIdx]
                : avgFps;

            int currentGc  = GC.CollectionCount(0);
            int gcDelta    = currentGc - _lastGcCount;
            _lastGcCount   = currentGc;

            return new PerformanceSnapshot
            {
                avgFps              = avgFps,
                onePercentLow       = onePercentLow,
                pointOnePercentLow  = pointOnePercentLow,
                avgFrameTimeMs      = avgMs,
                maxFrameTimeMs      = max,
                gcCollectCount      = gcDelta,
                totalAllocatedMB    = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024),
                usedHeapMB          = GC.GetTotalMemory(false) / (1024 * 1024),
                timestamp           = DateTime.Now,
            };
        }
    }

    // ── Data types ────────────────────────────────────────────────────────────

    /// <summary>
    /// A point-in-time snapshot of performance metrics.
    /// </summary>
    [Serializable]
    public struct PerformanceSnapshot
    {
        /// <summary>Average FPS over the rolling 60-frame window.</summary>
        public float avgFps;

        /// <summary>Lowest FPS from the worst 1 % of frames.</summary>
        public float onePercentLow;

        /// <summary>Lowest FPS from the worst 0.1 % of frames.</summary>
        public float pointOnePercentLow;

        /// <summary>Average frame time in milliseconds.</summary>
        public float avgFrameTimeMs;

        /// <summary>Maximum single-frame time in milliseconds in this window.</summary>
        public float maxFrameTimeMs;

        /// <summary>Number of Gen-0 GC collections since the previous snapshot.</summary>
        public int gcCollectCount;

        /// <summary>Total memory allocated by Unity's native layer (MB).</summary>
        public long totalAllocatedMB;

        /// <summary>Managed heap memory in use (MB).</summary>
        public long usedHeapMB;

        /// <summary>Wall-clock time when this snapshot was taken.</summary>
        public DateTime timestamp;
    }
}
