#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SWEF.Performance;

namespace SWEF.Editor
{
    /// <summary>
    /// Custom EditorWindow: <b>SWEF → Performance Profiler</b>.
    /// Shows real-time FPS / memory / GC graphs, pool stats, texture memory
    /// breakdown, and draw-call analysis during Play mode.
    /// Repaints every 0.5 s to minimise editor overhead.
    /// </summary>
    public class PerformanceProfilerWindow : EditorWindow
    {
        // ── Scroll positions ──────────────────────────────────────────────────────
        private Vector2 _poolScroll;
        private Vector2 _textureScroll;

        // ── Graph data ───────────────────────────────────────────────────────────
        private const int GraphPoints = 60;
        private readonly float[] _fpsHistory    = new float[GraphPoints];
        private readonly float[] _memHistory    = new float[GraphPoints];
        private readonly float[] _gcHistory     = new float[GraphPoints];
        private int _histHead;

        private double _lastRepaintTime;
        private const double RepaintInterval = 0.5;

        // ── Editor menu entry ─────────────────────────────────────────────────────
        [MenuItem("SWEF/Performance Profiler")]
        public static void ShowWindow()
        {
            var window = GetWindow<PerformanceProfilerWindow>("Performance Profiler");
            window.minSize = new Vector2(500, 600);
        }

        // ── Unity editor lifecycle ────────────────────────────────────────────────
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastRepaintTime < RepaintInterval) return;
            _lastRepaintTime = now;

            if (!Application.isPlaying) return;

