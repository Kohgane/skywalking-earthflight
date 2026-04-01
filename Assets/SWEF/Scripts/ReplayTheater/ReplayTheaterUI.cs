using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Replay;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Full UI panel for the Replay Theater mode.
    /// Hosts transport controls, a multi-track timeline (video / audio / effects),
    /// clip thumbnails, drag-and-drop reordering, a real-time preview window,
    /// an export-progress dialog, keyboard shortcuts, and references to the
    /// new editing and export managers.
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

        [Header("Timeline — Video Track")]
        [SerializeField] private TimelineTrack timelineTrack;

        [Header("Timeline — Audio Track")]
        [SerializeField] private RectTransform audioTrackContainer;

        [Header("Timeline — Effects Track")]
        [SerializeField] private RectTransform effectsTrackContainer;

        [Header("Clip Thumbnails")]
        [SerializeField] private RectTransform clipThumbnailContainer;
        [SerializeField] private RawImage      clipThumbnailPrefab;

        [Header("Drag and Drop")]
        [SerializeField] private bool enableDragDrop = true;

        [Header("Preview Window")]
        [SerializeField] private RawImage previewWindow;

        [Header("Export Progress")]
        [SerializeField] private GameObject exportProgressPanel;
        [SerializeField] private Slider     exportProgressSlider;
        [SerializeField] private Text       exportProgressLabel;

        [Header("Keyboard Shortcuts")]
        [SerializeField] private GameObject shortcutsPanel;

        [Header("Minimap")]
        [SerializeField] private RawImage minimapImage;

        [Header("Metadata")]
        [SerializeField] private Text replayNameLabel;
        [SerializeField] private Text replayDurationLabel;

        [Header("Clip List")]
        [SerializeField] private RectTransform clipListContainer;

        [Header("Exit")]
        [SerializeField] private Button exitButton;

        #endregion

        #region Inspector — References

        [Header("Dependencies")]
        [SerializeField] private ReplayTheaterManager  manager;
        [SerializeField] private ReplayTimeline        timeline;
        [SerializeField] private CinematicCameraEditor cameraEditor;
        [SerializeField] private ReplayTheaterSettings settings;

        [Header("Editor Managers")]
        [SerializeField] private ReplayEditorManager  editorManager;
        [SerializeField] private ReplayExportManager  exportManager;

        #endregion

        #region State

        private bool _initialised;

        // Drag-and-drop state
        private int  _dragSourceIndex = -1;

        private static readonly string[] SpeedLabels  = { "0.25×", "0.5×", "1×", "2×", "4×" };
        private static readonly float[]  SpeedValues  = { 0.25f,  0.5f,  1f,  2f,  4f  };
        private static readonly string[] CameraLabels =
        {
            "Free Cam", "Follow Cam", "Orbit Cam", "Track Cam", "Dolly Cam"
        };

        private static readonly List<string> SpeedLabelList  = new List<string>(SpeedLabels);
        private static readonly List<string> CameraLabelList = new List<string>(CameraLabels);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (settings == null)
                settings = Resources.Load<ReplayTheaterSettings>("ReplayTheaterSettings");

            // Auto-discover
            if (manager        == null) manager        = FindFirstObjectByType<ReplayTheaterManager>();
            if (timeline       == null) timeline       = FindFirstObjectByType<ReplayTimeline>();
            if (cameraEditor   == null) cameraEditor   = FindFirstObjectByType<CinematicCameraEditor>();
            if (editorManager  == null) editorManager  = ReplayEditorManager.Instance;
            if (exportManager  == null) exportManager  = ReplayExportManager.Instance;
        }

        private void Start()
        {
            WireUpButtons();
            PopulateDropdowns();
            SubscribeToTimeline();
            SubscribeToEditorManager();
            SubscribeToExportManager();
            HideExportProgress();
            Hide();
        }

        private void Update()
        {
            if (rootPanel == null || !rootPanel.activeSelf) return;

            if (timeline != null)
                UpdateTimeLabel();

            HandleKeyboardShortcuts();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEditorManager();
            UnsubscribeFromExportManager();
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

        /// <summary>
        /// Updates the export-progress dialog to display <paramref name="progress"/>.
        /// Shows the dialog if it is hidden.
        /// </summary>
        /// <param name="progress">Progress value in the range [0, 1].</param>
        public void ShowExportProgress(float progress)
        {
            if (exportProgressPanel != null) exportProgressPanel.SetActive(true);

            float clamped = Mathf.Clamp01(progress);

            if (exportProgressSlider != null)
                exportProgressSlider.value = clamped;

            if (exportProgressLabel != null)
                exportProgressLabel.text = $"Exporting… {clamped * 100f:F0}%";
        }

        /// <summary>Hides the export-progress dialog.</summary>
        public void HideExportProgress()
        {
            if (exportProgressPanel != null) exportProgressPanel.SetActive(false);
        }

        /// <summary>
        /// Rebuilds the clip list UI from the current project in <see cref="ReplayEditorManager"/>.
        /// Spawns thumbnail placeholders when a prefab is assigned.
        /// </summary>
        public void RefreshClipList()
        {
            if (clipListContainer == null) return;

            // Clear existing entries
            foreach (Transform child in clipListContainer)
                Destroy(child.gameObject);

            var project = editorManager?.CurrentProject;
            if (project == null) return;

            for (int i = 0; i < project.clips.Count; i++)
            {
                var clip = project.clips[i];
                SpawnClipThumbnail(clip, i);
            }

            Debug.Log($"[SWEF] ReplayTheaterUI: Clip list refreshed ({project.clips.Count} clips).");
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
            if (speedDropdown != null)
            {
                speedDropdown.ClearOptions();
                speedDropdown.AddOptions(SpeedLabelList);
                speedDropdown.value = 2; // default 1×
                speedDropdown.onValueChanged.AddListener(OnSpeedChanged);
            }

            if (cameraModeDropdown != null)
            {
                cameraModeDropdown.ClearOptions();
                cameraModeDropdown.AddOptions(CameraLabelList);
                cameraModeDropdown.onValueChanged.AddListener(OnCameraModeChanged);
            }
        }

        private void SubscribeToTimeline()
        {
            if (timeline == null) return;
            timeline.OnTimeChanged      += OnTimelineTimeChanged;
            timeline.OnPlayStateChanged += OnPlayStateChanged;
        }

        private void SubscribeToEditorManager()
        {
            if (editorManager == null) return;
            editorManager.OnProjectCreated += OnProjectCreated;
            editorManager.OnClipAdded      += OnClipAdded;
            editorManager.OnClipRemoved    += OnClipRemoved;
        }

        private void UnsubscribeFromEditorManager()
        {
            if (editorManager == null) return;
            editorManager.OnProjectCreated -= OnProjectCreated;
            editorManager.OnClipAdded      -= OnClipAdded;
            editorManager.OnClipRemoved    -= OnClipRemoved;
        }

        private void SubscribeToExportManager()
        {
            if (exportManager == null) return;
            exportManager.OnExportProgress  += ShowExportProgress;
            exportManager.OnExportCompleted += OnExportCompleted;
            exportManager.OnExportFailed    += OnExportFailed;
        }

        private void UnsubscribeFromExportManager()
        {
            if (exportManager == null) return;
            exportManager.OnExportProgress  -= ShowExportProgress;
            exportManager.OnExportCompleted -= OnExportCompleted;
            exportManager.OnExportFailed    -= OnExportFailed;
        }

        #endregion

        #region Button Handlers

        private void OnPlayClicked()  => manager?.Play();
        private void OnPauseClicked() => manager?.Pause();
        private void OnStopClicked()  => manager?.Stop();

        private void OnLoopClicked()
        {
            if (timeline == null) return;
            var values = System.Enum.GetValues(typeof(ReplayTimeline.LoopMode));
            var next   = (ReplayTimeline.LoopMode)(((int)timeline.LoopMode + 1) % values.Length);
            timeline.SetLoopMode(next);
            Debug.Log($"[SWEF] ReplayTheaterUI: Loop mode → {next}.");
        }

        private void OnExitClicked() => manager?.ExitTheater();

        private void OnSpeedChanged(int index)
        {
            if (index < 0 || index >= SpeedValues.Length) return;
            timeline?.SetSpeed(SpeedValues[index]);
        }

        private void OnCameraModeChanged(int index)
        {
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

        #region Keyboard Shortcuts

        private void HandleKeyboardShortcuts()
        {
            // Space — play / pause toggle
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (manager?.State == ReplayTheaterManager.TheaterState.Playing)
                    manager.Pause();
                else
                    manager?.Play();
            }

            // Left / Right — seek ±5 seconds
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                timeline?.SeekTo((timeline?.CurrentTime ?? 0f) - 5f);

            if (Input.GetKeyDown(KeyCode.RightArrow))
                timeline?.SeekTo((timeline?.CurrentTime ?? 0f) + 5f);

            // S — split clip at current time (placeholder: logs intent)
            if (Input.GetKeyDown(KeyCode.S))
                Debug.Log("[SWEF] ReplayTheaterUI: Split shortcut triggered.");

            // T — trim mode (placeholder: logs intent)
            if (Input.GetKeyDown(KeyCode.T))
                Debug.Log("[SWEF] ReplayTheaterUI: Trim shortcut triggered.");

            // Ctrl+Z / Ctrl+Y — undo / redo
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.Z)) editorManager?.Undo();
                if (Input.GetKeyDown(KeyCode.Y)) editorManager?.Redo();
            }
        }

        #endregion

        #region Editor Manager Callbacks

        private void OnProjectCreated(ReplayProject project)
        {
            Debug.Log($"[SWEF] ReplayTheaterUI: Project created — '{project.title}'.");
            RefreshClipList();
        }

        private void OnClipAdded(ReplayClip clip)
        {
            Debug.Log($"[SWEF] ReplayTheaterUI: Clip added — '{clip.clipId}'.");
            RefreshClipList();
        }

        private void OnClipRemoved(ReplayClip clip)
        {
            Debug.Log($"[SWEF] ReplayTheaterUI: Clip removed — '{clip.clipId}'.");
            RefreshClipList();
        }

        #endregion

        #region Export Manager Callbacks

        private void OnExportCompleted(string path)
        {
            HideExportProgress();
            Debug.Log($"[SWEF] ReplayTheaterUI: Export complete → '{path}'.");
        }

        private void OnExportFailed(string error)
        {
            HideExportProgress();
            Debug.LogWarning($"[SWEF] ReplayTheaterUI: Export failed — {error}.");
        }

        #endregion

        #region Timeline Callbacks

        private void OnTimelineTimeChanged(float time)
        {
            cameraEditor?.EvaluateAndApply(time);
        }

        private void OnPlayStateChanged(bool playing)
        {
            if (playButton  != null) playButton.interactable  = !playing;
            if (pauseButton != null) pauseButton.interactable =  playing;
        }

        #endregion

        #region Clip Thumbnails & Drag-and-Drop

        private void SpawnClipThumbnail(ReplayClip clip, int index)
        {
            if (clipThumbnailPrefab == null || clipListContainer == null) return;

            var thumb = Instantiate(clipThumbnailPrefab, clipListContainer);
            thumb.name = $"Thumb_{clip.clipId}";

            // Store the clip index on the GameObject for drag-drop identification
            var tag = thumb.gameObject.AddComponent<ClipIndexTag>();
            tag.Index  = index;
            tag.ClipId = clip.clipId;

            if (enableDragDrop)
                WireDragDrop(thumb, index);
        }

        private void WireDragDrop(RawImage thumb, int index)
        {
            var trigger = thumb.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var beginEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.BeginDrag
            };
            int captured = index;
            beginEntry.callback.AddListener(_ => _dragSourceIndex = captured);
            trigger.triggers.Add(beginEntry);

            var dropEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.Drop
            };
            dropEntry.callback.AddListener(_ => OnClipDropped(captured));
            trigger.triggers.Add(dropEntry);
        }

        private void OnClipDropped(int targetIndex)
        {
            int src = _dragSourceIndex;
            int dst = targetIndex;
            _dragSourceIndex = -1;

            if (src < 0 || src == dst) return;

            var project = editorManager?.CurrentProject;
            if (project == null) return;

            var clip = project.clips[src];
            project.clips.RemoveAt(src);
            // After removing src, items after it shift down by one; adjust dst accordingly.
            int insertAt = Mathf.Clamp(dst > src ? dst - 1 : dst, 0, project.clips.Count);
            project.clips.Insert(insertAt, clip);

            Debug.Log($"[SWEF] ReplayTheaterUI: Clip moved from {src} → {dst}.");
            RefreshClipList();
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
            int   mins = (int)(seconds / 60f);
            float secs = seconds - mins * 60f;
            return $"{mins:D2}:{secs:00.000}";
        }

        #endregion

        #region Inner Types

        /// <summary>Tiny component used to tag a clip thumbnail with its list index and clip ID.</summary>
        private class ClipIndexTag : MonoBehaviour
        {
            /// <summary>Zero-based index of this clip in the project clip list.</summary>
            public int    Index;
            /// <summary>Identifier of the clip represented by this thumbnail.</summary>
            public string ClipId;
        }

        #endregion
    }
}

