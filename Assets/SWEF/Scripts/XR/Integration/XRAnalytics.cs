// XRAnalytics.cs — Phase 112: VR/XR Flight Experience
// VR-specific telemetry: session duration, comfort usage, gesture accuracy.
// Namespace: SWEF.XR

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Collects and reports VR/XR telemetry data including session duration,
    /// comfort level usage, gesture recognition counts, and platform statistics.
    /// All methods are safe to call regardless of whether analytics are enabled.
    /// </summary>
    public static class XRAnalytics
    {
        // ── Private state ─────────────────────────────────────────────────────────
        private static string _sessionId        = string.Empty;
        private static float  _sessionStartTime = -1f;
        private static int    _gestureCount;
        private static readonly Dictionary<XRComfortLevel, int> _comfortLevelUsage =
            new Dictionary<XRComfortLevel, int>();

        // ── Session ────────────────────────────────────────────────────────────────

        /// <summary>Begins tracking a new XR session.</summary>
        public static void BeginSession(XRPlatform platform)
        {
            _sessionId        = Guid.NewGuid().ToString();
            _sessionStartTime = Time.realtimeSinceStartup;
            _gestureCount     = 0;
            _comfortLevelUsage.Clear();
            Debug.Log($"[SWEF] XRAnalytics: Session started — id={_sessionId}, platform={platform}.");
        }

        /// <summary>Ends the current session and returns a summary event.</summary>
        public static XRAnalyticsEvent EndSession(XRPlatform platform, XRComfortLevel comfort)
        {
            float duration = _sessionStartTime >= 0f
                ? Time.realtimeSinceStartup - _sessionStartTime
                : 0f;

            var evt = new XRAnalyticsEvent
            {
                SessionId              = _sessionId,
                Platform               = platform,
                ComfortLevel           = comfort,
                SessionDurationSeconds = duration,
                GesturesRecognised     = _gestureCount,
                Timestamp              = DateTime.UtcNow.ToString("o")
            };

            Debug.Log($"[SWEF] XRAnalytics: Session ended — duration={duration:F1}s, gestures={_gestureCount}.");
            _sessionStartTime = -1f;
            return evt;
        }

        // ── Tracking ───────────────────────────────────────────────────────────────

        /// <summary>Records a gesture recognition event.</summary>
        public static void TrackGesture(XRHandedness hand, XRGestureType gesture)
        {
            _gestureCount++;
        }

        /// <summary>Records a comfort level change.</summary>
        public static void TrackComfortLevelChange(XRComfortLevel level)
        {
            if (!_comfortLevelUsage.ContainsKey(level))
                _comfortLevelUsage[level] = 0;
            _comfortLevelUsage[level]++;
        }

        /// <summary>Records a teleport event.</summary>
        public static void TrackTeleport(Vector3 destination)
        {
            Debug.Log($"[SWEF] XRAnalytics: Teleport to {destination}.");
        }

        /// <summary>Records a VR photo capture.</summary>
        public static void TrackPhotoCaptured(VRPhotoFormat format)
        {
            Debug.Log($"[SWEF] XRAnalytics: Photo captured ({format}).");
        }

        // ── Accessors ──────────────────────────────────────────────────────────────

        /// <summary>Total gestures recognised in the current session.</summary>
        public static int SessionGestureCount => _gestureCount;

        /// <summary>Current session identifier.</summary>
        public static string SessionId => _sessionId;

        /// <summary>Elapsed session time in seconds.</summary>
        public static float SessionDuration =>
            _sessionStartTime >= 0f ? Time.realtimeSinceStartup - _sessionStartTime : 0f;
    }
}
