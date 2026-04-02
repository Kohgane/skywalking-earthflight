// CompetitiveRacingAnalytics.cs — SWEF Competitive Racing & Time Trial System (Phase 88)

#if SWEF_ANALYTICS_AVAILABLE
using SWEF.Analytics;
#endif

namespace SWEF.CompetitiveRacing
{
    /// <summary>
    /// Phase 88 — Static utility class that dispatches Competitive Racing telemetry
    /// events to <c>SWEF.Analytics.TelemetryDispatcher</c>.
    ///
    /// <para>All methods are no-ops when <c>SWEF_ANALYTICS_AVAILABLE</c> is not defined,
    /// so they are always safe to call.</para>
    /// </summary>
    public static class CompetitiveRacingAnalytics
    {
        // ── Race Session ──────────────────────────────────────────────────────────

        /// <summary>Records that a race session was started on <paramref name="courseId"/>.</summary>
        public static void RecordRaceStart(string courseId, RaceMode mode)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("race_start", new System.Collections.Generic.Dictionary<string, object>
            {
                { "course_id", courseId },
                { "mode", mode.ToString() }
            });
#endif
        }

        /// <summary>Records that a race was finished, including total time and medal earned.</summary>
        public static void RecordRaceFinish(string courseId, float totalTime, string medal)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("race_finish", new System.Collections.Generic.Dictionary<string, object>
            {
                { "course_id",   courseId   },
                { "total_time",  totalTime  },
                { "medal",       medal      }
            });
#endif
        }

        // ── Splits ────────────────────────────────────────────────────────────────

        /// <summary>Records the split time at a specific checkpoint index.</summary>
        public static void RecordCheckpointSplit(string courseId, int checkpointIndex, float splitTime)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("checkpoint_split", new System.Collections.Generic.Dictionary<string, object>
            {
                { "course_id",        courseId        },
                { "checkpoint_index", checkpointIndex },
                { "split_time",       splitTime       }
            });
#endif
        }

        // ── Records ───────────────────────────────────────────────────────────────

        /// <summary>Records that a player set a new personal best on <paramref name="courseId"/>.</summary>
        public static void RecordPersonalBest(string courseId, float totalTime)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("personal_best", new System.Collections.Generic.Dictionary<string, object>
            {
                { "course_id",  courseId  },
                { "total_time", totalTime }
            });
#endif
        }

        /// <summary>Records that a global course record was broken.</summary>
        public static void RecordNewRecord(string courseId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("new_record", new System.Collections.Generic.Dictionary<string, object>
            {
                { "course_id", courseId }
            });
#endif
        }

        // ── Course Authoring ──────────────────────────────────────────────────────

        /// <summary>Records that a new course was created.</summary>
        public static void RecordCourseCreated(string courseId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("course_created", new System.Collections.Generic.Dictionary<string, object>
            {
                { "course_id", courseId }
            });
#endif
        }

        /// <summary>Records that a course was shared via any channel.</summary>
        public static void RecordCourseShared(string courseId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("course_shared", new System.Collections.Generic.Dictionary<string, object>
            {
                { "course_id", courseId }
            });
#endif
        }

        /// <summary>Records a player rating submission for a course.</summary>
        public static void RecordCourseRated(string courseId, int rating)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("course_rated", new System.Collections.Generic.Dictionary<string, object>
            {
                { "course_id", courseId },
                { "rating",    rating   }
            });
#endif
        }
    }
}
