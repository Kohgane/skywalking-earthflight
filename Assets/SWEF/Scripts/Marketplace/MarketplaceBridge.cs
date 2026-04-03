// MarketplaceBridge.cs — SWEF Community Content Marketplace (Phase 94)
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>
    /// Static bridge that connects the Marketplace system to the wider SWEF ecosystem:
    ///
    /// <list type="bullet">
    ///   <item><see cref="SWEF.Progression.ProgressionManager"/> — deduct/add currency on transactions.</item>
    ///   <item><see cref="SWEF.Workshop.WorkshopManager"/> — import purchased builds/liveries/decals.</item>
    ///   <item><see cref="SWEF.Multiplayer.SharedWaypointManager"/> — import waypoint packs.</item>
    ///   <item><see cref="SWEF.Achievement.AchievementManager"/> — marketplace achievements.</item>
    ///   <item><see cref="SWEF.SocialHub.SocialActivityFeed"/> — activity feed posts.</item>
    ///   <item><see cref="SWEF.Analytics.TelemetryDispatcher"/> — telemetry events.</item>
    ///   <item><see cref="SWEF.Security.InputSanitizer"/> — content validation.</item>
    /// </list>
    ///
    /// <para>All integration calls are guarded by compile-time symbols so the class
    /// compiles cleanly even when dependent modules are absent.</para>
    /// </summary>
    public static class MarketplaceBridge
    {
        // ── Achievement keys ──────────────────────────────────────────────────

        private const string AchievFirstListing      = "first_listing";
        private const string AchievFirstPurchase     = "first_purchase";
        private const string AchievTopCreator        = "top_creator";        // 100 downloads
        private const string AchievMarketplaceMogul  = "marketplace_mogul";  // 50 sales
        private const string AchievFiveStarCreator   = "five_star_creator";
        private const string AchievContentCollector  = "content_collector";  // buy 25 items

        // ── Currency deduction ────────────────────────────────────────────────

        /// <summary>
        /// Attempts to deduct <paramref name="amount"/> from the local player's balance.
        /// </summary>
        /// <param name="amount">Amount to deduct.</param>
        /// <param name="source">Transaction label.</param>
        /// <returns><c>true</c> if the deduction succeeded.</returns>
        public static bool TryDeductCurrency(int amount, string source)
        {
#if SWEF_PROGRESSION_AVAILABLE
            var prog = SWEF.Progression.ProgressionManager.Instance;
            if (prog == null) return true; // Graceful fallback: allow purchase

            if (prog.GetCurrency() < amount)
                return false;

            prog.AddCurrency(-amount, source);
            return true;
#else
            return true; // No progression system — allow all purchases
#endif
        }

        // ── Content validation ────────────────────────────────────────────────

        /// <summary>
        /// Validates raw content-data JSON using the Security system.
        /// </summary>
        /// <param name="contentData">JSON payload to validate.</param>
        /// <returns><c>true</c> if validation passes.</returns>
        public static bool ValidateContentData(string contentData)
        {
#if SWEF_SECURITY_AVAILABLE
            var result = SWEF.Security.InputSanitizer.ValidateGenericInput(contentData);
            return result.isValid;
#else
            return true;
#endif
        }

        // ── Listing published ─────────────────────────────────────────────────

        /// <summary>Called by <see cref="MarketplaceManager"/> when a listing is published.</summary>
        public static void OnListingPublished(MarketplaceListingData listing)
        {
            if (listing == null) return;

            ReportAchievement(AchievFirstListing, 1);

            PostActivity("marketplace_listing_published",
                $"Published a new {listing.category} listing: \"{listing.title}\"",
                listing.listingId);
        }

        // ── Listing purchased ─────────────────────────────────────────────────

        /// <summary>Called by <see cref="MarketplaceManager"/> when a purchase completes.</summary>
        public static void OnListingPurchased(MarketplaceListingData listing,
            MarketplaceTransactionData transaction)
        {
            if (listing == null || transaction == null) return;

            // Award XP for the purchase
#if SWEF_PROGRESSION_AVAILABLE
            SWEF.Progression.ProgressionManager.Instance?.AddXP(25, "marketplace_purchase");
#endif

            // Achievement progress
            ReportAchievement(AchievFirstPurchase, 1);
            ReportAchievement(AchievContentCollector, 1);

            // Creator earnings
            CreatorDashboardController dashCtrl =
                Object.FindFirstObjectByType<CreatorDashboardController>();
            dashCtrl?.RecordEarning(transaction);

            // Sales-count achievement for the creator
            ReportAchievement(AchievMarketplaceMogul, 1);

            // Import purchased content
            ImportContent(listing);

            PostActivity("marketplace_purchase",
                $"Purchased \"{listing.title}\" from the marketplace.",
                listing.listingId);
        }

        // ── Listing downloaded (free) ─────────────────────────────────────────

        /// <summary>Called by <see cref="MarketplaceManager"/> when a free listing is downloaded.</summary>
        public static void OnListingDownloaded(MarketplaceListingData listing)
        {
            if (listing == null) return;

            ReportAchievement(AchievFirstPurchase, 1);
            ReportAchievement(AchievContentCollector, 1);

            ImportContent(listing);

            PostActivity("marketplace_download",
                $"Downloaded free content: \"{listing.title}\".",
                listing.listingId);
        }

        // ── Review submitted ──────────────────────────────────────────────────

        /// <summary>Called by <see cref="ReviewManager"/> when a review is submitted.</summary>
        public static void OnReviewSubmitted(MarketplaceReviewData review)
        {
            if (review == null) return;

            if (review.rating >= 5)
                ReportAchievement(AchievFiveStarCreator, 1);

            // Notify creator
            MarketplaceManager.Instance?.GetListingById(review.listingId);

            PostActivity("marketplace_review",
                $"Left a {review.rating}-star review on a marketplace listing.",
                review.listingId);
        }

        // ── Creator followed ──────────────────────────────────────────────────

        /// <summary>Called by <see cref="CreatorDashboardController"/> when a creator is followed.</summary>
        public static void OnCreatorFollowed(string creatorId)
        {
            PostActivity("marketplace_creator_followed",
                $"Followed marketplace creator {creatorId}.");
        }

        // ── Creator download milestone ────────────────────────────────────────

        /// <summary>
        /// Should be called when a creator's cumulative download count crosses 100.
        /// Reports the <c>top_creator</c> achievement.
        /// </summary>
        public static void OnCreatorReachedDownloadMilestone()
        {
            ReportAchievement(AchievTopCreator, 1);
        }

        // ── Content import ────────────────────────────────────────────────────

        /// <summary>
        /// Unpacks the content payload of a listing and applies it to the appropriate SWEF system.
        /// Delegates to <see cref="ContentPackager.UnpackContent"/>.
        /// </summary>
        public static void ImportContent(MarketplaceListingData listing)
        {
            ContentPackager.UnpackContent(listing);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static void ReportAchievement(string key, int amount)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.ReportProgress(key, amount);
#endif
        }

        private static void PostActivity(string activityType, string detail,
            string relatedId = "")
        {
#if SWEF_SOCIAL_AVAILABLE
            SWEF.SocialHub.SocialActivityFeed.Instance?.PostActivity(activityType, detail, relatedId);
#endif
        }
    }
}
