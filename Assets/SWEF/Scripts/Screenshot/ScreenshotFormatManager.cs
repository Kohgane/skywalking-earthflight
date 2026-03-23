using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Screenshot
{
    /// <summary>
    /// Advanced screenshot format and output management.
    /// Extends the basic ScreenshotController with super-resolution, multiple formats,
    /// metadata embedding, thumbnails, batch burst capture, and async save.
    /// </summary>
    public class ScreenshotFormatManager : MonoBehaviour
    {
        #region Enums

        /// <summary>Output formats supported by the format manager.</summary>
        public enum OutputFormat
        {
            PNG,
            JPG,
            BMP,
            EXR
        }

        /// <summary>Super-resolution multiplier applied before downscaling.</summary>
        public enum SuperResolution
        {
            Native = 1,
            X2     = 2,
            X4     = 4
        }

        #endregion

        #region Events

        /// <summary>Fired when a screenshot file has been written to disk.</summary>
        public static event Action<string> OnScreenshotSaved;

        /// <summary>Fired when a screenshot fails to save.</summary>
        public static event Action<string> OnScreenshotFailed;

        /// <summary>Fired when the storage limit is exceeded during cleanup.</summary>
        public static event Action OnStorageLimitReached;

        #endregion

        #region Inspector

        [Header("Format")]
        [SerializeField]
        private OutputFormat _format = OutputFormat.PNG;

        [SerializeField, Range(1, 100), Tooltip("JPEG quality (only applies to JPG format).")]
        private int _jpegQuality = 95;

        [Header("Resolution")]
        [SerializeField]
        private SuperResolution _superRes = SuperResolution.Native;

        [Header("Output Directory")]
        [SerializeField, Tooltip("Sub-folder inside Application.persistentDataPath.")]
        private string _outputSubFolder = "Screenshots";

        [Header("Storage Management")]
        [SerializeField, Tooltip("Maximum number of screenshots to keep. 0 = unlimited.")]
        private int _maxStoredScreenshots = 200;

        [Header("Thumbnails")]
        [SerializeField]
        private bool _generateThumbnail = true;

        [SerializeField, Range(64, 512)]
        private int _thumbnailSize = 256;

        [Header("Metadata")]
        [SerializeField, Tooltip("Additional JSON metadata embedded in a sidecar file.")]
        private bool _embedMetadata = true;

        [Header("References")]
        [SerializeField]
        private Camera _captureCamera;

        #endregion

        #region Private State

        private string OutputDirectory =>
            Path.Combine(Application.persistentDataPath, _outputSubFolder);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_captureCamera == null)
                _captureCamera = Camera.main;

            Directory.CreateDirectory(OutputDirectory);
        }

        #endregion

        #region Public API

        /// <summary>Capture a single screenshot asynchronously.</summary>
        public void CaptureScreenshot() => StartCoroutine(CaptureRoutine(1));

        /// <summary>Capture a burst of <paramref name="count"/> screenshots.</summary>
        public void CaptureBurst(int count) => StartCoroutine(CaptureRoutine(count));

        /// <summary>Estimate the file size (bytes) of a screenshot at the current settings
        /// without actually rendering.</summary>
        public long EstimateFileSizeBytes()
        {
            int mult = (int)_superRes;
            long pixels = (long)Screen.width * mult * Screen.height * mult;

            switch (_format)
            {
                case OutputFormat.JPG:  return pixels * 3 / 8;         // lossy estimate
                case OutputFormat.BMP:  return pixels * 3;
                case OutputFormat.EXR:  return pixels * 12;            // 4 bytes × 3 channels
                default:                return pixels * 4;             // PNG RGBA
            }
        }

        #endregion

        #region Capture Coroutine

        private IEnumerator CaptureRoutine(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new WaitForEndOfFrame();

                Texture2D tex = null;
                try
                {
                    tex = RenderFrame();
                }
                catch (Exception ex)
                {
                    OnScreenshotFailed?.Invoke(ex.Message);
                    yield break;
                }

                // Offload the actual save to avoid stalling the main thread longer
                string path = BuildFilePath(i);
                yield return SaveAsync(tex, path);

                if (_generateThumbnail)
                    SaveThumbnail(tex, path);

                if (_embedMetadata)
                    SaveMetadataSidecar(path);

                if (_maxStoredScreenshots > 0)
                    CleanupOldScreenshots();

                Destroy(tex);

                if (count > 1)
                    yield return null;   // one-frame gap between burst frames
            }
        }

        private Texture2D RenderFrame()
        {
            int mult = (int)_superRes;
            int w    = Screen.width  * mult;
            int h    = Screen.height * mult;

            var rt       = new RenderTexture(w, h, 24);
            var previous = _captureCamera.targetTexture;
            _captureCamera.targetTexture = rt;
            _captureCamera.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            _captureCamera.targetTexture = previous;
            RenderTexture.active = null;
            rt.Release();
            Destroy(rt);

            // Downscale if super-res
            if (mult > 1)
                tex = Downscale(tex, Screen.width, Screen.height);

            return tex;
        }

        private IEnumerator SaveAsync(Texture2D tex, string path)
        {
            byte[] bytes = null;
            bool done    = false;
            Exception error = null;

            // Encode on a background thread via a short local action flag
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    bytes = Encode(tex);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                done = true;
            });

            while (!done)
                yield return null;

            if (error != null)
            {
                OnScreenshotFailed?.Invoke(error.Message);
                yield break;
            }

            try
            {
                File.WriteAllBytes(path, bytes);
                OnScreenshotSaved?.Invoke(path);
            }
            catch (Exception ex)
            {
                OnScreenshotFailed?.Invoke(ex.Message);
            }
        }

        #endregion

        #region Helpers

        private byte[] Encode(Texture2D tex)
        {
            switch (_format)
            {
                case OutputFormat.JPG: return tex.EncodeToJPG(_jpegQuality);
                case OutputFormat.EXR: return tex.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
                default:               return tex.EncodeToPNG();
            }
        }

        private string BuildFilePath(int burstIndex)
        {
            string ts   = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string ext  = _format == OutputFormat.JPG ? "jpg"
                        : _format == OutputFormat.BMP ? "bmp"
                        : _format == OutputFormat.EXR ? "exr"
                        : "png";
            string name = burstIndex > 0
                ? $"SWEF_{ts}_{burstIndex:D3}.{ext}"
                : $"SWEF_{ts}.{ext}";
            return Path.Combine(OutputDirectory, name);
        }

        private void SaveThumbnail(Texture2D source, string screenshotPath)
        {
            try
            {
                // Maintain source aspect ratio inside the thumbnail bounding box
                float aspect   = source.width / (float)source.height;
                int thumbW     = aspect >= 1f ? _thumbnailSize : Mathf.RoundToInt(_thumbnailSize * aspect);
                int thumbH     = aspect >= 1f ? Mathf.RoundToInt(_thumbnailSize / aspect) : _thumbnailSize;
                var thumb = Downscale(DuplicateTexture(source), thumbW, thumbH);
                string thumbPath = Path.ChangeExtension(screenshotPath, null) + "_thumb.png";
                File.WriteAllBytes(thumbPath, thumb.EncodeToPNG());
                Destroy(thumb);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ScreenshotFormatManager] Thumbnail save failed: {ex.Message}");
            }
        }

        private void SaveMetadataSidecar(string screenshotPath)
        {
            try
            {
                var meta = new ScreenshotMetadata
                {
                    timestamp      = DateTime.UtcNow.ToString("o"),
                    gameVersion    = Application.version,
                    platform       = Application.platform.ToString(),
                    resolution     = $"{Screen.width}x{Screen.height}",
                    superRes       = _superRes.ToString(),
                    format         = _format.ToString()
                };

                string json = JsonUtility.ToJson(meta, true);
                string side = Path.ChangeExtension(screenshotPath, null) + "_meta.json";
                File.WriteAllText(side, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ScreenshotFormatManager] Metadata sidecar failed: {ex.Message}");
            }
        }

        private void CleanupOldScreenshots()
        {
            try
            {
                var files = new List<string>(Directory.GetFiles(OutputDirectory, "SWEF_*.png"));
                files.AddRange(Directory.GetFiles(OutputDirectory, "SWEF_*.jpg"));
                files.AddRange(Directory.GetFiles(OutputDirectory, "SWEF_*.exr"));
                files.AddRange(Directory.GetFiles(OutputDirectory, "SWEF_*.bmp"));
                files.Sort();

                if (files.Count > _maxStoredScreenshots)
                {
                    OnStorageLimitReached?.Invoke();
                    int toRemove = files.Count - _maxStoredScreenshots;
                    for (int i = 0; i < toRemove; i++)
                    {
                        File.Delete(files[i]);
                        string meta  = Path.ChangeExtension(files[i], null) + "_meta.json";
                        string thumb = Path.ChangeExtension(files[i], null) + "_thumb.png";
                        if (File.Exists(meta))  File.Delete(meta);
                        if (File.Exists(thumb)) File.Delete(thumb);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ScreenshotFormatManager] Cleanup failed: {ex.Message}");
            }
        }

        private Texture2D Downscale(Texture2D src, int targetW, int targetH)
        {
            var rt = new RenderTexture(targetW, targetH, 0);
            Graphics.Blit(src, rt);

            RenderTexture.active = rt;
            var result = new Texture2D(targetW, targetH, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            rt.Release();
            Destroy(rt);
            Destroy(src);
            return result;
        }

        private Texture2D DuplicateTexture(Texture2D source)
        {
            var copy = new Texture2D(source.width, source.height, source.format, false);
            copy.SetPixels(source.GetPixels());
            copy.Apply();
            return copy;
        }

        #endregion

        #region Metadata Type

        [Serializable]
        private class ScreenshotMetadata
        {
            public string timestamp;
            public string gameVersion;
            public string platform;
            public string resolution;
            public string superRes;
            public string format;
        }

        #endregion
    }
}
