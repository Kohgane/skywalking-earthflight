using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.UI;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Full-screen music library browser.
    /// <para>
    /// Tabs: All Tracks, Playlists, Favourites, Genres, Moods, Recently Played.
    /// Supports real-time search, sort options, grid/list view toggle, playlist
    /// creation/editing, and track detail panel.
    /// Uses scroll-view virtualisation for large libraries.
    /// Integrates with <see cref="AccessibilityController"/> and
    /// <see cref="SWEF.Localization.LocalizationManager"/>.
    /// </para>
    /// </summary>
    public class MusicLibraryUI : MonoBehaviour
    {
        // ── Tabs ──────────────────────────────────────────────────────────────────
        private enum LibraryTab { AllTracks, Playlists, Favourites, Genres, Moods, RecentlyPlayed }

        // ── Sort options ──────────────────────────────────────────────────────────
        /// <summary>Available sort criteria for the track list.</summary>
        public enum SortOption { Title, Artist, Duration, DateAdded, Energy, MostPlayed }

        // ── Inspector — Panels ────────────────────────────────────────────────────
        [Header("Panels")]
        [Tooltip("Root canvas group for the library screen.")]
        [SerializeField] private CanvasGroup libraryRoot;

        [Tooltip("Scroll view content parent where track rows are parented.")]
        [SerializeField] private RectTransform scrollContent;

        [Tooltip("Prefab for a single track row in list view.")]
        [SerializeField] private GameObject trackRowPrefab;

        [Tooltip("Prefab for a single track card in grid view.")]
        [SerializeField] private GameObject trackCardPrefab;

        // ── Inspector — Tabs ──────────────────────────────────────────────────────
        [Header("Tab Buttons")]
        [SerializeField] private Button tabAllTracksButton;
        [SerializeField] private Button tabPlaylistsButton;
        [SerializeField] private Button tabFavouritesButton;
        [SerializeField] private Button tabGenresButton;
        [SerializeField] private Button tabMoodsButton;
        [SerializeField] private Button tabRecentButton;

        // ── Inspector — Search & Sort ─────────────────────────────────────────────
        [Header("Search & Sort")]
        [Tooltip("Search input field.")]
        [SerializeField] private InputField searchInput;

        [Tooltip("Dropdown for sort option selection.")]
        [SerializeField] private Dropdown sortDropdown;

        [Tooltip("Toggle between grid and list view.")]
        [SerializeField] private Toggle gridViewToggle;

        // ── Inspector — Track Detail Panel ────────────────────────────────────────
        [Header("Track Detail Panel")]
        [Tooltip("Detail panel root.")]
        [SerializeField] private GameObject detailPanel;

        [SerializeField] private Text detailTitleLabel;
        [SerializeField] private Text detailArtistLabel;
        [SerializeField] private Text detailAlbumLabel;
        [SerializeField] private Text detailDurationLabel;
        [SerializeField] private Text detailGenreLabel;
        [SerializeField] private Text detailMoodLabel;
        [SerializeField] private Text detailBpmLabel;
        [SerializeField] private Text detailEnergyLabel;
        [SerializeField] private Button detailPlayButton;
        [SerializeField] private Button detailFavButton;
        [SerializeField] private Button detailCloseButton;

        // ── Inspector — Playlist Creation ─────────────────────────────────────────
        [Header("Playlist Creation")]
        [SerializeField] private GameObject createPlaylistPanel;
        [SerializeField] private InputField playlistNameInput;
        [SerializeField] private Button     createPlaylistConfirmButton;
        [SerializeField] private Button     createPlaylistCancelButton;
        [SerializeField] private Button     openCreatePlaylistButton;

        // ── Private state ─────────────────────────────────────────────────────────
        private LibraryTab       _activeTab      = LibraryTab.AllTracks;
        private SortOption       _sortOption     = SortOption.Title;
        private bool             _isGridView     = false;
        private string           _searchQuery    = string.Empty;
        private List<MusicTrack> _filteredTracks = new List<MusicTrack>();
        private List<string>     _recentlyPlayed = new List<string>();
        private MusicTrack       _selectedTrack;

        private readonly List<string>     _pendingPlaylistTracks = new List<string>();
        private          AccessibilityController _accessibilityController;

        // Virtualised row pool
        private readonly List<GameObject> _rowPool = new List<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _accessibilityController = FindFirstObjectByType<AccessibilityController>();
        }

        private void Start()
        {
            // Tab buttons
            tabAllTracksButton ?.onClick.AddListener(() => SwitchTab(LibraryTab.AllTracks));
            tabPlaylistsButton ?.onClick.AddListener(() => SwitchTab(LibraryTab.Playlists));
            tabFavouritesButton?.onClick.AddListener(() => SwitchTab(LibraryTab.Favourites));
            tabGenresButton    ?.onClick.AddListener(() => SwitchTab(LibraryTab.Genres));
            tabMoodsButton     ?.onClick.AddListener(() => SwitchTab(LibraryTab.Moods));
            tabRecentButton    ?.onClick.AddListener(() => SwitchTab(LibraryTab.RecentlyPlayed));

            // Search
            if (searchInput != null)
                searchInput.onValueChanged.AddListener(OnSearchChanged);

            // Sort
            if (sortDropdown != null)
            {
                PopulateSortDropdown();
                sortDropdown.onValueChanged.AddListener(OnSortChanged);
            }

            // Grid/List toggle
            if (gridViewToggle != null)
                gridViewToggle.onValueChanged.AddListener(OnViewToggleChanged);

            // Detail panel
            if (detailPlayButton  != null) detailPlayButton.onClick.AddListener(OnDetailPlayClicked);
            if (detailFavButton   != null) detailFavButton.onClick.AddListener(OnDetailFavClicked);
            if (detailCloseButton != null) detailCloseButton.onClick.AddListener(CloseDetailPanel);

            // Playlist creation
            if (openCreatePlaylistButton     != null) openCreatePlaylistButton.onClick.AddListener(OpenCreatePlaylist);
            if (createPlaylistConfirmButton  != null) createPlaylistConfirmButton.onClick.AddListener(ConfirmCreatePlaylist);
            if (createPlaylistCancelButton   != null) createPlaylistCancelButton.onClick.AddListener(CancelCreatePlaylist);

            // Subscribe to track-changed event to track recently played
            if (MusicPlayerManager.Instance != null)
                MusicPlayerManager.Instance.OnTrackChanged += OnTrackChangedInManager;

            if (detailPanel          != null) detailPanel.SetActive(false);
            if (createPlaylistPanel  != null) createPlaylistPanel.SetActive(false);

            Refresh();
        }

        private void OnDestroy()
        {
            if (MusicPlayerManager.Instance != null)
                MusicPlayerManager.Instance.OnTrackChanged -= OnTrackChangedInManager;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Refreshes the displayed list based on the active tab, search query, and sort option.</summary>
        public void Refresh()
        {
            _filteredTracks = GatherTracks();
            _filteredTracks = FilterBySearch(_filteredTracks, _searchQuery);
            _filteredTracks = Sort(_filteredTracks, _sortOption);
            RebuildScrollView();
        }

        /// <summary>Opens the library panel.</summary>
        public void Open()
        {
            if (libraryRoot != null)
            {
                libraryRoot.gameObject.SetActive(true);
                libraryRoot.alpha          = 1f;
                libraryRoot.interactable   = true;
                libraryRoot.blocksRaycasts = true;
            }
            Refresh();
        }

        /// <summary>Closes the library panel.</summary>
        public void Close()
        {
            if (libraryRoot != null)
            {
                libraryRoot.gameObject.SetActive(false);
                libraryRoot.interactable   = false;
                libraryRoot.blocksRaycasts = false;
            }
        }

        // ── Tab switching ─────────────────────────────────────────────────────────

        private void SwitchTab(LibraryTab tab)
        {
            _activeTab = tab;
            Refresh();
        }

        // ── Data gathering ────────────────────────────────────────────────────────

        private List<MusicTrack> GatherTracks()
        {
            if (MusicPlayerManager.Instance == null) return new List<MusicTrack>();

            switch (_activeTab)
            {
                case LibraryTab.AllTracks:
                    return new List<MusicTrack>(MusicPlayerManager.Instance.GetAllTracks());

                case LibraryTab.Favourites:
                    return MusicPlayerManager.Instance.GetFavorites();

                case LibraryTab.RecentlyPlayed:
                {
                    var recent = new List<MusicTrack>();
                    foreach (string id in _recentlyPlayed)
                    {
                        MusicTrack t = MusicPlayerManager.Instance.GetCurrentTrack();
                        // Look up each recent track ID
                        if (t != null && t.trackId == id)
                        {
                            recent.Add(t);
                        }
                        else
                        {
                            // Iterate to find it
                            foreach (MusicTrack track in MusicPlayerManager.Instance.GetAllTracks())
                                if (track.trackId == id) { recent.Add(track); break; }
                        }
                    }
                    return recent;
                }

                default:
                    return new List<MusicTrack>(MusicPlayerManager.Instance.GetAllTracks());
            }
        }

        private static List<MusicTrack> FilterBySearch(List<MusicTrack> tracks, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return tracks;
            string lower  = query.ToLowerInvariant();
            var    result = new List<MusicTrack>();
            foreach (MusicTrack t in tracks)
            {
                bool match = (t.title  != null && t.title.ToLowerInvariant().Contains(lower))
                          || (t.artist != null && t.artist.ToLowerInvariant().Contains(lower))
                          || (t.album  != null && t.album.ToLowerInvariant().Contains(lower));
                if (match) result.Add(t);
            }
            return result;
        }

        private static List<MusicTrack> Sort(List<MusicTrack> tracks, SortOption option)
        {
            var sorted = new List<MusicTrack>(tracks);
            switch (option)
            {
                case SortOption.Title:
                    sorted.Sort((a, b) => string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase));
                    break;
                case SortOption.Artist:
                    sorted.Sort((a, b) => string.Compare(a.artist, b.artist, StringComparison.OrdinalIgnoreCase));
                    break;
                case SortOption.Duration:
                    sorted.Sort((a, b) => a.durationSeconds.CompareTo(b.durationSeconds));
                    break;
                case SortOption.Energy:
                    sorted.Sort((a, b) => b.energy.CompareTo(a.energy)); // highest first
                    break;
                default:
                    break;
            }
            return sorted;
        }

        // ── Scroll-view virtualisation ────────────────────────────────────────────

        private void RebuildScrollView()
        {
            if (scrollContent == null) return;

            // Return existing rows to pool
            foreach (GameObject row in _rowPool)
                row.SetActive(false);

            // We reuse or create rows for each track
            for (int i = 0; i < _filteredTracks.Count; i++)
            {
                MusicTrack track = _filteredTracks[i];
                GameObject row   = GetOrCreateRow(i);
                PopulateRow(row, track);
                row.SetActive(true);
            }

            // Hide leftover pool entries
            for (int i = _filteredTracks.Count; i < _rowPool.Count; i++)
                _rowPool[i].SetActive(false);
        }

        private GameObject GetOrCreateRow(int index)
        {
            if (index < _rowPool.Count)
                return _rowPool[index];

            GameObject prefab = (_isGridView && trackCardPrefab != null)
                ? trackCardPrefab
                : trackRowPrefab;

            if (prefab == null)
            {
                // Fallback: create a minimal row with a button
                var go    = new GameObject($"TrackRow_{index}", typeof(RectTransform), typeof(Button));
                go.transform.SetParent(scrollContent, false);
                _rowPool.Add(go);
                return go;
            }

            GameObject inst = Instantiate(prefab, scrollContent);
            _rowPool.Add(inst);
            return inst;
        }

        private void PopulateRow(GameObject row, MusicTrack track)
        {
            // Populate text labels if the prefab exposes them
            Text[] labels = row.GetComponentsInChildren<Text>();
            if (labels.Length > 0) labels[0].text = track.title  ?? string.Empty;
            if (labels.Length > 1) labels[1].text = track.artist ?? string.Empty;
            if (labels.Length > 2) labels[2].text = track.FormattedDuration();

            // Tap to show detail
            Button btn = row.GetComponentInChildren<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                MusicTrack captured = track;
                btn.onClick.AddListener(() => ShowDetailPanel(captured));
            }
        }

        // ── Detail panel ──────────────────────────────────────────────────────────

        private void ShowDetailPanel(MusicTrack track)
        {
            _selectedTrack = track;
            if (detailPanel == null) return;
            detailPanel.SetActive(true);

            if (detailTitleLabel    != null) detailTitleLabel.text    = track.title ?? string.Empty;
            if (detailArtistLabel   != null) detailArtistLabel.text   = track.artist ?? string.Empty;
            if (detailAlbumLabel    != null) detailAlbumLabel.text    = track.album ?? string.Empty;
            if (detailDurationLabel != null) detailDurationLabel.text = track.FormattedDuration();
            if (detailGenreLabel    != null) detailGenreLabel.text    = track.genre ?? string.Empty;
            if (detailBpmLabel      != null) detailBpmLabel.text      = $"{track.bpm:F0} BPM";
            if (detailEnergyLabel   != null) detailEnergyLabel.text   = $"{track.energy:P0}";

            if (detailMoodLabel != null)
            {
                string moods = track.moodTags != null ? string.Join(", ", track.moodTags) : string.Empty;
                detailMoodLabel.text = moods;
            }

            // Update fav button colour
            if (detailFavButton != null)
            {
                var img = detailFavButton.GetComponent<Image>();
                if (img != null)
                    img.color = track.isFavorite ? Color.yellow : Color.white;
            }
        }

        private void CloseDetailPanel()
        {
            if (detailPanel != null) detailPanel.SetActive(false);
        }

        private void OnDetailPlayClicked()
        {
            if (_selectedTrack == null || MusicPlayerManager.Instance == null) return;
            MusicPlayerManager.Instance.PlayTrack(_selectedTrack.trackId);
            CloseDetailPanel();
        }

        private void OnDetailFavClicked()
        {
            if (_selectedTrack == null || MusicPlayerManager.Instance == null) return;
            MusicPlayerManager.Instance.ToggleFavorite(_selectedTrack.trackId);
            ShowDetailPanel(_selectedTrack); // refresh fav state
        }

        // ── Playlist creation ─────────────────────────────────────────────────────

        private void OpenCreatePlaylist()
        {
            if (createPlaylistPanel != null) createPlaylistPanel.SetActive(true);
            _pendingPlaylistTracks.Clear();
        }

        private void ConfirmCreatePlaylist()
        {
            if (MusicPlayerManager.Instance == null) return;
            string name = playlistNameInput != null ? playlistNameInput.text : "New Playlist";
            if (string.IsNullOrWhiteSpace(name)) name = "New Playlist";
            MusicPlayerManager.Instance.CreatePlaylist(name, new List<string>(_pendingPlaylistTracks));
            if (createPlaylistPanel != null) createPlaylistPanel.SetActive(false);
            SwitchTab(LibraryTab.Playlists);
        }

        private void CancelCreatePlaylist()
        {
            if (createPlaylistPanel != null) createPlaylistPanel.SetActive(false);
        }

        // ── Search / Sort / View callbacks ────────────────────────────────────────

        private void OnSearchChanged(string query)
        {
            _searchQuery = query;
            Refresh();
        }

        private void OnSortChanged(int index)
        {
            if (index >= 0 && index < Enum.GetValues(typeof(SortOption)).Length)
                _sortOption = (SortOption)index;
            Refresh();
        }

        private void OnViewToggleChanged(bool isGrid)
        {
            _isGridView = isGrid;
            // Invalidate the row pool so RebuildScrollView recreates rows with the correct prefab.
            // Rows are hidden/reused, not destroyed, to preserve GC performance.
            foreach (GameObject row in _rowPool)
                if (row != null) row.SetActive(false);
            _rowPool.Clear();
            Refresh();
        }

        private void PopulateSortDropdown()
        {
            if (sortDropdown == null) return;
            sortDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            foreach (SortOption o in Enum.GetValues(typeof(SortOption)))
                options.Add(Localize($"music.sort.{o.ToString().ToLower()}"));
            sortDropdown.AddOptions(options);
        }

        // ── Recently played tracking ──────────────────────────────────────────────

        private void OnTrackChangedInManager(MusicTrack track)
        {
            if (track == null) return;
            _recentlyPlayed.Remove(track.trackId);
            _recentlyPlayed.Insert(0, track.trackId);
            if (_recentlyPlayed.Count > 50) _recentlyPlayed.RemoveAt(_recentlyPlayed.Count - 1);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private string Localize(string key)
        {
            return SWEF.Localization.LocalizationManager.Instance != null
                ? SWEF.Localization.LocalizationManager.Instance.GetText(key)
                : key;
        }
    }
}
