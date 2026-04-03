using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Singleton that manages community-shared waypoints.
    /// Supports sharing, importing, proximity queries, likes, and deep links.
    /// Persisted to <c>shared_waypoints.json</c>.
    /// Deep link: <c>swef://waypoint?id=xxx</c>.
    /// </summary>
    public class SharedWaypointManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared instance of the waypoint manager.</summary>
        public static SharedWaypointManager Instance { get; private set; }
        #endregion

        #region Constants
        private const string PersistenceFileName = "shared_waypoints.json";
        private const double EarthRadiusKm = 6371.0;
        #endregion

        #region Private State
        private readonly List<SharedWaypointData> _waypoints = new List<SharedWaypointData>();
        private string _persistencePath;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _persistencePath = Path.Combine(Application.persistentDataPath, PersistenceFileName);
            LoadWaypoints();
            RegisterDeepLink();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Shares the local player's current location as a named waypoint.
        /// </summary>
        /// <param name="name">Waypoint display name.</param>
        /// <param name="latitude">Latitude in decimal degrees.</param>
        /// <param name="longitude">Longitude in decimal degrees.</param>
        /// <param name="altitude">Altitude above sea level in metres.</param>
        /// <param name="description">Optional description.</param>
        /// <param name="category">Waypoint category.</param>
        /// <param name="isPublic">True to share with all players; false for friends only.</param>
        /// <returns>The newly created <see cref="SharedWaypointData"/>.</returns>
        public SharedWaypointData ShareWaypoint(string name, double latitude, double longitude,
            double altitude, string description = "", WaypointCategory category = WaypointCategory.Custom,
            bool isPublic = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("[SWEF] Multiplayer: ShareWaypoint called with empty name.");
                return null;
            }

            string localId = PlayerProfileManager.Instance?.GetLocalProfile()?.playerId ?? "local";

            var waypoint = new SharedWaypointData
            {
                waypointId = Guid.NewGuid().ToString(),
                name = name,
                latitude = latitude,
                longitude = longitude,
                altitude = altitude,
                sharedBy = localId,
                sharedAt = DateTime.UtcNow.ToString("o"),
                description = description,
                category = category,
                likes = 0,
                isPublic = isPublic
            };

            _waypoints.Add(waypoint);
            SaveWaypoints();

            MultiplayerAnalytics.RecordWaypointShared(category.ToString());
            MultiplayerBridge.OnWaypointShared(waypoint);

            Debug.Log($"[SWEF] Multiplayer: Waypoint shared — {name} ({waypoint.waypointId})");
            return waypoint;
        }

        /// <summary>
        /// Imports a shared waypoint into the local collection (e.g. from a deep link).
        /// </summary>
        /// <param name="waypoint">Waypoint data to import.</param>
        public void ImportWaypoint(SharedWaypointData waypoint)
        {
            if (waypoint == null)
            {
                Debug.LogWarning("[SWEF] Multiplayer: ImportWaypoint called with null data.");
                return;
            }
            if (_waypoints.Exists(w => w.waypointId == waypoint.waypointId))
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Waypoint {waypoint.waypointId} already imported.");
                return;
            }

            _waypoints.Add(waypoint);
            SaveWaypoints();

            MultiplayerBridge.OnWaypointVisited(waypoint);
            Debug.Log($"[SWEF] Multiplayer: Waypoint imported — {waypoint.name}");
        }

        /// <summary>
        /// Returns all public waypoints within a given radius of a position.
        /// Uses the Haversine formula for distance calculation.
        /// </summary>
        /// <param name="lat">Centre latitude in decimal degrees.</param>
        /// <param name="lon">Centre longitude in decimal degrees.</param>
        /// <param name="radiusKm">Search radius in kilometres.</param>
        /// <returns>List of matching waypoints sorted by distance (nearest first).</returns>
        public List<SharedWaypointData> GetNearbySharedWaypoints(double lat, double lon, double radiusKm)
        {
            var result = new List<SharedWaypointData>();
            foreach (var wp in _waypoints)
            {
                if (!wp.isPublic) continue;
                double dist = HaversineKm(lat, lon, wp.latitude, wp.longitude);
                if (dist <= radiusKm)
                    result.Add(wp);
            }
            result.Sort((a, b) =>
                HaversineKm(lat, lon, a.latitude, a.longitude)
                    .CompareTo(HaversineKm(lat, lon, b.latitude, b.longitude)));
            return result;
        }

        /// <summary>
        /// Increments the like count on a waypoint.
        /// </summary>
        /// <param name="waypointId">ID of the waypoint to like.</param>
        public void LikeWaypoint(string waypointId)
        {
            var wp = _waypoints.Find(w => w.waypointId == waypointId);
            if (wp == null)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Waypoint {waypointId} not found for like.");
                return;
            }
            wp.likes++;
            SaveWaypoints();
            MultiplayerAnalytics.RecordWaypointLiked();
        }

        /// <summary>
        /// Returns the most-liked public waypoints, sorted descending by like count.
        /// </summary>
        /// <param name="limit">Maximum number of waypoints to return.</param>
        public List<SharedWaypointData> GetPopularWaypoints(int limit = 20)
        {
            var sorted = new List<SharedWaypointData>(_waypoints.FindAll(w => w.isPublic));
            sorted.Sort((a, b) => b.likes.CompareTo(a.likes));
            if (sorted.Count > limit)
                sorted.RemoveRange(limit, sorted.Count - limit);
            return sorted;
        }

        /// <summary>
        /// Returns waypoints shared by any of the local player's friends.
        /// </summary>
        public List<SharedWaypointData> GetFriendWaypoints()
        {
            if (FriendSystemController.Instance == null) return new List<SharedWaypointData>();
            var friends = FriendSystemController.Instance.GetFriendList();
            var friendIds = new HashSet<string>();
            foreach (var f in friends)
                friendIds.Add(f.friendId);

            return _waypoints.FindAll(w => friendIds.Contains(w.sharedBy));
        }

        /// <summary>
        /// Looks up a waypoint by its ID.
        /// </summary>
        /// <param name="waypointId">Waypoint GUID to find.</param>
        /// <returns>The matching waypoint, or null.</returns>
        public SharedWaypointData GetWaypointById(string waypointId) =>
            string.IsNullOrEmpty(waypointId)
                ? null
                : _waypoints.Find(w => w.waypointId == waypointId);
        #endregion

        #region Deep Link
        private void RegisterDeepLink()
        {
#if SWEF_DEEPLINK_AVAILABLE
            SWEF.Core.DeepLinkHandler.RegisterRoute("waypoint", HandleWaypointDeepLink);
#endif
        }

        private void HandleWaypointDeepLink(string url)
        {
            // Parse swef://waypoint?id=xxx
            string id = ExtractQueryParam(url, "id");
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[SWEF] Multiplayer: Waypoint deep link missing 'id' parameter.");
                return;
            }
            var wp = GetWaypointById(id);
            if (wp != null)
            {
                Debug.Log($"[SWEF] Multiplayer: Deep link — viewing waypoint {wp.name}");
            }
            else
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Deep link — waypoint {id} not found locally.");
            }
        }

        private static string ExtractQueryParam(string url, string param)
        {
            if (string.IsNullOrEmpty(url)) return null;
            int q = url.IndexOf('?');
            if (q < 0) return null;
            string query = url.Substring(q + 1);
            foreach (string pair in query.Split('&'))
            {
                int eq = pair.IndexOf('=');
                if (eq < 0) continue;
                if (pair.Substring(0, eq) == param)
                    return Uri.UnescapeDataString(pair.Substring(eq + 1));
            }
            return null;
        }
        #endregion

        #region Haversine Distance
        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }
        #endregion

        #region Persistence
        private void SaveWaypoints()
        {
            try
            {
                var wrapper = new WaypointListWrapper { waypoints = _waypoints };
                File.WriteAllText(_persistencePath, JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to save waypoints — {ex.Message}");
            }
        }

        private void LoadWaypoints()
        {
            if (!File.Exists(_persistencePath)) return;
            try
            {
                string json = File.ReadAllText(_persistencePath);
                var wrapper = JsonUtility.FromJson<WaypointListWrapper>(json);
                if (wrapper?.waypoints != null)
                    _waypoints.AddRange(wrapper.waypoints);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to load waypoints — {ex.Message}");
            }
        }

        [Serializable]
        private class WaypointListWrapper
        {
            public List<SharedWaypointData> waypoints;
        }
        #endregion
    }
}
