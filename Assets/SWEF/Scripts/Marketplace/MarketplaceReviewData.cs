// MarketplaceReviewData.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>
    /// Serializable record representing a player review for a marketplace listing.
    /// Persisted inside <c>marketplace_reviews.json</c> by <see cref="ReviewManager"/>.
    /// </summary>
    [Serializable]
    public class MarketplaceReviewData
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Unique review identifier (GUID string).</summary>
        [Tooltip("Unique review identifier (GUID string).")]
        public string reviewId = Guid.NewGuid().ToString();

        /// <summary>Listing this review refers to.</summary>
        [Tooltip("Listing this review refers to.")]
        public string listingId = string.Empty;

        /// <summary>Player ID of the reviewer.</summary>
        [Tooltip("Player ID of the reviewer.")]
        public string reviewerId = string.Empty;

        /// <summary>Display name of the reviewer.</summary>
        [Tooltip("Display name of the reviewer.")]
        public string reviewerName = string.Empty;

        // ── Content ───────────────────────────────────────────────────────────

        /// <summary>Star rating in the range [1, 5].</summary>
        [Tooltip("Star rating [1–5].")]
        public int rating = 5;

        /// <summary>Optional text comment.</summary>
        [Tooltip("Optional text comment.")]
        public string comment = string.Empty;

        // ── Timestamps ────────────────────────────────────────────────────────

        /// <summary>UTC creation timestamp (ISO-8601).</summary>
        [Tooltip("UTC creation timestamp (ISO-8601).")]
        public string createdAt = DateTime.UtcNow.ToString("o");

        /// <summary>Whether this review has been edited after submission.</summary>
        [Tooltip("Whether this review has been edited after submission.")]
        public bool isEdited;

        // ── Community ─────────────────────────────────────────────────────────

        /// <summary>Number of other players who marked this review as helpful.</summary>
        [Tooltip("Number of helpful votes.")]
        public int helpfulCount;
    }
}
