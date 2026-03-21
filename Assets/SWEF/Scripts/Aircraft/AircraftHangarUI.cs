using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Localization;

namespace SWEF.Aircraft
{
    /// <summary>
    /// Full-screen hangar / garage UI. Displays a scrollable grid of
    /// <see cref="AircraftSkinCardUI"/> cards with category, rarity and sort
    /// filtering. Interacts with <see cref="AircraftCustomizationManager"/> to
    /// equip skins and with <see cref="AircraftPreviewController"/> to show 3-D
    /// previews.
    /// </summary>
    public class AircraftHangarUI : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Root")]
        [SerializeField] private GameObject hangarRoot;

        [Header("Grid")]
        [SerializeField] private Transform skinGridParent;
        [SerializeField] private GameObject skinCardPrefab;

        [Header("Filters")]
        [SerializeField] private Button[] partFilterButtons; // ordered as AircraftPartType values
        [SerializeField] private Dropdown rarityFilterDropdown;

        [Header("Sort")]
        [SerializeField] private Dropdown sortDropdown;

        [Header("Loadout Info")]
        [SerializeField] private Text loadoutNameText;
        [SerializeField] private InputField loadoutNameInput;
        [SerializeField] private Button createLoadoutButton;
        [SerializeField] private Button deleteLoadoutButton;

        [Header("Preview")]
        [SerializeField] private AircraftPreviewController previewController;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private AircraftCustomizationManager _manager;
        private AircraftSkinRegistry _registry;
        private LocalizationManager _localization;

        private AircraftPartType _partFilter = AircraftPartType.Body;
        private int _rarityFilter = -1; // -1 = all
        private int _sortMode = 0;      // 0=name, 1=rarity, 2=unlocked first

        private readonly List<AircraftSkinCardUI> _spawnedCards = new List<AircraftSkinCardUI>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _manager      = AircraftCustomizationManager.Instance;
            _registry     = AircraftSkinRegistry.Instance;
            _localization = FindObjectOfType<LocalizationManager>();

            if (rarityFilterDropdown != null)
                rarityFilterDropdown.onValueChanged.AddListener(OnRarityFilterChanged);
            if (sortDropdown != null)
                sortDropdown.onValueChanged.AddListener(SetSortMode);
            if (createLoadoutButton != null)
                createLoadoutButton.onClick.AddListener(OnCreateLoadoutClicked);
            if (deleteLoadoutButton != null)
                deleteLoadoutButton.onClick.AddListener(OnDeleteLoadoutClicked);

            // Wire part-filter buttons
            if (partFilterButtons != null)
            {
                var partValues = (AircraftPartType[])Enum.GetValues(typeof(AircraftPartType));
                for (int i = 0; i < partFilterButtons.Length && i < partValues.Length; i++)
                {
                    int capturedIdx = i;
                    AircraftPartType capturedPart = partValues[i];
                    if (partFilterButtons[capturedIdx] != null)
                        partFilterButtons[capturedIdx].onClick.AddListener(
                            () => SetPartFilter(capturedPart));
                }
            }

            if (hangarRoot != null)
                hangarRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (_manager != null)
            {
                _manager.OnSkinUnlocked    += OnSkinUnlocked;
                _manager.OnLoadoutChanged  += OnLoadoutChanged;
            }
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.OnSkinUnlocked    -= OnSkinUnlocked;
                _manager.OnLoadoutChanged  -= OnLoadoutChanged;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the hangar and refreshes the skin grid.</summary>
        public void Open()
        {
            if (hangarRoot != null) hangarRoot.SetActive(true);
            RefreshGrid();
            RefreshLoadoutInfo();
        }

        /// <summary>Hides the hangar.</summary>
        public void Close()
        {
            if (hangarRoot != null) hangarRoot.SetActive(false);
        }

