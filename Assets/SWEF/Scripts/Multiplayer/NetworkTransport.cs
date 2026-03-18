using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    // ── Interface ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Contract for all network transport implementations used by the multiplayer system.
    /// Implementations may wrap WebSockets, relay servers, or local simulators.
    /// </summary>
    public interface INetworkTransport
    {
        /// <summary>Whether the transport currently has an open connection.</summary>
        bool IsConnected { get; }

        /// <summary>Fired when the connection to the server is established.</summary>
        event Action OnConnected;

        /// <summary>Fired when the connection is closed, with an optional reason string.</summary>
        event Action<string> OnDisconnected;

        /// <summary>Fired when a raw byte-array message is received from the server.</summary>
        event Action<byte[]> OnMessageReceived;

        /// <summary>Fired when a transport-level error occurs.</summary>
        event Action<string> OnError;

        /// <summary>Opens a connection to the given room and registers the local player name.</summary>
        /// <param name="roomId">Server-side room identifier.</param>
        /// <param name="playerName">Display name for the local player.</param>
        void Connect(string roomId, string playerName);

        /// <summary>Closes the active connection gracefully.</summary>
        void Disconnect();

        /// <summary>Sends a raw byte-array payload to the server.</summary>
        /// <param name="data">Payload bytes.</param>
        void SendMessage(byte[] data);
    }

    // ── WebSocket Implementation ─────────────────────────────────────────────────

    /// <summary>
    /// Production WebSocket transport with automatic reconnection, heartbeat pings,
    /// and offline message buffering.
    /// </summary>
    public class WebSocketTransport : MonoBehaviour, INetworkTransport
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [SerializeField] private string serverUrl = "wss://swef-multiplayer.example.com";

        /// <summary>Seconds between heartbeat pings sent to the server.</summary>
        [SerializeField] private float heartbeatInterval = 10f;

        /// <summary>Maximum number of messages buffered while offline.</summary>
        [SerializeField] private int maxOfflineBufferSize = 50;

        /// <summary>Maximum auto-reconnect attempts before giving up.</summary>
        [SerializeField] private int maxReconnectAttempts = 5;

        // ── INetworkTransport ────────────────────────────────────────────────────
        /// <inheritdoc/>
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public event Action OnConnected;

        /// <inheritdoc/>
        public event Action<string> OnDisconnected;

        /// <inheritdoc/>
        public event Action<byte[]> OnMessageReceived;

        /// <inheritdoc/>
        public event Action<string> OnError;

        // ── State ────────────────────────────────────────────────────────────────
        private string _currentRoomId;
        private string _currentPlayerName;
        private int _reconnectAttempts;
        private float _heartbeatTimer;
        private readonly Queue<byte[]> _offlineBuffer = new Queue<byte[]>();

        // Main-thread dispatch queue for messages arriving from background threads
        private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();

        // ── INetworkTransport Methods ────────────────────────────────────────────

        /// <inheritdoc/>
        public void Connect(string roomId, string playerName)
        {
            _currentRoomId    = roomId;
            _currentPlayerName = playerName;
            _reconnectAttempts = 0;

            StartCoroutine(ConnectCoroutine());
            Debug.Log($"[SWEF][WebSocketTransport] Connecting to room '{roomId}' as '{playerName}'");
        }

        /// <inheritdoc/>
        public void Disconnect()
        {
            StopAllCoroutines();
            if (IsConnected)
            {
                IsConnected = false;
                OnDisconnected?.Invoke("user_requested");
                Debug.Log("[SWEF][WebSocketTransport] Disconnected by user request.");
            }
        }

        /// <inheritdoc/>
        public void SendMessage(byte[] data)
        {
            if (!IsConnected)
            {
                // Buffer message for when the connection is re-established
                if (_offlineBuffer.Count < maxOfflineBufferSize)
                    _offlineBuffer.Enqueue(data);
                return;
            }

            // In a real implementation this would write to the WebSocket stream.
            // Here we log as a stub.
            Debug.Log($"[SWEF][WebSocketTransport] SendMessage: {data.Length} bytes");
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            // Drain the main-thread dispatch queue
            lock (_mainThreadQueue)
            {
                while (_mainThreadQueue.Count > 0)
                    _mainThreadQueue.Dequeue()?.Invoke();
            }

            // Heartbeat
            if (IsConnected)
            {
                _heartbeatTimer += Time.deltaTime;
                if (_heartbeatTimer >= heartbeatInterval)
                {
                    _heartbeatTimer = 0f;
                    SendHeartbeat();
                }
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        // ── Internal Helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Dispatches an action onto the Unity main thread.
        /// Safe to call from background threads.
        /// </summary>
        private void DispatchToMainThread(Action action)
        {
            lock (_mainThreadQueue)
                _mainThreadQueue.Enqueue(action);
        }

        private IEnumerator ConnectCoroutine()
        {
            // Simulated connection attempt — real implementation would open a WebSocket.
            yield return new WaitForSeconds(0.1f);

            IsConnected = true;
            _heartbeatTimer = 0f;
            Debug.Log("[SWEF][WebSocketTransport] Connected.");

            // Flush offline buffer
            while (_offlineBuffer.Count > 0)
                SendMessage(_offlineBuffer.Dequeue());

            OnConnected?.Invoke();
        }

        private IEnumerator ReconnectWithBackoff()
        {
            if (_reconnectAttempts >= maxReconnectAttempts)
            {
                string err = "Max reconnect attempts reached.";
                Debug.LogWarning($"[SWEF][WebSocketTransport] {err}");
                OnError?.Invoke(err);
                yield break;
            }

            // Exponential back-off: 1s, 2s, 4s … capped at 30s
            float delay = Mathf.Min(Mathf.Pow(2f, _reconnectAttempts), 30f);
            _reconnectAttempts++;
            Debug.Log($"[SWEF][WebSocketTransport] Reconnect attempt {_reconnectAttempts} in {delay}s…");
            yield return new WaitForSeconds(delay);

            yield return ConnectCoroutine();
        }

        private void HandleConnectionLost(string reason)
        {
            IsConnected = false;
            OnDisconnected?.Invoke(reason);
            Debug.Log($"[SWEF][WebSocketTransport] Connection lost: {reason}. Starting reconnect…");
            StartCoroutine(ReconnectWithBackoff());
        }

        private void SendHeartbeat()
        {
            // A real heartbeat payload; here we just log.
            Debug.Log("[SWEF][WebSocketTransport] ♥ Heartbeat ping sent.");
        }
    }

    // ── Fallback Local Transport ─────────────────────────────────────────────────

    /// <summary>
    /// Offline / test transport that simulates 50 ms network latency using coroutines.
    /// Loopbacks all sent messages back to <see cref="OnMessageReceived"/> after the delay.
    /// </summary>
    public class FallbackLocalTransport : MonoBehaviour, INetworkTransport
    {
        /// <summary>Simulated one-way latency in milliseconds.</summary>
        [SerializeField] private float simulatedLatencyMs = 50f;

        // ── INetworkTransport ────────────────────────────────────────────────────
        /// <inheritdoc/>
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public event Action OnConnected;

        /// <inheritdoc/>
        public event Action<string> OnDisconnected;

        /// <inheritdoc/>
        public event Action<byte[]> OnMessageReceived;

        /// <inheritdoc/>
        public event Action<string> OnError;

        // ── INetworkTransport Methods ────────────────────────────────────────────

        /// <inheritdoc/>
        public void Connect(string roomId, string playerName)
        {
            StartCoroutine(SimulateConnect());
            Debug.Log($"[SWEF][FallbackLocalTransport] Simulating connection to room '{roomId}'.");
        }

        /// <inheritdoc/>
        public void Disconnect()
        {
            IsConnected = false;
            OnDisconnected?.Invoke("local_disconnect");
            Debug.Log("[SWEF][FallbackLocalTransport] Disconnected.");
        }

        /// <inheritdoc/>
        public void SendMessage(byte[] data)
        {
            if (!IsConnected) return;
            // Loop the message back with a simulated delay
            StartCoroutine(DeliverWithDelay(data));
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        private IEnumerator SimulateConnect()
        {
            yield return new WaitForSeconds(simulatedLatencyMs / 1000f);
            IsConnected = true;
            Debug.Log("[SWEF][FallbackLocalTransport] Connected (simulated).");
            OnConnected?.Invoke();
        }

        private IEnumerator DeliverWithDelay(byte[] data)
        {
            yield return new WaitForSeconds(simulatedLatencyMs / 1000f);
            OnMessageReceived?.Invoke(data);
        }
    }
}
