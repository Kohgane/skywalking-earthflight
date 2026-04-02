// RaceHUD.cs — SWEF Competitive Racing & Time Trial System (Phase 88)
using System;
using UnityEngine;
using UnityEngine.UI;
using SWEF.UI;

namespace SWEF.CompetitiveRacing
{
    /// <summary>
    /// Phase 88 — In-race HUD overlay.  Displays the race timer, split delta,
    /// checkpoint progress, lap counter, wrong-way warning, medal prediction,
    /// elimination countdown, ghost comparison, and a compact/full HUD toggle.
    ///
    /// <para>Subscribes to <see cref="RaceManager"/> events on enable/disable.</para>
    /// </summary>
    public class RaceHUD : MonoBehaviour
    {
        // ── Inspector — Panels ────────────────────────────────────────────────────

        [Header("Panels")]
        [SerializeField] private GameObject _fullHUDPanel;
        [SerializeField] private GameObject _compactHUDPanel;
        [SerializeField] private GameObject _wrongWayBanner;
        [SerializeField] private GameObject _alertBanner;

        // ── Inspector — Timer ─────────────────────────────────────────────────────

        [Header("Timer")]
        [SerializeField] private Text _elapsedTimeText;
        [SerializeField] private Text _splitDeltaText;

        // ── Inspector — Progress ──────────────────────────────────────────────────

        [Header("Progress")]
        [SerializeField] private Text   _checkpointProgressText;
        [SerializeField] private Text   _lapCounterText;
        [SerializeField] private Slider _progressSlider;

        // ── Inspector — Speed / Alt ───────────────────────────────────────────────

        [Header("Flight Data")]
        [SerializeField] private Text _speedText;
        [SerializeField] private Text _altitudeText;

        // ── Inspector — Medal ─────────────────────────────────────────────────────

        [Header("Medal Prediction")]
        [SerializeField] private Text  _medalPredictionText;

        // ── Inspector — Elimination ───────────────────────────────────────────────

        [Header("Elimination Mode")]
        [SerializeField] private GameObject _eliminationPanel;
        [SerializeField] private Text       _eliminationCountdownText;

        // ── Inspector — Ghost Panel ───────────────────────────────────────────────

        [Header("Ghost Comparison")]
        [SerializeField] private GhostRaceHUD _ghostRaceHUD;

        // ── Inspector — Alert ─────────────────────────────────────────────────────

        [Header("Alert Banner")]
        [SerializeField] private Text _alertText;

        // ── Private State ─────────────────────────────────────────────────────────

        private bool  _isCompactMode;
        private float _alertTimer;
        private float _eliminationTimer;
        private bool  _wrongWayActive;

        private Flight.FlightController  _playerFlight;
        private Flight.AltitudeController _playerAltitude;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _playerFlight   = FindFirstObjectByType<Flight.FlightController>();
            _playerAltitude = FindFirstObjectByType<Flight.AltitudeController>();
            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            if (RaceManager.Instance == null) return;
            RaceManager.Instance.OnRaceStarted     += HandleRaceStarted;
            RaceManager.Instance.OnRaceFinished    += HandleRaceFinished;
            RaceManager.Instance.OnCheckpointCaptured += HandleCheckpointCaptured;
            RaceManager.Instance.OnLapCompleted    += HandleLapCompleted;
            RaceManager.Instance.OnWrongWay        += HandleWrongWay;
            RaceManager.Instance.OnEliminated      += HandleEliminated;
            RaceManager.Instance.OnRaceAlert       += HandleRaceAlert;
        }

        private void OnDisable()
        {
            if (RaceManager.Instance == null) return;
            RaceManager.Instance.OnRaceStarted     -= HandleRaceStarted;
            RaceManager.Instance.OnRaceFinished    -= HandleRaceFinished;
            RaceManager.Instance.OnCheckpointCaptured -= HandleCheckpointCaptured;
            RaceManager.Instance.OnLapCompleted    -= HandleLapCompleted;
            RaceManager.Instance.OnWrongWay        -= HandleWrongWay;
            RaceManager.Instance.OnEliminated      -= HandleEliminated;
            RaceManager.Instance.OnRaceAlert       -= HandleRaceAlert;
        }

