// CreatorProfileData.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>
    /// Serializable profile for a marketplace content creator.
    /// Persisted inside <c>creator_profile.json</c> by <see cref="CreatorDashboardController"/>.
    /// </summary>
    [Serializable]
    public class CreatorProfileData
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Unique creator identifier (matches the player's account ID).</summary>
        [Tooltip("Unique creator identifier.")]
        public string creatorId = string.Empty;

        /// <summary>Public display name of the creator.</summary>
        [Tooltip("Public display name.")]
        public string displayName = string.Empty;

        /// <summary>Short biography shown on the creator page.</summary>
        [Tooltip("Short biography.")]
        public string bio = string.Empty;

        // ── Stats ─────────────────────────────────────────────────────────────

        /// <summary>Total number of published listings by this creator.</summary>
        [Tooltip("Total published listings.")]
        public int totalListings;

        /// <summary>Combined download count across all listings.</summary>
        [Tooltip("Combined download count.")]
        public int totalDownloads;

        /// <summary>Weighted average rating across all rated listings.</summary>
        [Tooltip("Weighted average rating [1–5].")]
        public float averageRating;

        /// <summary>Number of players following this creator.</summary>
        [Tooltip("Number of followers.")]
        public int followerCount;

        // ── Flags ─────────────────────────────────────────────────────────────

        /// <summary>Whether this creator has been verified by moderation.</summary>
        [Tooltip("Whether this creator is verified.")]
        public bool isVerified;

        // ── Timestamps ────────────────────────────────────────────────────────

        /// <summary>UTC date the creator profile was first created (ISO-8601).</summary>
        [Tooltip("UTC join date (ISO-8601).")]
        public string joinedAt = DateTime.UtcNow.ToString("o");

        // ── Featured Content ──────────────────────────────────────────────────

        /// <summary>Listing IDs pinned by the creator to appear at the top of their profile.</summary>
        [Tooltip("Listing IDs featured on the creator profile.")]
        public List<string> featuredListings = new List<string>();
    }
}
