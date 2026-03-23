// MemoryProfiler.cs — SWEF Performance Profiler & Debug Overlay
using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace SWEF.DebugOverlay
{
    /// <summary>
    /// MonoBehaviour that monitors managed heap usage, detects memory spikes,
    /// and optionally logs periodic snapshots. Fires events on spikes and GC collections.
    /// </summary>
    public class MemoryProfiler : MonoBehaviour
    {
        #region Inspector Fields

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [Header("Memory Profiler Configuration")]
        [Tooltip("How often (seconds) memory is polled.")]
        [SerializeField] private float pollInterval = 1f;

        [Tooltip("Allocated managed memory increase (MB) within one poll that counts as a spike.")]
        [SerializeField] private float spikeThresholdMB = 50f;

        [Tooltip("Enable periodic logging of memory snapshots to the Unity console.")]
        [SerializeField] private bool enablePeriodicLogging;

        [Tooltip("Interval (seconds) between periodic log entries when enabled.")]
        [SerializeField] private float loggingInterval = 10f;
#endif

        #endregion

        #region Events

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>Fired when allocated managed memory jumps by more than the spike threshold.</summary>
        public event Action<float> OnMemorySpike;

        /// <summary>Fired when a GC collection is detected on the given generation.</summary>
        public event Action<int> OnGCCollected;
#endif

        #endregion

        #region Private State

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private MemorySnapshot _latest;
        private float _pollTimer;
        private float _logTimer;

        // GC tracking
        private int _lastGcGen0;
        private int _lastGcGen1;
        private int _lastGcGen2;

        // Spike detection
        private float _prevAllocatedMB;
#endif

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _lastGcGen0 = GC.CollectionCount(0);
            _lastGcGen1 = GC.CollectionCount(1);
            _lastGcGen2 = GC.CollectionCount(2);
            _prevAllocatedMB = Profiler.GetMonoUsedSizeLong() / (1024f * 1024f);
#endif
        }

        private void Update()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _pollTimer += Time.unscaledDeltaTime;
            if (_pollTimer >= pollInterval)
            {
                _pollTimer = 0f;
                Poll();
            }

            if (enablePeriodicLogging)
            {
                _logTimer += Time.unscaledDeltaTime;
                if (_logTimer >= loggingInterval)
                {
                    _logTimer = 0f;
                    LogSnapshot();
                }
            }
#endif
        }

        #endregion

        #region Public API

        /// <summary>Returns the most recently captured <see cref="MemorySnapshot"/>.</summary>
        public MemorySnapshot GetCurrentSnapshot() =>
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _latest;
#else
            default;
#endif

        #endregion

        #region Private Helpers

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void Poll()
        {
            float allocMB    = Profiler.GetMonoUsedSizeLong()       / (1024f * 1024f);
            float reservedMB = Profiler.GetMonoHeapSizeLong()       / (1024f * 1024f);
            float totalResMB = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
            float totalUseMB = Profiler.GetTotalAllocatedMemoryLong()/ (1024f * 1024f);

            _latest = new MemorySnapshot
            {
                allocatedManagedMB = allocMB,
                reservedManagedMB  = reservedMB,
                totalReservedMB    = totalResMB,
                totalUsedMB        = totalUseMB,
                gpuEstimateMB      = 0f,   // platform-dependent; left as 0 by default
                timestamp          = DateTime.UtcNow
            };

            // Spike detection
            float delta = allocMB - _prevAllocatedMB;
            if (delta > spikeThresholdMB)
                OnMemorySpike?.Invoke(allocMB);
            _prevAllocatedMB = allocMB;

            // GC collection detection
            int g0 = GC.CollectionCount(0);
            int g1 = GC.CollectionCount(1);
            int g2 = GC.CollectionCount(2);

            if (g0 != _lastGcGen0) { OnGCCollected?.Invoke(0); _lastGcGen0 = g0; }
            if (g1 != _lastGcGen1) { OnGCCollected?.Invoke(1); _lastGcGen1 = g1; }
            if (g2 != _lastGcGen2) { OnGCCollected?.Invoke(2); _lastGcGen2 = g2; }
        }

        private void LogSnapshot()
        {
            Debug.Log($"[MemoryProfiler] Alloc={_latest.allocatedManagedMB:F1} MB  " +
                      $"Reserved={_latest.reservedManagedMB:F1} MB  " +
                      $"TotalUsed={_latest.totalUsedMB:F1} MB");
        }
#endif

        #endregion
    }
}
