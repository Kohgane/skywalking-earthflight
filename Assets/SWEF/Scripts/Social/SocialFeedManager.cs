using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SWEF.Core;

namespace SWEF.Social
{
    /// <summary>
    /// Singleton that manages the local social feed of <see cref="SocialPost"/> entries.
    /// Posts are stored as individual JSON files under
    /// <c>Application.persistentDataPath/SocialFeed/</c>.
    /// The feed is kept at most <see cref="MaxFeedSize"/> entries; oldest posts are
    /// pruned automatically when the limit is exceeded.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class SocialFeedManager : MonoBehaviour
    {
        // ── Constants ────────────────────────────────────────────────────────
        private const int    MaxFeedSize  = 200;
        private const string FeedFolder   = "SocialFeed";
        private const string PostFileSuffix = ".json";

        // ── Singleton ────────────────────────────────────────────────────────
        public static SocialFeedManager Instance { get; private set; }

        // ── Feed data ────────────────────────────────────────────────────────
        /// <summary>Cached feed; newest post first.</summary>
        public List<SocialPost> LocalFeed { get; private set; } = new List<SocialPost>();

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Fired when a new post is created.</summary>
        public event Action<SocialPost> OnPostCreated;
        /// <summary>Fired when a post is deleted, passing the deleted post ID.</summary>
        public event Action<string>     OnPostDeleted;
        /// <summary>Fired when the feed is refreshed (reloaded from disk).</summary>
        public event Action             OnFeedRefreshed;

        // ── Unity ────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureFeedDirectory();
            RefreshFeed();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new <see cref="SocialPost"/> from the current
        /// <see cref="SWEFSession"/> location, saves it to disk, and fires
        /// <see cref="OnPostCreated"/>.
        /// Callers should verify <see cref="SWEFSession.HasFix"/> before calling
        /// this if GPS coordinates are required; the post is still created when
        /// <c>HasFix</c> is false but lat/lon will be zero.
        /// </summary>
        public SocialPost CreatePost(string imagePath, string caption)
        {
            var post = new SocialPost
            {
                postId           = Guid.NewGuid().ToString(),
                authorName       = CommunityProfileManager.Instance != null
                                       ? CommunityProfileManager.Instance.GetDisplayName()
                                       : "Pilot",
                imagePath        = imagePath,
                thumbnailPath    = imagePath, // same file; caller may override with a resized copy
                caption          = caption,
                latitude         = SWEFSession.Lat,
                longitude        = SWEFSession.Lon,
                altitude         = (float)SWEFSession.Alt,
                timestamp        = DateTime.UtcNow.ToString("O"),
                likeCount        = 0,
                isLikedByMe      = false,
                weatherCondition = string.Empty,
                tags             = new System.Collections.Generic.List<string>()
            };

            SavePostToDisk(post);

            // Insert newest-first and prune if needed
            LocalFeed.Insert(0, post);
            PruneOldestPosts();

            OnPostCreated?.Invoke(post);
            return post;
        }

        /// <summary>Removes a post from the feed and deletes its JSON file.</summary>
        public void DeletePost(string postId)
        {
            int idx = LocalFeed.FindIndex(p => p.postId == postId);
            if (idx < 0) return;

            LocalFeed.RemoveAt(idx);
            DeletePostFromDisk(postId);
            OnPostDeleted?.Invoke(postId);
        }

        /// <summary>
        /// Reloads all posts from the <c>SocialFeed/</c> directory and rebuilds
        /// <see cref="LocalFeed"/>, then fires <see cref="OnFeedRefreshed"/>.
        /// </summary>
        public void RefreshFeed()
        {
            LocalFeed.Clear();
            string dir = GetFeedDirectory();

            if (!Directory.Exists(dir))
            {
                OnFeedRefreshed?.Invoke();
                return;
            }

            string[] files = Directory.GetFiles(dir, "*" + PostFileSuffix);
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    SocialPost post = SocialPost.FromJson(json);
                    if (post != null && !string.IsNullOrEmpty(post.postId))
                        LocalFeed.Add(post);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF] SocialFeedManager: failed to load post from {file}: {ex.Message}");
                }
            }

            // Sort newest-first by timestamp
            LocalFeed.Sort((a, b) =>
            {
                bool aOk = DateTime.TryParse(a.timestamp, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime aTime);
                bool bOk = DateTime.TryParse(b.timestamp, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime bTime);
                if (!aOk && !bOk) return 0;
                if (!aOk) return 1;
                if (!bOk) return -1;
                return bTime.CompareTo(aTime);
            });

            PruneOldestPosts();
            OnFeedRefreshed?.Invoke();
        }

        /// <summary>
        /// Toggles the like state on the specified post, persisting the change to disk.
        /// </summary>
        public void ToggleLike(string postId)
        {
            SocialPost post = LocalFeed.Find(p => p.postId == postId);
            if (post == null) return;

            if (post.isLikedByMe)
            {
                post.isLikedByMe = false;
                post.likeCount   = Mathf.Max(0, post.likeCount - 1);
            }
            else
            {
                post.isLikedByMe = true;
                post.likeCount++;
            }

            SavePostToDisk(post);
        }

        /// <summary>Returns all posts authored by <paramref name="authorName"/>.</summary>
        public List<SocialPost> GetPostsByAuthor(string authorName)
        {
            return LocalFeed.FindAll(p =>
                string.Equals(p.authorName, authorName, StringComparison.OrdinalIgnoreCase));
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private string GetFeedDirectory() =>
            Path.Combine(Application.persistentDataPath, FeedFolder);

        private void EnsureFeedDirectory() =>
            Directory.CreateDirectory(GetFeedDirectory());

        private string GetPostFilePath(string postId) =>
            Path.Combine(GetFeedDirectory(), postId + PostFileSuffix);

        private void SavePostToDisk(SocialPost post)
        {
            try
            {
                EnsureFeedDirectory();
                File.WriteAllText(GetPostFilePath(post.postId), post.ToJson());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] SocialFeedManager: failed to save post {post.postId}: {ex.Message}");
            }
        }

        private void DeletePostFromDisk(string postId)
        {
            string path = GetPostFilePath(postId);
            if (File.Exists(path))
            {
                try { File.Delete(path); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF] SocialFeedManager: failed to delete {path}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Removes the oldest posts (end of list) when <see cref="MaxFeedSize"/> is exceeded.
        /// Deleted posts are also removed from disk.
        /// </summary>
        private void PruneOldestPosts()
        {
            while (LocalFeed.Count > MaxFeedSize)
            {
                SocialPost oldest = LocalFeed[LocalFeed.Count - 1];
                LocalFeed.RemoveAt(LocalFeed.Count - 1);
                DeletePostFromDisk(oldest.postId);
            }
        }
    }
}
