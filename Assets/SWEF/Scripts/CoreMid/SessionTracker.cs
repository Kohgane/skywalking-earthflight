using System;
using UnityEngine;

namespace SWEF.Core
{
    /// <summary>
    /// Lightweight per-session metric tracker.
    /// Attach to any persistent GameObject in the World scene.
    /// Call <see cref="EndSession"/> when the session ends to merge
    /// accumulated data into lifetime PlayerPrefs stats.
    /// </summary>
    public class SessionTracker : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static SessionTracker Instance { get; private set; }

        // ── PlayerPrefs lifetime keys ────────────────────────────────────────
        private const string KeyTotalFlightMinutes = "SWEF_TotalFlightMinutes";
        private const string KeyTotalTeleports     = "SWEF_TotalTeleports";
        private const string KeyTotalScreenshots   = "SWEF_TotalScreenshots";
        private const string KeyLifetimeMaxAlt     = "SWEF_LifetimeMaxAltitude";
        private const string KeyLifetimeMaxSpeed   = "SWEF_LifetimeMaxSpeed";

        // ── Session metrics ──────────────────────────────────────────────────
        /// <summary>Total seconds elapsed in this session.</summary>
        public float SessionDurationSeconds  { get; private set; }

        /// <summary>Cumulative world-space distance traveled this session (metres).</summary>
        public float DistanceTraveledThisSession { get; private set; }

        /// <summary>Number of teleports performed this session.</summary>
        public int TeleportsThisSession { get; private set; }

        /// <summary>Number of screenshots taken this session.</summary>
        public int ScreenshotsThisSession { get; private set; }

        /// <summary>Highest altitude (metres) reached this session.</summary>
        public float MaxAltitudeThisSession { get; private set; }

        /// <summary>Highest speed (m/s) reached this session.</summary>
        public float MaxSpeedThisSession { get; private set; }

