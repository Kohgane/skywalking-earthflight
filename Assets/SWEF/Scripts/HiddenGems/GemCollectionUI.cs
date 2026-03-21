using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SWEF.Localization;
using SWEF.GuidedTour;
using SWEF.Favorites;
using SWEF.Social;

namespace SWEF.HiddenGems
{
    /// <summary>
    /// Full-screen collection gallery for discovered and undiscovered hidden gems.
    /// Provides tabs (All / By Continent / By Category / By Rarity / Favorites),
    /// a grid of gem cards, a detail view, and integration with <see cref="WaypointNavigator"/>.
    /// </summary>
    public class GemCollectionUI : MonoBehaviour
    {
        // ── Enums ─────────────────────────────────────────────────────────────────
        public enum CollectionTab   { All, ByContinent, ByCategory, ByRarity, Favorites }
        public enum SortMode        { Name, DateDiscovered, Rarity, Distance, Continent }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Tabs")]
        [SerializeField] private Button tabAllButton;
        [SerializeField] private Button tabContinentButton;
        [SerializeField] private Button tabCategoryButton;
        [SerializeField] private Button tabRarityButton;
        [SerializeField] private Button tabFavoritesButton;

        [Header("Grid")]
        [SerializeField] private Transform            cardContainer;
        [SerializeField] private GameObject           gemCardPrefab;
        [SerializeField] private TMP_InputField       searchInput;
        [SerializeField] private TMP_Dropdown         sortDropdown;

        [Header("Progress")]
        [SerializeField] private TextMeshProUGUI overallProgressText;
        [SerializeField] private Slider          overallProgressSlider;

        [Header("Detail View")]
        [SerializeField] private GameObject          detailPanel;
        [SerializeField] private TextMeshProUGUI     detailNameText;
        [SerializeField] private TextMeshProUGUI     detailDescriptionText;
        [SerializeField] private TextMeshProUGUI     detailFactText;
        [SerializeField] private TextMeshProUGUI     detailStatsText;
        [SerializeField] private TextMeshProUGUI     detailRarityText;
        [SerializeField] private Button              detailNavigateButton;
        [SerializeField] private Button              detailFavoriteButton;
        [SerializeField] private Button              detailShareButton;
        [SerializeField] private Button              detailAddToTourButton;
        [SerializeField] private Button              detailCloseButton;

        [Header("Close")]
        [SerializeField] private Button closeButton;

        // ── State ─────────────────────────────────────────────────────────────────
        private CollectionTab              _activeTab       = CollectionTab.All;
        private SortMode                   _sortMode        = SortMode.Rarity;
        private string                     _searchQuery     = "";
        private HiddenGemDefinition        _selectedGem;
        private readonly List<GameObject>  _spawnedCards    = new List<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            BindButtons();
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void BindButtons()
        {
            if (tabAllButton       != null) tabAllButton.onClick.AddListener(()       => SetTab(CollectionTab.All));
            if (tabContinentButton != null) tabContinentButton.onClick.AddListener(() => SetTab(CollectionTab.ByContinent));
            if (tabCategoryButton  != null) tabCategoryButton.onClick.AddListener(()  => SetTab(CollectionTab.ByCategory));
            if (tabRarityButton    != null) tabRarityButton.onClick.AddListener(()    => SetTab(CollectionTab.ByRarity));
            if (tabFavoritesButton != null) tabFavoritesButton.onClick.AddListener(() => SetTab(CollectionTab.Favorites));
            if (closeButton        != null) closeButton.onClick.AddListener(Hide);

            if (searchInput  != null) searchInput.onValueChanged.AddListener(OnSearch);
            if (sortDropdown != null) sortDropdown.onValueChanged.AddListener(OnSort);

            if (detailNavigateButton  != null) detailNavigateButton.onClick.AddListener(OnDetailNavigate);
            if (detailFavoriteButton  != null) detailFavoriteButton.onClick.AddListener(OnDetailFavorite);
            if (detailShareButton     != null) detailShareButton.onClick.AddListener(OnDetailShare);
            if (detailAddToTourButton != null) detailAddToTourButton.onClick.AddListener(OnDetailAddToTour);
            if (detailCloseButton     != null) detailCloseButton.onClick.AddListener(HideDetail);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Opens the collection panel and refreshes all cards.</summary>
        public void Show()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            RefreshAll();
        }