            SampleHistory();
            Repaint();
        }

        // ── GUI ───────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            EditorGUILayout.LabelField("SWEF — Performance Profiler", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to see live data.", MessageType.Info);
                return;
            }

            DrawActionButtons();
            EditorGUILayout.Space(4);

            DrawSnapshotSection();
            EditorGUILayout.Space(4);

            DrawGraphs();
            EditorGUILayout.Space(4);

            DrawPoolStats();
            EditorGUILayout.Space(4);

            DrawTextureBreakdown();
            EditorGUILayout.Space(4);

            DrawDrawCallSection();
        }

        // ── Sections ─────────────────────────────────────────────────────────────
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Export CSV Report", GUILayout.Height(24)))
                PerformanceProfiler.Instance?.ExportReport();

            if (GUILayout.Button("Force GC", GUILayout.Height(24)))
            {
                var gc = Object.FindFirstObjectByType<GarbageCollectionTracker>();
                gc?.ForceCollect();
            }

            if (GUILayout.Button("Optimize Textures", GUILayout.Height(24)))
            {
                var opt = Object.FindFirstObjectByType<TextureMemoryOptimizer>();
                opt?.OptimizeTextures(1024);
            }

            if (GUILayout.Button("Clear History", GUILayout.Height(24)))
                PerformanceProfiler.Instance?.ResetHistory();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSnapshotSection()
        {
            var profiler = PerformanceProfiler.Instance;
            if (profiler == null) { EditorGUILayout.LabelField("PerformanceProfiler not found in scene."); return; }

            var snap = profiler.GetCurrentSnapshot();
            EditorGUILayout.LabelField("Current Snapshot", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"Avg FPS:       {snap.avgFps:F1}");
            EditorGUILayout.LabelField($"1% Low:        {snap.onePercentLow:F1}");
            EditorGUILayout.LabelField($"0.1% Low:      {snap.pointOnePercentLow:F1}");
            EditorGUILayout.LabelField($"Avg Frame:     {snap.avgFrameTimeMs:F2} ms");
            EditorGUILayout.LabelField($"Max Frame:     {snap.maxFrameTimeMs:F2} ms");
            EditorGUILayout.LabelField($"GC Collects:   {snap.gcCollectCount}");
            EditorGUILayout.LabelField($"Allocated:     {snap.totalAllocatedMB} MB");
            EditorGUILayout.LabelField($"Heap Used:     {snap.usedHeapMB} MB");
            EditorGUILayout.LabelField($"History:       {profiler.History.Count} snapshots");
            EditorGUI.indentLevel--;
        }

        private void DrawGraphs()
        {
            EditorGUILayout.LabelField("FPS History (last 60 samples)", EditorStyles.boldLabel);
            DrawLineGraph(_fpsHistory, Color.green,  0f, 120f, 40f);

            EditorGUILayout.LabelField("Memory (MB)", EditorStyles.boldLabel);
            DrawLineGraph(_memHistory, Color.cyan,   0f, (float)SystemInfo.systemMemorySize, 40f);

            EditorGUILayout.LabelField("GC Alloc (KB/f)", EditorStyles.boldLabel);
            DrawLineGraph(_gcHistory, Color.yellow,  0f, 100f, 40f);
        }

        private void DrawLineGraph(float[] data, Color lineColor, float minV, float maxV, float height)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                GUILayout.Height(height), GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            if (Event.current.type != EventType.Repaint) return;

            Handles.color = lineColor;
            float w = rect.width / (GraphPoints - 1);
            float range = maxV - minV;
            if (range <= 0f) range = 1f;

            for (int i = 0; i < GraphPoints - 1; i++)
            {
                int idx0 = (_histHead + i)     % GraphPoints;
                int idx1 = (_histHead + i + 1) % GraphPoints;

                float t0 = Mathf.Clamp01((data[idx0] - minV) / range);
                float t1 = Mathf.Clamp01((data[idx1] - minV) / range);

                Vector3 p0 = new Vector3(rect.x + i * w,       rect.yMax - t0 * rect.height);
                Vector3 p1 = new Vector3(rect.x + (i + 1) * w, rect.yMax - t1 * rect.height);
                Handles.DrawLine(p0, p1);
            }
        }

        private void DrawPoolStats()
        {
            var pm = MemoryPoolManager.Instance;
            EditorGUILayout.LabelField("Object Pool Stats", EditorStyles.boldLabel);

            if (pm == null) { EditorGUI.indentLevel++; EditorGUILayout.LabelField("MemoryPoolManager not in scene."); EditorGUI.indentLevel--; return; }

            var stats = pm.GetPoolStats();
            if (stats.Count == 0) { EditorGUI.indentLevel++; EditorGUILayout.LabelField("No pools registered."); EditorGUI.indentLevel--; return; }

            _poolScroll = EditorGUILayout.BeginScrollView(_poolScroll, GUILayout.MaxHeight(100));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pool Name",  GUILayout.Width(160));
            EditorGUILayout.LabelField("Active",     GUILayout.Width(60));
            EditorGUILayout.LabelField("Pooled",     GUILayout.Width(60));
            EditorGUILayout.LabelField("Total",      GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            foreach (var kv in stats)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(kv.Key,                         GUILayout.Width(160));
                EditorGUILayout.LabelField(kv.Value.active.ToString(),     GUILayout.Width(60));
                EditorGUILayout.LabelField(kv.Value.pooled.ToString(),     GUILayout.Width(60));
                EditorGUILayout.LabelField(kv.Value.total.ToString(),      GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTextureBreakdown()
        {
            var opt = Object.FindFirstObjectByType<TextureMemoryOptimizer>();
            EditorGUILayout.LabelField("Texture Memory", EditorStyles.boldLabel);

            if (opt == null) { EditorGUI.indentLevel++; EditorGUILayout.LabelField("TextureMemoryOptimizer not in scene."); EditorGUI.indentLevel--; return; }

            long totalMB = opt.GetTotalTextureMemoryMB();
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"Total: {totalMB} MB");
            EditorGUI.indentLevel--;

            _textureScroll = EditorGUILayout.BeginScrollView(_textureScroll, GUILayout.MaxHeight(120));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name",       GUILayout.Width(180));
            EditorGUILayout.LabelField("Size",        GUILayout.Width(80));
            EditorGUILayout.LabelField("Format",      GUILayout.Width(100));
            EditorGUILayout.LabelField("Mem (KB)",    GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            var textures = opt.GetLoadedTextures();
            int shown = Mathf.Min(textures.Count, 20);
            for (int i = 0; i < shown; i++)
            {
                var t = textures[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(t.name,                        GUILayout.Width(180));
                EditorGUILayout.LabelField($"{t.width}×{t.height}",      GUILayout.Width(80));
                EditorGUILayout.LabelField(t.format.ToString(),           GUILayout.Width(100));
                EditorGUILayout.LabelField((t.memorySizeBytes / 1024).ToString(), GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDrawCallSection()
        {
            var analyzer = Object.FindFirstObjectByType<DrawCallAnalyzer>();
            EditorGUILayout.LabelField("Draw Call Analysis", EditorStyles.boldLabel);

            if (analyzer == null) { EditorGUI.indentLevel++; EditorGUILayout.LabelField("DrawCallAnalyzer not in scene."); EditorGUI.indentLevel--; return; }

            var stats = analyzer.GetCurrentStats();
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"Draw Calls:  {stats.drawCalls}");
            EditorGUILayout.LabelField($"Batches:     {stats.batches}");
            EditorGUILayout.LabelField($"Triangles:   {stats.triangles}");
            EditorGUILayout.LabelField($"Shadow:      {stats.shadowCasters}");
            EditorGUILayout.LabelField($"Efficiency:  {analyzer.GetBatchingEfficiency() * 100f:F1}%");
            EditorGUILayout.LabelField($"Renderers:   {analyzer.GetRendererCount()}");
            EditorGUI.indentLevel--;
        }

        // ── History sampling ──────────────────────────────────────────────────────
        private void SampleHistory()
        {
            var profiler  = PerformanceProfiler.Instance;
            var gcTracker = Object.FindFirstObjectByType<GarbageCollectionTracker>();

            float fps    = profiler != null ? profiler.GetCurrentSnapshot().avgFps : 0f;
            float memMB  = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            float gcKB   = gcTracker != null ? gcTracker.GetAverageAllocPerFrame() / 1024f : 0f;

            _fpsHistory[_histHead] = fps;
            _memHistory[_histHead] = memMB;
            _gcHistory[_histHead]  = gcKB;

            _histHead = (_histHead + 1) % GraphPoints;
        }
    }
}
#endif
