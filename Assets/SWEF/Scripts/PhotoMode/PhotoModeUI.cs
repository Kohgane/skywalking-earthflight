using System;
using UnityEngine;
using UnityEngine.UI;
using SWEF.UI;
using SWEF.Accessibility;

namespace SWEF.PhotoMode
{
    /// <summary>
    /// Full photo mode HUD MonoBehaviour.  Manages the viewfinder overlays, drone
    /// controls, camera settings panels, filter strip, and post-capture review flow.
    /// Integrates with <see cref="HudBinder"/> for overlay toggling and
    /// <see cref="AccessibilityManager"/> for text scaling.
    /// </summary>
    public class PhotoModeUI : MonoBehaviour
    {
        #region Constants
        private const float PinchZoomSensitivity  = 0.1f;
        private const float SliderAnimSpeed       = 8f;
        private const float ShutterFlashDuration  = 0.15f;
        private const float ReviewSlideUpDuration = 0.3f;
        #endregion

        #region Inspector
        [Header("Panel Roots")]
        [Tooltip("Top status bar (battery, mode selector, settings gear).")]
        [SerializeField] private GameObject topBar;

        [Tooltip("Bottom bar (shutter, gallery shortcut, filter access).")]
        [SerializeField] private GameObject bottomBar;

        [Tooltip("Left settings panel (FOV, aperture, ISO, shutter, WB, focus sliders).")]
        [SerializeField] private GameObject leftPanel;

        [Tooltip("Right filter browser panel.")]
        [SerializeField] private GameObject rightPanel;

        [Tooltip("Post-capture review panel.")]
        [SerializeField] private GameObject reviewPanel;

        [Header("Overlays")]
        [Tooltip("Rule-of-thirds grid overlay Image.")]
        [SerializeField] private GameObject gridOverlay;

        [Tooltip("Horizon level indicator.")]
        [SerializeField] private GameObject levelOverlay;

        [Tooltip("Histogram panel.")]
        [SerializeField] private GameObject histogramOverlay;

        [Tooltip("Zebra overexposure overlay.")]
        [SerializeField] private GameObject zebraOverlay;

        [Tooltip("White flash Image used for shutter effect.")]
        [SerializeField] private Image shutterFlash;

        [Header("Battery & Drone Mini-Map")]
        [Tooltip("Battery bar fill image.")]
        [SerializeField] private Image batteryFill;

        [Tooltip("Drone mini-map root.")]
        [SerializeField] private GameObject miniMap;

        [Header("References (auto-found if null)")]
        [SerializeField] private DroneCameraController droneController;
        [SerializeField] private PhotoCameraController cameraController;
        [SerializeField] private PhotoCaptureManager   captureManager;
        [SerializeField] private PhotoFilterSystem     filterSystem;
        [SerializeField] private HudBinder             hudBinder;
        [SerializeField] private AccessibilityManager  accessibilityManager;
        #endregion

        #region Events
        /// <summary>Fired when the player taps the shutter button.</summary>
        public event Action OnShutterPressed;

        /// <summary>Fired when the user opens the gallery from the HUD shortcut.</summary>
        public event Action OnGalleryShortcutPressed;
        #endregion

        #region Private state
        private bool  _isVisible;
        private float _pinchLastDist;
        private bool  _isPinching;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (droneController    == null) droneController    = FindObjectOfType<DroneCameraController>();
            if (cameraController   == null) cameraController   = FindObjectOfType<PhotoCameraController>();
            if (captureManager     == null) captureManager     = FindObjectOfType<PhotoCaptureManager>();
            if (filterSystem       == null) filterSystem       = FindObjectOfType<PhotoFilterSystem>();
            if (hudBinder          == null) hudBinder          = FindObjectOfType<HudBinder>();
            if (accessibilityManager == null) accessibilityManager = FindObjectOfType<AccessibilityManager>();
        }

        private void OnEnable()
        {
            if (captureManager != null)
                captureManager.OnPhotoCaptured += HandlePhotoCaptured;
            if (droneController != null)
            {
                droneController.OnBatteryLow      += HandleBatteryLow;
                droneController.OnMaxRangeReached  += HandleMaxRange;
            }
        }

        private void OnDisable()
        {
            if (captureManager != null)
                captureManager.OnPhotoCaptured -= HandlePhotoCaptured;
            if (droneController != null)
            {
                droneController.OnBatteryLow      -= HandleBatteryLow;
                droneController.OnMaxRangeReached  -= HandleMaxRange;
            }
        }

        private void Update()
        {
            if (!_isVisible) return;
            UpdateBatteryIndicator();
            HandlePinchToZoom();
            HandleTapToFocus();
        }
        #endregion

