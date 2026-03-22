using System;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Localization;

namespace SWEF.TimeOfDay
{
    /// <summary>
    /// HUD and expanded-panel UI for the Dynamic Time-of-Day system.
    /// <para>
    /// Displays a compact clock widget (always visible) and an optional expanded panel
    /// with sunrise/sunset times, moon phase, day-length bar, and time-lapse controls.
    /// All text is localized via <see cref="LocalizationManager"/>.
    /// </para>
    /// </summary>
    public class TimeOfDayUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Mini Clock Widget")]
        [Tooltip("Text element showing the current time.")]
        [SerializeField] private Text clockText;

        [Tooltip("Text element showing the day phase name (localized).")]
        [SerializeField] private Text dayPhaseText;

        [Tooltip("Text element showing the current season.")]
        [SerializeField] private Text seasonText;

        [Tooltip("Icon image that switches between a sun and moon sprite.")]
        [SerializeField] private Image sunMoonIcon;

        [Tooltip("Sun sprite shown during daytime.")]
        [SerializeField] private Sprite sunSprite;

        [Tooltip("Moon sprite shown during nighttime.")]
        [SerializeField] private Sprite moonSprite;

        [Tooltip("Image shown as a 'Golden Hour!' badge.")]
        [SerializeField] private GameObject goldenHourBadge;

        [Header("Expanded Panel")]
        [Tooltip("Root GameObject of the expanded panel (toggled on/off).")]
        [SerializeField] private GameObject expandedPanel;

        [Tooltip("Text showing today's sunrise time.")]
        [SerializeField] private Text sunriseText;

        [Tooltip("Text showing today's sunset time.")]
        [SerializeField] private Text sunsetText;

        [Tooltip("Text showing today's day length.")]
        [SerializeField] private Text dayLengthText;

        [Tooltip("Text showing moon phase name.")]
        [SerializeField] private Text moonPhaseText;

        [Tooltip("Slider used as a day-length bar graph (0–24 h mapped to 0–1).")]
        [SerializeField] private Slider dayLengthBar;

        [Header("Time-lapse Controls")]
        [Tooltip("Button that increases time scale.")]
        [SerializeField] private Button speedUpButton;

        [Tooltip("Button that decreases time scale.")]
        [SerializeField] private Button slowDownButton;

        [Tooltip("Button that pauses/resumes time.")]
        [SerializeField] private Button pauseButton;

        [Tooltip("Text label on the pause/resume button.")]
        [SerializeField] private Text pauseButtonLabel;

        [Tooltip("Text showing the current time scale.")]
        [SerializeField] private Text timeScaleText;

        [Header("Photo Mode")]
        [Tooltip("Time scrubber slider used in photo mode (0–24 h mapped to 0–1).")]
        [SerializeField] private Slider photoTimeScrubber;

        [Tooltip("Root of the photo-mode time controls.")]
        [SerializeField] private GameObject photoModeControls;

        [Header("Settings")]
        [Tooltip("Display time in 24-hour format when true, 12-hour when false.")]
        [SerializeField] private bool use24HourFormat = true;

        [Tooltip("Multiplier steps for speed-up / slow-down buttons.")]
        [SerializeField] private float timeScaleStep = 2f;

        [Header("References (auto-found if null)")]
        [SerializeField] private TimeOfDayManager timeOfDayManager;
        [SerializeField] private LocalizationManager localizationManager;

        // ── State ─────────────────────────────────────────────────────────────────
        private bool _panelOpen;
        private bool _inPhotoMode;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (timeOfDayManager == null)
                timeOfDayManager = FindFirstObjectByType<TimeOfDayManager>();

