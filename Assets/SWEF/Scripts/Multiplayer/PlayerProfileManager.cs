using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Singleton that manages the local player's profile and caches remote player profiles.
    /// Automatically syncs rank and flight hours from <c>ProgressionManager</c>.
    /// Profile is persisted to <c>player_profile.json</c>.
    /// </summary>
    public class PlayerProfileManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared instance of the profile manager.</summary>
        public static PlayerProfileManager Instance { get; private set; }
        #endregion

        #region Constants
        private const string ProfileFileName = "player_profile.json";
        private const int MaxCachedProfiles = 100;
        #endregion

        #region Inspector
        [Header("Profile Settings")]
        [SerializeField, Tooltip("Default display name used when no profile exists yet.")]
        private string defaultDisplayName = "Pilot";

        [SerializeField, Tooltip("Default avatar URL.")]
        private string defaultAvatarUrl = "";
        #endregion

        #region Private State
        private PlayerProfileData _localProfile;
        private readonly Dictionary<string, PlayerProfileData> _remoteCache =
            new Dictionary<string, PlayerProfileData>();
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
            _persistencePath = Path.Combine(Application.persistentDataPath, ProfileFileName);
            LoadLocalProfile();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Returns the local player's profile, creating a default one if none exists.
        /// </summary>
        /// <returns>The local <see cref="PlayerProfileData"/>.</returns>
        public PlayerProfileData GetLocalProfile()
        {
            if (_localProfile == null)
                CreateDefaultProfile();
            return _localProfile;
        }

        /// <summary>
        /// Updates fields on the local profile and persists the change.
        /// </summary>
        /// <param name="displayName">New display name, or null to keep existing.</param>
        /// <param name="avatarUrl">New avatar URL, or null to keep existing.</param>
        /// <param name="status">New player status.</param>
        public void UpdateLocalProfile(string displayName = null, string avatarUrl = null,
            PlayerStatus? status = null)
        {
            if (_localProfile == null)
                CreateDefaultProfile();

            if (!string.IsNullOrEmpty(displayName))
                _localProfile.displayName = displayName;
            if (!string.IsNullOrEmpty(avatarUrl))
                _localProfile.avatarUrl = avatarUrl;
            if (status.HasValue)
                _localProfile.status = status.Value;

            _localProfile.lastSeen = DateTime.UtcNow.ToString("o");
            SyncProgressionData();
            SaveLocalProfile();
        }

        /// <summary>
        /// Updates the local player's in-world position on their profile.
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees.</param>
        /// <param name="longitude">Longitude in decimal degrees.</param>
        /// <param name="altitude">Altitude above sea level in metres.</param>
        public void UpdateLocalPosition(double latitude, double longitude, double altitude)
        {
            if (_localProfile == null) CreateDefaultProfile();
            _localProfile.currentLatitude = latitude;
            _localProfile.currentLongitude = longitude;
            _localProfile.currentAltitude = altitude;
        }

        /// <summary>
        /// Returns a cached remote player profile, or null if not cached.
        /// </summary>
        /// <param name="playerId">The target player's unique ID.</param>
        /// <returns>Cached <see cref="PlayerProfileData"/> or null.</returns>
        public PlayerProfileData GetRemoteProfile(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogWarning("[SWEF] Multiplayer: GetRemoteProfile called with null/empty playerId.");
                return null;
            }
            _remoteCache.TryGetValue(playerId, out PlayerProfileData cached);
            return cached;
        }

        /// <summary>
        /// Stores a remote player profile in the local cache.
        /// Evicts oldest entry if the cache is full.
        /// </summary>
        /// <param name="profile">Profile to cache.</param>
        public void CacheProfile(PlayerProfileData profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("[SWEF] Multiplayer: CacheProfile called with null profile.");
                return;
            }
            if (_remoteCache.Count >= MaxCachedProfiles && !_remoteCache.ContainsKey(profile.playerId))
            {
                // Evict one entry — collect the key first to avoid modifying the collection during iteration
                string evictKey = null;
                foreach (var key in _remoteCache.Keys) { evictKey = key; break; }
                if (evictKey != null) _remoteCache.Remove(evictKey);
            }
            _remoteCache[profile.playerId] = profile;
        }
        #endregion

        #region Progression Sync
        /// <summary>
        /// Pulls rank and flight hours from <c>ProgressionManager</c> (if available)
        /// and writes them to the local profile.
        /// </summary>
        public void SyncProgressionData()
        {
#if SWEF_PROGRESSION_AVAILABLE
            if (SWEF.Progression.ProgressionManager.Instance != null && _localProfile != null)
            {
                _localProfile.pilotRank = SWEF.Progression.ProgressionManager.Instance.CurrentRankLabel;
                _localProfile.totalFlightHours = SWEF.Progression.ProgressionManager.Instance.TotalFlightHours;
            }
#endif
        }
        #endregion

        #region Persistence
        private void CreateDefaultProfile()
        {
            _localProfile = new PlayerProfileData
            {
                playerId = Guid.NewGuid().ToString(),
                displayName = defaultDisplayName,
                avatarUrl = defaultAvatarUrl,
                pilotRank = "Cadet",
                totalFlightHours = 0f,
                status = PlayerStatus.Online,
                lastSeen = DateTime.UtcNow.ToString("o")
            };
            SaveLocalProfile();
        }

        private void SaveLocalProfile()
        {
            try
            {
                File.WriteAllText(_persistencePath, JsonUtility.ToJson(_localProfile, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to save player profile — {ex.Message}");
            }
        }

        private void LoadLocalProfile()
        {
            if (!File.Exists(_persistencePath))
            {
                CreateDefaultProfile();
                return;
            }
            try
            {
                string json = File.ReadAllText(_persistencePath);
                _localProfile = JsonUtility.FromJson<PlayerProfileData>(json);
                if (_localProfile == null)
                    CreateDefaultProfile();
                else
                    SyncProgressionData();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to load player profile — {ex.Message}");
                CreateDefaultProfile();
            }
        }
        #endregion
    }
}
