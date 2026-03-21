using UnityEngine;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// Types of activity that a daily/weekly challenge can target.
    /// </summary>
    public enum ChallengeType
    {
        FlyDistance,
        ReachAltitude,
        FlyDuration,
        VisitLocations,
        TakePhotos,
        CompleteFormation,
        AchieveSpeed,
        CompleteTour,
        PlayMultiplayer,
        UseSkill
    }

    /// <summary>
    /// Difficulty tiers for challenges.
    /// </summary>
    public enum ChallengeDifficulty
    {
        Easy,
        Medium,
        Hard,
        Elite
    }

    /// <summary>
    /// ScriptableObject that defines a single daily challenge template.
    /// Place instances under <c>Resources/DailyChallenges/</c> for automatic loading.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/DailyChallenge/Challenge Definition", fileName = "NewDailyChallengeDefinition")]
    public class DailyChallengeDefinition : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        [Header("Identity")]
        /// <summary>Unique stable identifier (e.g. "fly_5km_easy").</summary>
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
        /// Target value in natural units:
        /// distance km | altitude m | duration sec | count | speed km/h
        /// </summary>
        [SerializeField] public float targetValue;

        /// <summary>Difficulty tier used when the daily set is assembled.</summary>
        [SerializeField] public ChallengeDifficulty difficulty;

        // ── Rewards ───────────────────────────────────────────────────────────────

        [Header("Rewards")]
        /// <summary>Base XP awarded on completion (before streak multiplier).</summary>
        [SerializeField] public int baseXPReward;

        /// <summary>Virtual-currency (Sky Coins) awarded on completion.</summary>
        [SerializeField] public int baseCurrencyReward;

        /// <summary>Season-pass points awarded on completion.</summary>
        [SerializeField] public int seasonPointReward;

        // ── Presentation ──────────────────────────────────────────────────────────

        [Header("Presentation")]
        /// <summary>Tint colour used on the challenge card UI.</summary>
        [SerializeField] public Color iconColor = Color.white;

        // ── Gating ────────────────────────────────────────────────────────────────

        [Header("Gating")]
        /// <summary>Minimum pilot rank level required to see this challenge (1 = all players).</summary>
        [SerializeField] public int requiredRankLevel = 1;
    }
}
