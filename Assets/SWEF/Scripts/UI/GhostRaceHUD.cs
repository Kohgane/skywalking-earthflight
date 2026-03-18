using UnityEngine;
using UnityEngine.UI;
using SWEF.Replay;

namespace SWEF.UI
{
    /// <summary>
    /// Heads-up display overlay for the ghost racing feature.
    /// Subscribes to <see cref="GhostRacer"/> events and updates comparison stats,
    /// progress, and status text every frame. Auto-hides when no race is active.
    /// </summary>
    public class GhostRaceHUD : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject raceHudPanel;

        [Header("Comparison Text")]
        [SerializeField] private Text timeDeltaText;
        [SerializeField] private Text altitudeDeltaText;
        [SerializeField] private Text speedDeltaText;

        [Header("Progress")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text   progressTimeText;

        [Header("Status")]
        [SerializeField] private Text   ghostStatusText;
        [SerializeField] private Button stopRaceButton;
        [SerializeField] private Button pauseResumeButton;

        [Header("Playback Speed")]
        [SerializeField] private Slider speedSlider;

        [Header("Colours")]
        [SerializeField] private Color aheadColor   = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color behindColor  = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color neutralColor = Color.white;

        // ── Dependencies ──────────────────────────────────────────────────────────
        [Header("Dependencies")]
        [SerializeField] private GhostRacer ghostRacer;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (ghostRacer == null)
                ghostRacer = FindFirstObjectByType<GhostRacer>();

            if (ghostRacer != null)
            {
                ghostRacer.OnRaceStarted      += HandleRaceStarted;
                ghostRacer.OnRaceFinished     += HandleRaceFinished;
                ghostRacer.OnTimeDeltaChanged += HandleTimeDeltaChanged;
            }

            if (stopRaceButton != null)
                stopRaceButton.onClick.AddListener(OnStopPressed);
            if (pauseResumeButton != null)
                pauseResumeButton.onClick.AddListener(OnPauseResumePressed);
            if (speedSlider != null)
            {
                speedSlider.minValue = 0.25f;
                speedSlider.maxValue = 4.0f;
                speedSlider.value    = 1.0f;
                speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
            }

            SetPanelVisible(false);
        }

        private void OnDestroy()
        {
            if (ghostRacer != null)
            {
                ghostRacer.OnRaceStarted      -= HandleRaceStarted;
                ghostRacer.OnRaceFinished     -= HandleRaceFinished;
                ghostRacer.OnTimeDeltaChanged -= HandleTimeDeltaChanged;
            }
        }

        private void Update()
        {
            if (ghostRacer == null) return;
            if (ghostRacer.CurrentState != GhostRacer.GhostState.Racing &&
                ghostRacer.CurrentState != GhostRacer.GhostState.Paused) return;

            UpdateProgressUI();
            UpdateComparisonUI();
            UpdateStatusUI();
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void HandleRaceStarted()
        {
            SetPanelVisible(true);
            if (ghostStatusText != null) ghostStatusText.text = "🏁 RACING";
        }

        private void HandleRaceFinished()
        {
            if (ghostStatusText != null) ghostStatusText.text = "🎉 FINISHED";
            // Keep HUD visible briefly so the player can see the final stats,
            // then hide after a delay
            Invoke(nameof(HidePanel), 5f);
        }

        private void HandleTimeDeltaChanged(float delta)
        {
            UpdateTimeDeltaDisplay(delta);
        }

        // ── Button callbacks ──────────────────────────────────────────────────────

        private void OnStopPressed()
        {
            ghostRacer?.StopRace();
            SetPanelVisible(false);
        }

        private void OnPauseResumePressed()
        {
            if (ghostRacer == null) return;
            if (ghostRacer.CurrentState == GhostRacer.GhostState.Racing)
            {
                ghostRacer.PauseRace();
                if (ghostStatusText != null)   ghostStatusText.text     = "⏸ PAUSED";
                if (pauseResumeButton != null)
                {
                    var txt = pauseResumeButton.GetComponentInChildren<Text>();
                    if (txt != null) txt.text = "▶";
                }
            }
            else if (ghostRacer.CurrentState == GhostRacer.GhostState.Paused)
            {
                ghostRacer.ResumeRace();
                if (ghostStatusText != null)   ghostStatusText.text     = "🏁 RACING";
                if (pauseResumeButton != null)
                {
                    var txt = pauseResumeButton.GetComponentInChildren<Text>();
                    if (txt != null) txt.text = "⏸";
                }
            }
        }

        private void OnSpeedSliderChanged(float value)
        {
            ghostRacer?.SetPlaybackSpeed(value);
        }

        // ── UI update helpers ─────────────────────────────────────────────────────

        private void UpdateProgressUI()
        {
            if (ghostRacer == null) return;

            float progress = ghostRacer.PlaybackProgress01;
            float duration = ghostRacer.ReplayDuration;
            float elapsed  = progress * duration;

            if (progressSlider != null)
                progressSlider.value = progress;

            if (progressTimeText != null)
                progressTimeText.text = $"{FormatTime(elapsed)} / {FormatTime(duration)}";
        }

        private void UpdateComparisonUI()
        {
            if (ghostRacer == null) return;

            UpdateTimeDeltaDisplay(ghostRacer.TimeDelta);

            if (altitudeDeltaText != null)
            {
                float alt = ghostRacer.AltitudeDelta;
                altitudeDeltaText.text = alt >= 0f
                    ? $"↑ {alt:F0} m"
                    : $"↓ {Mathf.Abs(alt):F0} m";
            }

            if (speedDeltaText != null)
            {
                float spd = ghostRacer.SpeedDelta * 3.6f; // m/s → km/h
                speedDeltaText.text = spd >= 0f
                    ? $"+{spd:F0} km/h"
                    : $"{spd:F0} km/h";
            }
        }

        private void UpdateTimeDeltaDisplay(float delta)
        {
            if (timeDeltaText == null) return;

            if (Mathf.Abs(delta) <= 0.5f)
            {
                timeDeltaText.text  = "= EVEN";
                timeDeltaText.color = neutralColor;
            }
            else if (delta > 0f)
            {
                timeDeltaText.text  = $"+{delta:F1}s AHEAD";
                timeDeltaText.color = aheadColor;
            }
            else
            {
                timeDeltaText.text  = $"{delta:F1}s BEHIND";
                timeDeltaText.color = behindColor;
            }
        }

        private void UpdateStatusUI()
        {
            if (ghostStatusText == null || ghostRacer == null) return;
            if (ghostRacer.CurrentState == GhostRacer.GhostState.Racing)
                ghostStatusText.text = "🏁 RACING";
            else if (ghostRacer.CurrentState == GhostRacer.GhostState.Paused)
                ghostStatusText.text = "⏸ PAUSED";
        }

        private void SetPanelVisible(bool visible)
        {
            if (raceHudPanel != null) raceHudPanel.SetActive(visible);
        }

        private void HidePanel() => SetPanelVisible(false);

        private static string FormatTime(float seconds)
        {
            int m = (int)seconds / 60;
            int s = (int)seconds % 60;
            return $"{m}:{s:00}";
        }
    }
}
