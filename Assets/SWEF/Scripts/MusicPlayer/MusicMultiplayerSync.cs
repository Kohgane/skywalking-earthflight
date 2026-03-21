using System;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Multiplayer;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Multiplayer shared-listening experience for the In-Flight Music Player.
    /// <para>
    /// Modes:
    /// <list type="bullet">
    ///   <item><see cref="MultiplayerMusicMode.Solo"/>        — independent playback per player.</item>
    ///   <item><see cref="MultiplayerMusicMode.SharedRadio"/> — host's state is mirrored to all room members.</item>
    ///   <item><see cref="MultiplayerMusicMode.DJMode"/>      — a designated DJ controls the playlist; others listen.</item>
    ///   <item><see cref="MultiplayerMusicMode.VotePlay"/>    — players vote on the next track from a shared queue.</item>
    /// </list>
    /// </para>
    /// Uses <see cref="NetworkManager2"/> and <see cref="RoomManager"/> for room awareness.
    /// </summary>
    public class MusicMultiplayerSync : MonoBehaviour
    {
        // ── Multiplayer mode enum ─────────────────────────────────────────────────
        /// <summary>Defines how music is shared across a multiplayer room.</summary>
        public enum MultiplayerMusicMode
        {
            /// <summary>Each player controls their own music independently.</summary>
            Solo,
            /// <summary>Host's playback state is broadcast to and mirrored by all room members.</summary>
            SharedRadio,
            /// <summary>A designated DJ controls the playlist; others listen passively.</summary>
            DJMode,
            /// <summary>All players vote on the next track from a shared candidate queue.</summary>
            VotePlay
        }

        // ── Sync payload ──────────────────────────────────────────────────────────
        /// <summary>
        /// Serialisable payload that carries music playback state across the network.
        /// </summary>
        [Serializable]
        public class MusicSyncPayload
        {
            /// <summary>Track ID of the currently playing or requested track.</summary>
            public string trackId;

            /// <summary>Normalised playback position (0–1).</summary>
            public float playbackPosition;

            /// <summary>Whether the track is actively playing on the sender's client.</summary>
            public bool isPlaying;

            /// <summary>Player ID of the room host or DJ.</summary>
            public string hostPlayerId;

            /// <summary>
            /// Vote map used in <see cref="MultiplayerMusicMode.VotePlay"/> mode.
            /// Key = trackId, Value = vote count.
            /// </summary>
            public SerializableVoteMap votes = new SerializableVoteMap();
        }

        /// <summary>A serialisable wrapper around a vote dictionary.</summary>
        [Serializable]
        public class SerializableVoteMap
        {
            public List<string> trackIds = new List<string>();
            public List<int>    counts   = new List<int>();

            /// <summary>Converts to a standard Dictionary for easy lookup.</summary>
            public Dictionary<string, int> ToDictionary()
            {
                var dict = new Dictionary<string, int>();
                for (int i = 0; i < Mathf.Min(trackIds.Count, counts.Count); i++)
                    dict[trackIds[i]] = counts[i];
                return dict;
            }
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References (auto-found if null)")]
        [Tooltip("NetworkManager2 reference. Auto-found if null.")]
        [SerializeField] private NetworkManager2 networkManager;

        [Tooltip("RoomManager reference. Auto-found if null.")]
        [SerializeField] private RoomManager roomManager;

        [Header("Settings")]
        [Tooltip("How often the host broadcasts its playback state (seconds).")]
        [SerializeField] private float broadcastInterval = 5f;

        [Tooltip("Acceptable playback-position deviation in seconds before a client re-syncs.")]
        [SerializeField] private float syncToleranceSeconds = 2f;

        // ── Private state ─────────────────────────────────────────────────────────
        private MultiplayerMusicMode    _mode           = MultiplayerMusicMode.Solo;
        private MusicSyncPayload        _latestPayload;
        private float                   _broadcastTimer;
        private Dictionary<string, int> _votes          = new Dictionary<string, int>();
        private string                  _localPlayerId  = string.Empty;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the active multiplayer mode changes.</summary>
        public event Action<MultiplayerMusicMode> OnModeChanged;

        /// <summary>Fired when a new sync payload is received from the host.</summary>
        public event Action<MusicSyncPayload> OnPayloadReceived;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Returns <c>true</c> when the local player is the room host.</summary>
        public bool IsHost => networkManager != null && networkManager.IsHost;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (networkManager == null)
                networkManager = NetworkManager2.Instance ?? FindFirstObjectByType<NetworkManager2>();
            if (roomManager == null)
                roomManager = RoomManager.Instance ?? FindFirstObjectByType<RoomManager>();
        }

        private void Start()
        {
            // Cache local player ID
            if (roomManager != null)
            {
                PlayerInfo local = roomManager.GetLocalPlayerInfo();
                _localPlayerId = local != null ? local.playerId : string.Empty;
            }
        }

        private void Update()
        {
            if (_mode == MultiplayerMusicMode.Solo) return;

            // Host broadcasts at regular intervals
            if (IsHost)
            {
                _broadcastTimer += Time.deltaTime;
                if (_broadcastTimer >= broadcastInterval)
                {
                    _broadcastTimer = 0f;
                    BroadcastPlaybackState();
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Switches the active multiplayer music mode.</summary>
        public void SetMultiplayerMode(MultiplayerMusicMode newMode)
        {
            if (_mode == newMode) return;
            _mode = newMode;
            _votes.Clear();
            OnModeChanged?.Invoke(newMode);
            Debug.Log($"[SWEF][MusicMultiplayerSync] Mode changed to {newMode}");
        }

        /// <summary>Returns the currently active multiplayer music mode.</summary>
        public MultiplayerMusicMode GetCurrentMode() => _mode;

        /// <summary>
        /// Assembles the current playback state into a <see cref="MusicSyncPayload"/> and
        /// logs it for broadcast. In a full networking implementation, this payload would be
        /// serialised and sent via the room's data channel.
        /// </summary>
        public void BroadcastPlaybackState()
        {
            if (MusicPlayerManager.Instance == null) return;

            MusicPlayerState state = MusicPlayerManager.Instance.State;
            var payload = new MusicSyncPayload
            {
                trackId         = state.currentTrackId,
                playbackPosition = state.playbackPosition,
                isPlaying       = state.isPlaying,
                hostPlayerId    = _localPlayerId
            };

            // In production, serialize to JSON and send via the network layer:
            // string json = JsonUtility.ToJson(payload);
            // networkManager.BroadcastToRoom(json);

            _latestPayload = payload;
            Debug.Log($"[SWEF][MusicMultiplayerSync] Broadcasting: track={payload.trackId} pos={payload.playbackPosition:F2}");
        }

        /// <summary>
        /// Applies an incoming sync payload from the host.
        /// Snaps playback position if the deviation exceeds <see cref="syncToleranceSeconds"/>.
        /// </summary>
        public void ReceivePlaybackState(MusicSyncPayload payload)
        {
            if (payload == null) return;
            if (IsHost) return;            // host never applies its own broadcast
            if (_mode == MultiplayerMusicMode.Solo) return;

            _latestPayload = payload;
            OnPayloadReceived?.Invoke(payload);

            if (MusicPlayerManager.Instance == null) return;

            MusicPlayerState state = MusicPlayerManager.Instance.State;

            // Switch track if different
            if (payload.trackId != state.currentTrackId && !string.IsNullOrEmpty(payload.trackId))
            {
                MusicPlayerManager.Instance.PlayTrack(payload.trackId);
                MusicPlayerManager.Instance.Seek(payload.playbackPosition);
                return;
            }

            // Sync position if drift is too large
            // Compare both positions in normalised [0,1] space; use a fixed normalised tolerance.
            const float normalisedTolerance = 0.01f; // 1 % of track length
            float normDrift = Mathf.Abs(payload.playbackPosition - state.playbackPosition);
            if (normDrift > normalisedTolerance)
                MusicPlayerManager.Instance.Seek(payload.playbackPosition);

            // Play/pause parity
            if (payload.isPlaying && state.isPaused)
                MusicPlayerManager.Instance.Resume();
            else if (!payload.isPlaying && state.isPlaying)
                MusicPlayerManager.Instance.Pause();
        }

        /// <summary>
        /// Casts a vote for the given track in <see cref="MultiplayerMusicMode.VotePlay"/> mode.
        /// When the voting threshold is reached (any track gets &gt;50 % of room votes),
        /// the track is queued for playback.
        /// </summary>
        public void VoteForTrack(string trackId)
        {
            if (_mode != MultiplayerMusicMode.VotePlay) return;
            if (string.IsNullOrEmpty(trackId)) return;

            if (!_votes.ContainsKey(trackId)) _votes[trackId] = 0;
            _votes[trackId]++;

            int playerCount = GetRoomPlayerCount();
            if (playerCount > 0 && _votes[trackId] > playerCount / 2)
            {
                // Majority reached — apply the winning track
                ApplyVoteWinner(trackId);
            }

            Debug.Log($"[SWEF][MusicMultiplayerSync] Vote for '{trackId}': {_votes[trackId]}/{playerCount}");
        }

        /// <summary>Returns a snapshot of current vote counts keyed by track ID.</summary>
        public Dictionary<string, int> GetVoteResults() => new Dictionary<string, int>(_votes);

        /// <summary>Returns the latest received sync payload (may be null).</summary>
        public MusicSyncPayload GetLatestPayload() => _latestPayload;

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ApplyVoteWinner(string trackId)
        {
            if (MusicPlayerManager.Instance == null) return;
            MusicPlayerManager.Instance.PlayTrack(trackId);
            _votes.Clear();
        }

        private int GetRoomPlayerCount()
        {
            if (roomManager == null) return 1;
            return roomManager.PlayersInRoom != null ? roomManager.PlayersInRoom.Count : 1;
        }
    }
}
