using System.Collections;
using UnityEngine;

namespace SWEF.Offline
{
    /// <summary>
    /// Connection type reported by <see cref="OfflineManager"/>.
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>No network interface available.</summary>
        None,
        /// <summary>Connected via WiFi or wired LAN.</summary>
        WiFi,
        /// <summary>Connected via mobile/cellular data.</summary>
        Cellular
    }

    /// <summary>
    /// Singleton MonoBehaviour that monitors network reachability and manages
    /// offline state.  Survives scene loads via DontDestroyOnLoad.
    ///
    /// <para>Poll interval, force-offline preference (PlayerPrefs key
    /// <c>SWEF_ForceOffline</c>), and connectivity-change events are all
    /// handled here so other systems only need to read <see cref="IsOffline"/>
    /// or subscribe to the events.</para>
    /// </summary>
    public class OfflineManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static OfflineManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Polling")]
        [Tooltip("How often (seconds) to re-check network reachability.")]
        [SerializeField] private float pollIntervalSec = 2f;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>
        /// <c>true</c> when the device has no usable internet connection OR when
        /// <see cref="IsForcedOffline"/> is active.
        /// </summary>
        public bool IsOffline { get; private set; }

        /// <summary><c>true</c> when offline mode is forced via <see cref="ForceOfflineMode"/>.</summary>
        public bool IsForcedOffline { get; private set; }

        /// <summary>Current detected connection type.</summary>
        public ConnectionType CurrentConnection { get; private set; }

        /// <summary>UTC timestamp when the device first went offline this session; null while online.</summary>
        public System.DateTime? OfflineSince { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised whenever connectivity changes.  Parameter is <c>true</c> when going online.</summary>
        public event System.Action<bool> OnConnectivityChanged;

        /// <summary>Raised when the app enters offline mode.</summary>
        public event System.Action OnOfflineModeEntered;

        /// <summary>Raised when connectivity is restored after being offline.</summary>
        public event System.Action OnOnlineModeRestored;

        // ── PlayerPrefs key ───────────────────────────────────────────────────────
        private const string KeyForceOffline = "SWEF_ForceOffline";

        // ── Internal ──────────────────────────────────────────────────────────────
        private Coroutine _pollCoroutine;

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

            // Restore user preference
            IsForcedOffline = PlayerPrefs.GetInt(KeyForceOffline, 0) == 1;
        }

        private void Start()
        {
            // Evaluate immediately so dependent systems have a valid state right away
            EvaluateConnectivity();

            _pollCoroutine = StartCoroutine(PollConnectivity());

            // Notify dependent systems about cached data on startup
            bool hasCachedData = TileCacheManager.Instance != null &&
                                 TileCacheManager.Instance.GetCachedRegions().Count > 0;
            if (hasCachedData)
                Debug.Log("[SWEF] OfflineManager: cached tile regions available for offline flight.");
        }

        private void OnDestroy()
        {
            if (_pollCoroutine != null)
                StopCoroutine(_pollCoroutine);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Enables or disables forced offline mode and persists the preference.
        /// </summary>
        /// <param name="force"><c>true</c> to force offline; <c>false</c> to restore
        /// auto-detection.</param>
        public void ForceOfflineMode(bool force)
        {
            IsForcedOffline = force;
            PlayerPrefs.SetInt(KeyForceOffline, force ? 1 : 0);
            PlayerPrefs.Save();

            // Re-evaluate immediately
            EvaluateConnectivity();
            Debug.Log($"[SWEF] OfflineManager: force-offline set to {force}");
        }

        // ── Internal polling ──────────────────────────────────────────────────────

        private IEnumerator PollConnectivity()
        {
            var wait = new WaitForSeconds(pollIntervalSec);
            while (true)
            {
                if (this == null || !gameObject.activeInHierarchy) yield break;
                EvaluateConnectivity();
                yield return wait;
            }
        }

        private void EvaluateConnectivity()
        {
            ConnectionType newConn = MapReachability(Application.internetReachability);
            bool newOffline = IsForcedOffline || newConn == ConnectionType.None;

            // Update connection type (always, even if offline state unchanged)
            CurrentConnection = newConn;

            if (newOffline == IsOffline) return;   // no state change

            bool wasOffline = IsOffline;
            IsOffline = newOffline;

            if (IsOffline)
            {
                OfflineSince = System.DateTime.UtcNow;
                Debug.Log($"[SWEF] OfflineManager: entered offline mode at {OfflineSince:u}");
                Core.AnalyticsLogger.LogEvent("offline_mode_entered");
                OnOfflineModeEntered?.Invoke();
            }
            else
            {
                var duration = OfflineSince.HasValue
                    ? (System.DateTime.UtcNow - OfflineSince.Value).TotalSeconds
                    : 0.0;
                OfflineSince = null;
                Debug.Log($"[SWEF] OfflineManager: online mode restored (offline for {duration:F0}s)");
                Core.AnalyticsLogger.LogEvent("online_mode_restored", $"duration={duration:F0}s");
                OnOnlineModeRestored?.Invoke();
            }

            OnConnectivityChanged?.Invoke(!IsOffline);
        }

        private static ConnectionType MapReachability(NetworkReachability r) => r switch
        {
            NetworkReachability.ReachableViaLocalAreaNetwork => ConnectionType.WiFi,
            NetworkReachability.ReachableViaCarrierDataNetwork => ConnectionType.Cellular,
            _ => ConnectionType.None
        };
    }
}
