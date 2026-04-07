using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Core
{
    /// <summary>
    /// Singleton memory monitor. Survives scene loads via DontDestroyOnLoad.
    /// Periodically samples total allocated memory and triggers GC / cache
    /// clears when configurable thresholds are exceeded.
    /// </summary>
    public class MemoryManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static MemoryManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Monitoring")]
        [SerializeField] private float checkIntervalSec = 5f;

        [Header("Thresholds (MB)")]
        [SerializeField] private float memoryWarningThresholdMB  = 1024f;
        [SerializeField] private float memoryCriticalThresholdMB = 1536f;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired when allocated memory exceeds the warning threshold or 80% of system RAM.
        /// Argument is current MB usage (as long).
        /// </summary>
        public event Action<long> OnMemoryWarning;

        /// <summary>Fired when allocated memory exceeds the critical threshold. Argument is current MB usage.</summary>
        public event Action<float> OnMemoryCritical;

        // ── Public state ─────────────────────────────────────────────────────────
        /// <summary>Most recently sampled total allocated memory in megabytes.</summary>
        public float CurrentMemoryMB { get; private set; }

        /// <summary>Current used memory in MB (alias for Phase 26 interoperability).</summary>
        public long CurrentUsedMB => (long)CurrentMemoryMB;

        /// <summary>Peak allocated memory in MB recorded since session start.</summary>
        public long PeakUsedMB { get; private set; }

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
        }

        private void Start()
        {
            StartCoroutine(MonitorRoutine());
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private IEnumerator MonitorRoutine()
        {
            var interval = new WaitForSecondsRealtime(checkIntervalSec);
            while (true)
            {
                yield return interval;
                CheckMemory();
            }
        }

        private void CheckMemory()
        {
            long bytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            float mb   = bytes / (1024f * 1024f);
            CurrentMemoryMB = mb;

            if ((long)mb > PeakUsedMB)
                PeakUsedMB = (long)mb;

            // Phase 26 — fire OnMemoryWarning at 80% of system RAM
            int systemMB = SystemInfo.systemMemorySize;
            if (systemMB > 0 && mb >= systemMB * 0.8f)
                OnMemoryWarning?.Invoke((long)mb);

            if (mb >= memoryCriticalThresholdMB)
            {
                Debug.Log($"[SWEF] Memory critical: {mb:F1}MB");
                Resources.UnloadUnusedAssets();
                GC.Collect();
                Caching.ClearCache();
                OnMemoryCritical?.Invoke(mb);
            }
            else if (mb >= memoryWarningThresholdMB)
            {
                Debug.Log($"[SWEF] Memory warning: {mb:F1}MB");
                Resources.UnloadUnusedAssets();
                GC.Collect();
                // Note: the threshold-based OnMemoryWarning (Phase 26) fires independently at 80 % system RAM (see above).
            }
        }
    }
}
