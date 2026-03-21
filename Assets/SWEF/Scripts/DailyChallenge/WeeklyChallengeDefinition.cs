using UnityEngine;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// ScriptableObject that defines a single weekly mega-challenge template.
    /// Weekly challenges are harder than daily challenges and offer larger rewards.
    /// Place instances under <c>Resources/WeeklyChallenges/</c> for automatic loading.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/DailyChallenge/Weekly Challenge Definition", fileName = "NewWeeklyChallengeDefinition")]
    public class WeeklyChallengeDefinition : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        [Header("Identity")]
        /// <summary>Unique stable identifier (e.g. "weekly_fly_300km").</summary>
        [SerializeField] public string challengeId;

        /// <summary>Localization key for the challenge title.</summary>
        [SerializeField] public string titleKey;

        /// <summary>Localization key for the challenge description.</summary>
        [SerializeField] public string descriptionKey;

        // ── Gameplay ──────────────────────────────────────────────────────────────

        [Header("Gameplay")]
        /// <summary>Category of activity required to complete this challenge.</summary>
        [SerializeField] public ChallengeType challengeType;

        /// <summary>
        /// Target value in natural units.
        /// Weekly targets are much higher than daily equivalents.
        /// </summary>
        [SerializeField] public float targetValue;

        // ── Rewards ───────────────────────────────────────────────────────────────

        [Header("Rewards")]
        /// <summary>XP awarded on completion.</summary>
        [SerializeField] public int xpReward;

        /// <summary>Virtual-currency (Sky Coins) awarded on completion.</summary>
        [SerializeField] public int currencyReward;

        /// <summary>Season-pass points awarded (typically 3× a daily equivalent).</summary>
        [SerializeField] public int seasonPointReward;

        /// <summary>Optional exclusive cosmetic item id granted on completion.</summary>
        [SerializeField] public string bonusCosmeticId;

        // ── Schedule ──────────────────────────────────────────────────────────────

        [Header("Schedule")]
        /// <summary>Number of days this challenge remains active (default 7).</summary>
        [SerializeField] public int validDurationDays = 7;
    }
}
