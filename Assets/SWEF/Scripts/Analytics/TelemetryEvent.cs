using System;
using System.Collections.Generic;

namespace SWEF.Analytics
{
    /// <summary>
    /// Immutable telemetry event record captured at a single point in time.
    /// </summary>
    [Serializable]
    public class TelemetryEvent
    {
        /// <summary>Unique event identifier (GUID).</summary>
        public string eventId;

        /// <summary>Human-readable event name (e.g. "flight_start").</summary>
        public string eventName;

        /// <summary>Broad category: "flight", "ui", "purchase", "social", "performance", "error".</summary>
        public string category;

        /// <summary>Arbitrary key-value properties attached to this event.</summary>
        public Dictionary<string, object> properties;

        /// <summary>Wall-clock time when the event was created (UTC).</summary>
        public DateTime timestamp;

        /// <summary>Identifier of the session in which the event was captured.</summary>
        public string sessionId;

        /// <summary>Anonymized device identifier — never the raw hardware ID.</summary>
        public string userId;

        /// <summary>Monotonically increasing counter per session; used for ordering.</summary>
        public int sequenceNumber;

        internal TelemetryEvent() { }
    }

    // ────────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Fluent builder for <see cref="TelemetryEvent"/>.
    /// </summary>
    public class TelemetryEventBuilder
    {
        private readonly TelemetryEvent _event;

        private TelemetryEventBuilder(string eventName)
        {
            _event = new TelemetryEvent
            {
                eventId    = Guid.NewGuid().ToString(),
                eventName  = eventName,
                timestamp  = DateTime.UtcNow,
                properties = new Dictionary<string, object>(),
            };
        }

        /// <summary>Starts building a new event with the given name.</summary>
        public static TelemetryEventBuilder Create(string eventName) =>
            new TelemetryEventBuilder(eventName);

        /// <summary>Sets the event category.</summary>
        public TelemetryEventBuilder WithCategory(string category)
        {
            _event.category = category;
            return this;
        }

        /// <summary>Adds a single property key/value pair.</summary>
        public TelemetryEventBuilder WithProperty(string key, object value)
        {
            _event.properties[key] = value;
            return this;
        }

        /// <summary>Merges a dictionary of properties into the event.</summary>
        public TelemetryEventBuilder WithProperties(Dictionary<string, object> props)
        {
            if (props == null) return this;
            foreach (var kvp in props)
                _event.properties[kvp.Key] = kvp.Value;
            return this;
        }

        /// <summary>
        /// Finalises and returns the built <see cref="TelemetryEvent"/>.
        /// Session-level fields (sessionId, userId, sequenceNumber) are filled
        /// by <see cref="TelemetryDispatcher"/> when the event is enqueued.
        /// </summary>
        public TelemetryEvent Build() => _event;
    }

    // ────────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Standard event name constants used throughout the analytics pipeline.
    /// </summary>
    public static class AnalyticsEvents
    {
        // ── Flight ───────────────────────────────────────────────────────────────
        public const string FlightStart       = "flight_start";
        public const string FlightEnd         = "flight_end";
        public const string AltitudeMilestone = "altitude_milestone";
        public const string SpeedRecord       = "speed_record";
        public const string FlightDuration    = "flight_duration";

        // ── UI ───────────────────────────────────────────────────────────────────
        public const string ScreenView      = "screen_view";
        public const string ButtonClick     = "button_click";
        public const string SettingsChange  = "settings_change";
        public const string TutorialStep    = "tutorial_step";

        // ── Purchase ─────────────────────────────────────────────────────────────
        public const string IapInitiated  = "iap_initiated";
        public const string IapCompleted  = "iap_completed";
        public const string IapFailed     = "iap_failed";
        public const string AdShown       = "ad_shown";
        public const string AdClicked     = "ad_clicked";

        // ── Social ───────────────────────────────────────────────────────────────
        public const string ShareInitiated    = "share_initiated";
        public const string ShareCompleted    = "share_completed";
        public const string LeaderboardView   = "leaderboard_view";
        public const string MultiplayerJoin   = "multiplayer_join";

        // ── Performance ──────────────────────────────────────────────────────────
        public const string FpsDrop       = "fps_drop";
        public const string MemoryWarning = "memory_warning";
        public const string CrashReport   = "crash_report";
        public const string LoadingTime   = "loading_time";

        // ── Error ────────────────────────────────────────────────────────────────
        public const string ErrorCaught     = "error_caught";
        public const string NetworkFailure  = "network_failure";
        public const string ApiTimeout      = "api_timeout";

        // ── Session ──────────────────────────────────────────────────────────────
        public const string SessionStart   = "session_start";
        public const string SessionEnd     = "session_end";
        public const string SessionSummary = "session_summary";

        // ── Achievement ──────────────────────────────────────────────────────────
        public const string AchievementUnlocked = "achievement_unlocked";

        // ── A/B Testing ──────────────────────────────────────────────────────────
        public const string AbTestExposure   = "ab_test_exposure";
        public const string AbTestConversion = "ab_test_conversion";

        // ── Misc ─────────────────────────────────────────────────────────────────
        public const string FeatureDiscovery  = "feature_discovery";
        public const string QualityChange     = "quality_change";
        public const string AltitudeSample    = "altitude_sample";
        public const string FpsSample         = "fps_sample";
    }
}
