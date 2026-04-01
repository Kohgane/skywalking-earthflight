using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// Static utility that calculates weighted exam scores, letter grades, and pass/fail status.
    /// All methods are pure (no side-effects) and safe to call from tests or any thread.
    /// </summary>
    public static class ExamScoringEngine
    {
        // ── Passing thresholds by difficulty ─────────────────────────────────────
        /// <summary>Returns the minimum score (0–100) required to pass an exam of the given difficulty.</summary>
        public static float GetPassingThreshold(ExamDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ExamDifficulty.Bronze:   return 60f;
                case ExamDifficulty.Silver:   return 70f;
                case ExamDifficulty.Gold:     return 80f;
                case ExamDifficulty.Platinum: return 90f;
                default:                      return 60f;
            }
        }

        // ── Core scoring ──────────────────────────────────────────────────────────

        /// <summary>
        /// Calculates a composite 0–100 score from per-objective scores,
        /// applying penalties and bonuses.
        /// </summary>
        /// <param name="objectiveScores">Individual objective scores (each 0–100).</param>
        /// <param name="penaltyPoints">Total penalty deduction points.</param>
        /// <param name="bonusPoints">Total bonus addition points.</param>
        /// <returns>Clamped composite score 0–100.</returns>
        public static float CalculateScore(List<ObjectiveScore> objectiveScores,
                                           float penaltyPoints, float bonusPoints)
        {
            if (objectiveScores == null || objectiveScores.Count == 0)
                return Mathf.Clamp(bonusPoints - penaltyPoints, 0f, 100f);

            float totalWeight = 0f;
            float weightedSum = 0f;
            foreach (var obj in objectiveScores)
            {
                totalWeight += 1f;
                weightedSum += Mathf.Clamp(obj.score, 0f, 100f);
            }

            float baseScore = totalWeight > 0f ? weightedSum / totalWeight : 0f;
            return Mathf.Clamp(baseScore - penaltyPoints + bonusPoints, 0f, 100f);
        }

        /// <summary>
        /// Calculates a composite 0–100 score from weighted objective scores,
        /// applying penalties and bonuses. Weights on each <see cref="ObjectiveScore"/>
        /// are honoured if non-zero; otherwise equal weighting is used.
        /// </summary>
        public static float CalculateWeightedScore(List<ObjectiveScore> objectiveScores,
                                                   List<float> weights,
                                                   float penaltyPoints, float bonusPoints)
        {
            if (objectiveScores == null || objectiveScores.Count == 0)
                return Mathf.Clamp(bonusPoints - penaltyPoints, 0f, 100f);

            float totalWeight = 0f;
            float weightedSum = 0f;
            for (int i = 0; i < objectiveScores.Count; i++)
            {
                float w = (weights != null && i < weights.Count && weights[i] > 0f) ? weights[i] : 1f;
                totalWeight += w;
                weightedSum += Mathf.Clamp(objectiveScores[i].score, 0f, 100f) * w;
            }

            float baseScore = totalWeight > 0f ? weightedSum / totalWeight : 0f;
            return Mathf.Clamp(baseScore - penaltyPoints + bonusPoints, 0f, 100f);
        }

        // ── Letter grades ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the letter grade string for a 0–100 score.
        /// Grades: A+ (97–100), A (93–96), A- (90–92), B+ (87–89), B (83–86),
        /// B- (80–82), C+ (77–79), C (73–76), C- (70–72), D (60–69), F (0–59).
        /// </summary>
        public static string GetLetterGrade(float score)
        {
            if (score >= 97f) return "A+";
            if (score >= 93f) return "A";
            if (score >= 90f) return "A-";
            if (score >= 87f) return "B+";
            if (score >= 83f) return "B";
            if (score >= 80f) return "B-";
            if (score >= 77f) return "C+";
            if (score >= 73f) return "C";
            if (score >= 70f) return "C-";
            if (score >= 60f) return "D";
            return "F";
        }

        /// <summary>Returns true when <paramref name="score"/> meets <paramref name="passingThreshold"/>.</summary>
        public static bool GetPassStatus(float score, float passingThreshold)
        {
            return score >= passingThreshold;
        }

        // ── Specialised scoring formulas ──────────────────────────────────────────

        /// <summary>
        /// Calculates a composite landing score (0–100) based on the four primary factors.
        /// Weights: touchdown speed 25%, centerline deviation 25%, descent rate 25%,
        /// G-force 15%, with 10% reserved for approach stability (assumed perfect here
        /// as it is validated externally).
        /// </summary>
        /// <param name="touchdownSpeedKnots">
        /// Deviation from target touchdown speed in knots (absolute value).
        /// </param>
        /// <param name="centerlineDeviationMeters">Lateral offset from runway centreline in metres.</param>
        /// <param name="descentRateFpm">Vertical descent rate at touchdown in ft/min.</param>
        /// <param name="gForce">G-force at touchdown (e.g. 1.0 = normal, 1.5 = firm).</param>
        /// <param name="approachStabilityScore">
        /// Approach stability score 0–100 (default 100 = stable). Pass the actual value
        /// from approach monitoring logic when available.
        /// </param>
        /// <returns>Composite landing score 0–100.</returns>
        public static float CalculateLandingScore(float touchdownSpeedKnots,
                                                   float centerlineDeviationMeters,
                                                   float descentRateFpm,
                                                   float gForce,
                                                   float approachStabilityScore = 100f)
        {
            float speedScore       = ScoreTouchdownSpeed(Mathf.Abs(touchdownSpeedKnots));
            float centerlineScore  = ScoreCenterlineDeviation(Mathf.Abs(centerlineDeviationMeters));
            float descentScore     = ScoreDescentRate(Mathf.Abs(descentRateFpm));
            float gForceScore      = ScoreGForce(gForce);
            float stabilityScore   = Mathf.Clamp(approachStabilityScore, 0f, 100f);

            return speedScore * 0.25f
                 + centerlineScore * 0.25f
                 + descentScore * 0.25f
                 + gForceScore * 0.15f
                 + stabilityScore * 0.10f;
        }

        /// <summary>
        /// Calculates an IFR score (0–100) from deviations during instrument flight.
        /// </summary>
        /// <param name="headingDeviationDeg">Average heading deviation from assigned heading in degrees.</param>
        /// <param name="altitudeDeviationFt">Average altitude deviation from assigned altitude in feet.</param>
        /// <param name="speedDeviationKnots">Average speed deviation from assigned speed in knots.</param>
        /// <param name="waypointAccuracy">Fraction of waypoints hit within tolerance (0–1).</param>
        public static float CalculateIFRScore(float headingDeviationDeg, float altitudeDeviationFt,
                                              float speedDeviationKnots, float waypointAccuracy)
        {
            float headingScore  = ScoreDeviation(headingDeviationDeg,  2f,  5f, 10f, 20f);
            float altScore      = ScoreDeviation(altitudeDeviationFt, 50f, 100f, 200f, 400f);
            float speedScore    = ScoreDeviation(speedDeviationKnots,  5f, 10f,  20f, 40f);
            float wayptScore    = Mathf.Clamp01(waypointAccuracy) * 100f;

            return headingScore * 0.30f
                 + altScore     * 0.30f
                 + speedScore   * 0.20f
                 + wayptScore   * 0.20f;
        }

        /// <summary>
        /// Calculates a formation flying score (0–100).
        /// </summary>
        /// <param name="positionDeviationMeters">Average distance from assigned formation slot in metres.</param>
        /// <param name="headingMatchDeg">Average heading difference from lead aircraft in degrees.</param>
        /// <param name="speedMatchKnots">Average speed difference from lead aircraft in knots.</param>
        public static float CalculateFormationScore(float positionDeviationMeters,
                                                    float headingMatchDeg,
                                                    float speedMatchKnots)
        {
            float posScore     = ScoreDeviation(positionDeviationMeters,  5f, 15f, 30f, 60f);
            float headingScore = ScoreDeviation(headingMatchDeg,          2f,  5f, 10f, 20f);
            float speedScore   = ScoreDeviation(speedMatchKnots,          5f, 10f, 20f, 40f);

            return posScore     * 0.50f
                 + headingScore * 0.30f
                 + speedScore   * 0.20f;
        }

        // ── Private scoring helpers ───────────────────────────────────────────────

        private static float ScoreTouchdownSpeed(float deviationKnots)
        {
            if (deviationKnots <= 10f) return 100f;
            if (deviationKnots <= 20f) return 75f;
            if (deviationKnots <= 30f) return 50f;
            return 25f;
        }

        private static float ScoreCenterlineDeviation(float meters)
        {
            if (meters < 3f)  return 100f;
            if (meters < 10f) return 75f;
            if (meters < 20f) return 50f;
            return 25f;
        }

        private static float ScoreDescentRate(float fpm)
        {
            if (fpm < 200f) return 100f;
            if (fpm < 400f) return 75f;
            if (fpm < 600f) return 50f;
            return 25f;
        }

        private static float ScoreGForce(float g)
        {
            if (g < 1.2f) return 100f;
            if (g < 1.5f) return 75f;
            if (g < 2.0f) return 50f;
            return 25f;
        }

        /// <summary>Generic four-band deviation scorer returning 100/75/50/25.</summary>
        private static float ScoreDeviation(float value, float t1, float t2, float t3, float t4)
        {
            if (value <= t1) return 100f;
            if (value <= t2) return 75f;
            if (value <= t3) return 50f;
            return 25f;
        }
    }
}
