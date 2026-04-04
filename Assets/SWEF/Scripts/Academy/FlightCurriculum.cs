// FlightCurriculum.cs — SWEF Flight Academy & Certification System (Phase 104)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Academy
{
    /// <summary>Broad difficulty bracket that groups curricula.</summary>
    public enum CurriculumLevel
    {
        /// <summary>Fundamentals — pre-solo, first principles.</summary>
        Beginner,
        /// <summary>Post-solo cross-country, basic IFR, navigation.</summary>
        Intermediate,
        /// <summary>Commercial operations, complex procedures, high-altitude.</summary>
        Advanced,
        /// <summary>ATP-level multi-engine, RVSM, oceanic, emergency mastery.</summary>
        Expert
    }

    /// <summary>
    /// A named, ordered collection of <see cref="FlightLesson"/> objects that together
    /// constitute a complete training programme leading to a <see cref="LicenseTier"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Academy/Curriculum", fileName = "NewCurriculum")]
    public class FlightCurriculum : ScriptableObject
    {
        // ── Identity ───────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Stable identifier used to persist progress (snake_case).")]
        public string curriculumId;

        [Tooltip("Full display name shown in the Academy UI.")]
        public string curriculumName;

        [TextArea(2, 5)]
        [Tooltip("Overview paragraph describing the curriculum.")]
        public string description;

        // ── Classification ─────────────────────────────────────────────────────

        [Header("Classification")]
        [Tooltip("Difficulty bracket for UI filtering.")]
        public CurriculumLevel level = CurriculumLevel.Beginner;

        [Tooltip("The license tier awarded on successful completion.")]
        public LicenseTier targetTier = LicenseTier.StudentPilot;

        [Tooltip("Minimum license tier required before enrolling (None = open to all).")]
        public LicenseTier prerequisiteTier = LicenseTier.None;

        // ── Content ────────────────────────────────────────────────────────────

        [Header("Lessons")]
        [Tooltip("Ordered list of lessons in this curriculum.")]
        public List<FlightLesson> lessons = new List<FlightLesson>();

        // ── Metadata ───────────────────────────────────────────────────────────

        [Header("Metadata")]
        [Tooltip("Estimated total hours to complete the curriculum.")]
        [Min(0f)]
        public float estimatedHours = 2f;

        [Tooltip("Resource path to the curriculum icon (relative to Resources/Academy/).")]
        public string iconResourcePath;

        // ── Computed helpers ───────────────────────────────────────────────────

        /// <summary>Total number of lessons in this curriculum.</summary>
        public int LessonCount => lessons?.Count ?? 0;

        /// <summary>Returns the lesson with the given <paramref name="lessonId"/>, or <c>null</c>.</summary>
        public FlightLesson GetLesson(string lessonId)
        {
            if (lessons == null) return null;
            foreach (var lesson in lessons)
                if (lesson.lessonId == lessonId) return lesson;
            return null;
        }

        public override string ToString() =>
            $"[Curriculum:{curriculumId}] {curriculumName} | {level} | {LessonCount} lessons → {targetTier}";
    }
}
