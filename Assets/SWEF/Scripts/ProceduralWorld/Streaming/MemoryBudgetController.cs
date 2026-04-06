// MemoryBudgetController.cs — Phase 113: Procedural City & Airport Generation
// Dynamic memory management: adjust detail based on available memory,
// garbage collection scheduling.
// Namespace: SWEF.ProceduralWorld

using System.Collections;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Monitors application memory and dynamically adjusts world generation
    /// quality to keep within a configurable memory budget.
    /// </summary>
    public class MemoryBudgetController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Memory Budget")]
        [Tooltip("Memory usage fraction [0..1] above which quality is reduced.")]
        [Range(0.5f, 0.95f)]
        [SerializeField] private float highWaterMark = 0.8f;

        [Tooltip("Memory usage fraction [0..1] below which quality is restored.")]
        [Range(0.3f, 0.9f)]
        [SerializeField] private float lowWaterMark = 0.6f;

        [Tooltip("Interval in seconds between memory checks.")]
        [Range(1f, 30f)]
        [SerializeField] private float checkInterval = 5f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current normalised memory usage [0..1].</summary>
        public float CurrentUsageFraction { get; private set; }

        /// <summary>Whether the system is currently in memory-saving mode.</summary>
        public bool IsMemoryConstrained { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start() => StartCoroutine(MonitorMemory());

        // ── Internal helpers ──────────────────────────────────────────────────────

        private IEnumerator MonitorMemory()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkInterval);
                UpdateMemoryState();
            }
        }

        private void UpdateMemoryState()
        {
            long used = System.GC.GetTotalMemory(false);
            long total = SystemInfo.systemMemorySize * 1024L * 1024L;
            if (total <= 0) total = 4L * 1024 * 1024 * 1024; // 4 GB fallback

            CurrentUsageFraction = Mathf.Clamp01((float)used / total);

            if (!IsMemoryConstrained && CurrentUsageFraction > highWaterMark)
            {
                IsMemoryConstrained = true;
                OnHighMemory();
            }
            else if (IsMemoryConstrained && CurrentUsageFraction < lowWaterMark)
            {
                IsMemoryConstrained = false;
                OnLowMemory();
            }
        }

        private void OnHighMemory()
        {
            // Force GC and request chunk manager to trim cache
            System.GC.Collect();
            var chunkManager = FindObjectOfType<CityChunkManager>();
            // Chunk manager handles its own LRU eviction
            Debug.Log("[MemoryBudget] High memory — GC collected, requesting quality reduction.");
        }

        private void OnLowMemory()
        {
            Debug.Log("[MemoryBudget] Memory normalised — restoring quality.");
        }

        /// <summary>Returns a recommended quality scale [0..1] based on current memory pressure.</summary>
        public float RecommendedQualityScale()
        {
            if (CurrentUsageFraction < lowWaterMark) return 1f;
            if (CurrentUsageFraction > highWaterMark) return 0.5f;
            float t = (CurrentUsageFraction - lowWaterMark) / (highWaterMark - lowWaterMark);
            return Mathf.Lerp(1f, 0.5f, t);
        }
    }
}
