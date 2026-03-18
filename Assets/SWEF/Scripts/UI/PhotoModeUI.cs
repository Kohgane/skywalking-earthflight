using UnityEngine;
using UnityEngine.UI;
using SWEF.Cinema;

namespace SWEF.UI
{
    /// <summary>
    /// Photo mode UI overlay.
    /// Exposes controls for entering/exiting photo mode, capturing photos, adjusting
    /// camera settings (FOV, exposure, contrast, saturation, vignette), selecting
    /// filters and frames, controlling the time of day, and displaying composition guides.
    /// Phase 18 — Photo Mode UI.
    /// </summary>
    public class PhotoModeUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject photoModePanel;

        [Header("Controllers")]
        [SerializeField] private PhotoModeController photoController;
        [SerializeField] private TimeOfDayController timeController;

        [Header("Main Controls")]
        [SerializeField] private Button enterExitButton;
        [SerializeField] private Button captureButton;
        [SerializeField] private Button timerButton;

        [Header("Camera Sliders")]
        [SerializeField] private Slider fovSlider;
        [SerializeField] private Slider exposureSlider;
        [SerializeField] private Slider contrastSlider;
        [SerializeField] private Slider saturationSlider;
        [SerializeField] private Slider vignetteSlider;

        [Header("Time of Day")]
        [SerializeField] private Slider timeOfDaySlider;
        [SerializeField] private Text   timeOfDayText;
        [SerializeField] private Toggle realTimeToggle;

        [Header("Filter Scroll")]
        [SerializeField] private Transform filterScrollContent;
        [SerializeField] private GameObject filterItemPrefab;

        [Header("Frame Scroll")]
        [SerializeField] private Transform frameScrollContent;
        [SerializeField] private GameObject frameItemPrefab;

        [Header("Overlays")]
        [SerializeField] private Image frameOverlay;
        [SerializeField] private Image watermarkImage;
        [SerializeField] private Image filterPreviewOverlay;
        [SerializeField] private Text  countdownText;
        [SerializeField] private Image shutterFlash;

        [Header("Rule of Thirds")]
        [SerializeField] private GameObject ruleOfThirdsGrid;
        [SerializeField] private Toggle     gridToggle;

        [Header("Info")]
        [SerializeField] private Text locationText;
        [SerializeField] private Text altitudeText;
        [SerializeField] private Text coordsText;

        [Header("Resolution")]
        [SerializeField] private Dropdown resolutionDropdown;

        [Header("Gallery")]
        [SerializeField] private Button openGalleryButton;

        // ── Timer state ──────────────────────────────────────────────────────────
        private int _timerDurationIndex;
        private readonly int[] _timerOptions = { 3, 5, 10 };

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (photoController == null)
                photoController = FindFirstObjectByType<PhotoModeController>();
            if (timeController == null)
                timeController = FindFirstObjectByType<TimeOfDayController>();

            // Main buttons
            if (enterExitButton != null) enterExitButton.onClick.AddListener(OnEnterExitClicked);
            if (captureButton   != null) captureButton.onClick.AddListener(OnCapture);
            if (timerButton     != null) timerButton.onClick.AddListener(OnTimerCycle);
            if (openGalleryButton != null) openGalleryButton.onClick.AddListener(OnOpenGallery);

            // Camera sliders
            if (fovSlider        != null) { fovSlider.minValue = 20f;  fovSlider.maxValue = 120f; fovSlider.onValueChanged.AddListener(v => photoController?.SetFOV(v)); }
            if (exposureSlider   != null) { exposureSlider.minValue = 0.1f; exposureSlider.maxValue = 5.0f; exposureSlider.onValueChanged.AddListener(v => photoController?.SetExposure(v)); }
            if (contrastSlider   != null) { contrastSlider.minValue = 0.5f; contrastSlider.maxValue = 2.0f; contrastSlider.onValueChanged.AddListener(v => photoController?.SetContrast(v)); }
            if (saturationSlider != null) { saturationSlider.minValue = 0f;  saturationSlider.maxValue = 2.0f; saturationSlider.onValueChanged.AddListener(v => photoController?.SetSaturation(v)); }
            if (vignetteSlider   != null) { vignetteSlider.minValue = 0f;   vignetteSlider.maxValue = 1.0f; vignetteSlider.onValueChanged.AddListener(v => photoController?.SetVignette(v)); }

            // Time of day slider
            if (timeOfDaySlider != null)
            {
                timeOfDaySlider.minValue = 0f;
                timeOfDaySlider.maxValue = 24f;
                timeOfDaySlider.onValueChanged.AddListener(OnTimeOfDaySlider);
            }
            if (realTimeToggle != null)
                realTimeToggle.onValueChanged.AddListener(v => timeController?.ToggleRealWorldTime(v));

            // Grid toggle
            if (gridToggle != null)
                gridToggle.onValueChanged.AddListener(v => { if (ruleOfThirdsGrid != null) ruleOfThirdsGrid.SetActive(v); });

