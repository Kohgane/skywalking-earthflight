using System;
using System.Collections;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SWEF.CloudRendering
{
    /// <summary>
    /// WebSocket-based client that receives rendered frames from the cloud server.
    /// Implements a ring-buffer frame queue, latency measurement, and exponential-backoff
    /// reconnection (up to <see cref="MaxRetries"/> attempts).
    /// </summary>
    public class StreamingClient : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Singleton instance.</summary>
        public static StreamingClient Instance { get; private set; }
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Connection")]
        [SerializeField] private int frameBufferSize = 5;
        [SerializeField] private int maxRetries = 5;
        [SerializeField] private float initialRetryDelaySec = 1f;

        // ── Internal state ────────────────────────────────────────────────────────
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private byte[][] _frameBuffer;
        private int _writeHead;
        private int _readHead;
        private int _bufferedFrames;

        private float _latencyMs;
        private long _sendTimestampMs;

        private int _retryCount;
        private bool _isConnected;
        private string _serverUrl;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when a new frame payload has been received and buffered.</summary>
        public event Action<byte[]> OnFrameReceived;

        /// <summary>Raised when the measured round-trip latency is updated.</summary>
        public event Action<float> OnLatencyUpdated;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Most recently measured round-trip latency in milliseconds.</summary>
        public float LatencyMs => _latencyMs;

        /// <summary>Whether the WebSocket connection is currently open.</summary>
        public bool IsConnected => _isConnected;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _frameBuffer = new byte[Mathf.Clamp(frameBufferSize, 3, 10)][];
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Disconnect();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Connects to the cloud rendering server at <paramref name="serverUrl"/>.</summary>
        public async void Connect(string serverUrl)
        {
            _serverUrl = serverUrl;
            _retryCount = 0;
            await ConnectWithRetryAsync();
        }

        /// <summary>Closes the WebSocket connection and cancels any pending operations.</summary>
        public void Disconnect()
        {
            _cts?.Cancel();
            _isConnected = false;
            try
            {
                _webSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect",
                    CancellationToken.None).Wait(500);
            }
            catch { /* best-effort close */ }
            _webSocket?.Dispose();
            _webSocket = null;
            Debug.Log("[SWEF] StreamingClient disconnected");
        }

        /// <summary>
        /// Returns the next buffered frame or <c>null</c> when the buffer is empty.
        /// </summary>
        public byte[] DequeueFrame()
        {
            if (_bufferedFrames == 0) return null;
            var frame = _frameBuffer[_readHead % _frameBuffer.Length];
            _readHead++;
            _bufferedFrames--;
            return frame;
        }

        /// <summary>
        /// Sends a binary packet (e.g. serialised input) to the connected cloud server.
        /// Does nothing when not connected.
        /// </summary>
        public async void SendPacket(byte[] data)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open || data == null) return;
            try
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(data),
                    WebSocketMessageType.Binary, true, _cts?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] StreamingClient.SendPacket failed: {ex.Message}");
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private async Task ConnectWithRetryAsync()
        {
            float delay = initialRetryDelaySec;
            while (_retryCount <= maxRetries)
            {
                try
                {
                    _cts?.Dispose();
                    _cts = new CancellationTokenSource();
                    _webSocket?.Dispose();
                    _webSocket = new ClientWebSocket();

                    await _webSocket.ConnectAsync(new Uri(_serverUrl), _cts.Token);
                    _isConnected = true;
                    _retryCount = 0;
                    FindFirstObjectByType<CloudRenderingManager>()
                        ?.SetConnectionStatus(CloudRenderingManager.ConnectionStatus.Connected);
                    Debug.Log($"[SWEF] StreamingClient connected to {_serverUrl}");
                    _ = ReceiveLoopAsync();
                    return;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    _retryCount++;
                    if (_retryCount > maxRetries)
                    {
                        Debug.LogError($"[SWEF] StreamingClient: max retries reached. Last error: {ex.Message}");
                        FindFirstObjectByType<CloudRenderingManager>()
                            ?.SetConnectionStatus(CloudRenderingManager.ConnectionStatus.Error);
                        return;
                    }
                    Debug.LogWarning($"[SWEF] StreamingClient connect attempt {_retryCount} failed — retrying in {delay:F1}s");
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                    delay = Mathf.Min(delay * 2f, 30f); // exponential backoff, capped at 30 s
                }
            }
        }

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[1024 * 512]; // 512 KB receive buffer
            FindFirstObjectByType<CloudRenderingManager>()
                ?.SetConnectionStatus(CloudRenderingManager.ConnectionStatus.Streaming);

            while (_webSocket != null && _webSocket.State == WebSocketState.Open
                   && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    long recvStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _isConnected = false;
                        Debug.Log("[SWEF] StreamingClient: server closed connection");
                        FindFirstObjectByType<CloudRenderingManager>()
                            ?.SetConnectionStatus(CloudRenderingManager.ConnectionStatus.Disconnected);
                        break;
                    }

                    // Measure round-trip latency
                    _latencyMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - recvStart;
                    OnLatencyUpdated?.Invoke(_latencyMs);

                    // Copy frame into ring buffer
                    var frame = new byte[result.Count];
                    Array.Copy(buffer, frame, result.Count);
                    EnqueueFrame(frame);
                    OnFrameReceived?.Invoke(frame);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF] StreamingClient receive error: {ex.Message}");
                    _isConnected = false;
                    FindFirstObjectByType<CloudRenderingManager>()
                        ?.SetConnectionStatus(CloudRenderingManager.ConnectionStatus.Error);
                    // Attempt reconnect
                    await ConnectWithRetryAsync();
                    break;
                }
            }
        }

        private void EnqueueFrame(byte[] frame)
        {
            _frameBuffer[_writeHead % _frameBuffer.Length] = frame;
            _writeHead++;
            if (_bufferedFrames < _frameBuffer.Length)
                _bufferedFrames++;
            else
                _readHead++; // overwrite oldest — keep buffer size constant
        }
    }
}
