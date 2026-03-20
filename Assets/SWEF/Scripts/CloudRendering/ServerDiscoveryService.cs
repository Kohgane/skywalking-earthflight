using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.CloudRendering
{
    /// <summary>
    /// Discovers the nearest cloud rendering server by pinging multiple regional
    /// endpoints and selecting the one with the lowest latency.  The result is
    /// cached in <see cref="PlayerPrefs"/> and the server that was last chosen is
    /// restored on the next startup.
    /// </summary>
    public class ServerDiscoveryService : MonoBehaviour
    {
        // ── Nested types ──────────────────────────────────────────────────────────

        /// <summary>Describes a single cloud rendering server endpoint.</summary>
        [Serializable]
        public struct CloudServer
        {
            /// <summary>WebSocket URL of the server.</summary>
            public string url;

            /// <summary>Geographic region identifier.</summary>
            public string region;

            /// <summary>Most recently measured ping in milliseconds.</summary>
            public float pingMs;

            /// <summary>Percentage of server capacity currently available (0–100).</summary>
            public float capacity;

            /// <summary>Whether the server is currently accepting connections.</summary>
            public bool isAvailable;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Endpoints (one ping URL per region)")]
        [SerializeField] private string pingUrlUsEast    = "https://render-us-east.swef.example.com/ping";
        [SerializeField] private string pingUrlUsWest    = "https://render-us-west.swef.example.com/ping";
        [SerializeField] private string pingUrlEuWest    = "https://render-eu-west.swef.example.com/ping";
        [SerializeField] private string pingUrlEuCentral = "https://render-eu-central.swef.example.com/ping";
        [SerializeField] private string pingUrlAsiaEast  = "https://render-asia-east.swef.example.com/ping";
        [SerializeField] private string pingUrlAsiaSouth = "https://render-asia-south.swef.example.com/ping";

        [SerializeField] private float pingTimeoutSec = 5f;

        // ── Internal state ────────────────────────────────────────────────────────
        private CloudServer _bestServer;
        private bool _discoveryInProgress;

        private const string PrefKeyBestUrl    = "SWEF_CloudBestUrl";
        private const string PrefKeyBestRegion = "SWEF_CloudBestRegion";

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when server discovery completes, passing the best server found.</summary>
        public event Action<CloudServer> OnDiscoveryComplete;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>The currently selected best server.</summary>
        public CloudServer BestServer => _bestServer;

        /// <summary>Whether a discovery pass is currently running.</summary>
        public bool IsDiscoveryInProgress => _discoveryInProgress;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            // Restore cached selection
            string cachedUrl    = PlayerPrefs.GetString(PrefKeyBestUrl,    string.Empty);
            string cachedRegion = PlayerPrefs.GetString(PrefKeyBestRegion, string.Empty);
            if (!string.IsNullOrEmpty(cachedUrl))
            {
                _bestServer = new CloudServer { url = cachedUrl, region = cachedRegion, isAvailable = true };
                Debug.Log($"[SWEF] ServerDiscovery: loaded cached server {cachedRegion} → {cachedUrl}");
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Pings all regional endpoints and selects the lowest-latency available server.</summary>
        public async void DiscoverServers()
        {
            if (_discoveryInProgress) return;
            _discoveryInProgress = true;

            Debug.Log("[SWEF] ServerDiscovery: pinging regional endpoints…");

            var candidates = new List<(string url, string region)>
            {
                (pingUrlUsEast,    "US-East"),
                (pingUrlUsWest,    "US-West"),
                (pingUrlEuWest,    "EU-West"),
                (pingUrlEuCentral, "EU-Central"),
                (pingUrlAsiaEast,  "Asia-East"),
                (pingUrlAsiaSouth, "Asia-South"),
            };

            CloudServer best = default;
            best.pingMs      = float.MaxValue;

            foreach (var (url, region) in candidates)
            {
                float ms = await PingEndpointAsync(url);
                if (ms < best.pingMs)
                {
                    best = new CloudServer
                    {
                        url       = url.Replace("/ping", string.Empty),
                        region    = region,
                        pingMs    = ms,
                        capacity  = 100f, // placeholder — real server reports capacity via response headers or JSON body
                        isAvailable = ms < pingTimeoutSec * 1000f,
                    };
                }
            }

            _bestServer = best;
            _discoveryInProgress = false;

            // Cache selection
            PlayerPrefs.SetString(PrefKeyBestUrl,    best.url);
            PlayerPrefs.SetString(PrefKeyBestRegion, best.region);
            PlayerPrefs.Save();

            // Propagate to session config
            var session = FindFirstObjectByType<CloudSessionManager>();
            if (session != null)
            {
                session.Config.serverUrl = best.url;
                session.Config.region    = best.region;
            }

            OnDiscoveryComplete?.Invoke(best);
            Debug.Log($"[SWEF] ServerDiscovery: best server → {best.region} ({best.pingMs:F0} ms)");
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private async Task<float> PingEndpointAsync(string url)
        {
            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            try
            {
                using var req = UnityWebRequest.Head(url);
                req.timeout   = Mathf.RoundToInt(pingTimeoutSec);
                var op = req.SendWebRequest();
                while (!op.isDone)
                    await Task.Yield();

                if (req.result == UnityWebRequest.Result.Success)
                    return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] ServerDiscovery: ping failed for {url} — {ex.Message}");
            }
            return float.MaxValue;
        }
    }
}
