// UGCBrowseUI.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — Full-screen community content browser UI.
    ///
    /// <para>Displays a grid or list of community content cards with thumbnails,
    /// a search bar with tag hints, a filter sidebar, content-detail modal,
    /// download progress, and an installed-content management panel.</para>
    ///
    /// <para>All <see cref="SerializeField"/> references are null-safe.</para>
    /// </summary>
    public sealed class UGCBrowseUI : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Root")]
        [SerializeField] private GameObject _panelRoot;

        [Header("Search")]
        [SerializeField] private InputField _searchBar;
        [SerializeField] private Button     _btnSearch;
        [SerializeField] private Button     _btnClearSearch;

        [Header("Filter Sidebar")]
        [SerializeField] private Dropdown   _filterType;
        [SerializeField] private Dropdown   _filterDifficulty;
        [SerializeField] private Dropdown   _filterCategory;
        [SerializeField] private Slider     _filterMinRating;
        [SerializeField] private Dropdown   _sortMode;
        [SerializeField] private Button     _btnClearFilters;

        [Header("Content Grid")]
        [SerializeField] private Transform  _contentGrid;
        [SerializeField] private GameObject _contentCardPrefab;

        [Header("Pagination")]
        [SerializeField] private Button _btnPrevPage;
        [SerializeField] private Button _btnNextPage;
        [SerializeField] private Text   _lblPageInfo;

        [Header("Detail Modal")]
        [SerializeField] private GameObject _detailModal;
        [SerializeField] private Text       _detailTitle;
        [SerializeField] private Text       _detailAuthor;
        [SerializeField] private Text       _detailDescription;
        [SerializeField] private Text       _detailRating;
        [SerializeField] private Text       _detailDownloads;
        [SerializeField] private Button     _btnDownload;
        [SerializeField] private Button     _btnCloseDetail;

        [Header("References")]
        [SerializeField] private UGCBrowseController _controller;

        // ── Internal state ─────────────────────────────────────────────────────

        private UGCContent _selectedContent;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            WireButtons();
            WireControllerEvents();
            if (_detailModal != null) _detailModal.SetActive(false);
        }

        private void OnDestroy()
        {
            UnwireControllerEvents();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Shows or hides the browse panel.</summary>
        public void SetVisible(bool visible)
        {
            if (_panelRoot != null) _panelRoot.SetActive(visible);
            if (visible && _controller != null) _controller.ClearFilters();
        }

        // ── Private setup ──────────────────────────────────────────────────────

        private void WireButtons()
        {
            _btnSearch?.onClick.AddListener(() => _controller?.SetSearchQuery(_searchBar?.text));
            _btnClearSearch?.onClick.AddListener(() =>
            {
                if (_searchBar != null) _searchBar.text = string.Empty;
                _controller?.SetSearchQuery(string.Empty);
            });

            _filterType?.onValueChanged.AddListener(OnTypeFilterChanged);
            _filterDifficulty?.onValueChanged.AddListener(OnDifficultyFilterChanged);
            _filterCategory?.onValueChanged.AddListener(OnCategoryFilterChanged);
            _filterMinRating?.onValueChanged.AddListener(v => _controller?.SetMinRatingFilter(v));
            _sortMode?.onValueChanged.AddListener(OnSortModeChanged);
            _btnClearFilters?.onClick.AddListener(() => _controller?.ClearFilters());

            _btnPrevPage?.onClick.AddListener(() => _controller?.PrevPage());
            _btnNextPage?.onClick.AddListener(() => _controller?.NextPage());

            _btnDownload?.onClick.AddListener(OnDownloadClicked);
            _btnCloseDetail?.onClick.AddListener(() => { if (_detailModal != null) _detailModal.SetActive(false); });
        }

        private void WireControllerEvents()
        {
            if (_controller == null) return;
            _controller.OnResultsUpdated  += OnResultsUpdated;
            _controller.OnContentSelected += OnContentSelected;
        }

        private void UnwireControllerEvents()
        {
            if (_controller == null) return;
            _controller.OnResultsUpdated  -= OnResultsUpdated;
            _controller.OnContentSelected -= OnContentSelected;
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void OnResultsUpdated(IReadOnlyList<UGCContent> results)
        {
            PopulateGrid(results);
            UpdatePageLabel();
        }

        private void OnContentSelected(UGCContent content)
        {
            _selectedContent = content;
            ShowDetailModal(content);
        }

        private void OnTypeFilterChanged(int idx)
        {
            // 0 = "All", 1..N = enum values
            if (idx <= 0) { _controller?.SetTypeFilter(null); return; }
            var values = (UGCContentType[])System.Enum.GetValues(typeof(UGCContentType));
            if (idx - 1 < values.Length) _controller?.SetTypeFilter(values[idx - 1]);
        }

        private void OnDifficultyFilterChanged(int idx)
        {
            if (idx <= 0) { _controller?.SetDifficultyFilter(null); return; }
            var values = (UGCDifficulty[])System.Enum.GetValues(typeof(UGCDifficulty));
            if (idx - 1 < values.Length) _controller?.SetDifficultyFilter(values[idx - 1]);
        }

        private void OnCategoryFilterChanged(int idx)
        {
            if (idx <= 0) { _controller?.SetCategoryFilter(null); return; }
            var values = (UGCCategory[])System.Enum.GetValues(typeof(UGCCategory));
            if (idx - 1 < values.Length) _controller?.SetCategoryFilter(values[idx - 1]);
        }

        private void OnSortModeChanged(int idx)
        {
            var values = (BrowseSortMode[])System.Enum.GetValues(typeof(BrowseSortMode));
            if (idx < values.Length) _controller?.SetSortMode(values[idx]);
        }

        private void OnDownloadClicked()
        {
            if (_selectedContent == null) return;
            UGCPublishManager.Instance?.DownloadAndInstall(_selectedContent);
            if (_detailModal != null) _detailModal.SetActive(false);
        }

        // ── Grid population ────────────────────────────────────────────────────

        private void PopulateGrid(IReadOnlyList<UGCContent> results)
        {
            if (_contentGrid == null || _contentCardPrefab == null) return;

            foreach (Transform child in _contentGrid)
                Destroy(child.gameObject);

            foreach (var item in results)
            {
                var card  = Instantiate(_contentCardPrefab, _contentGrid);
                var label = card.GetComponentInChildren<Text>();
                if (label != null) label.text = $"{item.title}\n★{item.rating:F1} | {item.downloadCount}↓";

                var btn = card.GetComponent<Button>();
                var captured = item;
                btn?.onClick.AddListener(() => _controller?.SelectContent(captured));
            }
        }

        private void ShowDetailModal(UGCContent content)
        {
            if (_detailModal == null || content == null) return;
            _detailModal.SetActive(true);
            if (_detailTitle       != null) _detailTitle.text       = content.title;
            if (_detailAuthor      != null) _detailAuthor.text      = $"By {content.authorName}";
            if (_detailDescription != null) _detailDescription.text = content.description;
            if (_detailRating      != null) _detailRating.text      = $"★ {content.rating:F1}";
            if (_detailDownloads   != null) _detailDownloads.text   = $"{content.downloadCount} downloads";
        }

        private void UpdatePageLabel()
        {
            if (_lblPageInfo == null || _controller == null) return;
            _lblPageInfo.text = $"Page {_controller.CurrentPage + 1}";
        }
    }
}
