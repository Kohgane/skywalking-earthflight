// MarketplaceTransactionData.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>Type of marketplace transaction.</summary>
    public enum TransactionType
    {
        /// <summary>Standard paid purchase.</summary>
        Purchase,

        /// <summary>Free download (no currency exchanged).</summary>
        Free,

        /// <summary>Listing gifted from one player to another.</summary>
        Gift,

        /// <summary>Currency refunded to buyer.</summary>
        Refund,
    }

    /// <summary>Processing status of a marketplace transaction.</summary>
    public enum TransactionStatus
    {
        /// <summary>Transaction completed successfully.</summary>
        Completed,

        /// <summary>Transaction is being processed.</summary>
        Pending,

        /// <summary>Transaction was refunded to the buyer.</summary>
        Refunded,

        /// <summary>Creator earnings from this transaction have been withdrawn/settled.</summary>
        Settled,
    }

    /// <summary>
    /// Serializable record of a single marketplace transaction (purchase, free download, gift, or refund).
    /// Persisted by <see cref="MarketplaceManager"/> as part of the player library.
    /// </summary>
    [Serializable]
    public class MarketplaceTransactionData
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Unique transaction identifier (GUID string).</summary>
        [Tooltip("Unique transaction identifier (GUID string).")]
        public string transactionId = Guid.NewGuid().ToString();

        /// <summary>Player ID of the buyer.</summary>
        [Tooltip("Player ID of the buyer.")]
        public string buyerId = string.Empty;

        /// <summary>Player ID of the seller (empty for free content).</summary>
        [Tooltip("Player ID of the seller.")]
        public string sellerId = string.Empty;

        /// <summary>Listing that was transacted.</summary>
        [Tooltip("Listing that was transacted.")]
        public string listingId = string.Empty;

        // ── Value ─────────────────────────────────────────────────────────────

        /// <summary>Currency amount exchanged (0 for free downloads).</summary>
        [Tooltip("Currency amount exchanged.")]
        public int price;

        /// <summary>Type of this transaction.</summary>
        [Tooltip("Type of transaction.")]
        public TransactionType transactionType = TransactionType.Purchase;

        // ── Timestamps & Status ───────────────────────────────────────────────

        /// <summary>UTC timestamp of this transaction (ISO-8601).</summary>
        [Tooltip("UTC timestamp (ISO-8601).")]
        public string timestamp = DateTime.UtcNow.ToString("o");

        /// <summary>Processing status.</summary>
        [Tooltip("Transaction processing status.")]
        public TransactionStatus status = TransactionStatus.Completed;
    }
}
