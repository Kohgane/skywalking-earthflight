// UGCShareManager.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;
using System.IO;
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — Handles export, import, and sharing of UGC content packages.
    ///
    /// <para>Exports content as <c>.swefugc</c> JSON files, imports them back, generates
    /// shareable codes, copies links to the clipboard, and handles the deep-link
    /// scheme <c>swef://ugc?id=xxx</c>.</para>
    ///
    /// <para>Follows the same pattern as <c>SWEF.Multiplayer.AircraftShareManager</c>.</para>
    /// </summary>
    public sealed class UGCShareManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static UGCShareManager Instance { get; private set; }

        // ── Constants ──────────────────────────────────────────────────────────

        private const string DeepLinkScheme = "swef://ugc?id=";

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when content is exported. Argument is the export file path.</summary>
        public event Action<string> OnContentExported;

        /// <summary>Raised when content is imported. Argument is the imported content record.</summary>
        public event Action<UGCContent> OnContentImported;

        /// <summary>Raised when a share link is copied to the clipboard. Argument is the link string.</summary>
        public event Action<string> OnLinkCopied;

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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Exports <paramref name="content"/> to a <c>.swefugc</c> file in the specified directory.
        /// </summary>
        /// <param name="content">Content to export.</param>
        /// <param name="exportDirectory">Directory to write the file into. Defaults to <c>Application.persistentDataPath</c>.</param>
        /// <returns>The file path of the exported file, or <c>null</c> on failure.</returns>
        public string ExportContent(UGCContent content, string exportDirectory = null)
        {
            if (content == null)
            {
                Debug.LogWarning("[UGCShareManager] ExportContent — content is null.");
                return null;
            }

            exportDirectory ??= Application.persistentDataPath;

            string safeTitle = SanitiseFileName(content.title);
            string fileName  = $"{safeTitle}_{content.contentId.Substring(0, Mathf.Min(8, content.contentId.Length))}{UGCConfig.ExportExtension}";
            string filePath  = Path.Combine(exportDirectory, fileName);

            try
            {
                string json = JsonUtility.ToJson(content, prettyPrint: true);
                File.WriteAllText(filePath, json);
                OnContentExported?.Invoke(filePath);
                Debug.Log($"[UGCShareManager] Exported to: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGCShareManager] ExportContent failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Imports a <c>.swefugc</c> file from the given path and installs it to the local library.
        /// </summary>
        /// <param name="filePath">Absolute path to the <c>.swefugc</c> file.</param>
        /// <returns>The imported <see cref="UGCContent"/>, or <c>null</c> on failure.</returns>
        public UGCContent ImportContent(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[UGCShareManager] ImportContent — file not found: {filePath}");
                return null;
            }

            try
            {
                string json    = File.ReadAllText(filePath);
                var    content = JsonUtility.FromJson<UGCContent>(json);

                if (content == null || string.IsNullOrEmpty(content.contentId))
                {
                    Debug.LogWarning("[UGCShareManager] ImportContent — invalid content file.");
                    return null;
                }

                UGCPublishManager.Instance?.InstallContent(content);
                OnContentImported?.Invoke(content);
                Debug.Log($"[UGCShareManager] Imported: {content.title}");
                return content;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGCShareManager] ImportContent failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates a deep-link URL for the given content ID.
        /// Format: <c>swef://ugc?id=&lt;contentId&gt;</c>
        /// </summary>
        public string GenerateDeepLink(string contentId)
        {
            return DeepLinkScheme + contentId;
        }

        /// <summary>
        /// Copies the deep-link for <paramref name="contentId"/> to the system clipboard.
        /// </summary>
        public void CopyLinkToClipboard(string contentId)
        {
            string link = GenerateDeepLink(contentId);
            GUIUtility.systemCopyBuffer = link;
            OnLinkCopied?.Invoke(link);
            Debug.Log($"[UGCShareManager] Link copied: {link}");
        }

        /// <summary>
        /// Handles an incoming deep-link URI.  Extracts the content ID and triggers
        /// a download/install if <see cref="UGCPublishManager"/> is available.
        /// </summary>
        /// <param name="uri">Full deep-link URI, e.g. <c>swef://ugc?id=abc123</c>.</param>
        public void HandleDeepLink(string uri)
        {
            if (string.IsNullOrEmpty(uri) || !uri.StartsWith(DeepLinkScheme)) return;

            string contentId = uri.Substring(DeepLinkScheme.Length);
            Debug.Log($"[UGCShareManager] Deep link received for content ID: {contentId}");

            // In a production build this would query the server for the content record.
#if SWEF_MULTIPLAYER_AVAILABLE
            // Example: fetch from network and install
            Debug.Log("[UGCShareManager] (SWEF_MULTIPLAYER_AVAILABLE) Would fetch content from server.");
#endif
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static string SanitiseFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Length > 40 ? name.Substring(0, 40) : name;
        }
    }
}
