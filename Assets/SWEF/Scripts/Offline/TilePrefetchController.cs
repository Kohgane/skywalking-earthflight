using System.Collections;
using UnityEngine;

namespace SWEF.Offline
{
    /// <summary>
    /// Handles intelligent prefetching of 3D tiles based on the current flight
    /// trajectory.  Only runs on WiFi and is throttled to avoid excessive
    /// bandwidth consumption.
    ///
    /// <para>Integrates with <see cref="SWEF.Flight.FlightController"/> for speed
    /// and heading, and with <see cref="TileCacheManager"/> to trigger caching of
    /// predicted positions.</para>
    /// </summary>
    public class TilePrefetchController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static TilePrefetchController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Prefetch Settings")]
        [Tooltip("How many seconds ahead to predict flight position.")]
        [SerializeField] private float prefetchAheadSeconds = 30f;

        [Tooltip("Radius (km) of each prefetch tile request.")]
        [SerializeField] private float prefetchRadiusKm = 5f;

        [Tooltip("Minimum interval (seconds) between consecutive prefetch requests.")]
        [SerializeField] private float minPrefetchIntervalSec = 10f;

        [Tooltip("Altitude range (metres) included in each prefetch.")]
        [SerializeField] private float prefetchAltRange = 5000f;

        [Header("References")]
        [Tooltip("Flight controller to read heading and speed from.  Auto-found if null.")]
        [SerializeField] private Flight.FlightController flightController;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when a prefetch request starts.</summary>
        public event System.Action<double, double> OnPrefetchStarted;

        /// <summary>Raised when a prefetch request completes.</summary>
        public event System.Action<double, double> OnPrefetchCompleted;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Whether prefetching is currently enabled.</summary>
        public bool IsEnabled { get; private set; } = true;

        // ── Internal ──────────────────────────────────────────────────────────────
        private float _lastPrefetchTime = -999f;
        private Coroutine _prefetchCoroutine;

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
        }

        private void Start()
        {
            if (flightController == null)
                flightController = FindFirstObjectByType<Flight.FlightController>();

            if (flightController == null)
                Debug.LogWarning("[SWEF] TilePrefetchController: FlightController not found — prefetch disabled until assigned.");
        }

        private void Update()
        {
            if (!IsEnabled) return;
            if (flightController == null) return;

            // Only prefetch on WiFi
            if (OfflineManager.Instance != null &&
                OfflineManager.Instance.CurrentConnection != ConnectionType.WiFi)
                return;

            // Throttle
            if (Time.time - _lastPrefetchTime < minPrefetchIntervalSec) return;

            _lastPrefetchTime = Time.time;
            TriggerPrefetch();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Enables or disables tile prefetching.
        /// </summary>
        /// <param name="enabled"><c>true</c> to enable; <c>false</c> to suspend.</param>
        public void Enable(bool enabled)
        {
            IsEnabled = enabled;
            Debug.Log($"[SWEF] TilePrefetchController: prefetch {(enabled ? "enabled" : "disabled")}.");
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void TriggerPrefetch()
        {
            if (TileCacheManager.Instance == null) return;

            // Predict future lat/lon using current heading and speed
            float speedMps = flightController.CurrentSpeedMps;
            float headingDeg = flightController.transform.eulerAngles.y;

            double distanceKm = (speedMps * prefetchAheadSeconds) / 1000.0;

            // Current position from SWEFSession
            double currentLat = Core.SWEFSession.Lat;
            double currentLon = Core.SWEFSession.Lon;

            // Project forward using simple flat-earth approximation for small distances
            double headingRad = headingDeg * System.Math.PI / 180.0;
            double deltaLat = (distanceKm / 111.32) * System.Math.Cos(headingRad);
            double deltaLon = (distanceKm / (111.32 * System.Math.Cos(currentLat * System.Math.PI / 180.0)))
                              * System.Math.Sin(headingRad);

            double targetLat = currentLat + deltaLat;
            double targetLon = currentLon + deltaLon;

            // Skip if already cached
            if (TileCacheManager.Instance.IsCacheAvailableForLocation(targetLat, targetLon))
                return;

            string regionName = $"prefetch_{targetLat:F2}_{targetLon:F2}";

            Debug.Log($"[SWEF] TilePrefetchController: prefetching tiles at " +
                      $"({targetLat:F4}, {targetLon:F4})");

            OnPrefetchStarted?.Invoke(targetLat, targetLon);

            // Wire completion event before starting
            void OnComplete(string name)
            {
                if (name == regionName)
                {
                    TileCacheManager.Instance.OnCacheCompleted -= OnComplete;
                    OnPrefetchCompleted?.Invoke(targetLat, targetLon);
                }
            }
            TileCacheManager.Instance.OnCacheCompleted += OnComplete;

            TileCacheManager.Instance.CacheRegionAsync(
                regionName, targetLat, targetLon, prefetchRadiusKm, prefetchAltRange);
        }
    }
}
