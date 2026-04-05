// SpectatorAnalytics.cs — SWEF Phase 107: Live Streaming & Spectator Mode
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Spectator
{
    /// <summary>
    /// Phase 107 — Telemetry bridge for the Spectator / Streaming system.
    ///
    /// <para>Tracks spectator mode usage, camera mode popularity, and stream duration.
    /// All telemetry calls are guarded by <c>#if SWEF_ANALYTICS_AVAILABLE</c> so
    /// the class compiles and is fully usable without the analytics module.</para>
    /// </summary>
    public sealed class SpectatorAnalytics : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SpectatorAnalytics Instance { get; private set; }

        // ── Internal session data ──────────────────────────────────────────────
        private float _spectatorSessionStart;
        private float _streamSessionStart;
        private readonly Dictionary<SpectatorCameraMode, int> _cameraModeUsage =
            new Dictionary<SpectatorCameraMode, int>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            var spectator = SpectatorModeController.Instance;
            if (spectator != null)
            {
                spectator.OnSpectatorModeEntered   += OnSpectatorEntered;
                spectator.OnSpectatorModeExited    += OnSpectatorExited;
                spectator.OnCameraModeChanged      += OnCameraModeChanged;
            }

            var streaming = StreamingIntegrationManager.Instance;
            if (streaming != null)
            {
                streaming.OnStreamStarted += OnStreamStarted;
                streaming.OnStreamEnded   += OnStreamEnded;
            }
        }

        private void OnDisable()
        {
            var spectator = SpectatorModeController.Instance;
            if (spectator != null)
            {
                spectator.OnSpectatorModeEntered   -= OnSpectatorEntered;
                spectator.OnSpectatorModeExited    -= OnSpectatorExited;
                spectator.OnCameraModeChanged      -= OnCameraModeChanged;
            }

            var streaming = StreamingIntegrationManager.Instance;
            if (streaming != null)
            {
                streaming.OnStreamStarted -= OnStreamStarted;
                streaming.OnStreamEnded   -= OnStreamEnded;
            }
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void OnSpectatorEntered()
        {
            _spectatorSessionStart = Time.time;
            Emit("spectator_mode_entered", ("timestamp", (object)Time.time));
        }

        private void OnSpectatorExited()
        {
            float duration = Time.time - _spectatorSessionStart;
            Emit("spectator_mode_exited", ("duration_seconds", (object)duration));
        }

        private void OnCameraModeChanged(SpectatorCameraMode mode)
        {
            if (!_cameraModeUsage.ContainsKey(mode)) _cameraModeUsage[mode] = 0;
            _cameraModeUsage[mode]++;
            Emit("spectator_camera_mode_changed", ("mode", (object)mode.ToString()),
                                                  ("total_uses", (object)_cameraModeUsage[mode]));
        }

        private void OnStreamStarted(StreamingPlatform platform)
        {
            _streamSessionStart = Time.time;
            Emit("stream_started", ("platform", (object)platform.ToString()));
        }

        private void OnStreamEnded()
        {
            float duration = Time.time - _streamSessionStart;
            Emit("stream_ended", ("duration_seconds", (object)duration));
        }

        // ── Emit helper ────────────────────────────────────────────────────────

        private static void Emit(string eventName, params (string key, object value)[] props)
        {
#if SWEF_ANALYTICS_AVAILABLE
            var dict = new Dictionary<string, object>(props.Length);
            foreach (var (key, value) in props)
                dict[key] = value;
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent(eventName, dict);
#else
            if (Debug.isDebugBuild)
            {
                var sb = new System.Text.StringBuilder($"[SpectatorAnalytics] {eventName}");
                foreach (var (key, value) in props)
                    sb.Append($" | {key}={value}");
                Debug.Log(sb.ToString());
            }
#endif
        }
    }
}
