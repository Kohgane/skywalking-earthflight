using System;
using UnityEngine;
using SWEF.Analytics;

namespace SWEF.PhotoMode
{
    /// <summary>
    /// Tracks Photo Mode usage and emits analytics events via
    /// <see cref="UserBehaviorTracker"/> and the <see cref="TelemetryDispatcher"/> pipeline.
    /// </summary>
    public class PhotoModeAnalytics : MonoBehaviour
    {
        #region Constants
        private const string EventEntered       = "photo_mode_entered";
        private const string EventExited        = "photo_mode_exited";
        private const string EventCaptured      = "photo_captured";
        private const string EventFilterApplied = "filter_applied";
        private const string EventFrameApplied  = "frame_applied";
        private const string EventDroneDeployed = "drone_deployed";
        private const string EventDroneModeChg  = "drone_mode_changed";
        private const string EventShared        = "photo_shared";
        private const string EventGalleryOpened = "gallery_opened";
        private const string EventSlideshow     = "slideshow_started";
        #endregion

        #region Inspector
        [Header("References (auto-found if null)")]
        [SerializeField] private UserBehaviorTracker behaviorTracker;
        [SerializeField] private TelemetryDispatcher telemetryDispatcher;

        [Header("References — Photo Mode")]
        [SerializeField] private DroneCameraController droneController;
        [SerializeField] private PhotoCaptureManager   captureManager;
        [SerializeField] private PhotoFilterSystem     filterSystem;
        #endregion

        #region Private state
        private float _sessionStart;
        private int   _photosThisSession;
        private float _droneDeployTime;
        private float _totalDroneTime;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (behaviorTracker    == null) behaviorTracker    = FindObjectOfType<UserBehaviorTracker>();
            if (telemetryDispatcher == null) telemetryDispatcher = FindObjectOfType<TelemetryDispatcher>();
            if (droneController    == null) droneController    = FindObjectOfType<DroneCameraController>();
            if (captureManager     == null) captureManager     = FindObjectOfType<PhotoCaptureManager>();
            if (filterSystem       == null) filterSystem       = FindObjectOfType<PhotoFilterSystem>();
        }

        private void OnEnable()
        {
            if (captureManager != null)
                captureManager.OnPhotoCaptured += HandlePhotoCaptured;
            if (droneController != null)
            {
                droneController.OnDeployed     += HandleDroneDeployed;
                droneController.OnRecalled     += HandleDroneRecalled;
                droneController.OnModeChanged  += HandleDroneModeChanged;
            }
        }

        private void OnDisable()
        {
            if (captureManager != null)
                captureManager.OnPhotoCaptured -= HandlePhotoCaptured;
            if (droneController != null)
            {
                droneController.OnDeployed    -= HandleDroneDeployed;
                droneController.OnRecalled    -= HandleDroneRecalled;
                droneController.OnModeChanged -= HandleDroneModeChanged;
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Records that the player entered Photo Mode.  Call from the entry point.
        /// </summary>
        public void TrackEntered()
        {
            _sessionStart      = Time.realtimeSinceStartup;
            _photosThisSession = 0;
            behaviorTracker?.TrackScreenOpen("PhotoMode");
            EnqueueEvent(EventEntered);
        }

        /// <summary>
        /// Records that the player exited Photo Mode and summarises the session.
        /// </summary>
        public void TrackExited()
        {
            float duration = Time.realtimeSinceStartup - _sessionStart;
            behaviorTracker?.TrackScreenClose("PhotoMode");
            EnqueueEvent(EventExited,
                $"duration={duration:F1}s photos={_photosThisSession} avg_drone_time={_totalDroneTime:F1}s");
        }

        /// <summary>
        /// Records that a filter was applied.
        /// </summary>
        /// <param name="filter">Filter that was applied.</param>
        /// <param name="intensity">Blend intensity used.</param>
        public void TrackFilterApplied(PhotoFilter filter, float intensity)
        {
            behaviorTracker?.TrackSettingsChange("photo_filter", null, filter.ToString());
            EnqueueEvent(EventFilterApplied, $"filter={filter} intensity={intensity:F2}");
        }

        /// <summary>
        /// Records that a frame style was applied.
        /// </summary>
        /// <param name="frame">Frame style that was applied.</param>
        public void TrackFrameApplied(FrameStyle frame)
        {
            behaviorTracker?.TrackSettingsChange("photo_frame", null, frame.ToString());
            EnqueueEvent(EventFrameApplied, $"frame={frame}");
        }

        /// <summary>
        /// Records that a photo was shared via the social share flow.
        /// </summary>
        /// <param name="photoId">ID of the shared photo.</param>
        public void TrackPhotoShared(string photoId)
        {
            EnqueueEvent(EventShared, $"photo_id={photoId}");
        }

        /// <summary>
        /// Records that the gallery was opened.
        /// </summary>
        public void TrackGalleryOpened()
        {
            behaviorTracker?.TrackScreenOpen("PhotoGallery");
            EnqueueEvent(EventGalleryOpened);
        }

        /// <summary>
        /// Records that the gallery slideshow was started.
        /// </summary>
        /// <param name="photoCount">Number of photos in the slideshow.</param>
        public void TrackSlideshowStarted(int photoCount)
        {
            EnqueueEvent(EventSlideshow, $"count={photoCount}");
        }
        #endregion

        #region Event handlers
        private void HandlePhotoCaptured(PhotoMetadata meta)
        {
            _photosThisSession++;
            string filter = meta?.cameraSettings?.filter.ToString() ?? "None";
            string frame  = meta?.cameraSettings?.frame.ToString()  ?? "None";
            EnqueueEvent(EventCaptured, $"filter={filter} frame={frame}");
        }

        private void HandleDroneDeployed()
        {
            _droneDeployTime = Time.realtimeSinceStartup;
            EnqueueEvent(EventDroneDeployed);
        }

        private void HandleDroneRecalled()
        {
            if (_droneDeployTime > 0f)
                _totalDroneTime += Time.realtimeSinceStartup - _droneDeployTime;
            _droneDeployTime = 0f;
        }

        private void HandleDroneModeChanged(DroneMode mode)
        {
            EnqueueEvent(EventDroneModeChg, $"mode={mode}");
        }
        #endregion

        #region Helpers
        private void EnqueueEvent(string name, string extraData = "")
        {
            if (telemetryDispatcher == null) return;
            var evt = TelemetryEventBuilder.Create(name)
                .WithCategory("photo_mode")
                .WithProperty("extra", extraData)
                .Build();
            telemetryDispatcher.EnqueueEvent(evt);
        }
        #endregion
    }
}
