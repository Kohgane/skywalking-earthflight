using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Offline
{
    /// <summary>
    /// Predefined region entry for the <see cref="RegionDownloadUI"/> list.
    /// </summary>
    [System.Serializable]
    public class PredefinedRegion
    {
        /// <summary>Display name for the region.</summary>
        public string name;
        /// <summary>Estimated download size description (e.g. "~320 MB").</summary>
        public string estimatedSize;
        /// <summary>Centre latitude in degrees.</summary>
        public double lat;
        /// <summary>Centre longitude in degrees.</summary>
        public double lon;
        /// <summary>Cache radius in kilometres.</summary>
        public float radiusKm;
        /// <summary>Altitude range in metres.</summary>
        public float altRangeM;
    }

    /// <summary>
    /// UI panel for manually downloading tile regions for offline use.
    ///
    /// <para>Accessible from the Settings panel.  Lists predefined popular
    /// regions plus a "Cache Current Location" button.  Each row shows the
    /// region name, estimated size, a download progress bar, and Download /
    /// Delete buttons.  Uses a <see cref="ScrollRect"/> for the list.</para>
    /// </summary>
    public class RegionDownloadUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Layout")]
        [Tooltip("Parent transform inside the ScrollRect that holds region rows.")]
        [SerializeField] private Transform regionListParent;

        [Tooltip("Prefab for a single region row.  Must contain the expected child names " +
                 "(see RegionRowBinding).")]
        [SerializeField] private GameObject regionRowPrefab;

        [Header("Footer")]
        [Tooltip("Label showing total cache usage.")]
        [SerializeField] private TextMeshProUGUI totalCacheLabel;

        [Tooltip("Label for the 'Cache Current Location' button.")]
        [SerializeField] private TextMeshProUGUI cacheCurrentLocationLabel;

        [Tooltip("Button that caches the player's current GPS location.")]
        [SerializeField] private Button cacheCurrentLocationButton;

        [Header("Current Location Cache")]
        [Tooltip("Radius (km) to cache around the current GPS position.")]
        [SerializeField] private float currentLocationRadiusKm = 5f;

        [Tooltip("Altitude range (m) to include when caching the current location.")]
        [SerializeField] private float currentLocationAltRangeM = 3000f;

        [Header("Predefined Regions")]
        [SerializeField] private List<PredefinedRegion> predefinedRegions = new List<PredefinedRegion>
        {
            new PredefinedRegion { name="New York City",  estimatedSize="~280 MB", lat=40.7128, lon=-74.0060, radiusKm=8f,  altRangeM=3000f },
            new PredefinedRegion { name="Tokyo",          estimatedSize="~310 MB", lat=35.6762, lon=139.6503, radiusKm=8f,  altRangeM=3000f },
            new PredefinedRegion { name="London",         estimatedSize="~270 MB", lat=51.5074, lon=-0.1278,  radiusKm=8f,  altRangeM=3000f },
            new PredefinedRegion { name="Paris",          estimatedSize="~250 MB", lat=48.8566, lon=2.3522,   radiusKm=7f,  altRangeM=3000f },
            new PredefinedRegion { name="Dubai",          estimatedSize="~210 MB", lat=25.2048, lon=55.2708,  radiusKm=7f,  altRangeM=3000f },
            new PredefinedRegion { name="Sydney",         estimatedSize="~230 MB", lat=-33.8688, lon=151.2093, radiusKm=7f, altRangeM=3000f },
            new PredefinedRegion { name="Seoul",          estimatedSize="~240 MB", lat=37.5665, lon=126.9780, radiusKm=7f,  altRangeM=3000f },
            new PredefinedRegion { name="Grand Canyon",   estimatedSize="~180 MB", lat=36.1069, lon=-112.1129, radiusKm=10f, altRangeM=5000f },
            new PredefinedRegion { name="Mount Everest",  estimatedSize="~150 MB", lat=27.9881, lon=86.9250,  radiusKm=8f,  altRangeM=9000f },
            new PredefinedRegion { name="Great Barrier Reef", estimatedSize="~200 MB", lat=-18.2871, lon=147.6992, radiusKm=12f, altRangeM=1000f },
        };

        // ── Internal ──────────────────────────────────────────────────────────────
        private readonly Dictionary<string, RegionRowBinding> _rows =
            new Dictionary<string, RegionRowBinding>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            BuildRows();
            UpdateCacheCurrentLocationButton();
            RefreshTotalCacheLabel();

            if (TileCacheManager.Instance != null)
            {
                TileCacheManager.Instance.OnCacheProgress   += OnProgress;
                TileCacheManager.Instance.OnCacheCompleted  += OnCompleted;
                TileCacheManager.Instance.OnCacheDeleted    += OnDeleted;
            }

            if (cacheCurrentLocationButton != null)
                cacheCurrentLocationButton.onClick.AddListener(OnCacheCurrentLocationClicked);
        }

        private void OnDestroy()
        {
            if (TileCacheManager.Instance != null)
            {
                TileCacheManager.Instance.OnCacheProgress  -= OnProgress;
                TileCacheManager.Instance.OnCacheCompleted -= OnCompleted;
                TileCacheManager.Instance.OnCacheDeleted   -= OnDeleted;
            }
        }

        // ── Row construction ──────────────────────────────────────────────────────

        private void BuildRows()
        {
            if (regionRowPrefab == null || regionListParent == null) return;

            foreach (var region in predefinedRegions)
            {
                var go  = Instantiate(regionRowPrefab, regionListParent);
                var row = new RegionRowBinding(go, region);
                row.OnDownloadClicked += () => StartDownload(region);
                row.OnDeleteClicked   += () => DeleteRegion(region.name);
                _rows[region.name] = row;
                RefreshRowState(region.name);
            }
        }

        // ── Button handlers ───────────────────────────────────────────────────────

        private void StartDownload(PredefinedRegion region)
        {
            if (TileCacheManager.Instance == null) return;
            TileCacheManager.Instance.CacheRegionAsync(
                region.name, region.lat, region.lon, region.radiusKm, region.altRangeM);
            Debug.Log($"[SWEF] RegionDownloadUI: download started for '{region.name}'");
        }

        private void DeleteRegion(string name)
        {
            TileCacheManager.Instance?.DeleteRegion(name);
        }

        private void OnCacheCurrentLocationClicked()
        {
            if (!Core.SWEFSession.HasFix)
            {
                Debug.LogWarning("[SWEF] RegionDownloadUI: no GPS fix — cannot cache current location.");
                return;
            }

            string name = $"Current Location ({System.DateTime.UtcNow:HH:mm} UTC)";
            TileCacheManager.Instance?.CacheRegionAsync(
                name,
                Core.SWEFSession.Lat,
                Core.SWEFSession.Lon,
                currentLocationRadiusKm,
                currentLocationAltRangeM);

            Debug.Log($"[SWEF] RegionDownloadUI: caching current location " +
                      $"({Core.SWEFSession.Lat:F4}, {Core.SWEFSession.Lon:F4})");
        }

        // ── TileCacheManager event handlers ───────────────────────────────────────

        private void OnProgress(string regionName, float progress)
        {
            if (_rows.TryGetValue(regionName, out var row))
                row.SetProgress(progress);
            RefreshTotalCacheLabel();
        }

        private void OnCompleted(string regionName)
        {
            RefreshRowState(regionName);
            RefreshTotalCacheLabel();
        }

        private void OnDeleted(string regionName)
        {
            RefreshRowState(regionName);
            RefreshTotalCacheLabel();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void RefreshRowState(string regionName)
        {
            if (!_rows.TryGetValue(regionName, out var row)) return;
            bool isCached = TileCacheManager.Instance != null &&
                            TileCacheManager.Instance.GetCachedRegions()
                                .Exists(r => r.name == regionName);
            row.SetCached(isCached);
        }

        private void RefreshTotalCacheLabel()
        {
            if (totalCacheLabel == null || TileCacheManager.Instance == null) return;
            long used = TileCacheManager.Instance.GetTotalCacheSizeBytes();
            long max  = TileCacheManager.Instance.GetMaxCacheSizeBytes();
            totalCacheLabel.text = $"Cache: {FormatBytes(used)} / {FormatBytes(max)}";
        }

        private void UpdateCacheCurrentLocationButton()
        {
            if (cacheCurrentLocationButton == null) return;
            cacheCurrentLocationButton.interactable = Core.SWEFSession.HasFix;
            if (cacheCurrentLocationLabel != null)
                cacheCurrentLocationLabel.text = Core.SWEFSession.HasFix
                    ? "Cache Current Location"
                    : "Cache Current Location (no GPS)";
        }

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
    }

    // ── Row binding helper (no MonoBehaviour) ─────────────────────────────────────
    /// <summary>
    /// Binds a region row prefab instance to a <see cref="PredefinedRegion"/> entry
    /// and exposes download/delete events.
    ///
    /// <para>Expected child names in the prefab: <c>NameLabel</c>, <c>SizeLabel</c>,
    /// <c>StatusLabel</c>, <c>ProgressBar</c>, <c>DownloadButton</c>,
    /// <c>DeleteButton</c>.</para>
    /// </summary>
    internal class RegionRowBinding
    {
        public event System.Action OnDownloadClicked;
        public event System.Action OnDeleteClicked;

        private readonly TextMeshProUGUI _nameLabel;
        private readonly TextMeshProUGUI _sizeLabel;
        private readonly TextMeshProUGUI _statusLabel;
        private readonly Slider          _progressBar;
        private readonly Button          _downloadButton;
        private readonly Button          _deleteButton;

        public RegionRowBinding(GameObject go, PredefinedRegion region)
        {
            _nameLabel      = go.transform.Find("NameLabel")?.GetComponent<TextMeshProUGUI>();
            _sizeLabel      = go.transform.Find("SizeLabel")?.GetComponent<TextMeshProUGUI>();
            _statusLabel    = go.transform.Find("StatusLabel")?.GetComponent<TextMeshProUGUI>();
            _progressBar    = go.transform.Find("ProgressBar")?.GetComponent<Slider>();
            _downloadButton = go.transform.Find("DownloadButton")?.GetComponent<Button>();
            _deleteButton   = go.transform.Find("DeleteButton")?.GetComponent<Button>();

            if (_nameLabel  != null) _nameLabel.text  = region.name;
            if (_sizeLabel  != null) _sizeLabel.text  = region.estimatedSize;
            if (_progressBar != null) _progressBar.value = 0f;

            _downloadButton?.onClick.AddListener(() => OnDownloadClicked?.Invoke());
            _deleteButton?.onClick.AddListener(() => OnDeleteClicked?.Invoke());
        }

        public void SetProgress(float progress)
        {
            if (_progressBar != null) _progressBar.value = progress;
            if (_statusLabel != null)
                _statusLabel.text = progress < 1f
                    ? $"Downloading {progress * 100f:F0}%"
                    : "Cached";
        }

        public void SetCached(bool cached)
        {
            if (_statusLabel != null)
                _statusLabel.text = cached ? "Cached" : "Not downloaded";
            if (_downloadButton != null) _downloadButton.gameObject.SetActive(!cached);
            if (_deleteButton   != null) _deleteButton.gameObject.SetActive(cached);
            if (_progressBar    != null) _progressBar.value = cached ? 1f : 0f;
        }
    }
}
