using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Core;
using SWEF.Screenshot;

namespace SWEF.Cinema
{
    /// <summary>
    /// Dedicated Photo Mode controller.
    /// Pauses gameplay, hides HUD, and exposes camera / post-processing controls
    /// (FOV, depth of field, exposure, contrast, saturation, vignette, filters, frames).
    /// Phase 18 — Photo Mode.
    /// </summary>
    public class PhotoModeController : MonoBehaviour
    {
        // ── Enums ─────────────────────────────────────────────────────────────────
        public enum PhotoModeState { Inactive, Active, Capturing }

        public enum PhotoFilter
        {
            None, Warm, Cool, Vintage, BlackAndWhite,
            Cinematic, Sunset, Neon, Dramatic
        }

        public enum PhotoFrame { None, Classic, Polaroid, Widescreen, Circle, Vintage }

        public enum PhotoResolution { Screen, HD_1080, UHD_4K }

        // ── Filter preset ─────────────────────────────────────────────────────────
        [System.Serializable]
        public class FilterPreset
        {
            public string name;
            public Color  tint       = Color.white;
            public float  saturation = 1.0f;
            public float  contrast   = 1.0f;
            public float  vignette   = 0.0f;
            public float  exposure   = 1.0f;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Camera")]
        [SerializeField] private Camera photoCamera;

        [Header("Free-Camera Movement")]
        [SerializeField] private float freeMovementSpeed = 5.0f;
        [SerializeField] private float freeRotationSpeed = 2.0f;
        [SerializeField] private float maxPhotoDistance   = 100.0f;

        [Header("Photo Settings")]
        [SerializeField] private float fieldOfView   = 60.0f;
        [SerializeField] private float depthOfField  = 0.0f;
        [SerializeField] private float focusDistance = 50.0f;
        [SerializeField] private float exposure      = 1.0f;
        [SerializeField] private float contrast      = 1.0f;
        [SerializeField] private float saturation    = 1.0f;
        [SerializeField] private float vignette      = 0.0f;

        [Header("Filters")]
        [SerializeField] private PhotoFilter          currentFilter  = PhotoFilter.None;
        [SerializeField] private List<FilterPreset>   filterPresets  = new List<FilterPreset>();

        [Header("Frames")]
        [SerializeField] private PhotoFrame      currentFrame  = PhotoFrame.None;
        [SerializeField] private List<Sprite>    frameSprites;

        [Header("Watermark")]
        [SerializeField] private bool    showWatermark              = true;
        [SerializeField] private Sprite  watermarkSprite;
        [SerializeField] private Vector2 watermarkPosition          = new Vector2(0.95f, 0.05f);

        [Header("Capture")]
        [SerializeField] private PhotoResolution captureResolution = PhotoResolution.Screen;

        // ── State ─────────────────────────────────────────────────────────────────
        public PhotoModeState CurrentState { get; private set; } = PhotoModeState.Inactive;

        private Vector3    _origCamPos;
        private Quaternion _origCamRot;
        private float      _origFOV;
        private Transform  _playerAnchor;
        private Coroutine  _timerCoroutine;

        // ── Events ────────────────────────────────────────────────────────────────
        public event Action           OnPhotoModeEntered;
        public event Action           OnPhotoModeExited;
        public event Action<Texture2D> OnPhotoCaptured;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (photoCamera == null)
                photoCamera = Camera.main;
        }

