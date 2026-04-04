// FlightExam.cs — SWEF Flight Academy & Certification System (Phase 104)
using System;
using UnityEngine;

namespace SWEF.Academy
{
    /// <summary>Pass/fail result of a completed <see cref="FlightExam"/>.</summary>
    [Serializable]
    public class ExamResult
    {
        /// <summary>ID of the exam this result belongs to.</summary>
        public string examId;

        /// <summary>UTC timestamp when the exam was completed.</summary>
        public string completedAtUtc;

        /// <summary>Theory quiz score (0–100).</summary>
        public float theoryScore;

        /// <summary>Practical test score (0–100).</summary>
        public float practicalScore;

        /// <summary>Weighted overall score (0–100).</summary>
        public float overallScore;

        /// <summary>Whether the candidate passed the exam.</summary>
        public bool passed;

        /// <summary>Number of attempts so far (1-based).</summary>
        public int attemptNumber;

        public override string ToString() =>
            $"[ExamResult:{examId}] Overall: {overallScore:F1} | Theory: {theoryScore:F1} | Practical: {practicalScore:F1} | {(passed ? "PASS" : "FAIL")}";
    }

    /// <summary>
    /// Combines a <see cref="TheoryModule"/> and a <see cref="PracticalExercise"/> into a
    /// formal examination that gates <see cref="LicenseTier"/> award.
    /// The overall score is a weighted combination of both components.
    /// </summary>
    [Serializable]
    public class FlightExam
    {
        /// <summary>Stable identifier (snake_case).</summary>
        public string examId;

        /// <summary>Display title (e.g. "Private Pilot Written Test").</summary>
        public string examTitle;

        /// <summary>The license tier awarded on passing this exam.</summary>
        public LicenseTier targetTier;

        /// <summary>Theory portion of the exam (<c>null</c> if theory-only exam is not used).</summary>
        public TheoryModule theoryComponent;

        /// <summary>Practical flight test portion (<c>null</c> if written-only).</summary>
        public PracticalExercise practicalComponent;

        /// <summary>
        /// Weight of the theory score when computing the overall score (0–1).
        /// The practical weight is automatically <c>1 – theoryWeight</c>.
        /// </summary>
        [Range(0f, 1f)]
        public float theoryWeight = 0.4f;

        /// <summary>Minimum overall score (0–100) required to pass.</summary>
        [Range(0f, 100f)]
        public float passingScore = 70f;

        /// <summary>Maximum number of attempts allowed (0 = unlimited).</summary>
        [Min(0)]
        public int maxAttempts = 3;

        // ── Scoring ────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the weighted overall score from individual component scores.
        /// If a component is absent its weight is redistributed to the other component.
        /// </summary>
        public float ComputeOverallScore(float theoryScore, float practicalScore)
        {
            bool hasTheory     = theoryComponent     != null;
            bool hasPractical  = practicalComponent  != null;

            if (hasTheory && hasPractical)
            {
                float practicalWeight = 1f - theoryWeight;
                return theoryScore * theoryWeight + practicalScore * practicalWeight;
            }
            if (hasTheory)    return theoryScore;
            if (hasPractical) return practicalScore;
            return 0f;
        }

        /// <summary>
        /// Builds an <see cref="ExamResult"/> from component scores and the current attempt count.
        /// </summary>
        public ExamResult BuildResult(float theoryScore, float practicalScore, int attemptNumber)
        {
            float overall = ComputeOverallScore(theoryScore, practicalScore);
            return new ExamResult
            {
                examId           = examId,
                completedAtUtc   = DateTime.UtcNow.ToString("o"),
                theoryScore      = theoryScore,
                practicalScore   = practicalScore,
                overallScore     = overall,
                passed           = overall >= passingScore,
                attemptNumber    = attemptNumber
            };
        }

        public override string ToString() =>
            $"[Exam:{examId}] {examTitle} | Pass: {passingScore}% → {targetTier}";
    }
}
