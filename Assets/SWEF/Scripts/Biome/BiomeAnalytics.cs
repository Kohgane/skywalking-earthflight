// BiomeAnalytics.cs — SWEF Terrain Detail & Biome System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Biome
{
    /// <summary>
    /// Snapshot returned by <see cref="BiomeAnalytics.GetBiomeAnalyticsSummary"/>.
    /// </summary>
    [Serializable]
    public sealed class BiomeAnalyticsSummary
    {
        /// <summary>Total number of distinct biomes the player has entered.</summary>
        public int uniqueBiomesVisited;

        /// <summary>Total number of biome transition crossings recorded.</summary>
        public int totalTransitionsCrossed;

        /// <summary>Biome in which the most time has been spent.</summary>
        public BiomeType mostVisitedBiome;

        /// <summary>Rarest biome encountered (fewest visit entries).</summary>
        public BiomeType rarestBiomeVisited;

        /// <summary>Biome diversity score 0–1 (0 = only one biome, 1 = all biomes).</summary>
        public float diversityScore;

        /// <summary>Time spent in each visited biome in seconds.</summary>
        public Dictionary<BiomeType, float> timePerBiomeSeconds;

        /// <summary>Visit counts per biome.</summary>
        public Dictionary<BiomeType, int> visitCountPerBiome;
    }

    /// <summary>
    /// Static utility class that tracks biome visit data across the session.
    /// All state is in-memory and is reset when the application quits.
    /// </summary>
    public static class BiomeAnalytics
    {
        #region Private State

        private static readonly int TotalBiomeCount = Enum.GetValues(typeof(BiomeType)).Length;

        private static readonly Dictionary<BiomeType, float> TimePerBiome  = new Dictionary<BiomeType, float>();
        private static readonly Dictionary<BiomeType, int>   VisitCount    = new Dictionary<BiomeType, int>();
        private static readonly Dictionary<(BiomeType, BiomeType), int> TransitionCount
            = new Dictionary<(BiomeType, BiomeType), int>();

        private static int   _totalTransitions;
        private static float _currentEntryTime;
        private static bool  _hasCurrentBiome;
        private static BiomeType _currentBiome;

        #endregion

        #region Public API

        /// <summary>
        /// Records that the player has entered the specified biome.
        /// </summary>
        /// <param name="biome">The biome that was entered.</param>
        public static void RecordBiomeEntry(BiomeType biome)
        {
            _currentBiome     = biome;
            _currentEntryTime = Time.realtimeSinceStartup;
            _hasCurrentBiome  = true;

            if (!VisitCount.ContainsKey(biome)) VisitCount[biome]    = 0;
            VisitCount[biome]++;

            if (!TimePerBiome.ContainsKey(biome)) TimePerBiome[biome] = 0f;
        }

        /// <summary>
        /// Records that the player has exited the specified biome.
        /// </summary>
        /// <param name="biome">The biome that was exited.</param>
        /// <param name="durationSeconds">Time spent in the biome in seconds.
        /// Pass 0 or a negative value to have the duration computed automatically.</param>
        public static void RecordBiomeExit(BiomeType biome, float durationSeconds)
        {
            float duration = durationSeconds > 0f
                ? durationSeconds
                : (_hasCurrentBiome && _currentBiome == biome
                    ? Time.realtimeSinceStartup - _currentEntryTime
                    : 0f);

            if (!TimePerBiome.ContainsKey(biome)) TimePerBiome[biome] = 0f;
            TimePerBiome[biome] += duration;

            _hasCurrentBiome = false;
        }

        /// <summary>
        /// Records that the player has crossed the boundary from one biome to another.
        /// </summary>
        /// <param name="from">Biome the player was leaving.</param>
        /// <param name="to">Biome the player entered.</param>
        public static void RecordTransitionCrossed(BiomeType from, BiomeType to)
        {
            _totalTransitions++;
            var key = (from, to);
            if (!TransitionCount.ContainsKey(key)) TransitionCount[key] = 0;
            TransitionCount[key]++;
        }

        /// <summary>
        /// Returns an immutable snapshot of all biome analytics gathered in this session.
        /// </summary>
        /// <returns>A <see cref="BiomeAnalyticsSummary"/> containing all current stats.</returns>
        public static BiomeAnalyticsSummary GetBiomeAnalyticsSummary()
        {
            FlushCurrentBiomeTime();

            BiomeType mostVisited = BiomeType.Temperate;
            float     mostTime    = -1f;
            BiomeType rarest      = BiomeType.Volcanic;
            int       fewestVisits = int.MaxValue;

            foreach (var kv in TimePerBiome)
            {
                if (kv.Value > mostTime) { mostTime = kv.Value; mostVisited = kv.Key; }
            }
            foreach (var kv in VisitCount)
            {
                if (kv.Value < fewestVisits) { fewestVisits = kv.Value; rarest = kv.Key; }
            }

            return new BiomeAnalyticsSummary
            {
                uniqueBiomesVisited   = TimePerBiome.Count,
                totalTransitionsCrossed = _totalTransitions,
                mostVisitedBiome      = mostVisited,
                rarestBiomeVisited    = rarest,
                diversityScore        = GetBiomeDiversityScore(),
                timePerBiomeSeconds   = new Dictionary<BiomeType, float>(TimePerBiome),
                visitCountPerBiome    = new Dictionary<BiomeType, int>(VisitCount)
            };
        }

        /// <summary>
        /// Calculates a biome diversity score based on how many unique biomes
        /// have been visited relative to the total number of biome types.
        /// </summary>
        /// <returns>Diversity score 0–1.</returns>
        public static float GetBiomeDiversityScore()
        {
            if (TotalBiomeCount <= 0) return 0f;
            return Mathf.Clamp01((float)TimePerBiome.Count / TotalBiomeCount);
        }

        /// <summary>Resets all tracked analytics data.</summary>
        public static void Reset()
        {
            TimePerBiome.Clear();
            VisitCount.Clear();
            TransitionCount.Clear();
            _totalTransitions = 0;
            _hasCurrentBiome  = false;
        }

        #endregion

        #region Private Helpers

        private static void FlushCurrentBiomeTime()
        {
            if (!_hasCurrentBiome) return;
            float duration = Time.realtimeSinceStartup - _currentEntryTime;
            if (!TimePerBiome.ContainsKey(_currentBiome)) TimePerBiome[_currentBiome] = 0f;
            TimePerBiome[_currentBiome] += duration;
            _currentEntryTime = Time.realtimeSinceStartup;
        }

        #endregion
    }
}
