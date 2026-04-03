// PerformanceMonitor.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Accessibility
{
    /// <summary>
    /// Singleton MonoBehaviour that tracks FPS, frame time, and memory usage,
    /// exposes the current <see cref="PerformanceMetrics"/> snapshot, and optionally
    /// renders a lightweight on-screen debug overlay.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static PerformanceMonitor Instance { get; private set; }

        // ── Serialised fields ─────────────────────────────────────────────────────
        [Header("Sampling")]
        [SerializeField, Tooltip("Number of frames to average FPS over.")]
        private int fpsSampleCount = 60;

        [SerializeField, Tooltip("Seconds between metric broadcasts.")]
        private float broadcastInterval = 0.5f;

        [Header("Debug Overlay")]
        [SerializeField, Tooltip("Show FPS/memory debug overlay at runtime.")]
        private bool showOverlay;

        [SerializeField] private Text overlayText;

        // ── Runtime state ─────────────────────────────────────────────────────────
        // ── FPS sample buffer ─────────────────────────────────────────────────────
        // The buffer is allocated at the maximum supported count (120 samples).
        // fpsSampleCount controls how many of those samples are averaged;
        // it is clamped to [1, 120] at snapshot time.
        private readonly float[] _fpsSamples = new float[120]; // max buffer; actual count = fpsSampleCount
        private int   _sampleIndex;
        private float _broadcastTimer;

        /// <summary>Last collected performance snapshot.</summary>
        public PerformanceMetrics Current { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // Rolling FPS sample
            float dt = Time.unscaledDeltaTime;
            _fpsSamples[_sampleIndex % _fpsSamples.Length] = dt > 0f ? 1f / dt : 0f;
            _sampleIndex++;

            _broadcastTimer += dt;
            if (_broadcastTimer >= broadcastInterval)
            {
                _broadcastTimer = 0f;
                Snapshot();
            }
        }

        // ── Snapshot ──────────────────────────────────────────────────────────────

        private void Snapshot()
        {
            // Clamp sample count to valid range
            int count = Mathf.Clamp(Mathf.Min(_sampleIndex, fpsSampleCount), 1, _fpsSamples.Length);
            float sum = 0f;
            for (int i = 0; i < count; i++)
                sum += _fpsSamples[(_sampleIndex - 1 - i + _fpsSamples.Length) % _fpsSamples.Length];

            float avgFps = count > 0 ? sum / count : 0f;
            float frameTimeMs = avgFps > 0f ? 1000f / avgFps : 0f;
            float memMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            float gpuMB = SystemInfo.graphicsMemorySize; // static, not per-frame — best available without URP-specific APIs

            Current = new PerformanceMetrics
            {
                fps               = avgFps,
                frameTime         = frameTimeMs,
                memoryUsageMB     = memMB,
                gpuMemoryMB       = gpuMB,
                drawCalls         = 0,   // requires Unity Profiler API — filled by future extension
                triangleCount     = 0,
                tileCacheHitRate  = 0f,
                activeParticleCount = 0
            };

            if (showOverlay && overlayText != null)
                overlayText.text = Current.ToString();
        }

        // ── Overlay toggle ────────────────────────────────────────────────────────

        /// <summary>Toggles the FPS/memory debug overlay on or off.</summary>
        public void SetOverlayVisible(bool visible)
        {
            showOverlay = visible;
            if (overlayText != null)
                overlayText.enabled = visible;
        }
    }
}
