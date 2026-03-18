using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Recorder
{
    /// <summary>
    /// UI panel for flight recording and playback controls.
    /// Record/Stop/Play/Clear buttons and a progress slider.
    /// </summary>
    public class RecorderUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private FlightRecorder recorder;
        [SerializeField] private FlightPlayback playback;

        [Header("Buttons")]
        [SerializeField] private Button recordButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button toggleButton;

        [Header("UI Elements")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject recorderPanel;

        [Header("Phase 17 — Replay")]
        [SerializeField] private Button saveReplayButton;
        [SerializeField] private Button openReplayBrowserButton;

        private float _recordStartTime;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (recorder == null)
                recorder = FindFirstObjectByType<FlightRecorder>();
            if (playback == null)
                playback = FindFirstObjectByType<FlightPlayback>();

            if (recordButton != null)
                recordButton.onClick.AddListener(OnRecordPressed);
            if (stopButton != null)
                stopButton.onClick.AddListener(OnStopPressed);
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayPressed);
            if (clearButton != null)
                clearButton.onClick.AddListener(OnClearPressed);
            if (toggleButton != null)
                toggleButton.onClick.AddListener(TogglePanel);

            // Phase 17 — Replay
            if (saveReplayButton != null)
                saveReplayButton.onClick.AddListener(OnSaveReplayPressed);
            if (openReplayBrowserButton != null)
                openReplayBrowserButton.onClick.AddListener(OnOpenReplayBrowserPressed);
        }

        private void Update()
        {
            UpdateStatusText();
            UpdateProgressSlider();
        }

        // ── Button callbacks ──────────────────────────────────────────────────────
        private void OnRecordPressed()
        {
            if (recorder == null) return;
            _recordStartTime = Time.time;
            recorder.StartRecording();
        }

        private void OnStopPressed()
        {
            if (recorder != null && recorder.IsRecording)
                recorder.StopRecording();
            else if (playback != null && playback.IsPlaying)
                playback.Stop();
        }

        private void OnPlayPressed()
        {
            if (playback == null) return;
            playback.Play();
        }

        private void OnClearPressed()
        {
            if (recorder == null) return;
            recorder.ClearRecording();
        }

        private void TogglePanel()
        {
            if (recorderPanel != null)
                recorderPanel.SetActive(!recorderPanel.activeSelf);
        }

        // ── Phase 17 — Replay callbacks ───────────────────────────────────────────

        private void OnSaveReplayPressed()
        {
            if (recorder == null) return;

            // Stop active recording before exporting
            if (recorder.IsRecording)
                recorder.StopRecording();

            if (recorder.GetFrames().Count == 0)
            {
                Debug.LogWarning("[SWEF] RecorderUI: No frames to save.");
                return;
            }

            var fileManager = Replay.ReplayFileManager.Instance
                ?? FindFirstObjectByType<Replay.ReplayFileManager>();

            if (fileManager == null)
            {
                Debug.LogWarning("[SWEF] RecorderUI: ReplayFileManager not found.");
                return;
            }

            var data = recorder.ExportToReplayData();
            fileManager.SaveReplay(data);

            // Toast
            if (statusText != null)
            {
                statusText.text = "Replay saved! ✈️";
                CancelInvoke(nameof(ResetStatusText));
                Invoke(nameof(ResetStatusText), 3f);
            }
            Debug.Log("[SWEF] RecorderUI: Replay saved via save button.");
        }

        private void OnOpenReplayBrowserPressed()
        {
            var browser = FindFirstObjectByType<UI.ReplayBrowserUI>();
            if (browser != null)
            {
                browser.gameObject.SetActive(true);
                browser.Refresh();
            }
            else
            {
                Debug.LogWarning("[SWEF] RecorderUI: ReplayBrowserUI not found in scene.");
            }
        }

        private void ResetStatusText()
        {
            UpdateStatusText();
        }

        // ── UI update ─────────────────────────────────────────────────────────────
        private void UpdateStatusText()
        {
            if (statusText == null) return;

            if (recorder != null && recorder.IsRecording)
            {
                float elapsed = Time.time - _recordStartTime;
                statusText.text = $"🔴 REC {elapsed:0.0}s";
            }
            else if (playback != null && playback.IsPlaying)
            {
                float progress = playback.PlaybackProgress01 * 100f;
                statusText.text = $"▶ {progress:0}%";
            }
            else
            {
                int frameCount = recorder != null ? recorder.Frames.Count : 0;
                statusText.text = $"Ready ({frameCount} frames)";
            }
        }

        private void UpdateProgressSlider()
        {
            if (progressSlider == null) return;
            if (playback != null && playback.IsPlaying)
                progressSlider.value = playback.PlaybackProgress01;
        }
    }
}