            if (localizationManager == null)
                localizationManager = FindFirstObjectByType<LocalizationManager>();
        }

        private void OnEnable()
        {
            if (speedUpButton  != null) speedUpButton.onClick.AddListener(OnSpeedUp);
            if (slowDownButton != null) slowDownButton.onClick.AddListener(OnSlowDown);
            if (pauseButton    != null) pauseButton.onClick.AddListener(OnTogglePause);

            if (photoTimeScrubber != null)
                photoTimeScrubber.onValueChanged.AddListener(OnPhotoScrubberChanged);
        }

        private void OnDisable()
        {
            if (speedUpButton  != null) speedUpButton.onClick.RemoveListener(OnSpeedUp);
            if (slowDownButton != null) slowDownButton.onClick.RemoveListener(OnSlowDown);
            if (pauseButton    != null) pauseButton.onClick.RemoveListener(OnTogglePause);

            if (photoTimeScrubber != null)
                photoTimeScrubber.onValueChanged.RemoveListener(OnPhotoScrubberChanged);
        }

        private void Update()
        {
            if (timeOfDayManager == null) return;
            RefreshMiniClock();
            if (_panelOpen) RefreshExpandedPanel();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Toggles the expanded time panel on/off.</summary>
        public void ToggleExpandedPanel()
        {
            _panelOpen = !_panelOpen;
            if (expandedPanel != null) expandedPanel.SetActive(_panelOpen);
        }

        /// <summary>Activates or deactivates photo-mode time controls.</summary>
        public void SetPhotoMode(bool active)
        {
            _inPhotoMode = active;
            if (photoModeControls != null) photoModeControls.SetActive(active);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void RefreshMiniClock()
        {
            float   hour    = timeOfDayManager.CurrentHour;
            DayPhase phase  = timeOfDayManager.CurrentDayPhase;
            Season  season  = timeOfDayManager.CurrentSeason;
            bool    isDay   = timeOfDayManager.GetSunMoonState()?.isDaytime ?? true;

            // Clock text
            if (clockText != null)
                clockText.text = FormatHour(hour);

            // Day phase label (localized)
            if (dayPhaseText != null)
                dayPhaseText.text = LocalizePhase(phase);

            // Season label
            if (seasonText != null)
                seasonText.text = LocalizeSeason(season);

            // Sun/moon icon
            if (sunMoonIcon != null)
            {
                sunMoonIcon.sprite = isDay ? sunSprite : moonSprite;
            }

            // Golden hour badge
            if (goldenHourBadge != null)
                goldenHourBadge.SetActive(phase == DayPhase.GoldenHour);
        }

        private void RefreshExpandedPanel()
        {
            SunMoonState state = timeOfDayManager.GetSunMoonState();
            if (state == null) return;

            if (sunriseText  != null) sunriseText.text  = Localize("tod_sunrise") + ": " + FormatHour(state.sunriseTime);
            if (sunsetText   != null) sunsetText.text   = Localize("tod_sunset")  + ": " + FormatHour(state.sunsetTime);
            if (dayLengthText!= null) dayLengthText.text= Localize("tod_day_length") + ": " + $"{state.dayLengthHours:F1}h";
            if (moonPhaseText != null) moonPhaseText.text= Localize("tod_moon") + ": " + LocalizeMoonPhase(state.moonPhase);

            if (dayLengthBar != null)
                dayLengthBar.value = Mathf.Clamp01(state.dayLengthHours / 24f);

            // Pause button label
            if (pauseButtonLabel != null)
                pauseButtonLabel.text = timeOfDayManager.IsPaused ? Localize("tod_resume") : Localize("tod_pause");

            // Time scale display
            // Note: we query the internal config via a workaround (accessing timeOfDayManager)
            if (timeScaleText != null)
                timeScaleText.text = $"×{GetTimeScale():F1}";
        }

        private void OnSpeedUp()
        {
            if (timeOfDayManager == null) return;
            float current = GetTimeScale();
            timeOfDayManager.SetTimeScale(Mathf.Clamp(current * timeScaleStep, 0.25f, 3600f));
        }

        private void OnSlowDown()
        {
            if (timeOfDayManager == null) return;
            float current = GetTimeScale();
            timeOfDayManager.SetTimeScale(Mathf.Clamp(current / timeScaleStep, 0.25f, 3600f));
        }

        private void OnTogglePause()
        {
            if (timeOfDayManager == null) return;
            if (timeOfDayManager.IsPaused) timeOfDayManager.ResumeTime();
            else                           timeOfDayManager.PauseTime();
        }

        private void OnPhotoScrubberChanged(float value)
        {
            if (!_inPhotoMode || timeOfDayManager == null) return;
            timeOfDayManager.SetTime(value * 24f);
        }

        private string FormatHour(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            int h = (int)hour;
            int m = (int)((hour - h) * 60f);
            if (use24HourFormat)
                return $"{h:D2}:{m:D2}";
            string ampm = h >= 12 ? "PM" : "AM";
            int h12 = h % 12; if (h12 == 0) h12 = 12;
            return $"{h12}:{m:D2} {ampm}";
        }

        private string LocalizePhase(DayPhase phase)
        {
            string key = "tod_phase_" + phase.ToString().ToLower();
            return Localize(key);
        }

        private string LocalizeSeason(Season s)
        {
            string key = "tod_season_" + s.ToString().ToLower();
            return Localize(key);
        }

        private string LocalizeMoonPhase(MoonPhase mp)
        {
            string key = "tod_moon_" + mp.ToString().ToLower();
            return Localize(key);
        }

        private string Localize(string key)
        {
            if (localizationManager != null)
                return localizationManager.GetText(key);
            return key; // fallback: return key itself
        }

        private float GetTimeScale()
        {
            if (timeOfDayManager != null)
                return timeOfDayManager.CurrentTimeScale;
            return 1f;
        }
    }
}
