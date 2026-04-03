// MarketplaceSearchQuery.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>Sort options for marketplace search results.</summary>
    public enum MarketplaceSortBy
    {
        /// <summary>Most recently published listings first.</summary>
        Newest,

        /// <summary>Most downloaded listings first.</summary>
        Popular,

        /// <summary>Listings with the highest average rating first.</summary>
        HighestRated,

        /// <summary>Listings with the most total downloads first.</summary>
        MostDownloaded,

        /// <summary>Cheapest listings first.</summary>
        PriceAsc,

        /// <summary>Most expensive listings first.</summary>
        PriceDesc,
    }

    /// <summary>
    /// Serializable search/filter query submitted to <see cref="MarketplaceSearchController"/>.
    /// </summary>
    [Serializable]
    public class MarketplaceSearchQuery
    {
        // ── Text ──────────────────────────────────────────────────────────────

        /// <summary>Free-text search string matched against title, description and tags.</summary>
        [Tooltip("Free-text search string.")]
        public string searchText = string.Empty;

        // ── Filters ───────────────────────────────────────────────────────────

        /// <summary>Restrict results to a specific category. <c>null</c> means all categories.</summary>
        [Tooltip("Category filter (null = all).")]
        public MarketplaceCategory? category;

        /// <summary>Sort order for results.</summary>
        [Tooltip("Sort order for results.")]
        public MarketplaceSortBy sortBy = MarketplaceSortBy.Newest;

        /// <summary>Minimum acceptable average rating [1–5]. 0 means no minimum.</summary>
        [Tooltip("Minimum average rating filter [1–5]. 0 = no minimum.")]
        public float minRating;

        /// <summary>Maximum price filter. 0 means no maximum.</summary>
        [Tooltip("Maximum price filter. 0 = no maximum.")]
        public int maxPrice;

        /// <summary>When <c>true</c>, only free listings are returned.</summary>
        [Tooltip("Only include free listings.")]
        public bool freeOnly;

        /// <summary>When <c>true</c>, only listings verified by moderation are returned.</summary>
        [Tooltip("Only include verified listings.")]
        public bool verifiedOnly;

        /// <summary>Restrict results to a specific creator by player ID. Empty string means all creators.</summary>
        [Tooltip("Creator ID filter. Empty = all creators.")]
        public string creatorId = string.Empty;

        /// <summary>Tags that must be present on returned listings. Empty list means no tag filter.</summary>
        [Tooltip("Tag filter (all listed tags must match).")]
        public List<string> tags = new List<string>();

        // ── Pagination ────────────────────────────────────────────────────────

        /// <summary>Zero-based page index.</summary>
        [Tooltip("Zero-based page index.")]
        public int page;

        /// <summary>Number of listings per page.</summary>
        [Tooltip("Number of listings per page.")]
        public int pageSize = 20;
    }
}
