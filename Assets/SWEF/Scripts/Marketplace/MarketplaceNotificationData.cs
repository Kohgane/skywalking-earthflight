// MarketplaceNotificationData.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>Category of marketplace notification.</summary>
    public enum MarketplaceNotificationType
    {
        /// <summary>A player purchased the creator's listing.</summary>
        Sale,

        /// <summary>A new review was submitted on one of the creator's listings.</summary>
        NewReview,

        /// <summary>A player started following the creator.</summary>
        NewFollower,

        /// <summary>One of the creator's listings was featured on the home page.</summary>
        ListingFeatured,

        /// <summary>A listing the player follows was updated.</summary>
        ListingUpdated,

        /// <summary>A gift purchase was received.</summary>
        GiftReceived,
    }

    /// <summary>
    /// Serializable record representing a marketplace notification for a player or creator.
    /// Managed in memory by <see cref="MarketplaceManager"/>; surfaced to the UI on demand.
    /// </summary>
    [Serializable]
    public class MarketplaceNotificationData
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Unique notification identifier (GUID string).</summary>
        [Tooltip("Unique notification identifier.")]
        public string notificationId = Guid.NewGuid().ToString();

        /// <summary>Player ID this notification belongs to.</summary>
        [Tooltip("Recipient player ID.")]
        public string recipientId = string.Empty;

        // ── Content ───────────────────────────────────────────────────────────

        /// <summary>Type of event that triggered this notification.</summary>
        [Tooltip("Notification type.")]
        public MarketplaceNotificationType notificationType = MarketplaceNotificationType.Sale;

        /// <summary>Localised title text for the notification.</summary>
        [Tooltip("Short title.")]
        public string title = string.Empty;

        /// <summary>Localised body text for the notification.</summary>
        [Tooltip("Notification body.")]
        public string body = string.Empty;

        /// <summary>Optional listing ID related to this notification.</summary>
        [Tooltip("Related listing ID (may be empty).")]
        public string relatedListingId = string.Empty;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>Whether the player has read this notification.</summary>
        [Tooltip("Whether the notification has been read.")]
        public bool isRead;

        /// <summary>UTC creation timestamp (ISO-8601).</summary>
        [Tooltip("UTC creation timestamp (ISO-8601).")]
        public string createdAt = DateTime.UtcNow.ToString("o");
    }
}
