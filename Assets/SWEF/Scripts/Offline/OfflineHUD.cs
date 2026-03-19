using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Offline
{
    /// <summary>
    /// HUD widget that displays offline status and cache usage information.
    ///
    /// <para>Shows a compact offline indicator icon and label when disconnected;
    /// an optional expandable panel reveals connection type, cache usage bar,
    /// cached region count, and time since last online.</para>
    ///
    /// <para>Position: top-right corner of the HUD canvas (set in the inspector).
    /// Visibility transitions use a 0.3-second alpha fade.</para>
    /// </summary>
    public class OfflineHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Core References")]
        [Tooltip("Root CanvasGroup that controls overall visibility.")]
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Tooltip("Icon shown when offline.")]
        [SerializeField] private Image offlineIcon;

        [Tooltip("Label showing 'Offline' or connection status text.")]
        [SerializeField] private TextMeshProUGUI statusLabel;

        [Header("Expanded Panel (optional)")]
        [Tooltip("CanvasGroup of the expandable detail panel.")]
        [SerializeField] private CanvasGroup detailPanel;

        [Tooltip("Label showing cache used / max (e.g. '456 MB / 2 GB').")]
        [SerializeField] private TextMeshProUGUI cacheUsageLabel;

        [Tooltip("Progress bar for cache usage.")]
        [SerializeField] private Slider cacheBar;

        [Tooltip("Label showing number of cached regions.")]
        [SerializeField] private TextMeshProUGUI regionCountLabel;

        [Tooltip("Label showing time since last online (e.g. '5m 23s').")]
        [SerializeField] private TextMeshProUGUI timeSinceOnlineLabel;

        [Header("Animation")]
        [Tooltip("Duration (seconds) of the alpha fade in/out transition.")]
        [SerializeField] private float fadeDuration = 0.3f;

        // ── Internal ──────────────────────────────────────────────────────────────
        private bool _visible;
        private Coroutine _fadeCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            // Start hidden
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha          = 0f;
                rootCanvasGroup.interactable   = false;
                rootCanvasGroup.blocksRaycasts = false;
            }
        }

        private void Start()
        {
            if (OfflineManager.Instance != null && OfflineManager.Instance.IsOffline)
                ShowOfflineIndicator();
        }

        private void Update()
        {
            if (_visible && OfflineManager.Instance != null && OfflineManager.Instance.IsOffline)
                RefreshCacheStats();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fades the offline indicator in and refreshes displayed data.
        /// </summary>
        public void ShowOfflineIndicator()
        {
            _visible = true;
            RefreshStatus();
            RefreshCacheStats();
            FadeTo(1f);
        }

        /// <summary>
        /// Fades the offline indicator out.
        /// </summary>
        public void HideOfflineIndicator()
        {
            _visible = false;
            FadeTo(0f);
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void RefreshStatus()
        {
            if (statusLabel == null || OfflineManager.Instance == null) return;

            string connText = OfflineManager.Instance.IsForcedOffline
                ? "Offline (forced)"
                : "Offline";
            statusLabel.text = connText;
        }

        private void RefreshCacheStats()
        {
            if (OfflineManager.Instance != null)
            {
                // Time since offline
                if (timeSinceOnlineLabel != null && OfflineManager.Instance.OfflineSince.HasValue)
                {
                    double secs = (System.DateTime.UtcNow - OfflineManager.Instance.OfflineSince.Value)
                                  .TotalSeconds;
                    timeSinceOnlineLabel.text = FormatDuration(secs);
                }

                // Connection type
                if (statusLabel != null)
                {
                    string connStr = OfflineManager.Instance.IsForcedOffline
                        ? "Offline (forced)"
                        : $"Offline — {OfflineManager.Instance.CurrentConnection}";
                    statusLabel.text = connStr;
                }
            }

            if (TileCacheManager.Instance == null) return;

            long used = TileCacheManager.Instance.GetTotalCacheSizeBytes();
            long max  = TileCacheManager.Instance.GetMaxCacheSizeBytes();
            int  regionCount = TileCacheManager.Instance.GetCachedRegions().Count;

            if (cacheUsageLabel != null)
                cacheUsageLabel.text = $"{FormatBytes(used)} / {FormatBytes(max)}";

            if (cacheBar != null)
                cacheBar.value = max > 0 ? (float)((double)used / max) : 0f;

            if (regionCountLabel != null)
                regionCountLabel.text = $"Regions: {regionCount}";
        }

        private void FadeTo(float target)
        {
            if (rootCanvasGroup == null) return;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCoroutine(target));
        }

        private IEnumerator FadeCoroutine(float target)
        {
            if (this == null || !gameObject.activeInHierarchy) yield break;

            float start    = rootCanvasGroup.alpha;
            float elapsed  = 0f;

            // Enable interactions immediately when fading in
            if (target > 0f)
            {
                rootCanvasGroup.interactable   = true;
                rootCanvasGroup.blocksRaycasts = true;
            }

            while (elapsed < fadeDuration)
            {
                if (this == null || !gameObject.activeInHierarchy) yield break;
                elapsed += Time.deltaTime;
                rootCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                yield return null;
            }

            rootCanvasGroup.alpha = target;

            if (target <= 0f)
            {
                rootCanvasGroup.interactable   = false;
                rootCanvasGroup.blocksRaycasts = false;
            }
        }

        // ── Formatting helpers ────────────────────────────────────────────────────

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
            if (bytes >= 1024L * 1024)
                return $"{bytes / (1024.0 * 1024):F0} MB";
            if (bytes >= 1024L)
                return $"{bytes / 1024.0:F0} KB";
            return $"{bytes} B";
        }

        private static string FormatDuration(double totalSecs)
        {
            int h = (int)(totalSecs / 3600);
            int m = (int)(totalSecs % 3600 / 60);
            int s = (int)(totalSecs % 60);
            if (h > 0) return $"{h}h {m}m";
            if (m > 0) return $"{m}m {s}s";
            return $"{s}s";
        }
    }
}