        /// <summary>Rebuilds the skin card grid using current filter/sort state.</summary>
        public void RefreshGrid()
        {
            ClearGrid();

            if (_registry == null || skinCardPrefab == null || skinGridParent == null) return;

            List<AircraftSkinDefinition> skins = _registry.GetSkinsByPart(_partFilter);

            // Rarity filter
            if (_rarityFilter >= 0 && _rarityFilter < Enum.GetValues(typeof(AircraftSkinRarity)).Length)
            {
                var rarity = (AircraftSkinRarity)_rarityFilter;
                skins = skins.FindAll(s => s.rarity == rarity);
            }

            // Sort
            switch (_sortMode)
            {
                case 1: // rarity desc
                    skins.Sort((a, b) => b.rarity.CompareTo(a.rarity));
                    break;
                case 2: // unlocked first
                    skins.Sort((a, b) =>
                    {
                        bool ua = _manager != null && _manager.IsSkinUnlocked(a.skinId);
                        bool ub = _manager != null && _manager.IsSkinUnlocked(b.skinId);
                        if (ua == ub) return string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase);
                        return ua ? -1 : 1;
                    });
                    break;
                default: // name
                    skins.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase));
                    break;
            }

            foreach (var skin in skins)
            {
                var cardGO = Instantiate(skinCardPrefab, skinGridParent);
                var card = cardGO.GetComponent<AircraftSkinCardUI>();
                if (card != null)
                {
                    card.Initialize(skin, _manager, this);
                    _spawnedCards.Add(card);
                }
            }
        }

        /// <summary>Sets the active part-type filter and refreshes the grid.</summary>
        public void SetPartFilter(AircraftPartType part)
        {
            _partFilter = part;
            RefreshGrid();
        }

        /// <summary>Sets the active rarity filter (0-based enum index, -1 = all) and refreshes.</summary>
        public void SetRarityFilter(AircraftSkinRarity rarity)
        {
            _rarityFilter = (int)rarity;
            RefreshGrid();
        }

        /// <summary>Sets the sort mode (0=name, 1=rarity, 2=unlocked first) and refreshes.</summary>
        public void SetSortMode(int mode)
        {
            _sortMode = mode;
            RefreshGrid();
        }

        /// <summary>Opens the detail / preview panel for the tapped skin.</summary>
        public void OnSkinCardTapped(string skinId)
        {
            if (previewController == null || _manager == null || _registry == null) return;
            var skin = _registry.GetSkin(skinId);
            if (skin == null) return;

            previewController.PreviewSingleSkin(skin.partType, skinId);
        }

        /// <summary>Equips the given skin via <see cref="AircraftCustomizationManager"/>.</summary>
        public void OnEquipPressed(string skinId)
        {
            if (_manager == null || _registry == null) return;
            var skin = _registry.GetSkin(skinId);
            if (skin == null) return;
            _manager.EquipSkin(skin.partType, skinId);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ClearGrid()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();
        }

        private void RefreshLoadoutInfo()
        {
            if (_manager == null) return;
            var loadout = _manager.ActiveLoadout;
            if (loadout == null) return;
            if (loadoutNameText != null)
                loadoutNameText.text = loadout.loadoutName;
        }

        private void OnRarityFilterChanged(int index)
        {
            // 0 = all, then one per rarity
            _rarityFilter = index - 1;
            RefreshGrid();
        }

        private void OnSkinUnlocked(string skinId) => RefreshGrid();
        private void OnLoadoutChanged(AircraftLoadout loadout)
        {
            RefreshGrid();
            RefreshLoadoutInfo();
        }

        private void OnCreateLoadoutClicked()
        {
            if (_manager == null) return;
            string name = (loadoutNameInput != null && !string.IsNullOrEmpty(loadoutNameInput.text))
                ? loadoutNameInput.text
                : "New Loadout";
            _manager.CreateLoadout(name);
            RefreshLoadoutInfo();
        }

        private void OnDeleteLoadoutClicked()
        {
            if (_manager == null || _manager.ActiveLoadout == null) return;
            _manager.DeleteLoadout(_manager.ActiveLoadout.loadoutId);
            RefreshLoadoutInfo();
        }
    }
}
