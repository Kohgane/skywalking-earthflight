// SquadronBaseRenderer.cs — Phase 109: Clan/Squadron System
// UI-based renderer for the squadron base — facility cards, levels, interactions.
// Namespace: SWEF.Squadron

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — Renders the squadron base as a set of UI elements.
    /// Shows facility cards with levels, upgrade progress, and interactive selection.
    /// Also displays a trophy section for completed missions and achievements.
    /// </summary>
    public sealed class SquadronBaseRenderer : MonoBehaviour
    {
        // ── Inspector references ───────────────────────────────────────────────

        [Header("Base UI")]
        [SerializeField] private Transform facilityGrid;
        [SerializeField] private GameObject facilityCardPrefab;

        [Header("Trophy Room")]
        [SerializeField] private Transform trophyGrid;
        [SerializeField] private GameObject trophyItemPrefab;

        [Header("Selected Facility")]
        [SerializeField] private GameObject facilityDetailPanel;
        [SerializeField] private Text facilityNameText;
        [SerializeField] private Text facilityLevelText;
        [SerializeField] private Text facilityBonusText;
        [SerializeField] private Text facilityUpgradeCostText;
        [SerializeField] private Button upgradeButton;

        // ── State ──────────────────────────────────────────────────────────────

        private SquadronFacility _selectedFacility;
        private readonly List<GameObject> _facilityCards = new List<GameObject>();
        private readonly List<GameObject> _trophyItems   = new List<GameObject>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (SquadronBaseManager.Instance != null)
            {
                SquadronBaseManager.Instance.OnFacilityUpgraded += HandleFacilityUpgraded;
                SquadronBaseManager.Instance.OnAreaUnlocked      += HandleAreaUnlocked;
                SquadronBaseManager.Instance.OnDecorationPlaced  += HandleDecorationPlaced;
            }

            RefreshView();
        }

        private void OnDisable()
        {
            if (SquadronBaseManager.Instance != null)
            {
                SquadronBaseManager.Instance.OnFacilityUpgraded -= HandleFacilityUpgraded;
                SquadronBaseManager.Instance.OnAreaUnlocked      -= HandleAreaUnlocked;
                SquadronBaseManager.Instance.OnDecorationPlaced  -= HandleDecorationPlaced;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Refreshes all facility cards from the current base state.</summary>
        public void RefreshView()
        {
            BuildFacilityGrid();
            RefreshTrophyRoom();
            if (facilityDetailPanel != null)
                facilityDetailPanel.SetActive(false);
        }

        /// <summary>Selects a facility for the detail panel.</summary>
        public void SelectFacility(SquadronFacility facility)
        {
            _selectedFacility = facility;
            ShowFacilityDetail(facility);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void BuildFacilityGrid()
        {
            foreach (var card in _facilityCards)
                if (card != null) Destroy(card);
            _facilityCards.Clear();

            if (facilityGrid == null || facilityCardPrefab == null) return;

            foreach (SquadronFacility facility in Enum.GetValues(typeof(SquadronFacility)))
            {
                var card = Instantiate(facilityCardPrefab, facilityGrid);
                _facilityCards.Add(card);

                int level = SquadronBaseManager.Instance?.GetFacilityLevel(facility) ?? 0;

                // Populate card texts via child Text components by index (loose coupling)
                var texts = card.GetComponentsInChildren<Text>();
                if (texts.Length > 0) texts[0].text = facility.ToString();
                if (texts.Length > 1) texts[1].text  = $"Lv {level}";

                // Button to select this facility
                var btn = card.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    var captured = facility;
                    btn.onClick.AddListener(() => SelectFacility(captured));
                }
            }
        }

        private void ShowFacilityDetail(SquadronFacility facility)
        {
            if (facilityDetailPanel == null) return;
            facilityDetailPanel.SetActive(true);

            int level = SquadronBaseManager.Instance?.GetFacilityLevel(facility) ?? 0;
            float bonus = SquadronBaseManager.Instance?.GetFacilityBonus(facility) ?? 0f;

            if (facilityNameText  != null) facilityNameText.text  = facility.ToString();
            if (facilityLevelText != null) facilityLevelText.text  = $"Level {level} / {SquadronConfig.FacilityMaxLevel}";
            if (facilityBonusText != null) facilityBonusText.text  = $"Bonus: {bonus:F0}";

            bool canUpgrade = level < SquadronConfig.FacilityMaxLevel;
            if (facilityUpgradeCostText != null)
            {
                facilityUpgradeCostText.text = canUpgrade
                    ? $"Upgrade: {SquadronConfig.FacilityUpgradeCosts[level]} XP"
                    : "MAX LEVEL";
            }

            if (upgradeButton != null)
            {
                upgradeButton.interactable = canUpgrade &&
                    (SquadronManager.Instance?.HasPermission(SquadronPermission.EditBase) ?? false);
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(() =>
                {
                    SquadronBaseManager.Instance?.UpgradeFacility(_selectedFacility);
                    ShowFacilityDetail(_selectedFacility);
                    BuildFacilityGrid();
                });
            }
        }

        private void RefreshTrophyRoom()
        {
            foreach (var item in _trophyItems)
                if (item != null) Destroy(item);
            _trophyItems.Clear();

            if (trophyGrid == null || trophyItemPrefab == null) return;

            var completed = SquadronMissionController.Instance?.GetCompletedMissions();
            if (completed == null) return;

            foreach (var mission in completed)
            {
                var item = Instantiate(trophyItemPrefab, trophyGrid);
                _trophyItems.Add(item);

                var texts = item.GetComponentsInChildren<Text>();
                if (texts.Length > 0) texts[0].text = mission.title;
                if (texts.Length > 1) texts[1].text  = mission.missionType.ToString();
            }
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void HandleFacilityUpgraded(SquadronFacility facility, int newLevel)
        {
            BuildFacilityGrid();
            if (_selectedFacility == facility)
                ShowFacilityDetail(facility);
        }

        private void HandleAreaUnlocked(string areaId)
        {
            Debug.Log($"[SquadronBaseRenderer] Area unlocked: {areaId}");
        }

        private void HandleDecorationPlaced(string decorationId)
        {
            Debug.Log($"[SquadronBaseRenderer] Decoration placed: {decorationId}");
        }
    }
}
