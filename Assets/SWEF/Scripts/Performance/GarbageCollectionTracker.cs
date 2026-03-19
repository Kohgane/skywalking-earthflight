using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Performance
{
    /// <summary>
    /// Tracks managed-heap allocations per frame to surface GC hotspots.
    /// Uses <see cref="GC.GetTotalMemory"/> differentials and maintains a
    /// 300-frame circular buffer of <see cref="GCAllocationFrame"/> records.
    /// </summary>
    public class GarbageCollectionTracker : MonoBehaviour
    {
        // ── Constants ────────────────────────────────────────────────────────────
        private const int   CircularBufferSize     = 300;
        private const long  SpikeThresholdBytes    = 10 * 1024;      // 10 KB
        private const long  HighAllocWarningBytesPerFrame = 50 * 1024L; // 50 KB avg

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired when a single frame allocates more than 10 KB of managed memory.
        /// The argument is the number of bytes allocated that frame.
        /// </summary>
        public event Action<long> OnAllocationSpike;

        // ── Circular buffer ──────────────────────────────────────────────────────
        private readonly GCAllocationFrame[] _buffer = new GCAllocationFrame[CircularBufferSize];
        private int  _head;      // next write index
        private int  _count;     // how many entries are valid

        private long _prevMemory;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _prevMemory = GC.GetTotalMemory(false);
        }

        private void Update()
        {
            long current   = GC.GetTotalMemory(false);
            long allocated = Math.Max(0L, current - _prevMemory);
            _prevMemory    = current;

            var frame = new GCAllocationFrame
            {
                frameNumber    = Time.frameCount,
                allocatedBytes = allocated,
                timestamp      = Time.realtimeSinceStartup,
            };

            _buffer[_head] = frame;
            _head          = (_head + 1) % CircularBufferSize;
            if (_count < CircularBufferSize) _count++;

            if (allocated > SpikeThresholdBytes)
                OnAllocationSpike?.Invoke(allocated);

            // Warn when rolling average is too high
            float avg = GetAverageAllocPerFrame();
            if (avg > HighAllocWarningBytesPerFrame)
                Debug.LogWarning($"[SWEF] GarbageCollectionTracker: high allocation rate — {avg / 1024f:F1} KB/frame avg");
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>Average bytes allocated per frame across the circular buffer.</summary>
        public float GetAverageAllocPerFrame()
        {
            if (_count == 0) return 0f;
            long sum = 0;
            for (int i = 0; i < _count; i++)
                sum += _buffer[i].allocatedBytes;
            return (float)sum / _count;
        }

        /// <summary>Maximum single-frame allocation observed in the buffer.</summary>
        public float GetPeakAllocPerFrame()
        {
            long peak = 0;
            for (int i = 0; i < _count; i++)
                if (_buffer[i].allocatedBytes > peak)
                    peak = _buffer[i].allocatedBytes;
            return peak;
        }

        /// <summary>
        /// Returns <c>true</c> if the most recent frame allocated more than
        /// <paramref name="thresholdBytes"/> bytes.
        /// </summary>
        public bool IsAllocating(long thresholdBytes = 1024)
        {
            if (_count == 0) return false;
            int lastIndex = (_head - 1 + CircularBufferSize) % CircularBufferSize;
            return _buffer[lastIndex].allocatedBytes > thresholdBytes;
        }

        /// <summary>
        /// Returns all frames in the buffer where the allocation exceeded
        /// <paramref name="thresholdBytes"/>.
        /// </summary>
        public List<GCAllocationFrame> GetAllocationSpikes(long thresholdBytes)
        {
            var spikes = new List<GCAllocationFrame>();
            for (int i = 0; i < _count; i++)
                if (_buffer[i].allocatedBytes > thresholdBytes)
                    spikes.Add(_buffer[i]);
            return spikes;
        }

        /// <summary>
        /// Forces a full GC collection and logs heap size before and after.
        /// </summary>
        public void ForceCollect()
        {
            long before = GC.GetTotalMemory(false);
            GC.Collect();
            long after = GC.GetTotalMemory(true);
            Debug.Log($"[SWEF] GarbageCollectionTracker: ForceCollect — before={before / 1024}KB, after={after / 1024}KB, freed={(before - after) / 1024}KB");
        }
    }

    // ── Data types ────────────────────────────────────────────────────────────

    /// <summary>Per-frame GC allocation record.</summary>
    [Serializable]
    public struct GCAllocationFrame
    {
        /// <summary>Unity frame number.</summary>
        public int   frameNumber;

        /// <summary>Managed bytes allocated during this frame.</summary>
        public long  allocatedBytes;

        /// <summary><see cref="Time.realtimeSinceStartup"/> at the time of recording.</summary>
        public float timestamp;
    }
}
