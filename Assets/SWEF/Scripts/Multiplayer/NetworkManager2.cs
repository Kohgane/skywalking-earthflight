using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.Multiplayer
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes the measured quality of a player's network connection.
    /// </summary>
    public enum ConnectionQuality
    {
        /// <summary>RTT &lt; 50 ms, packet loss &lt; 1 %.</summary>
        Excellent,
        /// <summary>RTT 50–100 ms, packet loss &lt; 3 %.</summary>
        Good,
        /// <summary>RTT 100–200 ms, packet loss &lt; 8 %.</summary>
        Fair,
        /// <summary>RTT &gt; 200 ms or packet loss ≥ 8 %.</summary>
        Poor
    }

    // ── Data Classes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes a lobby that players can create, browse, or join.
    /// </summary>
    [Serializable]
    public class LobbyInfo
    {
        /// <summary>6-character alphanumeric room code used to join directly.</summary>
        public string roomCode;
        /// <summary>Unique lobby GUID.</summary>
        public string lobbyId;
        /// <summary>Display name of the lobby.</summary>
        public string lobbyName;
        /// <summary>Display name of the current host player.</summary>
        public string hostPlayerName;
        /// <summary>Current number of players (excluding spectators).</summary>
        public int playerCount;
        /// <summary>Maximum players allowed in this lobby.</summary>
        public int maxPlayers = 8;
        /// <summary>Number of spectator slots filled.</summary>
        public int spectatorCount;
        /// <summary>Maximum spectator slots (default 4).</summary>
        public int maxSpectators = 4;
        /// <summary>Whether the lobby is open to public browsing.</summary>
        public bool isPublic;
        /// <summary>UTC time the lobby was created.</summary>
        public DateTime createdAt;
    }

    /// <summary>
    /// Snapshot of a connected player's network metrics.
    /// </summary>
    [Serializable]
    public class PlayerNetworkMetrics
    {
        /// <summary>Player network identifier.</summary>
        public string playerId;
        /// <summary>Round-trip time in milliseconds.</summary>
        public float rttMs;
        /// <summary>Packet loss as a fraction (0 = none, 1 = 100 % loss).</summary>
        public float packetLoss;
        /// <summary>Jitter (standard deviation of RTT) in milliseconds.</summary>
        public float jitterMs;
        /// <summary>Derived connection quality tier.</summary>
        public ConnectionQuality quality;
        /// <summary>Timestamp of this metrics snapshot.</summary>
        public float timestamp;
    }

    // ── NetworkManager2 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Advanced network manager that complements <see cref="MultiplayerManager"/> with
    /// Phase 33 features: 6-character room-code lobbies, automatic host migration,
    /// UDP NAT punch-through with relay fallback, and per-player connection-quality
    /// monitoring (RTT, packet loss, jitter).
    /// </summary>
    public class NetworkManager2 : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static NetworkManager2 Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Session Settings")]
        [Tooltip("Maximum players per session (excluding spectators).")]
        [SerializeField] private int maxPlayers = 8;

        [Tooltip("Maximum spectator slots per session.")]
        [SerializeField] private int maxSpectators = 4;

        [Header("Connection Quality")]
        [Tooltip("How often (seconds) to sample and update connection metrics.")]
        [SerializeField] private float metricsUpdateInterval = 2f;

        [Header("Host Migration")]
        [Tooltip("Seconds to wait for a new host before declaring the session lost.")]
        [SerializeField] private float hostMigrationTimeout = 5f;

        [Header("NAT / Relay")]
        [Tooltip("STUN-like relay server URL used for NAT punch-through attempts.")]
        [SerializeField] private string stunRelayUrl = "stun.swef.example.com:3478";

        [Tooltip("Maximum UDP hole-punch attempts before falling back to relay.")]
        [SerializeField] private int maxNatPunchAttempts = 5;

        [Header("References")]
        [SerializeField] private WebSocketTransport transport;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the local player successfully creates a lobby.</summary>
        public event Action<LobbyInfo> OnLobbyCreated;

        /// <summary>Fired when the local player successfully joins a lobby.</summary>
        public event Action<LobbyInfo> OnLobbyJoined;

        /// <summary>Fired when host authority transfers to a new player.</summary>
        public event Action<string> OnHostMigrated;

        /// <summary>Fired when a remote player connects to the session.</summary>
        public event Action<string> OnPlayerConnected;

        /// <summary>Fired when a remote player disconnects from the session.</summary>
        public event Action<string> OnPlayerDisconnected;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Whether the local player is the current session host.</summary>
        public bool IsHost { get; private set; }

        /// <summary>The active lobby (null when not in a session).</summary>
        public LobbyInfo CurrentLobby { get; private set; }

        /// <summary>ID of the player currently acting as host.</summary>
        public string HostPlayerId { get; private set; }

        /// <summary>Read-only snapshot of all connected players' network metrics.</summary>
        public IReadOnlyList<PlayerNetworkMetrics> PlayerMetrics => _playerMetrics;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<PlayerNetworkMetrics> _playerMetrics = new();
        private readonly Dictionary<string, float> _joinTimes = new();
        private string _localPlayerId;
        private float _metricsTimer;
        private bool _migrationInProgress;

        // ── Main-thread dispatch queue ────────────────────────────────────────────
        private readonly Queue<Action> _mainThreadQueue = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _localPlayerId = Guid.NewGuid().ToString("N").Substring(0, 8);

            if (transport == null)
                transport = FindFirstObjectByType<WebSocketTransport>();
        }

        private void Start()
        {
            if (transport != null)
            {
                transport.OnConnected     += HandleTransportConnected;
                transport.OnDisconnected  += HandleTransportDisconnected;
                transport.OnMessageReceived += HandleRawMessage;
            }
        }

        private void Update()
        {
            // Flush main-thread callbacks dispatched from background threads.
            lock (_mainThreadQueue)
            {
                while (_mainThreadQueue.Count > 0)
                    _mainThreadQueue.Dequeue()?.Invoke();
            }

            // Periodic metrics update.
            _metricsTimer += Time.deltaTime;
            if (_metricsTimer >= metricsUpdateInterval)
            {
                _metricsTimer = 0f;
                UpdateConnectionMetrics();
            }
        }

        private void OnDestroy()
        {
            if (transport != null)
            {
                transport.OnConnected     -= HandleTransportConnected;
                transport.OnDisconnected  -= HandleTransportDisconnected;
                transport.OnMessageReceived -= HandleRawMessage;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new lobby with a randomly generated 6-character alphanumeric room code.
        /// </summary>
        /// <param name="lobbyName">Human-readable name for the lobby.</param>
        /// <param name="isPublic">Whether the lobby appears in public listings.</param>
        public void CreateLobby(string lobbyName, bool isPublic = true)
        {
            var lobby = new LobbyInfo
            {
                roomCode       = GenerateRoomCode(),
                lobbyId        = Guid.NewGuid().ToString(),
                lobbyName      = lobbyName,
                hostPlayerName = _localPlayerId,
                maxPlayers     = maxPlayers,
                maxSpectators  = maxSpectators,
                isPublic       = isPublic,
                createdAt      = DateTime.UtcNow,
                playerCount    = 1
            };

            CurrentLobby = lobby;
            IsHost       = true;
            HostPlayerId = _localPlayerId;

            Debug.Log($"[SWEF][NetworkManager2] Created lobby '{lobbyName}' code={lobby.roomCode}");
            OnLobbyCreated?.Invoke(lobby);

            try
            {
                transport?.Connect(lobby.lobbyId, _localPlayerId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF][NetworkManager2] Transport connect failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Joins an existing lobby by its 6-character room code.
        /// Attempts UDP NAT punch-through first, falls back to relay on failure.
        /// </summary>
        /// <param name="roomCode">6-character alphanumeric room code.</param>
        public void JoinLobby(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 6)
            {
                Debug.LogWarning("[SWEF][NetworkManager2] Invalid room code: must be 6 alphanumeric characters.");
                return;
            }

            StartCoroutine(NatPunchAndConnect(roomCode.ToUpperInvariant()));
        }

        /// <summary>
        /// Leaves the current session gracefully. If the local player is host,
        /// initiates host migration before disconnecting.
        /// </summary>
        public void LeaveLobby()
        {
            if (CurrentLobby == null) return;

            if (IsHost)
                StartCoroutine(MigrateHostThenDisconnect());
            else
                Disconnect();
        }

        /// <summary>
        /// Returns the current connection quality for the given player.
        /// </summary>
        /// <param name="playerId">Target player identifier.</param>
        /// <returns>Connection quality tier, or <c>Excellent</c> if no data exists.</returns>
        public ConnectionQuality GetConnectionQuality(string playerId)
        {
            var m = _playerMetrics.FirstOrDefault(x => x.playerId == playerId);
            return m?.quality ?? ConnectionQuality.Excellent;
        }

        // ── Room code ─────────────────────────────────────────────────────────────

        /// <summary>Generates a 6-character uppercase alphanumeric room code.</summary>
        private static string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // excludes O, 0, I, 1
            var rng  = new System.Random();
            var code = new char[6];
            for (int i = 0; i < code.Length; i++)
                code[i] = chars[rng.Next(chars.Length)];
            return new string(code);
        }

        // ── NAT punch-through ─────────────────────────────────────────────────────

        /// <summary>
        /// Attempts UDP hole-punching via the configured STUN relay server.
        /// Falls back to relay transport after <see cref="maxNatPunchAttempts"/> failures.
        /// </summary>
        private IEnumerator NatPunchAndConnect(string roomCode)
        {
            Debug.Log($"[SWEF][NetworkManager2] Attempting NAT punch-through for room {roomCode} via {stunRelayUrl}");

            bool connected = false;
            for (int attempt = 1; attempt <= maxNatPunchAttempts && !connected; attempt++)
            {
                Debug.Log($"[SWEF][NetworkManager2] NAT punch attempt {attempt}/{maxNatPunchAttempts}");
                // Simulate UDP hole-punch handshake delay.
                yield return new WaitForSeconds(0.3f);

                // In production, send a UDP punch packet to the STUN relay; check for response.
                // Here we simulate occasional success.
                if (attempt >= 2)
                {
                    connected = true;
                    Debug.Log($"[SWEF][NetworkManager2] NAT punch succeeded on attempt {attempt}.");
                }
            }

            if (!connected)
            {
                Debug.LogWarning("[SWEF][NetworkManager2] NAT punch failed; falling back to relay transport.");
            }

            // Proceed with connection regardless (relay fallback).
            try
            {
                transport?.Connect(roomCode, _localPlayerId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF][NetworkManager2] Connect error: {ex.Message}");
            }
        }

        // ── Host migration ────────────────────────────────────────────────────────

        /// <summary>
        /// Migrates host authority to the best-connected remaining player,
        /// then disconnects the local transport.
        /// </summary>
        private IEnumerator MigrateHostThenDisconnect()
        {
            if (_migrationInProgress) yield break;
            _migrationInProgress = true;

            // Rank remaining players by lowest RTT (best connection).
            var candidates = _playerMetrics
                .Where(m => m.playerId != _localPlayerId)
                .OrderBy(m => m.rttMs)
                .ToList();

            if (candidates.Count > 0)
            {
                string newHostId = candidates[0].playerId;
                HostPlayerId = newHostId;
                IsHost = false;
                Debug.Log($"[SWEF][NetworkManager2] Migrating host to {newHostId} (RTT {candidates[0].rttMs:F0} ms).");
                OnHostMigrated?.Invoke(newHostId);
            }

            yield return new WaitForSeconds(0.5f);
            Disconnect();
            _migrationInProgress = false;
        }

        /// <summary>
        /// Handles detection that the current host has disconnected unexpectedly.
        /// Promotes the best-latency remaining player to host.
        /// </summary>
        private void HandleHostDisconnected()
        {
            if (IsHost) return; // We are already host.

            var candidates = _playerMetrics
                .Where(m => m.playerId != HostPlayerId)
                .OrderBy(m => m.rttMs)
                .ToList();

            if (candidates.Count == 0) return;

            string newHostId = candidates[0].playerId;
            HostPlayerId = newHostId;
            IsHost = (newHostId == _localPlayerId);

            Debug.Log($"[SWEF][NetworkManager2] Host migration: new host is {newHostId} (IsLocalHost={IsHost}).");
            OnHostMigrated?.Invoke(newHostId);
        }

        // ── Connection quality ────────────────────────────────────────────────────

        /// <summary>Refreshes per-player connection metrics using simulated ping data.</summary>
        private void UpdateConnectionMetrics()
        {
            foreach (var metrics in _playerMetrics)
            {
                // In production, these values come from network pings / acknowledgement timing.
                // Simulate plausible values for demonstration.
                metrics.rttMs      = Mathf.Max(5f, metrics.rttMs + UnityEngine.Random.Range(-5f, 5f));
                metrics.packetLoss = Mathf.Clamp01(metrics.packetLoss + UnityEngine.Random.Range(-0.005f, 0.005f));
                metrics.jitterMs   = Mathf.Max(0f, metrics.jitterMs + UnityEngine.Random.Range(-2f, 2f));
                metrics.quality    = ClassifyQuality(metrics.rttMs, metrics.packetLoss);
                metrics.timestamp  = Time.time;
            }
        }

        /// <summary>Maps RTT and packet loss to a <see cref="ConnectionQuality"/> tier.</summary>
        private static ConnectionQuality ClassifyQuality(float rttMs, float packetLoss)
        {
            if (rttMs < 50f  && packetLoss < 0.01f) return ConnectionQuality.Excellent;
            if (rttMs < 100f && packetLoss < 0.03f) return ConnectionQuality.Good;
            if (rttMs < 200f && packetLoss < 0.08f) return ConnectionQuality.Fair;
            return ConnectionQuality.Poor;
        }

        // ── Transport callbacks ───────────────────────────────────────────────────

        private void HandleTransportConnected()
        {
            Debug.Log("[SWEF][NetworkManager2] Transport connected.");
        }

        private void HandleTransportDisconnected(string reason)
        {
            Debug.Log($"[SWEF][NetworkManager2] Transport disconnected: {reason}");

            // If the reason encodes a structured disconnect (e.g. JSON), parse the player ID
            // from the data. For now, trigger migration whenever any disconnect is detected
            // and the local player is not the host — the host may have left.
            if (CurrentLobby != null && !IsHost)
                DispatchToMainThread(HandleHostDisconnected);
        }

        private void HandleRawMessage(byte[] data)
        {
            // Parse incoming message type and dispatch accordingly.
            // In production, deserialize a typed network message header.
            if (data == null || data.Length == 0) return;

            DispatchToMainThread(() =>
            {
                // Placeholder: real implementation would decode and route messages.
                Debug.Log($"[SWEF][NetworkManager2] Received {data.Length} bytes.");
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void Disconnect()
        {
            try { transport?.Disconnect(); }
            catch (Exception ex) { Debug.LogWarning($"[SWEF][NetworkManager2] Disconnect error: {ex.Message}"); }

            CurrentLobby = null;
            IsHost       = false;
            _playerMetrics.Clear();
        }

        /// <summary>Enqueues an action to run on the Unity main thread.</summary>
        private void DispatchToMainThread(Action action)
        {
            lock (_mainThreadQueue)
                _mainThreadQueue.Enqueue(action);
        }
    }
}
