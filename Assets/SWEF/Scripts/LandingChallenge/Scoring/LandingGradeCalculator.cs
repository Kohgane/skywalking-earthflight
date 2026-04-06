// LandingGradeCalculator.cs — Phase 120: Precision Landing Challenge System
// Grade assignment: weighted score → letter grade, bonus multipliers, deductions.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Static utility that converts a weighted numeric score into a
    /// <see cref="LandingGrade"/> and applies bonus multipliers or deductions.
    /// </summary>
    public static class LandingGradeCalculator
    {
        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Compute a <see cref="LandingGrade"/> from a raw score (0–1000) using
        /// the thresholds in <paramref name="cfg"/>.
        /// </summary>
        public static LandingGrade ComputeGrade(float score, LandingChallengeConfig cfg)
        {
            if (cfg == null)
                return FallbackGrade(score);

            if (score >= cfg.perfectThreshold)   return LandingGrade.Perfect;
            if (score >= cfg.excellentThreshold)  return LandingGrade.Excellent;
            if (score >= cfg.goodThreshold)       return LandingGrade.Good;
            if (score >= cfg.fairThreshold)       return LandingGrade.Fair;
            if (score >= cfg.poorThreshold)       return LandingGrade.Poor;
            return LandingGrade.Crash;
        }

        /// <summary>
        /// Apply bounce and go-around deductions, then recompute grade.
        /// </summary>
        public static float ApplyDeductions(float baseScore, int bounces, bool wentAround,
                                            LandingChallengeConfig cfg)
        {
            if (cfg == null) return baseScore;
            float score = baseScore;
            score -= bounces * cfg.bouncePenalty;
            if (wentAround) score -= cfg.goAroundPenalty;
            return Mathf.Clamp(score, 0f, 1000f);
        }

        /// <summary>
        /// Apply bonus modifiers (manual flight, night, no-HUD, weather).
        /// </summary>
        public static float ApplyBonuses(float baseScore, bool manualFlight, bool isNight,
                                         bool noHud, float weatherSeverity,
                                         LandingChallengeConfig cfg)
        {
            if (cfg == null) return baseScore;
            float score = baseScore;
            if (manualFlight) score += cfg.manualFlightBonus;
            if (isNight)      score += cfg.nightBonus;
            if (noHud)        score += cfg.noHudBonus;
            score += baseScore * cfg.weatherBonusMultiplier * weatherSeverity;
            return Mathf.Clamp(score, 0f, 1000f);
        }

        /// <summary>Returns a human-readable label for a grade.</summary>
        public static string GradeLabel(LandingGrade grade)
        {
            switch (grade)
            {
                case LandingGrade.Perfect:   return "PERFECT";
                case LandingGrade.Excellent: return "EXCELLENT";
                case LandingGrade.Good:      return "GOOD";
                case LandingGrade.Fair:      return "FAIR";
                case LandingGrade.Poor:      return "POOR";
                case LandingGrade.Crash:     return "CRASH";
                default:                     return "UNKNOWN";
            }
        }

        // ── Private ───────────────────────────────────────────────────────────

        private static LandingGrade FallbackGrade(float score)
        {
            if (score >= 950f) return LandingGrade.Perfect;
            if (score >= 850f) return LandingGrade.Excellent;
            if (score >= 700f) return LandingGrade.Good;
            if (score >= 500f) return LandingGrade.Fair;
            if (score >= 200f) return LandingGrade.Poor;
            return LandingGrade.Crash;
        }
    }
}
