using UnityEngine;
using UnityEngine.UI;
using SWEF.Cinema;

namespace SWEF.UI
{
    /// <summary>
    /// HUD panel for time-of-day controls visible outside photo mode.
    /// Provides quick-set buttons (sunrise/noon/sunset/midnight), a time slider,
    /// a time-speed slider, and a collapsible compact mode.
    /// Phase 18 — Time-of-Day UI.
    /// </summary>
    public class TimeOfDayUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject timePanel;

        [Header("Controller")]
        [SerializeField] private TimeOfDayController timeController;

        [Header("Controls")]
        [SerializeField] private Slider timeSlider;
        [SerializeField] private Text   timeText;
        [SerializeField] private Text   periodText;
        [SerializeField] private Toggle realTimeToggle;

        [Header("Quick-Set Buttons")]
        [SerializeField] private Button sunriseButton;
        [SerializeField] private Button noonButton;
        [SerializeField] private Button sunsetButton;
        [SerializeField] private Button midnightButton;

        [Header("Speed")]
        [SerializeField] private Slider speedSlider;

        [Header("Toggle")]
        [SerializeField] private Button togglePanelButton;

        // ── State ─────────────────────────────────────────────────────────────────
        private bool _panelVisible = true;
        private bool _ignoreCallbacks;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (timeController == null)
                timeController = FindFirstObjectByType<TimeOfDayController>();

            // Time slider (0–24)
            if (timeSlider != null)
            {
                timeSlider.minValue = 0f;
                timeSlider.maxValue = 24f;
                timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
            }

            // Speed slider (0–10)
            if (speedSlider != null)
            {
                speedSlider.minValue = 0f;
                speedSlider.maxValue = 10f;
                speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
            }

            // Real-time toggle
            if (realTimeToggle != null)
                realTimeToggle.onValueChanged.AddListener(v => timeController?.ToggleRealWorldTime(v));

            // Quick-set buttons
            if (sunriseButton  != null) sunriseButton.onClick.AddListener(()  => SetTime(6f));
            if (noonButton     != null) noonButton.onClick.AddListener(()     => SetTime(12f));
            if (sunsetButton   != null) sunsetButton.onClick.AddListener(()   => SetTime(18f));
            if (midnightButton != null) midnightButton.onClick.AddListener(() => SetTime(0f));

            // Toggle panel
            if (togglePanelButton != null) togglePanelButton.onClick.AddListener(TogglePanel);
        }

        private void OnEnable()
        {
            if (timeController != null)
                timeController.OnTimeChanged += OnTimeChanged;
        }

        private void OnDisable()
        {
            if (timeController != null)
                timeController.OnTimeChanged -= OnTimeChanged;
        }

        private void Start()
        {
            RefreshUI();
        }

        // ── Public ────────────────────────────────────────────────────────────────
        /// <summary>Shows or hides the full panel, keeping only the compact toggle button visible.</summary>
        public void TogglePanel()
        {
            _panelVisible = !_panelVisible;
            if (timePanel != null) timePanel.SetActive(_panelVisible);
        }

        // ── Internals ────────────────────────────────────────────────────────────
        private void SetTime(float hour)
        {
            if (timeController == null) return;
            timeController.SetTimeOfDay(hour);
            RefreshUI();
        }

        private void OnTimeSliderChanged(float value)
        {
            if (_ignoreCallbacks || timeController == null) return;
            timeController.SetTimeOfDay(value);
            UpdateLabels();
        }

        private void OnSpeedSliderChanged(float value)
        {
            if (_ignoreCallbacks || timeController == null) return;
            timeController.SetTimeSpeed(value);
        }

        private void OnTimeChanged(float hour)
        {
            _ignoreCallbacks = true;
            if (timeSlider != null)
                timeSlider.SetValueWithoutNotify(hour);
            _ignoreCallbacks = false;
            UpdateLabels();
        }

        private void RefreshUI()
        {
            if (timeController == null) return;

            _ignoreCallbacks = true;
            if (timeSlider != null) timeSlider.SetValueWithoutNotify(timeController.GetTimeOfDay());
            _ignoreCallbacks = false;

            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (timeController == null) return;

            if (timeText != null)
                timeText.text = timeController.GetTimeString();

            if (periodText != null)
            {
                if (timeController.IsGoldenHour)
                    periodText.text = "🌅 Golden Hour";
                else if (timeController.IsNight)
                    periodText.text = "🌙 Night";
                else if (timeController.IsDaytime)
                    periodText.text = "☀️ Daytime";
                else
                    periodText.text = "🌇 Dusk/Dawn";
            }
        }
    }
}
