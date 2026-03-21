using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SWEF.Localization;

namespace SWEF.Journal
{
    /// <summary>
    /// Full-screen journal browser panel.
    /// Displays a scrollable list of <see cref="FlightLogEntry"/> cards
    /// with filter bar, sort dropdown, and free-text search.
    /// </summary>
    public class JournalPanelUI : MonoBehaviour
    {
        // ── Inspector — Layout ────────────────────────────────────────────────────
        [Header("Layout")]
        [Tooltip("Root panel to show/hide.")]
        [SerializeField] private GameObject panelRoot;

        [Tooltip("ScrollRect containing the entry card list.")]
        [SerializeField] private ScrollRect scrollRect;

        [Tooltip("Container transform inside the ScrollRect's content area.")]
        [SerializeField] private Transform listContainer;

        [Tooltip("Prefab for a single flight-entry card.")]
        [SerializeField] private GameObject entryCardPrefab;

        [Tooltip("Message shown when no entries match the current filter.")]
        [SerializeField] private GameObject emptyStateObject;

        // ── Inspector — Filter bar ────────────────────────────────────────────────
        [Header("Filter Bar")]
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Dropdown   sortDropdown;
        [SerializeField] private Toggle         favoritesToggle;
        [SerializeField] private TMP_InputField dateFromInput;
        [SerializeField] private TMP_InputField dateToInput;
        [SerializeField] private TMP_InputField weatherFilterInput;
        [SerializeField] private TMP_InputField tourFilterInput;

        // ── Inspector — Buttons ───────────────────────────────────────────────────
        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button refreshButton;

        // ── Inspector — Sub-panels ────────────────────────────────────────────────
        [Header("Sub-panels")]
        [SerializeField] private JournalDetailUI     detailUI;
        [SerializeField] private JournalStatisticsUI statisticsUI;

        // ── State ─────────────────────────────────────────────────────────────────
        private JournalFilter _filter = new JournalFilter();
        private List<FlightLogEntry> _displayedEntries = new List<FlightLogEntry>();
        private readonly List<GameObject> _cardPool = new List<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (closeButton   != null) closeButton.onClick.AddListener(Hide);
            if (statsButton   != null) statsButton.onClick.AddListener(OpenStats);
            if (refreshButton != null) refreshButton.onClick.AddListener(Refresh);

            if (searchInput      != null) searchInput.onValueChanged.AddListener(_ => OnFilterChanged());
            if (sortDropdown     != null) sortDropdown.onValueChanged.AddListener(_ => OnFilterChanged());
            if (favoritesToggle  != null) favoritesToggle.onValueChanged.AddListener(_ => OnFilterChanged());
            if (dateFromInput    != null) dateFromInput.onValueChanged.AddListener(_ => OnFilterChanged());
            if (dateToInput      != null) dateToInput.onValueChanged.AddListener(_ => OnFilterChanged());
            if (weatherFilterInput != null) weatherFilterInput.onValueChanged.AddListener(_ => OnFilterChanged());
            if (tourFilterInput  != null) tourFilterInput.onValueChanged.AddListener(_ => OnFilterChanged());

            if (sortDropdown != null)
                PopulateSortDropdown();

            if (JournalManager.Instance != null)
            {
                JournalManager.Instance.OnNewEntryAdded  += _ => Refresh();
                JournalManager.Instance.OnEntryUpdated   += _ => Refresh();
                JournalManager.Instance.OnEntryDeleted   += _ => Refresh();
            }
        }

        private void OnEnable()
        {
            Refresh();
            ApplyLocalization();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the journal panel and refreshes the entry list.</summary>
        public void Show()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            Refresh();
        }

