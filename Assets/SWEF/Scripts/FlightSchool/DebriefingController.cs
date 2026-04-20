using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Post-lesson debrief manager (Phase 84).
    /// Builds human-readable debrief data from a <see cref="LessonGradeReport"/>:
    /// grade breakdown, personal-best comparison, actionable tips based on the
    /// weakest criteria, and optional replay offer. The UI layer binds to
    /// <see cref="OnDebriefReady"/> to render the result.
    /// </summary>
    public class DebriefingController : MonoBehaviour
    {
        /// <summary>Structured tip payload surfaced to the player after a lesson.</summary>
        [Serializable]
        public class DebriefTip
        {
            /// <summary>Criterion the tip targets (e.g. "precision").</summary>
            public string criteriaId;

            /// <summary>Short actionable advice.</summary>
            public string message;

            /// <summary>Severity in [0, 1]: 1.0 is "fix this first".</summary>
            public float severity;
        }

        /// <summary>Aggregated debrief view model forwarded to the UI.</summary>
        public class DebriefPayload
        {
            public FlightLesson        lesson;
            public LessonGradeReport   report;
            public bool                isNewPersonalBest;
            public float               previousBestScore;
            public List<DebriefTip>    tips = new List<DebriefTip>();
        }

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when a new debrief payload has been constructed.</summary>
        public event Action<DebriefPayload> OnDebriefReady;

        /// <summary>Fired when the player explicitly closes the debrief screen.</summary>
        public event Action OnDebriefClosed;

        // ── Inspector ────────────────────────────────────────────────────────────

        [SerializeField] private FlightSchoolManager schoolManager;

        [Tooltip("Maximum number of tips surfaced in a single debrief.")]
        [Range(0, 5)] [SerializeField] private int maxTips = 3;

        [Tooltip("Criteria with score below this threshold become tip candidates.")]
        [Range(0f, 100f)] [SerializeField] private float tipThreshold = 80f;

        // ── Public state ─────────────────────────────────────────────────────────

        /// <summary>Most recently emitted debrief payload, or <c>null</c>.</summary>
        public DebriefPayload LastPayload { get; private set; }

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (schoolManager == null) schoolManager = FlightSchoolManager.Instance;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a <see cref="DebriefPayload"/> from a completed lesson / grade report
        /// and emits <see cref="OnDebriefReady"/>.
        /// </summary>
        public DebriefPayload ShowDebrief(FlightLesson lesson, LessonGradeReport report)
        {
            if (lesson == null || report == null) return null;

            var payload = new DebriefPayload
            {
                lesson            = lesson,
                report            = report,
                previousBestScore = lesson.bestScore,
                isNewPersonalBest = report.finalScore > lesson.bestScore,
                tips              = BuildTips(report)
            };

            LastPayload = payload;
            OnDebriefReady?.Invoke(payload);
            return payload;
        }

        /// <summary>Clears the payload and notifies listeners the debrief is dismissed.</summary>
        public void CloseDebrief()
        {
            LastPayload = null;
            OnDebriefClosed?.Invoke();
        }

        /// <summary>
        /// Builds ranked tips from the weakest criteria in <paramref name="report"/>.
        /// Only criteria scoring below <see cref="tipThreshold"/> become candidates.
        /// </summary>
        public List<DebriefTip> BuildTips(LessonGradeReport report)
        {
            var result = new List<DebriefTip>();
            if (report?.criteria == null) return result;

            var candidates = new List<GradeCriteria>();
            foreach (var c in report.criteria)
                if (c.score < tipThreshold) candidates.Add(c);

            candidates.Sort((a, b) => a.score.CompareTo(b.score));

            int take = Mathf.Min(maxTips, candidates.Count);
            for (int i = 0; i < take; i++)
            {
                var c = candidates[i];
                result.Add(new DebriefTip
                {
                    criteriaId = c.criteriaId,
                    message    = GenerateTipMessage(c.criteriaId, c.score),
                    severity   = Mathf.Clamp01((tipThreshold - c.score) / tipThreshold)
                });
            }
            return result;
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns a coaching message appropriate for <paramref name="criteriaId"/>
        /// and <paramref name="score"/>. Pure function — safe to test in isolation.
        /// </summary>
        public static string GenerateTipMessage(string criteriaId, float score)
        {
            if (string.IsNullOrEmpty(criteriaId)) return string.Empty;

            string severity = score < 50f ? "significantly" : score < 70f ? "sometimes" : "slightly";

            switch (criteriaId)
            {
                case "precision":
                    return $"Your altitude/heading tracking drifted {severity}. Trim the aircraft and make small corrections.";
                case "smoothness":
                    return $"Control inputs {severity} jerky. Move the stick/yoke gradually and avoid over-correcting.";
                case "timing":
                    return $"Lesson took {severity} longer than the estimate. Plan maneuvers earlier.";
                case "safety":
                    return $"Stall/overspeed/terrain warnings triggered {severity}. Keep one eye on the warning indicators.";
                case "fuel":
                    return $"Fuel burn was {severity} high. Lean the mixture and fly at economy cruise power.";
                default:
                    return $"Review the '{criteriaId}' criterion — scored {score:F0}/100.";
            }
        }
    }
}