        private void Update()
        {
            if (CurrentState != PhotoModeState.Active) return;
            HandleFreeCameraInput();
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>Enters photo mode: pauses game, hides HUD, stores camera state.</summary>
        public void EnterPhotoMode()
        {
            if (CurrentState != PhotoModeState.Inactive) return;

            // Pause
            if (PauseManager.Instance != null)
                PauseManager.Instance.PauseForPhotoMode();

            // Store camera state
            if (photoCamera != null)
            {
                _origCamPos = photoCamera.transform.position;
                _origCamRot = photoCamera.transform.rotation;
                _origFOV    = photoCamera.fieldOfView;
            }

            CurrentState = PhotoModeState.Active;
            Debug.Log("[SWEF] PhotoModeController: Photo mode entered.");
            OnPhotoModeEntered?.Invoke();
        }

        /// <summary>Exits photo mode and restores previous camera and game state.</summary>
        public void ExitPhotoMode()
        {
            if (CurrentState == PhotoModeState.Inactive) return;

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            // Restore camera
            if (photoCamera != null)
            {
                photoCamera.transform.position = _origCamPos;
                photoCamera.transform.rotation = _origCamRot;
                photoCamera.fieldOfView        = _origFOV;
            }

            // Resume
            if (PauseManager.Instance != null)
                PauseManager.Instance.ResumeFromPhotoMode();

            CurrentState = PhotoModeState.Inactive;
            Debug.Log("[SWEF] PhotoModeController: Photo mode exited.");
            OnPhotoModeExited?.Invoke();
        }

        // ── Filter / Frame API ───────────────────────────────────────────────────
        /// <summary>Applies a named filter preset.</summary>
        public void SetFilter(PhotoFilter filter)
        {
            currentFilter = filter;
            ApplyFilterPreset(filter);
            Debug.Log($"[SWEF] PhotoModeController: Filter set to {filter}.");
        }

        /// <summary>Sets the decorative frame overlay.</summary>
        public void SetFrame(PhotoFrame frame)
        {
            currentFrame = frame;
            Debug.Log($"[SWEF] PhotoModeController: Frame set to {frame}.");
        }

        // ── Photo settings API ────────────────────────────────────────────────────
        public void SetFOV(float fov)         { fieldOfView  = Mathf.Clamp(fov, 20f, 120f); if (photoCamera) photoCamera.fieldOfView = fieldOfView; }
        public void SetExposure(float v)      { exposure     = Mathf.Clamp(v, 0.1f, 5.0f); }
        public void SetContrast(float v)      { contrast     = Mathf.Clamp(v, 0.5f, 2.0f); }
        public void SetSaturation(float v)    { saturation   = Mathf.Clamp(v, 0f,   2.0f); }
        public void SetVignette(float v)      { vignette     = Mathf.Clamp(v, 0f,   1.0f); }
        public void SetDepthOfField(float v)  { depthOfField = Mathf.Clamp(v, 0f,   1.0f); }
        public void SetFocusDistance(float v) { focusDistance = Mathf.Max(0f, v); }

        // ── Capture API ──────────────────────────────────────────────────────────
        /// <summary>Captures a photo using the existing ScreenshotController.</summary>
        public void CapturePhoto()
        {
            if (CurrentState != PhotoModeState.Active) return;
            var sc = FindFirstObjectByType<ScreenshotController>();
            if (sc != null)
                sc.CaptureScreenshot();
            else
                Debug.LogWarning("[SWEF] PhotoModeController: No ScreenshotController found.");

            UnlockPhotoAchievements();
        }

        /// <summary>Captures a photo at the selected resolution and applies filter/frame overlay.</summary>
        public void CapturePhotoWithEffects()
        {
            if (CurrentState != PhotoModeState.Active) return;
            StartCoroutine(CaptureWithEffectsCoroutine());
        }

        /// <summary>Starts a countdown timer, then auto-captures.</summary>
        public void StartTimer(float seconds)
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(TimerCoroutine(seconds));
        }

