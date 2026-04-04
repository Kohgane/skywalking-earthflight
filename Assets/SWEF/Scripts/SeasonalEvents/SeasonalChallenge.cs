// Phase 99 — Seasonal Live Events & Battle Pass
// Assets/SWEF/Scripts/SeasonalEvents/SeasonalChallenge.cs
using System;
using UnityEngine;

namespace SWEF.SeasonalEvents
{
    /// <summary>
    /// Broad categories of task a seasonal challenge can represent.
    /// </summary>
    public enum ChallengeType
    {
        /// <summary>Fly to a specific geographic coordinate.</summary>
        FlyToLocation,
        /// <summary>Accumulate a target total distance flown (kilometres).</summary>
        FlyDistance,
        /// <summary>Reach or exceed a target altitude (metres).</summary>
        AchieveAltitude,
        /// <summary>Complete a flight during specific in-game weather conditions.</summary>
        WeatherFlight,
        /// <summary>Complete a point-to-point route within a time limit.</summary>
        TimeTrial,
        /// <summary>Capture a qualifying in-game photograph.</summary>
        PhotoChallenge,
        /// <summary>Fly in close proximity to other players.</summary>
        FormationFlight
    }

    /// <summary>
    /// Data class for a single seasonal challenge (daily or weekly).
    /// </summary>
    [Serializable]
    public class SeasonalChallenge
    {
        /// <summary>Unique identifier for this challenge (e.g. "wk01_fly_himalaya").</summary>
        public string ChallengeId;

        /// <summary>Localised display title shown in the challenge list.</summary>
        public string Title;

        /// <summary>Brief explanation of what the player must accomplish.</summary>
        public string Description;

        /// <summary>Amount of battle-pass XP awarded upon completion.</summary>
        public int XPReward;

        /// <summary>Type / category of this challenge.</summary>
        public ChallengeType Type;

        /// <summary>
        /// Numeric or string target value, interpreted according to <see cref="Type"/>.
        /// E.g. for <see cref="ChallengeType.FlyDistance"/> this would be "500" (km).
        /// </summary>
        public string Target;

        /// <summary>Player's current progress value (0 … <see cref="Target"/>).</summary>
        public float Progress;

        /// <summary><c>true</c> once the challenge completion condition has been met.</summary>
        public bool IsCompleted;

        /// <summary>
        /// Optional bonus XP granted for season-themed challenges.
        /// Added on top of <see cref="XPReward"/>.
        /// </summary>
        public int BonusXP;

        /// <summary>UTC date/time when this challenge expires, ISO 8601 string.</summary>
        public string ExpiresAt;

        /// <summary>Returns <c>true</c> if this challenge has passed its expiry.</summary>
        public bool IsExpired()
        {
            if (string.IsNullOrEmpty(ExpiresAt)) return false;
            return DateTime.TryParse(ExpiresAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                && DateTime.UtcNow >= dt.ToUniversalTime();
        }

        /// <summary>Total XP earned for this challenge (base + bonus).</summary>
        public int TotalXP => XPReward + BonusXP;
    }
}
