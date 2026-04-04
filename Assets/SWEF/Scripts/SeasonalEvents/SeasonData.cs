// Phase 99 — Seasonal Live Events & Battle Pass
// Assets/SWEF/Scripts/SeasonalEvents/SeasonData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SeasonalEvents
{
    /// <summary>
    /// Data class representing a single POI (point of interest) on the seasonal map.
    /// </summary>
    [Serializable]
    public class SeasonalMapMarker
    {
        /// <summary>Unique identifier for this POI.</summary>
        public string MarkerId;

        /// <summary>Display name of this POI.</summary>
        public string DisplayName;

        /// <summary>Latitude of the POI location.</summary>
        public double Latitude;

        /// <summary>Longitude of the POI location.</summary>
        public double Longitude;

        /// <summary>Optional description shown in the UI.</summary>
        public string Description;
    }

    /// <summary>
    /// Data class representing a season's full configuration.
    /// Serializable to JSON via <c>JsonUtility</c>.
    /// </summary>
    [Serializable]
    public class SeasonData
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        /// <summary>Unique stable identifier for this season (e.g. "season_1").</summary>
        public string SeasonId;

        /// <summary>Human-readable display name for the season.</summary>
        public string SeasonName;

        /// <summary>Short theme keyword (e.g. "Winter Wonderland", "Sky Pioneer").</summary>
        public string Theme;

        /// <summary>Optional long-form description of the season's narrative.</summary>
        public string Description;

        // ── Schedule ──────────────────────────────────────────────────────────────

        /// <summary>UTC date/time when this season begins, in ISO 8601 format.</summary>
        public string StartDate;

        /// <summary>UTC date/time when this season ends, in ISO 8601 format.</summary>
        public string EndDate;

        // ── Progression ───────────────────────────────────────────────────────────

        /// <summary>Total number of tiers available in this season.</summary>
        public int TierCount = 50;

        // ── Rewards ───────────────────────────────────────────────────────────────

        /// <summary>One reward per tier on the free track (index 0 = tier 1).</summary>
        public List<BattlePassReward> FreeTrackRewards = new List<BattlePassReward>();

        /// <summary>One reward per tier on the premium track (index 0 = tier 1).</summary>
        public List<BattlePassReward> PremiumTrackRewards = new List<BattlePassReward>();

        // ── Challenges ────────────────────────────────────────────────────────────

        /// <summary>Featured challenge IDs for this season.</summary>
        public List<string> FeaturedChallengeIds = new List<string>();

        // ── Map ───────────────────────────────────────────────────────────────────

        /// <summary>Seasonal map markers / POIs unlocked during this season.</summary>
        public List<SeasonalMapMarker> MapMarkers = new List<SeasonalMapMarker>();

        // ── Convenience ───────────────────────────────────────────────────────────

        /// <summary>
        /// Parses <see cref="StartDate"/> as a UTC <see cref="DateTime"/>.
        /// Returns <see cref="DateTime.MinValue"/> if the field is empty or unparseable.
        /// </summary>
        public DateTime GetStartDateUtc()
        {
            if (string.IsNullOrEmpty(StartDate)) return DateTime.MinValue;
            return DateTime.TryParse(StartDate, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt.ToUniversalTime()
                : DateTime.MinValue;
        }

        /// <summary>
        /// Parses <see cref="EndDate"/> as a UTC <see cref="DateTime"/>.
        /// Returns <see cref="DateTime.MaxValue"/> if the field is empty or unparseable.
        /// </summary>
        public DateTime GetEndDateUtc()
        {
            if (string.IsNullOrEmpty(EndDate)) return DateTime.MaxValue;
            return DateTime.TryParse(EndDate, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt.ToUniversalTime()
                : DateTime.MaxValue;
        }

        /// <summary>Returns <c>true</c> when <see cref="DateTime.UtcNow"/> falls within this season's window.</summary>
        public bool IsActive()
        {
            var now = DateTime.UtcNow;
            return now >= GetStartDateUtc() && now < GetEndDateUtc();
        }

        /// <summary>Serializes this object to a JSON string using <see cref="JsonUtility"/>.</summary>
        public string ToJson() => JsonUtility.ToJson(this, prettyPrint: true);

        /// <summary>Deserializes a <see cref="SeasonData"/> instance from a JSON string.</summary>
        public static SeasonData FromJson(string json) => JsonUtility.FromJson<SeasonData>(json);
    }
}
