using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Localization;

namespace SWEF.Achievement
{
    /// <summary>
    /// Full-screen achievement gallery panel.
    /// Displays all achievements with category filtering and sort options.
    /// </summary>
    public class AchievementPanelUI : MonoBehaviour
    {
        // ── Sort options ──────────────────────────────────────────────────────────
        private enum SortMode { Default, Tier, Progress, UnlockedFirst }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Stats")]
        [SerializeField] private Text totalUnlockedText;
        [SerializeField] private Text totalXPText;
        [SerializeField] private Text completionPctText;

        [Header("Filter / Sort")]
        [SerializeField] private Transform  filterTabContainer;
        [SerializeField] private Button     filterTabPrefab;
        [SerializeField] private Dropdown   sortDropdown;

        [Header("Grid")]
        [SerializeField] private Transform       gridContainer;
        [SerializeField] private AchievementCardUI cardPrefab;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private AchievementCategory? _activeCategory;
        private SortMode             _sortMode = SortMode.Default;
        private readonly List<AchievementCardUI> _cards = new List<AchievementCardUI>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            closeButton?.onClick.AddListener(Close);
            openButton?.onClick.AddListener(Open);

            BuildFilterTabs();
            BuildSortDropdown();

            panelRoot?.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Opens the achievement gallery and refreshes its content.</summary>
        public void Open()
        {
            panelRoot?.SetActive(true);
            Refresh();
        }

        /// <summary>Closes the achievement gallery.</summary>
        public void Close()
        {
            panelRoot?.SetActive(false);
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private void Refresh()
        {
            var mgr = AchievementManager.Instance;
            if (mgr == null) return;

            UpdateStats(mgr);
            RebuildCards(mgr);
        }

        private void UpdateStats(AchievementManager mgr)
        {
            var allStates = mgr.GetAllStates();
            int total    = allStates.Count;
            int unlocked = 0;
            foreach (var s in allStates)
                if (s.unlocked) unlocked++;

            float pct = total > 0 ? (unlocked / (float)total) * 100f : 0f;

            if (totalUnlockedText != null)
                totalUnlockedText.text = $"{unlocked} / {total}";

            if (totalXPText != null)
            {
                var loc = LocalizationManager.Instance;
                string fmt = loc != null ? loc.Get("achievement_total_xp") : "Total XP: {0}";
                totalXPText.text = string.Format(fmt, mgr.GetTotalXP());
            }

            if (completionPctText != null)
            {
                var loc = LocalizationManager.Instance;
                string fmt = loc != null ? loc.Get("achievement_progress") : "{0}% Complete";
                completionPctText.text = string.Format(fmt, Mathf.RoundToInt(pct));
            }
        }

        private void RebuildCards(AchievementManager mgr)
        {
            // Clear existing cards.
            foreach (var card in _cards)
                if (card != null) Destroy(card.gameObject);
            _cards.Clear();

            if (cardPrefab == null || gridContainer == null) return;

            // Gather definitions matching active filter.
            var allStates = mgr.GetAllStates();
            var filtered  = new List<AchievementState>();

            foreach (var state in allStates)
            {
                var def = mgr.GetDefinition(state.achievementId);
                if (def == null) continue;
                if (_activeCategory.HasValue && def.category != _activeCategory.Value) continue;
                filtered.Add(state);
            }

            // Sort.
            filtered.Sort((a, b) =>
            {
                var defA = mgr.GetDefinition(a.achievementId);
                var defB = mgr.GetDefinition(b.achievementId);
                if (defA == null || defB == null) return 0;

                return _sortMode switch
                {
                    SortMode.Tier         => defB.tier.CompareTo(defA.tier),
                    SortMode.Progress     => b.Progress01.CompareTo(a.Progress01),
                    SortMode.UnlockedFirst=> b.unlocked.CompareTo(a.unlocked),
                    _                     => 0
                };
            });

            foreach (var state in filtered)
            {
                var def = mgr.GetDefinition(state.achievementId);
                if (def == null) continue;

                var card = Instantiate(cardPrefab, gridContainer);
                card.Populate(def, state);
                _cards.Add(card);
            }
        }

        private void BuildFilterTabs()
        {
            if (filterTabContainer == null || filterTabPrefab == null) return;

            // "All" tab
            CreateFilterTab("achievement_filter_all", null);

            foreach (AchievementCategory cat in System.Enum.GetValues(typeof(AchievementCategory)))
                CreateFilterTab($"achievement_filter_{cat.ToString().ToLower()}", cat);
        }

        private void CreateFilterTab(string locKey, AchievementCategory? category)
        {
            var btn = Instantiate(filterTabPrefab, filterTabContainer);
            var label = btn.GetComponentInChildren<Text>();
            if (label != null)
            {
                var loc = LocalizationManager.Instance;
                label.text = loc != null ? loc.Get(locKey) : locKey;
            }

            btn.onClick.AddListener(() =>
            {
                _activeCategory = category;
                Refresh();
            });
        }

        private void BuildSortDropdown()
        {
            if (sortDropdown == null) return;

            sortDropdown.ClearOptions();
            sortDropdown.AddOptions(new List<string>
            {
                "Default", "Tier", "Progress", "Unlocked First"
            });

            sortDropdown.onValueChanged.AddListener(idx =>
            {
                _sortMode = (SortMode)idx;
                Refresh();
            });
        }
    }
}
