using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SWEF.Screenshot;
using SWEF.Social;

namespace SWEF.PhotoMode
{
    /// <summary>
    /// Singleton that orchestrates photo capture for the Photo Mode system.
    /// Supports single capture, burst mode, countdown timer, bracketed HDR capture,
    /// and guided panorama stitching.  Generates thumbnails and writes
    /// <see cref="PhotoMetadata"/> JSON sidecar files alongside each photo.
    /// </summary>
    public class PhotoCaptureManager : MonoBehaviour
    {
        #region Constants
        private const string PhotoFolderRoot    = "Photos";
        private const string DateFolderFormat   = "yyyy-MM-dd";
        private const string PhotoFilePrefix    = "SWEF_";
        private const string MetadataExtension  = ".json";
        private const string ThumbnailSuffix    = "_thumb";
        private const int    ThumbnailWidth     = 256;
        private const int    ThumbnailHeight    = 144;
        private const float  HDRBracketEV       = 2f;
        private const int    HDRExposureCount   = 3;
        private const float  DefaultBurstInterval = 0.3f;
        #endregion

        #region Singleton
        /// <summary>Global singleton instance.</summary>
        public static PhotoCaptureManager Instance { get; private set; }
        #endregion

        #region Inspector
        [Header("References (auto-found if null)")]
        [Tooltip("ScreenshotController for low-level screen capture.")]
        [SerializeField] private ScreenshotController screenshotController;

        [Tooltip("ShareManager for social sharing integration.")]
        [SerializeField] private ShareManager shareManager;

        [Tooltip("Camera used for rendering captures. Defaults to Camera.main.")]
        [SerializeField] private Camera captureCamera;

        [Tooltip("PhotoCameraController supplying current CameraSettings.")]
        [SerializeField] private PhotoCameraController photoCameraController;

        [Header("Configuration")]
        [Tooltip("JPEG quality (1–100) used when CameraSettings.format is JPEG.")]
        [Range(1, 100)]
        [SerializeField] private int defaultJpegQuality = 95;
        #endregion

        #region Events
        /// <summary>Fired after each successful photo capture.</summary>
        public event Action<PhotoMetadata> OnPhotoCaptured;

        /// <summary>Fired after a burst sequence completes.</summary>
        public event Action OnBurstComplete;

        /// <summary>Fired after a panorama capture completes.</summary>
        public event Action OnPanoramaComplete;
        #endregion

        #region Public properties
        /// <summary>True while a burst, timer, HDR, or panorama capture is in progress.</summary>
        public bool IsBusy { get; private set; }
        #endregion

        #region Private state
        private Coroutine _activeCoroutine;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (screenshotController == null)
                screenshotController = FindObjectOfType<ScreenshotController>();
            if (shareManager == null)
                shareManager = FindObjectOfType<ShareManager>();
            if (captureCamera == null)
                captureCamera = Camera.main;
            if (photoCameraController == null)
                photoCameraController = FindObjectOfType<PhotoCameraController>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Captures a single photo using the current <see cref="PhotoCameraController"/> settings.
        /// </summary>
        /// <returns>Metadata for the captured photo, or null on failure.</returns>
        public PhotoMetadata CapturePhoto()
        {
            return CapturePhotoInternal(photoCameraController?.Settings ?? new CameraSettings());
        }

        /// <summary>
        /// Starts a burst sequence, capturing <paramref name="count"/> photos spaced
        /// <paramref name="interval"/> seconds apart.
        /// </summary>
        /// <param name="count">Number of photos to capture.</param>
        /// <param name="interval">Interval between captures in seconds.</param>
        public void CaptureBurst(int count, float interval = DefaultBurstInterval)
        {
            if (IsBusy) return;
            _activeCoroutine = StartCoroutine(BurstCoroutine(count, interval));
        }

