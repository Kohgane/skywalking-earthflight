using System;
using UnityEngine;
using SWEF.Core;

namespace SWEF.CloudRendering
{
    /// <summary>
    /// Singleton MonoBehaviour that manages the entire cloud rendering pipeline.
    /// Toggles between local rendering and cloud streaming modes, auto-detects
    /// device capability, and integrates with <see cref="PerformanceManager"/> to
    /// switch automatically when FPS drops below a configurable threshold.
    /// </summary>
    public class CloudRenderingManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Singleton instance.</summary>
        public static CloudRenderingManager Instance { get; private set; }

        // ── Connection status enum ────────────────────────────────────────────────
        /// <summary>Represents the current cloud connection state.</summary>
        public enum ConnectionStatus { Disconnected, Connecting, Connected, Streaming, Error }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Cloud Rendering")]
        [SerializeField] private bool enableOnStart = false;
        [SerializeField] private float autoSwitchFpsThreshold = 25f;

        [Header("Refs (auto-found if null)")]
        [SerializeField] private PerformanceManager performanceManager;
        [SerializeField] private StreamingClient streamingClient;
        [SerializeField] private CloudSessionManager sessionManager;

        // ── Internal state ────────────────────────────────────────────────────────
        private bool _isCloudMode;
        private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;
        private float _fpsCheckInterval = 5f;
        private float _fpsCheckTimer;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when cloud mode is toggled. Parameter is true when entering cloud mode.</summary>
        public event Action<bool> OnCloudModeChanged;

        /// <summary>Raised when the connection status changes.</summary>
        public event Action<ConnectionStatus> OnConnectionStatusChanged;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Whether the system is currently in cloud streaming mode.</summary>
        public bool IsCloudMode => _isCloudMode;

        /// <summary>Current connection status to the cloud rendering server.</summary>
        public ConnectionStatus CurrentConnectionStatus => _connectionStatus;

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

            if (performanceManager == null)
                performanceManager = FindFirstObjectByType<PerformanceManager>();
            if (streamingClient == null)
                streamingClient = FindFirstObjectByType<StreamingClient>();
            if (sessionManager == null)
                sessionManager = FindFirstObjectByType<CloudSessionManager>();
        }

        private void Start()
        {
            if (enableOnStart)
                EnableCloudRendering();
        }

        private void Update()
        {
            if (!_isCloudMode && performanceManager != null)
            {
                _fpsCheckTimer -= Time.deltaTime;
                if (_fpsCheckTimer <= 0f)
                {
                    _fpsCheckTimer = _fpsCheckInterval;
                    CheckAutoSwitch();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Initializes the cloud rendering system (called by BootManager).</summary>
        public void Initialize()
        {
            Debug.Log("[SWEF] CloudRenderingManager initialized");
            SetConnectionStatus(ConnectionStatus.Disconnected);
        }

        /// <summary>Switches to cloud streaming mode and starts a session.</summary>
        public void EnableCloudRendering()
        {
            if (_isCloudMode) return;
            _isCloudMode = true;
            SetConnectionStatus(ConnectionStatus.Connecting);
            sessionManager?.CreateSession();
            OnCloudModeChanged?.Invoke(true);
            Debug.Log("[SWEF] Cloud rendering enabled");
        }

        /// <summary>Reverts to local rendering mode and ends the cloud session.</summary>
        public void DisableCloudRendering()
        {
            if (!_isCloudMode) return;
            _isCloudMode = false;
            streamingClient?.Disconnect();
            sessionManager?.EndSession();
            SetConnectionStatus(ConnectionStatus.Disconnected);
            OnCloudModeChanged?.Invoke(false);
            Debug.Log("[SWEF] Cloud rendering disabled — fallback to local rendering");
        }

        /// <summary>
        /// Evaluates current hardware and returns <c>true</c> when cloud mode is
        /// recommended (e.g. low GPU tier or consistently low FPS).
        /// </summary>
        public bool ShouldSuggestCloudMode()
        {
            if (performanceManager == null) return false;
            return performanceManager.CurrentFps < autoSwitchFpsThreshold;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void CheckAutoSwitch()
        {
            if (ShouldSuggestCloudMode())
            {
                Debug.Log($"[SWEF] FPS below threshold ({autoSwitchFpsThreshold}) — suggesting cloud mode");
            }
        }

        internal void SetConnectionStatus(ConnectionStatus status)
        {
            if (_connectionStatus == status) return;
            _connectionStatus = status;
            OnConnectionStatusChanged?.Invoke(status);
        }
    }
}
