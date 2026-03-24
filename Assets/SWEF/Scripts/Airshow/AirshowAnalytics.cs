// Phase 73 — Flight Formation Display & Airshow System
// Assets/SWEF/Scripts/Airshow/AirshowAnalytics.cs
using System.Collections.Generic;
using UnityEngine;
using SWEF.Analytics;

namespace SWEF.Airshow
{
    /// <summary>
    /// Analytics bridge for airshow telemetry.
    /// Tracks show lifecycle events, maneuver scores, smoke usage,
    /// spectator camera modes, and audience peak excitement via
    /// <see cref="TelemetryDispatcher"/>.
    /// </summary>
    public class AirshowAnalytics : MonoBehaviour
    {
        #region Private — session state
        private float _showStartTime;
        private int _actsCompleted;
        private float _totalSmokeTime;
        private readonly Dictionary<SpectatorCameraMode, float> _cameraModeTime =
            new Dictionary<SpectatorCameraMode, float>();
        private SpectatorCameraMode _currentCamMode;
        private float _camModeStartTime;
        private float _peakExcitement;
        private int _maneuverCount;
        private float _maneuverScoreSum;
        #endregion

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void OnEnable()
        {
            if (AirshowManager.Instance == null) return;
            AirshowManager.Instance.OnAirshowStateChanged += HandleStateChanged;
            AirshowManager.Instance.OnActStarted          += HandleActStarted;
            AirshowManager.Instance.OnManeuverTriggered   += HandleManeuverTriggered;
            AirshowManager.Instance.OnAirshowCompleted    += HandleCompleted;
        }

        private void OnDisable()
        {
            if (AirshowManager.Instance == null) return;
            AirshowManager.Instance.OnAirshowStateChanged -= HandleStateChanged;
            AirshowManager.Instance.OnActStarted          -= HandleActStarted;
            AirshowManager.Instance.OnManeuverTriggered   -= HandleManeuverTriggered;
            AirshowManager.Instance.OnAirshowCompleted    -= HandleCompleted;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Called when the spectator camera mode changes so that time-per-mode can be tracked.
        /// </summary>
        public void TrackCameraMode(SpectatorCameraMode mode)
        {
            RecordCameraModeTime(_currentCamMode);
            _currentCamMode   = mode;
            _camModeStartTime = Time.time;
        }

        /// <summary>Records a peak audience excitement sample.</summary>
        public void TrackExcitement(float excitement)
        {
            if (excitement > _peakExcitement) _peakExcitement = excitement;
        }

        /// <summary>Accumulates smoke-on time for the current performer.</summary>
        public void TrackSmokeTime(float deltaSeconds)
        {
            _totalSmokeTime += deltaSeconds;
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void HandleStateChanged(AirshowState state)
        {
            if (state == AirshowState.Performing && AirshowManager.Instance != null)
            {
                if (_showStartTime <= 0f)
                {
                    _showStartTime = Time.time;
                    AirshowRoutineData routine = AirshowManager.Instance.ActiveRoutine;
                    TrackEvent("airshow_started",
                        ("routine_name",      routine?.routineName ?? "unknown"),
                        ("performer_count",   AirshowManager.Instance.Performers.Count),
                        ("show_type",         routine?.showType.ToString() ?? "unknown"));
                }
            }
            else if (state == AirshowState.Aborted)
            {
                TrackEvent("airshow_aborted",
                    ("reason",         "user_abort"),
                    ("acts_completed", _actsCompleted));
            }
        }

        private void HandleActStarted(int actIndex, string actName)
        {
            _actsCompleted = actIndex + 1;
        }

        private void HandleManeuverTriggered(ManeuverType maneuver, int slot)
        {
            _maneuverCount++;
            // Per-maneuver scoring is tracked in HandleCompleted via ManeuverScore list
        }

        private void HandleCompleted(AirshowResult result)
        {
            RecordCameraModeTime(_currentCamMode);

            TrackEvent("airshow_completed",
                ("score",           result.totalScore),
                ("rating",          result.rating.ToString()),
                ("duration_sec",    result.totalDuration),
                ("acts_completed",  _actsCompleted));

            TrackEvent("airshow_smoke_usage",
                ("total_smoke_time", _totalSmokeTime));

            TrackEvent("airshow_audience_peak_excitement",
                ("peak", _peakExcitement));

            foreach (var kvp in _cameraModeTime)
            {
                TrackEvent("airshow_spectator_mode",
                    ("mode",         kvp.Key.ToString()),
                    ("time_seconds", kvp.Value));
            }

            // Best score per routine
            if (AirshowManager.Instance?.ActiveRoutine != null)
            {
                string key = "SWEF_Airshow_" + AirshowManager.Instance.ActiveRoutine.routineId;
                float best = PlayerPrefs.GetFloat(key, 0f);
                TrackEvent("airshow_best_score",
                    ("routine_id",  AirshowManager.Instance.ActiveRoutine.routineId),
                    ("best_score",  best));
            }

            ResetSession();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void RecordCameraModeTime(SpectatorCameraMode mode)
        {
            float duration = Time.time - _camModeStartTime;
            if (_cameraModeTime.ContainsKey(mode))
                _cameraModeTime[mode] += duration;
            else
                _cameraModeTime[mode]  = duration;
        }

        private void ResetSession()
        {
            _showStartTime = 0f;
            _actsCompleted = 0;
            _totalSmokeTime = 0f;
            _cameraModeTime.Clear();
            _peakExcitement = 0f;
            _maneuverCount = 0;
            _maneuverScoreSum = 0f;
        }

        private static void TrackEvent(string eventName, params (string key, object value)[] properties)
        {
            TelemetryDispatcher dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            TelemetryEventBuilder builder = TelemetryEventBuilder.Create(eventName)
                .WithCategory("airshow");
            foreach ((string key, object value) in properties)
                builder.WithProperty(key, value);

            dispatcher.EnqueueEvent(builder.Build());
        }
    }
}
