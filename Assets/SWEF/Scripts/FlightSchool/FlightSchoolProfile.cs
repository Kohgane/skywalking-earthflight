using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Designer-authored ScriptableObject holding the tuneable parameters
    /// for the Flight School system (Phase 84). Drop an asset of this type
    /// on <see cref="FlightSchoolManager"/> to override defaults without a code change.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Flight School Profile",
                     fileName = "FlightSchoolProfile")]
    public class FlightSchoolProfile : ScriptableObject
    {
        // ── Curriculum ───────────────────────────────────────────────────────────

        [Header("Curriculum")]
        [Tooltip("Lessons preloaded into the manager when no save file exists. Leave empty to use the built-in defaults.")]
        public List<FlightLesson> defaultLessons = new List<FlightLesson>();

        [Tooltip("Certifications preloaded when no save file exists.")]
        public List<PilotCertification> defaultCertifications = new List<PilotCertification>();

        // ── Exams ────────────────────────────────────────────────────────────────

        [Header("Certification Exams")]
        [Tooltip("Practical tests attached to each certification tier.")]
        public List<CertificationExam> exams = new List<CertificationExam>();

        // ── Grading ──────────────────────────────────────────────────────────────

        [Header("Grading Weights")]
        [Tooltip("Per-criterion weight overrides. An entry with weight ≤ 0 is ignored.")]
        public List<GradeCriteria> gradingWeights = new List<GradeCriteria>
        {
            new GradeCriteria { criteriaId = "precision",  displayName = "Precision",       weight = 2f   },
            new GradeCriteria { criteriaId = "smoothness", displayName = "Smoothness",      weight = 1.5f },
            new GradeCriteria { criteriaId = "timing",     displayName = "Timing",          weight = 1f   },
            new GradeCriteria { criteriaId = "safety",     displayName = "Safety",          weight = 2.5f },
            new GradeCriteria { criteriaId = "fuel",       displayName = "Fuel Efficiency", weight = 1f   }
        };

        // ── Skill tree ───────────────────────────────────────────────────────────

        [Header("Skill Tree")]
        [Tooltip("Pre-authored skill-tree graph. Leave empty to use the built-in default.")]
        public SkillTreeData skillTree = new SkillTreeData();

        // ── Difficulty ───────────────────────────────────────────────────────────

        [Header("Difficulty")]
        [Tooltip("Global multiplier applied to constraint penalties.")]
        [Range(0f, 4f)] public float penaltyMultiplier = 1f;

        [Tooltip("XP multiplier applied on first-time lesson completion.")]
        [Range(0f, 5f)] public float xpMultiplier = 1f;

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies this profile's weight overrides to <paramref name="grading"/>
        /// (skipping zero/negative weights). Safe to call with <c>null</c>.
        /// </summary>
        public void ApplyGradingWeightsTo(FlightGradingSystem grading)
        {
            if (grading == null || gradingWeights == null) return;
            foreach (var w in gradingWeights)
            {
                if (w == null || w.weight <= 0f) continue;
                grading.SetCriterionWeight(w.criteriaId, w.weight);
            }
        }

        /// <summary>Returns the configured exam for <paramref name="certType"/>, or <c>null</c>.</summary>
        public CertificationExam FindExam(CertificationType certType)
        {
            if (exams == null) return null;
            foreach (var e in exams)
                if (e != null && e.certType == certType) return e;
            return null;
        }
    }
}
