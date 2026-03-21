using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// Reward type categories for season pass tier rewards.
    /// </summary>
    public enum SeasonRewardType
    {
        XP,
        Currency,
        Cosmetic,
        Title,
        SkillPoint
    }

    /// <summary>
    /// Describes a single tier reward on the free or premium season track.
    /// </summary>
    [Serializable]
    public struct SeasonReward
    {
        /// <summary>Tier number this reward belongs to (1-based).</summary>
        public int tier;

        /// <summary>Category of reward granted.</summary>
        public SeasonRewardType rewardType;

        /// <summary>
        /// Resource / cosmetic / title identifier.
        /// Unused for XP and Currency types.
        /// </summary>
        public string rewardId;

        /// <summary>Numeric quantity (XP amount, coin amount, skill-point count, etc.).</summary>
        public int amount;

        /// <summary>Localization key for the reward display name.</summary>
        public string displayNameKey;
    }

    /// <summary>
    /// ScriptableObject that defines a full season (battle-pass cycle).
    /// Place instances under <c>Resources/Seasons/</c> for automatic loading.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/DailyChallenge/Season Definition", fileName = "NewSeasonDefinition")]
    public class SeasonDefinition : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        [Header("Identity")]
        /// <summary>Unique stable identifier (e.g. "season_1").</summary>
        [SerializeField] public string seasonId;

        /// <summary>Localization key for the season display name.</summary>
        [SerializeField] public string seasonNameKey;

        /// <summary>Localization key for the season description.</summary>
        [SerializeField] public string seasonDescriptionKey;

        // ── Schedule ──────────────────────────────────────────────────────────────

        [Header("Schedule (ISO 8601 UTC)")]
        /// <summary>Season start date/time in ISO 8601 format (UTC).</summary>
        [SerializeField] public string startDate;

        /// <summary>Season end date/time in ISO 8601 format (UTC).</summary>
        [SerializeField] public string endDate;

        // ── Progression ───────────────────────────────────────────────────────────

        [Header("Progression")]
        /// <summary>Total number of tiers in this season (default 50).</summary>
        [SerializeField] public int totalTiers = 50;

        /// <summary>Season points needed to advance one tier (default 100).</summary>
        [SerializeField] public int pointsPerTier = 100;

        // ── Presentation ──────────────────────────────────────────────────────────

        [Header("Presentation")]
        /// <summary>Primary theme colour for the season UI.</summary>
        [SerializeField] public Color themeColor = Color.cyan;

        /// <summary>Optional theme icon sprite (nullable).</summary>
        [SerializeField] public Sprite themeIcon;

        // ── Rewards ───────────────────────────────────────────────────────────────

        [Header("Rewards")]
        /// <summary>One reward per tier for the free track (index 0 = tier 1).</summary>
        [SerializeField] public List<SeasonReward> freeRewards = new List<SeasonReward>();

        /// <summary>One reward per tier for the premium track (index 0 = tier 1).</summary>
        [SerializeField] public List<SeasonReward> premiumRewards = new List<SeasonReward>();

        // ── Convenience ───────────────────────────────────────────────────────────

        /// <summary>Parses <see cref="startDate"/> and returns a UTC DateTime.</summary>
        public DateTime GetStartDateUtc() =>
            DateTime.TryParse(startDate, out var dt) ? dt.ToUniversalTime() : DateTime.MinValue;

        /// <summary>Parses <see cref="endDate"/> and returns a UTC DateTime.</summary>
        public DateTime GetEndDateUtc() =>
            DateTime.TryParse(endDate, out var dt) ? dt.ToUniversalTime() : DateTime.MaxValue;
    }
}
