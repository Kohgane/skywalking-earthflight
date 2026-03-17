using System.Collections.Generic;
using UnityEngine;
using SWEF.Flight;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Core multiplayer orchestrator. Broadcasts the local player's flight state
    /// and manages the lifecycle of remote player avatars. Uses an abstract event-based
    /// transport layer so any networking backend (WebSocket, Photon, etc.) can be
    /// plugged in by subscribing to <see cref="OnLocalStateBroadcast"/>.
    /// </summary>
    public class MultiplayerManager : MonoBehaviour
    {
        [Header("Local Player Sources")]
        [SerializeField] private FlightController localFlight;
        [SerializeField] private AltitudeController localAltitude;

        [Header("Avatar Spawning")]
        [SerializeField] private GameObject avatarPrefab;
        [SerializeField] private Transform avatarParent;

        [Header("Timing")]
        [SerializeField] private float broadcastInterval = 0.1f;  // 10 Hz
        [SerializeField] private float stalePlayerTimeout = 10f;

        private readonly Dictionary<string, PlayerAvatar> _remotePlayers = new();
        private float _broadcastTimer;
        private string _localPlayerId;

        /// <summary>
        /// Fired every <see cref="broadcastInterval"/> seconds with the local player ID
        /// and current <see cref="AvatarState"/>. Subscribe to send data over the network.
        /// </summary>
        public event System.Action<string, AvatarState> OnLocalStateBroadcast;

        /// <summary>Network identifier generated for the local player this session.</summary>
        public string LocalPlayerId => _localPlayerId;

        /// <summary>Number of currently tracked remote players.</summary>
        public int RemotePlayerCount => _remotePlayers.Count;

        private void Awake()
        {
            if (localFlight == null)
                localFlight = FindFirstObjectByType<FlightController>();

            if (localAltitude == null)
                localAltitude = FindFirstObjectByType<AltitudeController>();

            // Generate a short unique ID for this session's local player.
            _localPlayerId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private void Update()
        {
            _broadcastTimer += Time.deltaTime;
            if (_broadcastTimer >= broadcastInterval)
            {
                BroadcastLocalState();
                _broadcastTimer = 0f;
            }

            CleanupStalePlayers();
        }

        /// <summary>
        /// Builds an <see cref="AvatarState"/> from the local controllers and fires
        /// <see cref="OnLocalStateBroadcast"/> so a transport layer can send it.
        /// </summary>
        private void BroadcastLocalState()
        {
            var state = new AvatarState
            {
                position = transform.position,
                rotation = transform.rotation,
                speedMps = localFlight != null ? localFlight.CurrentSpeedMps : 0f,
                altitudeMeters = localAltitude != null ? localAltitude.CurrentAltitudeMeters : 0f,
                displayName = "Me"
            };

            OnLocalStateBroadcast?.Invoke(_localPlayerId, state);
        }

        /// <summary>
        /// Call this from the network transport whenever a remote player state arrives.
        /// Spawns a new avatar prefab on first sight of a player; otherwise updates
        /// the existing one.
        /// </summary>
        /// <param name="playerId">Unique identifier of the remote player.</param>
        /// <param name="state">Latest state snapshot from the network.</param>
        public void ReceiveRemoteState(string playerId, AvatarState state)
        {
            if (playerId == _localPlayerId)
                return;

            if (!_remotePlayers.TryGetValue(playerId, out PlayerAvatar avatar))
            {
                if (avatarPrefab == null)
                {
                    Debug.LogWarning("[SWEF] MultiplayerManager: avatarPrefab is not assigned.");
                    return;
                }

                GameObject go = Instantiate(avatarPrefab,
                    state.position,
                    state.rotation,
                    avatarParent);

                avatar = go.GetComponent<PlayerAvatar>();
                if (avatar == null)
                {
                    Debug.LogWarning("[SWEF] MultiplayerManager: avatarPrefab has no PlayerAvatar component.");
                    Destroy(go);
                    return;
                }

                avatar.Initialize(playerId, state);
                _remotePlayers[playerId] = avatar;
                Debug.Log($"[SWEF] Remote player joined: {playerId}");
            }
            else
            {
                avatar.UpdateState(state);
            }
        }

        /// <summary>
        /// Manually removes and destroys the avatar for the given player ID.
        /// </summary>
        /// <param name="playerId">Player to remove.</param>
        public void RemovePlayer(string playerId)
        {
            if (_remotePlayers.TryGetValue(playerId, out PlayerAvatar avatar))
            {
                if (avatar != null)
                    Destroy(avatar.gameObject);
                _remotePlayers.Remove(playerId);
                Debug.Log($"[SWEF] Remote player removed: {playerId}");
            }
        }

        /// <summary>Removes and destroys all remote player avatars.</summary>
        public void RemoveAllPlayers()
        {
            foreach (var kvp in _remotePlayers)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            _remotePlayers.Clear();
        }

        /// <summary>
        /// Iterates all tracked remote players and removes any whose last update
        /// is older than <see cref="stalePlayerTimeout"/> seconds.
        /// </summary>
        private void CleanupStalePlayers()
        {
            var stale = new List<string>();

            foreach (var kvp in _remotePlayers)
            {
                if (kvp.Value == null || kvp.Value.TimeSinceLastUpdate > stalePlayerTimeout)
                    stale.Add(kvp.Key);
            }

            foreach (string id in stale)
                RemovePlayer(id);
        }

        private void OnDestroy()
        {
            RemoveAllPlayers();
        }
    }
}
