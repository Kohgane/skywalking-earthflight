using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Progression
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Broad tier grouping for pilot ranks.
    /// </summary>
    public enum RankTier
    {
        /// <summary>Ranks 1–5.</summary>
        Trainee,
        /// <summary>Ranks 6–12.</summary>
        Cadet,
        /// <summary>Ranks 13–20.</summary>
        Pilot,
        /// <summary>Ranks 21–28.</summary>
        Captain,
        /// <summary>Ranks 29–36.</summary>
        Commander,
        /// <summary>Ranks 37–42.</summary>
        Ace,
        /// <summary>Ranks 43–48.</summary>
        Legend,
        /// <summary>Ranks 49–50.</summary>
        Skywalker
    }

    /// <summary>
    /// Type of reward granted when reaching a particular rank.
    /// </summary>
    public enum RankRewardType
    {
        /// <summary>A visual cosmetic item.</summary>
        Cosmetic,
        /// <summary>A player title displayed in the profile.</summary>
        Title,
        /// <summary>A skill-tree point or node unlock.</summary>
        Skill,
        /// <summary>An in-game feature or mode unlock.</summary>
        Feature
    }

    // ── Structs ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single reward granted upon reaching a pilot rank.
    /// </summary>
    [Serializable]
    public struct RankReward
    {
        /// <summary>Reward type.</summary>
        public RankRewardType type;
        /// <summary>Unique identifier for the reward item.</summary>
        public string id;
        /// <summary>Localization key for the reward display name.</summary>
        public string displayNameKey;
    }

    // ── ScriptableObject ─────────────────────────────────────────────────────────

    /// <summary>
    /// Defines a single pilot rank level, its XP threshold, tier, icon, colour,
    /// and the rewards unlocked upon reaching it.
    /// Create via <c>Assets → Create → SWEF → Progression → PilotRankData</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Progression/PilotRankData", fileName = "PilotRankData")]
    public class PilotRankData : ScriptableObject
    {
        // ── Identity ───────────────────────────────────────────────────────────

        [Header("Identity")]
        /// <summary>Unique string identifier for this rank (e.g. "rank_01").</summary>
        [SerializeField] public string rankId;

        /// <summary>Human-readable rank name (English fallback).</summary>
        [SerializeField] public string rankName;

        /// <summary>Localization key used to look up the translated rank name.</summary>
        [SerializeField] public string rankNameKey;

        // ── Level & XP ────────────────────────────────────────────────────────

        [Header("Level & XP")]
        /// <summary>Numeric level 1–50.</summary>
        [SerializeField] public int rankLevel;

        /// <summary>Cumulative XP required to reach this rank.</summary>
        [SerializeField] public long requiredXP;

        // ── Visual ────────────────────────────────────────────────────────────

        [Header("Visual")]
        /// <summary>Tier this rank belongs to.</summary>
        [SerializeField] public RankTier rankTier;

        /// <summary>Optional sprite displayed as a rank badge.</summary>
        [SerializeField] public Sprite rankIcon;

        /// <summary>Colour associated with this rank (used for UI theming).</summary>
        [SerializeField] public Color rankColor = Color.white;

        // ── Rewards ───────────────────────────────────────────────────────────

        [Header("Unlock Rewards")]
        /// <summary>Items unlocked when the player first reaches this rank.</summary>
        [SerializeField] public List<RankReward> unlockRewards = new List<RankReward>();
    }
}
