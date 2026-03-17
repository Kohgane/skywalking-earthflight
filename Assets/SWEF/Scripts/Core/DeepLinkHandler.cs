using UnityEngine;
using SWEF.Teleport;

namespace SWEF.Core
{
    /// <summary>
    /// Handles incoming deep links using the custom URL scheme
    /// <c>swef://teleport?lat=35.6762&amp;lon=139.6503&amp;name=Tokyo</c>.
    /// Parses the <c>lat</c>, <c>lon</c>, and <c>name</c> parameters and triggers a
    /// teleport via <see cref="TeleportController"/>.
    /// Place this component in the Boot scene; it survives scene loads via
    /// <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class DeepLinkHandler : MonoBehaviour
    {
        [SerializeField] private TeleportController teleport;

        /// <summary>Singleton instance; persists across scene loads.</summary>
        public static DeepLinkHandler Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (teleport == null)
                teleport = FindFirstObjectByType<TeleportController>();

            // Subscribe for future deep links
            Application.deepLinkActivated += OnDeepLink;

            // Handle a cold-start deep link (app launched via URL)
            if (!string.IsNullOrEmpty(Application.absoluteURL))
                OnDeepLink(Application.absoluteURL);
        }

        private void OnDestroy()
        {
            Application.deepLinkActivated -= OnDeepLink;
        }

        // ------------------------------------------------------------------ //
        //  Deep link processing                                                //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Called when the OS delivers a deep link URL to the app.
        /// Parses the query parameters and triggers a teleport if the data is valid.
        /// </summary>
        private void OnDeepLink(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            Debug.Log($"[SWEF] DeepLinkHandler: received URL → {url}");

            string latStr  = ParseQueryParam(url, "lat");
            string lonStr  = ParseQueryParam(url, "lon");
            string name    = ParseQueryParam(url, "name") ?? string.Empty;

            if (!double.TryParse(latStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lat) ||
                !double.TryParse(lonStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lon))
            {
                Debug.LogWarning($"[SWEF] DeepLinkHandler: could not parse lat/lon from '{url}'.");
                return;
            }

            if (lat < -90.0 || lat > 90.0)
            {
                Debug.LogWarning($"[SWEF] DeepLinkHandler: lat={lat} out of range (-90..90).");
                return;
            }

            if (lon < -180.0 || lon > 180.0)
            {
                Debug.LogWarning($"[SWEF] DeepLinkHandler: lon={lon} out of range (-180..180).");
                return;
            }

            if (teleport == null)
            {
                // Try to find it at teleport time in case it wasn't available at Awake
                teleport = FindFirstObjectByType<TeleportController>();
                if (teleport == null)
                {
                    Debug.LogWarning("[SWEF] DeepLinkHandler: TeleportController not found; cannot teleport.");
                    return;
                }
            }

            Debug.Log($"[SWEF] DeepLinkHandler: teleporting to lat={lat}, lon={lon}, name='{name}'");
            teleport.TeleportTo(lat, lon, name);
        }

        // ------------------------------------------------------------------ //
        //  URL parsing helper                                                  //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Extracts the value of <paramref name="key"/> from the query string of
        /// <paramref name="url"/>, or returns <c>null</c> when the key is absent.
        /// </summary>
        private string ParseQueryParam(string url, string key)
        {
            // Locate the query string
            int qIndex = url.IndexOf('?');
            if (qIndex < 0) return null;

            string query = url.Substring(qIndex + 1);
            string prefix = key + "=";

            foreach (string segment in query.Split('&'))
            {
                if (segment.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    string raw = segment.Substring(prefix.Length);
                    return UnityWebRequestDecode(raw);
                }
            }
            return null;
        }

        /// <summary>Decodes percent-encoded characters in a URL component.</summary>
        private static string UnityWebRequestDecode(string encoded)
        {
            return System.Uri.UnescapeDataString(encoded.Replace("+", " "));
        }
    }
}
