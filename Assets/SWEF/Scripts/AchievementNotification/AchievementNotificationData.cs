// AchievementNotificationData.cs — SWEF Achievement Notification & Popup System

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AchievementNotification
{
    #region Enumerations

    /// <summary>Rarity tier of an achievement, used for color theming and sound escalation.</summary>
    public enum AchievementTier
    {
        /// <summary>Entry-level achievement.</summary>
        Bronze,
        /// <summary>Intermediate achievement.</summary>
        Silver,
        /// <summary>High-value achievement.</summary>
        Gold,
        /// <summary>Top-tier achievement.</summary>
        Platinum,
        /// <summary>Hidden or special achievement revealed only on unlock.</summary>
        Secret
    }

    /// <summary>Display priority of a queued notification.</summary>
    public enum NotificationPriority
    {
        /// <summary>Shown after all other notifications.</summary>
        Low,
        /// <summary>Standard queue ordering.</summary>
        Normal,
        /// <summary>Inserted ahead of Normal and Low notifications.</summary>
        High,
        /// <summary>Shown immediately, interrupting the current notification if necessary.</summary>
        Critical
    }

    /// <summary>Type of reward attached to an achievement.</summary>
    public enum RewardType
    {
        /// <summary>Experience points awarded to the pilot.</summary>
        XP,
        /// <summary>In-game currency.</summary>
        Currency,
        /// <summary>Cosmetic item (skin, trail, icon).</summary>
        Cosmetic,
        /// <summary>Pilot title or rank prefix.</summary>
        Title
    }

    /// <summary>Orientation from which a toast slides into the screen.</summary>
    public enum SlideDirection
    {
        /// <summary>Slides in from the top edge.</summary>
        Top,
        /// <summary>Slides in from the bottom edge.</summary>
        Bottom,
        /// <summary>Slides in from the left edge.</summary>
        Left,
        /// <summary>Slides in from the right edge.</summary>
        Right
    }

    /// <summary>Compact vs full card display mode for the popup UI.</summary>
    public enum DisplayMode
    {
        /// <summary>Small toast card shown in-flight.</summary>
        Mini,
        /// <summary>Large full-screen unlock card.</summary>
        Full
    }

    #endregion

    #region Configuration Structs

    /// <summary>Runtime tuning parameters for the notification system.</summary>
    [Serializable]
    public struct AchievementNotificationConfig
    {
        /// <summary>How long (seconds) each notification stays visible before auto-dismiss.</summary>
        [Tooltip("How long (seconds) each notification stays visible before auto-dismiss.")]
        public float displayDuration;

        /// <summary>Speed multiplier for the slide-in animation (higher = faster).</summary>
        [Tooltip("Speed multiplier for the slide-in animation.")]
        public float slideInSpeed;

        /// <summary>Duration (seconds) of the fade-out when a notification is dismissed.</summary>
        [Tooltip("Duration (seconds) of the fade-out when a notification is dismissed.")]
        public float fadeDuration;

        /// <summary>Maximum number of notifications held in the queue at one time.</summary>
        [Tooltip("Maximum number of notifications held in the queue at one time.")]
        public int maxQueueSize;

        /// <summary>Returns a config with sensible defaults.</summary>
        public static AchievementNotificationConfig Default => new AchievementNotificationConfig
        {
            displayDuration = 4f,
            slideInSpeed    = 8f,
            fadeDuration    = 0.4f,
            maxQueueSize    = 20
        };
    }

    /// <summary>All data required to display a single achievement notification.</summary>
    [Serializable]
    public struct AchievementDisplayInfo
    {
        /// <summary>Localised title string of the achievement.</summary>
        [Tooltip("Localised title string of the achievement.")]
        public string title;

        /// <summary>Localised description string of the achievement.</summary>
        [Tooltip("Localised description string of the achievement.")]
        public string description;

        /// <summary>Resources-relative path to the achievement icon sprite.</summary>
        [Tooltip("Resources-relative path to the achievement icon sprite.")]
        public string iconPath;

        /// <summary>Rarity tier, used for color theming and sound selection.</summary>
        [Tooltip("Rarity tier of this achievement.")]
        public AchievementTier tier;

        /// <summary>XP reward granted on unlock.</summary>
        [Tooltip("XP reward granted on unlock.")]
        public int xpReward;

        /// <summary>UTC timestamp when the achievement was unlocked.</summary>
        [Tooltip("UTC timestamp when the achievement was unlocked.")]
        public DateTime unlockTimestamp;
    }

    /// <summary>Configuration for a single reward display sequence.</summary>
    [Serializable]
    public struct RewardDisplayConfig
    {
        /// <summary>Category of the reward.</summary>
        [Tooltip("Category of the reward.")]
        public RewardType rewardType;

        /// <summary>Numeric amount (XP value, currency amount, etc.).</summary>
        [Tooltip("Numeric amount for the reward counter animation.")]
        public int amount;

        /// <summary>Human-readable label shown beneath the counter (e.g. "XP", "Coins", "Eagle Wing Trail").</summary>
        [Tooltip("Human-readable label shown beneath the reward counter.")]
        public string displayString;
    }

    #endregion

    #region ScriptableObject Profile

    /// <summary>
    /// ScriptableObject that combines all notification configuration into a single
    /// reusable asset.  Create instances via
    /// <c>Assets → Create → SWEF → AchievementNotification → Profile</c>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "SWEF/AchievementNotification/Profile",
        fileName = "AchievementNotificationProfile")]
    public class AchievementNotificationProfile : ScriptableObject
    {
        #region Inspector

        [Header("Notification Config")]
        /// <summary>Core timing and queue parameters.</summary>
        [Tooltip("Core timing and queue parameters.")]
        public AchievementNotificationConfig notificationConfig = AchievementNotificationConfig.Default;

        [Header("Toast Settings")]
        /// <summary>Direction from which toast notifications slide onto the screen.</summary>
        [Tooltip("Direction from which toast notifications slide onto the screen.")]
        public SlideDirection toastSlideDirection = SlideDirection.Top;

        /// <summary>Maximum number of toast cards stacked on screen simultaneously.</summary>
        [Tooltip("Maximum number of toast cards stacked on screen simultaneously.")]
        [Range(1, 5)]
        public int maxSimultaneousToasts = 3;

        [Header("Tier Color Overrides")]
        /// <summary>Override color for Bronze-tier notifications (default: bronze-brown).</summary>
        [Tooltip("Override color for Bronze-tier notifications.")]
        public Color bronzeColor = new Color(0.80f, 0.50f, 0.20f, 1f);

        /// <summary>Override color for Silver-tier notifications (default: light gray).</summary>
        [Tooltip("Override color for Silver-tier notifications.")]
        public Color silverColor = new Color(0.75f, 0.75f, 0.75f, 1f);

        /// <summary>Override color for Gold-tier notifications (default: gold-yellow).</summary>
        [Tooltip("Override color for Gold-tier notifications.")]
        public Color goldColor = new Color(1.00f, 0.84f, 0.00f, 1f);

        /// <summary>Override color for Platinum-tier notifications (default: near-white).</summary>
        [Tooltip("Override color for Platinum-tier notifications.")]
        public Color platinumColor = new Color(0.90f, 0.95f, 1.00f, 1f);

        /// <summary>Override color for Secret-tier notifications (default: purple).</summary>
        [Tooltip("Override color for Secret-tier notifications.")]
        public Color secretColor = new Color(0.60f, 0.20f, 0.80f, 1f);

        [Header("Reward Display")]
        /// <summary>Duration (seconds) of the reward counter lerp animation.</summary>
        [Tooltip("Duration (seconds) of the reward counter lerp animation.")]
        public float rewardCounterDuration = 1.5f;

        [Header("Sound")]
        /// <summary>Whether tier-specific fanfare sounds should play on unlock.</summary>
        [Tooltip("Whether tier-specific fanfare sounds should play on unlock.")]
        public bool enableUnlockSounds = true;

        #endregion

        #region Helpers

        /// <summary>Returns the theme color mapped to the given <paramref name="tier"/>.</summary>
        /// <param name="tier">Achievement tier to look up.</param>
        public Color GetTierColor(AchievementTier tier)
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

    #endregion
}