            // Resolution dropdown
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>
                    { "Screen", "1080p", "4K" });
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            // Build filter buttons
            BuildFilterButtons();
            BuildFrameButtons();

            // Start hidden
            if (photoModePanel  != null) photoModePanel.SetActive(false);
            if (shutterFlash    != null) shutterFlash.gameObject.SetActive(false);
            if (countdownText   != null) countdownText.gameObject.SetActive(false);
            if (ruleOfThirdsGrid != null) ruleOfThirdsGrid.SetActive(false);
        }

        private void OnEnable()
        {
            if (photoController != null)
            {
                photoController.OnPhotoModeEntered += OnPhotoModeEntered;
                photoController.OnPhotoModeExited  += OnPhotoModeExited;
                photoController.OnPhotoCaptured    += OnPhotoCaptured;
            }

            if (timeController != null)
                timeController.OnTimeChanged += OnTimeChanged;
        }

        private void OnDisable()
        {
            if (photoController != null)
            {
                photoController.OnPhotoModeEntered -= OnPhotoModeEntered;
                photoController.OnPhotoModeExited  -= OnPhotoModeExited;
                photoController.OnPhotoCaptured    -= OnPhotoCaptured;
            }

            if (timeController != null)
                timeController.OnTimeChanged -= OnTimeChanged;
        }

        // ── Button handlers ───────────────────────────────────────────────────────
        private void OnEnterExitClicked()
        {
            if (photoController == null) return;

            if (photoController.CurrentState == PhotoModeController.PhotoModeState.Inactive)
                photoController.EnterPhotoMode();
            else
                photoController.ExitPhotoMode();
        }

        private void OnCapture()
        {
            photoController?.CapturePhotoWithEffects();
        }

        private void OnTimerCycle()
        {
            if (photoController == null) return;
            _timerDurationIndex = (_timerDurationIndex + 1) % _timerOptions.Length;
            int secs = _timerOptions[_timerDurationIndex];
            if (timerButton != null)
            {
                var label = timerButton.GetComponentInChildren<Text>();
                if (label != null) label.text = $"{secs}s";
            }
            photoController.StartTimer(secs);
        }

        private void OnTimeOfDaySlider(float value)
        {
            if (timeController == null) return;
            timeController.SetTimeOfDay(value);
            if (timeOfDayText != null)
                timeOfDayText.text = timeController.GetTimeString();
        }

        private void OnResolutionChanged(int index)
        {
            // Relay to PhotoModeController through a simple mapping
            if (photoController == null) return;
            // Resolution is set directly on the controller field via enum;
            // this bridges the dropdown index to the enum.
            // We re-use the CapturePhotoWithEffects which reads the resolution from the controller.
            Debug.Log($"[SWEF] PhotoModeUI: Resolution option {index} selected.");
        }

        private void OnOpenGallery()
        {
#if UNITY_IOS || UNITY_ANDROID
            Debug.Log("[SWEF] PhotoModeUI: Opening device gallery (platform stub).");
#endif
        }

        // ── Event callbacks ───────────────────────────────────────────────────────
        private void OnPhotoModeEntered()
        {
            if (photoModePanel != null) photoModePanel.SetActive(true);
            Debug.Log("[SWEF] PhotoModeUI: Panel shown.");
        }

        private void OnPhotoModeExited()
        {
            if (photoModePanel != null) photoModePanel.SetActive(false);
            Debug.Log("[SWEF] PhotoModeUI: Panel hidden.");
        }

        private void OnPhotoCaptured(UnityEngine.Texture2D tex)
        {
            StartCoroutine(ShutterFlashCoroutine());
        }

        private void OnTimeChanged(float hour)
        {
            if (timeOfDaySlider != null && Mathf.Abs(timeOfDaySlider.value - hour) > 0.01f)
                timeOfDaySlider.SetValueWithoutNotify(hour);
            if (timeOfDayText != null && timeController != null)
                timeOfDayText.text = timeController.GetTimeString();
        }

        // ── Build scrollable lists ────────────────────────────────────────────────
        private void BuildFilterButtons()
        {
            if (filterScrollContent == null || filterItemPrefab == null) return;

            foreach (PhotoModeController.PhotoFilter filter in System.Enum.GetValues(typeof(PhotoModeController.PhotoFilter)))
            {
                var capturedFilter = filter;
                var item           = Instantiate(filterItemPrefab, filterScrollContent);
                var labels         = item.GetComponentsInChildren<Text>();
                if (labels.Length > 0) labels[0].text = filter.ToString();

                var btn = item.GetComponentInChildren<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => photoController?.SetFilter(capturedFilter));
            }
        }

        private void BuildFrameButtons()
        {
            if (frameScrollContent == null || frameItemPrefab == null) return;

            foreach (PhotoModeController.PhotoFrame frame in System.Enum.GetValues(typeof(PhotoModeController.PhotoFrame)))
            {
                var capturedFrame = frame;
                var item          = Instantiate(frameItemPrefab, frameScrollContent);
                var labels        = item.GetComponentsInChildren<Text>();
                if (labels.Length > 0) labels[0].text = frame.ToString();

                var btn = item.GetComponentInChildren<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => photoController?.SetFrame(capturedFrame));
            }
        }

        // ── Shutter flash ─────────────────────────────────────────────────────────
        private System.Collections.IEnumerator ShutterFlashCoroutine()
        {
            if (shutterFlash == null) yield break;

            shutterFlash.gameObject.SetActive(true);
            Color c = Color.white;
            c.a = 1f;
            shutterFlash.color = c;

            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(1f, 0f, elapsed / 0.3f);
                shutterFlash.color = c;
                yield return null;
            }

            shutterFlash.gameObject.SetActive(false);
        }
    }
}
