using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SWEF.Progression;
using SWEF.Achievement;
using SWEF.DailyChallenge;

namespace SWEF.SocialHub
{
    /// <summary>
    /// Singleton MonoBehaviour that builds, caches and vends <see cref="PlayerProfile"/>
    /// instances.  The local player's profile is assembled at runtime from live system
    /// data (<see cref="ProgressionManager"/>, <see cref="AchievementManager"/>,
    /// <see cref="DailyChallengeManager"/>, <see cref="SeasonPassManager"/>,
    /// <see cref="CosmeticUnlockManager"/>).  Remote / friend profiles are loaded from
    /// and persisted to <c>Application.persistentDataPath/social_profiles.json</c>.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public class PlayerProfileManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static PlayerProfileManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired whenever the local player profile is refreshed.</summary>
        public event Action<PlayerProfile> OnLocalProfileUpdated;

        /// <summary>Fired whenever a remote profile cache entry is added or updated.</summary>
        public event Action<PlayerProfile> OnRemoteProfileUpdated;

        // ── Constants ─────────────────────────────────────────────────────────────
        private const string PrefsPlayerId   = "SWEF_PlayerId";
        private const string PrefsDisplayName = "SWEF_DisplayName";
        private const string PrefsAvatarId   = "SWEF_AvatarId";
        private static readonly string SaveFileName = "social_profiles.json";

        // ── State ─────────────────────────────────────────────────────────────────
        private PlayerProfile _localProfile;

        /// <summary>Cached profiles keyed by player id (remote / friend profiles).</summary>
        private readonly Dictionary<string, PlayerProfile> _remoteProfiles =
            new Dictionary<string, PlayerProfile>();

        [Serializable]
        private class ProfileCache
        {
            public List<PlayerProfile> profiles = new List<PlayerProfile>();
        }

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

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

