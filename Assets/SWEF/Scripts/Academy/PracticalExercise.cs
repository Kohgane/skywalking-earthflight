// PracticalExercise.cs — SWEF Flight Academy & Certification System (Phase 104)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Academy
{
    /// <summary>Category of in-flight manoeuvre required for a practical exercise.</summary>
    public enum ExerciseType
    {
        /// <summary>Basic takeoff procedure from a runway or helipad.</summary>
        Takeoff,
        /// <summary>Landing approach and touchdown.</summary>
        Landing,
        /// <summary>Navigation between two or more waypoints.</summary>
        Navigation,
        /// <summary>Holding a heading, altitude and speed within defined tolerances.</summary>
        SteadyFlight,
        /// <summary>Emergency procedures (engine-out, forced landing, etc.).</summary>
        EmergencyProcedure,
        /// <summary>Formation flying within a specified envelope.</summary>
        FormationFlying,
        /// <summary>Instrument-only (simulated IMC) flight segment.</summary>
        InstrumentFlight,
        /// <summary>Aerial photography / objective-based free-flight task.</summary>
        FreeFlight
    }

    /// <summary>
    /// One measurable objective within a <see cref="PracticalExercise"/>.
    /// The <see cref="TrainingSessionManager"/> evaluates these at runtime.
    /// </summary>
    [Serializable]
    public class ExerciseObjective
    {
        /// <summary>Short description shown in the HUD (e.g. "Maintain altitude ±50 m").</summary>
        public string description;

        /// <summary>Unique tag used by the evaluator to identify this objective.</summary>
        public string objectiveTag;

        /// <summary>Maximum points awarded for completing this objective.</summary>
        [Min(0)]
        public int maxPoints = 10;

        /// <summary>Whether failing this objective also fails the entire exercise.</summary>
        public bool isMandatory = true;
    }

    /// <summary>
    /// Practical in-flight component of a <see cref="FlightLesson"/>.
    /// Defines the manoeuvres, success criteria, and scoring rules.
    /// </summary>
    [Serializable]
    public class PracticalExercise
    {
        /// <summary>Display title for this exercise.</summary>
        public string exerciseTitle;

        /// <summary>Briefing shown to the player before the exercise begins.</summary>
        [TextArea(2, 6)]
        public string briefingText;

        /// <summary>Which type of manoeuvre this exercise primarily tests.</summary>
        public ExerciseType exerciseType;

        /// <summary>Ordered list of measurable objectives.</summary>
        public List<ExerciseObjective> objectives = new List<ExerciseObjective>();

        /// <summary>Maximum time allowed to complete the exercise (seconds; 0 = unlimited).</summary>
        [Min(0f)]
        public float timeLimitSeconds = 0f;

        /// <summary>Minimum percentage of total points required to pass (0–100).</summary>
        [Range(0f, 100f)]
        public float passingScorePercent = 60f;

        // ── Scoring helper ─────────────────────────────────────────────────────

        /// <summary>
        /// Calculates the percentage score from a dictionary of earned points per objective tag.
        /// </summary>
        public float CalculateScore(Dictionary<string, int> earnedPoints)
        {
            if (objectives == null || objectives.Count == 0) return 0f;
            if (earnedPoints == null) return 0f;

            int total  = 0;
            int earned = 0;
            foreach (var obj in objectives)
            {
                total += obj.maxPoints;
                if (earnedPoints.TryGetValue(obj.objectiveTag, out int pts))
                    earned += Mathf.Clamp(pts, 0, obj.maxPoints);
            }
            return total > 0 ? (float)earned / total * 100f : 0f;
        }

        /// <summary>Returns <c>true</c> if the given score meets the passing threshold.</summary>
        public bool IsPassing(float score) => score >= passingScorePercent;

        public override string ToString() =>
            $"[Practical:{exerciseTitle}] Type: {exerciseType} | Pass: {passingScorePercent}%";
    }
}
