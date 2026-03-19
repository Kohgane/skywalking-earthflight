using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Offline
{
    /// <summary>
    /// Represents a single cached geographic region stored on device.
    /// </summary>
    [Serializable]
    public class CachedRegion
    {
        /// <summary>Human-readable region name.</summary>
        public string name;
        /// <summary>Centre latitude in degrees.</summary>
        public double lat;
        /// <summary>Centre longitude in degrees.</summary>
        public double lon;
        /// <summary>Cache radius in kilometres.</summary>
        public float radiusKm;
        /// <summary>Altitude range included in the cache (metres).</summary>
        public float altitudeRange;
        /// <summary>Size of stored data in bytes.</summary>
        public long sizeBytes;
        /// <summary>When the region was originally cached (ISO 8601 UTC string).</summary>
        public string cachedAtUtc;
        /// <summary>When the region was last accessed (ISO 8601 UTC string).</summary>
        public string lastAccessedUtc;

        // ── Helpers (not serialised) ──────────────────────────────────────────────
        [NonSerialized] public DateTime CachedAt;
        [NonSerialized] public DateTime LastAccessed;

        /// <summary>Parses the UTC string fields back into <see cref="DateTime"/> values.</summary>
        public void HydrateDates()
        {
            CachedAt = DateTime.TryParse(cachedAtUtc, out var ca) ? ca : DateTime.UtcNow;
            LastAccessed = DateTime.TryParse(lastAccessedUtc, out var la) ? la : DateTime.UtcNow;
        }
    }

    // ── JSON wrapper ─────────────────────────────────────────────────────────────
    [Serializable]
    internal class RegionList
    {
        public List<CachedRegion> regions = new List<CachedRegion>();
    }

    /// <summary>
    /// Manages persistent caching of 3D tile data to device storage.
    /// Survives scene loads via DontDestroyOnLoad.
    ///
    /// <para>Storage root: <c>Application.persistentDataPath/TileCache/</c>.
    /// Metadata is persisted as JSON in <c>regions.json</c> inside that folder.
    /// When the total cache size exceeds <see cref="MaxCacheSizeBytes"/> the
    /// least-recently-used region is evicted automatically (LRU).</para>
    ///
    /// <para>Actual Cesium tile payload download is stubbed out here; it will be
    /// wired to the Cesium offline API once that surface is available.</para>
    /// </summary>
    public class TileCacheManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static TileCacheManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Cache Limits")]
        [Tooltip("Maximum total cache size in bytes.  Default: 2 GB.")]
        [SerializeField] private long maxCacheSizeBytes = 2L * 1024 * 1024 * 1024; // 2 GB

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised periodically during a cache operation with the current progress (0–1).</summary>
        public event Action<string, float> OnCacheProgress;
        /// <summary>Raised when a region finishes caching successfully.</summary>
        public event Action<string> OnCacheCompleted;
        /// <summary>Raised when a region is deleted from the cache.</summary>
        public event Action<string> OnCacheDeleted;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Configured maximum cache size in bytes.</summary>
        public long MaxCacheSizeBytes => maxCacheSizeBytes;

        // ── Internal ──────────────────────────────────────────────────────────────
        private readonly List<CachedRegion> _regions = new List<CachedRegion>();
        private string _cacheRoot;
        private string _metaPath;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _cacheRoot = Path.Combine(Application.persistentDataPath, "TileCache");
            _metaPath  = Path.Combine(_cacheRoot, "regions.json");

            EnsureDirectory();
            LoadMetadata();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the current list of cached regions.
        /// </summary>
        public List<CachedRegion> GetCachedRegions() => new List<CachedRegion>(_regions);

        /// <summary>
        /// Total on-disk size of all cached regions in bytes.
        /// </summary>
        public long GetTotalCacheSizeBytes()
        {
            long total = 0;
            foreach (var r in _regions) total += r.sizeBytes;
            return total;
        }

        /// <summary>
        /// Configured maximum cache size in bytes.
        /// </summary>
        public long GetMaxCacheSizeBytes() => maxCacheSizeBytes;

        /// <summary>
        /// Returns <c>true</c> if any cached region covers the given coordinate.
        /// </summary>
        /// <param name="lat">Latitude in degrees.</param>
        /// <param name="lon">Longitude in degrees.</param>
        public bool IsCacheAvailableForLocation(double lat, double lon)
        {
            foreach (var r in _regions)
            {
                double distKm = HaversineKm(lat, lon, r.lat, r.lon);
                if (distKm <= r.radiusKm)
                {
                    // Touch last-accessed
                    r.LastAccessed = DateTime.UtcNow;
                    r.lastAccessedUtc = r.LastAccessed.ToString("o");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Starts caching a region asynchronously.  Progress is reported via
        /// <see cref="OnCacheProgress"/>.  When finished, <see cref="OnCacheCompleted"/>
        /// is raised and the region is persisted.
        /// </summary>
        /// <param name="name">Unique display name for the region.</param>
        /// <param name="lat">Centre latitude.</param>
        /// <param name="lon">Centre longitude.</param>
        /// <param name="radiusKm">Cache radius in kilometres.</param>
        /// <param name="altRange">Altitude range to include (metres).</param>
        public Coroutine CacheRegionAsync(string name, double lat, double lon,
                                          float radiusKm, float altRange)
        {
            return StartCoroutine(DoCacheRegion(name, lat, lon, radiusKm, altRange));
        }

        /// <summary>
        /// Deletes a cached region by name.
        /// </summary>
        /// <param name="name">Region name to delete.</param>
        /// <returns><c>true</c> if the region was found and removed.</returns>
        public bool DeleteRegion(string name)
        {
            int idx = _regions.FindIndex(r => r.name == name);
            if (idx < 0)
            {
                Debug.LogWarning($"[SWEF] TileCacheManager: region '{name}' not found for deletion.");
                return false;
            }

            var region = _regions[idx];
            _regions.RemoveAt(idx);

            // Remove backing data directory if it exists
            string regionDir = Path.Combine(_cacheRoot, SanitizeName(name));
            try
            {
                if (Directory.Exists(regionDir))
                    Directory.Delete(regionDir, recursive: true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] TileCacheManager: could not remove region dir: {ex.Message}");
            }

            SaveMetadata();
            Debug.Log($"[SWEF] TileCacheManager: deleted region '{name}'");
            Core.AnalyticsLogger.LogEvent("tile_cache_deleted", name);
            OnCacheDeleted?.Invoke(name);
            return true;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private IEnumerator DoCacheRegion(string name, double lat, double lon,
                                           float radiusKm, float altRange)
        {
            // Evict if necessary before writing
            EvictIfNeeded(EstimateRegionBytes(radiusKm));

            string regionDir = Path.Combine(_cacheRoot, SanitizeName(name));
            EnsureDirectory(regionDir);

            Debug.Log($"[SWEF] TileCacheManager: starting cache for '{name}' " +
                      $"(lat={lat:F4}, lon={lon:F4}, r={radiusKm} km)");

            // --- Stub download simulation ---
            // Replace this section with real Cesium tile API calls when available.
            const int steps = 20;
            long estimatedBytes = EstimateRegionBytes(radiusKm);

            for (int i = 0; i < steps; i++)
            {
                if (this == null || !gameObject.activeInHierarchy) yield break;

                float progress = (float)(i + 1) / steps;
                OnCacheProgress?.Invoke(name, progress);
                yield return new WaitForSeconds(0.1f);   // simulate download latency
            }
            // --- End stub ---

            var region = new CachedRegion
            {
                name           = name,
                lat            = lat,
                lon            = lon,
                radiusKm       = radiusKm,
                altitudeRange  = altRange,
                sizeBytes      = estimatedBytes,
                CachedAt       = DateTime.UtcNow,
                LastAccessed   = DateTime.UtcNow,
            };
            region.cachedAtUtc     = region.CachedAt.ToString("o");
            region.lastAccessedUtc = region.LastAccessed.ToString("o");

            // Replace existing entry if name collision
            int existing = _regions.FindIndex(r => r.name == name);
            if (existing >= 0)
                _regions[existing] = region;
            else
                _regions.Add(region);

            SaveMetadata();
            Debug.Log($"[SWEF] TileCacheManager: cached '{name}' " +
                      $"({estimatedBytes / (1024 * 1024)} MB simulated)");
            Core.AnalyticsLogger.LogEvent("tile_cache_completed", name);
            OnCacheCompleted?.Invoke(name);
        }

        /// <summary>Rough size estimate based on radius: πr² × density factor.</summary>
        private static long EstimateRegionBytes(float radiusKm)
        {
            const long bytesPerSqKm = 5L * 1024 * 1024; // 5 MB/km²
            double areaSqKm = Math.PI * radiusKm * radiusKm;
            return (long)(areaSqKm * bytesPerSqKm);
        }

        /// <summary>
        /// LRU eviction: removes oldest-accessed regions until there is room for
        /// <paramref name="neededBytes"/> of new data.
        /// </summary>
        private void EvictIfNeeded(long neededBytes)
        {
            while (_regions.Count > 0 && GetTotalCacheSizeBytes() + neededBytes > maxCacheSizeBytes)
            {
                // Find LRU region
                int lruIdx = 0;
                for (int i = 1; i < _regions.Count; i++)
                {
                    if (_regions[i].LastAccessed < _regions[lruIdx].LastAccessed)
                        lruIdx = i;
                }
                string evictName = _regions[lruIdx].name;
                Debug.Log($"[SWEF] TileCacheManager: LRU evicting '{evictName}'");
                DeleteRegion(evictName);
            }
        }

        private void LoadMetadata()
        {
            _regions.Clear();
            if (!File.Exists(_metaPath)) return;

            try
            {
                string json = File.ReadAllText(_metaPath);
                var wrapper = JsonUtility.FromJson<RegionList>(json);
                if (wrapper?.regions != null)
                {
                    foreach (var r in wrapper.regions)
                    {
                        r.HydrateDates();
                        _regions.Add(r);
                    }
                }
                Debug.Log($"[SWEF] TileCacheManager: loaded {_regions.Count} cached region(s).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] TileCacheManager: failed to load metadata: {ex.Message}");
            }
        }

        private void SaveMetadata()
        {
            try
            {
                EnsureDirectory();
                var wrapper = new RegionList { regions = _regions };
                File.WriteAllText(_metaPath, JsonUtility.ToJson(wrapper, prettyPrint: true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] TileCacheManager: failed to save metadata: {ex.Message}");
            }
        }

        private void EnsureDirectory() => EnsureDirectory(_cacheRoot);

        private static void EnsureDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] TileCacheManager: could not create directory '{path}': {ex.Message}");
            }
        }

        private static string SanitizeName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        // ── Haversine distance ────────────────────────────────────────────────────

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Earth radius km
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        }
    }
}
