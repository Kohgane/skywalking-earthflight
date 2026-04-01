using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Singleton MonoBehaviour that acts as the social hub for shared replays.
    /// Maintains an in-memory gallery, featured replays, view/like counts, comments,
    /// and bookmarks.
    /// </summary>
    public class ReplaySharingHub : MonoBehaviour
    {
        #region Singleton

        private static ReplaySharingHub _instance;

        /// <summary>Global singleton instance.</summary>
        public static ReplaySharingHub Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<ReplaySharingHub>();
                return _instance;
            }
        }

        #endregion

        #region Inspector

        [Header("Gallery Settings")]
        [SerializeField] private int maxFeaturedReplays = 10;
        [SerializeField] private int maxGalleryItems    = 100;

        #endregion

        #region State

        private List<ShareResult>                      _gallery         = new List<ShareResult>();
        private Dictionary<string, int>                _viewCounts      = new Dictionary<string, int>();
        private Dictionary<string, int>                _likeCounts      = new Dictionary<string, int>();
        private Dictionary<string, List<string>>       _comments        = new Dictionary<string, List<string>>();
        private List<string>                           _bookmarkedIds   = new List<string>();
        private List<ShareResult>                      _featuredReplays = new List<ShareResult>();

        #endregion

        #region Events

        /// <summary>Fired when a replay is successfully shared.  Parameter is the share result.</summary>
        public event Action<ShareResult> OnReplayShared;

        /// <summary>Fired when a replay is liked.  Parameter is the share identifier.</summary>
        public event Action<string> OnReplayLiked;

        /// <summary>Fired when a replay is bookmarked.  Parameter is the share identifier.</summary>
        public event Action<string> OnReplayBookmarked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Shares a project to the specified platform with the given privacy setting,
        /// adds the result to the gallery, and fires <see cref="OnReplayShared"/>.
        /// </summary>
        /// <param name="project">Project to share.</param>
        /// <param name="platform">Target sharing platform.</param>
        /// <param name="privacy">Audience visibility setting.</param>
        /// <returns>A <see cref="ShareResult"/> describing the share.</returns>
        public ShareResult ShareReplay(ReplayProject project, SharingPlatform platform, PrivacySetting privacy)
        {
            if (project == null) return null;

            var result = new ShareResult
            {
                url        = $"https://swef.game/replay/{project.projectId}",
                platform   = platform,
                sharedAt   = DateTime.UtcNow,
                expiresAt  = DateTime.UtcNow.AddDays(30),
                shareCode  = GenerateShareCode(project.projectId),
                isPublic   = privacy == PrivacySetting.Public
            };

            if (_gallery.Count >= maxGalleryItems)
                _gallery.RemoveAt(0);

            _gallery.Add(result);
            _viewCounts[result.shareCode]  = 0;
            _likeCounts[result.shareCode]  = 0;
            _comments[result.shareCode]    = new List<string>();

            // Add to featured if there is room
            if (result.isPublic && _featuredReplays.Count < maxFeaturedReplays)
                _featuredReplays.Add(result);

            Debug.Log($"[SWEF] ReplaySharingHub: Replay '{project.title}' shared ({platform}, code: {result.shareCode}).");
            OnReplayShared?.Invoke(result);
            return result;
        }

        /// <summary>Increments the like count for the given share identifier.</summary>
        /// <param name="shareId">Share code or identifier to like.</param>
        public void LikeReplay(string shareId)
        {
            if (!_likeCounts.ContainsKey(shareId)) _likeCounts[shareId] = 0;
            _likeCounts[shareId]++;
            Debug.Log($"[SWEF] ReplaySharingHub: Replay '{shareId}' liked ({_likeCounts[shareId]} total).");
            OnReplayLiked?.Invoke(shareId);
        }

        /// <summary>Appends a comment to the specified share.</summary>
        /// <param name="shareId">Share code or identifier.</param>
        /// <param name="comment">Comment text.</param>
        public void AddComment(string shareId, string comment)
        {
            if (!_comments.ContainsKey(shareId))
                _comments[shareId] = new List<string>();

            _comments[shareId].Add(comment);
            Debug.Log($"[SWEF] ReplaySharingHub: Comment added to '{shareId}'.");
        }

        /// <summary>Bookmarks the specified share for the local player.</summary>
        /// <param name="shareId">Share code or identifier to bookmark.</param>
        public void BookmarkReplay(string shareId)
        {
            if (!_bookmarkedIds.Contains(shareId))
            {
                _bookmarkedIds.Add(shareId);
                Debug.Log($"[SWEF] ReplaySharingHub: Replay '{shareId}' bookmarked.");
                OnReplayBookmarked?.Invoke(shareId);
            }
        }

        /// <summary>Removes the bookmark for the specified share.</summary>
        /// <param name="shareId">Share code or identifier to un-bookmark.</param>
        public void RemoveBookmark(string shareId)
        {
            if (_bookmarkedIds.Remove(shareId))
                Debug.Log($"[SWEF] ReplaySharingHub: Bookmark removed for '{shareId}'.");
        }

        /// <summary>Returns the view count for the given share identifier.</summary>
        /// <param name="shareId">Share code or identifier.</param>
        /// <returns>Number of views recorded.</returns>
        public int GetViewCount(string shareId)
        {
            return _viewCounts.TryGetValue(shareId, out int v) ? v : 0;
        }

        /// <summary>Returns the like count for the given share identifier.</summary>
        /// <param name="shareId">Share code or identifier.</param>
        /// <returns>Number of likes recorded.</returns>
        public int GetLikeCount(string shareId)
        {
            return _likeCounts.TryGetValue(shareId, out int v) ? v : 0;
        }

        /// <summary>Returns the comment list for the given share as a read-only view.</summary>
        /// <param name="shareId">Share code or identifier.</param>
        /// <returns>Read-only list of comments, or an empty list.</returns>
        public IReadOnlyList<string> GetComments(string shareId)
        {
            return _comments.TryGetValue(shareId, out var list) ? list : new List<string>();
        }

        /// <summary>Returns all items in the gallery as a read-only view.</summary>
        /// <returns>Read-only list of <see cref="ShareResult"/>.</returns>
        public IReadOnlyList<ShareResult> GetGallery()
        {
            return _gallery;
        }

        /// <summary>Returns all featured replays as a read-only view.</summary>
        /// <returns>Read-only list of featured <see cref="ShareResult"/>.</returns>
        public IReadOnlyList<ShareResult> GetFeaturedReplays()
        {
            return _featuredReplays;
        }

        /// <summary>
        /// Updates the privacy setting of the share identified by <paramref name="shareId"/>.
        /// </summary>
        /// <param name="shareId">Share code or identifier.</param>
        /// <param name="privacy">New privacy setting.</param>
        public void SetPrivacy(string shareId, PrivacySetting privacy)
        {
            var result = _gallery.Find(r => r.shareCode == shareId);
            if (result == null) return;

            result.isPublic = privacy == PrivacySetting.Public;
            Debug.Log($"[SWEF] ReplaySharingHub: Privacy for '{shareId}' → {privacy}.");
        }

        /// <summary>Increments the view counter for the specified share.</summary>
        /// <param name="shareId">Share code or identifier to record a view for.</param>
        public void IncrementViewCount(string shareId)
        {
            if (!_viewCounts.ContainsKey(shareId)) _viewCounts[shareId] = 0;
            _viewCounts[shareId]++;
        }

        #endregion

        #region Internals

        private static string GenerateShareCode(string seed)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rng  = new System.Random(seed.GetHashCode() ^ Environment.TickCount);
            var code = new System.Text.StringBuilder(8);
            for (int i = 0; i < 8; i++)
                code.Append(chars[rng.Next(chars.Length)]);
            return code.ToString();
        }

        #endregion
    }
}
