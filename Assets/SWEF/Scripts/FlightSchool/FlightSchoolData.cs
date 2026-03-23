using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightSchool
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>Broad subject area a lesson belongs to.</summary>
    public enum LessonCategory
    {
        BasicControls,
        Navigation,
        WeatherFlying,
        Aerobatics,
        EmergencyProcedures,
        Formation
    }

    /// <summary>Skill level required for a lesson.</summary>
    public enum LessonDifficulty
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert
    }

    /// <summary>Current progress state of a lesson for the local player.</summary>
    public enum LessonStatus
    {
        Locked,
        Available,
        InProgress,
        Completed,
        Mastered
    }

    /// <summary>Pilot certification tiers awarded upon completing curricula.</summary>
    public enum CertificationType
    {
        StudentPilot,
        PrivatePilot,
        CommercialPilot,
        AcrobaticPilot,
        MasterAviator
    }

    // ── Data classes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A single measurable task that must be completed within a <see cref="FlightLesson"/>.
    /// </summary>
    [Serializable]
    public class LessonObjective
    {
        /// <summary>Unique identifier for this objective within its parent lesson.</summary>
        public string objectiveId;

        /// <summary>Human-readable description shown in the HUD and lesson detail panel.</summary>
        public string description;

        /// <summary>Goal value the player must reach (e.g. altitude in metres, duration in seconds).</summary>
        public float targetValue;

        /// <summary>Player's current measured value. Updated by <see cref="FlightSchoolManager.CompleteObjective"/>.</summary>
        public float currentValue;

        /// <summary>Whether the objective has been satisfied.</summary>
        public bool isCompleted;

        /// <summary>Returns the objective's completion ratio, clamped to [0, 1].</summary>
        public float Progress01()
        {
            if (targetValue <= 0f) return isCompleted ? 1f : 0f;
            return Mathf.Clamp01(currentValue / targetValue);
        }
    }

    /// <summary>
    /// A structured flying lesson with objectives, prerequisites, and scoring metadata.
    /// </summary>
    [Serializable]
    public class FlightLesson
    {
        /// <summary>Globally unique lesson identifier (e.g. "basic_takeoff").</summary>
        public string lessonId;

        /// <summary>Localisation-friendly display title.</summary>
        public string title;

        /// <summary>Short overview shown in the curriculum browser.</summary>
        public string description;

        /// <summary>Subject area this lesson belongs to.</summary>
        public LessonCategory category;

        /// <summary>Skill level required.</summary>
        public LessonDifficulty difficulty;

        /// <summary>Current unlock / completion state for the local player.</summary>
        public LessonStatus status;

        /// <summary>Ordered list of objectives the player must complete.</summary>
        public List<LessonObjective> objectives = new List<LessonObjective>();

        /// <summary>IDs of lessons that must be completed before this one unlocks.</summary>
        public List<string> prerequisites = new List<string>();

        /// <summary>Approximate duration shown to the player before they start.</summary>
        public int estimatedMinutes;

        /// <summary>Experience points awarded on first completion.</summary>
        public int xpReward;

        /// <summary>Player's personal best normalised score (0–100).</summary>
        public float bestScore;

        /// <summary>Number of times the player has finished this lesson.</summary>
        public int completionCount;

        /// <summary>Pre-lesson text displayed in the briefing screen.</summary>
        public string briefingText;

        /// <summary>Post-lesson summary text displayed in the debrief screen.</summary>
        public string debriefingText;

        /// <summary>
        /// Calculates the average progress across all objectives, returning a value in [0, 1].
        /// Returns 0 when there are no objectives.
        /// </summary>
        public float OverallProgress()
        {
            if (objectives == null || objectives.Count == 0) return 0f;
            float sum = 0f;
            foreach (var obj in objectives) sum += obj.Progress01();
            return sum / objectives.Count;
        }

        /// <summary>
        /// Returns <c>true</c> when all prerequisite lessons appear in
        /// <paramref name="completedLessonIds"/>.
        /// </summary>
        /// <param name="completedLessonIds">Set of lesson IDs the player has already finished.</param>
        public bool ArePrerequisitesMet(List<string> completedLessonIds)
        {
            if (prerequisites == null || prerequisites.Count == 0) return true;
            if (completedLessonIds == null) return false;
            foreach (var prereq in prerequisites)
            {
                if (!completedLessonIds.Contains(prereq)) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// A pilot certification awarded when the player completes a defined set of lessons.
    /// </summary>
    [Serializable]
    public class PilotCertification
    {
        /// <summary>Certification tier.</summary>
        public CertificationType certType;

        /// <summary>Display name shown in the UI (e.g. "Private Pilot Certificate").</summary>
        public string displayName;

        /// <summary>IDs of lessons that must be completed to earn this certification.</summary>
        public List<string> requiredLessons = new List<string>();

        /// <summary>Whether the player has already earned this certification.</summary>
        public bool isEarned;

        /// <summary>ISO 8601 date-time string set when the certification was awarded.</summary>
        public string earnedDate;

        /// <summary>
        /// Returns the player's completion ratio toward this certification, in [0, 1].
        /// </summary>
        /// <param name="completedLessonIds">Lessons the player has already finished.</param>
        public float Progress(List<string> completedLessonIds)
        {
            if (requiredLessons == null || requiredLessons.Count == 0) return 1f;
            if (completedLessonIds == null) return 0f;
            int done = 0;
            foreach (var id in requiredLessons)
                if (completedLessonIds.Contains(id)) done++;
            return (float)done / requiredLessons.Count;
        }
    }
}
