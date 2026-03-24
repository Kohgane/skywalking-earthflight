// MissionReward.cs — SWEF Mission Briefing & Objective System (Phase 70)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Mission
{
    /// <summary>
    /// Phase 70 — Defines the rewards granted when a mission is completed.
    ///
    /// <para>Serialised as a nested field inside <see cref="MissionData"/>.  Final XP and
    /// currency are calculated via <see cref="CalculateFinalExperience"/> and
    /// <see cref="CalculateFinalCurrency"/>, which apply the rating multiplier, time bonus,
    /// and optional-objective bonus automatically.</para>
    /// </summary>
    [Serializable]
    public class MissionReward
    {
        // ── Base Rewards ──────────────────────────────────────────────────────

        /// <summary>Base experience points awarded on mission completion (before multipliers).</summary>
        [Tooltip("Base XP awarded on completion before any multipliers.")]
        [Min(0)]
        public int baseExperience;

        /// <summary>Base in-game currency awarded on completion (before multipliers).</summary>
        [Tooltip("Base currency awarded on completion before any multipliers.")]
        [Min(0)]
        public int baseCurrency;

        // ── Bonus Coefficients ────────────────────────────────────────────────

        /// <summary>
        /// Extra XP and currency granted per second the player finishes under par.
        /// Set 0 to disable the time bonus.
        /// </summary>
        [Tooltip("Bonus XP/currency per second under par time. 0 disables the bonus.")]
        [Min(0f)]
        public float timeBonus;

        /// <summary>
        /// Flat bonus XP and currency added for each completed optional objective
        /// (in addition to <see cref="MissionConfig.OptionalObjectiveBonus"/>).
        /// </summary>
        [Tooltip("Flat bonus per optional objective completed.")]
        [Min(0f)]
        public float optionalBonus;

        // ── Rating Multipliers ────────────────────────────────────────────────

        /// <summary>
        /// Per-rating multipliers applied to the total reward.
        /// Populated with canonical SWEF defaults on first access; override per-mission as needed.
        /// Not Unity-serialized (Dictionary is not natively serializable); values are set at runtime.
        /// </summary>
        [System.NonSerialized]
        public Dictionary<MissionRating, float> ratingMultipliers;

        /// <summary>
        /// Returns the <see cref="ratingMultipliers"/> dictionary, initialising it with SWEF
        /// default values on first access if it has not already been set.
        /// </summary>
        private Dictionary<MissionRating, float> GetOrCreateMultipliers()
        {
            if (ratingMultipliers == null)
            {
                ratingMultipliers = new Dictionary<MissionRating, float>
                {
                    { MissionRating.S, MissionConfig.SMultiplier },
                    { MissionRating.A, MissionConfig.AMultiplier },
                    { MissionRating.B, MissionConfig.BMultiplier },
                    { MissionRating.C, MissionConfig.CMultiplier },
                    { MissionRating.D, MissionConfig.DMultiplier },
                    { MissionRating.F, MissionConfig.FMultiplier },
                };
            }
            return ratingMultipliers;
        }

        // ── Unlock Rewards ────────────────────────────────────────────────────

        /// <summary>Item identifiers unlocked in the player's inventory on completion.</summary>
        [Tooltip("Item IDs added to the player's inventory on completion.")]
        public List<string> unlockItemIds = new List<string>();

        /// <summary>Mission ID that becomes available after this mission is completed.</summary>
        [Tooltip("Mission ID unlocked in the mission pool on completion. Leave empty for none.")]
        public string unlockMissionId;

        /// <summary>Achievement identifier granted on completion. Leave empty for none.</summary>
        [Tooltip("Achievement ID granted on completion. Leave empty for none.")]
        public string achievementId;

        // ── Calculation ───────────────────────────────────────────────────────

        /// <summary>
        /// Calculates the final experience points to award based on the mission result.
        /// </summary>
        /// <param name="result">The evaluated <see cref="MissionResult"/> for the run.</param>
        /// <returns>Final XP value (always ≥ 0).</returns>
        public int CalculateFinalExperience(MissionResult result)
        {
            if (result == null) return 0;
            float multiplier = GetRatingMultiplier(result.rating);
            float total = baseExperience * multiplier;
            total += result.timeSavedSeconds > 0f ? result.timeSavedSeconds * timeBonus : 0f;
            total += result.optionalCompleted * optionalBonus;
            return Mathf.Max(0, Mathf.RoundToInt(total));
        }

        /// <summary>
        /// Calculates the final currency to award based on the mission result.
        /// </summary>
        /// <param name="result">The evaluated <see cref="MissionResult"/> for the run.</param>
        /// <returns>Final currency value (always ≥ 0).</returns>
        public int CalculateFinalCurrency(MissionResult result)
        {
            if (result == null) return 0;
            float multiplier = GetRatingMultiplier(result.rating);
            float total = baseCurrency * multiplier;
            total += result.timeSavedSeconds > 0f ? result.timeSavedSeconds * timeBonus : 0f;
            total += result.optionalCompleted * optionalBonus;
            return Mathf.Max(0, Mathf.RoundToInt(total));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private float GetRatingMultiplier(MissionRating rating)
        {
            return GetOrCreateMultipliers().TryGetValue(rating, out float m) ? m : 1f;
        }
    }
}
