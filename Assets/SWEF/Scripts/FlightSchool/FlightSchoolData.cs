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
        /// Optional flight envelope constraints enforced by <see cref="SWEF.FlightSchool.FlightConstraintEnforcer"/>.
        /// Empty/null means no enforcement for this lesson.
        /// </summary>
        public List<FlightConstraint> constraints = new List<FlightConstraint>();

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

    // ── Phase 84 — Grading, Constraints, Exams, Skill Tree ───────────────────────

    /// <summary>Type of constraint applied to the player during a lesson.</summary>
    public enum ConstraintType
    {
        AltitudeRange,
        SpeedRange,
        HeadingRange,
        BankAngleLimit,
        GForceLimit,
        GeofenceRadius
    }

    /// <summary>
    /// Describes a flight envelope the player must stay inside for a lesson.
    /// Violations accumulate deviation penalties via <see cref="FlightInstructor"/>.
    /// </summary>
    [Serializable]
    public class FlightConstraint
    {
        /// <summary>Kind of constraint (altitude, speed, heading, bank, g-force, geofence).</summary>
        public ConstraintType type;

        /// <summary>Lower bound (metres, knots, degrees, g, or metres depending on <see cref="type"/>).</summary>
        public float minValue;

        /// <summary>Upper bound (same units as <see cref="minValue"/>).</summary>
        public float maxValue;

        /// <summary>Penalty points accrued per second while outside the envelope (0–10 typical).</summary>
        public float penaltyPerSecond = 1f;

        /// <summary>Soft buffer beyond the envelope where a warning is issued but no penalty applied.</summary>
        public float warningMargin;

        /// <summary>Human-readable description shown in the constraint HUD.</summary>
        public string description;

        /// <summary>
        /// Returns <c>true</c> if <paramref name="value"/> lies strictly within
        /// [<see cref="minValue"/>, <see cref="maxValue"/>].
        /// </summary>
        public bool IsWithin(float value) => value >= minValue && value <= maxValue;

        /// <summary>
        /// Returns <c>true</c> when <paramref name="value"/> is inside the warning
        /// margin but still outside the strict envelope.
        /// </summary>
        public bool IsInWarningZone(float value)
        {
            if (warningMargin <= 0f) return false;
            if (IsWithin(value)) return false;
            return value >= minValue - warningMargin && value <= maxValue + warningMargin;
        }
    }

    /// <summary>A single grading dimension with a weight and 0–100 score.</summary>
    [Serializable]
    public class GradeCriteria
    {
        /// <summary>Stable identifier (e.g. "precision", "smoothness").</summary>
        public string criteriaId;

        /// <summary>Localisable display name.</summary>
        public string displayName;

        /// <summary>Relative weight when aggregated into the final score (normalised at evaluation time).</summary>
        public float weight = 1f;

        /// <summary>Most recent score in the range [0, 100].</summary>
        public float score;

        /// <summary>Returns the weighted contribution <see cref="weight"/> × <see cref="score"/>.</summary>
        public float WeightedScore() => weight * score;
    }

    /// <summary>
    /// The post-lesson grade report produced by <see cref="SWEF.FlightSchool.FlightGradingSystem"/>.
    /// Includes per-criterion scores, a final aggregate, and a letter grade.
    /// </summary>
    [Serializable]
    public class LessonGradeReport
    {
        /// <summary>Lesson this report was generated for.</summary>
        public string lessonId;

        /// <summary>Individual grading dimensions with scores.</summary>
        public List<GradeCriteria> criteria = new List<GradeCriteria>();

        /// <summary>Aggregated final score in [0, 100].</summary>
        public float finalScore;

        /// <summary>Letter grade derived from <see cref="finalScore"/> (A/B/C/D/F).</summary>
        public string letterGrade = "F";

        /// <summary>ISO 8601 timestamp when the report was generated.</summary>
        public string timestamp;

        /// <summary>Total elapsed time of the evaluated lesson, seconds.</summary>
        public float durationSeconds;

        /// <summary>Number of objectives completed in the evaluated lesson.</summary>
        public int objectivesCompleted;

        /// <summary>Total objective count of the evaluated lesson.</summary>
        public int objectivesTotal;

        /// <summary>
        /// Converts a 0–100 score to a letter grade using thresholds
        /// A ≥ 90, B ≥ 80, C ≥ 70, D ≥ 60, otherwise F.
        /// </summary>
        public static string ScoreToLetter(float score)
        {
            if (score >= 90f) return "A";
            if (score >= 80f) return "B";
            if (score >= 70f) return "C";
            if (score >= 60f) return "D";
            return "F";
        }
    }

    /// <summary>
    /// Practical-test definition: an ordered sequence of lessons that must all
    /// be passed with <see cref="minimumPassScore"/> to earn a certification.
    /// </summary>
    [Serializable]
    public class CertificationExam
    {
        /// <summary>Certification the exam awards on success.</summary>
        public CertificationType certType;

        /// <summary>Lessons that make up the practical test, executed in order.</summary>
        public List<string> examLessonIds = new List<string>();

        /// <summary>Minimum score (0–100) required on every lesson.</summary>
        public float minimumPassScore = 70f;

        /// <summary>Maximum attempts the player may use before the exam is locked out.</summary>
        public int maxAttempts = 3;

        /// <summary>Attempt counter increment each time an exam session starts.</summary>
        public int attemptsUsed;

        /// <summary>Display-ready exam title.</summary>
        public string displayName;
    }

    /// <summary>
    /// A single node in the Flight School skill tree.
    /// Nodes unlock in a directed acyclic graph; completing a node's lesson
    /// unlocks its direct children.
    /// </summary>
    [Serializable]
    public class SkillNode
    {
        /// <summary>Unique node identifier.</summary>
        public string nodeId;

        /// <summary>Lesson that must be mastered to complete this node.</summary>
        public string lessonId;

        /// <summary>Child nodes unlocked when this node is completed.</summary>
        public List<string> childNodeIds = new List<string>();

        /// <summary>Whether the node has been unlocked for the local player.</summary>
        public bool isUnlocked;

        /// <summary>Position used by the skill-tree UI (x, y in pixels or grid units).</summary>
        public Vector2 uiPosition;

        /// <summary>Localisable display label.</summary>
        public string displayName;
    }

    /// <summary>Container for the full skill-tree graph.</summary>
    [Serializable]
    public class SkillTreeData
    {
        /// <summary>All nodes in the tree.</summary>
        public List<SkillNode> nodes = new List<SkillNode>();

        /// <summary>Looks up a node by id, returning <c>null</c> when not found.</summary>
        public SkillNode FindNode(string nodeId)
        {
            if (nodes == null || string.IsNullOrEmpty(nodeId)) return null;
            foreach (var n in nodes)
                if (n != null && n.nodeId == nodeId) return n;
            return null;
        }

        /// <summary>Returns all nodes whose <see cref="SkillNode.lessonId"/> equals <paramref name="lessonId"/>.</summary>
        public List<SkillNode> FindNodesByLesson(string lessonId)
        {
            var result = new List<SkillNode>();
            if (nodes == null || string.IsNullOrEmpty(lessonId)) return result;
            foreach (var n in nodes)
                if (n != null && n.lessonId == lessonId) result.Add(n);
            return result;
        }
    }
}
