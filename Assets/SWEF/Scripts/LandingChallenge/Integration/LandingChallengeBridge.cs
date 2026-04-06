// LandingChallengeBridge.cs — Phase 120: Precision Landing Challenge System
// Integration with existing SWEF systems: Flight, Weather, Achievement, Academy, Analytics, CloudSave.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

#if SWEF_LANDING_CHALLENGE_AVAILABLE
using SWEF.Flight;
using SWEF.Achievement;
using SWEF.Analytics;
#endif

#if SWEF_ACADEMY_AVAILABLE
using SWEF.Academy;
#endif

#if SWEF_CLOUD_SAVE_AVAILABLE
using SWEF.CloudSave;
#endif

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Bridge that connects the Precision Landing Challenge System to
    /// other SWEF subsystems (Flight, Weather, Achievement, Academy, Analytics,
    /// CloudSave) using compile-time feature guards.
    /// </summary>
    public class LandingChallengeBridge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Bridge Toggles")]
        [SerializeField] private bool connectToFlight       = true;
        [SerializeField] private bool connectToAchievements = true;
        [SerializeField] private bool connectToAnalytics    = true;
        [SerializeField] private bool connectToCloudSave    = true;
        [SerializeField] private bool connectToAcademy      = true;

        // ── State ─────────────────────────────────────────────────────────────

        private LandingChallengeManager _challengeManager;
        private LandingRewardManager    _rewardManager;
        private LandingChallengeAnalytics _analytics;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _challengeManager = LandingChallengeManager.Instance;
            _rewardManager    = GetComponentInChildren<LandingRewardManager>();
            _analytics        = GetComponentInChildren<LandingChallengeAnalytics>();
        }

        private void OnEnable()
        {
            if (_challengeManager == null) return;
            _challengeManager.OnChallengeStarted    += HandleChallengeStarted;
            _challengeManager.OnLandingScored       += HandleLandingScored;
            _challengeManager.OnChallengeCompleted  += HandleChallengeCompleted;
            _challengeManager.OnChallengeFailed     += HandleChallengeFailed;
        }

        private void OnDisable()
        {
            if (_challengeManager == null) return;
            _challengeManager.OnChallengeStarted    -= HandleChallengeStarted;
            _challengeManager.OnLandingScored       -= HandleLandingScored;
            _challengeManager.OnChallengeCompleted  -= HandleChallengeCompleted;
            _challengeManager.OnChallengeFailed     -= HandleChallengeFailed;
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void HandleChallengeStarted(ChallengeDefinition def)
        {
            _analytics?.TrackChallengeStarted(def.ChallengeId, def.Type, def.Difficulty);
            Debug.Log($"[LandingChallengeBridge] Challenge started: {def.ChallengeId}");
        }

        private void HandleLandingScored(LandingResult result)
        {
            _analytics?.TrackLandingScored(result.TotalScore, result.Grade);

#if SWEF_LANDING_CHALLENGE_AVAILABLE
            if (connectToAnalytics)
            {
                // Forward to SWEF analytics here
            }
#endif
        }

        private void HandleChallengeCompleted(ChallengeDefinition def, LandingResult result)
        {
            _analytics?.TrackChallengeCompleted(def.ChallengeId, result.TotalScore, result.Stars);

#if SWEF_LANDING_CHALLENGE_AVAILABLE
            if (connectToAchievements)
            {
                // Unlock achievements via AchievementManager here
            }
#endif

#if SWEF_ACADEMY_AVAILABLE
            if (connectToAcademy)
            {
                // Notify FlightAcademyManager of landing completion for certification tracking
            }
#endif

#if SWEF_CLOUD_SAVE_AVAILABLE
            if (connectToCloudSave)
            {
                // Trigger cloud save sync after challenge completion
            }
#endif
        }

        private void HandleChallengeFailed(ChallengeDefinition def)
        {
            _analytics?.TrackChallengeFailed(def.ChallengeId);
        }
    }
}
