// UGCAnalytics.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — Static telemetry helper for UGC-related events.
    ///
    /// <para>All methods are no-ops unless <c>SWEF_ANALYTICS_AVAILABLE</c> is defined,
    /// in which case events are forwarded to <c>SWEF.Analytics.AnalyticsManager</c>.</para>
    /// </summary>
    public static class UGCAnalytics
    {
        // ── Editor events ──────────────────────────────────────────────────────

        /// <summary>Tracks the UGC editor being opened by the player.</summary>
        public static void TrackEditorOpened()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("ugc_editor_opened");
#endif
        }

        /// <summary>Tracks the creation of a new UGC project.</summary>
        /// <param name="contentType">String representation of the <see cref="UGCContentType"/>.</param>
        public static void TrackProjectCreated(string contentType)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("ugc_project_created",
                ("content_type", contentType));
#endif
        }

        /// <summary>Tracks a project save operation.</summary>
        /// <param name="contentId">ID of the content project saved.</param>
        public static void TrackProjectSaved(string contentId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("ugc_project_saved",
                ("content_id", contentId));
#endif
        }

        /// <summary>Tracks a content test-play session.</summary>
        /// <param name="contentId">ID of the content tested.</param>
        /// <param name="passed">Whether the test-play passed.</param>
        public static void TrackContentTested(string contentId, bool passed)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("ugc_content_tested",
                ("content_id", contentId),
                ("passed",     passed.ToString()));
#endif
        }

        // ── Publishing events ──────────────────────────────────────────────────

        /// <summary>Tracks a content publish operation.</summary>
        public static void TrackContentPublished(string contentId, string contentType)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("ugc_content_published",
                ("content_id",   contentId),
                ("content_type", contentType));
#endif
        }

        /// <summary>Tracks a content download.</summary>
        public static void TrackContentDownloaded(string contentId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("ugc_content_downloaded",
                ("content_id", contentId));
#endif
        }

        /// <summary>Tracks a content play-through.</summary>
        /// <param name="contentId">ID of the content played.</param>
        /// <param name="completionSeconds">Total play time in seconds.</param>
        public static void TrackContentPlayed(string contentId, float completionSeconds)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("ugc_content_played",
                ("content_id",          contentId),
                ("completion_seconds",  completionSeconds.ToString("F0")));
#endif
        }

        // ── Community events ───────────────────────────────────────────────────

        /// <summary>Tracks a review submission.</summary>
        public static void TrackReviewSubmitted(string contentId, int starRating)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("ugc_review_submitted",
                ("content_id",  contentId),
                ("star_rating", starRating.ToString()));
#endif
        }

        /// <summary>Tracks a share action.</summary>
        public static void TrackContentShared(string contentId, string shareMethod)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("ugc_content_shared",
                ("content_id",   contentId),
                ("share_method", shareMethod));
#endif
        }

        /// <summary>Tracks a content report/flag.</summary>
        public static void TrackContentReported(string contentId, string reason)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("ugc_content_reported",
                ("content_id", contentId),
                ("reason",     reason));
#endif
        }
    }
}
