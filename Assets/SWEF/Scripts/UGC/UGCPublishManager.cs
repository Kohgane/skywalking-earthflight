// UGCPublishManager.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — Singleton MonoBehaviour that manages the full content publishing
    /// lifecycle: submit for review, publish/unpublish, version management, download,
    /// install, and the local content library.
    ///
    /// <para>Persists state to <c>ugc_library.json</c> and <c>ugc_published.json</c>
    /// inside <c>Application.persistentDataPath</c>.</para>
    ///
    /// <para>Attach to a persistent scene object — uses DontDestroyOnLoad.</para>
    /// </summary>
    public sealed class UGCPublishManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static UGCPublishManager Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when content is successfully published. Argument is the published content.</summary>
        public event Action<UGCContent> OnContentPublished;

        /// <summary>Raised when content is downloaded from the community. Argument is the content record.</summary>
        public event Action<UGCContent> OnContentDownloaded;

        /// <summary>Raised when downloaded content is installed to the local library.</summary>
        public event Action<UGCContent> OnContentInstalled;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>Returns a read-only view of the locally installed content library.</summary>
        public IReadOnlyList<UGCContent> InstalledContent => _installedContent;

        /// <summary>Returns a read-only view of content published by this player.</summary>
        public IReadOnlyList<UGCContent> PublishedContent => _publishedContent;

        // ── Internal state ─────────────────────────────────────────────────────

        private readonly List<UGCContent> _installedContent = new List<UGCContent>();
        private readonly List<UGCContent> _publishedContent = new List<UGCContent>();

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
            LoadLibrary();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API — publishing ────────────────────────────────────────────

        /// <summary>
        /// Submits <paramref name="content"/> for community review.
        /// Validation must pass (no blocking errors) before submission.
        /// </summary>
        /// <returns><c>true</c> if the submission was accepted; <c>false</c> otherwise.</returns>
        public bool SubmitForReview(UGCContent content)
        {
            if (content == null)
            {
                Debug.LogWarning("[UGCPublishManager] SubmitForReview — content is null.");
                return false;
            }

            var validation = UGCValidator.ValidateContent(content);
            if (!validation.IsPublishable)
            {
                Debug.LogWarning("[UGCPublishManager] SubmitForReview — content has blocking errors.");
                return false;
            }

            if (!content.metadata.hasBeenTested)
            {
                Debug.LogWarning("[UGCPublishManager] SubmitForReview — content has not been test-played.");
                return false;
            }

            content.status    = UGCStatus.UnderReview;
            content.updatedAt = DateTime.UtcNow.ToString("o");

            Debug.Log($"[UGCPublishManager] '{content.title}' submitted for review.");
            return true;
        }

        /// <summary>
        /// Publishes <paramref name="content"/> immediately (used for auto-publish or moderation approval).
        /// </summary>
        public void PublishContent(UGCContent content)
        {
            if (content == null) return;

            content.status    = UGCStatus.Published;
            content.updatedAt = DateTime.UtcNow.ToString("o");

            if (!_publishedContent.Contains(content))
                _publishedContent.Add(content);

            SavePublished();
            OnContentPublished?.Invoke(content);
            Debug.Log($"[UGCPublishManager] '{content.title}' published (v{content.version}).");
        }

        /// <summary>
        /// Unpublishes <paramref name="content"/>, setting its status to Archived.
        /// </summary>
        public void UnpublishContent(UGCContent content)
        {
            if (content == null) return;
            content.status    = UGCStatus.Archived;
            content.updatedAt = DateTime.UtcNow.ToString("o");
            SavePublished();
            Debug.Log($"[UGCPublishManager] '{content.title}' unpublished.");
        }

        /// <summary>
        /// Increments the version number and re-publishes updated content.
        /// </summary>
        public void UpdatePublishedContent(UGCContent content)
        {
            if (content == null) return;
            content.version++;
            PublishContent(content);
        }

        // ── Public API — library ───────────────────────────────────────────────

        /// <summary>
        /// Downloads a content record and installs it to the local library.
        /// In a production implementation this would fetch from a remote server.
        /// </summary>
        public void DownloadAndInstall(UGCContent content)
        {
            if (content == null) return;
            content.downloadCount++;
            OnContentDownloaded?.Invoke(content);

            InstallContent(content);
        }

        /// <summary>
        /// Installs <paramref name="content"/> to the local library without downloading.
        /// </summary>
        public void InstallContent(UGCContent content)
        {
            if (content == null) return;
            if (_installedContent.Exists(c => c.contentId == content.contentId))
            {
                // Update existing
                int idx = _installedContent.FindIndex(c => c.contentId == content.contentId);
                _installedContent[idx] = content;
            }
            else
            {
                _installedContent.Add(content);
            }

            SaveLibrary();
            OnContentInstalled?.Invoke(content);
            Debug.Log($"[UGCPublishManager] '{content.title}' installed.");
        }

        /// <summary>
        /// Removes the content with the given ID from the local library.
        /// </summary>
        public void UninstallContent(string contentId)
        {
            _installedContent.RemoveAll(c => c.contentId == contentId);
            SaveLibrary();
            Debug.Log($"[UGCPublishManager] Content '{contentId}' uninstalled.");
        }

        // ── Persistence ────────────────────────────────────────────────────────

        private void SaveLibrary()
        {
            var wrapper = new ContentListWrapper { items = _installedContent };
            WriteJson(UGCConfig.LibraryFileName, JsonUtility.ToJson(wrapper, true));
        }

        private void SavePublished()
        {
            var wrapper = new ContentListWrapper { items = _publishedContent };
            WriteJson(UGCConfig.PublishedFileName, JsonUtility.ToJson(wrapper, true));
        }

        private void LoadLibrary()
        {
            var lib = ReadJson<ContentListWrapper>(UGCConfig.LibraryFileName);
            if (lib?.items != null) _installedContent.AddRange(lib.items);

            var pub = ReadJson<ContentListWrapper>(UGCConfig.PublishedFileName);
            if (pub?.items != null) _publishedContent.AddRange(pub.items);
        }

        private void WriteJson(string fileName, string json)
        {
            try
            {
                File.WriteAllText(Path.Combine(Application.persistentDataPath, fileName), json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGCPublishManager] Write '{fileName}' failed: {ex.Message}");
            }
        }

        private T ReadJson<T>(string fileName) where T : class
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);
            if (!File.Exists(path)) return null;
            try
            {
                return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGCPublishManager] Read '{fileName}' failed: {ex.Message}");
                return null;
            }
        }

        // ── JSON wrapper ───────────────────────────────────────────────────────

        [Serializable]
        private sealed class ContentListWrapper
        {
            public List<UGCContent> items = new List<UGCContent>();
        }
    }
}
