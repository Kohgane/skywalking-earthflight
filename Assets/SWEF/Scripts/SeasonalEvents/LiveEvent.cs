// Phase 99 — Seasonal Live Events & Battle Pass
// Assets/SWEF/Scripts/SeasonalEvents/LiveEvent.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SeasonalEvents
{
    /// <summary>
    /// Categories of limited-time live event.
    /// </summary>
    public enum LiveEventType
    {
        /// <summary>A special server-driven weather pattern is active in certain regions.</summary>
        WeatherEvent,
        /// <summary>A rare or hidden location has been revealed for a limited time.</summary>
        Discovery,
        /// <summary>All players contribute to a shared progress goal.</summary>
        CommunityGoal,
        /// <summary>A holiday-themed event with special rewards.</summary>
        Holiday,
        /// <summary>A time-limited global speed-run competition.</summary>
        SpeedRun,
        /// <summary>A community photo contest with voting and rewards.</summary>
        PhotoContest
    }

    /// <summary>
    /// A threshold within a community goal that grants a reward when reached.
    /// </summary>
    [Serializable]
    public class CommunityRewardTier
    {
        /// <summary>Community progress percentage (0–100) required to unlock this reward.</summary>
        public float ProgressPercent;

        /// <summary>Reward granted to all participants when the threshold is reached.</summary>
        public BattlePassReward Reward;
    }

    /// <summary>
    /// Data class for a limited-time live event within a season.
    /// </summary>
    [Serializable]
    public class LiveEvent
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        /// <summary>Unique stable identifier (e.g. "evt_2026_winter_storm").</summary>
        public string EventId;

        /// <summary>Localised display name shown in event banners and notifications.</summary>
        public string EventName;

        /// <summary>Broad category of this event.</summary>
        public LiveEventType Type;

        /// <summary>Optional detailed description of the event context and rules.</summary>
        public string Description;

        // ── Schedule ──────────────────────────────────────────────────────────────

        /// <summary>UTC start time of the event, ISO 8601 string.</summary>
        public string StartTime;

        /// <summary>UTC end time of the event, ISO 8601 string.</summary>
        public string EndTime;

        // ── Rewards ───────────────────────────────────────────────────────────────

        /// <summary>Rewards available to individual players for participating in this event.</summary>
        public List<BattlePassReward> Rewards = new List<BattlePassReward>();

        // ── Community Goal ────────────────────────────────────────────────────────

        /// <summary>
        /// For <see cref="LiveEventType.CommunityGoal"/>: the total value all players must
        /// collectively reach (e.g. total km flown, total photos taken).
        /// </summary>
        public float TargetValue;

        /// <summary>Current aggregated contribution from all players.</summary>
        public float CurrentValue;

        /// <summary>
        /// Ordered list of reward thresholds unlocked as community progress reaches
        /// each <see cref="CommunityRewardTier.ProgressPercent"/>.
        /// </summary>
        public List<CommunityRewardTier> RewardTiers = new List<CommunityRewardTier>();

        // ── Computed ──────────────────────────────────────────────────────────────

        /// <summary>Community progress expressed as a 0–1 fraction.</summary>
        public float CommunityProgressFraction =>
            TargetValue > 0f ? Mathf.Clamp01(CurrentValue / TargetValue) : 0f;

        /// <summary>Parses <see cref="StartTime"/> as a UTC <see cref="DateTime"/>.</summary>
        public DateTime GetStartTimeUtc()
        {
            if (string.IsNullOrEmpty(StartTime)) return DateTime.MinValue;
            return DateTime.TryParse(StartTime, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt.ToUniversalTime()
                : DateTime.MinValue;
        }

        /// <summary>Parses <see cref="EndTime"/> as a UTC <see cref="DateTime"/>.</summary>
        public DateTime GetEndTimeUtc()
        {
            if (string.IsNullOrEmpty(EndTime)) return DateTime.MaxValue;
            return DateTime.TryParse(EndTime, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt.ToUniversalTime()
                : DateTime.MaxValue;
        }

        /// <summary>
        /// Returns <c>true</c> when <see cref="DateTime.UtcNow"/> falls within the event window.
        /// </summary>
        public bool IsActive()
        {
            var now = DateTime.UtcNow;
            return now >= GetStartTimeUtc() && now < GetEndTimeUtc();
        }

        /// <summary>Remaining duration until the event ends, or <see cref="TimeSpan.Zero"/> if already ended.</summary>
        public TimeSpan TimeRemaining()
        {
            var remaining = GetEndTimeUtc() - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
