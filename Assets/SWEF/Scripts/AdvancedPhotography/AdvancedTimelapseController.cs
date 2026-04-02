// AdvancedTimelapseController.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_WEATHER_AVAILABLE
using SWEF.Weather;
#endif

namespace SWEF.AdvancedPhotography
{
    /// <summary>Serializable configuration for a single timelapse session.</summary>
    [Serializable]
    public class TimelapseConfig
    {
        /// <summary>Capture mode determining what triggers each frame.</summary>
        public TimelapseMode mode = TimelapseMode.TimeInterval;

        /// <summary>Interval (seconds) between frames in TimeInterval mode.</summary>
        [Min(0.5f)] public float timeInterval = AdvancedPhotographyConfig.TimelapseDefaultInterval;

        /// <summary>Distance (metres) between frames in DistanceInterval mode.</summary>
        [Min(1f)] public float distanceInterval = AdvancedPhotographyConfig.TimelapseDefaultDistanceInterval;

        /// <summary>Solar angle step (degrees) between frames in SunTracking mode.</summary>
        [Range(0.5f, 15f)] public float sunAngleStep = 5f;

        /// <summary>Maximum number of frames to capture (0 = unlimited up to global max).</summary>
        [Min(0)] public int maxFrames = 0;
    }

    /// <summary>
    /// Phase 89 — MonoBehaviour that captures timelapse sequences.
    ///
    /// <para>Supports five modes: TimeInterval, DistanceInterval, SunTracking,
    /// WeatherChange, and DayNightCycle.  Frames are stored in a buffer and reported
    /// via events on completion.</para>
    /// </summary>
    public sealed class AdvancedTimelapseController : MonoBehaviour
    {
        #region Events

        /// <summary>Fired when a timelapse session starts.</summary>
        public event Action OnTimelapseStarted;

        /// <summary>Fired each time a frame is captured, passing the current frame index.</summary>
        public event Action<int> OnFrameCaptured;

        /// <summary>Fired when the timelapse session completes, passing the frame array.</summary>
        public event Action<Texture2D[]> OnTimelapseComplete;

        /// <summary>Fired when a timelapse session is cancelled before completion.</summary>
        public event Action OnTimelapseCancelled;

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Camera used to capture timelapse frames. Defaults to Camera.main if null.")]
        [SerializeField] private Camera _captureCamera;

        [Tooltip("Player transform used for DistanceInterval calculations.")]
        [SerializeField] private Transform _playerTransform;

        [Header("Output")]
        [Tooltip("Width (pixels) of each timelapse frame.")]
        [SerializeField] [Min(64)] private int _frameWidth = 1280;

        [Tooltip("Height (pixels) of each timelapse frame.")]
        [SerializeField] [Min(64)] private int _frameHeight = 720;

        #endregion

        #region Private State

        private bool _running  = false;
        private bool _paused   = false;
        private Coroutine _captureCoroutine;