        private void Update()
        {
            if (RaceManager.Instance == null) return;
            if (RaceManager.Instance.status != RaceStatus.Racing &&
                RaceManager.Instance.status != RaceStatus.Paused) return;

            UpdateTimer();
            UpdateProgress();
            UpdateFlightData();
            UpdateMedalPrediction();
            TickAlertBanner();
            TickWrongWay();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Toggles between compact and full HUD.</summary>
        public void ToggleCompactMode()
        {
            _isCompactMode = !_isCompactMode;
            if (_fullHUDPanel    != null) _fullHUDPanel.SetActive(!_isCompactMode);
            if (_compactHUDPanel != null) _compactHUDPanel.SetActive(_isCompactMode);
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void HandleRaceStarted(RaceCourse course)
        {
            SetPanelVisible(true);
            if (_lapCounterText     != null) _lapCounterText.text = "Lap 1";
            if (_eliminationPanel   != null)
                _eliminationPanel.SetActive(RaceManager.Instance.activeMode == RaceMode.Elimination);
            _eliminationTimer = CompetitiveRacingConfig.EliminationInterval;
        }

        private void HandleRaceFinished(RaceResult result)
        {
            Invoke(nameof(HideHUD), CompetitiveRacingConfig.RaceFinishHUDLingerDuration);
        }

        private void HandleCheckpointCaptured(RaceCheckpoint cp, float splitTime)
        {
            UpdateProgress();
        }

        private void HandleLapCompleted(int lap)
        {
            if (_lapCounterText != null)
                _lapCounterText.text = $"Lap {RaceManager.Instance.currentLap}";
        }

        private void HandleWrongWay()
        {
            if (_wrongWayBanner != null)
                _wrongWayBanner.SetActive(true);
            _wrongWayActive = true;
        }

        private void HandleEliminated(string playerId)
        {
            ShowAlert("⚠ ELIMINATED!");
        }

        private void HandleRaceAlert(RaceAlertType alert)
        {
            switch (alert)
            {
                case RaceAlertType.NewPersonalBest: ShowAlert("★ NEW PERSONAL BEST!"); break;
                case RaceAlertType.NewRecord:       ShowAlert("🏆 NEW RECORD!");        break;
                case RaceAlertType.BonusCheckpoint: ShowAlert("✚ BONUS!");             break;
                case RaceAlertType.CheckpointMissed:ShowAlert("✗ CHECKPOINT MISSED");  break;
                case RaceAlertType.LapComplete:     ShowAlert("LAP COMPLETE ✓");       break;
                case RaceAlertType.RaceFinished:    ShowAlert("RACE FINISHED 🏁");      break;
            }
        }

        // ── Private Update Helpers ────────────────────────────────────────────────

        private void UpdateTimer()
        {
            if (_elapsedTimeText == null) return;
            float t = RaceManager.Instance.elapsedTime;
            _elapsedTimeText.text = FormatTime(t);
        }

        private void UpdateProgress()
        {
            if (RaceManager.Instance.activeCourse == null) return;

            int total   = RaceManager.Instance.activeCourse.checkpoints.Count;
            int current = RaceManager.Instance.currentCheckpointIndex;

            if (_checkpointProgressText != null)
                _checkpointProgressText.text = $"{current} / {total}";

            if (_progressSlider != null)
                _progressSlider.value = total > 0 ? (float)current / total : 0f;
        }

        private void UpdateFlightData()
        {
            if (_speedText != null && _playerFlight != null)
                _speedText.text = $"{_playerFlight.CurrentSpeed:0} m/s";

            if (_altitudeText != null && _playerAltitude != null)
                _altitudeText.text = $"{_playerAltitude.CurrentAltitude:0} m";
        }

        private void UpdateMedalPrediction()
        {
            if (_medalPredictionText == null) return;
            var course = RaceManager.Instance.activeCourse;
            if (course == null) return;

            float t = RaceManager.Instance.elapsedTime;
            if (t <= course.goldTime)        _medalPredictionText.text = "🥇 On pace for GOLD";
            else if (t <= course.silverTime) _medalPredictionText.text = "🥈 On pace for SILVER";
            else if (t <= course.bronzeTime) _medalPredictionText.text = "🥉 On pace for BRONZE";
            else                             _medalPredictionText.text = "No medal at current pace";
        }

        private void TickAlertBanner()
        {
            if (_alertBanner == null || !_alertBanner.activeSelf) return;
            _alertTimer -= Time.deltaTime;
            if (_alertTimer <= 0f) _alertBanner.SetActive(false);
        }

        private void TickWrongWay()
        {
            if (!_wrongWayActive) return;
            if (_wrongWayBanner == null) return;
            // Hide after 2 seconds if no further wrong-way event
            // (handled externally: HandleWrongWay re-activates)
            _wrongWayActive = false;
            Invoke(nameof(HideWrongWayBanner), 2f);
        }

        private void HideWrongWayBanner()
        {
            if (_wrongWayBanner != null) _wrongWayBanner.SetActive(false);
        }

        private void ShowAlert(string message)
        {
            if (_alertText   != null) _alertText.text = message;
            if (_alertBanner != null) _alertBanner.SetActive(true);
            _alertTimer = CompetitiveRacingConfig.AlertBannerDuration;
        }

        private void SetPanelVisible(bool visible)
        {
            if (_fullHUDPanel    != null) _fullHUDPanel.SetActive(visible && !_isCompactMode);
            if (_compactHUDPanel != null) _compactHUDPanel.SetActive(visible && _isCompactMode);
        }

        private void HideHUD() => SetPanelVisible(false);

        private static string FormatTime(float seconds)
        {
            int m  = (int)seconds / 60;
            int s  = (int)seconds % 60;
            int ms = (int)((seconds % 1f) * 100);
            return $"{m}:{s:00}.{ms:00}";
        }
    }
}
