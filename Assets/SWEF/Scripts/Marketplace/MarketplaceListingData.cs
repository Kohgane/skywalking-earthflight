// MarketplaceListingData.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>
    /// Serializable record representing a single item listed in the Community Content Marketplace.
    /// Persisted inside <c>marketplace_listings.json</c> by <see cref="MarketplaceManager"/>.
    /// </summary>
    [Serializable]
    public class MarketplaceListingData
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Unique listing identifier (GUID string).</summary>
        [Tooltip("Unique listing identifier (GUID string).")]
        public string listingId = Guid.NewGuid().ToString();

        /// <summary>Player/creator ID of the seller.</summary>
        [Tooltip("Player/creator ID of the seller.")]
        public string sellerId = string.Empty;

        /// <summary>Display name of the seller.</summary>
        [Tooltip("Display name of the seller.")]
        public string sellerName = string.Empty;

        // ── Metadata ──────────────────────────────────────────────────────────

        /// <summary>Short title displayed in browse/search results.</summary>
        [Tooltip("Short title displayed in browse/search results.")]
        public string title = string.Empty;

        /// <summary>Full description of the listing.</summary>
        [Tooltip("Full description of the listing.")]
        public string description = string.Empty;

        /// <summary>Content category of this listing.</summary>
        [Tooltip("Content category of this listing.")]
        public MarketplaceCategory category = MarketplaceCategory.AircraftBuild;

        // ── Pricing ───────────────────────────────────────────────────────────

        /// <summary>In-game currency price. Ignored when <see cref="isFree"/> is <c>true</c>.</summary>
        [Tooltip("In-game currency price (ignored when isFree = true).")]
        public int price;

        /// <summary>Whether this listing is available at no cost.</summary>
        [Tooltip("Whether this listing is available at no cost.")]
        public bool isFree;

        // ── Discovery ─────────────────────────────────────────────────────────

        /// <summary>Searchable tags attached to this listing.</summary>
        [Tooltip("Searchable tags attached to this listing.")]
        public List<string> tags = new List<string>();

        /// <summary>Path to the listing thumbnail image resource.</summary>
        [Tooltip("Path to the listing thumbnail image resource.")]
        public string thumbnailPath = string.Empty;

        // ── Content ───────────────────────────────────────────────────────────

        /// <summary>JSON-serialized content payload (aircraft build, livery data, etc.).</summary>
        [Tooltip("JSON-serialized content payload.")]
        public string contentData = string.Empty;

        // ── Timestamps ────────────────────────────────────────────────────────

        /// <summary>UTC creation timestamp (ISO-8601).</summary>
        [Tooltip("UTC creation timestamp (ISO-8601).")]
        public string createdAt = DateTime.UtcNow.ToString("o");

        /// <summary>UTC last-updated timestamp (ISO-8601).</summary>
        [Tooltip("UTC last-updated timestamp (ISO-8601).")]
        public string updatedAt = DateTime.UtcNow.ToString("o");

        // ── Stats ─────────────────────────────────────────────────────────────

        /// <summary>Total number of times this listing has been downloaded.</summary>
        [Tooltip("Total download count.")]
        public int downloadCount;

        /// <summary>Average player rating in the range [1, 5].</summary>
        [Tooltip("Average player rating [1–5].")]
        public float ratingAverage;

        /// <summary>Number of ratings submitted.</summary>
        [Tooltip("Number of ratings submitted.")]
        public int ratingCount;

        // ── Flags ─────────────────────────────────────────────────────────────

        /// <summary>Whether this listing has been verified by moderation.</summary>
        [Tooltip("Whether this listing has been verified by moderation.")]
        public bool isVerified;

        /// <summary>Whether this listing is currently published and visible.</summary>
        [Tooltip("Whether this listing is currently published and visible.")]
        public bool isPublished;

        /// <summary>Version string of the content (e.g. "1.0.2").</summary>
        [Tooltip("Content version string.")]
        public string version = "1.0.0";
    }
}
