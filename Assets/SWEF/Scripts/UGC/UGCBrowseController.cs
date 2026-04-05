// UGCBrowseController.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — Controller for browsing, searching, and filtering community UGC content.
    ///
    /// <para>Operates on the local <see cref="UGCPublishManager.InstalledContent"/> list
    /// (in a production build this would also query a remote server).  Raises events
    /// whenever the result set changes so UI components can refresh.</para>
    /// </summary>
    public sealed class UGCBrowseController : MonoBehaviour
    {
        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when the search/filter results are updated. Argument is the new results list.</summary>
        public event Action<IReadOnlyList<UGCContent>> OnResultsUpdated;

        /// <summary>Raised when the user selects a content item for detail view. Argument is the selected content.</summary>
        public event Action<UGCContent> OnContentSelected;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>Current search query string.</summary>
        public string SearchQuery { get; private set; } = string.Empty;

        /// <summary>Active content type filter, or <c>null</c> for all types.</summary>
        public UGCContentType? TypeFilter { get; private set; }

        /// <summary>Active difficulty filter, or <c>null</c> for all difficulties.</summary>
        public UGCDifficulty? DifficultyFilter { get; private set; }

        /// <summary>Active category filter, or <c>null</c> for all categories.</summary>
        public UGCCategory? CategoryFilter { get; private set; }

        /// <summary>Minimum star rating filter (inclusive).</summary>
        public float MinRatingFilter { get; private set; } = 0f;

        /// <summary>Current sort mode.</summary>
        public BrowseSortMode SortMode { get; private set; } = BrowseSortMode.Newest;

        /// <summary>Current page index (zero-based).</summary>
        public int CurrentPage { get; private set; } = 0;

        /// <summary>Read-only view of the current page results.</summary>
        public IReadOnlyList<UGCContent> Results => _currentResults;

        // ── Internal state ─────────────────────────────────────────────────────

        private readonly List<UGCContent> _currentResults = new List<UGCContent>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            RefreshResults();
        }

        // ── Public API — search & filter ───────────────────────────────────────

        /// <summary>
        /// Sets the search query and refreshes results.
        /// </summary>
        public void SetSearchQuery(string query)
        {
            if (query != null && query.Length > UGCConfig.MaxSearchQueryLength)
                query = query.Substring(0, UGCConfig.MaxSearchQueryLength);
            SearchQuery = query ?? string.Empty;
            CurrentPage = 0;
            RefreshResults();
        }

        /// <summary>Sets the content-type filter. Pass <c>null</c> to clear.</summary>
        public void SetTypeFilter(UGCContentType? type)
        {
            TypeFilter  = type;
            CurrentPage = 0;
            RefreshResults();
        }

        /// <summary>Sets the difficulty filter. Pass <c>null</c> to clear.</summary>
        public void SetDifficultyFilter(UGCDifficulty? difficulty)
        {
            DifficultyFilter = difficulty;
            CurrentPage      = 0;
            RefreshResults();
        }

        /// <summary>Sets the category filter. Pass <c>null</c> to clear.</summary>
        public void SetCategoryFilter(UGCCategory? category)
        {
            CategoryFilter = category;
            CurrentPage    = 0;
            RefreshResults();
        }

        /// <summary>Sets the minimum rating filter (0–5).</summary>
        public void SetMinRatingFilter(float minRating)
        {
            MinRatingFilter = Mathf.Clamp(minRating, 0f, 5f);
            CurrentPage     = 0;
            RefreshResults();
        }

        /// <summary>Sets the sort mode and refreshes results.</summary>
        public void SetSortMode(BrowseSortMode mode)
        {
            SortMode    = mode;
            CurrentPage = 0;
            RefreshResults();
        }

        /// <summary>Clears all active filters and the search query.</summary>
        public void ClearFilters()
        {
            SearchQuery      = string.Empty;
            TypeFilter       = null;
            DifficultyFilter = null;
            CategoryFilter   = null;
            MinRatingFilter  = 0f;
            CurrentPage      = 0;
            RefreshResults();
        }

        /// <summary>Advances to the next page of results.</summary>
        public void NextPage()
        {
            CurrentPage++;
            RefreshResults();
        }

        /// <summary>Returns to the previous page of results.</summary>
        public void PrevPage()
        {
            if (CurrentPage <= 0) return;
            CurrentPage--;
            RefreshResults();
        }

        /// <summary>Raises <see cref="OnContentSelected"/> for the given content item.</summary>
        public void SelectContent(UGCContent content)
        {
            OnContentSelected?.Invoke(content);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void RefreshResults()
        {
            _currentResults.Clear();

            // Gather source — installed library + published items
            var source = new List<UGCContent>();
            if (UGCPublishManager.Instance != null)
            {
                source.AddRange(UGCPublishManager.Instance.InstalledContent);
                foreach (var c in UGCPublishManager.Instance.PublishedContent)
                    if (!source.Exists(x => x.contentId == c.contentId) &&
                        (c.status == UGCStatus.Published || c.status == UGCStatus.Featured))
                        source.Add(c);
            }

            // Apply filters
            foreach (var item in source)
            {
                if (item.status != UGCStatus.Published && item.status != UGCStatus.Featured) continue;
                if (TypeFilter.HasValue       && item.contentType != TypeFilter.Value)    continue;
                if (DifficultyFilter.HasValue  && item.difficulty  != DifficultyFilter.Value) continue;
                if (CategoryFilter.HasValue    && item.category    != CategoryFilter.Value)   continue;
                if (item.rating < MinRatingFilter) continue;
                if (!string.IsNullOrEmpty(SearchQuery) && !MatchesQuery(item, SearchQuery)) continue;

                _currentResults.Add(item);
            }

            // Sort
            SortResults();

            // Paginate
            int start = CurrentPage * UGCConfig.BrowsePageSize;
            if (start >= _currentResults.Count && _currentResults.Count > 0)
            {
                CurrentPage = (_currentResults.Count - 1) / UGCConfig.BrowsePageSize;
                start       = CurrentPage * UGCConfig.BrowsePageSize;
            }

            if (start > 0 && start < _currentResults.Count)
                _currentResults.RemoveRange(0, start);

            if (_currentResults.Count > UGCConfig.BrowsePageSize)
                _currentResults.RemoveRange(UGCConfig.BrowsePageSize, _currentResults.Count - UGCConfig.BrowsePageSize);

            OnResultsUpdated?.Invoke(_currentResults);
        }

        private static bool MatchesQuery(UGCContent item, string query)
        {
            string q = query.ToLowerInvariant();
            if (item.title.ToLowerInvariant().Contains(q)) return true;
            if (item.authorName.ToLowerInvariant().Contains(q)) return true;
            foreach (var tag in item.tags)
                if (tag.ToLowerInvariant().Contains(q)) return true;
            return false;
        }

        private void SortResults()
        {
            switch (SortMode)
            {
                case BrowseSortMode.Newest:
                    _currentResults.Sort((a, b) => string.Compare(b.createdAt, a.createdAt, StringComparison.Ordinal));
                    break;
                case BrowseSortMode.Popular:
                    _currentResults.Sort((a, b) => b.downloadCount.CompareTo(a.downloadCount));
                    break;
                case BrowseSortMode.HighestRated:
                    _currentResults.Sort((a, b) => b.rating.CompareTo(a.rating));
                    break;
                case BrowseSortMode.MostDownloaded:
                    _currentResults.Sort((a, b) => b.downloadCount.CompareTo(a.downloadCount));
                    break;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Sort mode enum
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sort order for the UGC community browse results.
    /// </summary>
    public enum BrowseSortMode
    {
        /// <summary>Most recently created content first.</summary>
        Newest,
        /// <summary>Most downloaded / played content first.</summary>
        Popular,
        /// <summary>Highest average star rating first.</summary>
        HighestRated,
        /// <summary>Highest absolute download count first.</summary>
        MostDownloaded,
    }
}
