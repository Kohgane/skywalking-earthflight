using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.GuidedTour
{
    /// <summary>
    /// Scrollable catalog UI that lists all available tours and lets the player
    /// filter by difficulty, completion status, or region, and search by name.
    /// Triggers <see cref="TourManager.StartTour"/> when the player selects an entry.
    /// </summary>
    public class TourCatalogUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private TourManager      tourManager;
        [SerializeField] private TourProgressTracker progressTracker;

        [Header("Catalog Content")]
        [SerializeField] private TourData[]       allTours;
        [SerializeField] private Transform        contentRoot;
        [SerializeField] private GameObject       tourEntryPrefab;

        [Header("Filters")]
        [SerializeField] private Dropdown difficultyFilterDropdown;
        [SerializeField] private Dropdown statusFilterDropdown;
        [SerializeField] private Dropdown regionFilterDropdown;
        [SerializeField] private InputField searchInputField;

        [Header("Panel")]
        [SerializeField] private GameObject catalogPanel;
        [SerializeField] private Button     closeButton;

        // ── Filter options ────────────────────────────────────────────────────────
        private enum StatusFilter { All, Available, Completed }
        private string _searchQuery  = string.Empty;
        private int    _diffFilter   = 0; // 0 = All
        private int    _statusFilter = 0; // index into StatusFilter
        private string _regionFilter = string.Empty;

        // ── Runtime entry list ────────────────────────────────────────────────────
        private readonly List<GameObject> _entryInstances = new List<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (tourManager     == null) tourManager      = FindFirstObjectByType<TourManager>();
            if (progressTracker == null) progressTracker  = FindFirstObjectByType<TourProgressTracker>();

            if (searchInputField        != null) searchInputField.onValueChanged.AddListener(OnSearchChanged);
            if (difficultyFilterDropdown != null) difficultyFilterDropdown.onValueChanged.AddListener(OnDiffFilterChanged);
            if (statusFilterDropdown    != null) statusFilterDropdown.onValueChanged.AddListener(OnStatusFilterChanged);
            if (regionFilterDropdown    != null) regionFilterDropdown.onValueChanged.AddListener(OnRegionFilterChanged);
            if (closeButton             != null) closeButton.onClick.AddListener(HideCatalog);
        }

        private void OnEnable()
        {
            RefreshEntries();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the catalog panel.</summary>
        public void ShowCatalog()
        {
            if (catalogPanel != null) catalogPanel.SetActive(true);
            RefreshEntries();
        }

        /// <summary>Hides the catalog panel.</summary>
        public void HideCatalog()
        {
            if (catalogPanel != null) catalogPanel.SetActive(false);
        }

        /// <summary>Refreshes the tour list using the current filter settings.</summary>
        public void RefreshEntries()
        {
            ClearEntries();

            if (allTours == null || contentRoot == null || tourEntryPrefab == null) return;

            foreach (var tour in allTours)
            {
                if (!MatchesFilters(tour)) continue;

                var go    = Instantiate(tourEntryPrefab, contentRoot);
                PopulateEntry(go, tour);
                _entryInstances.Add(go);
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private bool MatchesFilters(TourData tour)
        {
            // Search bar.
            if (!string.IsNullOrEmpty(_searchQuery)
                && !tour.tourName.ToLowerInvariant().Contains(_searchQuery.ToLowerInvariant()))
                return false;

            // Difficulty filter (index 0 = All, 1 = Easy, 2 = Medium, 3 = Hard).
            if (_diffFilter != 0 && (int)tour.difficulty != _diffFilter - 1)
                return false;

            // Region filter.
            if (!string.IsNullOrEmpty(_regionFilter)
                && !tour.region.Equals(_regionFilter, System.StringComparison.OrdinalIgnoreCase))
                return false;

            // Status filter.
            if (_statusFilter != 0)
            {
                bool isCompleted = progressTracker != null
                    && progressTracker.GetTourProgress(tour.tourId).starsEarned > 0;
                if (_statusFilter == (int)StatusFilter.Completed && !isCompleted) return false;
                if (_statusFilter == (int)StatusFilter.Available  && isCompleted) return false;
            }

            return true;
        }

        private void PopulateEntry(GameObject go, TourData tour)
        {
            // Name label.
            var nameLbl = go.transform.Find("Name")?.GetComponent<Text>();
            if (nameLbl != null) nameLbl.text = tour.tourName;

            // Description label.
            var descLbl = go.transform.Find("Description")?.GetComponent<Text>();
            if (descLbl != null) descLbl.text = tour.description;

            // Difficulty badge.
            var diffLbl = go.transform.Find("Difficulty")?.GetComponent<Text>();
            if (diffLbl != null)
            {
                diffLbl.text = tour.difficulty.ToString();
                diffLbl.color = tour.difficulty switch
                {
                    TourDifficulty.Easy   => Color.green,
                    TourDifficulty.Medium => Color.yellow,
                    TourDifficulty.Hard   => Color.red,
                    _                     => Color.white,
                };
            }

            // Duration label.
            var durLbl = go.transform.Find("Duration")?.GetComponent<Text>();
            if (durLbl != null)
                durLbl.text = $"{tour.estimatedDurationMinutes:F0} min";

            // Completion status.
            TourProgressTracker.TourResult progress = progressTracker != null
                ? progressTracker.GetTourProgress(tour.tourId)
                : default;
            var statusLbl = go.transform.Find("Status")?.GetComponent<Text>();
            if (statusLbl != null)
            {
                statusLbl.text = progress.starsEarned > 0
                    ? $"★ {progress.starsEarned}"
                    : "Available";
            }

            // Start button.
            var startBtn = go.GetComponentInChildren<Button>();
            if (startBtn != null)
            {
                // Capture for closure.
                var capturedTour = tour;
                startBtn.onClick.AddListener(() =>
                {
                    tourManager?.StartTour(capturedTour);
                    HideCatalog();
                });
            }
        }

        private void ClearEntries()
        {
            foreach (var go in _entryInstances)
                if (go != null) Destroy(go);
            _entryInstances.Clear();
        }

        // ── Filter callbacks ──────────────────────────────────────────────────────
        private void OnSearchChanged(string query)
        {
            _searchQuery = query;
            RefreshEntries();
        }

        private void OnDiffFilterChanged(int index)
        {
            _diffFilter = index;
            RefreshEntries();
        }

        private void OnStatusFilterChanged(int index)
        {
            _statusFilter = index;
            RefreshEntries();
        }

        private void OnRegionFilterChanged(int index)
        {
            // Assumes dropdown options mirror available regions; index 0 = "All".
            if (regionFilterDropdown != null && index > 0 && index < regionFilterDropdown.options.Count)
                _regionFilter = regionFilterDropdown.options[index].text;
            else
                _regionFilter = string.Empty;
            RefreshEntries();
        }
    }
}
