using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.SocialHub
{
    /// <summary>
    /// Describes the current relationship state between the local player and another player.
    /// </summary>
    public enum FriendStatus
    {
        /// <summary>No relationship — not a friend.</summary>
        None,
        /// <summary>A friend request has been sent but not yet accepted.</summary>
        RequestSent,
        /// <summary>A friend request has been received and is pending acceptance.</summary>
        RequestReceived,
        /// <summary>Both players have accepted the friend relationship.</summary>
        Friend
    }

    /// <summary>
    /// Persisted record for a single friend or pending friend relationship.
    /// </summary>
    [Serializable]
    public class FriendEntry
    {
        /// <summary>Unique identifier of the other player.</summary>
        public string playerId;

        /// <summary>Display name at the time of last interaction.</summary>
        public string displayName;

        /// <summary>Current relationship status.</summary>
        public FriendStatus status;

        /// <summary>UTC ISO 8601 timestamp of the last status change.</summary>
        public string lastUpdatedUtc;
    }

    /// <summary>
    /// Singleton MonoBehaviour that manages the local player's friend list.
    /// Persists friend entries to <c>Application.persistentDataPath/friends.json</c>.
    /// Friend profile data is synced via <see cref="PlayerProfileManager"/> when available.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    [DefaultExecutionOrder(-25)]
    public class FriendManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static FriendManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a friend request is sent. Parameter: the target's player id.</summary>
        public event Action<string> OnFriendRequestSent;

        /// <summary>Fired when a friend request is accepted. Parameter: the new friend's entry.</summary>
        public event Action<FriendEntry> OnFriendAdded;

        /// <summary>Fired when a friend is removed. Parameter: the removed player's id.</summary>
        public event Action<string> OnFriendRemoved;

        /// <summary>Fired when any change to the friend list occurs.</summary>
        public event Action OnFriendListChanged;

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string SaveFileName = "friends.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Serializable]
        private class FriendsSaveData
        {
            public List<FriendEntry> entries = new List<FriendEntry>();
        }

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly Dictionary<string, FriendEntry> _entries =
            new Dictionary<string, FriendEntry>();

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

        private void OnApplicationQuit() => Save();
        private void OnApplicationPause(bool p) { if (p) Save(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns all friend entries regardless of status.</summary>
        public IReadOnlyList<FriendEntry> GetAllEntries()
        {
            return new List<FriendEntry>(_entries.Values);
        }

        /// <summary>Returns only entries with <see cref="FriendStatus.Friend"/>.</summary>
        public IReadOnlyList<FriendEntry> GetFriends()
        {
            var list = new List<FriendEntry>();
            foreach (var e in _entries.Values)
                if (e.status == FriendStatus.Friend) list.Add(e);
            return list;
        }

        /// <summary>Returns entries where the local player sent an unanswered request.</summary>
        public IReadOnlyList<FriendEntry> GetPendingOutgoing()
        {
            var list = new List<FriendEntry>();
            foreach (var e in _entries.Values)
                if (e.status == FriendStatus.RequestSent) list.Add(e);
            return list;
        }

        /// <summary>Returns entries where the local player received an unanswered request.</summary>
        public IReadOnlyList<FriendEntry> GetPendingIncoming()
        {
            var list = new List<FriendEntry>();
            foreach (var e in _entries.Values)
                if (e.status == FriendStatus.RequestReceived) list.Add(e);
            return list;
        }

        /// <summary>Returns the <see cref="FriendStatus"/> for the given player, or <see cref="FriendStatus.None"/>.</summary>
        public FriendStatus GetStatus(string playerId)
        {
            if (_entries.TryGetValue(playerId, out FriendEntry e)) return e.status;
            return FriendStatus.None;
        }

        /// <summary>
        /// Sends a friend request to the player identified by <paramref name="playerId"/>.
        /// The entry is recorded locally with status <see cref="FriendStatus.RequestSent"/>.
        /// </summary>
        public void SendFriendRequest(string playerId, string displayName)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            if (_entries.ContainsKey(playerId))
            {
                Debug.LogWarning($"[SWEF] FriendManager: Relationship with '{playerId}' already exists.");
                return;
            }

            var entry = new FriendEntry
            {
                playerId       = playerId,
                displayName    = displayName ?? string.Empty,
                status         = FriendStatus.RequestSent,
                lastUpdatedUtc = DateTime.UtcNow.ToString("o")
            };
            _entries[playerId] = entry;
            Save();

            OnFriendRequestSent?.Invoke(playerId);
            OnFriendListChanged?.Invoke();
            Debug.Log($"[SWEF] FriendManager: Friend request sent to '{displayName}' ({playerId}).");
        }

        /// <summary>
        /// Records an incoming friend request from another player.
        /// Typically called when receiving a network message.
        /// </summary>
        public void ReceiveFriendRequest(string playerId, string displayName)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            if (_entries.ContainsKey(playerId)) return; // already tracked

            _entries[playerId] = new FriendEntry
            {
                playerId       = playerId,
                displayName    = displayName ?? string.Empty,
                status         = FriendStatus.RequestReceived,
                lastUpdatedUtc = DateTime.UtcNow.ToString("o")
            };
            Save();
            OnFriendListChanged?.Invoke();
        }

        /// <summary>
        /// Accepts a pending incoming friend request, promoting the entry to
        /// <see cref="FriendStatus.Friend"/>.
        /// </summary>
        public void AcceptFriendRequest(string playerId)
        {
            if (!_entries.TryGetValue(playerId, out FriendEntry entry)) return;
            if (entry.status != FriendStatus.RequestReceived)
            {
                Debug.LogWarning($"[SWEF] FriendManager: Cannot accept — no incoming request from '{playerId}'.");
                return;
            }

            entry.status         = FriendStatus.Friend;
            entry.lastUpdatedUtc = DateTime.UtcNow.ToString("o");
            Save();

            OnFriendAdded?.Invoke(entry);
            OnFriendListChanged?.Invoke();
            Debug.Log($"[SWEF] FriendManager: Accepted friend request from '{entry.displayName}'.");
        }

        /// <summary>
        /// Removes a friend or cancels a pending request in either direction.
        /// </summary>
        public void RemoveFriend(string playerId)
        {
            if (!_entries.ContainsKey(playerId)) return;
            _entries.Remove(playerId);
            Save();

            OnFriendRemoved?.Invoke(playerId);
            OnFriendListChanged?.Invoke();
            Debug.Log($"[SWEF] FriendManager: Removed friend '{playerId}'.");
        }

        /// <summary>
        /// Updates the cached display name for an existing friend entry.
        /// </summary>
        public void UpdateDisplayName(string playerId, string newDisplayName)
        {
            if (_entries.TryGetValue(playerId, out FriendEntry e))
            {
                e.displayName    = newDisplayName;
                e.lastUpdatedUtc = DateTime.UtcNow.ToString("o");
                Save();
                OnFriendListChanged?.Invoke();
            }
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        private void Save()
        {
            try
            {
                var data = new FriendsSaveData();
                data.entries.AddRange(_entries.Values);
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] FriendManager: Failed to save friends data — {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                var data = JsonUtility.FromJson<FriendsSaveData>(File.ReadAllText(SavePath));
                if (data?.entries == null) return;
                foreach (var e in data.entries)
                {
                    if (string.IsNullOrEmpty(e.playerId)) continue;
                    _entries[e.playerId] = e;
                }
                Debug.Log($"[SWEF] FriendManager: Loaded {_entries.Count} friend entries.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] FriendManager: Failed to load friends data — {ex.Message}");
            }
        }
    }
}
