// Phase 73 — Flight Formation Display & Airshow System
// Assets/SWEF/Scripts/Airshow/AirshowScoreCalculator.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Airshow
{
    /// <summary>Individual maneuver scoring breakdown.</summary>
    [Serializable]
    public struct ManeuverScore
    {
        public ManeuverType maneuverType;
        public float timingScore;
        public float positionScore;
        public float smoothnessScore;
        public float compositeScore;
    }

    /// <summary>Final airshow performance result.</summary>
    [Serializable]
    public struct AirshowResult
    {
        public string routineId;
        public float totalScore;
        public PerformanceRating rating;
        public float[] perActScores;
        public ManeuverType bestManeuver;
        public ManeuverType worstManeuver;
        public float totalDuration;
        public DateTime timestamp;
    }

    /// <summary>
    /// Pure C# static utility for calculating airshow performance scores.
    /// No MonoBehaviour — safe to call from any context.
    /// </summary>
    public static class AirshowScoreCalculator
    {
        // ── Score weights ────────────────────────────────────────────────────

        private const float WeightTiming     = 0.25f;
        private const float WeightPosition   = 0.30f;
        private const float WeightSmoothness = 0.20f;
        private const float WeightFormation  = 0.25f;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Scores how close the actual maneuver start time was to the expected time.
        /// Returns 100 for perfect timing, 0 when deviation equals or exceeds <paramref name="tolerance"/>.
        /// </summary>
        public static float CalculateTimingScore(float actualTime, float expectedTime, float tolerance)
        {
            if (tolerance <= 0f) return 100f;
            float deviation = Mathf.Abs(actualTime - expectedTime);
            return Mathf.Clamp01(1f - deviation / tolerance) * 100f;
        }

        /// <summary>
        /// Scores positional accuracy relative to the expected position.
        /// Returns 100 when on-target, 0 when deviation equals or exceeds <paramref name="tolerance"/>.
        /// </summary>
        public static float CalculatePositionScore(Vector3 actual, Vector3 expected, float tolerance)
        {
            if (tolerance <= 0f) return 100f;
            float deviation = Vector3.Distance(actual, expected);
            return Mathf.Clamp01(1f - deviation / tolerance) * 100f;
        }

        /// <summary>
        /// Scores flight smoothness based on G-force variance over the maneuver history.
        /// Low variance → high score (smooth flight).
        /// </summary>
        public static float CalculateSmoothnessScore(float[] gForceHistory)
        {
            if (gForceHistory == null || gForceHistory.Length == 0) return 100f;

            double sum = 0;
            foreach (float g in gForceHistory) sum += g;
            double mean = sum / gForceHistory.Length;

            double variance = 0;
            foreach (float g in gForceHistory)
            {
                double diff = g - mean;
                variance += diff * diff;
            }
            variance /= gForceHistory.Length;

            // Map variance: 0 → 100, variance ≥ 10 → 0
            float score = Mathf.Clamp01(1f - (float)(variance / 10.0)) * 100f;
            return score;
        }

        /// <summary>
        /// Scores formation accuracy based on the average positional deviation
        /// of all performers from their expected positions.
        /// </summary>
        public static float CalculateFormationScore(Vector3[] positions, Vector3[] expectedPositions)
        {
            if (positions == null || expectedPositions == null) return 100f;
            int count = Mathf.Min(positions.Length, expectedPositions.Length);
            if (count == 0) return 100f;

            float totalDeviation = 0f;
            for (int i = 0; i < count; i++)
                totalDeviation += Vector3.Distance(positions[i], expectedPositions[i]);

            float avgDeviation = totalDeviation / count;
            // Tolerance of 50 m → 0 score; 0 deviation → 100
            return Mathf.Clamp01(1f - avgDeviation / 50f) * 100f;
        }

        /// <summary>
        /// Combines the four component scores into a single weighted composite (0–100).
        /// </summary>
        public static float CalculateCompositeScore(
            float timing, float position, float smoothness, float formation,
            AirshowConfig config)
        {
            _ = config; // reserved for future per-config weighting
            return timing     * WeightTiming
                 + position   * WeightPosition
                 + smoothness * WeightSmoothness
                 + formation  * WeightFormation;
        }

        /// <summary>Maps a composite score (0–100) to a <see cref="PerformanceRating"/>.</summary>
        public static PerformanceRating GetRating(float compositeScore)
        {
            if (compositeScore >= 95f) return PerformanceRating.Perfect;
            if (compositeScore >= 85f) return PerformanceRating.Excellent;
            if (compositeScore >= 70f) return PerformanceRating.Great;
            if (compositeScore >= 50f) return PerformanceRating.Good;
            return PerformanceRating.NeedsWork;
        }

        /// <summary>
        /// Builds a complete <see cref="AirshowResult"/> from the maneuver score list.
        /// </summary>
        public static AirshowResult BuildResult(
            AirshowRoutineData routine,
            List<ManeuverScore> scores,
            float totalTime)
        {
            float totalScore = 0f;
            ManeuverType best  = ManeuverType.StraightAndLevel;
            ManeuverType worst = ManeuverType.StraightAndLevel;
            float bestScore  = -1f;
            float worstScore = 101f;

            foreach (ManeuverScore ms in scores)
            {
                totalScore += ms.compositeScore;
                if (ms.compositeScore > bestScore)  { bestScore  = ms.compositeScore; best  = ms.maneuverType; }
                if (ms.compositeScore < worstScore) { worstScore = ms.compositeScore; worst = ms.maneuverType; }
            }

            if (scores.Count > 0) totalScore /= scores.Count;

            return new AirshowResult
            {
                routineId     = routine != null ? routine.routineId : string.Empty,
                totalScore    = totalScore,
                rating        = GetRating(totalScore),
                perActScores  = Array.Empty<float>(),   // per-act breakdown reserved
                bestManeuver  = best,
                worstManeuver = worst,
                totalDuration = totalTime,
                timestamp     = DateTime.UtcNow
            };
        }
    }
}
