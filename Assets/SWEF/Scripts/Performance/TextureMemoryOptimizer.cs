using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace SWEF.Performance
{
    /// <summary>
    /// Monitors and optimises runtime texture memory usage.
    /// On Start it scans all loaded <see cref="Texture2D"/> objects and can
    /// optionally trigger automatic optimisation when memory exceeds a threshold.
    /// </summary>
    public class TextureMemoryOptimizer : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Auto-Optimize")]
        [Tooltip("Trigger optimisation automatically when texture memory exceeds this value.")]
        [SerializeField] private long autoOptimizeThresholdMB = 512;

        [Tooltip("Max resolution to clamp textures to during runtime optimisation.")]
        [SerializeField] private int defaultMaxResolution = 1024;

        [Tooltip("Enable automatic optimisation when the threshold is exceeded.")]
        [SerializeField] private bool autoOptimize = true;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired after <see cref="OptimizeTextures"/> completes.
        /// Arguments: memory before optimisation (MB), memory after (MB).
        /// </summary>
        public event Action<long, long> OnOptimizationComplete;

        // ── State ────────────────────────────────────────────────────────────────
        private List<TextureStats> _cachedStats = new List<TextureStats>();
        private float _autoCheckTimer;
        private const float AutoCheckInterval = 10f; // check every 10 seconds

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            RefreshTextureStats();
        }

        private void Update()
        {
            if (!autoOptimize) return;

            _autoCheckTimer += Time.unscaledDeltaTime;
            if (_autoCheckTimer < AutoCheckInterval) return;
            _autoCheckTimer = 0f;

            long totalMB = GetTotalTextureMemoryMB();
            if (totalMB > autoOptimizeThresholdMB)
            {
                Debug.Log($"[SWEF] TextureMemoryOptimizer: threshold exceeded ({totalMB} MB > {autoOptimizeThresholdMB} MB). Auto-optimizing.");
                OptimizeTextures(defaultMaxResolution);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>
        /// Returns all currently loaded textures sorted by memory size, largest first.
        /// </summary>
        public List<TextureStats> GetLoadedTextures()
        {
            RefreshTextureStats();
            return _cachedStats;
        }

        /// <summary>
        /// Returns the total memory used by all currently loaded <see cref="Texture2D"/>
        /// objects, in megabytes.
        /// </summary>
        public long GetTotalTextureMemoryMB()
        {
            long total = 0;
            Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            foreach (Texture2D t in textures)
                total += Profiler.GetRuntimeMemorySizeLong(t);
            return total / (1024 * 1024);
        }

        /// <summary>
        /// Downsamples any <see cref="Texture2D"/> whose dimensions exceed
        /// <paramref name="maxResolution"/> using bilinear filtering.
        /// This is a non-destructive runtime operation and does not modify assets on disk.
        /// </summary>
        public void OptimizeTextures(int maxResolution)
        {
            long beforeMB = GetTotalTextureMemoryMB();

            Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            foreach (Texture2D tex in textures)
            {
                if (!tex.isReadable) continue;
                if (tex.width <= maxResolution && tex.height <= maxResolution) continue;

                int newW = Mathf.Min(tex.width,  maxResolution);
                int newH = Mathf.Min(tex.height, maxResolution);

                TextureScale.Bilinear(tex, newW, newH);
                tex.Apply(updateMipmaps: true);
            }

            long afterMB = GetTotalTextureMemoryMB();
            Debug.Log($"[SWEF] TextureMemoryOptimizer: optimised textures {beforeMB} MB → {afterMB} MB");
            OnOptimizationComplete?.Invoke(beforeMB, afterMB);
        }

        /// <summary>
        /// Calls <see cref="Resources.UnloadUnusedAssets"/> followed by a managed GC
        /// to free unreferenced textures and other assets.
        /// </summary>
        public void UnloadUnusedTextures()
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
            Debug.Log("[SWEF] TextureMemoryOptimizer: unloaded unused textures.");
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void RefreshTextureStats()
        {
            _cachedStats.Clear();

            Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            foreach (Texture2D tex in textures)
            {
                long bytes = Profiler.GetRuntimeMemorySizeLong(tex);
                _cachedStats.Add(new TextureStats
                {
                    name            = tex.name,
                    width           = tex.width,
                    height          = tex.height,
                    format          = tex.format,
                    memorySizeBytes = bytes,
                    isMipMapped     = tex.mipmapCount > 1,
                    isReadable      = tex.isReadable,
                });
            }

            // Sort largest first
            _cachedStats.Sort((a, b) => b.memorySizeBytes.CompareTo(a.memorySizeBytes));
        }
    }

    // ── Data types ────────────────────────────────────────────────────────────

    /// <summary>Runtime statistics for a single loaded texture.</summary>
    [Serializable]
    public struct TextureStats
    {
        public string        name;
        public int           width;
        public int           height;
        public TextureFormat format;
        public long          memorySizeBytes;
        public bool          isMipMapped;
        public bool          isReadable;
    }

    // ── Bilinear texture scaling helper ──────────────────────────────────────────
    // Minimal self-contained implementation so we have no external dependencies.
    internal static class TextureScale
    {
        /// <summary>
        /// Scales <paramref name="tex"/> in place using bilinear filtering.
        /// Requires the texture to be readable.
        /// </summary>
        internal static void Bilinear(Texture2D tex, int newWidth, int newHeight)
        {
            Color[] srcPixels = tex.GetPixels();
            int     srcW      = tex.width;
            int     srcH      = tex.height;

            Color[] dstPixels = new Color[newWidth * newHeight];

            float scaleX = newWidth  > 1 ? (float)(srcW - 1) / (newWidth  - 1) : 0f;
            float scaleY = newHeight > 1 ? (float)(srcH - 1) / (newHeight - 1) : 0f;

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    float srcX = x * scaleX;
                    float srcY = y * scaleY;
                    int   x0   = Mathf.FloorToInt(srcX);
                    int   y0   = Mathf.FloorToInt(srcY);
                    int   x1   = Mathf.Min(x0 + 1, srcW - 1);
                    int   y1   = Mathf.Min(y0 + 1, srcH - 1);
                    float tx   = srcX - x0;
                    float ty   = srcY - y0;

                    Color c00 = srcPixels[y0 * srcW + x0];
                    Color c10 = srcPixels[y0 * srcW + x1];
                    Color c01 = srcPixels[y1 * srcW + x0];
                    Color c11 = srcPixels[y1 * srcW + x1];

                    dstPixels[y * newWidth + x] =
                        Color.Lerp(Color.Lerp(c00, c10, tx), Color.Lerp(c01, c11, tx), ty);
                }
            }

            tex.Reinitialize(newWidth, newHeight);
            tex.SetPixels(dstPixels);
        }
    }
}