        private List<Texture2D> _frames = new List<Texture2D>();
        private float _elapsedTime = 0f;
        private float _lastCaptureTime = 0f;
        private Vector3 _lastPosition;
        private float _distanceTravelled = 0f;

#if SWEF_WEATHER_AVAILABLE
        private string _lastWeatherCondition = "";
#endif

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_captureCamera == null)
                _captureCamera = Camera.main;
        }

        #endregion

        #region Public API

        /// <summary>Starts a timelapse session with the specified configuration.</summary>
        public void StartTimelapse(TimelapseConfig config)
        {
            if (_running)
            {
                Debug.LogWarning("[SWEF] AdvancedTimelapseController: already running.");
                return;
            }

            _frames.Clear();
            _elapsedTime      = 0f;
            _lastCaptureTime  = 0f;
            _distanceTravelled = 0f;

            if (_playerTransform != null)
                _lastPosition = _playerTransform.position;

#if SWEF_WEATHER_AVAILABLE
            _lastWeatherCondition = WeatherManager.Instance?.CurrentWeather ?? "";
#endif

            _captureCoroutine = StartCoroutine(TimelapseLoop(config));
            OnTimelapseStarted?.Invoke();
            Debug.Log($"[SWEF] AdvancedTimelapseController: started ({config.mode})");
        }

        /// <summary>Stops the timelapse, finalises the frame buffer, and fires completion event.</summary>
        public void StopTimelapse()
        {
            if (!_running) return;

            if (_captureCoroutine != null)
                StopCoroutine(_captureCoroutine);

            _running = false;
            _paused  = false;

            OnTimelapseComplete?.Invoke(_frames.ToArray());
            AdvancedPhotographyAnalytics.RecordTimelapseCompleted(_frames.Count);
            Debug.Log($"[SWEF] AdvancedTimelapseController: completed ({_frames.Count} frames)");
        }

        /// <summary>Pauses frame capture without discarding the buffer.</summary>
        public void PauseTimelapse()
        {
            _paused = true;
            Debug.Log("[SWEF] AdvancedTimelapseController: paused");
        }

        /// <summary>Resumes a paused timelapse.</summary>
        public void ResumeTimelapse()
        {
            _paused = false;
            Debug.Log("[SWEF] AdvancedTimelapseController: resumed");
        }

        /// <summary>Returns the number of frames currently in the buffer.</summary>
        public int GetFrameCount() => _frames.Count;

        /// <summary>Returns the elapsed recording time in seconds.</summary>
        public float GetElapsedTime() => _elapsedTime;

        #endregion

        #region Private — Timelapse Loop

        private IEnumerator TimelapseLoop(TimelapseConfig config)
        {
            _running = true;
            int maxFrames = config.maxFrames > 0
                ? Mathf.Min(config.maxFrames, AdvancedPhotographyConfig.TimelapseMaxFrameCount)
                : AdvancedPhotographyConfig.TimelapseMaxFrameCount;

            while (_running && _frames.Count < maxFrames)
            {
                if (_paused)
                {
                    yield return null;
                    continue;
                }

                _elapsedTime += Time.deltaTime;

                bool shouldCapture = false;

                switch (config.mode)
                {
                    case TimelapseMode.TimeInterval:
                        shouldCapture = (_elapsedTime - _lastCaptureTime) >= config.timeInterval;
                        break;

                    case TimelapseMode.DistanceInterval:
                        if (_playerTransform != null)
                        {
                            _distanceTravelled += Vector3.Distance(_playerTransform.position, _lastPosition);
                            _lastPosition       = _playerTransform.position;
                        }
                        shouldCapture = _distanceTravelled >= config.distanceInterval;
                        if (shouldCapture) _distanceTravelled = 0f;
                        break;

                    case TimelapseMode.WeatherChange:
#if SWEF_WEATHER_AVAILABLE
                        string current = WeatherManager.Instance?.CurrentWeather ?? "";
                        if (current != _lastWeatherCondition)
                        {
                            shouldCapture = true;
                            _lastWeatherCondition = current;
                        }
#endif
                        break;

                    case TimelapseMode.SunTracking:
                    case TimelapseMode.DayNightCycle:
                        // Fallback: capture every 10 s when no specific integration is available.
                        shouldCapture = (_elapsedTime - _lastCaptureTime) >= 10f;
                        break;
                }

                if (shouldCapture)
                {
                    Texture2D frame = CaptureFrame();
                    if (frame != null)
                    {
                        _frames.Add(frame);
                        _lastCaptureTime = _elapsedTime;
                        OnFrameCaptured?.Invoke(_frames.Count - 1);
                    }
                }

                yield return null;
            }

            StopTimelapse();
        }

        private Texture2D CaptureFrame()
        {
            if (_captureCamera == null) return null;

            var rt = new RenderTexture(_frameWidth, _frameHeight, 24);
            _captureCamera.targetTexture = rt;
            _captureCamera.Render();
            _captureCamera.targetTexture = null;

            RenderTexture.active = rt;
            var tex = new Texture2D(_frameWidth, _frameHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _frameWidth, _frameHeight), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            UnityEngine.Object.Destroy(rt);
            return tex;
        }

        #endregion
    }
}
