// FlightLesson.cs — SWEF Flight Academy & Certification System (Phase 104)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Academy
{
    /// <summary>
    /// Completion state for a single lesson, serialised to the save file.
    /// </summary>
    public enum LessonStatus
    {
        /// <summary>Not yet started.</summary>
        Locked,
        /// <summary>Lesson has been unlocked but not completed.</summary>
        Available,
        /// <summary>Currently in progress.</summary>
        InProgress,
        /// <summary>Theory and practical portions both completed.</summary>
        Completed
    }

    /// <summary>
    /// Definition of a single lesson inside a <see cref="FlightCurriculum"/>.
    /// Each lesson contains a theory component (optional) and/or a practical exercise (optional).
    /// </summary>
    [Serializable]
    public class FlightLesson
    {
        /// <summary>Stable identifier used to persist progress (snake_case).</summary>
        public string lessonId;

        /// <summary>Short display title shown in the Academy UI.</summary>
        public string title;

        /// <summary>Multi-sentence description of what the student learns.</summary>
        [TextArea(2, 6)]
        public string description;

        /// <summary>Estimated completion time in minutes.</summary>
        [Min(1)]
        public int estimatedMinutes = 15;

        /// <summary>Required lesson IDs that must be completed before this one unlocks.</summary>
        public List<string> prerequisites = new List<string>();

        /// <summary>
        /// Theory module attached to this lesson (<c>null</c> if theory-free).
        /// </summary>
        public TheoryModule theoryModule;

        /// <summary>
        /// Practical exercise attached to this lesson (<c>null</c> if practical-free).
        /// </summary>
        public PracticalExercise practicalExercise;

        /// <summary>XP reward granted on first completion.</summary>
        [Min(0)]
        public int xpReward = 100;

        // ── Computed helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when the lesson has both a theory and a practical component.
        /// </summary>
        public bool HasBothComponents =>
            theoryModule != null && practicalExercise != null;

        public override string ToString() =>
            $"[Lesson:{lessonId}] {title} (~{estimatedMinutes} min)";
    }
}
