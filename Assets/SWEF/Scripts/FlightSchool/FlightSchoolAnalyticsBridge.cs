using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Bridges <see cref="FlightSchoolManager"/> and <see cref="FlightInstructor"/> events
    /// to the SWEF analytics pipeline (<c>SWEF.Analytics.UserBehaviorTracker</c>).
    /// Subscribe / unsubscribe automatically via <c>OnEnable</c> / <c>OnDisable</c>.
    /// </summary>
    public class FlightSchoolAnalyticsBridge : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [SerializeField] private FlightSchoolManager schoolManager;
        [SerializeField] private FlightInstructor    instructor;

        // ── Internal ─────────────────────────────────────────────────────────────

        private readonly Dictionary<string, float> _lessonStartTimes =
            new Dictionary<string, float>();

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (schoolManager == null) schoolManager = FlightSchoolManager.Instance;
        }

        private void OnEnable()
        {
            if (schoolManager != null)
            {
                schoolManager.OnLessonStarted       += HandleLessonStarted;
                schoolManager.OnLessonCompleted     += HandleLessonCompleted;
                schoolManager.OnCertificationEarned += HandleCertificationEarned;
                schoolManager.OnXpEarned            += HandleXpEarned;
            }
        }

        private void OnDisable()
        {
            if (schoolManager != null)
            {
                schoolManager.OnLessonStarted       -= HandleLessonStarted;
                schoolManager.OnLessonCompleted     -= HandleLessonCompleted;
                schoolManager.OnCertificationEarned -= HandleCertificationEarned;
                schoolManager.OnXpEarned            -= HandleXpEarned;
            }
        }

        // ── Public tracking methods ──────────────────────────────────────────────

        /// <summary>
        /// Logs a lesson-start event with lesson metadata to the analytics pipeline.
        /// </summary>
        /// <param name="lesson">The lesson that was started.</param>
        public void RecordLessonStart(FlightLesson lesson)
        {
            if (lesson == null) return;

            _lessonStartTimes[lesson.lessonId] = Time.realtimeSinceStartup;

            LogEvent("lesson_started", new Dictionary<string, object>
            {
                { "lesson_id",         lesson.lessonId },
                { "lesson_title",      lesson.title    },
                { "category",          lesson.category.ToString() },
                { "difficulty",        lesson.difficulty.ToString() },
                { "estimated_minutes", lesson.estimatedMinutes },
                { "completion_count",  lesson.completionCount }
            });
        }

        /// <summary>
        /// Logs a lesson-completion event with score and duration.
        /// </summary>
        /// <param name="lesson">The completed lesson.</param>
        /// <param name="score">Final score (0–100).</param>
        /// <param name="durationSeconds">Seconds elapsed since the lesson started.</param>
        public void RecordLessonComplete(FlightLesson lesson, float score, float durationSeconds)
        {
            if (lesson == null) return;

            LogEvent("lesson_completed", new Dictionary<string, object>
            {
                { "lesson_id",         lesson.lessonId },
                { "lesson_title",      lesson.title    },
                { "category",          lesson.category.ToString() },
                { "difficulty",        lesson.difficulty.ToString() },
                { "score",             score },
                { "duration_seconds",  durationSeconds },
                { "xp_reward",         lesson.xpReward },
                { "is_mastered",       lesson.status == LessonStatus.Mastered }
            });
        }

        /// <summary>
        /// Logs a certification-earned event.
        /// </summary>
        /// <param name="cert">The certification that was earned.</param>
        public void RecordCertificationEarned(PilotCertification cert)
        {
            if (cert == null) return;

            LogEvent("certification_earned", new Dictionary<string, object>
            {
                { "cert_type",    cert.certType.ToString() },
                { "display_name", cert.displayName },
                { "earned_date",  cert.earnedDate  }
            });
        }

        /// <summary>
        /// Logs a hint-request event so designers can identify lessons where players
        /// struggle.
        /// </summary>
        /// <param name="lessonId">Lesson in which the hint was requested.</param>
        /// <param name="objectiveId">The active objective when the hint was requested.</param>
        public void RecordHintRequested(string lessonId, string objectiveId)
        {
            LogEvent("hint_requested", new Dictionary<string, object>
            {
                { "lesson_id",    lessonId    },
                { "objective_id", objectiveId }
            });
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void HandleLessonStarted(FlightLesson lesson)  => RecordLessonStart(lesson);

        private void HandleLessonCompleted(FlightLesson lesson)
        {
            float duration = 0f;
            if (_lessonStartTimes.TryGetValue(lesson.lessonId, out float startTime))
            {
                duration = Time.realtimeSinceStartup - startTime;
                _lessonStartTimes.Remove(lesson.lessonId);
            }
            RecordLessonComplete(lesson, lesson.bestScore, duration);
        }

        private void HandleCertificationEarned(PilotCertification cert) => RecordCertificationEarned(cert);

        private void HandleXpEarned(int amount)
        {
            LogEvent("xp_earned", new Dictionary<string, object>
            {
                { "amount",         amount },
                { "total_xp",       schoolManager != null ? schoolManager.totalXpEarned : 0 }
            });
        }

        // ── Analytics bridge ─────────────────────────────────────────────────────

        /// <summary>
        /// Forwards events to <c>SWEF.Analytics.UserBehaviorTracker</c> via reflection.
        /// Falls back to <see cref="Debug.Log"/> in development builds when the
        /// analytics system is unavailable.
        /// </summary>
        private void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            var trackerType = Type.GetType("SWEF.Analytics.UserBehaviorTracker, Assembly-CSharp");
            if (trackerType != null)
            {
                var instanceProp = trackerType.GetProperty("Instance");
                var instance     = instanceProp?.GetValue(null) as MonoBehaviour;
                if (instance != null)
                {
                    var method = trackerType.GetMethod("TrackFeatureDiscovery",
                        new[] { typeof(string) });
                    method?.Invoke(instance, new object[] { eventName });
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var sb = new StringBuilder();
            sb.Append($"[SWEF Analytics] {eventName}");
            if (parameters != null && parameters.Count > 0)
            {
                sb.Append(" {");
                foreach (var kv in parameters)
                    sb.Append($" {kv.Key}={kv.Value},");
                sb.Append(" }");
            }
            Debug.Log(sb.ToString());
#endif
        }
    }
}
