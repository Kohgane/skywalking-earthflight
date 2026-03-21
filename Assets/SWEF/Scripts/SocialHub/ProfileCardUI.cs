using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.SocialHub
{
    /// <summary>
    /// Displays a compact summary card for a single <see cref="PlayerProfile"/>.
    /// Bind inspector references to the relevant UI elements in the prefab, then
    /// call <see cref="Bind"/> to populate the card.
    /// Optionally exposes an <c>Add Friend</c> / <c>View Profile</c> action button.
    /// </summary>
    public class ProfileCardUI : MonoBehaviour
    {
        // ── Inspector — Avatar & Identity ─────────────────────────────────────────
        [Header("Identity")]
        [Tooltip("Image component used to display the player's avatar sprite.")]
        [SerializeField] private Image avatarImage;

        [Tooltip("Fallback sprite when no avatar asset can be resolved.")]
        [SerializeField] private Sprite defaultAvatarSprite;

        [SerializeField] private TextMeshProUGUI displayNameText;
        [SerializeField] private TextMeshProUGUI titleText;

        // ── Inspector — Rank ──────────────────────────────────────────────────────
        [Header("Rank")]
        [SerializeField] private TextMeshProUGUI rankLevelText;
        [SerializeField] private TextMeshProUGUI rankNameText;

        // ── Inspector — Stats summary ─────────────────────────────────────────────
        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI totalFlightsText;
        [SerializeField] private TextMeshProUGUI totalXpText;
        [SerializeField] private TextMeshProUGUI achievementsText;
        [SerializeField] private TextMeshProUGUI dailyStreakText;
        [SerializeField] private TextMeshProUGUI seasonTierText;

        // ── Inspector — Premium badge ─────────────────────────────────────────────
        [Header("Premium")]
        [Tooltip("Badge GameObject shown when isPremium is true.")]
        [SerializeField] private GameObject premiumBadge;

        // ── Inspector — Action buttons ────────────────────────────────────────────
        [Header("Action Buttons")]
        [Tooltip("Button for sending a friend request or viewing the full profile.")]
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI actionButtonLabel;

        // ── State ─────────────────────────────────────────────────────────────────
        private PlayerProfile _profile;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Populates all UI elements on this card from the given <paramref name="profile"/>.
        /// </summary>
        public void Bind(PlayerProfile profile)
        {
            _profile = profile;
            if (profile == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // Identity
            if (displayNameText != null)
                displayNameText.text = profile.displayName;

            if (titleText != null)
                titleText.text = string.IsNullOrEmpty(profile.titleId) ? string.Empty : profile.titleId;

            // Avatar — attempt to load from Resources; fall back to default.
            if (avatarImage != null)
            {
                Sprite loaded = null;
                if (!string.IsNullOrEmpty(profile.avatarId))
                    loaded = Resources.Load<Sprite>($"Avatars/{profile.avatarId}");
                avatarImage.sprite = loaded != null ? loaded : defaultAvatarSprite;
            }

            // Rank
            if (rankLevelText != null)
                rankLevelText.text = profile.pilotRankLevel.ToString();
            if (rankNameText != null)
                rankNameText.text = profile.pilotRankName;

            // Stats
            if (totalFlightsText != null)
                totalFlightsText.text = profile.totalFlights.ToString();
            if (totalXpText != null)
                totalXpText.text = FormatLargeNumber(profile.totalXP);
            if (achievementsText != null)
                achievementsText.text = $"{profile.achievementsUnlocked}/{profile.achievementsTotal}";
            if (dailyStreakText != null)
                dailyStreakText.text = profile.dailyStreak.ToString();
            if (seasonTierText != null)
                seasonTierText.text = profile.seasonTier.ToString();

            // Premium badge
            if (premiumBadge != null)
                premiumBadge.SetActive(profile.isPremium);
        }

        /// <summary>
        /// Configures the action button label and click callback.
        /// Pass <c>null</c> for <paramref name="onClick"/> to hide the button.
        /// </summary>
        public void SetActionButton(string label, UnityEngine.Events.UnityAction onClick)
        {
            if (actionButton == null) return;

            if (onClick == null)
            {
                actionButton.gameObject.SetActive(false);
                return;
            }

            actionButton.gameObject.SetActive(true);
            if (actionButtonLabel != null)
                actionButtonLabel.text = label;

            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(onClick);
        }

        /// <summary>Returns the profile currently bound to this card, or <c>null</c>.</summary>
        public PlayerProfile GetProfile() => _profile;

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string FormatLargeNumber(long value)
        {
            if (value >= 1_000_000) return $"{value / 1_000_000f:0.#}M";
            if (value >= 1_000)    return $"{value / 1_000f:0.#}K";
            return value.ToString();
        }
    }
}
