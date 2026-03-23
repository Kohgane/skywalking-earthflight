// AchievementPopupUI.cs — SWEF Achievement Notification & Popup System

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.AchievementNotification
{
    /// <summary>
    /// MonoBehaviour that composes and themes an achievement popup card.
    ///
    /// <para>The card can operate in two modes set via <see cref="SetDisplayMode"/>:
    /// <list type="bullet">
    ///   <item><see cref="DisplayMode.Mini"/> — compact toast card shown in-flight.</item>
    ///   <item><see cref="DisplayMode.Full"/> — large full-screen unlock card.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Tier-based color theming is applied automatically when
    /// <see cref="SetupCard"/> is called:
    /// Bronze = brown, Silver = gray, Gold = yellow,
    /// Platinum = near-white, Secret = purple.</para>
    /// </summary>
    public class AchievementPopupUI : MonoBehaviour
    {
        #region Inspector — Content

        [Header("Content References")]
        [Tooltip("Image component for the achievement icon.")]
        [SerializeField] private Image iconImage;

        [Tooltip("Text component for the achievement title.")]
        [SerializeField] private Text titleText;

        [Tooltip("Text component for the achievement description.")]
        [SerializeField] private Text descriptionText;

        [Tooltip("Text component for the XP reward value.")]
        [SerializeField] private Text xpText;

        [Tooltip("Image component for the tier badge icon.")]
        [SerializeField] private Image tierBadgeImage;

        [Tooltip("Text component for the tier badge label (e.g. 'GOLD').")]
        [SerializeField] private Text tierBadgeLabel;

        #endregion

        #region Inspector — Theming

        [Header("Tier Theming")]
        [Tooltip("Background graphic whose color changes to match the achievement tier.")]
        [SerializeField] private Graphic backgroundGraphic;

        [Tooltip("Accent graphic (border / glow) whose color changes to match the tier.")]
        [SerializeField] private Graphic accentGraphic;

        [Tooltip("Bronze-tier color (default: bronze-brown).")]
        [SerializeField] private Color bronzeColor  = new Color(0.80f, 0.50f, 0.20f, 1f);

        [Tooltip("Silver-tier color (default: light gray).")]
        [SerializeField] private Color silverColor  = new Color(0.75f, 0.75f, 0.75f, 1f);

        [Tooltip("Gold-tier color (default: gold-yellow).")]
        [SerializeField] private Color goldColor    = new Color(1.00f, 0.84f, 0.00f, 1f);

        [Tooltip("Platinum-tier color (default: near-white).")]
        [SerializeField] private Color platinumColor = new Color(0.90f, 0.95f, 1.00f, 1f);

        [Tooltip("Secret-tier color (default: purple).")]
        [SerializeField] private Color secretColor  = new Color(0.60f, 0.20f, 0.80f, 1f);

        #endregion

        #region Inspector — Display Mode

        [Header("Display Mode")]
        [Tooltip("GameObject shown only in Mini mode.")]
        [SerializeField] private GameObject miniOnlyRoot;

        [Tooltip("GameObject shown only in Full mode.")]
        [SerializeField] private GameObject fullOnlyRoot;

        #endregion

        #region State

        private DisplayMode _currentMode = DisplayMode.Mini;

        #endregion

        #region Public API

        /// <summary>
        /// Populates all UI elements with data from <paramref name="info"/> and applies
        /// tier-based color theming.
        /// </summary>
        /// <param name="info">Achievement data to display.</param>
        public void SetupCard(AchievementDisplayInfo info)
        {
            // Icon.
            if (iconImage != null && !string.IsNullOrEmpty(info.iconPath))
            {
                var sprite = Resources.Load<Sprite>(info.iconPath);
                if (sprite != null) iconImage.sprite = sprite;
            }

            // Text.
            if (titleText != null)       titleText.text       = info.title;
            if (descriptionText != null) descriptionText.text = info.description;
            if (xpText != null)          xpText.text          = "+" + info.xpReward + " XP";

            // Tier badge.
            string tierName = info.tier.ToString().ToUpper();
            if (tierBadgeLabel != null)  tierBadgeLabel.text  = tierName;

            // Apply tier color.
            Color tierColor = GetTierColor(info.tier);
            if (backgroundGraphic != null) backgroundGraphic.color = tierColor;
            if (accentGraphic != null)     accentGraphic.color     = tierColor;
            if (tierBadgeImage != null)    tierBadgeImage.color    = tierColor;
        }

        /// <summary>
        /// Switches the card between <see cref="DisplayMode.Mini"/> and
        /// <see cref="DisplayMode.Full"/> layouts.
        /// </summary>
        /// <param name="mode">Target display mode.</param>
        public void SetDisplayMode(DisplayMode mode)
        {
            _currentMode = mode;
            bool isMini = mode == DisplayMode.Mini;
            if (miniOnlyRoot != null) miniOnlyRoot.SetActive(isMini);
            if (fullOnlyRoot != null) fullOnlyRoot.SetActive(!isMini);
        }

        /// <summary>Returns the currently active display mode.</summary>
        public DisplayMode CurrentMode => _currentMode;

        #endregion

        #region Helpers

        private Color GetTierColor(AchievementTier tier)
        {
            switch (tier)
            {
                case AchievementTier.Silver:   return silverColor;
                case AchievementTier.Gold:     return goldColor;
                case AchievementTier.Platinum: return platinumColor;
                case AchievementTier.Secret:   return secretColor;
                default:                       return bronzeColor;
            }
        }

        #endregion
    }
}
