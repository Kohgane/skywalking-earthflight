// LandingCoachSystem.cs — Phase 120: Precision Landing Challenge System
// AI coach: real-time feedback during approach, post-landing tips.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — AI landing coach that provides real-time verbal/text feedback
    /// during the approach ("too high", "increase speed") and post-landing tips.
    /// Uses a cooldown to avoid message spam.
    /// </summary>
    public class LandingCoachSystem : MonoBehaviour
    {
        // ── Coach Message ─────────────────────────────────────────────────────

        [System.Serializable]
        public class CoachMessage
        {
            public string Text;
            public float  Urgency; // 0 = informational, 1 = critical
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Coaching Settings")]
        [SerializeField] private float messageCooldownSec = 5f;
        [SerializeField] private bool  voiceFeedbackEnabled = true;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly Queue<CoachMessage> _messageQueue = new Queue<CoachMessage>();
        private float                        _lastMessageTime = -999f;
        private bool                         _isActive;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when the coach has a new message to display/speak.</summary>
        public event System.Action<CoachMessage> OnCoachMessage;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Whether the coach is currently monitoring.</summary>
        public bool IsActive => _isActive;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Activate the coach for the current challenge session.</summary>
        public void Activate() { _isActive = true; _messageQueue.Clear(); }

        /// <summary>Deactivate the coach.</summary>
        public void Deactivate() => _isActive = false;

        /// <summary>
        /// Feed real-time approach data to generate coaching feedback.
        /// </summary>
        public void EvaluateApproach(float glideSlopeDots, float locDots,
                                      float speedKnots, float targetSpeedKnots,
                                      float altFeetAGL, bool gearDown)
        {
            if (!_isActive) return;

            if (Mathf.Abs(glideSlopeDots) > 1f)
                Enqueue(glideSlopeDots > 0 ? "Too high — descend to glideslope" : "Too low — climb to glideslope", glideSlopeDots > 1.5f ? 0.9f : 0.5f);

            if (Mathf.Abs(locDots) > 0.75f)
                Enqueue(locDots > 0 ? "Left of centreline — correct right" : "Right of centreline — correct left", 0.6f);

            float speedDev = speedKnots - targetSpeedKnots;
            if (Mathf.Abs(speedDev) > 10f)
                Enqueue(speedDev > 0 ? "Too fast — reduce speed" : "Too slow — increase speed", 0.7f);

            if (altFeetAGL < 2000f && !gearDown)
                Enqueue("Gear down!", 1f);
        }

        /// <summary>Generate a post-landing coaching tip based on the result.</summary>
        public List<string> GeneratePostLandingTips(LandingResult result)
        {
            var tips = new List<string>();
            if (result == null) return tips;

            if (Mathf.Abs(result.CenterlineDeviationMetres) > 10f)
                tips.Add("Work on centreline tracking — try using gentle rudder inputs on final.");

            if (Mathf.Abs(result.SinkRateFPM) > 500f)
                tips.Add("Sink rate was high — arrest the descent earlier in the flare.");

            if (result.BounceCount > 0)
                tips.Add("Avoid bouncing by holding a stable speed and flaring smoothly.");

            if (result.Grade == LandingGrade.Perfect || result.Grade == LandingGrade.Excellent)
                tips.Add("Outstanding approach! Try a higher difficulty challenge next.");

            return tips;
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (!_isActive || _messageQueue.Count == 0) return;
            if (Time.time - _lastMessageTime < messageCooldownSec) return;

            var msg = _messageQueue.Dequeue();
            _lastMessageTime = Time.time;
            OnCoachMessage?.Invoke(msg);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Enqueue(string text, float urgency)
        {
            _messageQueue.Enqueue(new CoachMessage { Text = text, Urgency = urgency });
        }
    }
}