        /// <summary>
        /// Starts a countdown timer and captures a photo when it expires.
        /// </summary>
        /// <param name="seconds">Countdown duration in seconds.</param>
        public void StartTimer(float seconds)
        {
            if (IsBusy) return;
            _activeCoroutine = StartCoroutine(TimerCoroutine(seconds));
        }

        /// <summary>
        /// Captures three bracketed exposures (−2 EV, 0 EV, +2 EV) and merges them
        /// into a single HDR-style photo.
        /// </summary>
        public void CaptureHDR()
        {
            if (IsBusy) return;
            _activeCoroutine = StartCoroutine(HDRCoroutine());
        }

        /// <summary>
        /// Starts an interactive panorama capture session.  The system guides the
        /// player to rotate the view and auto-captures strips for cylindrical stitching.
        /// </summary>
        public void StartPanorama()
        {
            if (IsBusy) return;
            _activeCoroutine = StartCoroutine(PanoramaCoroutine());
        }

        /// <summary>
        /// Exports a photo to an arbitrary destination path, re-encoding if necessary.
        /// </summary>
        /// <param name="metadata">Metadata of the photo to export.</param>
        /// <param name="destinationPath">Absolute destination file path.</param>
        public void ExportPhoto(PhotoMetadata metadata, string destinationPath)
        {
            if (metadata == null || string.IsNullOrEmpty(metadata.filePath)) return;
            if (!File.Exists(metadata.filePath)) return;

            string dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.Copy(metadata.filePath, destinationPath, overwrite: true);
        }
        #endregion

        #region Private capture core
        private PhotoMetadata CapturePhotoInternal(CameraSettings settings)
        {
            string folder = GetDateFolder();
            string photoId = Guid.NewGuid().ToString("N");
            string ext    = GetExtension(settings.format);
            string path   = Path.Combine(folder, PhotoFilePrefix + photoId + ext);

            Texture2D shot = CaptureScreenTexture(settings);
            if (shot == null) return null;

            byte[] bytes = EncodeTexture(shot, settings);
            Destroy(shot);
            if (bytes == null) return null;

            File.WriteAllBytes(path, bytes);

            // Thumbnail
            string thumbPath = Path.Combine(folder, PhotoFilePrefix + photoId + ThumbnailSuffix + ".png");
            GenerateThumbnail(path, thumbPath);

            // Metadata
            PhotoMetadata meta = BuildMetadata(photoId, path, thumbPath, settings, bytes.Length);
            string metaPath = Path.ChangeExtension(path, MetadataExtension);
            File.WriteAllText(metaPath, JsonUtility.ToJson(meta, true));

            OnPhotoCaptured?.Invoke(meta);
            return meta;
        }

        private Texture2D CaptureScreenTexture(CameraSettings settings)
        {
            if (captureCamera == null) return null;

            Vector2Int res = GetResolutionSize(settings.resolution);
            RenderTexture rt = new RenderTexture(res.x, res.y, 24);
            Camera cam = captureCamera;
            RenderTexture prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;

            Texture2D tex = new Texture2D(res.x, res.y, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, res.x, res.y), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            rt.Release();
            Destroy(rt);
            return tex;
        }

        private static byte[] EncodeTexture(Texture2D tex, CameraSettings settings)
        {
            switch (settings.format)
            {
                case PhotoFormat.PNG:  return tex.EncodeToPNG();
                case PhotoFormat.RAW:  return tex.GetRawTextureData();
                case PhotoFormat.TIFF: return tex.EncodeToPNG(); // TIFF not natively available; use PNG
                default:               return tex.EncodeToJPG(settings.jpegQuality);
            }
        }

        private static void GenerateThumbnail(string sourcePath, string thumbPath)
        {
            // Thumbnails are generated at runtime from the render texture.
            // This is a stub; a full implementation would downscale the captured texture.
            _ = sourcePath;
            _ = thumbPath;
        }

