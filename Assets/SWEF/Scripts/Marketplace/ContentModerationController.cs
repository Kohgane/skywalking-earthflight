// ContentModerationController.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>Reason a listing or creator was reported.</summary>
    public enum ModerationReportReason
    {
        /// <summary>Inappropriate or offensive content.</summary>
        Inappropriate,

        /// <summary>Potential copyright violation.</summary>
        CopyrightViolation,

        /// <summary>Content is broken or non-functional.</summary>
        Broken,

        /// <summary>Spam or misleading listing.</summary>
        Spam,

        /// <summary>Content provides an unfair gameplay advantage (cheating).</summary>
        Cheating,
    }

    /// <summary>
    /// A single moderation report (listing or creator).
    /// </summary>
    [Serializable]
    public class ModerationReport
    {
        /// <summary>Unique report identifier.</summary>
        public string reportId = Guid.NewGuid().ToString();

        /// <summary>ID of the listing or creator being reported.</summary>
        public string targetId = string.Empty;

        /// <summary>Whether the target is a listing (<c>true</c>) or a creator (<c>false</c>).</summary>
        public bool isListingReport = true;

        /// <summary>Player ID of the reporter.</summary>
        public string reporterId = string.Empty;

        /// <summary>Report reason.</summary>
        public ModerationReportReason reason = ModerationReportReason.Inappropriate;

        /// <summary>UTC timestamp (ISO-8601).</summary>
        public string createdAt = DateTime.UtcNow.ToString("o");

        /// <summary>Whether this report has been reviewed by a moderator.</summary>
        public bool isReviewed;
    }

    /// <summary>
    /// Singleton that handles content moderation: auto-validation on publish,
    /// community-submitted reports, and auto-flag logic when a threshold is exceeded.
    ///
    /// <para>Persistence: <c>moderation_reports.json</c>.</para>
    /// </summary>
    public class ContentModerationController : MonoBehaviour
    {
        #region Singleton

        /// <summary>Shared singleton instance.</summary>
        public static ContentModerationController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _reportsPath = Path.Combine(Application.persistentDataPath, "moderation_reports.json");
            LoadReports();
        }

        #endregion

        #region Inspector

        [Header("Auto-Flag Settings")]
        [Tooltip("Number of distinct reports on the same target before it is auto-flagged.")]
        [SerializeField] private int _autoFlagThreshold = 3;

        [Tooltip("Maximum content-data payload length (characters) allowed for publishing.")]
        [SerializeField] private int _maxContentDataLength = 65536;

        #endregion

        #region Private State

        private readonly List<ModerationReport> _reports = new List<ModerationReport>();
        private string _reportsPath;
        private string LocalPlayerId => "local_player";

        #endregion

        #region Public API — Report Listing

        /// <summary>
        /// Submits a community report against a listing.
        /// Automatically unpublishes the listing if the auto-flag threshold is exceeded.
        /// </summary>
        /// <param name="listingId">Listing to report.</param>
        /// <param name="reason">Report reason.</param>
        public void ReportListing(string listingId, string reason)
        {
            if (string.IsNullOrEmpty(listingId)) return;

            if (!Enum.TryParse<ModerationReportReason>(reason, true, out var parsedReason))
                parsedReason = ModerationReportReason.Inappropriate;

            var report = new ModerationReport
            {
                targetId        = listingId,
                isListingReport = true,
                reporterId      = LocalPlayerId,
                reason          = parsedReason,
            };

            _reports.Add(report);
            SaveReports();

            MarketplaceAnalytics.RecordContentReported(listingId, parsedReason.ToString());

            CheckAutoFlag(listingId, isListing: true);
        }

        /// <summary>
        /// Overload that accepts a typed <see cref="ModerationReportReason"/>.
        /// </summary>
        public void ReportListing(string listingId, ModerationReportReason reason)
            => ReportListing(listingId, reason.ToString());

        #endregion

        #region Public API — Report Creator

        /// <summary>
        /// Submits a community report against a creator.
        /// </summary>
        /// <param name="creatorId">Creator to report.</param>
        /// <param name="reason">Report reason.</param>
        public void ReportCreator(string creatorId, string reason)
        {
            if (string.IsNullOrEmpty(creatorId)) return;

            if (!Enum.TryParse<ModerationReportReason>(reason, true, out var parsedReason))
                parsedReason = ModerationReportReason.Inappropriate;

            var report = new ModerationReport
            {
                targetId        = creatorId,
                isListingReport = false,
                reporterId      = LocalPlayerId,
                reason          = parsedReason,
            };

            _reports.Add(report);
            SaveReports();

            MarketplaceAnalytics.RecordContentReported(creatorId, parsedReason.ToString());
        }

        /// <summary>
        /// Overload that accepts a typed <see cref="ModerationReportReason"/>.
        /// </summary>
        public void ReportCreator(string creatorId, ModerationReportReason reason)
            => ReportCreator(creatorId, reason.ToString());

        #endregion

        #region Public API — Auto-Validation

        /// <summary>
        /// Runs automated validation checks on content before publishing:
        /// size limits, profanity in title/description, and basic data-integrity checks.
        /// </summary>
        /// <param name="listing">Listing to validate.</param>
        /// <returns><c>true</c> if the listing passes all checks.</returns>
        public bool AutoValidate(MarketplaceListingData listing)
        {
            if (listing == null) return false;

            if (listing.contentData?.Length > _maxContentDataLength)
            {
                Debug.LogWarning($"[SWEF] Marketplace: AutoValidate — content data too large for listing {listing.listingId}.");
                return false;
            }

#if SWEF_SECURITY_AVAILABLE
            if (SWEF.Security.ProfanityFilter.ContainsProfanity(listing.title) ||
                SWEF.Security.ProfanityFilter.ContainsProfanity(listing.description))
            {
                Debug.LogWarning($"[SWEF] Marketplace: AutoValidate — profanity detected in listing {listing.listingId}.");
                return false;
            }
#endif

            return true;
        }

        #endregion

        #region Public API — Query

        /// <summary>Returns all moderation reports for a given target.</summary>
        /// <param name="targetId">Listing or creator ID.</param>
        public List<ModerationReport> GetReportsFor(string targetId)
        {
            return _reports.Where(r => r.targetId == targetId).ToList();
        }

        #endregion

        #region Private — Auto-Flag

        private void CheckAutoFlag(string targetId, bool isListing)
        {
            int reportCount = _reports.Count(r => r.targetId == targetId && r.isListingReport == isListing);

            if (reportCount < _autoFlagThreshold) return;

            if (isListing)
            {
                var mgr = MarketplaceManager.Instance;
                if (mgr == null) return;

                var listing = mgr.GetListingById(targetId);
                if (listing != null && listing.isPublished)
                {
                    mgr.UnpublishListing(targetId);
                    Debug.LogWarning($"[SWEF] Marketplace: Listing {targetId} auto-flagged and unpublished after {reportCount} reports.");
                }
            }
        }

        #endregion

        #region Persistence

        private void SaveReports()
        {
            try
            {
                string json = JsonUtility.ToJson(new ReportsWrapper { reports = _reports }, true);
                File.WriteAllText(_reportsPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to save moderation reports — {ex.Message}");
            }
        }

        private void LoadReports()
        {
            _reports.Clear();
            if (!File.Exists(_reportsPath)) return;
            try
            {
                var wrapper = JsonUtility.FromJson<ReportsWrapper>(File.ReadAllText(_reportsPath));
                if (wrapper?.reports != null)
                    _reports.AddRange(wrapper.reports);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to load moderation reports — {ex.Message}");
            }
        }

        [Serializable] private class ReportsWrapper { public List<ModerationReport> reports; }

        #endregion
    }
}
