using System;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Replay
{
    /// <summary>
    /// Phase 48 — UI controller for the replay timeline interface.
    /// Drives the timeline scrubber, transport controls, speed buttons,
    /// camera-angle selector, loop toggle, and recording info panel.
    /// Keyboard shortcuts: Space = play/pause, ← / → = seek ±5 s,
    /// + / - = speed up / down.
    /// </summary>
    public class ReplayTimelineUI : MonoBehaviour
    {
        #region Constants

        private const float SeekStepSec   = 5f;
        private const string TimeFormat   = "{0:00}:{1:00}.{2:00}";

        #endregion

        #region Inspector — Transport

        [Header("Transport Controls")]
        [SerializeField] private Button   playButton;
        [SerializeField] private Button   pauseButton;
        [SerializeField] private Button   stopButton;
        [SerializeField] private Slider   timelineSlider;
        [SerializeField] private Text     currentTimeText;
        [SerializeField] private Text     totalTimeText;
        [SerializeField] private Toggle   loopToggle;

        #endregion

        #region Inspector — Speed

        [Header("Speed Buttons")]
        [SerializeField] private Button speedQuarterButton;
        [SerializeField] private Button speedHalfButton;
        [SerializeField] private Button speedNormalButton;
        [SerializeField] private Button speedDoubleButton;
        [SerializeField] private Button speedQuadButton;

        #endregion

        #region Inspector — Camera

        [Header("Camera Angle")]
        [SerializeField] private Dropdown cameraAngleDropdown;

        #endregion

        #region Inspector — Info

        [Header("Recording Info")]
        [SerializeField] private Text aircraftText;
        [SerializeField] private Text dateText;
        [SerializeField] private Text routeText;
        [SerializeField] private Text durationText;
        [SerializeField] private Text pilotText;

        #endregion

        #region Inspector — Mini-Map

        [Header("Mini-Map (optional)")]
        [Tooltip("RawImage that displays the mini-map texture.")]
        [SerializeField] private RawImage miniMapImage;
        [Tooltip("Image used as the current-position indicator on the mini-map.")]
        [SerializeField] private RectTransform miniMapIndicator;

        #endregion

        #region Private State

        private FlightPlaybackController _playback;
        private ReplayCameraController   _cameraCtrl;
        private bool                     _scrubbing;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _playback   = FindFirstObjectByType<FlightPlaybackController>();
            _cameraCtrl = FindFirstObjectByType<ReplayCameraController>();
        }

        private void OnEnable()
        {
            WireButtons();
            if (_playback != null)
            {
                _playback.OnPlaybackTimeChanged   += HandleTimeChanged;
                _playback.OnPlaybackSpeedChanged  += HandleSpeedChanged;
            }
        }

        private void OnDisable()
        {
            if (_playback != null)
            {
                _playback.OnPlaybackTimeChanged   -= HandleTimeChanged;
                _playback.OnPlaybackSpeedChanged  -= HandleSpeedChanged;
            }
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
        }

        #endregion

        #region Public API

        /// <summary>Refreshes all UI elements to reflect a newly loaded recording.</summary>
        public void DisplayRecording(FlightRecording recording)
        {
            if (recording == null) return;

            if (aircraftText  != null) aircraftText.text  = recording.aircraftType;
            if (dateText      != null) dateText.text      = recording.date;
            if (routeText     != null) routeText.text     = recording.routeName;
            if (durationText  != null) durationText.text  = FormatTime(recording.duration);
            if (pilotText     != null) pilotText.text     = recording.pilotName;

            if (totalTimeText != null) totalTimeText.text = FormatTime(recording.duration);
            if (timelineSlider != null)
            {
                timelineSlider.minValue = 0f;
                timelineSlider.maxValue = 1f;
                timelineSlider.value    = 0f;
            }
        }

        #endregion

        #region Private — Button Wiring

        private void WireButtons()
        {
            BindButton(playButton,          () => _playback?.Play());
            BindButton(pauseButton,         () => _playback?.Pause());
            BindButton(stopButton,          () => _playback?.StopPlayback());
            BindButton(speedQuarterButton,  () => _playback?.SetSpeed(PlaybackSpeed.Quarter));
            BindButton(speedHalfButton,     () => _playback?.SetSpeed(PlaybackSpeed.Half));
            BindButton(speedNormalButton,   () => _playback?.SetSpeed(PlaybackSpeed.Normal));
            BindButton(speedDoubleButton,   () => _playback?.SetSpeed(PlaybackSpeed.Double));
            BindButton(speedQuadButton,     () => _playback?.SetSpeed(PlaybackSpeed.Quadruple));

            if (loopToggle != null)
                loopToggle.onValueChanged.AddListener(v =>
                {
                    if (_playback != null) _playback.LoopMode = v;
                });

            if (timelineSlider != null)
            {
                timelineSlider.onValueChanged.AddListener(v =>
                {
                    if (_scrubbing) _playback?.SeekNormalised(v);
                });

                // Use EventSystem PointerDown/Up to distinguish user drags from code updates.
                var trigger = timelineSlider.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>()
                           ?? timelineSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

                AddEventTriggerEntry(trigger,
                    UnityEngine.EventSystems.EventTriggerType.PointerDown, _ => _scrubbing = true);
                AddEventTriggerEntry(trigger,
                    UnityEngine.EventSystems.EventTriggerType.PointerUp,   _ => _scrubbing = false);
            }

            if (cameraAngleDropdown != null)
            {
                cameraAngleDropdown.ClearOptions();
                var options = new System.Collections.Generic.List<string>
                {
                    "Follow", "Cockpit", "Chase", "Orbit", "Free", "Cinematic"
                };
                cameraAngleDropdown.AddOptions(options);
                cameraAngleDropdown.onValueChanged.AddListener(i =>
                    _cameraCtrl?.SetAngle((CameraAngle)i));
            }
        }

        private static void BindButton(Button btn, Action action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => action?.Invoke());
        }

        private static void AddEventTriggerEntry(
            UnityEngine.EventSystems.EventTrigger trigger,
            UnityEngine.EventSystems.EventTriggerType type,
            UnityEngine.Events.UnityAction<UnityEngine.EventSystems.BaseEventData> callback)
        {
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        #endregion

        #region Private — Event Handlers

        private void HandleTimeChanged(float progress)
        {
            if (_playback == null) return;

            if (currentTimeText != null)
                currentTimeText.text = FormatTime(_playback.CurrentTime);

            if (timelineSlider != null && !_scrubbing)
                timelineSlider.SetValueWithoutNotify(progress);

            UpdateMiniMapIndicator(progress);
        }

        private void HandleSpeedChanged(PlaybackSpeed speed)
        {
            // Highlight the active speed button — simple color swap.
            Button[] btns = { speedQuarterButton, speedHalfButton, speedNormalButton,
                              speedDoubleButton, speedQuadButton };
            for (int i = 0; i < btns.Length; i++)
            {
                if (btns[i] == null) continue;
                var colors = btns[i].colors;
                colors.normalColor = (i == (int)speed) ? Color.white : new Color(0.7f, 0.7f, 0.7f);
                btns[i].colors = colors;
            }
        }

        #endregion

        #region Private — Keyboard Shortcuts

        private void HandleKeyboardShortcuts()
        {
            if (_playback == null) return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_playback.IsPlaying) _playback.Pause();
                else _playback.Play();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                _playback.Seek(_playback.CurrentTime - SeekStepSec);

            if (Input.GetKeyDown(KeyCode.RightArrow))
                _playback.Seek(_playback.CurrentTime + SeekStepSec);

            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
                StepSpeed(+1);

            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                StepSpeed(-1);
        }

        private void StepSpeed(int delta)
        {
            if (_playback == null) return;
            int next = Mathf.Clamp((int)_playback.Speed + delta, 0, 4);
            _playback.SetSpeed((PlaybackSpeed)next);
        }

        #endregion

        #region Private — Mini-Map

        private void UpdateMiniMapIndicator(float progress)
        {
            if (miniMapIndicator == null || miniMapImage == null) return;
            Rect rect = miniMapImage.rectTransform.rect;
            float x   = rect.x + rect.width  * progress;
            float y   = rect.y + rect.height * 0.5f;
            miniMapIndicator.anchoredPosition = new Vector2(x, y);
        }

        #endregion

        #region Private — Formatting

        private static string FormatTime(float totalSeconds)
        {
            int min  = (int)(totalSeconds / 60f);
            int sec  = (int)(totalSeconds % 60f);
            int cs   = (int)((totalSeconds - Mathf.Floor(totalSeconds)) * 100f);
            return string.Format(TimeFormat, min, sec, cs);
        }

        #endregion
    }
}
