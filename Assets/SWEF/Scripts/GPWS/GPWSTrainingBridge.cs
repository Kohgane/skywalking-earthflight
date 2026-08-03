using UnityEngine;

#if SWEF_FLIGHTSCHOOL_AVAILABLE
using SWEF.FlightSchool;
#endif

#if SWEF_ANALYTICS_AVAILABLE
using SWEF.Analytics;
#endif

namespace SWEF.GPWS
{
    /// <summary>
    /// Phase 121 — Bridge between GPWS and the Flight School training system.
    ///
    /// <para>Tracks trainee responses to GPWS alerts (reaction time, correct escape
    /// manoeuvre) and reports objectives to
    /// <c>SWEF.FlightSchool.FlightSchoolManager</c> when
    /// <c>SWEF_FLIGHTSCHOOL_AVAILABLE</c> is defined.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class GPWSTrainingBridge : MonoBehaviour
    {
        #region Inspector

        [Header("Lesson Configuration")]
        [Tooltip("Lesson ID used for GPWS response training.")]
        [SerializeField] private string gpwsLessonId = "gpws_response_drill";

        [Tooltip("Maximum acceptable reaction time in seconds.")]
        [SerializeField] private float maxReactionTimeSec = 5f;

        #endregion

        #region Private State

        private GPWSManager _mgr;
        private float _alertTimestamp;
        private bool _awaitingResponse;
        private int _correctResponses;
        private int _totalAlerts;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _mgr = GPWSManager.Instance;
            if (_mgr == null) return;

            _mgr.OnGPWSAlert += HandleAlert;
            _mgr.OnGPWSAlertCleared += HandleAlertCleared;
        }

        private void OnDestroy()
        {
            if (_mgr != null)
            {
                _mgr.OnGPWSAlert -= HandleAlert;
                _mgr.OnGPWSAlertCleared -= HandleAlertCleared;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleAlert(GPWSAlert alert)
        {
            if (alert.level < GPWSAlertLevel.Warning) return;

            _alertTimestamp = Time.time;
            _awaitingResponse = true;
            _totalAlerts++;
        }

        private void HandleAlertCleared(GPWSAlert alert)
        {
            if (!_awaitingResponse) return;
            _awaitingResponse = false;

            float reactionTime = Time.time - _alertTimestamp;
            bool timely = reactionTime <= maxReactionTimeSec;
            if (timely) _correctResponses++;

            float score = timely ? Mathf.InverseLerp(maxReactionTimeSec, 0f, reactionTime) : 0f;

#if SWEF_FLIGHTSCHOOL_AVAILABLE
            var fsm = FlightSchoolManager.Instance;
            if (fsm != null)
            {
                fsm.CompleteObjective(gpwsLessonId, "gpws_reaction", score);
                if (_totalAlerts >= 5)
                {
                    float overallScore = (float)_correctResponses / _totalAlerts * 100f;
                    fsm.CompleteLesson(gpwsLessonId, overallScore);
                }
            }
#endif

#if SWEF_ANALYTICS_AVAILABLE
            var td = TelemetryDispatcher.Instance ?? FindFirstObjectByType<TelemetryDispatcher>();
            td?.Dispatch("gpws_reaction_time", reactionTime);
#endif
        }

        #endregion

        #region Public API

        /// <summary>Returns the trainee's current GPWS response score (0–100).</summary>
        public float GetScore()
        {
            return _totalAlerts > 0 ? (float)_correctResponses / _totalAlerts * 100f : 0f;
        }

        /// <summary>Resets training session counters.</summary>
        public void ResetSession()
        {
            _correctResponses = 0;
            _totalAlerts = 0;
            _awaitingResponse = false;
        }

        #endregion
    }
}