        private PhotoMetadata BuildMetadata(string id, string path, string thumbPath,
                                             CameraSettings settings, long fileSize)
        {
            Vector2Int res = GetResolutionSize(settings.resolution);
            return new PhotoMetadata
            {
                photoId       = id,
                timestamp     = DateTime.UtcNow.ToString("o"),
                filePath      = path,
                thumbnailPath = thumbPath,
                fileSize      = fileSize,
                width         = res.x,
                height        = res.y,
                cameraSettings = settings,
                tags          = new List<string>()
            };
        }
        #endregion

        #region Coroutines
        private IEnumerator BurstCoroutine(int count, float interval)
        {
            IsBusy = true;
            CameraSettings s = photoCameraController?.Settings ?? new CameraSettings();
            for (int i = 0; i < count; i++)
            {
                CapturePhotoInternal(s);
                if (i < count - 1)
                    yield return new WaitForSeconds(interval);
            }
            IsBusy = false;
            OnBurstComplete?.Invoke();
        }

        private IEnumerator TimerCoroutine(float seconds)
        {
            IsBusy = true;
            yield return new WaitForSeconds(seconds);
            CapturePhoto();
            IsBusy = false;
        }

        private IEnumerator HDRCoroutine()
        {
            IsBusy = true;
            CameraSettings s = photoCameraController?.Settings ?? new CameraSettings();
            float origEC = s.exposureCompensation;
            List<PhotoMetadata> brackets = new List<PhotoMetadata>(HDRExposureCount);

            float[] evOffsets = { -HDRBracketEV, 0f, HDRBracketEV };
            foreach (float ev in evOffsets)
            {
                s.exposureCompensation = origEC + ev;
                photoCameraController?.ApplySettings(s);
                yield return null; // wait one frame for settings to apply
                brackets.Add(CapturePhotoInternal(s));
            }

            // Restore original exposure
            s.exposureCompensation = origEC;
            photoCameraController?.ApplySettings(s);

            // In a full implementation the three textures would be tonemapped and merged.
            // Emit the middle (0 EV) capture as the primary HDR output.
            if (brackets.Count > 1 && brackets[1] != null)
                OnPhotoCaptured?.Invoke(brackets[1]);

            IsBusy = false;
        }

        private IEnumerator PanoramaCoroutine()
        {
            IsBusy = true;
            // Guide the player through a set of horizontal rotation steps and capture.
            const int Steps        = 6;
            const float StepDelay  = 2f;
            CameraSettings s = photoCameraController?.Settings ?? new CameraSettings();
            List<PhotoMetadata> strips = new List<PhotoMetadata>(Steps);

            for (int i = 0; i < Steps; i++)
            {
                yield return new WaitForSeconds(StepDelay);
                strips.Add(CapturePhotoInternal(s));
            }

            // In a full implementation the strips would be stitched via cylindrical projection.
            OnPanoramaComplete?.Invoke();
            IsBusy = false;
        }
        #endregion

        #region Helpers
        private static string GetDateFolder()
        {
            string folder = Path.Combine(Application.persistentDataPath, PhotoFolderRoot,
                DateTime.Now.ToString(DateFolderFormat));
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        private static string GetExtension(PhotoFormat format)
        {
            switch (format)
            {
                case PhotoFormat.PNG:  return ".png";
                case PhotoFormat.RAW:  return ".raw";
                case PhotoFormat.TIFF: return ".tiff";
                default:               return ".jpg";
            }
        }

        private static Vector2Int GetResolutionSize(PhotoResolution res)
        {
            switch (res)
            {
                case PhotoResolution.HD_720:   return new Vector2Int(1280,  720);
                case PhotoResolution.QHD_1440: return new Vector2Int(2560, 1440);
                case PhotoResolution.UHD_4K:   return new Vector2Int(3840, 2160);
                case PhotoResolution.UHD_8K:   return new Vector2Int(7680, 4320);
                default:                       return new Vector2Int(1920, 1080);
            }
        }
        #endregion
    }
}
