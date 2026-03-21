using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.Progression
{
    /// <summary>
    /// Full-screen player profile and statistics page.
    /// Displays rank information, lifetime flight stats, skill tree, cosmetics gallery,
    /// and a scrollable XP history log.
    /// Call <see cref="Open"/> / <see cref="Close"/> from a menu button.
    /// </summary>
    public class ProgressionProfileUI : MonoBehaviour
    {
        // ── Inspector — Root ──────────────────────────────────────────────────────
        [Header("Root Panel")]
        [Tooltip("Root GameObject that is shown/hidden when opening or closing the profile.")]
        [SerializeField] private GameObject rootPanel;

        // ── Inspector — Rank card ─────────────────────────────────────────────────
        [Header("Rank Card")]
        [SerializeField] private Image        rankCardBadge;
        [SerializeField] private TextMeshProUGUI rankCardNameText;
        [SerializeField] private TextMeshProUGUI rankCardTierText;
        [SerializeField] private TextMeshProUGUI rankCardLevelText;
        [SerializeField] private Slider       rankCardXPBar;
        [SerializeField] private TextMeshProUGUI rankCardXPLabel;

        // ── Inspector — Flight stats ──────────────────────────────────────────────
        [Header("Flight Stats")]
        [SerializeField] private TextMeshProUGUI statFlightTime;
        [SerializeField] private TextMeshProUGUI statDistance;
        [SerializeField] private TextMeshProUGUI statFlights;
        [SerializeField] private TextMeshProUGUI statTopAltitude;
        [SerializeField] private TextMeshProUGUI statTopSpeed;

        // ── Inspector — Skill tree ────────────────────────────────────────────────
        [Header("Skill Tree")]
        [Tooltip("Prefab for a single skill node button (needs SkillNodeUI component).")]
        [SerializeField] private GameObject skillNodePrefab;
        [Tooltip("Container where skill nodes are instantiated.")]
        [SerializeField] private RectTransform skillTreeContainer;
        [Tooltip("Text showing available skill points.")]
        [SerializeField] private TextMeshProUGUI skillPointsText;

        // ── Inspector — Cosmetics gallery ─────────────────────────────────────────
        [Header("Cosmetics Gallery")]
        [Tooltip("Prefab for a single cosmetic card (needs CosmeticCardUI component).")]
        [SerializeField] private GameObject cosmeticCardPrefab;
        [Tooltip("Container where cosmetic cards are instantiated.")]
        [SerializeField] private RectTransform cosmeticsContainer;
        [Tooltip("Dropdown to filter by cosmetic category.")]
        [SerializeField] private TMP_Dropdown cosmeticCategoryFilter;

        // ── Inspector — XP History ────────────────────────────────────────────────
        [Header("XP History")]
        [Tooltip("Prefab for a single XP history row (needs TextMeshProUGUI children).")]
        [SerializeField] private GameObject xpHistoryRowPrefab;
        [Tooltip("Scroll content container for XP history rows.")]
        [SerializeField] private RectTransform xpHistoryContainer;

        // ── Internal state ────────────────────────────────────────────────────────
        private ProgressionManager    _progression;
        private SkillTreeManager      _skillTree;
        private CosmeticUnlockManager _cosmetics;

        private readonly List<GameObject> _skillNodes      = new List<GameObject>();
        private readonly List<GameObject> _cosmeticCards   = new List<GameObject>();
        private readonly List<GameObject> _xpHistoryRows   = new List<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            _progression = ProgressionManager.Instance    ?? FindFirstObjectByType<ProgressionManager>();
            _skillTree   = SkillTreeManager.Instance      ?? FindFirstObjectByType<SkillTreeManager>();
            _cosmetics   = CosmeticUnlockManager.Instance ?? FindFirstObjectByType<CosmeticUnlockManager>();

            if (rootPanel != null) rootPanel.SetActive(false);

            if (cosmeticCategoryFilter != null)
                cosmeticCategoryFilter.onValueChanged.AddListener(_ => RefreshCosmeticsGallery());
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Opens the profile UI and refreshes all sections.</summary>
        public void Open()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            RefreshAll();
        }

        /// <summary>Closes the profile UI.</summary>
        public void Close()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        /// <summary>Refreshes every section of the profile UI from the current manager state.</summary>
        public void RefreshAll()
        {
            RefreshRankCard();
            RefreshFlightStats();
            RefreshSkillTree();
            RefreshCosmeticsGallery();
            RefreshXPHistory();
        }

        // ── Section refreshes ─────────────────────────────────────────────────────

        private void RefreshRankCard()
        {
            if (_progression == null) return;
            var rank = _progression.GetCurrentRank();
            if (rank == null) return;

            if (rankCardBadge    != null) { rankCardBadge.sprite = rank.rankIcon; rankCardBadge.color = rank.rankColor; }
            if (rankCardNameText != null) rankCardNameText.text  = rank.rankName;
            if (rankCardTierText != null) rankCardTierText.text  = rank.rankTier.ToString();
            if (rankCardLevelText != null) rankCardLevelText.text = $"Level {_progression.CurrentRankLevel}";

            float progress = _progression.GetProgressToNextRank01();
            if (rankCardXPBar   != null) rankCardXPBar.value    = progress;

            var next = _progression.GetNextRank();
            if (rankCardXPLabel != null)
                rankCardXPLabel.text = next != null
                    ? $"{_progression.CurrentXP:N0} / {next.requiredXP:N0} XP"
                    : "MAX RANK";
        }

        private void RefreshFlightStats()
        {
            if (_progression == null) return;

            float hours   = _progression.TotalFlightTimeSeconds / 3600f;
            float minutes = (_progression.TotalFlightTimeSeconds % 3600f) / 60f;

            if (statFlightTime   != null) statFlightTime.text   = $"{(int)hours}h {(int)minutes}m";
            if (statDistance     != null) statDistance.text     = $"{_progression.TotalDistanceKm:N0} km";
            if (statFlights      != null) statFlights.text      = _progression.TotalFlightsCompleted.ToString();
            if (statTopAltitude  != null) statTopAltitude.text  = $"{_progression.TopAltitude:N0} m";
            if (statTopSpeed     != null) statTopSpeed.text     = $"{_progression.TopSpeedMps * 3.6f:N0} km/h";
        }

        private void RefreshSkillTree()
        {
            if (_skillTree == null || skillTreeContainer == null || skillNodePrefab == null) return;

            // Clear existing
            foreach (var go in _skillNodes) if (go != null) Destroy(go);
            _skillNodes.Clear();

            if (skillPointsText != null)
                skillPointsText.text = $"Skill Points: {_skillTree.GetAvailableSkillPoints()}";

            foreach (var skill in _skillTree.GetAllSkills())
            {
                var go  = Instantiate(skillNodePrefab, skillTreeContainer);
                bool unlocked = _skillTree.IsSkillUnlocked(skill.skillId);

                // Try to find a text child and a button
                var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = skill.skillId; // real projects would localize

                var btn = go.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    var capturedId = skill.skillId;
                    btn.interactable = !unlocked;
                    btn.onClick.AddListener(() =>
                    {
                        _skillTree.UnlockSkill(capturedId);
                        RefreshSkillTree();
                    });
                }

                _skillNodes.Add(go);
            }
        }

        private void RefreshCosmeticsGallery()
        {
            if (_cosmetics == null || cosmeticsContainer == null || cosmeticCardPrefab == null) return;

            foreach (var go in _cosmeticCards) if (go != null) Destroy(go);
            _cosmeticCards.Clear();

            int filterIdx = cosmeticCategoryFilter != null ? cosmeticCategoryFilter.value : -1;

            var all = new List<CosmeticUnlockManager.CosmeticItem>();
            all.AddRange(_cosmetics.GetUnlockedCosmetics());
            all.AddRange(_cosmetics.GetLockedCosmetics());

            foreach (var item in all)
            {
                if (filterIdx > 0 && (int)item.category != filterIdx - 1) continue;

                var go  = Instantiate(cosmeticCardPrefab, cosmeticsContainer);
                var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = item.id;

                var btn = go.GetComponentInChildren<Button>();
                if (btn != null && _cosmetics.IsUnlocked(item.id))
                {
                    var capturedItem = item;
                    btn.onClick.AddListener(() =>
                    {
                        _cosmetics.EquipCosmetic(capturedItem.id, capturedItem.category);
                    });
                }
                else if (btn != null)
                {
                    btn.interactable = false;
                }

                _cosmeticCards.Add(go);
            }
        }

        private void RefreshXPHistory()
        {
            if (_progression == null || xpHistoryContainer == null || xpHistoryRowPrefab == null) return;

            foreach (var go in _xpHistoryRows) if (go != null) Destroy(go);
            _xpHistoryRows.Clear();

            foreach (var (amount, source, timestamp) in _progression.XPHistory)
            {
                var go = Instantiate(xpHistoryRowPrefab, xpHistoryContainer);
                var texts = go.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 1) texts[0].text = $"+{amount} XP";
                if (texts.Length >= 2) texts[1].text = source;
                if (texts.Length >= 3) texts[2].text = timestamp;
                _xpHistoryRows.Add(go);
            }
        }
    }
}
