using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    // ── Data Classes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot of a multiplayer room at a point in time.
    /// Serializable so it can be sent over the network as JSON.
    /// </summary>
    [Serializable]
    public class RoomInfo
    {
        /// <summary>Unique identifier for the room (GUID).</summary>
        public string roomId;

        /// <summary>Human-readable room name.</summary>
        public string roomName;

        /// <summary>Display name of the player who created the room.</summary>
        public string hostPlayerName;

        /// <summary>Current number of players in the room.</summary>
        public int playerCount;

        /// <summary>Maximum number of players allowed (default 8).</summary>
        public int maxPlayers = 8;

        /// <summary>Whether the room is publicly listed.</summary>
        public bool isPublic;

        /// <summary>Geographic region for matchmaking (e.g. "us-east").</summary>
        public string region;

        /// <summary>UTC time the room was created.</summary>
        public DateTime createdAt;
    }

    /// <summary>
    /// Configuration supplied by the host when creating a new room.
    /// </summary>
    [Serializable]
    public class RoomSettings
    {
        /// <summary>Maximum players (1–16).</summary>
        public int maxPlayers = 8;

        /// <summary>Whether the room appears in public room lists.</summary>
        public bool isPublic = true;

        /// <summary>Whether spectators are allowed beyond <see cref="maxPlayers"/>.</summary>
        public bool allowSpectators = false;

        /// <summary>Whether weather conditions are synchronised across all players.</summary>
        public bool weatherSync = true;

        /// <summary>Preferred region for matchmaking ("auto" uses server default).</summary>
        public string regionFilter = "auto";
    }

    /// <summary>
    /// Runtime information about a player inside a room.
    /// </summary>
    [Serializable]
    public class PlayerInfo
    {
        /// <summary>Unique player identifier.</summary>
        public string playerId;

        /// <summary>Display name chosen by the player.</summary>
        public string playerName;

        /// <summary>Index into the avatar colour palette (0–7).</summary>
        public int avatarIndex;

        /// <summary>Whether this player is the room host.</summary>
        public bool isHost;

        /// <summary>Whether this player has toggled the Ready state.</summary>
        public bool isReady;

        /// <summary>UTC time the player joined the room.</summary>
        public DateTime joinedAt;
    }

    // ── Room Manager ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton MonoBehaviour that manages multiplayer rooms and lobbies.
    /// Handles room creation, joining, leaving, and player tracking.
    /// Room state is serialised as JSON and replicated to all connected players.
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Singleton instance; set during <see cref="Awake"/>.</summary>
        public static RoomManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the local player creates a new room.</summary>
        public event Action<RoomInfo> OnRoomCreated;

        /// <summary>Fired when the local player successfully joins a room.</summary>
        public event Action<RoomInfo> OnRoomJoined;

        /// <summary>Fired when the local player leaves a room.</summary>
        public event Action OnRoomLeft;

        /// <summary>Fired when a remote player joins the current room.</summary>
        public event Action<PlayerInfo> OnPlayerJoined;

        /// <summary>Fired when a remote player leaves the current room.</summary>
        public event Action<PlayerInfo> OnPlayerLeft;

        /// <summary>Fired when the public room list is refreshed.</summary>
        public event Action<List<RoomInfo>> OnRoomListUpdated;

        // ── Inspector ────────────────────────────────────────────────────────────
        [SerializeField] private MultiplayerSettings multiplayerSettings;

        // ── State ────────────────────────────────────────────────────────────────
        private RoomInfo _currentRoom;
        private string _localPlayerId;
        private readonly List<PlayerInfo> _playersInRoom = new List<PlayerInfo>();

        // Simulated public room list (populated locally for offline/demo purposes)
        private readonly List<RoomInfo> _publicRooms = new List<RoomInfo>();

        /// <summary>The room the local player is currently in, or null if not in a room.</summary>
        public RoomInfo CurrentRoom => _currentRoom;

        /// <summary>Whether the local player is currently inside a room.</summary>
        public bool IsInRoom => _currentRoom != null;

        /// <summary>Whether the local player is the host of the current room.</summary>
        public bool IsHost
        {
            get
            {
                if (_currentRoom == null) return false;
                return _currentRoom.hostPlayerName == GetLocalPlayerInfo()?.playerName;
            }
        }

        /// <summary>Read-only snapshot of players currently in the room.</summary>
        public IReadOnlyList<PlayerInfo> PlayersInRoom => _playersInRoom.AsReadOnly();

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (multiplayerSettings == null)
                multiplayerSettings = FindFirstObjectByType<MultiplayerSettings>();

            _localPlayerId = Guid.NewGuid().ToString("N").Substring(0, 8);

            // Seed a handful of simulated public rooms for the lobby browser
            SeedSimulatedRooms();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new room with the supplied settings.
        /// The local player automatically becomes the host.
        /// </summary>
        /// <param name="settings">Room configuration.</param>
        /// <returns>The newly created <see cref="RoomInfo"/>.</returns>
        public RoomInfo CreateRoom(RoomSettings settings)
        {
            if (IsInRoom)
                LeaveRoom();

            string playerName = multiplayerSettings != null
                ? multiplayerSettings.PlayerName
                : "Pilot";

            var room = new RoomInfo
            {
                roomId         = Guid.NewGuid().ToString(),
                roomName       = $"{playerName}'s Room",
                hostPlayerName = playerName,
                playerCount    = 1,
                maxPlayers     = Mathf.Clamp(settings.maxPlayers, 1, 16),
                isPublic       = settings.isPublic,
                region         = settings.regionFilter == "auto" ? DetectRegion() : settings.regionFilter,
                createdAt      = DateTime.UtcNow
            };

            _currentRoom = room;
            _playersInRoom.Clear();
            _playersInRoom.Add(BuildLocalPlayerInfo(isHost: true));

            if (settings.isPublic)
                _publicRooms.Add(room);

            Debug.Log($"[SWEF][RoomManager] Room created: {room.roomId} '{room.roomName}'");
            OnRoomCreated?.Invoke(room);
            return room;
        }

        /// <summary>
        /// Joins an existing room by its identifier.
        /// </summary>
        /// <param name="roomId">Unique room identifier.</param>
        public void JoinRoom(string roomId)
        {
            if (IsInRoom)
                LeaveRoom();

            // Find the room in the simulated list
            RoomInfo target = _publicRooms.Find(r => r.roomId == roomId);
            if (target == null)
            {
                Debug.LogWarning($"[SWEF][RoomManager] Room '{roomId}' not found.");
                return;
            }

            if (target.playerCount >= target.maxPlayers)
            {
                Debug.LogWarning($"[SWEF][RoomManager] Room '{roomId}' is full.");
                return;
            }

            _currentRoom = target;
            target.playerCount++;
            _playersInRoom.Clear();
            _playersInRoom.Add(BuildLocalPlayerInfo(isHost: false));

            Debug.Log($"[SWEF][RoomManager] Joined room '{target.roomName}'");
            OnRoomJoined?.Invoke(target);
        }

        /// <summary>
        /// Joins the best available public room matching the supplied filter,
        /// or creates one if none exists.
        /// </summary>
        /// <param name="filter">Criteria for the room to join.</param>
        public void JoinRandomRoom(RoomSettings filter)
        {
            RoomInfo best = null;
            int bestSlots = 0;

            foreach (var room in _publicRooms)
            {
                if (!room.isPublic) continue;
                if (room.playerCount >= room.maxPlayers) continue;
                if (filter.regionFilter != "auto" && room.region != filter.regionFilter) continue;

                int slots = room.maxPlayers - room.playerCount;
                if (slots > bestSlots)
                {
                    bestSlots = slots;
                    best = room;
                }
            }

            if (best != null)
                JoinRoom(best.roomId);
            else
                CreateRoom(filter);
        }

        /// <summary>
        /// Removes the local player from the current room and notifies other players.
        /// </summary>
        public void LeaveRoom()
        {
            if (!IsInRoom) return;

            Debug.Log($"[SWEF][RoomManager] Leaving room '{_currentRoom.roomName}'");

            if (_currentRoom.playerCount > 0)
                _currentRoom.playerCount--;

            // Remove host entry from public list if empty
            if (_currentRoom.playerCount <= 0)
                _publicRooms.Remove(_currentRoom);

            _currentRoom = null;
            _playersInRoom.Clear();

            OnRoomLeft?.Invoke();
        }

        /// <summary>
        /// Returns the list of currently available public rooms.
        /// In production this would query the matchmaking server; here it returns the
        /// local simulated list.
        /// </summary>
        /// <returns>List of <see cref="RoomInfo"/> entries.</returns>
        public List<RoomInfo> GetRoomList()
        {
            OnRoomListUpdated?.Invoke(_publicRooms);
            return new List<RoomInfo>(_publicRooms);
        }

        /// <summary>
        /// Kicks a player from the room. Only the host may call this.
        /// </summary>
        /// <param name="playerId">ID of the player to kick.</param>
        public void KickPlayer(string playerId)
        {
            if (!IsHost)
            {
                Debug.LogWarning("[SWEF][RoomManager] KickPlayer: only the host can kick players.");
                return;
            }

            PlayerInfo found = _playersInRoom.Find(p => p.playerId == playerId);
            if (found != null)
            {
                _playersInRoom.Remove(found);
                if (_currentRoom != null && _currentRoom.playerCount > 0)
                    _currentRoom.playerCount--;
                Debug.Log($"[SWEF][RoomManager] Kicked player '{found.playerName}'");
                OnPlayerLeft?.Invoke(found);
            }
        }

        /// <summary>
        /// Simulates a remote player joining (used for testing/editor debug).
        /// </summary>
        /// <param name="info">Player info of the joining player.</param>
        public void SimulatePlayerJoin(PlayerInfo info)
        {
            if (!IsInRoom) return;
            _playersInRoom.Add(info);
            if (_currentRoom != null) _currentRoom.playerCount++;
            OnPlayerJoined?.Invoke(info);
        }

        /// <summary>
        /// Simulates a remote player leaving (used for testing/editor debug).
        /// </summary>
        /// <param name="playerId">ID of the player to remove.</param>
        public void SimulatePlayerLeave(string playerId)
        {
            PlayerInfo found = _playersInRoom.Find(p => p.playerId == playerId);
            if (found == null) return;
            _playersInRoom.Remove(found);
            if (_currentRoom != null && _currentRoom.playerCount > 0)
                _currentRoom.playerCount--;
            OnPlayerLeft?.Invoke(found);
        }

        /// <summary>Returns the <see cref="PlayerInfo"/> for the local player.</summary>
        public PlayerInfo GetLocalPlayerInfo() =>
            _playersInRoom.Find(p => p.playerId == _localPlayerId);

        // ── Internal Helpers ─────────────────────────────────────────────────────

        private PlayerInfo BuildLocalPlayerInfo(bool isHost)
        {
            string name  = multiplayerSettings != null ? multiplayerSettings.PlayerName  : "Pilot";
            int    color = multiplayerSettings != null ? multiplayerSettings.AvatarColorIndex : 0;

            return new PlayerInfo
            {
                playerId    = _localPlayerId,
                playerName  = name,
                avatarIndex = color,
                isHost      = isHost,
                isReady     = false,
                joinedAt    = DateTime.UtcNow
            };
        }

        private static string DetectRegion() => "auto";

        private void SeedSimulatedRooms()
        {
            var names = new[] { "Sky Racers", "Flight Club", "High Altitude", "Cloud Chasers" };
            var regions = new[] { "us-east", "eu-west", "ap-south", "us-west" };
            var rng = new System.Random(42);

            for (int i = 0; i < names.Length; i++)
            {
                int max = 8;
                int count = rng.Next(1, max);
                _publicRooms.Add(new RoomInfo
                {
                    roomId         = Guid.NewGuid().ToString(),
                    roomName       = names[i],
                    hostPlayerName = $"Pilot{rng.Next(1000, 9999)}",
                    playerCount    = count,
                    maxPlayers     = max,
                    isPublic       = true,
                    region         = regions[i],
                    createdAt      = DateTime.UtcNow.AddMinutes(-rng.Next(1, 60))
                });
            }
        }
    }
}
