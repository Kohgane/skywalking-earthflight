using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Singleton that manages the local player's friend list.
    /// Handles add/remove, online status, invites, and mutual flight tracking.
    /// Persisted to <c>friends_list.json</c>.
    /// </summary>
    public class FriendSystemController : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared instance of the friend system controller.</summary>
        public static FriendSystemController Instance { get; private set; }
        #endregion

        #region Constants
        private const string PersistenceFileName = "friends_list.json";
        #endregion

        #region Events
        /// <summary>Fired when a new friend is successfully added.</summary>
        public event Action<FriendData> OnFriendAdded;
        /// <summary>Fired when a friend is removed from the list.</summary>
        public event Action<string> OnFriendRemoved;
        /// <summary>Fired when a friend's status changes to online.</summary>
        public event Action<string> OnFriendOnline;
        /// <summary>Fired when a friend's status changes to offline.</summary>
        public event Action<string> OnFriendOffline;
        /// <summary>Fired when a flight invitation is received from a friend.</summary>
        public event Action<string, string> OnInviteReceived;
        #endregion

        #region Private State
        private readonly List<FriendData> _friends = new List<FriendData>();
        private readonly List<string> _pendingInvites = new List<string>();
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
            LoadFriendList();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Friend Management
        /// <summary>
        /// Adds a player to the friend list.
        /// </summary>
        /// <param name="profile">Profile of the player to add.</param>
        public void AddFriend(PlayerProfileData profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("[SWEF] Multiplayer: AddFriend called with null profile.");
                return;
            }
            if (IsFriend(profile.playerId))
            {
                Debug.LogWarning($"[SWEF] Multiplayer: {profile.playerId} is already a friend.");
                return;
            }

            var friendData = new FriendData
            {
                friendId = profile.playerId,
                profile = profile,
                friendSince = DateTime.UtcNow.ToString("o"),
                isFavorite = false,
                mutualFlightCount = 0
            };

            _friends.Add(friendData);
            SaveFriendList();

            MultiplayerAnalytics.RecordFriendAdded();
            MultiplayerBridge.OnFriendAdded(friendData);

            OnFriendAdded?.Invoke(friendData);
            Debug.Log($"[SWEF] Multiplayer: Friend added — {profile.displayName}");
        }

        /// <summary>
        /// Removes a player from the friend list.
        /// </summary>
        /// <param name="friendId">The friend's player ID.</param>
        public void RemoveFriend(string friendId)
        {
            if (string.IsNullOrEmpty(friendId))
            {
                Debug.LogWarning("[SWEF] Multiplayer: RemoveFriend called with null/empty friendId.");
                return;
            }

            int removed = _friends.RemoveAll(f => f.friendId == friendId);
            if (removed == 0)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Friend {friendId} not found.");
                return;
            }

            SaveFriendList();
            MultiplayerAnalytics.RecordFriendRemoved();
            OnFriendRemoved?.Invoke(friendId);
        }

        /// <summary>
        /// Returns the full friend list.
        /// </summary>
        public List<FriendData> GetFriendList() => new List<FriendData>(_friends);

        /// <summary>
        /// Returns only friends whose status is <see cref="PlayerStatus.Online"/> or
        /// <see cref="PlayerStatus.InFlight"/>.
        /// </summary>
        public List<FriendData> GetOnlineFriends() =>
            _friends.FindAll(f => f.profile != null &&
                (f.profile.status == PlayerStatus.Online || f.profile.status == PlayerStatus.InFlight));

        /// <summary>
        /// Checks whether the given player is on the local friend list.
        /// </summary>
        /// <param name="playerId">Player ID to check.</param>
        /// <returns>True if they are a friend.</returns>
        public bool IsFriend(string playerId) =>
            !string.IsNullOrEmpty(playerId) && _friends.Exists(f => f.friendId == playerId);

        /// <summary>
        /// Updates a friend's cached profile and fires online/offline events as appropriate.
        /// </summary>
        /// <param name="updatedProfile">The updated profile snapshot.</param>
        public void UpdateFriendProfile(PlayerProfileData updatedProfile)
        {
            if (updatedProfile == null) return;
            var friend = _friends.Find(f => f.friendId == updatedProfile.playerId);
            if (friend == null) return;

            PlayerStatus oldStatus = friend.profile?.status ?? PlayerStatus.Offline;
            friend.profile = updatedProfile;
            SaveFriendList();

            if (oldStatus == PlayerStatus.Offline &&
                updatedProfile.status != PlayerStatus.Offline)
                OnFriendOnline?.Invoke(updatedProfile.playerId);
            else if (oldStatus != PlayerStatus.Offline &&
                     updatedProfile.status == PlayerStatus.Offline)
                OnFriendOffline?.Invoke(updatedProfile.playerId);
        }

        /// <summary>
        /// Increments the mutual flight count for a friend.
        /// Called at the end of a shared flight session.
        /// </summary>
        /// <param name="friendId">The friend's player ID.</param>
        public void IncrementMutualFlightCount(string friendId)
        {
            var friend = _friends.Find(f => f.friendId == friendId);
            if (friend == null) return;
            friend.mutualFlightCount++;
            SaveFriendList();
        }
        #endregion

        #region Invitations
        /// <summary>
        /// Sends a flight session invitation to a friend.
        /// </summary>
        /// <param name="friendId">Target friend's player ID.</param>
        public void InviteToFlight(string friendId)
        {
            if (!IsFriend(friendId))
            {
                Debug.LogWarning($"[SWEF] Multiplayer: InviteToFlight — {friendId} is not a friend.");
                return;
            }
            if (!MultiplayerSessionManager.Instance || !MultiplayerSessionManager.Instance.IsInSession)
            {
                Debug.LogWarning("[SWEF] Multiplayer: InviteToFlight — not in an active session.");
                return;
            }

            MultiplayerAnalytics.RecordFriendInvited();
            // In a real network layer the invite would be sent via NetworkManager2.
            Debug.Log($"[SWEF] Multiplayer: Invite sent to {friendId}");
        }

        /// <summary>
        /// Accepts a pending flight invitation, joining the session.
        /// </summary>
        /// <param name="sessionId">Session ID from the invitation.</param>
        public void AcceptInvite(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                Debug.LogWarning("[SWEF] Multiplayer: AcceptInvite called with null/empty sessionId.");
                return;
            }

            _pendingInvites.Remove(sessionId);
            MultiplayerSessionManager.Instance?.JoinSession(sessionId);
        }

        /// <summary>
        /// Declines a pending flight invitation.
        /// </summary>
        /// <param name="sessionId">Session ID from the invitation.</param>
        public void DeclineInvite(string sessionId)
        {
            _pendingInvites.Remove(sessionId);
            Debug.Log($"[SWEF] Multiplayer: Invite to {sessionId} declined.");
        }

        /// <summary>
        /// Called by the network layer when a remote player sends an invite to the local player.
        /// </summary>
        /// <param name="fromPlayerId">Sender's player ID.</param>
        /// <param name="sessionId">Session to join.</param>
        public void ReceiveInvite(string fromPlayerId, string sessionId)
        {
            if (!IsFriend(fromPlayerId)) return;
            if (!_pendingInvites.Contains(sessionId))
                _pendingInvites.Add(sessionId);
            OnInviteReceived?.Invoke(fromPlayerId, sessionId);
        }
        #endregion

        #region Persistence
        private void SaveFriendList()
        {
            try
            {
                var wrapper = new FriendListWrapper { friends = _friends };
                File.WriteAllText(_persistencePath, JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to save friend list — {ex.Message}");
            }
        }

        private void LoadFriendList()
        {
            if (!File.Exists(_persistencePath)) return;
            try
            {
                string json = File.ReadAllText(_persistencePath);
                var wrapper = JsonUtility.FromJson<FriendListWrapper>(json);
                if (wrapper?.friends != null)
                    _friends.AddRange(wrapper.friends);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to load friend list — {ex.Message}");
            }
        }

        [Serializable]
        private class FriendListWrapper
        {
            public List<FriendData> friends;
        }
        #endregion
    }
}
