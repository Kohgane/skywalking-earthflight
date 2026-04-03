// PerformanceMetrics.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System;

namespace SWEF.Accessibility
{
    /// <summary>
    /// Lightweight struct capturing a single frame's performance snapshot.
    /// Populated by <see cref="PerformanceMonitor"/> and consumed by <see cref="DynamicQualityScaler"/>.
    /// </summary>
    [Serializable]
    public struct PerformanceMetrics
    {
        /// <summary>Smoothed frames per second.</summary>
        public float fps;

        /// <summary>Raw frame delta time in milliseconds.</summary>
        public float frameTime;

        /// <summary>Total managed-heap memory in use (MB).</summary>
        public float memoryUsageMB;

        /// <summary>GPU memory currently allocated (MB). Platform-dependent; 0 if unavailable.</summary>
        public float gpuMemoryMB;

        /// <summary>Rendered draw calls this frame.</summary>
        public int drawCalls;

        /// <summary>Total triangle count rendered this frame.</summary>
        public int triangleCount;

        /// <summary>Cesium 3D tile cache hit rate (0–1).</summary>
        public float tileCacheHitRate;

        /// <summary>Number of currently active particle-system particles.</summary>
        public int activeParticleCount;

        /// <inheritdoc/>
        public override string ToString() =>
            $"FPS:{fps:F1} Frame:{frameTime:F1}ms Mem:{memoryUsageMB:F0}MB " +
            $"DC:{drawCalls} Tris:{triangleCount} Particles:{activeParticleCount}";
    }
}