            LoadRemoteProfileCache();
        }

        private void Start()
        {
            // Build the local profile once all systems have initialised (DefaultExecutionOrder -30).
            RefreshLocalProfile();

            // Subscribe to live updates so the local profile stays fresh.
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnXPGained    += (_, __) => RefreshLocalProfile();
                ProgressionManager.Instance.OnRankUp      += (_, __)  => RefreshLocalProfile();
                ProgressionManager.Instance.OnStatsUpdated += RefreshLocalProfile;
            }
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked += _ => RefreshLocalProfile();
            if (DailyChallengeManager.Instance != null)
                DailyChallengeManager.Instance.OnStreakUpdated += _ => RefreshLocalProfile();
            if (SeasonPassManager.Instance != null)
            {
                SeasonPassManager.Instance.OnTierAdvanced += _ => RefreshLocalProfile();
                SeasonPassManager.Instance.OnRewardClaimed += _ => RefreshLocalProfile();
            }
        }

        private void OnApplicationQuit() => SaveRemoteProfileCache();
        private void OnApplicationPause(bool p) { if (p) SaveRemoteProfileCache(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the local player's profile, refreshing it from live system data first.
        /// Never returns null.
        /// </summary>
        public PlayerProfile GetLocalProfile()
        {
            RefreshLocalProfile();
            return _localProfile;
        }

        /// <summary>
        /// Updates the local player's display name and persists it to PlayerPrefs.
        /// </summary>
        /// <param name="newName">New display name (2–20 characters, trimmed).</param>
        /// <returns><c>true</c> if the name was accepted; <c>false</c> if validation failed.</returns>
        public bool SetDisplayName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return false;
            newName = newName.Trim();
            if (newName.Length < 2 || newName.Length > 20) return false;

            PlayerPrefs.SetString(PrefsDisplayName, newName);
            PlayerPrefs.Save();
            RefreshLocalProfile();
            return true;
        }

        /// <summary>
        /// Sets the local player's selected avatar identifier and persists it.
        /// </summary>
        public void SetAvatarId(string avatarId)
        {
            PlayerPrefs.SetString(PrefsAvatarId, avatarId ?? string.Empty);
            PlayerPrefs.Save();
            RefreshLocalProfile();
        }

        /// <summary>
        /// Stores or updates a remote player profile in the local cache.
        /// Call this when receiving profile data from a remote source (friend sync, lobby).
        /// </summary>
        public void CacheRemoteProfile(PlayerProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.playerId)) return;
            _remoteProfiles[profile.playerId] = profile;
            OnRemoteProfileUpdated?.Invoke(profile);
            SaveRemoteProfileCache();
        }

        /// <summary>
        /// Retrieves a cached remote profile by player id, or <c>null</c> if unknown.
        /// </summary>
        public PlayerProfile GetRemoteProfile(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null;
            _remoteProfiles.TryGetValue(playerId, out PlayerProfile p);
            return p;
        }

        /// <summary>
        /// Returns all cached remote player profiles.
        /// </summary>
        public IReadOnlyList<PlayerProfile> GetAllRemoteProfiles()
        {
            var list = new List<PlayerProfile>(_remoteProfiles.Values);
            return list;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the local player profile from live system data and fires
        /// <see cref="OnLocalProfileUpdated"/>.
        /// </summary>
        private void RefreshLocalProfile()
        {
            _localProfile = BuildLocalProfile();
            OnLocalProfileUpdated?.Invoke(_localProfile);
        }

        private PlayerProfile BuildLocalProfile()
        {
            var p = new PlayerProfile();

            // ── Identity ──────────────────────────────────────────────────────
            p.playerId    = GetOrCreatePlayerId();
            p.displayName = PlayerPrefs.GetString(PrefsDisplayName, "Pilot");
            p.avatarId    = PlayerPrefs.GetString(PrefsAvatarId, string.Empty);

            // ── Progression ───────────────────────────────────────────────────
            var progression = ProgressionManager.Instance;
            if (progression != null)
            {
                p.pilotRankLevel = progression.CurrentRankLevel;
                var rank = progression.GetCurrentRank();
                p.pilotRankName  = rank != null ? rank.rankName : string.Empty;
                p.totalXP        = progression.GetTotalXP();
                p.totalFlightTimeMinutes = progression.TotalFlightTimeSeconds / 60f;
                p.totalDistanceKm = progression.TotalDistanceKm;
                p.maxAltitudeMeters = progression.TopAltitude;
                // TopSpeedMps → km/h
                p.maxSpeedKmh    = progression.TopSpeedMps * 3.6f;
                p.totalFlights   = progression.TotalFlightsCompleted;
            }

            // ── Achievements ──────────────────────────────────────────────────
            var achievements = AchievementManager.Instance;
            if (achievements != null)
            {
                var states = achievements.GetAllStates();
                p.achievementsTotal    = states.Count;
                int unlocked = 0;
                foreach (var s in states)
                    if (s.unlocked) unlocked++;
                p.achievementsUnlocked = unlocked;
            }

            // ── Daily Streak ──────────────────────────────────────────────────
            if (DailyChallengeManager.Instance != null)
                p.dailyStreak = DailyChallengeManager.Instance.GetDailyStreak();

            // ── Season Pass ───────────────────────────────────────────────────
            if (SeasonPassManager.Instance != null)
            {
                p.seasonTier = SeasonPassManager.Instance.GetCurrentTier();
                p.isPremium  = SeasonPassManager.Instance.IsPremiumUnlocked;
            }

            // ── Equipped cosmetics ────────────────────────────────────────────
            p.equippedCosmetics = new Dictionary<string, string>();
            var cosmetics = CosmeticUnlockManager.Instance;
            if (cosmetics != null)
            {
                foreach (CosmeticCategory cat in Enum.GetValues(typeof(CosmeticCategory)))
                {
                    string id = cosmetics.GetEquipped(cat);
                    if (!string.IsNullOrEmpty(id))
                        p.equippedCosmetics[cat.ToString()] = id;
                }
                // Derive titleId from equipped NameTag
                p.titleId = cosmetics.GetEquipped(CosmeticCategory.NameTag);
            }

            p.FlushCosmeticsDict();
            return p;
        }

        private static string GetOrCreatePlayerId()
        {
            string id = PlayerPrefs.GetString(PrefsPlayerId, string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                PlayerPrefs.SetString(PrefsPlayerId, id);
                PlayerPrefs.Save();
            }
            return id;
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        private void SaveRemoteProfileCache()
        {
            try
            {
                var cache = new ProfileCache();
                foreach (var kv in _remoteProfiles)
                {
                    kv.Value.FlushCosmeticsDict();
                    cache.profiles.Add(kv.Value);
                }
                File.WriteAllText(SavePath, JsonUtility.ToJson(cache, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] PlayerProfileManager: Failed to save profile cache — {ex.Message}");
            }
        }

        private void LoadRemoteProfileCache()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                var cache = JsonUtility.FromJson<ProfileCache>(File.ReadAllText(SavePath));
                if (cache?.profiles == null) return;
                foreach (var profile in cache.profiles)
                {
                    if (string.IsNullOrEmpty(profile.playerId)) continue;
                    profile.RebuildCosmeticsDict();
                    _remoteProfiles[profile.playerId] = profile;
                }
                Debug.Log($"[SWEF] PlayerProfileManager: Loaded {_remoteProfiles.Count} cached remote profiles.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] PlayerProfileManager: Failed to load profile cache — {ex.Message}");
            }
        }
    }
}
