// MarketplaceManager.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>
    /// Singleton MonoBehaviour that manages the full lifecycle of the Community Content Marketplace:
    /// listing discovery, publishing, purchasing, library management, and notifications.
    ///
    /// <para>Persistence files written to <c>Application.persistentDataPath</c>:</para>
    /// <list type="bullet">
    ///   <item><c>marketplace_listings.json</c> — all published listings.</item>
    ///   <item><c>marketplace_library.json</c> — the local player's purchased/downloaded library.</item>
    /// </list>
    /// </summary>
    public class MarketplaceManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Shared singleton instance.</summary>
        public static MarketplaceManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _listingsPath = Path.Combine(Application.persistentDataPath, "marketplace_listings.json");
            _libraryPath  = Path.Combine(Application.persistentDataPath, "marketplace_library.json");

            LoadListings();
            LoadLibrary();
        }

        #endregion

        #region Inspector

        [Header("Marketplace Settings")]
        [Tooltip("Maximum number of tags allowed per listing.")]
        [SerializeField] private int _maxTagsPerListing = 10;

        [Tooltip("Maximum size in characters of the content-data payload per listing.")]
        [SerializeField] private int _maxContentDataLength = 65536;

        [Tooltip("Maximum number of characters in a listing title.")]
        [SerializeField] private int _maxTitleLength = 80;

        [Tooltip("Maximum number of characters in a listing description.")]
        [SerializeField] private int _maxDescriptionLength = 2000;

        #endregion

        #region Events

        /// <summary>Raised when a listing is published. Argument is the new listing.</summary>
        public event Action<MarketplaceListingData> OnListingPublished;

        /// <summary>Raised when a listing is purchased. Argument is the transaction.</summary>
        public event Action<MarketplaceTransactionData> OnListingPurchased;

        /// <summary>Raised when a free listing is downloaded. Argument is the transaction.</summary>
        public event Action<MarketplaceTransactionData> OnListingDownloaded;

        /// <summary>Raised when a listing is unpublished or removed. Argument is the listing ID.</summary>
        public event Action<string> OnListingRemoved;

        #endregion

        #region Private State

        private readonly List<MarketplaceListingData>     _listings     = new List<MarketplaceListingData>();
        private readonly List<MarketplaceTransactionData> _library      = new List<MarketplaceTransactionData>();
        private readonly List<MarketplaceNotificationData> _notifications = new List<MarketplaceNotificationData>();

        private string _listingsPath;
        private string _libraryPath;

        // Simulated local player ID — in production, fetch from PlayerProfileManager.
        private string LocalPlayerId => "local_player";

        #endregion

        #region Public API — Browse

        /// <summary>
        /// Returns a paginated list of published listings matching the given <paramref name="query"/>.
        /// </summary>
        /// <param name="query">Search and filter parameters.</param>
        /// <returns>Filtered, sorted, paged listing subset.</returns>
        public List<MarketplaceListingData> GetListings(MarketplaceSearchQuery query)
        {
            if (query == null)
            {
                Debug.LogWarning("[SWEF] Marketplace: GetListings called with null query.");
                return new List<MarketplaceListingData>();
            }

            IEnumerable<MarketplaceListingData> results = _listings.Where(l => l.isPublished);

            // Category filter
            if (query.category.HasValue)
                results = results.Where(l => l.category == query.category.Value);

            // Text search
            if (!string.IsNullOrEmpty(query.searchText))
            {
                string lower = query.searchText.ToLowerInvariant();
                results = results.Where(l =>
                    l.title.ToLowerInvariant().Contains(lower) ||
                    l.description.ToLowerInvariant().Contains(lower) ||
                    l.tags.Any(t => t.ToLowerInvariant().Contains(lower)));
            }

            // Rating filter
            if (query.minRating > 0)
                results = results.Where(l => l.ratingAverage >= query.minRating);

            // Price filter
            if (query.freeOnly)
                results = results.Where(l => l.isFree);
            else if (query.maxPrice > 0)
                results = results.Where(l => l.isFree || l.price <= query.maxPrice);

            // Verified filter
            if (query.verifiedOnly)
                results = results.Where(l => l.isVerified);

            // Creator filter
            if (!string.IsNullOrEmpty(query.creatorId))
                results = results.Where(l => l.sellerId == query.creatorId);

            // Tag filter
            if (query.tags != null && query.tags.Count > 0)
                results = results.Where(l => query.tags.All(t => l.tags.Contains(t)));

            // Sort
            results = query.sortBy switch
            {
                MarketplaceSortBy.Popular        => results.OrderByDescending(l => l.downloadCount),
                MarketplaceSortBy.HighestRated   => results.OrderByDescending(l => l.ratingAverage),
                MarketplaceSortBy.MostDownloaded => results.OrderByDescending(l => l.downloadCount),
                MarketplaceSortBy.PriceAsc       => results.OrderBy(l => l.isFree ? 0 : l.price),
                MarketplaceSortBy.PriceDesc      => results.OrderByDescending(l => l.isFree ? 0 : l.price),
                _                                => results.OrderByDescending(l => l.createdAt),
            };

            // Pagination
            int skip = Mathf.Max(0, query.page) * Mathf.Max(1, query.pageSize);
            return results.Skip(skip).Take(Mathf.Max(1, query.pageSize)).ToList();
        }

        /// <summary>Returns the listing with the given ID, or <c>null</c> if not found.</summary>
        /// <param name="id">Listing ID to look up.</param>
        public MarketplaceListingData GetListingById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _listings.Find(l => l.listingId == id);
        }

        /// <summary>Returns all published listings created by the local player.</summary>
        public List<MarketplaceListingData> GetMyListings()
        {
            return _listings.Where(l => l.sellerId == LocalPlayerId).ToList();
        }

        /// <summary>Returns all transactions (purchases + free downloads) made by the local player.</summary>
        public List<MarketplaceTransactionData> GetMyPurchases()
        {
            return _library.Where(t => t.buyerId == LocalPlayerId &&
                                       t.transactionType != TransactionType.Refund).ToList();
        }

        /// <summary>Returns the full local content library (all acquired listings).</summary>
        public List<MarketplaceTransactionData> GetMyLibrary() => new List<MarketplaceTransactionData>(_library);

        /// <summary>Returns pending notifications for the local player.</summary>
        public List<MarketplaceNotificationData> GetNotifications()
        {
            return _notifications.Where(n => n.recipientId == LocalPlayerId).ToList();
        }

        #endregion

        #region Public API — Publish / Update / Unpublish

        /// <summary>
        /// Validates and publishes a new listing.
        /// Content is validated via the Security system before publishing.
        /// </summary>
        /// <param name="listing">Listing data to publish.</param>
        /// <returns><c>true</c> if successfully published.</returns>
        public bool PublishListing(MarketplaceListingData listing)
        {
            if (listing == null)
            {
                Debug.LogWarning("[SWEF] Marketplace: PublishListing called with null listing.");
                return false;
            }

            if (!ValidateListingForPublish(listing, out string reason))
            {
                Debug.LogWarning($"[SWEF] Marketplace: PublishListing rejected — {reason}");
                return false;
            }

            listing.sellerId    = LocalPlayerId;
            listing.isPublished = true;
            listing.createdAt   = DateTime.UtcNow.ToString("o");
            listing.updatedAt   = listing.createdAt;

            _listings.Add(listing);
            SaveListings();

            OnListingPublished?.Invoke(listing);
            MarketplaceAnalytics.RecordListingPublished(listing.listingId, listing.category.ToString(), listing.isFree);
            MarketplaceBridge.OnListingPublished(listing);

            return true;
        }

        /// <summary>Unpublishes (hides) a listing owned by the local player.</summary>
        /// <param name="id">Listing ID to unpublish.</param>
        public void UnpublishListing(string id)
        {
            var listing = _listings.Find(l => l.listingId == id && l.sellerId == LocalPlayerId);
            if (listing == null)
            {
                Debug.LogWarning($"[SWEF] Marketplace: UnpublishListing — listing {id} not found or not owned.");
                return;
            }

            listing.isPublished = false;
            listing.updatedAt   = DateTime.UtcNow.ToString("o");
            SaveListings();

            OnListingRemoved?.Invoke(id);
            MarketplaceAnalytics.RecordListingRemoved(id);
        }

        /// <summary>Updates an existing listing owned by the local player.</summary>
        /// <param name="updated">Listing data with the same <c>listingId</c> but updated fields.</param>
        public void UpdateListing(MarketplaceListingData updated)
        {
            if (updated == null) return;

            int idx = _listings.FindIndex(l => l.listingId == updated.listingId && l.sellerId == LocalPlayerId);
            if (idx < 0)
            {
                Debug.LogWarning($"[SWEF] Marketplace: UpdateListing — listing {updated.listingId} not found or not owned.");
                return;
            }

            if (!ValidateListingForPublish(updated, out string reason))
            {
                Debug.LogWarning($"[SWEF] Marketplace: UpdateListing rejected — {reason}");
                return;
            }

            updated.updatedAt  = DateTime.UtcNow.ToString("o");
            _listings[idx]     = updated;
            SaveListings();
        }

        #endregion

        #region Public API — Acquire Content

        /// <summary>
        /// Purchases a paid listing: deducts currency, records a transaction, and adds it to the library.
        /// </summary>
        /// <param name="id">Listing ID to purchase.</param>
        /// <returns><c>true</c> if purchase succeeded.</returns>
        public bool PurchaseListing(string id)
        {
            var listing = GetListingById(id);
            if (listing == null || !listing.isPublished)
            {
                Debug.LogWarning($"[SWEF] Marketplace: PurchaseListing — listing {id} not available.");
                return false;
            }

            if (listing.isFree)
                return DownloadFreeContent(id);

            if (AlreadyOwns(id))
            {
                Debug.LogWarning($"[SWEF] Marketplace: PurchaseListing — already owns {id}.");
                return false;
            }

            // Deduct currency via bridge
            if (!MarketplaceBridge.TryDeductCurrency(listing.price, $"marketplace_purchase:{id}"))
            {
                Debug.LogWarning($"[SWEF] Marketplace: PurchaseListing — insufficient currency for {id}.");
                return false;
            }

            var transaction = new MarketplaceTransactionData
            {
                buyerId         = LocalPlayerId,
                sellerId        = listing.sellerId,
                listingId       = id,
                price           = listing.price,
                transactionType = TransactionType.Purchase,
                status          = TransactionStatus.Completed,
            };

            listing.downloadCount++;
            _library.Add(transaction);
            SaveLibrary();
            SaveListings();

            OnListingPurchased?.Invoke(transaction);
            MarketplaceAnalytics.RecordListingPurchased(id, listing.price);
            MarketplaceBridge.OnListingPurchased(listing, transaction);

            AddNotification(listing.sellerId, MarketplaceNotificationType.Sale,
                "marketplace_notif_sale_title", $"Your listing \"{listing.title}\" was purchased!", id);

            return true;
        }

        /// <summary>
        /// Downloads a free listing and adds it to the library.
        /// </summary>
        /// <param name="id">Listing ID to download.</param>
        /// <returns><c>true</c> if download succeeded.</returns>
        public bool DownloadFreeContent(string id)
        {
            var listing = GetListingById(id);
            if (listing == null || !listing.isPublished || !listing.isFree)
            {
                Debug.LogWarning($"[SWEF] Marketplace: DownloadFreeContent — listing {id} not available or not free.");
                return false;
            }

            if (AlreadyOwns(id))
            {
                Debug.LogWarning($"[SWEF] Marketplace: DownloadFreeContent — already owns {id}.");
                return false;
            }

            var transaction = new MarketplaceTransactionData
            {
                buyerId         = LocalPlayerId,
                sellerId        = listing.sellerId,
                listingId       = id,
                price           = 0,
                transactionType = TransactionType.Free,
                status          = TransactionStatus.Completed,
            };

            listing.downloadCount++;
            _library.Add(transaction);
            SaveLibrary();
            SaveListings();

            OnListingDownloaded?.Invoke(transaction);
            MarketplaceAnalytics.RecordListingDownloaded(id);
            MarketplaceBridge.OnListingDownloaded(listing);

            return true;
        }

        #endregion

        #region Private — Validation

        private bool ValidateListingForPublish(MarketplaceListingData listing, out string reason)
        {
            reason = string.Empty;

            if (string.IsNullOrWhiteSpace(listing.title))
            { reason = "title is empty"; return false; }

            if (listing.title.Length > _maxTitleLength)
            { reason = $"title exceeds {_maxTitleLength} characters"; return false; }

            if (listing.description.Length > _maxDescriptionLength)
            { reason = $"description exceeds {_maxDescriptionLength} characters"; return false; }

            if (listing.contentData.Length > _maxContentDataLength)
            { reason = $"content data exceeds {_maxContentDataLength} characters"; return false; }

            if (listing.tags != null && listing.tags.Count > _maxTagsPerListing)
            { reason = $"too many tags (max {_maxTagsPerListing})"; return false; }

            if (!listing.isFree && listing.price < 0)
            { reason = "price must be non-negative"; return false; }

            // Security validation via bridge
            if (!MarketplaceBridge.ValidateContentData(listing.contentData))
            { reason = "content data failed security validation"; return false; }

            // Profanity check on user-supplied text
#if SWEF_SECURITY_AVAILABLE
            if (SWEF.Security.ProfanityFilter.ContainsProfanity(listing.title) ||
                SWEF.Security.ProfanityFilter.ContainsProfanity(listing.description))
            { reason = "title or description contains inappropriate content"; return false; }
#endif

            return true;
        }

        private bool AlreadyOwns(string listingId)
        {
            return _library.Any(t => t.listingId == listingId &&
                                     t.buyerId   == LocalPlayerId &&
                                     t.status    != TransactionStatus.Refunded);
        }

        #endregion

        #region Private — Notifications

        private void AddNotification(string recipientId, MarketplaceNotificationType type,
            string title, string body, string relatedListingId = "")
        {
            _notifications.Add(new MarketplaceNotificationData
            {
                recipientId      = recipientId,
                notificationType = type,
                title            = title,
                body             = body,
                relatedListingId = relatedListingId,
            });
        }

        #endregion

        #region Persistence

        private void SaveListings()
        {
            try
            {
                string json = JsonUtility.ToJson(new ListingsWrapper { listings = _listings }, true);
                File.WriteAllText(_listingsPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to save listings — {ex.Message}");
            }
        }

        private void LoadListings()
        {
            _listings.Clear();
            if (!File.Exists(_listingsPath)) return;
            try
            {
                string json = File.ReadAllText(_listingsPath);
                var wrapper = JsonUtility.FromJson<ListingsWrapper>(json);
                if (wrapper?.listings != null)
                    _listings.AddRange(wrapper.listings);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to load listings — {ex.Message}");
            }
        }

        private void SaveLibrary()
        {
            try
            {
                string json = JsonUtility.ToJson(new LibraryWrapper { transactions = _library }, true);
                File.WriteAllText(_libraryPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to save library — {ex.Message}");
            }
        }

        private void LoadLibrary()
        {
            _library.Clear();
            if (!File.Exists(_libraryPath)) return;
            try
            {
                string json = File.ReadAllText(_libraryPath);
                var wrapper = JsonUtility.FromJson<LibraryWrapper>(json);
                if (wrapper?.transactions != null)
                    _library.AddRange(wrapper.transactions);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to load library — {ex.Message}");
            }
        }

        // Wrapper types required by JsonUtility (which cannot serialise top-level arrays)
        [Serializable] private class ListingsWrapper  { public List<MarketplaceListingData>     listings;     }
        [Serializable] private class LibraryWrapper   { public List<MarketplaceTransactionData> transactions; }

        #endregion
    }
}
