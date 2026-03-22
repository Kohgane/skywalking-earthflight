using System;
using UnityEngine;
using SWEF.TimeOfDay;

namespace SWEF.PhotoMode
{
    /// <summary>
    /// Manages the virtual camera settings (FOV, aperture, shutter speed, ISO, white
    /// balance, and focus) and drives real-time post-processing effects such as depth
    /// of field, motion blur, film grain, and colour grading.
    /// </summary>
    public class PhotoCameraController : MonoBehaviour
    {
        #region Constants
        private const float DefaultFOV            = 60f;
        private const float DefaultAperture       = 5.6f;
        private const float DefaultShutterSpeed   = 0.004f;  // 1/250 s
        private const int   DefaultISO            = 400;
        private const int   DefaultWhiteBalance   = 5500;
        private const float DefaultFocusDistance  = 10f;
        private const float MinFOV                = 10f;
        private const float MaxFOV                = 120f;
        private const float FocusRaycastMaxDist   = 1000f;
        private const float MotionBlurBaseline    = 0.004f;  // 1/250 s → no blur
        private const float GrainBaseISO          = 400f;
        #endregion

        #region Inspector
        [Header("References (auto-found if null)")]
        [Tooltip("Camera driven by this controller. Defaults to Camera.main.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("TimeOfDayManager reference. Auto-found if null.")]
        [SerializeField] private TimeOfDayManager timeOfDayManager;

        [Header("Initial Settings")]
        [SerializeField] private CameraSettings settings = new CameraSettings();
        #endregion

        #region Public properties
        /// <summary>Current camera settings.</summary>
        public CameraSettings Settings => settings;

        /// <summary>Computed normalised exposure value (0 = middle grey).</summary>
        public float ExposureEV { get; private set; }
        #endregion

        #region Private state
        private float _currentFOV;
        private float _currentGrain;
        private float _currentMotionBlur;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (timeOfDayManager == null)
                timeOfDayManager = FindObjectOfType<TimeOfDayManager>();

            _currentFOV = settings.fieldOfView;
            ApplyAllSettings();
        }

        private void Update()
        {
            if (settings.focusMode == FocusMode.Auto)
                AutoFocus();

            UpdateExposure();
        }
        #endregion

        #region Public API
        /// <summary>
        /// Sets the camera field of view in degrees and applies it immediately.
        /// </summary>
        /// <param name="fov">Desired FOV clamped to [10, 120].</param>
        public void SetFOV(float fov)
        {
            settings.fieldOfView = Mathf.Clamp(fov, MinFOV, MaxFOV);
            ApplyFOV();
        }

        /// <summary>
        /// Sets the aperture f-stop (1.4–22) and refreshes depth-of-field.
        /// </summary>
        /// <param name="aperture">f-stop value.</param>
        public void SetAperture(float aperture)
        {
            settings.aperture = Mathf.Clamp(aperture, 1.4f, 22f);
            ApplyDepthOfField();
        }

        /// <summary>
        /// Casts a ray from <paramref name="screenPoint"/> and sets focus distance
        /// to the hit position.  Falls back to <see cref="DefaultFocusDistance"/> if
        /// nothing is hit.
        /// </summary>
        /// <param name="screenPoint">Screen-space position to focus on.</param>
        public void SetFocus(Vector3 screenPoint)
        {
            if (targetCamera == null) return;

            Ray ray = targetCamera.ScreenPointToRay(screenPoint);
            settings.focusDistance = Physics.Raycast(ray, out RaycastHit hit, FocusRaycastMaxDist)
                ? hit.distance
                : DefaultFocusDistance;

            ApplyDepthOfField();
        }

        /// <summary>
        /// Casts a ray from the screen centre and updates focus distance automatically.
        /// </summary>
        public void AutoFocus()
        {
            if (targetCamera == null) return;
            Vector3 centre = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            SetFocus(centre);
        }

        /// <summary>
        /// Resets all camera settings to factory defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            settings.fieldOfView        = DefaultFOV;
            settings.aperture           = DefaultAperture;
            settings.shutterSpeed       = DefaultShutterSpeed;
            settings.iso                = DefaultISO;
            settings.whiteBalance       = DefaultWhiteBalance;
            settings.focusDistance      = DefaultFocusDistance;
            settings.exposureCompensation = 0f;
            settings.focusMode          = FocusMode.Auto;
            settings.filter             = PhotoFilter.None;
            settings.frame              = FrameStyle.None;
            ApplyAllSettings();
        }

        /// <summary>
        /// Applies an external <see cref="CameraSettings"/> snapshot to this controller.
        /// </summary>
        /// <param name="snapshot">Settings to apply.</param>
        public void ApplySettings(CameraSettings snapshot)
        {
            settings = snapshot;
            ApplyAllSettings();
        }
        #endregion

        #region Private helpers
        private void ApplyAllSettings()
        {
            ApplyFOV();
            ApplyDepthOfField();
            ApplyMotionBlur();
            ApplyGrain();
            ApplyColorGrading();
        }

        private void ApplyFOV()
        {
            if (targetCamera != null)
                targetCamera.fieldOfView = settings.fieldOfView;
        }

        private void ApplyDepthOfField()
        {
            // Depth-of-field parameters are forwarded to the post-processing volume.
            // In a full URP integration these would write to a DepthOfField override.
            float blurStrength = 1f - Mathf.InverseLerp(1.4f, 22f, settings.aperture);
            _currentFOV = settings.fieldOfView;
            _ = blurStrength; // used by post-processing volume (stubbed)
        }

        private void ApplyMotionBlur()
        {
            // Faster shutter → less blur; slower → more.
            _currentMotionBlur = Mathf.InverseLerp(0.000125f, 30f, settings.shutterSpeed);
        }

        private void ApplyGrain()
        {
            // Higher ISO → more grain.
            _currentGrain = Mathf.InverseLerp(100f, 12800f, settings.iso);
        }

        private void ApplyColorGrading()
        {
            // White balance and exposure compensation are forwarded to a
            // colour-grading post-processing volume override (stubbed in pure C#).
        }

        private void UpdateExposure()
        {
            // EV = log2(N² / t) − log2(ISO/100) + EC
            float n  = settings.aperture;
            float t  = Mathf.Max(settings.shutterSpeed, 0.00001f);
            float ev = Mathf.Log(n * n / t, 2f) - Mathf.Log(settings.iso / 100f, 2f) + settings.exposureCompensation;
            ExposureEV = ev;
        }
        #endregion
    }
}
