// RewardData.cs — SWEF Dynamic Event & World Quest System (Phase 64)
using System;
using System.Collections.Generic;

namespace SWEF.WorldEvent
{
    /// <summary>
    /// Fully describes the rewards granted to a player upon completing a world event or
    /// quest chain.  Serialised as part of <see cref="WorldEventData"/> and
    /// <see cref="QuestChain"/> assets.
    /// </summary>
    [Serializable]
    public sealed class RewardData
    {
        /// <summary>Experience points awarded on completion.</summary>
        public int experiencePoints;

        /// <summary>In-game currency units awarded on completion.</summary>
        public int currency;

        /// <summary>
        /// Item IDs unlocked for the player's inventory on completion.
        /// Each entry is matched against the item registry by string key.
        /// </summary>
        public List<string> unlockedItems = new List<string>();

        /// <summary>
        /// Optional achievement identifier granted on completion.
        /// Empty string means no achievement is linked.
        /// </summary>
        public string achievementId = string.Empty;

        /// <summary>Reputation points added to the player's standing on completion.</summary>
        public float reputationBonus;
    }
}
