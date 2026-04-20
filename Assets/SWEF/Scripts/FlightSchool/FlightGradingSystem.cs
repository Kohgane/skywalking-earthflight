using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Multi-criteria grading engine for Flight School lessons (Phase 84).
    /// Accepts per-criterion sample data during a lesson and produces a
    /// <see cref="LessonGradeReport"/> when the lesson finishes.
    ///
    /// Default criteria:
    ///   precision  — heading / altitude tracking error
    ///   smoothness — control-input jitter
    ///   timing     — finishing relative to estimated duration
    ///   safety     — stall / overspeed / terrain-warning avoidance
    ///   fuel       — fuel efficiency
    /// </summary>
    [DefaultExecutionOrder(-15)]
    public class FlightGradingSystem : MonoBehaviour
    {
        // ── Constants ────────────────────────────────────────────────────────────

        /// <summary>Default letter grade thresholds (A = 90+, B = 80+, C = 70+, D = 60+, F otherwise).</summary>
        public const float GradeAThreshold = 90f;
        public const float GradeBThreshold = 80f;
        public const float GradeCThreshold = 70f;
        public const float GradeDThreshold = 60f;

        // ── Singleton ────────────────────────────────────────────────────────────

        /// <summary>Optional singleton accessor. Not required — grading can be used per-instance.</summary>
        public static FlightGradingSystem Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired once a <see cref="LessonGradeReport"/> has been produced.</summary>
        public event Action<LessonGradeReport> OnGradeCalculated;

        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Default Weights")]
        [Tooltip("Relative weight for the 'precision' criterion.")]
        [Range(0f, 5f)] [SerializeField] private float precisionWeight  = 2f;

        [Tooltip("Relative weight for the 'smoothness' criterion.")]
        [Range(0f, 5f)] [SerializeField] private float smoothnessWeight = 1.5f;

        [Tooltip("Relative weight for the 'timing' criterion.")]
        [Range(0f, 5f)] [SerializeField] private float timingWeight     = 1f;

        [Tooltip("Relative weight for the 'safety' criterion.")]
        [Range(0f, 5f)] [SerializeField] private float safetyWeight     = 2.5f;

        [Tooltip("Relative weight for the 'fuel efficiency' criterion.")]
        [Range(0f, 5f)] [SerializeField] private float fuelWeight       = 1f;

        // ── Internal state ───────────────────────────────────────────────────────

        private readonly Dictionary<string, float> _criteriaScores = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _criteriaWeights = new Dictionary<string, float>();
        private bool _isSessionActive;
        private float _sessionStartTime;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ResetToDefaults();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Starts a fresh grading session — resets all criterion scores to 100.</summary>
        public void BeginSession()
        {
            ResetToDefaults();
            foreach (var id in new List<string>(_criteriaScores.Keys))
                _criteriaScores[id] = 100f;
            _sessionStartTime = Time.time;
            _isSessionActive  = true;
        }

        /// <summary>
        /// Records a sample for <paramref name="criteriaId"/>.
        /// <paramref name="sampleScore"/> must be in [0, 100] — lower values reduce
        /// the running criterion score using an exponential moving average.
        /// </summary>
        public void RecordSample(string criteriaId, float sampleScore)
        {
            if (!_isSessionActive || string.IsNullOrEmpty(criteriaId)) return;

            sampleScore = Mathf.Clamp(sampleScore, 0f, 100f);

            if (!_criteriaScores.TryGetValue(criteriaId, out float current))
                current = 100f;

            // EMA with α = 0.1 — smooths out spikes but remains responsive.
            _criteriaScores[criteriaId] = current * 0.9f + sampleScore * 0.1f;
        }

        /// <summary>
        /// Directly sets the final score for <paramref name="criteriaId"/>, bypassing the EMA.
        /// Useful for binary criteria like "safety violation occurred".
        /// </summary>
        public void SetCriterionScore(string criteriaId, float score)
        {
            if (string.IsNullOrEmpty(criteriaId)) return;
            _criteriaScores[criteriaId] = Mathf.Clamp(score, 0f, 100f);
        }

        /// <summary>
        /// Overrides the default weight for <paramref name="criteriaId"/>.
        /// Weights do not need to sum to any particular value — they are normalised
        /// automatically at evaluation time.
        /// </summary>
        public void SetCriterionWeight(string criteriaId, float weight)
        {
            if (string.IsNullOrEmpty(criteriaId)) return;
            _criteriaWeights[criteriaId] = Mathf.Max(0f, weight);
        }

        /// <summary>
        /// Produces the final <see cref="LessonGradeReport"/> for the active lesson,
        /// ending the session, and fires <see cref="OnGradeCalculated"/>.
        /// </summary>
        /// <param name="lesson">Lesson whose score is being finalised.</param>
        public LessonGradeReport Finalize(FlightLesson lesson)
        {
            var report = new LessonGradeReport
            {
                lessonId        = lesson?.lessonId ?? string.Empty,
                durationSeconds = _isSessionActive ? Time.time - _sessionStartTime : 0f,
                timestamp       = DateTime.UtcNow.ToString("o"),
                criteria        = new List<GradeCriteria>()
            };

            if (lesson != null && lesson.objectives != null)
            {
                report.objectivesTotal = lesson.objectives.Count;
                foreach (var o in lesson.objectives)
                    if (o != null && o.isCompleted) report.objectivesCompleted++;
            }

            float totalWeight = 0f;
            foreach (var kvp in _criteriaScores)
            {
                float w = _criteriaWeights.TryGetValue(kvp.Key, out float configured) ? configured : 1f;
                report.criteria.Add(new GradeCriteria
                {
                    criteriaId  = kvp.Key,
                    displayName = ToDisplayName(kvp.Key),
                    weight      = w,
                    score       = Mathf.Clamp(kvp.Value, 0f, 100f)
                });
                totalWeight += w;
            }

            report.finalScore  = ComputeFinalScore(report.criteria, totalWeight);
            report.letterGrade = LessonGradeReport.ScoreToLetter(report.finalScore);

            _isSessionActive = false;
            OnGradeCalculated?.Invoke(report);
            return report;
        }

        /// <summary>Aborts the current session without emitting a report.</summary>
        public void Cancel()
        {
            _isSessionActive = false;
        }

        /// <summary>Returns the current running score for <paramref name="criteriaId"/>, or 0 when unknown.</summary>
        public float GetCriterionScore(string criteriaId)
        {
            return _criteriaScores.TryGetValue(criteriaId, out float v) ? v : 0f;
        }

        /// <summary>Returns the current running aggregate score without finalising the session.</summary>
        public float GetLiveAggregateScore()
        {
            if (_criteriaScores.Count == 0) return 0f;

            float total = 0f, weightSum = 0f;
            foreach (var kvp in _criteriaScores)
            {
                float w = _criteriaWeights.TryGetValue(kvp.Key, out float configured) ? configured : 1f;
                total     += w * kvp.Value;
                weightSum += w;
            }
            return weightSum > 0f ? total / weightSum : 0f;
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Computes the weighted average of per-criterion scores, returning 0
        /// when no criteria or weights are present.
        /// </summary>
        public static float ComputeFinalScore(List<GradeCriteria> criteria, float totalWeight)
        {
            if (criteria == null || criteria.Count == 0 || totalWeight <= 0f) return 0f;

            float sum = 0f;
            foreach (var c in criteria)
                sum += c.WeightedScore();

            return Mathf.Clamp(sum / totalWeight, 0f, 100f);
        }

        private void ResetToDefaults()
        {
            _criteriaScores.Clear();
            _criteriaWeights.Clear();

            _criteriaScores["precision"]  = 100f; _criteriaWeights["precision"]  = precisionWeight;
            _criteriaScores["smoothness"] = 100f; _criteriaWeights["smoothness"] = smoothnessWeight;
            _criteriaScores["timing"]     = 100f; _criteriaWeights["timing"]     = timingWeight;
            _criteriaScores["safety"]     = 100f; _criteriaWeights["safety"]     = safetyWeight;
            _criteriaScores["fuel"]       = 100f; _criteriaWeights["fuel"]       = fuelWeight;
        }

        private static string ToDisplayName(string criteriaId)
        {
            switch (criteriaId)
            {
                case "precision":  return "Precision";
                case "smoothness": return "Smoothness";
                case "timing":     return "Timing";
                case "safety":     return "Safety";
                case "fuel":       return "Fuel Efficiency";
                default:           return criteriaId;
            }
        }
    }
}
