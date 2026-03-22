using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Handles sharing routes with other players via deep links, QR codes,
    /// social platforms, and multiplayer sessions. Also manages per-route ratings and
    /// leaderboard data structures.
    /// Integrates with <c>SWEF.Social.SocialShareManager</c> and
    /// <c>SWEF.Multiplayer.MultiplayerManager</c>.
    /// </summary>
    public class RouteShareManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static RouteShareManager Instance { get; private set; }

        #endregion

        #region Constants

        private const string ShareUrlBase    = "https://swef.app/route/";
        private const string ShareUrlScheme  = "swef://route/";

        #endregion

        #region Events

        /// <summary>Fired when a route is shared via any channel.</summary>
        public event Action<FlightRoute> OnRouteShared;

        /// <summary>Fired when the player submits a rating for a route.</summary>
        public event Action<string, float> OnRouteRated;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Public API — Sharing

        /// <summary>
        /// Shares the route summary and a screenshot via the platform's native share sheet
        /// by delegating to <c>SWEF.Social.SocialShareManager</c>.
        /// </summary>
        /// <param name="route">Route to share.</param>
        public void ShareRoute(FlightRoute route)
        {
            if (route == null) return;

            string link   = GenerateShareLink(route);
            string text   = BuildShareText(route, link);

            TrySocialShare(text, route.thumbnailPath);

            OnRouteShared?.Invoke(route);
            RoutePlannerAnalytics.Instance?.TrackRouteShared(route);
        }

        /// <summary>
        /// Generates a deep-link URL that other players can tap to open this route.
        /// </summary>
        /// <param name="route">Route to link.</param>
        /// <returns>Shareable URL string.</returns>
        public string GenerateShareLink(FlightRoute route)
        {
            if (route == null) return string.Empty;
            return $"{ShareUrlBase}{Uri.EscapeDataString(route.routeId)}";
        }

        /// <summary>
        /// Generates a QR code texture for the route share link.
        /// Returns a placeholder 2×2 white texture when the QR library is unavailable.
        /// </summary>
        /// <param name="route">Route to encode.</param>
        /// <returns>A <see cref="Texture2D"/> containing the QR code.</returns>
        public Texture2D GenerateQRCode(FlightRoute route)
        {
            string link = GenerateShareLink(route);

            // The real implementation would use a QR encoding library.
            // We create a readable placeholder texture here.
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            var pixels = new Color32[256 * 256];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();

            Debug.Log($"[SWEF] RouteShareManager: QR code placeholder created for link: {link}");
            return tex;
        }

        /// <summary>
        /// Exports the route as a <c>.swefroute</c> file and sends it via the platform
        /// share mechanism (e.g. Files, AirDrop, email).
        /// </summary>
        /// <param name="route">Route to export-share.</param>
        public void ShareRouteFile(FlightRoute route)
        {
            if (route == null) return;

            string exportPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                $"{SanitizeFilename(route.name)}.swefroute");

            RouteStorageManager.Instance?.ExportRoute(route, exportPath);

            if (System.IO.File.Exists(exportPath))
            {
                TrySocialShareFile(exportPath);
                OnRouteShared?.Invoke(route);
            }
        }

        /// <summary>
        /// Broadcasts the route definition to all players in the current multiplayer session
        /// so they can fly it together.
        /// </summary>
        /// <param name="route">Route to broadcast.</param>
        public void SendRouteToMultiplayerSession(FlightRoute route)
        {
            if (route == null) return;

            var mpType = Type.GetType("SWEF.Multiplayer.MultiplayerManager, Assembly-CSharp");
            if (mpType == null) return;

            var instance = FindObjectOfType(mpType) as MonoBehaviour;
            if (instance == null) return;

            var method = mpType.GetMethod("BroadcastCustomData",
                new[] { typeof(string), typeof(string) });
            if (method == null) return;

            string json = JsonUtility.ToJson(route);
            method.Invoke(instance, new object[] { "swef_route", json });
        }

        #endregion

        #region Public API — Rating

        /// <summary>
        /// Records a player rating for the route. Updates the running average.
        /// </summary>
        /// <param name="routeId">Target route id.</param>
        /// <param name="rating">Rating value, clamped to [0, 5].</param>
        public void RateRoute(string routeId, float rating)
        {
            rating = Mathf.Clamp(rating, 0f, 5f);

            var route = RouteStorageManager.Instance?.LoadRoute(routeId);
            if (route != null)
            {
                // Running-average update
                float total   = route.rating * route.ratingCount + rating;
                route.ratingCount++;
                route.rating  = total / route.ratingCount;
                RouteStorageManager.Instance?.SaveRoute(route);
            }

            OnRouteRated?.Invoke(routeId, rating);
            RoutePlannerAnalytics.Instance?.TrackRouteRated(routeId, rating);
        }

        /// <summary>
        /// Returns the leaderboard entries for a route ordered by best time ascending.
        /// In Phase 49 this returns the locally persisted list; a future backend would
        /// extend this with online data.
        /// </summary>
        /// <param name="routeId">Route to query.</param>
        /// <returns>List of leaderboard entries.</returns>
        public List<RouteLeaderboardEntry> GetRouteLeaderboard(string routeId)
        {
            // Placeholder: returns an empty list until backend integration is added
            return new List<RouteLeaderboardEntry>();
        }

        #endregion

        #region Private Helpers

        private static string BuildShareText(FlightRoute route, string link)
        {
            return $"Check out my flight route \"{route.name}\" in SWEF Earth Flight! "
                 + $"{route.estimatedDistance:F0} km, {route.estimatedDuration:F0} min. "
                 + $"{link}";
        }

        private void TrySocialShare(string text, string imagePath)
        {
            var shareType = Type.GetType("SWEF.Social.ShareManager, Assembly-CSharp");
            if (shareType == null) return;

            var instanceProp = shareType.GetProperty("Instance");
            if (instanceProp == null) return;

            var instance = instanceProp.GetValue(null) as MonoBehaviour;
            if (instance == null) return;

            // Try ShareText(string) first, then ShareTextWithImage(string, string)
            var method = shareType.GetMethod("ShareTextWithImage", new[] { typeof(string), typeof(string) })
                      ?? shareType.GetMethod("ShareText", new[] { typeof(string) });

            if (method?.GetParameters().Length == 2)
                method.Invoke(instance, new object[] { text, imagePath });
            else
                method?.Invoke(instance, new object[] { text });
        }

        private void TrySocialShareFile(string filePath)
        {
            // Platform-specific file sharing — delegates to native plugin layer
            Debug.Log($"[SWEF] RouteShareManager: share file: {filePath}");
        }

        private static string SanitizeFilename(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        #endregion
    }

    /// <summary>
    /// A single entry on a route leaderboard, storing the player's best completion time.
    /// </summary>
    [Serializable]
    public class RouteLeaderboardEntry
    {
        /// <summary>Player's platform user id.</summary>
        public string playerId;

        /// <summary>Player's display name.</summary>
        public string playerName;

        /// <summary>Best completion time in seconds.</summary>
        public float bestTimeSeconds;

        /// <summary>ISO-8601 timestamp of the best run.</summary>
        public string achievedAt;

        /// <summary>Player's rank on the leaderboard (1-indexed).</summary>
        public int rank;
    }
}
