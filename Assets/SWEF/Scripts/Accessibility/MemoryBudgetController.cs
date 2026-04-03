// MemoryBudgetController.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Accessibility
{
    /// <summary>
    /// MonoBehaviour that monitors managed-heap memory, enforces configurable budgets,
    /// and triggers cache-cleanup actions when budgets are exceeded.
    ///
    /// <para>Checks memory every <see cref="pollIntervalSeconds"/> seconds.
    /// When usage exceeds <see cref="warningThresholdMB"/> a warning is logged.
    /// When usage exceeds <see cref="criticalThresholdMB"/> caches are cleaned,
    /// resources unloaded, and the GC is collected.</para>
    /// </summary>
    public class MemoryBudgetController : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static MemoryBudgetController Instance { get; private set; }

        // ── Serialised fields ─────────────────────────────────────────────────────
        [Header("Budgets (MB)")]
        [SerializeField, Tooltip("Managed memory (MB) at which a warning is logged.")]
        private float warningThresholdMB = 512f;

        [SerializeField, Tooltip("Managed memory (MB) at which a critical cleanup is triggered.")]
        private float criticalThresholdMB = 768f;

        [Header("Polling")]
        [SerializeField, Tooltip("Seconds between memory-budget checks.")]
        private float pollIntervalSeconds = 5f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when managed memory crosses the warning threshold (rising edge).</summary>
        public event Action<float> OnMemoryWarning;

        /// <summary>Fired when a critical cleanup is triggered.</summary>
        public event Action<float> OnMemoryCritical;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private bool _warningActive;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()  { StartCoroutine(PollLoop()); }
        private void OnDisable() { StopAllCoroutines(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Current managed heap usage in megabytes.</summary>
        public float CurrentUsageMB =>
            UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

        /// <summary>Forces an immediate cache-cleanup pass regardless of current usage.</summary>
        public void ForceCleanup()
        {
            PerformCleanup(CurrentUsageMB);
        }

        // ── Poll loop ─────────────────────────────────────────────────────────────

        private IEnumerator PollLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(pollIntervalSeconds);

                float usageMB = CurrentUsageMB;

                if (usageMB >= criticalThresholdMB)
                {
                    Debug.LogWarning($"[SWEF] Accessibility: Memory critical — {usageMB:F0} MB. Running cleanup.");
                    PerformCleanup(usageMB);
                    OnMemoryCritical?.Invoke(usageMB);
                    _warningActive = false;
                }
                else if (usageMB >= warningThresholdMB && !_warningActive)
                {
                    Debug.LogWarning($"[SWEF] Accessibility: Memory warning — {usageMB:F0} MB.");
                    OnMemoryWarning?.Invoke(usageMB);
                    _warningActive = true;
                }
                else if (usageMB < warningThresholdMB)
                {
                    _warningActive = false;
                }
            }
        }

        // ── Cleanup ───────────────────────────────────────────────────────────────

        private void PerformCleanup(float usageMB)
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
            Debug.Log($"[SWEF] Accessibility: Memory cleanup complete. Was: {usageMB:F0} MB.");
        }
    }
}
