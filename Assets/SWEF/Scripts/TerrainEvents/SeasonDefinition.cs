// SeasonDefinition.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using System;
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — ScriptableObject that describes a single meteorological / calendar season
    /// together with the terrain-event probability modifiers that apply during it.
    ///
    /// <para>Create via <em>Assets → Create → SWEF/TerrainEvents/Season Definition</em>.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/TerrainEvents/Season Definition", fileName = "NewSeasonDefinition")]
    public class SeasonDefinition : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        [Header("Identity")]

        [Tooltip("Unique key used for lookups and save data (e.g. \"northern_winter\").")]
        public string seasonId;

        [Tooltip("Human-readable display name (e.g. \"Northern Winter\").")]
        public string seasonName;

        // ── Date Range ────────────────────────────────────────────────────────────

        [Header("Date Range")]

        [Tooltip("Day-of-year on which this season starts (1 = Jan 1).")]
        [Range(1, 365)]
        public int startDay = 1;

        [Tooltip("Day-of-year on which this season ends (inclusive).")]
        [Range(1, 365)]
        public int endDay = 90;

        // ── Event Probability Modifiers ───────────────────────────────────────────

        [Header("Event Probability Modifiers")]

        [Tooltip("Probability multipliers applied to specific TerrainEventType during this season. " +
                 "Index must match TerrainEventType enum values.")]
        public float[] eventTypeProbabilityMultipliers = new float[10];

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> if <paramref name="dayOfYear"/> falls within this season.</summary>
        public bool IsActive(int dayOfYear)
        {
            if (startDay <= endDay)
                return dayOfYear >= startDay && dayOfYear <= endDay;

            // Wrap-around season (e.g. Nov–Feb crosses year boundary)
            return dayOfYear >= startDay || dayOfYear <= endDay;
        }

        /// <summary>
        /// Returns the probability multiplier for <paramref name="eventType"/> during this season.
        /// Falls back to 1 if the array is not large enough.
        /// </summary>
        public float GetProbabilityMultiplier(TerrainEventType eventType)
        {
            int idx = (int)eventType;
            if (eventTypeProbabilityMultipliers == null || idx >= eventTypeProbabilityMultipliers.Length)
                return 1f;
            float value = eventTypeProbabilityMultipliers[idx];
            return value <= 0f ? 1f : value;
        }

        /// <summary>Returns the <see cref="SeasonDefinition"/> from <paramref name="seasons"/> that is active today.</summary>
        public static SeasonDefinition GetCurrentSeason(SeasonDefinition[] seasons)
        {
            int day = DateTime.UtcNow.DayOfYear;
            foreach (SeasonDefinition s in seasons)
                if (s != null && s.IsActive(day))
                    return s;
            return null;
        }
    }
}
