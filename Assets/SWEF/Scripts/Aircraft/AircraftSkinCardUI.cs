using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Aircraft
{
    /// <summary>
    /// Individual card displayed in the <see cref="AircraftHangarUI"/> grid.
    /// Shows the skin's preview icon, display name, rarity badge, lock state,
    /// equip button, equipped indicator, and favourite star toggle.
    /// </summary>
    public class AircraftSkinCardUI : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Visuals")]
        [SerializeField] private Image previewIcon;
        [SerializeField] private Text skinNameText;
        [SerializeField] private Image rarityBadge;
        [SerializeField] private Text rarityText;

        [Header("State Indicators")]
        [SerializeField] private GameObject lockIndicator;
        [SerializeField] private Text unlockConditionText;
        [SerializeField] private GameObject equippedIndicator;
        [SerializeField] private Button favoriteButton;
        [SerializeField] private Image favoriteIcon;

        [Header("Actions")]
        [SerializeField] private Button equipButton;
        [SerializeField] private Button cardButton;

        // ── Rarity colours ────────────────────────────────────────────────────────

        private static readonly Color ColorCommon    = new Color(0.75f, 0.75f, 0.75f);
        private static readonly Color ColorUncommon  = new Color(0.25f, 0.85f, 0.25f);
        private static readonly Color ColorRare      = new Color(0.10f, 0.50f, 1.00f);
        private static readonly Color ColorEpic      = new Color(0.65f, 0.15f, 0.90f);
        private static readonly Color ColorLegendary = new Color(1.00f, 0.70f, 0.10f);

        // ── Runtime state ─────────────────────────────────────────────────────────

        private AircraftSkinDefinition _skin;
        private AircraftCustomizationManager _manager;
        private AircraftHangarUI _hangar;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Configures the card to display the given skin and wires all button callbacks.
        /// </summary>
        public void Initialize(AircraftSkinDefinition skin,
                               AircraftCustomizationManager manager,
                               AircraftHangarUI hangar)
        {
            _skin    = skin;
            _manager = manager;
            _hangar  = hangar;

            if (_skin == null) return;

            // Name
            if (skinNameText != null)
                skinNameText.text = skin.displayName;

            // Preview icon
            if (previewIcon != null && !string.IsNullOrEmpty(skin.previewIconId))
            {
                var sprite = Resources.Load<Sprite>(skin.previewIconId);
                if (sprite != null) previewIcon.sprite = sprite;
            }

            // Rarity badge
            Color rarityColor = RarityColor(skin.rarity);
            if (rarityBadge  != null) rarityBadge.color = rarityColor;
            if (rarityText   != null) rarityText.text   = skin.rarity.ToString();

            // Lock / unlock
            bool unlocked = manager != null && manager.IsSkinUnlocked(skin.skinId);
            if (lockIndicator != null) lockIndicator.SetActive(!unlocked);
            if (equipButton   != null) equipButton.gameObject.SetActive(unlocked);

            if (!unlocked && unlockConditionText != null)
                unlockConditionText.text = AircraftUnlockEvaluator.GetUnlockProgressText(skin.unlockCondition);

            // Equipped indicator
            bool equipped = manager != null &&
                            manager.ActiveLoadout != null &&
                            manager.ActiveLoadout.GetSkinForPart(skin.partType) == skin.skinId;
            if (equippedIndicator != null) equippedIndicator.SetActive(equipped);

            // Favourite
            RefreshFavoriteIcon();

            // Wire buttons
            if (equipButton   != null) equipButton.onClick.AddListener(OnEquipClicked);
            if (favoriteButton != null) favoriteButton.onClick.AddListener(OnFavoriteClicked);
            if (cardButton     != null) cardButton.onClick.AddListener(OnCardTapped);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void OnEquipClicked()
        {
            if (_hangar != null && _skin != null)
                _hangar.OnEquipPressed(_skin.skinId);
        }

        private void OnFavoriteClicked()
        {
            if (_manager == null || _skin == null) return;
            _manager.ToggleFavorite(_skin.skinId);
            RefreshFavoriteIcon();
        }

        private void OnCardTapped()
        {
            if (_hangar != null && _skin != null)
                _hangar.OnSkinCardTapped(_skin.skinId);
        }

        private void RefreshFavoriteIcon()
        {
            if (_manager == null || _skin == null || favoriteIcon == null) return;
            bool fav = _manager.IsFavorite(_skin.skinId);
            favoriteIcon.color = fav ? Color.yellow : Color.grey;
        }

        private static Color RarityColor(AircraftSkinRarity rarity)
        {
            switch (rarity)
            {
                case AircraftSkinRarity.Common:    return ColorCommon;
                case AircraftSkinRarity.Uncommon:  return ColorUncommon;
                case AircraftSkinRarity.Rare:      return ColorRare;
                case AircraftSkinRarity.Epic:      return ColorEpic;
                case AircraftSkinRarity.Legendary: return ColorLegendary;
                default: return ColorCommon;
            }
        }
    }
}
