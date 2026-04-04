// Phase 99 — Seasonal Live Events & Battle Pass
// Assets/SWEF/Scripts/SeasonalEvents/BattlePassReward.cs
using System;
using UnityEngine;

namespace SWEF.SeasonalEvents
{
    /// <summary>
    /// Categories of reward that a battle-pass tier can grant.
    /// </summary>
    public enum RewardType
    {
        /// <summary>A custom livery / paint scheme for an aircraft.</summary>
        AircraftSkin,
        /// <summary>A custom contrail visual effect.</summary>
        ContrailEffect,
        /// <summary>A decorative item displayed inside the cockpit.</summary>
        CockpitDecor,
        /// <summary>A display title shown next to the player's name.</summary>
        Title,
        /// <summary>In-game soft currency awarded directly.</summary>
        Currency,
        /// <summary>A temporary XP multiplier boost.</summary>
        XPBoost,
        /// <summary>A post-processing photo filter for in-game photography.</summary>
        PhotoFilter,
        /// <summary>An additional music track for the adaptive music system.</summary>
        MusicTrack
    }

    /// <summary>
    /// Rarity tier for a reward, controlling visual presentation.
    /// </summary>
    public enum RewardRarity
    {
        /// <summary>Standard grey presentation.</summary>
        Common,
        /// <summary>Green accent presentation.</summary>
        Uncommon,
        /// <summary>Blue accent presentation.</summary>
        Rare,
        /// <summary>Purple glow presentation.</summary>
        Epic,
        /// <summary>Gold particle presentation.</summary>
        Legendary
    }

    /// <summary>
    /// Data class describing a single reward that can be granted from a battle-pass tier
    /// or season-end ceremony.
    /// </summary>
    [Serializable]
    public class BattlePassReward
    {
        /// <summary>Unique identifier for this reward (e.g. "skin_winter_hawk").</summary>
        public string RewardId;

        /// <summary>Category of this reward.</summary>
        public RewardType RewardType;

        /// <summary>Localised display name shown in the UI.</summary>
        public string DisplayName;

        /// <summary>Short description of what the reward is or does.</summary>
        public string Description;

        /// <summary>Resource path used to load the reward's icon sprite.</summary>
        public string IconPath;

        /// <summary>Rarity tier controlling visual effects and presentation.</summary>
        public RewardRarity Rarity;

        /// <summary>
        /// For <see cref="RewardType.Currency"/>: amount of currency granted.
        /// For <see cref="RewardType.XPBoost"/>: XP multiplier (e.g. 1.5 = 50 % bonus).
        /// Ignored for other reward types.
        /// </summary>
        public float QuantityOrMultiplier = 1f;
    }
}
