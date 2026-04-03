// MarketplaceAnalytics.cs — SWEF Community Content Marketplace (Phase 94)
using System.Collections.Generic;

namespace SWEF.Marketplace
{
    /// <summary>
    /// Static helper that wraps all Marketplace telemetry events and forwards them
    /// to <c>SWEF.Analytics.TelemetryDispatcher.EnqueueEvent</c>.
    ///
    /// <para>All calls are guarded by <c>#if SWEF_ANALYTICS_AVAILABLE</c> so the
    /// class compiles cleanly even when the Analytics module is absent.</para>
    ///
    /// <para>Events dispatched:</para>
    /// <list type="bullet">
    ///   <item><c>listing_published</c></item>
    ///   <item><c>listing_purchased</c></item>
    ///   <item><c>listing_downloaded</c></item>
    ///   <item><c>listing_removed</c></item>
    ///   <item><c>review_submitted</c></item>
    ///   <item><c>creator_followed</c></item>
    ///   <item><c>search_performed</c></item>
    ///   <item><c>content_reported</c></item>
    ///   <item><c>earnings_withdrawn</c></item>
    /// </list>
    /// </summary>
    public static class MarketplaceAnalytics
    {
        /// <summary>Records that a new listing was published.</summary>
        /// <param name="listingId">Published listing ID.</param>
        /// <param name="category">Category name.</param>
        /// <param name="isFree">Whether the listing is free.</param>
        public static void RecordListingPublished(string listingId, string category, bool isFree)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("listing_published",
                new Dictionary<string, object>
                {
                    { "listing_id", listingId },
                    { "category",   category  },
                    { "is_free",    isFree    },
                });
#endif
        }

        /// <summary>Records that a listing was purchased.</summary>
        /// <param name="listingId">Purchased listing ID.</param>
        /// <param name="price">Currency amount paid.</param>
        public static void RecordListingPurchased(string listingId, int price)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("listing_purchased",
                new Dictionary<string, object>
                {
                    { "listing_id", listingId },
                    { "price",      price     },
                });
#endif
        }

        /// <summary>Records that a free listing was downloaded.</summary>
        /// <param name="listingId">Downloaded listing ID.</param>
        public static void RecordListingDownloaded(string listingId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("listing_downloaded",
                new Dictionary<string, object>
                {
                    { "listing_id", listingId },
                });
#endif
        }

        /// <summary>Records that a listing was unpublished or removed.</summary>
        /// <param name="listingId">Removed listing ID.</param>
        public static void RecordListingRemoved(string listingId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("listing_removed",
                new Dictionary<string, object>
                {
                    { "listing_id", listingId },
                });
#endif
        }

        /// <summary>Records that a review was submitted.</summary>
        /// <param name="listingId">Reviewed listing ID.</param>
        /// <param name="rating">Star rating [1–5].</param>
        public static void RecordReviewSubmitted(string listingId, int rating)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("review_submitted",
                new Dictionary<string, object>
                {
                    { "listing_id", listingId },
                    { "rating",     rating    },
                });
#endif
        }

        /// <summary>Records that the local player followed a creator.</summary>
        /// <param name="creatorId">Followed creator ID.</param>
        public static void RecordCreatorFollowed(string creatorId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("creator_followed",
                new Dictionary<string, object>
                {
                    { "creator_id", creatorId },
                });
#endif
        }

        /// <summary>Records that a search was performed.</summary>
        /// <param name="searchText">Query text (may be empty).</param>
        /// <param name="category">Category filter applied (or "all").</param>
        /// <param name="resultCount">Number of results returned.</param>
        public static void RecordSearchPerformed(string searchText, string category, int resultCount)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("search_performed",
                new Dictionary<string, object>
                {
                    { "search_text",   searchText   },
                    { "category",      category     },
                    { "result_count",  resultCount  },
                });
#endif
        }

        /// <summary>Records that a listing or creator was reported.</summary>
        /// <param name="targetId">Reported listing or creator ID.</param>
        /// <param name="reason">Reason string.</param>
        public static void RecordContentReported(string targetId, string reason)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("content_reported",
                new Dictionary<string, object>
                {
                    { "target_id", targetId },
                    { "reason",    reason   },
                });
#endif
        }

        /// <summary>Records that a creator withdrew their earnings.</summary>
        /// <param name="creatorId">Creator who withdrew.</param>
        /// <param name="amount">Amount withdrawn.</param>
        public static void RecordEarningsWithdrawn(string creatorId, int amount)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("earnings_withdrawn",
                new Dictionary<string, object>
                {
                    { "creator_id", creatorId },
                    { "amount",     amount    },
                });
#endif
        }
    }
}
