// ReviewManager.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>
    /// Sort options for review lists.
    /// </summary>
    public enum ReviewSortBy
    {
        /// <summary>Newest reviews first.</summary>
        Newest,

        /// <summary>Most helpful reviews first.</summary>
        MostHelpful,

        /// <summary>Highest-rated first.</summary>
        HighestRating,

        /// <summary>Lowest-rated first.</summary>
        LowestRating,
    }

    /// <summary>
    /// Singleton that manages the full lifecycle of marketplace reviews:
    /// submission, editing, deletion, helpful votes, and report forwarding.
    /// Enforces one review per user per listing and applies the profanity filter.
    ///
    /// <para>Persistence: <c>marketplace_reviews.json</c>.</para>
    /// </summary>
    public class ReviewManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Shared singleton instance.</summary>
        public static ReviewManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _reviewsPath = Path.Combine(Application.persistentDataPath, "marketplace_reviews.json");
            LoadReviews();
        }

        #endregion

        #region Events

        /// <summary>Raised when a review is successfully submitted. Argument is the new review.</summary>
        public event Action<MarketplaceReviewData> OnReviewSubmitted;

        /// <summary>Raised when a review is edited. Argument is the updated review.</summary>
        public event Action<MarketplaceReviewData> OnReviewEdited;

        #endregion

        #region Private State

        private readonly List<MarketplaceReviewData> _reviews = new List<MarketplaceReviewData>();
        private string _reviewsPath;

        private string LocalPlayerId => "local_player";

        #endregion

        #region Public API

        /// <summary>
        /// Submits a new review for a listing.
        /// Fails if the player has already reviewed this listing or if the comment contains profanity.
        /// </summary>
        /// <param name="listingId">Listing to review.</param>
        /// <param name="rating">Star rating [1–5].</param>
        /// <param name="comment">Optional text comment.</param>
        /// <returns>The created review, or <c>null</c> on failure.</returns>
        public MarketplaceReviewData SubmitReview(string listingId, int rating, string comment)
        {
            if (string.IsNullOrEmpty(listingId))
            {
                Debug.LogWarning("[SWEF] Marketplace: SubmitReview — listingId is empty.");
                return null;
            }

            rating = Mathf.Clamp(rating, 1, 5);

            if (HasReviewed(listingId, LocalPlayerId))
            {
                Debug.LogWarning($"[SWEF] Marketplace: SubmitReview — player already reviewed listing {listingId}.");
                return null;
            }

            // Profanity filter
#if SWEF_SECURITY_AVAILABLE
            if (!string.IsNullOrEmpty(comment) &&
                SWEF.Security.ProfanityFilter.ContainsProfanity(comment))
            {
                comment = SWEF.Security.ProfanityFilter.FilterProfanity(comment);
            }
#endif

            var review = new MarketplaceReviewData
            {
                listingId    = listingId,
                reviewerId   = LocalPlayerId,
                reviewerName = GetLocalDisplayName(),
                rating       = rating,
                comment      = comment ?? string.Empty,
            };

            _reviews.Add(review);
            SaveReviews();

            UpdateListingRating(listingId);

            OnReviewSubmitted?.Invoke(review);
            MarketplaceAnalytics.RecordReviewSubmitted(listingId, rating);
            MarketplaceBridge.OnReviewSubmitted(review);

            return review;
        }

        /// <summary>
        /// Edits the text/rating of an existing review owned by the local player.
        /// </summary>
        /// <param name="reviewId">ID of the review to edit.</param>
        /// <param name="newRating">Updated star rating [1–5].</param>
        /// <param name="newComment">Updated comment text.</param>
        /// <returns><c>true</c> if edited successfully.</returns>
        public bool EditReview(string reviewId, int newRating, string newComment)
        {
            var review = _reviews.Find(r => r.reviewId == reviewId && r.reviewerId == LocalPlayerId);
            if (review == null)
            {
                Debug.LogWarning($"[SWEF] Marketplace: EditReview — review {reviewId} not found or not owned.");
                return false;
            }

#if SWEF_SECURITY_AVAILABLE
            if (!string.IsNullOrEmpty(newComment) &&
                SWEF.Security.ProfanityFilter.ContainsProfanity(newComment))
            {
                newComment = SWEF.Security.ProfanityFilter.FilterProfanity(newComment);
            }
#endif

            review.rating   = Mathf.Clamp(newRating, 1, 5);
            review.comment  = newComment ?? string.Empty;
            review.isEdited = true;

            SaveReviews();
            UpdateListingRating(review.listingId);

            OnReviewEdited?.Invoke(review);
            return true;
        }

        /// <summary>Deletes a review owned by the local player.</summary>
        /// <param name="reviewId">ID of the review to delete.</param>
        /// <returns><c>true</c> if deleted successfully.</returns>
        public bool DeleteReview(string reviewId)
        {
            int idx = _reviews.FindIndex(r => r.reviewId == reviewId && r.reviewerId == LocalPlayerId);
            if (idx < 0)
            {
                Debug.LogWarning($"[SWEF] Marketplace: DeleteReview — review {reviewId} not found or not owned.");
                return false;
            }

            string listingId = _reviews[idx].listingId;
            _reviews.RemoveAt(idx);
            SaveReviews();
            UpdateListingRating(listingId);
            return true;
        }

        /// <summary>Returns a paginated, sorted list of reviews for the given listing.</summary>
        /// <param name="listingId">Listing to query reviews for.</param>
        /// <param name="page">Zero-based page index.</param>
        /// <param name="pageSize">Results per page.</param>
        /// <param name="sortBy">Sort order.</param>
        public List<MarketplaceReviewData> GetReviews(string listingId, int page = 0,
            int pageSize = 10, ReviewSortBy sortBy = ReviewSortBy.Newest)
        {
            IEnumerable<MarketplaceReviewData> results = _reviews.Where(r => r.listingId == listingId);

            results = sortBy switch
            {
                ReviewSortBy.MostHelpful   => results.OrderByDescending(r => r.helpfulCount),
                ReviewSortBy.HighestRating => results.OrderByDescending(r => r.rating),
                ReviewSortBy.LowestRating  => results.OrderBy(r => r.rating),
                _                          => results.OrderByDescending(r => r.createdAt),
            };

            return results.Skip(page * pageSize).Take(pageSize).ToList();
        }

        /// <summary>Increments the helpful-vote count for a review.</summary>
        /// <param name="reviewId">ID of the review to upvote.</param>
        public void MarkHelpful(string reviewId)
        {
            var review = _reviews.Find(r => r.reviewId == reviewId);
            if (review == null) return;
            review.helpfulCount++;
            SaveReviews();
        }

        /// <summary>Reports a review to the <see cref="ContentModerationController"/>.</summary>
        /// <param name="reviewId">ID of the review to report.</param>
        /// <param name="reason">Human-readable reason.</param>
        public void ReportReview(string reviewId, string reason)
        {
            ContentModerationController.Instance?.ReportListing(reviewId, reason);
        }

        #endregion

        #region Private Helpers

        private bool HasReviewed(string listingId, string playerId)
        {
            return _reviews.Any(r => r.listingId == listingId && r.reviewerId == playerId);
        }

        private void UpdateListingRating(string listingId)
        {
            var mgr = MarketplaceManager.Instance;
            if (mgr == null) return;

            var listing = mgr.GetListingById(listingId);
            if (listing == null) return;

            var listingReviews = _reviews.Where(r => r.listingId == listingId).ToList();
            listing.ratingCount   = listingReviews.Count;
            listing.ratingAverage = listingReviews.Count > 0
                ? (float)listingReviews.Average(r => r.rating)
                : 0f;
        }

        private static string GetLocalDisplayName()
        {
#if SWEF_MULTIPLAYER_AVAILABLE
            return SWEF.Multiplayer.PlayerProfileManager.Instance?.LocalProfile?.displayName
                   ?? "Player";
#else
            return "Player";
#endif
        }

        #endregion

        #region Persistence

        private void SaveReviews()
        {
            try
            {
                string json = JsonUtility.ToJson(new ReviewsWrapper { reviews = _reviews }, true);
                File.WriteAllText(_reviewsPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to save reviews — {ex.Message}");
            }
        }

        private void LoadReviews()
        {
            _reviews.Clear();
            if (!File.Exists(_reviewsPath)) return;
            try
            {
                string json = File.ReadAllText(_reviewsPath);
                var wrapper = JsonUtility.FromJson<ReviewsWrapper>(json);
                if (wrapper?.reviews != null)
                    _reviews.AddRange(wrapper.reviews);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to load reviews — {ex.Message}");
            }
        }

        [Serializable] private class ReviewsWrapper { public List<MarketplaceReviewData> reviews; }

        #endregion
    }
}
