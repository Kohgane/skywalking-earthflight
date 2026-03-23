// DebugOverlayData.cs — SWEF Performance Profiler & Debug Overlay
using System;
using UnityEngine;

namespace SWEF.DebugOverlay
{
    #region Enumerations

    /// <summary>Controls how much information is shown in the debug overlay.</summary>
    public enum OverlayDisplayMode
    {
        /// <summary>Only FPS counter.</summary>
        Minimal,
        /// <summary>FPS + memory summary.</summary>
        Standard,
        /// <summary>FPS + memory + draw calls + frame time graph.</summary>
        Detailed,
        /// <summary>All stats plus console and extra developer tools.</summary>
        Developer
    }

    /// <summary>Performance health state used for colour-coded display.</summary>
    public enum PerformanceThreshold
    {
        /// <summary>Performance is within acceptable bounds (green).</summary>
        Good,
        /// <summary>Performance is approaching limits (yellow).</summary>
        Warning,
        /// <summary>Performance is unacceptably low (red).</summary>
        Critical
    }

    /// <summary>Screen corner anchor for the debug overlay panel.</summary>
    public enum OverlayPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    #endregion

    #region Configuration Structs

    /// <summary>Display configuration for the FPS counter widget.</summary>
    [System.Serializable]
    public struct FPSDisplayConfig
    {
        /// <summary>How often (in seconds) the displayed value refreshes.</summary>
        [Tooltip("How often the displayed FPS value is refreshed (seconds).")]
        public float updateInterval;

        /// <summary>FPS below this value triggers a Warning threshold colour.</summary>
        [Tooltip("FPS below this value shows Warning colour.")]
        public int warningThreshold;

        /// <summary>FPS below this value triggers a Critical threshold colour.</summary>
        [Tooltip("FPS below this value shows Critical colour.")]
        public int criticalThreshold;

        /// <summary>Whether to render the rolling FPS graph.</summary>
        [Tooltip("Show the rolling FPS history graph.")]
        public bool showGraph;

        /// <summary>Number of samples retained for the graph.</summary>
        [Tooltip("Number of FPS samples stored for the graph.")]
        public int graphSampleCount;

        /// <summary>Returns a default <see cref="FPSDisplayConfig"/>.</summary>
        public static FPSDisplayConfig Default => new FPSDisplayConfig
        {
            updateInterval   = 0.5f,
            warningThreshold = 30,
            criticalThreshold = 20,
            showGraph        = true,
            graphSampleCount = 128
        };
    }

    /// <summary>Display configuration for the memory stats widget.</summary>
    [System.Serializable]
    public struct MemoryDisplayConfig
    {
        /// <summary>Show managed (mono) heap usage.</summary>
        [Tooltip("Show managed (Mono) heap size.")]
        public bool showManagedHeap;

        /// <summary>Show native (UnityEngine) memory usage.</summary>
        [Tooltip("Show native UnityEngine memory.")]
        public bool showNative;

        /// <summary>Show a rough GPU memory estimate.</summary>
        [Tooltip("Show GPU memory estimate.")]
        public bool showGpuEstimate;

        /// <summary>Allocated managed memory above this value (MB) triggers a Warning.</summary>
        [Tooltip("Warning threshold for allocated managed memory in MB.")]
        public float warningThresholdMB;

        /// <summary>How often memory stats are polled (seconds).</summary>
        [Tooltip("Memory stats poll interval in seconds.")]
        public float pollInterval;

        /// <summary>Returns a default <see cref="MemoryDisplayConfig"/>.</summary>
        public static MemoryDisplayConfig Default => new MemoryDisplayConfig
        {
            showManagedHeap   = true,
            showNative        = true,
            showGpuEstimate   = false,
            warningThresholdMB = 512f,
            pollInterval      = 1f
        };
    }

    /// <summary>Display configuration for the draw-call monitor widget.</summary>
    [System.Serializable]
    public struct DrawCallDisplayConfig
    {
        /// <summary>Show dynamic batch count.</summary>
        [Tooltip("Show batch count.")]
        public bool showBatches;

        /// <summary>Show total triangle count.</summary>
        [Tooltip("Show triangle count.")]
        public bool showTriangles;

        /// <summary>Show total vertex count.</summary>
        [Tooltip("Show vertex count.")]
        public bool showVertices;

        /// <summary>Draw-call count above this value triggers a Warning event.</summary>
        [Tooltip("Draw call count that triggers a Warning.")]
        public int warningThreshold;

        /// <summary>Returns a default <see cref="DrawCallDisplayConfig"/>.</summary>
        public static DrawCallDisplayConfig Default => new DrawCallDisplayConfig
        {
            showBatches      = true,
            showTriangles    = true,
            showVertices     = false,
            warningThreshold = 300
        };
    }

    #endregion

