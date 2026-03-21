using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SWEF.Multiplayer;

namespace SWEF.SocialHub
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>Classifies the type of a social notification.</summary>
    public enum SocialNotificationType
    {
        /// <summary>Another player sent a friend request.</summary>
        FriendRequestReceived,
        /// <summary>A pending friend request was accepted.</summary>
        FriendRequestAccepted,
        /// <summary>A friend completed a notable activity (flight, achievement, rank-up).</summary>
        FriendActivity,
        /// <summary>A friend joined the same multiplayer lobby.</summary>
        FriendJoinedLobby,
        /// <summary>Generic social notification.</summary>
        Generic
    }

    /// <summary>A single social notification record.</summary>
    [Serializable]
    public class SocialNotification
    {
        /// <summary>Unique notification id.</summary>
        public string notificationId;
        /// <summary>Kind of notification.</summary>
        public SocialNotificationType type;
        /// <summary>Player id of the actor who triggered the notification.</summary>
        public string actorPlayerId;
        /// <summary>Display name of the actor.</summary>
        public string actorDisplayName;
        /// <summary>Optional context string (activity type, achievement name, etc.).</summary>
        public string contextLabel;
        /// <summary>UTC timestamp, ISO 8601.</summary>
        public string timestampUtc;
        /// <summary>Whether the notification has been read by the player.</summary>
        public bool isRead;
    }

    /// <summary>
    /// Singleton MonoBehaviour that manages social notifications.
    /// Hooks into <see cref="FriendManager"/> and <see cref="SocialActivityFeed"/> to
    /// generate notifications automatically.  Persists to
    /// <c>Application.persistentDataPath/social_notifications.json</c>.
    /// Other systems can post notifications via <see cref="PostNotification"/>.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    [DefaultExecutionOrder(-15)]
    public class SocialNotificationSystem : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SocialNotificationSystem Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a new notification arrives.</summary>
        public event Action<SocialNotification> OnNotificationReceived;

        /// <summary>Fired when the unread count changes.</summary>
        public event Action<int> OnUnreadCountChanged;

        // ── Constants ─────────────────────────────────────────────────────────────
        private const int MaxNotifications = 100;
        private static readonly string SaveFileName = "social_notifications.json";

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<SocialNotification> _notifications = new List<SocialNotification>();
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Serializable]
        private class SaveData
        {
            public List<SocialNotification> notifications = new List<SocialNotification>();
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
            if (FriendManager.Instance != null)
            {
                FriendManager.Instance.OnFriendAdded += OnFriendAdded;
                FriendManager.Instance.OnFriendListChanged += OnFriendListChanged;
            }

            if (SocialActivityFeed.Instance != null)
                SocialActivityFeed.Instance.OnActivityPosted += OnActivityPosted;

            if (NetworkManager2.Instance != null)
                NetworkManager2.Instance.OnPlayerConnected += OnMultiplayerPlayerConnected;
        }

        private void OnApplicationQuit() => Save();
        private void OnApplicationPause(bool p) { if (p) Save(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>All notifications, newest first.</summary>
        public IReadOnlyList<SocialNotification> GetAll() => _notifications;

        /// <summary>Number of unread notifications.</summary>
        public int GetUnreadCount()
        {
            int count = 0;
            foreach (var n in _notifications)
                if (!n.isRead) count++;
            return count;
        }

        /// <summary>Marks all notifications as read and fires <see cref="OnUnreadCountChanged"/>.</summary>
        public void MarkAllRead()
        {
            foreach (var n in _notifications)
                n.isRead = true;
            Save();
            OnUnreadCountChanged?.Invoke(0);
        }

        /// <summary>Marks a single notification as read by id.</summary>
        public void MarkRead(string notificationId)
        {
            foreach (var n in _notifications)
            {
                if (n.notificationId == notificationId && !n.isRead)
                {
                    n.isRead = true;
                    Save();
                    OnUnreadCountChanged?.Invoke(GetUnreadCount());
                    return;
                }
            }
        }

        /// <summary>
        /// Posts a new social notification.
        /// </summary>
        public void PostNotification(SocialNotificationType type, string actorPlayerId,
                                     string actorDisplayName, string contextLabel = "")
        {
            var n = new SocialNotification
            {
                notificationId  = Guid.NewGuid().ToString(),
                type            = type,
                actorPlayerId   = actorPlayerId,
                actorDisplayName = actorDisplayName,
                contextLabel    = contextLabel ?? string.Empty,
                timestampUtc    = DateTime.UtcNow.ToString("o"),
                isRead          = false
            };

            _notifications.Insert(0, n);
            if (_notifications.Count > MaxNotifications)
                _notifications.RemoveAt(_notifications.Count - 1);

            Save();
            OnNotificationReceived?.Invoke(n);
            OnUnreadCountChanged?.Invoke(GetUnreadCount());

            Debug.Log($"[SWEF] SocialNotificationSystem: {type} from '{actorDisplayName}'.");
        }

        /// <summary>Clears all notifications.</summary>
        public void ClearAll()
        {
            _notifications.Clear();
            Save();
            OnUnreadCountChanged?.Invoke(0);
        }

        // ── Internal event handlers ───────────────────────────────────────────────

        private void OnFriendAdded(FriendEntry friend)
        {
            PostNotification(SocialNotificationType.FriendRequestAccepted,
                             friend.playerId, friend.displayName);
        }

        private void OnFriendListChanged()
        {
            // Generate notifications for newly received incoming requests.
            if (FriendManager.Instance == null) return;
            foreach (var entry in FriendManager.Instance.GetPendingIncoming())
            {
                // Only post once (check if we already have a notification for this player).
                bool exists = false;
                foreach (var n in _notifications)
                {
                    if (n.actorPlayerId == entry.playerId &&
                        n.type == SocialNotificationType.FriendRequestReceived)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                    PostNotification(SocialNotificationType.FriendRequestReceived,
                                     entry.playerId, entry.displayName);
            }
        }

        private void OnActivityPosted(ActivityEntry entry)
        {
            // Only generate friend-activity notifications for friends, not the local player.
            string localId = PlayerPrefs.GetString("SWEF_PlayerId", string.Empty);
            if (entry.actorPlayerId == localId) return;
            if (FriendManager.Instance == null) return;
            if (FriendManager.Instance.GetStatus(entry.actorPlayerId) != FriendStatus.Friend) return;

            PostNotification(SocialNotificationType.FriendActivity,
                             entry.actorPlayerId, entry.actorDisplayName,
                             entry.activityType.ToString());
        }

        private void OnMultiplayerPlayerConnected(string playerId)
        {
            if (FriendManager.Instance == null) return;
            if (FriendManager.Instance.GetStatus(playerId) != FriendStatus.Friend) return;

            // Look up display name from cached remote profile.
            string displayName = playerId;
            if (PlayerProfileManager.Instance != null)
            {
                var profile = PlayerProfileManager.Instance.GetRemoteProfile(playerId);
                if (profile != null) displayName = profile.displayName;
            }

            PostNotification(SocialNotificationType.FriendJoinedLobby, playerId, displayName);
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        private void Save()
        {
            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(new SaveData { notifications = _notifications }, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] SocialNotificationSystem: Failed to save — {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (data?.notifications == null) return;
                _notifications.Clear();
                _notifications.AddRange(data.notifications);
                Debug.Log($"[SWEF] SocialNotificationSystem: Loaded {_notifications.Count} notifications ({GetUnreadCount()} unread).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] SocialNotificationSystem: Failed to load — {ex.Message}");
            }
        }
    }
}