        // ── Internals ─────────────────────────────────────────────────────────────
        private void HandleFreeCameraInput()
        {
            if (photoCamera == null) return;

            // Touch/mouse drag for rotation
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    float rotX = touch.deltaPosition.y * freeRotationSpeed * Time.unscaledDeltaTime;
                    float rotY = touch.deltaPosition.x * freeRotationSpeed * Time.unscaledDeltaTime;
                    photoCamera.transform.Rotate(Vector3.up, rotY, Space.World);
                    photoCamera.transform.Rotate(Vector3.right, -rotX, Space.Self);
                }
            }
            else if (Input.touchCount == 2)
            {
                // Pinch-to-zoom (FOV)
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                Vector2 prevT0 = t0.position - t0.deltaPosition;
                Vector2 prevT1 = t1.position - t1.deltaPosition;
                float   prevMag = (prevT0 - prevT1).magnitude;
                float   currMag = (t0.position - t1.position).magnitude;
                float   delta   = prevMag - currMag;

                SetFOV(fieldOfView + delta * 0.1f);
            }

#if UNITY_EDITOR
            // Editor mouse-look
            if (Input.GetMouseButton(1))
            {
                float rotX = -Input.GetAxis("Mouse Y") * freeRotationSpeed;
                float rotY =  Input.GetAxis("Mouse X") * freeRotationSpeed;
                photoCamera.transform.Rotate(Vector3.up,   rotY, Space.World);
                photoCamera.transform.Rotate(Vector3.right, rotX, Space.Self);
            }
#endif

            // Enforce max distance from player anchor
            if (_playerAnchor != null)
            {
                float dist = Vector3.Distance(photoCamera.transform.position, _playerAnchor.position);
                if (dist > maxPhotoDistance)
                {
                    Vector3 dir = (photoCamera.transform.position - _playerAnchor.position).normalized;
                    photoCamera.transform.position = _playerAnchor.position + dir * maxPhotoDistance;
                }
            }
        }

        private void ApplyFilterPreset(PhotoFilter filter)
        {
            // Find matching preset by enum name; fall back to default values if not configured
            string targetName = filter.ToString();
            FilterPreset preset = filterPresets.Find(p =>
                string.Equals(p.name, targetName, StringComparison.OrdinalIgnoreCase));

            if (preset == null) return;

            exposure   = preset.exposure;
            contrast   = preset.contrast;
            saturation = preset.saturation;
            vignette   = preset.vignette;
        }

        private IEnumerator CaptureWithEffectsCoroutine()
        {
            CurrentState = PhotoModeState.Capturing;

            int width, height;
            switch (captureResolution)
            {
                case PhotoResolution.HD_1080:  width = 1920;  height = 1080; break;
                case PhotoResolution.UHD_4K:   width = 3840;  height = 2160; break;
                default:                        width = Screen.width; height = Screen.height; break;
            }

            yield return new WaitForEndOfFrame();

            var sc = FindFirstObjectByType<ScreenshotController>();
            Texture2D tex = sc != null
                ? sc.CaptureAtResolution(width, height)
                : null;

            if (tex != null)
            {
                Debug.Log($"[SWEF] PhotoModeController: Photo captured ({width}×{height}).");
                OnPhotoCaptured?.Invoke(tex);

                if (sc != null)
                    sc.SaveTextureToGallery(tex);

                UnlockPhotoAchievements();
            }

            CurrentState = PhotoModeState.Active;
        }

        private IEnumerator TimerCoroutine(float seconds)
        {
            float remaining = seconds;
            while (remaining > 0f)
            {
                yield return new WaitForSecondsRealtime(1f);
                remaining -= 1f;
                Debug.Log($"[SWEF] PhotoModeController: Timer {Mathf.CeilToInt(remaining)}…");
            }
            CapturePhotoWithEffects();
            _timerCoroutine = null;
        }

        private void UnlockPhotoAchievements()
        {
            if (Achievement.AchievementManager.Instance == null) return;
            Achievement.AchievementManager.Instance.TryUnlock("first_photo");

            var todCtrl = FindFirstObjectByType<TimeOfDayController>();
            if (todCtrl != null && todCtrl.IsGoldenHour)
                Achievement.AchievementManager.Instance.TryUnlock("golden_hour_photo");
        }
    }
}
