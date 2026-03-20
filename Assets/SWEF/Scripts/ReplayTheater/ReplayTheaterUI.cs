using System;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Replay;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Full UI panel for the Replay Theater mode.
    /// Hosts the timeline scrub bar, transport controls, speed selector,
    /// camera mode selector, and exit button.
    /// </summary>
    public class ReplayTheaterUI : MonoBehaviour
    {
        #region Inspector — Panels

        [Header("Root Panel")]
        [SerializeField] private GameObject rootPanel;

        [Header("Transport Controls")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button loopButton;
        [SerializeField] private Text   timeLabel;

        [Header("Speed Selector")]
        [SerializeField] private Dropdown speedDropdown;

        [Header("Camera Mode Selector")]
        [SerializeField] private Dropdown cameraModeDropdown;

        [Header("Timeline Track")]
        [SerializeField] private TimelineTrack timelineTrack;

        [Header("Minimap")]
        [SerializeField] private RawImage minimapImage;

        [Header("Metadata")]
        [SerializeField] private Text replayNameLabel;
        [SerializeField] private Text replayDurationLabel;

        [Header("Exit")]
        [SerializeField] private Button exitButton;

        #endregion

        #region Inspector — References

        [Header("Dependencies")]
        [SerializeField] private ReplayTheaterManager manager;
        [SerializeField] private ReplayTimeline       timeline;
        [SerializeField] private CinematicCameraEditor cameraEditor;
        [SerializeField] private ReplayTheaterSettings settings;

        #endregion

        #region Private State

        private bool _initialised;

        private static readonly string[] SpeedLabels  = { "0.25×", "0.5×", "1×", "2×", "4×" };
        private static readonly float[]  SpeedValues  = { 0.25f,  0.5f,  1f,  2f,  4f  };
        private static readonly string[] CameraLabels =
        {
            "Free Cam", "Follow Cam", "Orbit Cam", "Track Cam", "Dolly Cam"
        };

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (settings == null)
                settings = Resources.Load<ReplayTheaterSettings>("ReplayTheaterSettings");

            // Auto-discover
            if (manager      == null) manager      = FindFirstObjectByType<ReplayTheaterManager>();
            if (timeline     == null) timeline     = FindFirstObjectByType<ReplayTimeline>();
            if (cameraEditor == null) cameraEditor = FindFirstObjectByType<CinematicCameraEditor>();
        }

        private void Start()
        {
            WireUpButtons();
            PopulateDropdowns();
            SubscribeToTimeline();
            Hide();
        }

        private void Update()
        {
            if (rootPanel == null || !rootPanel.activeSelf) return;
            if (timeline  == null) return;

            UpdateTimeLabel();
        }

        #endregion

        #region Public API

        /// <summary>Shows the theater UI and populates metadata from <paramref name="data"/>.</summary>
        /// <param name="data">The loaded replay data.</param>
        public void Show(ReplayData data)
        {
            if (rootPanel != null) rootPanel.SetActive(true);

            if (data != null)
            {
                if (replayNameLabel     != null) replayNameLabel.text     = data.playerName ?? "Replay";
                if (replayDurationLabel != null)
                    replayDurationLabel.text = FormatTime(data.GetDuration());
            }

            if (timelineTrack != null && timeline != null)
                timelineTrack.Bind(timeline);

            _initialised = true;
            Debug.Log("[SWEF] ReplayTheaterUI: Shown.");
        }

        /// <summary>Hides the theater UI.</summary>
        public void Hide()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            Debug.Log("[SWEF] ReplayTheaterUI: Hidden.");
        }

        /// <summary>Refreshes keyframe markers on the timeline track.</summary>
        public void RefreshKeyframeMarkers()
        {
            timelineTrack?.RefreshKeyframeMarkers(cameraEditor);
        }

        #endregion

        #region Setup

        private void WireUpButtons()
        {
            playButton?.onClick.AddListener(OnPlayClicked);
            pauseButton?.onClick.AddListener(OnPauseClicked);
            stopButton?.onClick.AddListener(OnStopClicked);
            loopButton?.onClick.AddListener(OnLoopClicked);
            exitButton?.onClick.AddListener(OnExitClicked);

            if (timelineTrack != null)
            {
                timelineTrack.OnScrub           += OnTimelineScrub;
                timelineTrack.OnKeyframeSelected += OnKeyframeSelected;
            }
        }

        private void PopulateDropdowns()
        {
            // Speed
            if (speedDropdown != null)
            {
                speedDropdown.ClearOptions();
                speedDropdown.AddOptions(new System.Collections.Generic.List<string>(SpeedLabels));
                speedDropdown.value = 2; // default 1×
                speedDropdown.onValueChanged.AddListener(OnSpeedChanged);
            }

            // Camera mode
            if (cameraModeDropdown != null)
            {
                cameraModeDropdown.ClearOptions();
                cameraModeDropdown.AddOptions(new System.Collections.Generic.List<string>(CameraLabels));
                cameraModeDropdown.onValueChanged.AddListener(OnCameraModeChanged);
            }
        }

        private void SubscribeToTimeline()
        {
            if (timeline == null) return;
            timeline.OnTimeChanged      += OnTimelineTimeChanged;
            timeline.OnPlayStateChanged += OnPlayStateChanged;
        }

        #endregion

        #region Button Handlers

        private void OnPlayClicked()
        {
            manager?.Play();
        }

        private void OnPauseClicked()
        {
            manager?.Pause();
        }

        private void OnStopClicked()
        {
            manager?.Stop();
        }

        private void OnLoopClicked()
        {
            if (timeline == null) return;
            var values = System.Enum.GetValues(typeof(ReplayTimeline.LoopMode));
            var next   = (ReplayTimeline.LoopMode)(((int)timeline.LoopMode + 1) % values.Length);
            timeline.SetLoopMode(next);
            Debug.Log($"[SWEF] ReplayTheaterUI: Loop mode → {next}.");
        }

        private void OnExitClicked()
        {
            manager?.ExitTheater();
        }

        private void OnSpeedChanged(int index)
        {
            if (index < 0 || index >= SpeedValues.Length) return;
            timeline?.SetSpeed(SpeedValues[index]);
        }

        private void OnCameraModeChanged(int index)
        {
            // Only logs here; actual mode is driven by keyframe evaluation
            var enumValues = System.Enum.GetValues(typeof(CameraMode));
            if (index < 0 || index >= enumValues.Length)
            {
                Debug.LogWarning($"[SWEF] ReplayTheaterUI: Camera mode index {index} is out of range.");
                return;
            }
            var mode = (CameraMode)enumValues.GetValue(index);
            Debug.Log($"[SWEF] ReplayTheaterUI: Camera mode selector → {mode}.");
        }

        private void OnTimelineScrub(float time)
        {
            manager?.BeginScrubbing();
            timeline?.SeekTo(time);
        }

        private void OnKeyframeSelected(int index)
        {
            Debug.Log($"[SWEF] ReplayTheaterUI: Keyframe {index} selected.");
        }

        #endregion

        #region Timeline Callbacks

        private void OnTimelineTimeChanged(float time)
        {
            // Camera editor drives the camera every frame
            cameraEditor?.EvaluateAndApply(time);
        }

        private void OnPlayStateChanged(bool playing)
        {
            if (playButton  != null) playButton.interactable  = !playing;
            if (pauseButton != null) pauseButton.interactable =  playing;
        }

        #endregion

        #region Helpers

        private void UpdateTimeLabel()
        {
            if (timeLabel == null) return;
            float cur   = timeline?.CurrentTime   ?? 0f;
            float total = timeline?.TotalDuration ?? 0f;
            timeLabel.text = $"{FormatTime(cur)} / {FormatTime(total)}";
        }

        private static string FormatTime(float seconds)
        {
            int    mins  = (int)(seconds / 60f);
            float  secs  = seconds - mins * 60f;
            return $"{mins:D2}:{secs:00.000}";
        }

        #endregion
    }
}
