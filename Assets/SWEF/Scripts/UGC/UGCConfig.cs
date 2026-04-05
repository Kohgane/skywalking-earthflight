// UGCConfig.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — Static configuration constants for the UGC system.
    ///
    /// <para>All tunable limits live here so they can be adjusted project-wide
    /// without searching through individual scripts.</para>
    /// </summary>
    public static class UGCConfig
    {
        // ── Content limits ─────────────────────────────────────────────────────

        /// <summary>Maximum number of waypoints allowed per content project.</summary>
        public const int MaxWaypoints = 200;

        /// <summary>Maximum number of event triggers per content project.</summary>
        public const int MaxTriggers = 50;

        /// <summary>Maximum number of zone areas per content project.</summary>
        public const int MaxZones = 30;

        /// <summary>Minimum waypoints required before a project is considered valid.</summary>
        public const int MinWaypointsRequired = 3;

        // ── Text limits ────────────────────────────────────────────────────────

        /// <summary>Maximum character count for a content title.</summary>
        public const int MaxTitleLength = 80;

        /// <summary>Minimum character count for a content title.</summary>
        public const int MinTitleLength = 3;

        /// <summary>Maximum character count for a content description.</summary>
        public const int MaxDescriptionLength = 1000;

        /// <summary>Maximum number of tags per content project.</summary>
        public const int MaxTagCount = 10;

        /// <summary>Maximum character count per individual tag.</summary>
        public const int MaxTagLength = 30;

        // ── File / size limits ─────────────────────────────────────────────────

        /// <summary>Maximum serialised content size in bytes (~512 KB).</summary>
        public const int MaxContentSizeBytes = 512 * 1024;

        /// <summary>Maximum thumbnail image size in bytes (~256 KB).</summary>
        public const int MaxThumbnailSizeBytes = 256 * 1024;

        /// <summary>File extension for exported UGC packages.</summary>
        public const string ExportExtension = ".swefugc";

        // ── Persistence paths ──────────────────────────────────────────────────

        /// <summary>Relative directory (inside <c>Application.persistentDataPath</c>) for project files.</summary>
        public const string ProjectsDirectory = "ugc_projects";

        /// <summary>File name for the installed-content library manifest.</summary>
        public const string LibraryFileName = "ugc_library.json";

        /// <summary>File name for published-content records.</summary>
        public const string PublishedFileName = "ugc_published.json";

        /// <summary>File name for the local review cache.</summary>
        public const string ReviewsFileName = "ugc_reviews.json";

        // ── Auto-save ──────────────────────────────────────────────────────────

        /// <summary>Interval in seconds between automatic project saves while the editor is open.</summary>
        public const float AutoSaveIntervalSeconds = 120f;

        /// <summary>Maximum undo history depth (number of reversible commands kept in memory).</summary>
        public const int MaxUndoHistory = 50;

        // ── Review / moderation ────────────────────────────────────────────────

        /// <summary>Duration in days a submission spends in the review queue before timing out.</summary>
        public const int ReviewPeriodDays = 7;

        /// <summary>Minimum average star rating (1–5) for a content to be eligible for featuring.</summary>
        public const float MinRatingForFeaturing = 4.2f;

        /// <summary>Minimum number of reviews required before featuring eligibility is checked.</summary>
        public const int MinReviewsForFeaturing = 10;

        // ── Quality score thresholds ───────────────────────────────────────────

        /// <summary>Quality score (0–100) at or above which content can be auto-published without manual review.</summary>
        public const int AutoPublishQualityThreshold = 80;

        /// <summary>Quality score below which content is sent for manual review before publishing.</summary>
        public const int ManualReviewQualityThreshold = 60;

        /// <summary>Quality score below which content is automatically rejected.</summary>
        public const int AutoRejectQualityThreshold = 30;

        // ── World / geo constraints ────────────────────────────────────────────

        /// <summary>Maximum distance in metres between consecutive waypoints before a warning is raised.</summary>
        public const float MaxWaypointSpacingMetres = 500_000f;

        /// <summary>Minimum trigger radius in metres.</summary>
        public const float MinTriggerRadiusMetres = 10f;

        /// <summary>Maximum trigger radius in metres.</summary>
        public const float MaxTriggerRadiusMetres = 50_000f;

        /// <summary>Maximum altitude in metres above sea level for any placed object.</summary>
        public const float MaxAltitudeMetres = 120_000f;

        // ── Browse / download ──────────────────────────────────────────────────

        /// <summary>Default number of content items fetched per browse page request.</summary>
        public const int BrowsePageSize = 20;

        /// <summary>Maximum search query length in characters.</summary>
        public const int MaxSearchQueryLength = 100;
    }
}
