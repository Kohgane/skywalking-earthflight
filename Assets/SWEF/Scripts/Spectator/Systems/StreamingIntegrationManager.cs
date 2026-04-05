// StreamingIntegrationManager.cs — SWEF Phase 107: Live Streaming & Spectator Mode
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Spectator
{
    /// <summary>
    /// Phase 107 — Manages live-stream session state and exposes overlay data
    /// for external tools such as OBS browser sources.
    ///
    /// <para>Tracks whether a stream is live, the target platform, a conceptual
    /// viewer count (updated externally or via mock data), and fires milestone
    /// events at configurable viewer thresholds.</para>
    /// </summary>
    public sealed class StreamingIntegrationManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static StreamingIntegrationManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────
        [SerializeField] private SpectatorConfig config;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a stream session starts.</summary>
        public event Action<StreamingPlatform> OnStreamStarted;

        /// <summary>Raised when a stream session ends.</summary>
        public event Action OnStreamEnded;

        /// <summary>Raised when a viewer count milestone is reached. Argument is the milestone value.</summary>
        public event Action<int> OnViewerMilestone;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> when a stream session is currently active.</summary>
        public bool IsStreaming { get; private set; }

        /// <summary>The platform being streamed to.</summary>
        public StreamingPlatform Platform { get; private set; }

        /// <summary>Current (conceptual) viewer count.</summary>
        public int ViewerCount { get; private set; }

        /// <summary>Elapsed streaming time in seconds.</summary>
        public float StreamUptime { get; private set; }

        // ── Internal state ─────────────────────────────────────────────────────
        private readonly HashSet<int> _triggeredMilestones = new HashSet<int>();
        private OverlayData _overlayData;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!IsStreaming) return;

            StreamUptime += Time.unscaledDeltaTime;
            CheckMilestones();
        }

        // ── Public API — stream lifecycle ──────────────────────────────────────

        /// <summary>
        /// Starts a new stream session on <paramref name="platform"/>.
        /// No-op if a session is already active.
        /// </summary>
        public void StartStream(StreamingPlatform platform)
        {
            if (IsStreaming) return;

            Platform      = platform;
            IsStreaming   = true;
            StreamUptime  = 0f;
            ViewerCount   = 0;
            _triggeredMilestones.Clear();

            OnStreamStarted?.Invoke(platform);
            Debug.Log($"[StreamingIntegrationManager] Stream started on {platform}.");
        }

        /// <summary>Ends the current stream session.</summary>
        public void EndStream()
        {
            if (!IsStreaming) return;
            IsStreaming = false;
            OnStreamEnded?.Invoke();
            Debug.Log("[StreamingIntegrationManager] Stream ended.");
        }

        // ── Public API — viewer count ──────────────────────────────────────────

        /// <summary>
        /// Updates the conceptual viewer count. Call this from your platform
        /// polling logic (e.g. Twitch API bridge) or from tests.
        /// </summary>
        public void UpdateViewerCount(int count)
        {
            ViewerCount = Mathf.Max(0, count);
        }

        // ── Public API — overlay data ──────────────────────────────────────────

        /// <summary>
        /// Returns a snapshot of flight data suitable for OBS/streaming overlays.
        /// Queries <see cref="SpectatorModeController"/> for the current target.
        /// </summary>
        public OverlayData GetOverlayData()
        {
            _overlayData = new OverlayData
            {
                isStreaming  = IsStreaming,
                platform     = Platform,
                viewerCount  = ViewerCount,
                uptimeSeconds = StreamUptime,
            };

#if SWEF_FLIGHT_AVAILABLE
            var spectator = SpectatorModeController.Instance;
            if (spectator != null && spectator.CurrentTarget != null)
            {
                var fc = spectator.CurrentTarget.GetComponent<SWEF.Flight.FlightController>();
                if (fc != null)
                {
                    _overlayData.targetSpeedKph    = fc.CurrentSpeedKph;
                    _overlayData.targetAltitudeM   = fc.CurrentAltitudeMetres;
                    _overlayData.targetHeadingDeg  = fc.CurrentHeadingDegrees;
                }
            }
#endif

            return _overlayData;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void CheckMilestones()
        {
            if (config == null) return;

            foreach (int milestone in config.viewerMilestones)
            {
                if (ViewerCount >= milestone && !_triggeredMilestones.Contains(milestone))
                {
                    _triggeredMilestones.Add(milestone);
                    OnViewerMilestone?.Invoke(milestone);
                    Debug.Log($"[StreamingIntegrationManager] Viewer milestone reached: {milestone}.");
                }
            }
        }
    }

    // ── Data types ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot of streaming and flight data used to populate external overlays
    /// (OBS browser sources, stream deck widgets, etc.).
    /// </summary>
    [Serializable]
    public sealed class OverlayData
    {
        /// <summary>Whether a stream is currently active.</summary>
        public bool isStreaming;
        /// <summary>Streaming platform.</summary>
        public StreamingPlatform platform;
        /// <summary>Current viewer count.</summary>
        public int viewerCount;
        /// <summary>Stream uptime in seconds.</summary>
        public float uptimeSeconds;
        /// <summary>Target aircraft speed in kph (requires SWEF_FLIGHT_AVAILABLE).</summary>
        public float targetSpeedKph;
        /// <summary>Target aircraft altitude in metres (requires SWEF_FLIGHT_AVAILABLE).</summary>
        public float targetAltitudeM;
        /// <summary>Target aircraft heading in degrees (requires SWEF_FLIGHT_AVAILABLE).</summary>
        public float targetHeadingDeg;
    }
}