        /// <summary>Hides the collection panel.</summary>
        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            HideDetail();
        }

        // ── Tab management ────────────────────────────────────────────────────────

        private void SetTab(CollectionTab tab)
        {
            _activeTab = tab;
            RefreshAll();
        }

        // ── Refresh ───────────────────────────────────────────────────────────────

        private void RefreshAll()
        {
            RefreshProgress();
            RefreshCards();
        }

        private void RefreshProgress()
        {
            if (HiddenGemManager.Instance == null) return;
            var (disc, total) = HiddenGemManager.Instance.GetDiscoveryProgress();
            var lm = LocalizationManager.Instance;
            if (overallProgressText != null)
                overallProgressText.text = lm != null
                    ? string.Format(lm.GetText("gem_total_progress"), disc, total)
                    : $"{disc}/{total} Hidden Gems Discovered";
            if (overallProgressSlider != null && total > 0)
                overallProgressSlider.value = (float)disc / total;
        }

        private void RefreshCards()
        {
            // Clear existing cards
            foreach (var c in _spawnedCards) Destroy(c);
            _spawnedCards.Clear();

            if (HiddenGemManager.Instance == null || gemCardPrefab == null || cardContainer == null)
                return;

            var gems = FilterAndSort(HiddenGemManager.Instance.GetAllGems());
            foreach (var gem in gems)
                SpawnCard(gem);
        }

        private List<HiddenGemDefinition> FilterAndSort(List<HiddenGemDefinition> source)
        {
            var mgr = HiddenGemManager.Instance;

            // Tab filter
            IEnumerable<HiddenGemDefinition> filtered = source;
            if (_activeTab == CollectionTab.Favorites)
                filtered = source.Where(g => mgr.GetGemState(g.gemId)?.isFavorited == true);

            // Search
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                string q = _searchQuery.ToLowerInvariant();
                filtered = filtered.Where(g =>
                    g.gemId.ToLowerInvariant().Contains(q) ||
                    g.country.ToLowerInvariant().Contains(q) ||
                    g.category.ToString().ToLowerInvariant().Contains(q) ||
                    g.continent.ToString().ToLowerInvariant().Contains(q));
            }

