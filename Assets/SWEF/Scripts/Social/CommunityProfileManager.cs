using System;
using UnityEngine;

namespace SWEF.Social
{
    /// <summary>
    /// Manages the local player profile used by the social feed features.
    /// Data is stored in <see cref="PlayerPrefs"/> under the key prefix
    /// <c>SWEF_Profile_</c> and loaded on first access.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class CommunityProfileManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static CommunityProfileManager Instance { get; private set; }

        private const string KeyPrefix = "SWEF_Profile_";
        private const string KeyJson   = KeyPrefix + "Json";

        // ── Events ───────────────────────────────────────────────────────────
        public event Action OnProfileUpdated;

        // ── Cached profile ───────────────────────────────────────────────────
        private PlayerProfile _profile;

        // ── Nested data class ────────────────────────────────────────────────

        /// <summary>Local player profile data serialised to PlayerPrefs as JSON.</summary>
        [Serializable]
        public class PlayerProfile
        {
            public string displayName       = "Pilot";
            public string avatarId          = string.Empty;
            public int    totalFlights;
            public float  totalDistanceKm;
            public float  maxAltitudeReached;
            public int    totalScreenshots;
            public int    totalLikesReceived;
            /// <summary>ISO 8601 join date string.</summary>
            public string joinDate = DateTime.UtcNow.ToString("O");
        }

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProfile();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Returns the current player profile (never null).</summary>
        public PlayerProfile GetProfile()
        {
            if (_profile == null) LoadProfile();
            return _profile;
        }

        /// <summary>
        /// Applies <paramref name="modifier"/> to the profile, then saves and
        /// fires <see cref="OnProfileUpdated"/>.
        /// </summary>
        public void UpdateProfile(Action<PlayerProfile> modifier)
        {
            if (modifier == null) return;
            modifier(_profile);
            SaveProfile();
            OnProfileUpdated?.Invoke();
        }

        /// <summary>Returns the player's display name, defaulting to "Pilot".</summary>
        public string GetDisplayName()
        {
            string name = GetProfile().displayName;
            return string.IsNullOrWhiteSpace(name) ? "Pilot" : name;
        }

        /// <summary>
        /// Increments an integer stat by name.
        /// Supported names: <c>totalFlights</c>, <c>totalScreenshots</c>,
        /// <c>totalLikesReceived</c>.
        /// </summary>
        public void IncrementStat(string statName)
        {
            UpdateProfile(p =>
            {
                switch (statName)
                {
                    case "totalFlights":       p.totalFlights++;       break;
                    case "totalScreenshots":   p.totalScreenshots++;   break;
                    case "totalLikesReceived": p.totalLikesReceived++; break;
                    default:
                        Debug.LogWarning($"[SWEF] CommunityProfileManager: unknown stat '{statName}'");
                        break;
                }
            });
        }

        // ── Persistence ──────────────────────────────────────────────────────

        private void LoadProfile()
        {
            if (PlayerPrefs.HasKey(KeyJson))
            {
                try
                {
                    _profile = JsonUtility.FromJson<PlayerProfile>(PlayerPrefs.GetString(KeyJson));
                }
                catch
                {
                    _profile = new PlayerProfile();
                }
            }
            else
            {
                _profile = new PlayerProfile();
            }

            if (_profile == null) _profile = new PlayerProfile();
        }

        private void SaveProfile()
        {
            if (_profile == null) return;
            PlayerPrefs.SetString(KeyJson, JsonUtility.ToJson(_profile));
            PlayerPrefs.Save();
        }
    }
}
