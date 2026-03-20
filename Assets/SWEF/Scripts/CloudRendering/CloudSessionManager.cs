using System;
using System.Collections;
using UnityEngine;

namespace SWEF.CloudRendering
{
    /// <summary>
    /// Manages the cloud rendering session lifecycle including session creation,
    /// heartbeat pings (every 5 seconds), authentication token management, and
    /// graceful termination.  Sessions are automatically terminated after
    /// <see cref="CloudSessionConfig.maxSessionMinutes"/> minutes of idle time.
    /// </summary>
    public class CloudSessionManager : MonoBehaviour
    {
        // ── Nested types ──────────────────────────────────────────────────────────

        /// <summary>Serialisable configuration for a cloud rendering session.</summary>
        [Serializable]
        public class CloudSessionConfig
        {
            /// <summary>WebSocket URL of the cloud rendering server.</summary>
            public string serverUrl = "ws://localhost:8080/render";

            /// <summary>Geographic region identifier (e.g. "US-East").</summary>
            public string region = "auto";

            /// <summary>Maximum session duration in minutes before automatic timeout.</summary>
            public int maxSessionMinutes = 30;

            /// <summary>Bearer token used to authenticate with the cloud server.</summary>
            public string authToken = "";
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Session Config")]
        [SerializeField] private CloudSessionConfig config = new CloudSessionConfig();
        [SerializeField] private float heartbeatIntervalSec = 5f;

        // ── Internal state ────────────────────────────────────────────────────────
        private string _sessionId;
        private float  _sessionStartTime;
        private float  _lastActivityTime;
        private bool   _sessionActive;
        private Coroutine _heartbeatCoroutine;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Unique identifier for the current session, or empty when no session is active.</summary>
        public string SessionId => _sessionId;

        /// <summary>Whether a cloud session is currently active.</summary>
        public bool IsSessionActive => _sessionActive;

        /// <summary>Provides read/write access to session configuration.</summary>
        public CloudSessionConfig Config => config;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Update()
        {
            if (!_sessionActive) return;

            // Check idle timeout
            float idleMinutes = (Time.realtimeSinceStartup - _lastActivityTime) / 60f;
            if (idleMinutes >= config.maxSessionMinutes)
            {
                Debug.Log($"[SWEF] CloudSession timed out after {config.maxSessionMinutes} min idle");
                EndSession();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Creates a new cloud rendering session and starts the heartbeat.</summary>
        public void CreateSession()
        {
            if (_sessionActive)
            {
                Debug.LogWarning("[SWEF] CloudSessionManager: session already active");
                return;
            }

            _sessionId        = GenerateSessionId();
            _sessionStartTime = Time.realtimeSinceStartup;
            _lastActivityTime = Time.realtimeSinceStartup;
            _sessionActive    = true;

            // Connect streaming client
            var client = FindFirstObjectByType<StreamingClient>();
            client?.Connect(config.serverUrl);

            // Discover best server if region is auto
            if (config.region == "auto")
                FindFirstObjectByType<ServerDiscoveryService>()?.DiscoverServers();

            _heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
            Debug.Log($"[SWEF] Cloud session created: {_sessionId} | region: {config.region}");
        }

        /// <summary>Terminates the current session and stops the heartbeat.</summary>
        public void EndSession()
        {
            if (!_sessionActive) return;

            _sessionActive = false;

            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
                _heartbeatCoroutine = null;
            }

            FindFirstObjectByType<StreamingClient>()?.Disconnect();
            Debug.Log($"[SWEF] Cloud session ended: {_sessionId}");
            _sessionId = string.Empty;
        }

        /// <summary>Registers activity to reset the idle timeout timer.</summary>
        public void RegisterActivity()
        {
            _lastActivityTime = Time.realtimeSinceStartup;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private IEnumerator HeartbeatLoop()
        {
            while (_sessionActive)
            {
                yield return new WaitForSeconds(heartbeatIntervalSec);
                if (_sessionActive)
                    Debug.Log($"[SWEF] Cloud heartbeat — session: {_sessionId}");
            }
        }

        private static string GenerateSessionId()
        {
            return $"swef-{System.Guid.NewGuid():D}";
        }
    }
}