            // Sort
            return _sortMode switch
            {
                SortMode.Name          => filtered.OrderBy(g => g.nameKey).ToList(),
                SortMode.DateDiscovered => filtered.OrderByDescending(g => mgr.GetGemState(g.gemId)?.discoveredDate ?? "").ToList(),
                SortMode.Rarity        => filtered.OrderByDescending(g => (int)g.rarity).ToList(),
                SortMode.Continent     => filtered.OrderBy(g => g.continent).ToList(),
                _                      => filtered.ToList()
            };
        }

        private void SpawnCard(HiddenGemDefinition gem)
        {
            var go   = Instantiate(gemCardPrefab, cardContainer);
            var mgr  = HiddenGemManager.Instance;
            bool discovered = mgr.IsGemDiscovered(gem.gemId);

            // Try to populate common fields via well-known component names
            var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
            var lm    = LocalizationManager.Instance;

            foreach (var t in texts)
            {
                if (t.gameObject.name == "GemNameText")
                    t.text = discovered
                        ? (lm != null ? lm.GetText(gem.nameKey) : gem.nameKey)
                        : "???";
                else if (t.gameObject.name == "GemRarityText")
                    t.text = lm != null ? lm.GetText($"gem_rarity_{gem.rarity.ToString().ToLowerInvariant()}") : gem.rarity.ToString();
                else if (t.gameObject.name == "GemCountryText")
                    t.text = discovered ? gem.country : gem.continent.ToString();
            }

            // Rarity border
            var border = go.transform.Find("RarityBorder")?.GetComponent<Image>();
            if (border != null && ColorUtility.TryParseHtmlString(HiddenGemDefinition.RarityColor(gem.rarity), out Color c))
                border.color = discovered ? c : new Color(0.3f, 0.3f, 0.3f);

            // Click → detail
            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            btn.onClick.AddListener(() => ShowDetail(gem));

            _spawnedCards.Add(go);
        }

        // ── Detail view ───────────────────────────────────────────────────────────

        private void ShowDetail(HiddenGemDefinition gem)
        {
            _selectedGem = gem;
            if (detailPanel != null) detailPanel.SetActive(true);

            var mgr   = HiddenGemManager.Instance;
            var lm    = LocalizationManager.Instance;
            var state = mgr?.GetGemState(gem.gemId);
            bool disc = state?.isDiscovered == true;

            if (detailNameText != null)
                detailNameText.text = disc && lm != null ? lm.GetText(gem.nameKey) : "???";

            if (detailDescriptionText != null)
                detailDescriptionText.text = disc && lm != null ? lm.GetText(gem.descriptionKey) : "";

            if (detailFactText != null)
                detailFactText.text = disc && lm != null ? lm.GetText(gem.factKey) : "";

            if (detailRarityText != null && ColorUtility.TryParseHtmlString(HiddenGemDefinition.RarityColor(gem.rarity), out Color rc))
            {
                string rarityLabel = lm != null ? lm.GetText($"gem_rarity_{gem.rarity.ToString().ToLowerInvariant()}") : gem.rarity.ToString();
                detailRarityText.text  = rarityLabel;
                detailRarityText.color = rc;
            }

            if (detailStatsText != null && state != null && disc)
            {
                detailStatsText.text =
                    $"Discovered: {state.discoveredDate}\n" +
                    $"Altitude: {state.discoveryAltitude:F0} m\n" +
                    $"Speed: {state.discoverySpeed:F1} m/s\n" +
                    $"Visits: {state.timesVisited}";
            }

            // Favorite button label
            if (detailFavoriteButton != null)
            {
                var label = detailFavoriteButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = state?.isFavorited == true ? "★ Favorited" : "☆ Favorite";
            }
        }

        private void HideDetail()
        {
            if (detailPanel != null) detailPanel.SetActive(false);
            _selectedGem = null;
        }

        // ── Detail button handlers ────────────────────────────────────────────────

        private void OnDetailNavigate()
        {
            if (_selectedGem == null) return;
            var nav  = FindFirstObjectByType<WaypointNavigator>();
            var dest = HiddenGemManager.GetWorldPosition(_selectedGem);
            nav?.SetManualTarget(dest);
            Hide();
        }

        private void OnDetailFavorite()
        {
            if (_selectedGem == null) return;
            HiddenGemManager.Instance?.ToggleFavorite(_selectedGem.gemId);
            ShowDetail(_selectedGem); // refresh
        }

        private void OnDetailShare()
        {
            if (_selectedGem == null) return;
            var lm   = LocalizationManager.Instance;
            string n = lm != null ? lm.GetText(_selectedGem.nameKey) : _selectedGem.nameKey;
            var sm   = FindFirstObjectByType<ShareManager>();
            sm?.ShareText($"I discovered {n} in Skywalking: Earth Flight! #SWEF #HiddenGems");
        }

        private void OnDetailAddToTour()
        {
            if (_selectedGem == null) return;
            var tour = GemTourGenerator.GenerateCustomTour(new System.Collections.Generic.List<string> { _selectedGem.gemId });
            TourManager.Instance?.StartTour(tour);
            Hide();
        }

        // ── Search / Sort ─────────────────────────────────────────────────────────

        private void OnSearch(string query)
        {
            _searchQuery = query;
            RefreshCards();
        }

        private void OnSort(int index)
        {
            _sortMode = (SortMode)index;
            RefreshCards();
        }
    }
}