        private float _sessionStartTime;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _sessionStartTime = Time.realtimeSinceStartup;
        }

        private void Start()
        {
            // Phase 21 — fire session_start telemetry (deferred to Start so dispatcher exists)
            FireSessionStartTelemetry();
        }

        private void Update()
        {
            SessionDurationSeconds = Time.realtimeSinceStartup - _sessionStartTime;
        }

        // ── Public update methods ────────────────────────────────────────────

        /// <summary>Records one teleport event for this session.</summary>
        public void IncrementTeleport()   => TeleportsThisSession++;

        /// <summary>Records one screenshot event for this session.</summary>
        public void IncrementScreenshot() => ScreenshotsThisSession++;

        /// <summary>
        /// Updates the session's maximum altitude if <paramref name="altitude"/> exceeds the current record.
        /// </summary>
        /// <param name="altitude">Current altitude in metres.</param>
        public void UpdateAltitude(float altitude)
        {
            if (altitude > MaxAltitudeThisSession)
                MaxAltitudeThisSession = altitude;
        }

        /// <summary>
        /// Updates the session's maximum speed if <paramref name="speed"/> exceeds the current record.
        /// </summary>
        /// <param name="speed">Current speed in m/s.</param>
        public void UpdateSpeed(float speed)
        {
            if (speed > MaxSpeedThisSession)
                MaxSpeedThisSession = speed;
        }

        /// <summary>
        /// Adds world-space distance to the session accumulator.
        /// Call each frame with <c>Vector3.Distance(prev, current)</c>.
        /// </summary>
        /// <param name="delta">Distance increment in metres.</param>
        public void AddDistance(float delta)
        {
            if (delta > 0f)
                DistanceTraveledThisSession += delta;
        }

        /// <summary>
        /// Returns a snapshot of the current session metrics.
        /// </summary>
        public SessionSummary GetSummary() => new SessionSummary
        {
            durationSeconds          = SessionDurationSeconds,
            distanceTraveledMeters   = DistanceTraveledThisSession,
            teleports                = TeleportsThisSession,
            screenshots              = ScreenshotsThisSession,
            maxAltitudeMeters        = MaxAltitudeThisSession,
            maxSpeedMps              = MaxSpeedThisSession,
        };

        /// <summary>
        /// Merges this session's data into the lifetime PlayerPrefs stats and saves.
        /// Call when the session ends (app quit or background).
        /// </summary>
        public void EndSession()
        {
            float prevMinutes  = PlayerPrefs.GetFloat(KeyTotalFlightMinutes, 0f);
            int   prevTele     = PlayerPrefs.GetInt(KeyTotalTeleports, 0);
            int   prevShots    = PlayerPrefs.GetInt(KeyTotalScreenshots, 0);
            float prevMaxAlt   = PlayerPrefs.GetFloat(KeyLifetimeMaxAlt, 0f);
            float prevMaxSpeed = PlayerPrefs.GetFloat(KeyLifetimeMaxSpeed, 0f);

            PlayerPrefs.SetFloat(KeyTotalFlightMinutes, prevMinutes + SessionDurationSeconds / 60f);
            PlayerPrefs.SetInt(KeyTotalTeleports,       prevTele + TeleportsThisSession);
            PlayerPrefs.SetInt(KeyTotalScreenshots,     prevShots + ScreenshotsThisSession);
            PlayerPrefs.SetFloat(KeyLifetimeMaxAlt,
                Mathf.Max(prevMaxAlt, MaxAltitudeThisSession));
            PlayerPrefs.SetFloat(KeyLifetimeMaxSpeed,
                Mathf.Max(prevMaxSpeed, MaxSpeedThisSession));
            PlayerPrefs.Save();

            Debug.Log($"[SWEF] SessionTracker: session ended — " +
                      $"{SessionDurationSeconds / 60f:0.1} min, " +
                      $"{DistanceTraveledThisSession / 1000f:0.1} km, " +
                      $"max alt {MaxAltitudeThisSession:0} m, " +
                      $"max speed {MaxSpeedThisSession:0} m/s");

            // Phase 21 — fire session_end telemetry
            FireSessionEndTelemetry();
        }

        // ── Phase 21 — Telemetry helpers ─────────────────────────────────────────

        private void FireSessionStartTelemetry()
        {
            var dispatcher = SWEF.Analytics.TelemetryDispatcher.Instance;
            var pcm        = SWEF.Analytics.PrivacyConsentManager.Instance;
            if (dispatcher == null) return;
            if (pcm != null && !pcm.HasConsent(SWEF.Analytics.PrivacyConsentManager.ConsentLevel.Analytics)) return;

            var evt = SWEF.Analytics.TelemetryEventBuilder.Create(SWEF.Analytics.AnalyticsEvents.SessionStart)
                .WithCategory("performance")
                .WithProperty("deviceModel", SystemInfo.deviceModel)
                .WithProperty("appVersion",  Application.version)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        private void FireSessionEndTelemetry()
        {
            var dispatcher = SWEF.Analytics.TelemetryDispatcher.Instance;
            var pcm        = SWEF.Analytics.PrivacyConsentManager.Instance;
            if (dispatcher == null) return;
            if (pcm != null && !pcm.HasConsent(SWEF.Analytics.PrivacyConsentManager.ConsentLevel.Analytics)) return;

            var evt = SWEF.Analytics.TelemetryEventBuilder.Create(SWEF.Analytics.AnalyticsEvents.SessionEnd)
                .WithCategory("performance")
                .WithProperty("durationSeconds",  SessionDurationSeconds)
                .WithProperty("teleports",        TeleportsThisSession)
                .WithProperty("screenshots",      ScreenshotsThisSession)
                .WithProperty("maxAltitudeM",     MaxAltitudeThisSession)
                .WithProperty("maxSpeedMps",      MaxSpeedThisSession)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }
    }

    /// <summary>Serialisable snapshot of per-session metrics.</summary>
    [Serializable]
    public struct SessionSummary
    {
        public float durationSeconds;
        public float distanceTraveledMeters;
        public int   teleports;
        public int   screenshots;
        public float maxAltitudeMeters;
        public float maxSpeedMps;
    }
}
