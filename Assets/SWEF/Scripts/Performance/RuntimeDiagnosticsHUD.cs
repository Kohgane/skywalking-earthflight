#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Performance
{
    /// <summary>
    /// Comprehensive on-screen diagnostics overlay for developer/QA builds.
    /// Shows FPS, frame-time graph, memory, GC allocation rate, draw calls,
    /// pool stats, and texture memory.
    /// Toggle with the configured <see cref="toggleKey"/> (default F3) or the on-screen button.
    /// Only compiled in Development builds and the Unity Editor.
    /// </summary>
    public class RuntimeDiagnosticsHUD : MonoBehaviour
    {
        private const string PrefKey = "SWEF_DiagnosticsEnabled";

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Canvas")]
        [SerializeField] private Canvas diagnosticsCanvas;

        [Header("Text fields")]
        [SerializeField] private Text fpsText;
        [SerializeField] private Text memoryText;
        [SerializeField] private Text drawCallText;
        [SerializeField] private Text gcText;
        [SerializeField] private Text poolStatsText;

        [Header("Graph")]
        [SerializeField] private RawImage frameTimeGraph;

        [Header("Memory bar")]
        [SerializeField] private Slider memoryBar;

        [Header("Buttons")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button exportButton;
        [SerializeField] private Button optimizeButton;

        [Header("Input")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;

        [Header("Update rate")]
        [SerializeField] private float updateIntervalSec = 0.5f;

        // ── Frame-time graph config ───────────────────────────────────────────────
        private const int GraphWidth        = 120;
        private const int GraphHeight       = 40;
        private const float MaxGraphFrameMs = 50f;

        // ── State ────────────────────────────────────────────────────────────────
        private bool       _visible;
        private float      _updateTimer;
        private Texture2D  _graphTex;

        // Rolling frame-time buffer for graph (newest at right)
        private readonly float[] _graphBuffer = new float[GraphWidth];
        private int _graphHead;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _graphTex = new Texture2D(GraphWidth, GraphHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
            };

            if (frameTimeGraph != null)
                frameTimeGraph.texture = _graphTex;

            if (toggleButton  != null) toggleButton.onClick.AddListener(ToggleVisibility);
            if (exportButton  != null) exportButton.onClick.AddListener(OnExportClicked);
            if (optimizeButton != null) optimizeButton.onClick.AddListener(OnOptimizeClicked);

            _visible = PlayerPrefs.GetInt(PrefKey, 0) == 1;
            ApplyVisibility();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                ToggleVisibility();

            // Record frame time for graph
            _graphBuffer[_graphHead] = Time.unscaledDeltaTime * 1000f;
            _graphHead = (_graphHead + 1) % GraphWidth;

            if (!_visible) return;

            _updateTimer += Time.unscaledDeltaTime;
            if (_updateTimer >= updateIntervalSec)
            {
                _updateTimer = 0f;
                RefreshHUD();
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>Shows or hides the diagnostics overlay.</summary>
        public void ToggleVisibility()
        {
            _visible = !_visible;
            PlayerPrefs.SetInt(PrefKey, _visible ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVisibility();
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void ApplyVisibility()
        {
            if (diagnosticsCanvas != null)
                diagnosticsCanvas.gameObject.SetActive(_visible);
        }

        private void RefreshHUD()
        {
            var profiler = PerformanceProfiler.Instance;
            var gcTracker = FindFirstObjectByType<GarbageCollectionTracker>();
            var drawAnalyzer = FindFirstObjectByType<DrawCallAnalyzer>();
            var poolManager = MemoryPoolManager.Instance;

            // FPS
            if (fpsText != null && profiler != null)
            {
                var snap = profiler.GetCurrentSnapshot();
                float fps = snap.avgFps;
                fpsText.color = fps >= 55f ? Color.green : fps >= 30f ? Color.yellow : Color.red;
                fpsText.text  = $"FPS: {fps:F0}  1%: {snap.onePercentLow:F0}  0.1%: {snap.pointOnePercentLow:F0}\nFT: {snap.avgFrameTimeMs:F1}ms  Max: {snap.maxFrameTimeMs:F1}ms";
            }

            // Memory
            long totalMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
            long systemMB = SystemInfo.systemMemorySize;
            if (memoryText != null)
                memoryText.text = $"Mem: {totalMB} MB / {systemMB} MB";
            if (memoryBar != null)
            {
                memoryBar.minValue = 0f;
                memoryBar.maxValue = 1f;
                memoryBar.value    = systemMB > 0 ? (float)totalMB / systemMB : 0f;
            }

            // Draw calls
            if (drawCallText != null && drawAnalyzer != null)
            {
                var stats = drawAnalyzer.GetCurrentStats();
                drawCallText.text = $"DrawCalls: {stats.drawCalls}  Batches: {stats.batches}  Tris: {stats.triangles}\nBatching: {drawAnalyzer.GetBatchingEfficiency() * 100f:F0}%";
            }

            // GC
            if (gcText != null && gcTracker != null)
            {
                gcText.text = $"GC Avg: {gcTracker.GetAverageAllocPerFrame() / 1024f:F1} KB/f  Peak: {gcTracker.GetPeakAllocPerFrame() / 1024f:F1} KB";
            }

            // Pool stats
            if (poolStatsText != null && poolManager != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Pools:");
                foreach (var kv in poolManager.GetPoolStats())
                    sb.AppendLine($"  {kv.Key}: {kv.Value.active}↑ {kv.Value.pooled}○ / {kv.Value.total}");
                poolStatsText.text = sb.ToString();
            }

            // Frame-time graph
            RenderGraph();
        }

        private void RenderGraph()
        {
            if (_graphTex == null) return;

            // Clear to dark background
            Color bg = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            Color[] pixels = new Color[GraphWidth * GraphHeight];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

            // 30 FPS line: 33.3 ms → y = 33.3/50 * GraphHeight
            int line30 = Mathf.RoundToInt((33.3f / MaxGraphFrameMs) * GraphHeight);
            for (int x = 0; x < GraphWidth; x++)
            {
                int idx = Mathf.Clamp(line30, 0, GraphHeight - 1) * GraphWidth + x;
                pixels[idx] = new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            for (int x = 0; x < GraphWidth; x++)
            {
                int bufIdx = (_graphHead + x) % GraphWidth;
                float ms   = _graphBuffer[bufIdx];
                float t    = Mathf.Clamp01(ms / MaxGraphFrameMs);
                int   barH = Mathf.RoundToInt(t * GraphHeight);
                Color col  = ms <= 33.3f ? Color.green : ms <= 50f ? Color.yellow : Color.red;

                for (int y = 0; y < barH; y++)
                    pixels[y * GraphWidth + x] = col;
            }

            _graphTex.SetPixels(pixels);
            _graphTex.Apply(false);
        }

        private void OnExportClicked()
        {
            PerformanceProfiler.Instance?.ExportReport();
        }

        private void OnOptimizeClicked()
        {
            FindFirstObjectByType<TextureMemoryOptimizer>()?.OptimizeTextures(1024);
            MemoryPoolManager.Instance?.ShrinkAll();
        }

        private void OnDestroy()
        {
            if (_graphTex != null)
                Destroy(_graphTex);
        }
    }
}
#endif
