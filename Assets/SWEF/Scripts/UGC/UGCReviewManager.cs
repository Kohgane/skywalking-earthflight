// UGCReviewManager.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — Singleton MonoBehaviour that manages community reviews.
    ///
    /// <para>Enforces one review per player per content, provides helpful/unhelpful
    /// voting, aggregates ratings, and persists reviews to <c>ugc_reviews.json</c>.</para>
    ///
    /// <para>Uses <c>SWEF.Security.ProfanityFilter</c> on comments when
    /// <c>SWEF_SECURITY_AVAILABLE</c> is defined.</para>
    ///
    /// <para>Attach to a persistent scene object — uses DontDestroyOnLoad.</para>
    /// </summary>
    public sealed class UGCReviewManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static UGCReviewManager Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a new review is submitted. Argument is the new review.</summary>
        public event Action<UGCReview> OnReviewSubmitted;

        /// <summary>Raised when a review's helpful count changes. Argument is the updated review.</summary>
        public event Action<UGCReview> OnReviewHelpfulVoted;

        // ── Internal state ─────────────────────────────────────────────────────

        private readonly List<UGCReview> _reviews = new List<UGCReview>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadReviews();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Submits a review for the given content.
        /// Returns <c>null</c> and logs a warning if the player has already reviewed this content,
        /// or if the comment contains profanity.
        /// </summary>
        public UGCReview SubmitReview(string contentId, string reviewerId, UGCRating rating, string comment = "")
        {
            // One review per player per content
            if (_reviews.Exists(r => r.contentId == contentId && r.reviewerId == reviewerId))
            {
                Debug.LogWarning($"[UGCReviewManager] Player '{reviewerId}' has already reviewed '{contentId}'.");
                return null;
            }

            // Profanity check on comment
            if (!string.IsNullOrEmpty(comment))
            {
#if SWEF_SECURITY_AVAILABLE
                if (Security.ProfanityFilter.ContainsProfanity(comment))
                {
                    Debug.LogWarning("[UGCReviewManager] Review comment contains prohibited language.");
                    return null;
                }
#endif
            }

            var review = UGCReview.Create(contentId, reviewerId, rating, comment);
            _reviews.Add(review);
            SaveReviews();
            OnReviewSubmitted?.Invoke(review);
            Debug.Log($"[UGCReviewManager] Review submitted for '{contentId}' by '{reviewerId}'.");
            return review;
        }

        /// <summary>
        /// Returns all reviews for the given content ID.
        /// </summary>
        public List<UGCReview> GetReviewsForContent(string contentId)
        {
            return _reviews.FindAll(r => r.contentId == contentId);
        }

        /// <summary>
        /// Computes the average star rating (as a float 1–5) for the given content.
        /// Returns 0 if there are no reviews.
        /// </summary>
        public float GetAverageRating(string contentId)
        {
            var contentReviews = GetReviewsForContent(contentId);
            if (contentReviews.Count == 0) return 0f;

            float total = 0f;
            foreach (var r in contentReviews)
                total += (int)r.rating;
            return total / contentReviews.Count;
        }

        /// <summary>
        /// Returns the number of reviews for the given content.
        /// </summary>
        public int GetReviewCount(string contentId) => _reviews.FindAll(r => r.contentId == contentId).Count;

        /// <summary>
        /// Marks the given review as helpful, incrementing its <see cref="UGCReview.helpfulCount"/>.
        /// </summary>
        public void VoteHelpful(string reviewId)
        {
            var review = _reviews.Find(r => r.reviewId == reviewId);
            if (review == null) return;
            review.helpfulCount++;
            SaveReviews();
            OnReviewHelpfulVoted?.Invoke(review);
        }

        // ── Persistence ────────────────────────────────────────────────────────

        private void SaveReviews()
        {
            var wrapper = new ReviewListWrapper { items = _reviews };
            string json = JsonUtility.ToJson(wrapper, true);
            string path = Path.Combine(Application.persistentDataPath, UGCConfig.ReviewsFileName);
            try
            {
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGCReviewManager] Save failed: {ex.Message}");
            }
        }

        private void LoadReviews()
        {
            string path = Path.Combine(Application.persistentDataPath, UGCConfig.ReviewsFileName);
            if (!File.Exists(path)) return;
            try
            {
                var wrapper = JsonUtility.FromJson<ReviewListWrapper>(File.ReadAllText(path));
                if (wrapper?.items != null) _reviews.AddRange(wrapper.items);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGCReviewManager] Load failed: {ex.Message}");
            }
        }

        // ── JSON wrapper ───────────────────────────────────────────────────────

        [Serializable]
        private sealed class ReviewListWrapper
        {
            public List<UGCReview> items = new List<UGCReview>();
        }
    }
}
