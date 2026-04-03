using System.Collections.Generic;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Static analytics helper for the Multiplayer system.
    /// All events are forwarded to <c>TelemetryDispatcher</c> when analytics are available.
    /// </summary>
    public static class MultiplayerAnalytics
    {
        // ── Session ──────────────────────────────────────────────────────────────

        /// <summary>Records a new multiplayer session being created.</summary>
        public static void RecordSessionCreated(string sessionType)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("session_created",
                new Dictionary<string, object> { { "session_type", sessionType } });
#endif
        }

        /// <summary>Records the local player joining an existing session.</summary>
        public static void RecordSessionJoined(string sessionType)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("session_joined",
                new Dictionary<string, object> { { "session_type", sessionType } });
#endif
        }

        /// <summary>Records the local player leaving a session.</summary>
        public static void RecordSessionLeft()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("session_left", null);
#endif
        }

        /// <summary>Records a position synchronisation tick (internal, high-frequency).</summary>
        public static void RecordPositionSync()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("position_sync", null);
#endif
        }

        // ── Friends ──────────────────────────────────────────────────────────────

        /// <summary>Records a new friend being added.</summary>
        public static void RecordFriendAdded()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("friend_added", null);
#endif
        }

        /// <summary>Records a friend being removed.</summary>
        public static void RecordFriendRemoved()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("friend_removed", null);
#endif
        }

        /// <summary>Records a flight invitation being sent to a friend.</summary>
        public static void RecordFriendInvited()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("friend_invited", null);
#endif
        }

        // ── Waypoints ────────────────────────────────────────────────────────────

        /// <summary>Records a community waypoint being shared.</summary>
        public static void RecordWaypointShared(string category)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("waypoint_shared",
                new Dictionary<string, object> { { "category", category } });
#endif
        }

        /// <summary>Records a waypoint receiving a like.</summary>
        public static void RecordWaypointLiked()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("waypoint_liked", null);
#endif
        }

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Records the local player joining a cross-session event.</summary>
        public static void RecordEventJoined(string eventType)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("event_joined",
                new Dictionary<string, object> { { "event_type", eventType } });
#endif
        }

        /// <summary>Records the local player completing a cross-session event.</summary>
        public static void RecordEventCompleted(string eventType)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("event_completed",
                new Dictionary<string, object> { { "event_type", eventType } });
#endif
        }

        // ── Formation ────────────────────────────────────────────────────────────

        /// <summary>Records a formation being formed between players.</summary>
        public static void RecordFormationFormed(int memberCount)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("formation_formed",
                new Dictionary<string, object> { { "member_count", memberCount } });
#endif
        }

        // ── Communication ────────────────────────────────────────────────────────

        /// <summary>Records a chat message being sent.</summary>
        public static void RecordChatMessageSent()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("chat_message_sent", null);
#endif
        }

        /// <summary>Records a pilot emote being used.</summary>
        public static void RecordEmoteUsed(string emoteName)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("emote_used",
                new Dictionary<string, object> { { "emote", emoteName } });
#endif
        }

        // ── Collaborative flight planning ─────────────────────────────────────────

        /// <summary>Records a waypoint being added to a collaborative flight plan.</summary>
        public static void RecordFlightPlanWaypointAdded()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("flight_plan_waypoint_added", null);
#endif
        }

        /// <summary>Records a collaborative flight plan being shared.</summary>
        public static void RecordFlightPlanShared()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("flight_plan_shared", null);
#endif
        }
    }
}