        #region Public API
        /// <summary>Shows the photo mode HUD and hides the main game HUD.</summary>
        public void Show()
        {
            _isVisible = true;
            gameObject.SetActive(true);
        }

        /// <summary>Hides the photo mode HUD and restores the main game HUD.</summary>
        public void Hide()
        {
            _isVisible = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Activates or deactivates the rule-of-thirds grid overlay.
        /// </summary>
        /// <param name="show">True to show.</param>
        public void SetGridVisible(bool show)
        {
            if (gridOverlay != null) gridOverlay.SetActive(show);
            if (cameraController != null) cameraController.Settings.enableGrid = show;
        }

        /// <summary>Activates or deactivates the horizon level overlay.</summary>
        /// <param name="show">True to show.</param>
        public void SetLevelVisible(bool show)
        {
            if (levelOverlay != null) levelOverlay.SetActive(show);
            if (cameraController != null) cameraController.Settings.enableLevel = show;
        }

        /// <summary>Activates or deactivates the live histogram overlay.</summary>
        /// <param name="show">True to show.</param>
        public void SetHistogramVisible(bool show)
        {
            if (histogramOverlay != null) histogramOverlay.SetActive(show);
            if (cameraController != null) cameraController.Settings.enableHistogram = show;
        }

        /// <summary>Activates or deactivates the zebra overexposure overlay.</summary>
        /// <param name="show">True to show.</param>
        public void SetZebraVisible(bool show)
        {
            if (zebraOverlay != null) zebraOverlay.SetActive(show);
            if (cameraController != null) cameraController.Settings.enableZebra = show;
        }

        /// <summary>
        /// Called by the shutter button in the UI.  Triggers a photo capture.
        /// </summary>
        public void OnShutterButtonPressed()
        {
            captureManager?.CapturePhoto();
            PlayShutterAnimation();
            OnShutterPressed?.Invoke();
        }

        /// <summary>
        /// Called by the gallery shortcut button in the UI.
        /// </summary>
        public void OnGalleryButtonPressed()
        {
            OnGalleryShortcutPressed?.Invoke();
        }

        /// <summary>
        /// Sets the active drone mode from a UI mode-selector button.
        /// </summary>
        /// <param name="modeIndex">Integer value of <see cref="DroneMode"/>.</param>
        public void OnDroneModeSelected(int modeIndex)
        {
            droneController?.SetMode((DroneMode)modeIndex);
        }
        #endregion

        #region Event handlers
        private void HandlePhotoCaptured(PhotoMetadata meta)
        {
            ShowReviewPanel(meta);
        }

        private void HandleBatteryLow()
        {
            Debug.Log("[PhotoModeUI] Drone battery low.");
        }

        private void HandleMaxRange()
        {
            Debug.Log("[PhotoModeUI] Drone max range reached.");
        }
        #endregion

        #region Private helpers
        private void UpdateBatteryIndicator()
        {
            if (batteryFill == null || droneController == null) return;
            batteryFill.fillAmount = droneController.BatteryNormalized;
            batteryFill.color      = droneController.BatteryNormalized < 0.15f ? Color.red : Color.green;
        }

        private void HandlePinchToZoom()
        {
            if (Input.touchCount != 2) { _isPinching = false; return; }

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float dist = Vector2.Distance(t0.position, t1.position);

            if (!_isPinching) { _pinchLastDist = dist; _isPinching = true; return; }

            float delta = dist - _pinchLastDist;
            _pinchLastDist = dist;

            if (cameraController != null)
                cameraController.SetFOV(cameraController.Settings.fieldOfView - delta * PinchZoomSensitivity);
        }

        private void HandleTapToFocus()
        {
            if (Input.touchCount != 1) return;
            Touch t = Input.GetTouch(0);
            if (t.phase != TouchPhase.Began) return;
            cameraController?.SetFocus(t.position);
        }

        private void PlayShutterAnimation()
        {
            if (shutterFlash == null) return;
            shutterFlash.gameObject.SetActive(true);
            StartCoroutine(FadeFlash());
        }

        private System.Collections.IEnumerator FadeFlash()
        {
            Color c = Color.white;
            shutterFlash.color = c;
            float t = 0f;
            while (t < ShutterFlashDuration)
            {
                t += Time.deltaTime;
                c.a = 1f - t / ShutterFlashDuration;
                shutterFlash.color = c;
                yield return null;
            }
            shutterFlash.gameObject.SetActive(false);
        }

        private void ShowReviewPanel(PhotoMetadata meta)
        {
            if (reviewPanel != null) reviewPanel.SetActive(true);
        }
        #endregion
    }
}
