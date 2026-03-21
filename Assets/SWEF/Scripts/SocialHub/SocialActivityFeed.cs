using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SWEF.Progression;
using SWEF.Achievement;
using SWEF.DailyChallenge;
using SWEF.Multiplayer;

namespace SWEF.SocialHub
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>Classifies the kind of social event recorded in the activity feed.</summary>
    public enum ActivityType
    {
        /// <summary>Player completed a flight session.</summary>
        FlightCompleted,
        /// <summary>Player unlocked an achievement.</summary>
        AchievementUnlocked,
        /// <summary>Player levelled up their pilot rank.</summary>
        RankUp,
        /// <summary>Player advanced a season-pass tier.</summary>
        SeasonTierReached,
        /// <summary>Player completed a daily challenge.</summary>
        ChallengeCompleted,
        /// <summary>Two players became friends.</summary>
        BecameFriends,
        /// <summary>Player joined a multiplayer session.</summary>
        JoinedMultiplayer,
        /// <summary>Custom / generic activity.</summary>
        Custom
    }

    /// <summary>
    /// A single entry in the social activity feed.
    /// </summary>
    [Serializable]
    public class ActivityEntry
    {
        /// <summary>Unique id for this entry.</summary>
        public string entryId;
        /// <summary>Player id who performed the activity.</summary>
        public string actorPlayerId;
        /// <summary>Display name of the actor at the time of the event.</summary>
        public string actorDisplayName;
        /// <summary>Kind of activity.</summary>
        public ActivityType activityType;
        /// <summary>Optional secondary context string (achievement name, rank name, etc.).</summary>
        public string contextLabel;
        /// <summary>UTC timestamp of the activity, ISO 8601.</summary>
        public string timestampUtc;
    }

    /// <summary>
    /// Singleton MonoBehaviour that records and persists a social activity feed.
    /// Automatically hooks into <see cref="PlayerProfileManager"/>,
    /// <see cref="SWEF.Achievement.AchievementManager"/>,
    /// <see cref="SWEF.DailyChallenge.DailyChallengeManager"/>,
    /// <see cref="SWEF.Progression.ProgressionManager"/>, and
    /// <see cref="FriendManager"/> to log events.
    /// Keeps a rolling window of at most <see cref="MaxEntries"/> entries.
    /// Persists to <c>Application.persistentDataPath/social_activity.json</c>.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class SocialActivityFeed : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SocialActivityFeed Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired whenever a new activity entry is posted.</summary>
        public event Action<ActivityEntry> OnActivityPosted;

        // ── Constants ─────────────────────────────────────────────────────────────
        private const int MaxEntries = 200;
        private static readonly string SaveFileName = "social_activity.json";

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<ActivityEntry> _entries = new List<ActivityEntry>();
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Serializable]
        private class SaveData
        {
            public List<ActivityEntry> entries = new List<ActivityEntry>();
        }

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
            Load();
        }

        private void Start()
        {
            // Subscribe to live events from other systems.
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnRankUp += OnRankUp;
                ProgressionManager.Instance.OnStatsUpdated += OnFlightStatsUpdated;
            }
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
            if (DailyChallengeManager.Instance != null)
                DailyChallengeManager.Instance.OnChallengeCompleted += OnChallengeCompleted;
            if (SeasonPassManager.Instance != null)
                SeasonPassManager.Instance.OnTierAdvanced += OnSeasonTierAdvanced;
            if (FriendManager.Instance != null)
                FriendManager.Instance.OnFriendAdded += OnFriendAdded;
            if (NetworkManager2.Instance != null)
                NetworkManager2.Instance.OnLobbyJoined += _ => OnLobbyJoined();
        }

        private void OnApplicationQuit() => Save();
        private void OnApplicationPause(bool p) { if (p) Save(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns all feed entries, newest first.</summary>
        public IReadOnlyList<ActivityEntry> GetEntries() => _entries;

        /// <summary>Returns up to <paramref name="count"/> most recent entries.</summary>
        public IReadOnlyList<ActivityEntry> GetRecent(int count)
        {
            int take = Mathf.Min(count, _entries.Count);
            return _entries.GetRange(0, take);
        }

        /// <summary>
        /// Posts a custom activity entry for the local player.
        /// </summary>
        public void PostActivity(ActivityType type, string contextLabel = "")
        {
            string playerId     = PlayerPrefs.GetString("SWEF_PlayerId", "local");
            string displayName  = PlayerPrefs.GetString("SWEF_DisplayName", "Pilot");
            PostActivity(playerId, displayName, type, contextLabel);
        }

        /// <summary>
        /// Posts an activity entry for any player (local or remote/friend).
        /// </summary>
        public void PostActivity(string actorPlayerId, string actorDisplayName,
                                 ActivityType type, string contextLabel = "")
        {
            var entry = new ActivityEntry
            {
                entryId          = Guid.NewGuid().ToString(),
                actorPlayerId    = actorPlayerId,
                actorDisplayName = actorDisplayName,
                activityType     = type,
                contextLabel     = contextLabel ?? string.Empty,
                timestampUtc     = DateTime.UtcNow.ToString("o")
            };

            _entries.Insert(0, entry);
            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(_entries.Count - 1);

            OnActivityPosted?.Invoke(entry);
            Save();
        }

        // ── Private event handlers ────────────────────────────────────────────────

        private bool _flightStatsChangedThisSession;

        private void OnFlightStatsUpdated()
        {
            // Throttle: only post a "flight completed" entry once per session update batch.
            _flightStatsChangedThisSession = true;
        }

        /// <summary>Called externally (e.g. by FlightController) when a flight session ends.</summary>
        public void NotifyFlightCompleted()
        {
            _flightStatsChangedThisSession = false;
            PostActivity(ActivityType.FlightCompleted);
        }

        private void OnRankUp(PilotRankData old, PilotRankData next)
        {
            string label = next != null ? next.rankName : string.Empty;
            PostActivity(ActivityType.RankUp, label);
        }

        private void OnAchievementUnlocked(AchievementDefinition def)
        {
            PostActivity(ActivityType.AchievementUnlocked, def?.titleKey ?? string.Empty);
        }

        private void OnChallengeCompleted(ActiveChallenge challenge)
        {
            PostActivity(ActivityType.ChallengeCompleted, challenge?.challengeId ?? string.Empty);
        }

        private void OnSeasonTierAdvanced(int newTier)
        {
            PostActivity(ActivityType.SeasonTierReached, newTier.ToString());
        }

        private void OnFriendAdded(FriendEntry friend)
        {
            PostActivity(ActivityType.BecameFriends, friend.displayName);
        }

        private void OnLobbyJoined()
        {
            PostActivity(ActivityType.JoinedMultiplayer);
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        private void Save()
        {
            try
            {
                var data = new SaveData { entries = _entries };
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] SocialActivityFeed: Failed to save feed — {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (data?.entries == null) return;
                _entries.Clear();
                _entries.AddRange(data.entries);
                Debug.Log($"[SWEF] SocialActivityFeed: Loaded {_entries.Count} activity entries.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] SocialActivityFeed: Failed to load feed — {ex.Message}");
            }
        }
    }
}
