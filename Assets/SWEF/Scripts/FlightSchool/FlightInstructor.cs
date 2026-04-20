using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Provides real-time in-flight guidance during a <see cref="FlightLesson"/>.
    /// Attach to a GameObject alongside <see cref="FlightSchoolManager"/>.
    /// Call <see cref="BeginInstruction"/> to start, then poll <see cref="UpdateInstruction"/>
    /// from <c>Update()</c> (or invoke it yourself each frame).
    /// </summary>
    public class FlightInstructor : MonoBehaviour
    {
        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Raised when the instructor produces a new step message or hint.</summary>
        public event Action<string> OnInstructionStep;

        /// <summary>Raised when the current <see cref="LessonObjective"/> is completed.</summary>
        public event Action<LessonObjective> OnObjectiveComplete;

        /// <summary>Raised when the lesson is fully evaluated, carrying the final score (0–100).</summary>
        public event Action<float> OnLessonEvaluated;

        // ── Inspector ────────────────────────────────────────────────────────────

        [SerializeField] private FlightSchoolManager schoolManager;

        [Tooltip("Optional grading engine used for detailed multi-criteria scoring (Phase 84).")]
        [SerializeField] private FlightGradingSystem gradingSystem;

        [Tooltip("Optional constraint enforcer wired automatically when the lesson starts (Phase 84).")]
        [SerializeField] private FlightConstraintEnforcer constraintEnforcer;

        [Tooltip("Minimum seconds between automatically delivered hints.")]
        [SerializeField] private float hintCooldown = 15f;

        // ── Public state ─────────────────────────────────────────────────────────

        /// <summary>The lesson currently being instructed, or <c>null</c> when idle.</summary>
        public FlightLesson activeLesson { get; private set; }

        /// <summary>Index of the objective that is currently active.</summary>
        public int currentObjectiveIndex { get; private set; }

        // ── Internal ─────────────────────────────────────────────────────────────

        /// <summary>Seconds since a hint was last delivered.</summary>
        public float timeSinceLastHint { get; private set; }

        private float _lessonStartTime;
        private float _totalDeviationPenalty;   // accumulated per-frame penalty (0–100)
        private bool  _isActive;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (schoolManager == null)
                schoolManager = FlightSchoolManager.Instance;
            if (gradingSystem == null)
                gradingSystem = FlightGradingSystem.Instance;
        }

        private void Update()
        {
            if (_isActive)
                UpdateInstruction();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Begins the guided instruction sequence for <paramref name="lesson"/>.
        /// Resets internal state and requests the manager to start the lesson.
        /// </summary>
        /// <param name="lesson">The lesson to teach.</param>
        public void BeginInstruction(FlightLesson lesson)
        {
            if (lesson == null) return;

            activeLesson           = lesson;
            currentObjectiveIndex  = 0;
            timeSinceLastHint      = hintCooldown; // allow immediate first hint
            _lessonStartTime       = Time.time;
            _totalDeviationPenalty = 0f;
            _isActive              = true;

            // Phase 84: begin a grading session + apply lesson constraints.
            gradingSystem?.BeginSession();
            if (constraintEnforcer != null)
                constraintEnforcer.SetConstraints(lesson.constraints);

            schoolManager?.StartLesson(lesson.lessonId);

            string firstObjectiveDesc = lesson.objectives != null && lesson.objectives.Count > 0
                ? lesson.objectives[0].description
                : "Follow instructions.";

            OnInstructionStep?.Invoke($"Lesson started: {lesson.title}. Objective: {firstObjectiveDesc}");
        }

        /// <summary>
        /// Checks whether the current objective has been satisfied and advances when needed.
        /// Should be called every frame while a lesson is in progress.
        /// </summary>
        public void UpdateInstruction()
        {
            if (!_isActive || activeLesson == null) return;

            timeSinceLastHint += Time.deltaTime;

            if (activeLesson.objectives == null || activeLesson.objectives.Count == 0)
                return;

            if (currentObjectiveIndex >= activeLesson.objectives.Count)
                return;

            var currentObjective = activeLesson.objectives[currentObjectiveIndex];

            if (currentObjective.isCompleted)
            {
                OnObjectiveComplete?.Invoke(currentObjective);
                AdvanceToNextObjective();
            }
        }

        /// <summary>
        /// Moves to the next objective in the active lesson.
        /// Ends the lesson automatically when all objectives are complete.
        /// </summary>
        public void AdvanceToNextObjective()
        {
            if (activeLesson == null) return;

            currentObjectiveIndex++;

            if (activeLesson.objectives != null
                && currentObjectiveIndex < activeLesson.objectives.Count)
            {
                var next = activeLesson.objectives[currentObjectiveIndex];
                OnInstructionStep?.Invoke($"Next objective: {next.description}");
            }
            else
            {
                // All objectives done — end lesson
                float score = EndInstruction();
                OnInstructionStep?.Invoke($"Lesson complete! Score: {score:F0}/100");
            }
        }

        /// <summary>
        /// Returns a contextual hint string for the current objective.
        /// Resets <see cref="timeSinceLastHint"/> to enforce the cooldown.
        /// </summary>
        /// <returns>Hint text, or an empty string when on cooldown.</returns>
        public string ProvideHint()
        {
            if (!_isActive || activeLesson == null) return string.Empty;

            if (timeSinceLastHint < hintCooldown) return string.Empty;

            timeSinceLastHint = 0f;

            if (activeLesson.objectives == null
                || currentObjectiveIndex >= activeLesson.objectives.Count)
                return "Follow the instructor's guidance.";

            var obj  = activeLesson.objectives[currentObjectiveIndex];
            float pct = obj.Progress01() * 100f;

            string hint = pct < 25f
                ? $"You're just getting started on '{obj.description}'. Keep going!"
                : pct < 50f
                    ? $"Good progress on '{obj.description}'. You're {pct:F0}% there."
                    : pct < 75f
                        ? $"Almost halfway through '{obj.description}'. Stay focused."
                        : $"Nearly done with '{obj.description}'! Push through to the end.";

            OnInstructionStep?.Invoke($"Hint: {hint}");
            return hint;
        }

        /// <summary>
        /// Scores the player's execution based on objective completion and deviation penalties.
        /// Returns a value in the range [0, 100].
        /// </summary>
        /// <returns>Score in [0, 100].</returns>
        public float EvaluatePerformance()
        {
            if (activeLesson == null) return 0f;

            // Phase 84: when a grading system is available, use its weighted aggregate.
            if (gradingSystem != null)
            {
                float liveScore = gradingSystem.GetLiveAggregateScore();
                if (liveScore > 0f) return Mathf.Clamp(liveScore - _totalDeviationPenalty, 0f, 100f);
            }

            if (activeLesson.objectives == null || activeLesson.objectives.Count == 0)
                return Mathf.Clamp01(1f - _totalDeviationPenalty / 100f) * 100f;

            // Base score: percentage of objectives completed
            int completed = 0;
            foreach (var obj in activeLesson.objectives)
                if (obj.isCompleted) completed++;

            float objectiveScore = (float)completed / activeLesson.objectives.Count * 80f;

            // Time bonus: up to 20 points for finishing quickly (relative to estimated time)
            float elapsedMinutes   = (Time.time - _lessonStartTime) / 60f;
            float estimatedMinutes = Mathf.Max(1f, activeLesson.estimatedMinutes);
            float timeBonus        = Mathf.Clamp01(1f - elapsedMinutes / (estimatedMinutes * 1.5f)) * 20f;

            float raw = objectiveScore + timeBonus - _totalDeviationPenalty;
            return Mathf.Clamp(raw, 0f, 100f);
        }

        /// <summary>
        /// Finalises the lesson, evaluates performance, notifies the manager, and
        /// raises <see cref="OnLessonEvaluated"/>.
        /// </summary>
        /// <returns>The final score in [0, 100].</returns>
        public float EndInstruction()
        {
            _isActive = false;

            float score = EvaluatePerformance();
            schoolManager?.CompleteLesson(activeLesson?.lessonId, score);
            OnLessonEvaluated?.Invoke(score);

            activeLesson = null;
            return score;
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Accumulates a deviation penalty (called by gameplay systems when the player
        /// wanders outside acceptable parameters).
        /// </summary>
        /// <param name="penaltyAmount">Penalty to add (positive value).</param>
        public void AddDeviationPenalty(float penaltyAmount)
        {
            _totalDeviationPenalty = Mathf.Min(_totalDeviationPenalty + penaltyAmount, 50f);
        }
    }
}
