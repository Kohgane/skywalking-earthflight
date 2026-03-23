using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.PhotoMode
{
    /// <summary>Automated timelapse photo capture during flight with interval, duration, and path options.</summary>
    public class TimelapseController : MonoBehaviour
    {
        #region Events

        /// <summary>Fired when timelapse recording begins.</summary>
        public static event Action OnTimelapseStarted;

        /// <summary>Fired when a timelapse frame is captured. Supplies current frame index and total.</summary>
        public static event Action<int, int> OnTimelapseFrameCaptured;

        /// <summary>Fired when the timelapse sequence completes. Supplies list of saved file paths.</summary>
        public static event Action<IReadOnlyList<string>> OnTimelapseCompleted;

        #endregion

        #region Inspector

        [Header("Capture Parameters")]
        [SerializeField, Tooltip("Seconds between each captured frame."), Range(0.5f, 300f)]
        private float _captureInterval = 5f;

        [SerializeField, Tooltip("Maximum number of frames to capture. 0 = unlimited (use duration).")]
        private int _maxFrameCount = 0;

        [SerializeField, Tooltip("Maximum recording duration in seconds. 0 = unlimited (use frame count).")]
        private float _maxDuration = 0f;

        [Header("Capture Mode")]
        [SerializeField, Tooltip("When enabled, capture every _distanceInterval metres instead of on a time basis.")]
        private bool _useDistanceInterval = false;

        [SerializeField, Tooltip("Distance in metres between captures when distance mode is active."), Range(10f, 10000f)]
        private float _distanceInterval = 500f;

        [Header("Output")]
        [SerializeField, Tooltip("Output format for each timelapse frame.")]
        private PhotoFormat _frameFormat = PhotoFormat.PNG;

        [SerializeField, Range(1, 100)]
        private int _jpegQuality = 85;

        [Header("References")]
        [SerializeField]
        private Camera _captureCamera;

        [SerializeField]
        private PhotoGalleryManager _galleryManager;

        [SerializeField, Tooltip("Optional: transform tracked for distance-based capture.")]
        private Transform _aircraftTransform;

        #endregion

        #region Private State

        private bool _isRecording;
        private bool _isPaused;
        private Coroutine _recordingCoroutine;
        private readonly List<string> _capturedPaths = new List<string>();
        private int _frameIndex;
        private float _elapsedDuration;
        private Vector3 _lastCapturePosition;
        private string _sessionFolder;

        #endregion

        #region Public Properties

        /// <summary>Whether a timelapse is currently in progress (including paused state).</summary>
        public bool IsRecording => _isRecording;

        /// <summary>Whether the timelapse is paused.</summary>
        public bool IsPaused => _isPaused;

        /// <summary>Number of frames captured in the current session.</summary>
        public int FrameCount => _frameIndex;

        /// <summary>Read-only list of paths saved during the current/last session.</summary>
        public IReadOnlyList<string> CapturedPaths => _capturedPaths;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_captureCamera == null)
                _captureCamera = Camera.main;

            if (_galleryManager == null)
                _galleryManager = FindObjectOfType<PhotoGalleryManager>();
        }

        private void OnDestroy()
        {
            if (_isRecording)
                StopTimelapse();
        }

        #endregion

        #region Public API

        /// <summary>Begin a new timelapse recording session.</summary>
        public void StartTimelapse()
        {
            if (_isRecording)
            {
                Debug.LogWarning("[TimelapseController] Timelapse already running.");
                return;
            }

            _capturedPaths.Clear();
            _frameIndex      = 0;
            _elapsedDuration = 0f;
            _isPaused        = false;
            _isRecording     = true;
            // Capture session folder name once at start so all frames share the same directory
            _sessionFolder = Path.Combine(Application.persistentDataPath, "Timelapse",
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(_sessionFolder);

            if (_aircraftTransform != null)
                _lastCapturePosition = _aircraftTransform.position;

            _recordingCoroutine = _useDistanceInterval
                ? StartCoroutine(DistanceBasedRoutine())
                : StartCoroutine(TimeBasedRoutine());

            OnTimelapseStarted?.Invoke();
        }

        /// <summary>Stop the timelapse and finalise the sequence.</summary>
        public void StopTimelapse()
        {
            if (!_isRecording) return;

            if (_recordingCoroutine != null)
            {
                StopCoroutine(_recordingCoroutine);
                _recordingCoroutine = null;
            }

            _isRecording = false;
            _isPaused    = false;

            _galleryManager?.RefreshGallery();
            OnTimelapseCompleted?.Invoke(_capturedPaths);
        }

        /// <summary>Pause the timelapse (no new frames are captured).</summary>
        public void Pause()
        {
            if (_isRecording && !_isPaused)
                _isPaused = true;
        }

        /// <summary>Resume a paused timelapse.</summary>
        public void Resume()
        {
            if (_isRecording && _isPaused)
                _isPaused = false;
        }

        #endregion

        #region Recording Coroutines

        private IEnumerator TimeBasedRoutine()
        {
            while (_isRecording)
            {
                if (!_isPaused)
                {
                    yield return new WaitForEndOfFrame();
                    CaptureFrame();

                    if (IsLimitReached()) break;
                }

                float waited = 0f;
                while (waited < _captureInterval)
                {
                    if (!_isPaused)
                        waited += Time.deltaTime;
                    yield return null;
                }

                _elapsedDuration += _captureInterval;
            }

            if (_isRecording) StopTimelapse();
        }

        private IEnumerator DistanceBasedRoutine()
        {
            while (_isRecording)
            {
                if (!_isPaused && _aircraftTransform != null)
                {
                    float dist = Vector3.Distance(_aircraftTransform.position, _lastCapturePosition);
                    if (dist >= _distanceInterval)
                    {
                        _lastCapturePosition = _aircraftTransform.position;
                        yield return new WaitForEndOfFrame();
                        CaptureFrame();

                        if (IsLimitReached()) break;
                    }
                }

                yield return null;
            }

            if (_isRecording) StopTimelapse();
        }

        private bool IsLimitReached()
        {
            if (_maxFrameCount > 0 && _frameIndex >= _maxFrameCount) return true;
            if (_maxDuration   > 0 && _elapsedDuration >= _maxDuration) return true;
            return false;
        }

        #endregion

        #region Frame Capture

        private void CaptureFrame()
        {
            string path = SaveFrame();
            if (string.IsNullOrEmpty(path)) return;

            _capturedPaths.Add(path);
            _frameIndex++;
            int total = _maxFrameCount > 0 ? _maxFrameCount : -1;
            OnTimelapseFrameCaptured?.Invoke(_frameIndex, total);
        }

        private string SaveFrame()
        {
            int w = Screen.width;
            int h = Screen.height;

            var rt = new RenderTexture(w, h, 24);
            var previous = _captureCamera.targetTexture;
            _captureCamera.targetTexture = rt;
            _captureCamera.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            _captureCamera.targetTexture = previous;
            RenderTexture.active = null;
            rt.Release();
            Destroy(rt);

            string ext      = _frameFormat == PhotoFormat.JPEG ? "jpg" : "png";
            string fileName = $"frame_{_frameIndex:D5}.{ext}";
            string path     = Path.Combine(_sessionFolder, fileName);

            byte[] bytes = _frameFormat == PhotoFormat.JPEG
                ? tex.EncodeToJPG(_jpegQuality)
                : tex.EncodeToPNG();

            Destroy(tex);

            try
            {
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TimelapseController] Frame save failed: {ex.Message}");
                return null;
            }

            return path;
        }

        #endregion
    }
}