    #region Data Containers

    /// <summary>Snapshot of memory metrics captured at a single point in time.</summary>
    [System.Serializable]
    public struct MemorySnapshot
    {
        /// <summary>Currently allocated managed heap in MB.</summary>
        public float allocatedManagedMB;

        /// <summary>Total reserved managed heap in MB.</summary>
        public float reservedManagedMB;

        /// <summary>Total reserved native (UnityEngine) memory in MB.</summary>
        public float totalReservedMB;

        /// <summary>Total used native (UnityEngine) memory in MB.</summary>
        public float totalUsedMB;

        /// <summary>Rough GPU memory estimate in MB (platform-dependent).</summary>
        public float gpuEstimateMB;

        /// <summary>UTC timestamp when this snapshot was taken.</summary>
        public DateTime timestamp;
    }

    /// <summary>Snapshot of rendering statistics for a single frame.</summary>
    [System.Serializable]
    public struct RenderingStats
    {
        /// <summary>Draw call count this frame.</summary>
        public int drawCalls;

        /// <summary>Batched draw call count this frame.</summary>
        public int batches;

        /// <summary>Total triangle count rendered this frame.</summary>
        public int triangles;

        /// <summary>Total vertex count rendered this frame.</summary>
        public int vertices;

        /// <summary>SetPass call count this frame.</summary>
        public int setPassCalls;

        /// <summary>Shadow caster count this frame.</summary>
        public int shadowCasters;
    }

    /// <summary>
    /// Combined snapshot aggregating FPS, memory, and rendering data at one instant.
    /// Returned by <see cref="DebugOverlayController.GetFullSnapshot"/>.
    /// </summary>
    [System.Serializable]
    public struct DebugOverlaySnapshot
    {
        /// <summary>Current instantaneous FPS.</summary>
        public float currentFPS;

        /// <summary>Rolling average FPS.</summary>
        public float averageFPS;

        /// <summary>Minimum FPS recorded this session.</summary>
        public float minFPS;

        /// <summary>Maximum FPS recorded this session.</summary>
        public float maxFPS;

        /// <summary>Current frame time in milliseconds.</summary>
        public float frameTimeMs;

        /// <summary>Memory snapshot taken at the same instant.</summary>
        public MemorySnapshot memory;

        /// <summary>Rendering stats snapshot taken at the same instant.</summary>
        public RenderingStats rendering;

        /// <summary>Current performance health state.</summary>
        public PerformanceThreshold performanceState;

        /// <summary>UTC timestamp of this snapshot.</summary>
        public DateTime timestamp;
    }

    /// <summary>Session-level performance analytics summary.</summary>
    [System.Serializable]
    public struct PerformanceSummary
    {
        /// <summary>Average FPS across the whole session.</summary>
        public float sessionAverageFPS;

        /// <summary>Lowest FPS observed during the session.</summary>
        public float sessionLowestFPS;

        /// <summary>Peak allocated managed memory in MB.</summary>
        public float peakMemoryMB;

        /// <summary>Total GC collections triggered during the session.</summary>
        public int totalGCCollections;

        /// <summary>Total frames recorded.</summary>
        public long totalFrames;

        /// <summary>Session wall-clock duration in seconds.</summary>
        public float sessionDurationSeconds;
    }

    #endregion

    #region ScriptableObject Profile

    /// <summary>
    /// Designer-facing asset that combines all overlay configuration into one
    /// inspectable ScriptableObject.  Create via
    /// <c>Assets → Create → SWEF → DebugOverlay → Overlay Profile</c>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "SWEF/DebugOverlay/Overlay Profile",
        fileName = "DebugOverlayProfile")]
    public class DebugOverlayProfile : ScriptableObject
    {
        #region Inspector Fields

        [Header("Display")]
        [Tooltip("Default display mode when the overlay is first shown.")]
        public OverlayDisplayMode defaultDisplayMode = OverlayDisplayMode.Standard;

        [Tooltip("Screen corner where the overlay panel is anchored.")]
        public OverlayPosition overlayPosition = OverlayPosition.TopLeft;

        [Tooltip("Alpha value of the overlay background panel (0 = transparent, 1 = opaque).")]
        [Range(0f, 1f)]
        public float backgroundAlpha = 0.6f;

        [Tooltip("Font size used for all overlay labels.")]
        [Range(8, 24)]
        public int fontSize = 12;

        [Header("FPS Counter")]
        public FPSDisplayConfig fpsConfig = FPSDisplayConfig.Default;

        [Header("Memory Profiler")]
        public MemoryDisplayConfig memoryConfig = MemoryDisplayConfig.Default;

        [Header("Draw Call Monitor")]
        public DrawCallDisplayConfig drawCallConfig = DrawCallDisplayConfig.Default;

        #endregion
    }

    #endregion
}