        /// <summary>Hides the journal panel.</summary>
        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>Re-fetches data from <see cref="JournalManager"/> and rebuilds the list.</summary>
        public void Refresh()
        {
            if (JournalManager.Instance == null) return;
            BuildFilter();
            _displayedEntries = JournalManager.Instance.GetFilteredEntries(_filter);
            RebuildList();
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private void OnFilterChanged() => Refresh();

        private void BuildFilter()
        {
            _filter.searchQuery    = searchInput          != null ? searchInput.text          : string.Empty;
            _filter.dateFrom       = dateFromInput        != null ? dateFromInput.text         : string.Empty;
            _filter.dateTo         = dateToInput          != null ? dateToInput.text           : string.Empty;
            _filter.weatherFilter  = weatherFilterInput   != null ? weatherFilterInput.text    : string.Empty;
            _filter.tourFilter     = tourFilterInput      != null ? tourFilterInput.text       : string.Empty;
            _filter.favoritesOnly  = favoritesToggle      != null && favoritesToggle.isOn;

            if (sortDropdown != null)
                _filter.sortBy = (JournalSortBy)sortDropdown.value;
            _filter.sortDescending = true;
        }

        private void RebuildList()
        {
            // Return pooled cards.
            foreach (var card in _cardPool)
                card.SetActive(false);

            if (_displayedEntries == null || _displayedEntries.Count == 0)
            {
                if (emptyStateObject != null) emptyStateObject.SetActive(true);
                return;
            }

            if (emptyStateObject != null) emptyStateObject.SetActive(false);

            for (int i = 0; i < _displayedEntries.Count; i++)
            {
                GameObject card = GetOrCreateCard(i);
                BindCard(card, _displayedEntries[i]);
            }
        }

        private GameObject GetOrCreateCard(int index)
        {
            if (index < _cardPool.Count)
            {
                _cardPool[index].SetActive(true);
                return _cardPool[index];
            }

            GameObject newCard = entryCardPrefab != null
                ? Instantiate(entryCardPrefab, listContainer)
                : new GameObject($"EntryCard_{index}", typeof(RectTransform));
            newCard.transform.SetParent(listContainer, false);
            _cardPool.Add(newCard);
            return newCard;
        }

        private void BindCard(GameObject card, FlightLogEntry entry)
        {
            // Bind text fields by name convention (works with or without a dedicated card script).
            BindText(card, "Date",      FormatDate(entry.flightDate));
            BindText(card, "Route",     $"{entry.departureLocation} → {entry.arrivalLocation}");
            BindText(card, "Duration",  FormatDuration(entry.durationSeconds));
            BindText(card, "Distance",  $"{entry.distanceKm:F1} km");
            BindText(card, "MaxAlt",    $"{entry.maxAltitudeM:F0} m");
            BindText(card, "Weather",   entry.weatherCondition);
            BindText(card, "Tour",      entry.tourName);

            // Favourite star icon.
            var star = card.transform.Find("FavoriteStar")?.GetComponent<Image>();
            if (star != null)
                star.color = entry.isFavorite ? Color.yellow : Color.grey;

            // Tap to expand detail.
            var btn = card.GetComponent<Button>();
            if (btn == null) btn = card.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            string id = entry.logId;
            btn.onClick.AddListener(() => OpenDetail(id));
        }

        private void OpenDetail(string logId)
        {
            if (detailUI == null) return;
            var entry = JournalManager.Instance?.GetEntry(logId);
            if (entry == null) return;
            detailUI.Show(entry);
        }

        private void OpenStats()
        {
            if (statisticsUI == null) return;
            statisticsUI.Show();
        }

        private static void BindText(GameObject card, string childName, string value)
        {
            var child = card.transform.Find(childName);
            if (child == null) return;
            var tmp = child.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = value ?? string.Empty;
        }

        private static string FormatDate(string isoDate)
        {
            if (DateTime.TryParse(isoDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
                return dt.ToLocalTime().ToString("MMM dd, yyyy HH:mm");
            return isoDate ?? string.Empty;
        }

        private static string FormatDuration(float seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0
                ? $"{ts.Hours}h {ts.Minutes:D2}m"
                : $"{ts.Minutes}m {ts.Seconds:D2}s";
        }

        private void PopulateSortDropdown()
        {
            sortDropdown.ClearOptions();
            var options = new List<string>
            {
                Localize("journal_sort_date"),
                Localize("journal_sort_duration"),
                Localize("journal_sort_distance"),
                Localize("journal_sort_altitude"),
                Localize("journal_sort_speed"),
                Localize("journal_sort_xp"),
            };
            sortDropdown.AddOptions(options);
        }

        private void ApplyLocalization()
        {
            // Refresh any localisation-dependent text.
            if (sortDropdown != null) PopulateSortDropdown();
        }

        private static string Localize(string key)
        {
            var loc = LocalizationManager.Instance;
            return loc != null ? loc.GetText(key) : key;
        }
    }
}
