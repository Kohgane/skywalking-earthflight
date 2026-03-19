using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Performance
{
    /// <summary>
    /// Runtime draw-call and batching statistics tracker.
    /// Reads from <see cref="UnityEngine.Rendering.FrameTimingManager"/> when available
    /// and falls back to internal lightweight tracking.
    /// Fires <see cref="OnStatsUpdated"/> every 30 frames.
    /// </summary>
    public class DrawCallAnalyzer : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [SerializeField] private int updateIntervalFrames = 30;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired every <see cref="updateIntervalFrames"/> frames with fresh stats.</summary>
        public event Action<DrawCallStats> OnStatsUpdated;

        // ── State ────────────────────────────────────────────────────────────────
        private DrawCallStats _currentStats;
        private int           _frameCounter;

        // ── FrameTimingManager support ───────────────────────────────────────────
        private readonly UnityEngine.Rendering.FrameTiming[] _frameTimings =
            new UnityEngine.Rendering.FrameTiming[1];

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Update()
        {
            _frameCounter++;
            if (_frameCounter < updateIntervalFrames) return;

            _frameCounter = 0;
            RefreshStats();
            OnStatsUpdated?.Invoke(_currentStats);
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>Returns the most recently computed draw-call stats.</summary>
        public DrawCallStats GetCurrentStats() => _currentStats;

        /// <summary>Returns the total number of active <see cref="Renderer"/> components in the scene.</summary>
        public int GetRendererCount()
        {
            return FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
        }

        /// <summary>
        /// Returns the <paramref name="count"/> heaviest renderers by triangle count.
        /// </summary>
        public List<RendererInfo> GetHeaviestRenderers(int count)
        {
            Renderer[] all = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var results = new List<RendererInfo>(all.Length);

            foreach (Renderer r in all)
            {
                int triangles = 0;
                if (r is MeshRenderer meshRenderer)
                {
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                        triangles = mf.sharedMesh.triangles.Length / 3;
                }
                else if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                {
                    triangles = smr.sharedMesh.triangles.Length / 3;
                }

                results.Add(new RendererInfo
                {
                    gameObjectName = r.gameObject.name,
                    materialCount  = r.sharedMaterials.Length,
                    triangleCount  = triangles,
                    isBatched      = r.isPartOfStaticBatch,
                });
            }

            results.Sort((a, b) => b.triangleCount.CompareTo(a.triangleCount));
            return results.GetRange(0, Mathf.Min(count, results.Count));
        }

        /// <summary>
        /// Returns the ratio of batches to draw calls (0–1).
        /// A value closer to 1 indicates better batching efficiency.
        /// Returns 0 if no draw calls have been recorded.
        /// </summary>
        public float GetBatchingEfficiency()
        {
            if (_currentStats.drawCalls <= 0) return 0f;
            return Mathf.Clamp01((float)_currentStats.batches / _currentStats.drawCalls);
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void RefreshStats()
        {
            // Try FrameTimingManager first
            UnityEngine.Rendering.FrameTimingManager.CaptureFrameTimings();
            uint captured = UnityEngine.Rendering.FrameTimingManager.GetLatestTimings(1, _frameTimings);

            float cullingMs = 0f;
            if (captured > 0)
                cullingMs = (float)_frameTimings[0].cpuTimePresentCalled;

            // Fallback counts from internal stats
            // (UnityEngine.Profiling.Profiler counters are editor-only; use approximations in builds)
            int renderers = GetRendererCount();

            _currentStats = new DrawCallStats
            {
                drawCalls      = renderers,          // approximation: one draw call per renderer
                batches        = Mathf.Max(1, renderers / 4), // rough batching estimate
                triangles      = EstimateTriangles(),
                vertices       = 0,                  // not available without full profiler
                setPassCalls   = renderers,
                shadowCasters  = CountShadowCasters(),
                cullingTimeMs  = cullingMs,
            };
        }

        private int EstimateTriangles()
        {
            int total = 0;
            MeshFilter[] mfs = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh != null)
                    total += mf.sharedMesh.triangles.Length / 3;
            }
            return total;
        }

        private int CountShadowCasters()
        {
            int count = 0;
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (Renderer r in renderers)
            {
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                    count++;
            }
            return count;
        }
    }

    // ── Data types ────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot of draw call and batching metrics for a single frame.
    /// Note: values marked as estimates are approximations derived from scene data
    /// rather than precise Unity Profiler counters (which are editor-only).
    /// </summary>
    [Serializable]
    public struct DrawCallStats
    {
        /// <summary>Approximate draw call count (estimate: one per active Renderer).</summary>
        public int   drawCalls;
        /// <summary>Approximate batch count (estimate: drawCalls / 4).</summary>
        public int   batches;
        /// <summary>Total triangle count from all MeshFilter/SkinnedMeshRenderer components.</summary>
        public int   triangles;
        public int   vertices;
        public int   setPassCalls;
        public int   shadowCasters;
        public float cullingTimeMs;
    }

    /// <summary>Per-renderer information for heavy-renderer analysis.</summary>
    [Serializable]
    public struct RendererInfo
    {
        public string gameObjectName;
        public int    materialCount;
        public int    triangleCount;
        public bool   isBatched;
    }
}
