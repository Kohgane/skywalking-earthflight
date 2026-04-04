// Phase 100 — AI Co-Pilot & Smart Assistant
// Assets/SWEF/Scripts/AICoPilot/SmartAutopilotBridge.cs
using System;
using UnityEngine;

namespace SWEF.AICoPilot
{
    /// <summary>
    /// Bridge between the AI Co-Pilot system and the SWEF.Autopilot module.
    /// Enables AI-suggested autopilot engagement, monitors autopilot behaviour,
    /// and manages "I have the controls" / "Take the controls" handoff interactions.
    /// </summary>
    [DisallowMultipleComponent]
    public class SmartAutopilotBridge : MonoBehaviour
    {
        #region Events

        /// <summary>
        /// Fired when control is handed off between the pilot and the AI autopilot.
        /// </summary>
        /// <param name="toAI">
        /// <c>true</c> if AI is taking control; <c>false</c> if control is returning to the pilot.
        /// </param>
        public event Action<bool> OnAutopilotHandoff;

        /// <summary>
        /// Fired when the autopilot behaves unexpectedly or deviates from expected flight path.
        /// </summary>
        /// <param name="description">Human-readable description of the anomaly.</param>
        public event Action<string> OnAutopilotAnomaly;

        #endregion

        #region Inspector

        [Header("Engagement Thresholds")]
        [Tooltip("Altitude AGL (m) above which AI may suggest engaging autopilot.")]
        [SerializeField] private float _minCruiseAltitudeM = 1500f;

        [Tooltip("Minimum cruise segment duration (seconds) before AI suggests autopilot.")]
        [SerializeField] private float _minCruiseDurationSuggestionSec = 300f;

        [Header("Anomaly Detection")]
        [Tooltip("Max heading deviation (degrees) from planned route before anomaly is flagged.")]
        [SerializeField] private float _headingAnomalyDeg = 20f;

        [Tooltip("Max altitude deviation (metres) from target before anomaly is flagged.")]
        [SerializeField] private float _altitudeAnomalyM = 200f;

        [Header("Cooldowns")]
        [Tooltip("Seconds between repeated autopilot engagement suggestions.")]
        [SerializeField] private float _suggestionCooldownSeconds = 120f;

        #endregion

        #region Private State

        private bool _autopilotActive;
        private bool _aiHasControl;
        private float _cruiseStartTime = -1f;
        private float _lastSuggestionTime = -1000f;

        // Simulated autopilot state — replace with SWEF.Autopilot bindings.
        private float _currentHeading;
        private float _targetHeading;
        private float _currentAltitude;
        private float _targetAltitude;
        private float _altitudeAGL;

        #endregion

        #region Properties

        /// <summary>Returns true if the autopilot is currently engaged.</summary>
        public bool AutopilotActive => _autopilotActive;

        /// <summary>Returns true if the AI co-pilot currently holds primary control.</summary>
        public bool AIHasControl => _aiHasControl;

        #endregion

        #region Public API

        /// <summary>
        /// Called periodically by <see cref="AICoPilotManager"/> or Update to evaluate
        /// autopilot state and issue suggestions/anomaly alerts.
        /// </summary>
        /// <param name="altitudeAGL">Current altitude above ground level in metres.</param>
        /// <param name="currentHeading">Current aircraft heading (degrees true).</param>
        /// <param name="targetHeading">Autopilot target heading (degrees true).</param>
        /// <param name="currentAltitude">Current barometric altitude (metres).</param>
        /// <param name="targetAltitude">Autopilot target altitude (metres).</param>
        /// <param name="autopilotEngaged">Whether the autopilot is currently engaged.</param>
        public void Evaluate(float altitudeAGL, float currentHeading, float targetHeading,
                             float currentAltitude, float targetAltitude, bool autopilotEngaged)
        {
            _altitudeAGL     = altitudeAGL;
            _currentHeading  = currentHeading;
            _targetHeading   = targetHeading;
            _currentAltitude = currentAltitude;
            _targetAltitude  = targetAltitude;
            _autopilotActive = autopilotEngaged;

            TrackCruiseTime();

            if (!_autopilotActive)
            {
                SuggestAutopilotIfAppropriate();
            }
            else
            {
                CheckForAnomalies();
            }
        }

        /// <summary>
        /// AI requests to take control ("I have the controls").
        /// The pilot must confirm via the UI before handoff occurs.
        /// </summary>
        public void RequestAIControl()
        {
            if (_aiHasControl) return;
            _aiHasControl = true;
            OnAutopilotHandoff?.Invoke(true);

            var dialogue = AICoPilotDialogueManager.Instance;
            dialogue?.EnqueueMessage("ARIA", "I have the controls. Autopilot engaged.", MessagePriority.Info);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[SmartAutopilotBridge] AI has the controls.");
#endif
        }

        /// <summary>
        /// Pilot reclaims control ("Take the controls" / "Your controls").
        /// </summary>
        public void ReturnControlToPilot()
        {
            if (!_aiHasControl) return;
            _aiHasControl = false;
            OnAutopilotHandoff?.Invoke(false);

            var dialogue = AICoPilotDialogueManager.Instance;
            dialogue?.EnqueueMessage("ARIA", "You have the controls. Autopilot disengaged.", MessagePriority.Info);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[SmartAutopilotBridge] Pilot has the controls.");
#endif
        }

        #endregion

        #region Private Methods

        private void TrackCruiseTime()
        {
            if (_altitudeAGL >= _minCruiseAltitudeM)
            {
                if (_cruiseStartTime < 0f)
                    _cruiseStartTime = Time.time;
            }
            else
            {
                _cruiseStartTime = -1f;
            }
        }

        private void SuggestAutopilotIfAppropriate()
        {
            if (_cruiseStartTime < 0f) return;
            if (Time.time - _lastSuggestionTime < _suggestionCooldownSeconds) return;

            float cruiseDuration = Time.time - _cruiseStartTime;
            if (cruiseDuration < _minCruiseDurationSuggestionSec) return;

            _lastSuggestionTime = Time.time;

            var dialogue = AICoPilotDialogueManager.Instance;
            dialogue?.EnqueueMessage("ARIA",
                "We're in a stable cruise segment. Would you like me to engage autopilot?",
                MessagePriority.Info);
        }

        private void CheckForAnomalies()
        {
            float headingDelta = Mathf.Abs(AngleDelta(_currentHeading, _targetHeading));
            if (headingDelta > _headingAnomalyDeg)
            {
                string desc = $"Autopilot heading deviation: {headingDelta:F0}° off target.";
                OnAutopilotAnomaly?.Invoke(desc);

                var dialogue = AICoPilotDialogueManager.Instance;
                dialogue?.EnqueueMessage("Autopilot", desc, MessagePriority.Warning);
            }

            float altDelta = Mathf.Abs(_currentAltitude - _targetAltitude);
            if (altDelta > _altitudeAnomalyM)
            {
                string desc = $"Autopilot altitude deviation: {altDelta:F0} m off target.";
                OnAutopilotAnomaly?.Invoke(desc);

                var dialogue = AICoPilotDialogueManager.Instance;
                dialogue?.EnqueueMessage("Autopilot", desc, MessagePriority.Warning);
            }
        }

        private static float AngleDelta(float from, float to)
        {
            return (to - from + 540f) % 360f - 180f;
        }

        #endregion
    }
}
