// Phase 99 — Seasonal Live Events & Battle Pass
// Assets/SWEF/Scripts/SeasonalEvents/BattlePassTier.cs
using System;
using UnityEngine;

namespace SWEF.SeasonalEvents
{
    /// <summary>
    /// Data class representing a single tier in the battle pass.
    /// Each tier has a required XP threshold and can grant one free and one premium reward.
    /// </summary>
    [Serializable]
    public class BattlePassTier
    {
        /// <summary>1-based tier number (e.g. 1–50).</summary>
        public int TierNumber;

        /// <summary>
        /// Cumulative XP required to reach this tier from the start of the season.
        /// E.g. tier 1 = 0, tier 2 = 500, tier 3 = 1000, …
        /// </summary>
        public int RequiredXP;

        /// <summary>
        /// Reward granted on the free track when this tier is unlocked.
        /// May be <c>null</c> if no free reward is offered for this tier.
        /// </summary>
        public BattlePassReward FreeReward;

        /// <summary>
        /// Reward granted on the premium track when this tier is unlocked.
        /// May be <c>null</c> if no premium reward is offered for this tier.
        /// Requires the player to own the premium pass.
        /// </summary>
        public BattlePassReward PremiumReward;

        /// <summary>
        /// Returns the reward appropriate for the given pass type.
        /// </summary>
        /// <param name="isPremium">
        /// <c>true</c> to return the premium reward; <c>false</c> for the free reward.
        /// </param>
        public BattlePassReward GetReward(bool isPremium) =>
            isPremium ? PremiumReward : FreeReward;
    }
}
