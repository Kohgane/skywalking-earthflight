using UnityEngine;
using UnityEngine.UI;
using SWEF.Localization;

namespace SWEF.Achievement
{
    /// <summary>
    /// Displays a single achievement inside the achievement gallery grid.
    /// Handles icon grayscale for locked achievements, hidden achievements ("???"),
    /// progress bar, tap-to-expand, and the share button.
    /// </summary>
    public class AchievementCardUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Icon")]
        [SerializeField] private Image iconImage;

        [Header("Text")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text xpText;
        [SerializeField] private Text unlockDateText;

        [Header("Progress")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text   progressLabel;

        [Header("Tier")]
        [SerializeField] private Image tierIndicator;

        [Header("Detail Panel")]
        [SerializeField] private GameObject detailPanel;

        [Header("Buttons")]
        [SerializeField] private Button shareButton;
        [SerializeField] private Button tapArea;

        // ── Tier colours (same palette as notification UI) ────────────────────────
        private static readonly Color32 ColourBronze   = new Color32(0xCD, 0x7F, 0x32, 0xFF);
        private static readonly Color32 ColourSilver   = new Color32(0xC0, 0xC0, 0xC0, 0xFF);
        private static readonly Color32 ColourGold     = new Color32(0xFF, 0xD7, 0x00, 0xFF);
        private static readonly Color32 ColourPlatinum = new Color32(0xE5, 0xE4, 0xE2, 0xFF);
        private static readonly Color32 ColourDiamond  = new Color32(0xB9, 0xF2, 0xFF, 0xFF);

        // ── Runtime data ──────────────────────────────────────────────────────────
        private AchievementDefinition _def;
        private AchievementState      _state;
        private bool _expanded;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Populates the card with the given definition and current state.</summary>
        public void Populate(AchievementDefinition def, AchievementState state)
        {
            _def   = def;
            _state = state;

            var loc = LocalizationManager.Instance;

            bool locked = !state.unlocked;
            bool hidden = def.isHidden && locked;

            // Icon — grayscale when locked.
            if (iconImage != null)
            {
                iconImage.sprite = def.icon;
                iconImage.color  = locked
                    ? new Color(0.35f, 0.35f, 0.35f, 1f)
                    : Color.white;
            }

            // Title.
            if (titleText != null)
            {
                if (hidden)
                    titleText.text = loc != null ? loc.Get("achievement_hidden") : "???";
                else
                    titleText.text = loc != null ? loc.Get(def.titleKey) : def.titleKey;
            }

            // Description.
            if (descriptionText != null)
            {
                if (hidden)
                    descriptionText.text = "";
                else
                    descriptionText.text = loc != null ? loc.Get(def.descriptionKey) : def.descriptionKey;
            }

            // XP reward.
            if (xpText != null)
                xpText.text = $"+{def.xpReward} XP";

            // Unlock date.
            if (unlockDateText != null)
            {
                string dateStr = "";
                if (state.unlocked && !string.IsNullOrEmpty(state.unlockDateISO) &&
                    System.DateTime.TryParse(state.unlockDateISO, out var parsed))
                {
                    dateStr = parsed.ToString("yyyy-MM-dd");
                }
                unlockDateText.text = dateStr;
            }

            // Progress bar.
            if (progressBar != null)
            {
                progressBar.minValue = 0f;
                progressBar.maxValue = 1f;
                progressBar.value    = state.Progress01;
            }

            if (progressLabel != null)
                progressLabel.text = state.unlocked
                    ? "✓"
                    : $"{Mathf.RoundToInt(state.Progress01 * 100f)}%";

            // Tier badge.
            if (tierIndicator != null)
                tierIndicator.color = TierColour(def.tier);

            // Detail panel — collapsed by default.
            detailPanel?.SetActive(false);
            _expanded = false;

            // Buttons.
            tapArea?.onClick.RemoveAllListeners();
            tapArea?.onClick.AddListener(ToggleDetail);

            shareButton?.onClick.RemoveAllListeners();
            if (state.unlocked)
            {
                shareButton?.gameObject.SetActive(true);
                shareButton?.onClick.AddListener(OnShareClicked);
            }
            else
            {
                shareButton?.gameObject.SetActive(false);
            }
        }

        // ── Handlers ─────────────────────────────────────────────────────────────
        private void ToggleDetail()
        {
            _expanded = !_expanded;
            detailPanel?.SetActive(_expanded);
        }

        private void OnShareClicked()
        {
            var sharer = FindFirstObjectByType<AchievementShareController>();
            sharer?.ShareAchievement(_def);
        }

        private static Color TierColour(AchievementTier tier) => tier switch
        {
            AchievementTier.Silver   => ColourSilver,
            AchievementTier.Gold     => ColourGold,
            AchievementTier.Platinum => ColourPlatinum,
            AchievementTier.Diamond  => ColourDiamond,
            _                        => ColourBronze
        };
    }
}
