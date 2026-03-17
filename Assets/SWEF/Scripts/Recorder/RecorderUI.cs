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
