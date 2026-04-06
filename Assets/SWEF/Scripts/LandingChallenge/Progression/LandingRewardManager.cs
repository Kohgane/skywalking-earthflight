// LandingRewardManager.cs — Phase 120: Precision Landing Challenge System
// Rewards: XP, currency, exclusive liveries, achievements, titles.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages rewards granted after a landing challenge completion.
    /// Distributes XP, currency, exclusive liveries for perfect landings,
    /// achievement unlocks, and special pilot titles.
    /// </summary>
    public class LandingRewardManager : MonoBehaviour
    {
        // ── Reward Data ───────────────────────────────────────────────────────

        [System.Serializable]
        public class RewardPackage
        {
            public int    XP;
            public int    Currency;
            public string LiveryId;
            public string TitleAwarded;
            public List<string> AchievementIds = new List<string>();
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Reward Configuration")]
        [SerializeField] private int baseXpPerStar       = 100;
        [SerializeField] private int baseCurrencyPerStar = 200;
        [SerializeField] private int perfectGradeXpBonus = 300;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when a reward package is distributed.</summary>
        public event System.Action<RewardPackage> OnRewardGranted;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Build and distribute a reward package based on challenge result.
        /// </summary>
        public RewardPackage GrantReward(ChallengeDefinition challenge, LandingResult result)
        {
            var pkg = new RewardPackage();

            // XP and currency
            pkg.XP       = result.Stars * baseXpPerStar;
            pkg.Currency = result.Stars * baseCurrencyPerStar;

            if (result.Grade == LandingGrade.Perfect)
            {
                pkg.XP       += perfectGradeXpBonus;
                pkg.LiveryId  = GetPerfectLivery(challenge.Type);
                pkg.TitleAwarded = GetTitle(challenge.Type);
            }

            // Achievement IDs
            if (result.Stars >= 1) pkg.AchievementIds.Add($"landing_{challenge.ChallengeId}_1star");
            if (result.Stars >= 2) pkg.AchievementIds.Add($"landing_{challenge.ChallengeId}_2star");
            if (result.Stars >= 3) pkg.AchievementIds.Add($"landing_{challenge.ChallengeId}_3star");
            if (result.Grade == LandingGrade.Perfect)
                pkg.AchievementIds.Add($"landing_{challenge.ChallengeId}_perfect");

            OnRewardGranted?.Invoke(pkg);
            return pkg;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private string GetPerfectLivery(ChallengeType type)
        {
            switch (type)
            {
                case ChallengeType.CarrierLanding:  return "livery_carrier_ace";
                case ChallengeType.MountainApproach: return "livery_mountain_goat";
                case ChallengeType.CrosswindLanding: return "livery_crosswind_king";
                case ChallengeType.NightLanding:    return "livery_night_owl";
                default:                            return "livery_precision_pilot";
            }
        }

        private string GetTitle(ChallengeType type)
        {
            switch (type)
            {
                case ChallengeType.CarrierLanding:  return "Carrier Ace";
                case ChallengeType.MountainApproach: return "Mountain Goat";
                case ChallengeType.CrosswindLanding: return "Crosswind King";
                case ChallengeType.NightLanding:    return "Night Owl";
                case ChallengeType.EmergencyLanding: return "Emergency Ace";
                default:                            return "Precision Pilot";
            }
        }
    }
}
