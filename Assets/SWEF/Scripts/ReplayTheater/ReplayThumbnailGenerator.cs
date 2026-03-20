using System;
using System.IO;
using UnityEngine;
using SWEF.Replay;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Generates and caches preview thumbnails for saved replays.
    /// Renders the scene from <see cref="captureCamera"/> at the 25% timestamp
    /// of the replay and saves the result as a PNG in the configured cache directory.
    /// </summary>
    public class ReplayThumbnailGenerator : MonoBehaviour
    {
        #region Inspector

        [Header("Settings")]
        [SerializeField] private ReplayTheaterSettings settings;

        [Header("Capture")]
        [SerializeField] private Camera captureCamera;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (settings == null)
                settings = Resources.Load<ReplayTheaterSettings>("ReplayTheaterSettings");
            if (captureCamera == null)
                captureCamera = Camera.main;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Generates a thumbnail for <paramref name="data"/> at the 25% replay position.
        /// The result is saved to the cache directory and returned as a <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="data">The replay to thumbnail.</param>
        /// <param name="width">Thumbnail width in pixels (0 = use settings default).</param>
        /// <param name="height">Thumbnail height in pixels (0 = use settings default).</param>
        /// <returns>
        /// A <see cref="Texture2D"/> containing the thumbnail, or <c>null</c> on failure.
        /// The caller is responsible for managing the lifetime of the returned texture.
        /// </returns>
        public Texture2D GenerateThumbnail(ReplayData data, int width = 0, int height = 0)
        {
            if (data == null)
            {
                Debug.LogWarning("[SWEF] ReplayThumbnailGenerator: Null replay data.");
                return null;
            }

            if (width  <= 0) width  = settings?.ThumbnailWidth  ?? 512;
            if (height <= 0) height = settings?.ThumbnailHeight ?? 288;

            var cam = captureCamera != null ? captureCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[SWEF] ReplayThumbnailGenerator: No camera available.");
                return null;
            }

            try
            {
                var rt  = new RenderTexture(width, height, 24);
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                RenderTexture.active = null;
                Destroy(rt);

                // Save to cache
                string cachePath = GetCachePath(data.replayId);
                EnsureDirectory(Path.GetDirectoryName(cachePath));
                File.WriteAllBytes(cachePath, tex.EncodeToPNG());

                // Store thumbnail bytes in replay data
                data.thumbnailPng = tex.EncodeToPNG();

                Debug.Log($"[SWEF] ReplayThumbnailGenerator: Thumbnail saved → {cachePath}");
                return tex;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] ReplayThumbnailGenerator: Failed — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a cached thumbnail for the given replay ID.
        /// Returns <c>null</c> if no cached file exists.
        /// </summary>
        /// <param name="replayId">Unique replay identifier.</param>
        /// <returns>Loaded <see cref="Texture2D"/> or <c>null</c>.</returns>
        public Texture2D LoadCachedThumbnail(string replayId)
        {
            string path = GetCachePath(replayId);
            if (!File.Exists(path)) return null;

            try
            {
                byte[]    bytes = File.ReadAllBytes(path);
                Texture2D tex   = new Texture2D(2, 2);
                if (tex.LoadImage(bytes)) return tex;

                Destroy(tex);
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] ReplayThumbnailGenerator: Could not load cached thumbnail — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a thumbnail from the PNG bytes embedded in <paramref name="data"/>.
        /// Returns <c>null</c> if no bytes are present.
        /// </summary>
        /// <param name="data">Replay data with embedded thumbnail bytes.</param>
        /// <returns>Loaded <see cref="Texture2D"/> or <c>null</c>.</returns>
        public static Texture2D LoadFromReplayData(ReplayData data)
        {
            if (data?.thumbnailPng == null || data.thumbnailPng.Length == 0) return null;

            var tex = new Texture2D(2, 2);
            if (tex.LoadImage(data.thumbnailPng)) return tex;

            UnityEngine.Object.Destroy(tex);
            return null;
        }

        /// <summary>Clears all cached thumbnail files from the cache directory.</summary>
        public void ClearCache()
        {
            string dir = settings?.ThumbnailCachePath
                         ?? Path.Combine(Application.persistentDataPath, "SWEF_ThumbnailCache");
            if (!Directory.Exists(dir)) return;

            foreach (string file in Directory.GetFiles(dir, "*.png"))
                File.Delete(file);

            Debug.Log("[SWEF] ReplayThumbnailGenerator: Thumbnail cache cleared.");
        }

        #endregion

        #region Internals

        private string GetCachePath(string replayId)
        {
            string dir = settings?.ThumbnailCachePath
                         ?? Path.Combine(Application.persistentDataPath, "SWEF_ThumbnailCache");
            string safeId = string.IsNullOrEmpty(replayId) ? "unknown" : replayId;
            return Path.Combine(dir, $"{safeId}.png");
        }

        private static void EnsureDirectory(string dir)
        {
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        #endregion
    }
}
