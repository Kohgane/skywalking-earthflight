// MarketplaceSearchController.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>
    /// MonoBehaviour that provides high-level search, trending, featured, and
    /// recommendation surfaces on top of <see cref="MarketplaceManager"/>.
    /// Maintains a local search-history ring buffer and per-category suggestion lists.
    /// </summary>
    public class MarketplaceSearchController : MonoBehaviour
    {
        #region Inspector

        [Header("Search Settings")]
        [Tooltip("Maximum number of recent searches to remember.")]
        [SerializeField] private int _maxSearchHistory = 20;

        [Tooltip("Maximum number of suggestions to return.")]
        [SerializeField] private int _maxSuggestions = 8;

        [Tooltip("Number of trending listings to surface.")]
        [SerializeField] private int _trendingCount = 10;

        [Tooltip("Number of featured listings to surface.")]
        [SerializeField] private int _featuredCount = 6;

        [Tooltip("Number of recommended listings to surface.")]
        [SerializeField] private int _recommendedCount = 8;

        #endregion

        #region Private State

        private readonly List<string> _searchHistory = new List<string>();

        #endregion

        #region Public API — Search

        /// <summary>
        /// Executes a search against <see cref="MarketplaceManager"/> and records the query text
        /// in the local history.
        /// </summary>
        /// <param name="query">Search and filter parameters.</param>
        /// <returns>Matching, paged listings.</returns>
        public List<MarketplaceListingData> Search(MarketplaceSearchQuery query)
        {
            if (query == null)
            {
                Debug.LogWarning("[SWEF] Marketplace: Search called with null query.");
                return new List<MarketplaceListingData>();
            }

            // Record non-empty search text in history
            if (!string.IsNullOrWhiteSpace(query.searchText))
                RecordSearchHistory(query.searchText.Trim());

            var results = MarketplaceManager.Instance?.GetListings(query)
                          ?? new List<MarketplaceListingData>();

            MarketplaceAnalytics.RecordSearchPerformed(
                query.searchText,
                query.category?.ToString() ?? "all",
                results.Count);

            return results;
        }

        #endregion

        #region Public API — Curated Lists

        /// <summary>
        /// Returns the most downloaded published listings.
        /// </summary>
        public List<MarketplaceListingData> GetTrending()
        {
            return GetAllPublished()
                .OrderByDescending(l => l.downloadCount)
                .Take(_trendingCount)
                .ToList();
        }

        /// <summary>
        /// Returns the highest-rated published listings (minimum 3 ratings required).
        /// </summary>
        public List<MarketplaceListingData> GetFeatured()
        {
            return GetAllPublished()
                .Where(l => l.ratingCount >= 3)
                .OrderByDescending(l => l.ratingAverage)
                .Take(_featuredCount)
                .ToList();
        }

        /// <summary>
        /// Returns recommended listings for the local player.
        /// Simple heuristic: verified listings the player has not yet purchased,
        /// ordered by rating then download count.
        /// </summary>
        public List<MarketplaceListingData> GetRecommended()
        {
            var library = MarketplaceManager.Instance?.GetMyLibrary() ?? new List<MarketplaceTransactionData>();
            var ownedIds = new HashSet<string>(library.Select(t => t.listingId));

            return GetAllPublished()
                .Where(l => !ownedIds.Contains(l.listingId))
                .OrderByDescending(l => l.ratingAverage)
                .ThenByDescending(l => l.downloadCount)
                .Take(_recommendedCount)
                .ToList();
        }

        #endregion

        #region Public API — History & Suggestions

        /// <summary>Returns the recent search-text history (most recent first).</summary>
        public IReadOnlyList<string> GetSearchHistory() => _searchHistory;

        /// <summary>
        /// Returns tag/title suggestions matching the given prefix from history and trending tags.
        /// </summary>
        /// <param name="prefix">Text prefix to match against.</param>
        public List<string> GetSuggestions(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return new List<string>();
            var candidates = _searchHistory
                .Concat(MarketplaceBrowseData.TrendingTags)
                .Where(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .Take(_maxSuggestions)
                .ToList();

            return candidates;
        }

        /// <summary>Clears the search-history ring buffer.</summary>
        public void ClearSearchHistory() => _searchHistory.Clear();

        #endregion

        #region Private Helpers

        private List<MarketplaceListingData> GetAllPublished()
        {
            var query = new MarketplaceSearchQuery { pageSize = int.MaxValue };
            return MarketplaceManager.Instance?.GetListings(query) ?? new List<MarketplaceListingData>();
        }

        private void RecordSearchHistory(string text)
        {
            _searchHistory.Remove(text); // Remove duplicate before re-inserting at front
            _searchHistory.Insert(0, text);
            if (_searchHistory.Count > _maxSearchHistory)
                _searchHistory.RemoveAt(_searchHistory.Count - 1);
        }

        #endregion
    }
}
