using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Static bridge that connects the Multiplayer system to the wider SWEF ecosystem:
    /// Progression, Achievement, SocialActivityFeed, TelemetryDispatcher, and DeepLinkHandler.
    /// All calls are guarded by compile-time symbols so the system degrades gracefully when
    /// a dependency is absent.
    /// </summary>
    public static class MultiplayerBridge
    {
        // ── Achievement keys ─────────────────────────────────────────────────────

        private const string AchievFirstFlight     = "first_multiplayer_flight";
        private const string AchievFormationMaster = "formation_master";
        private const string AchievSocialButterfly = "social_butterfly";
        private const string AchievEventChampion   = "event_champion";
        private const string AchievWaypointExplorer= "waypoint_explorer";
        private const string AchievCollabPlanner   = "collaborative_planner";
        private const string AchievChatVeteran     = "chat_veteran";

        // ── XP awards ────────────────────────────────────────────────────────────

        private const int XpSessionJoined  = 50;
        private const int XpEventCompleted = 200;
        private const int XpWaypointShared = 25;
        private const int XpFormation      = 10;
        private const int XpFlightPlan     = 30;

        // ── Deep link routes ─────────────────────────────────────────────────────

        private const string RouteWaypoint = "waypoint";
        private const string RouteSession  = "session";

        // ── Initialisation ───────────────────────────────────────────────────────

        /// <summary>
        /// Registers deep link routes for <c>swef://waypoint</c> and <c>swef://session</c>.
        /// Call once during application startup (e.g. from a bootstrap MonoBehaviour).
        /// </summary>
        public static void RegisterDeepLinks()
        {
#if SWEF_DEEPLINK_AVAILABLE
            SWEF.Core.DeepLinkHandler.RegisterRoute(RouteWaypoint, url =>
            {
                SharedWaypointManager.Instance?.HandleDeepLink(url);
            });
            SWEF.Core.DeepLinkHandler.RegisterRoute(RouteSession, url =>
            {
                string id = ExtractQueryParam(url, "id");
                if (!string.IsNullOrEmpty(id))
                    MultiplayerSessionManager.Instance?.JoinSession(id);
            });
#endif
        }

        // ── Session callbacks ────────────────────────────────────────────────────

        /// <summary>Called by <see cref="MultiplayerSessionManager"/> when a session is created.</summary>
        public static void OnSessionCreated(FlightSessionData session)
        {
            AddXP(XpSessionJoined);
            ReportAchievement(AchievFirstFlight, 1);
            PostActivity("multiplayer_session_created",
                $"Started a {session?.sessionType} session.");
            EnqueueTelemetry("session_created",
                new Dictionary<string, object>
                {
                    { "session_type", session?.sessionType.ToString() },
                    { "is_public",    session?.isPublic }
                });
        }

        /// <summary>Called by <see cref="MultiplayerSessionManager"/> when the local player joins a session.</summary>
        public static void OnSessionJoined(FlightSessionData session)
        {
            AddXP(XpSessionJoined);
            ReportAchievement(AchievFirstFlight, 1);
            PostActivity("multiplayer_session_joined",
                $"Joined a {session?.sessionType} session.");
            EnqueueTelemetry("session_joined",
                new Dictionary<string, object>
                {
                    { "session_id",   session?.sessionId },
                    { "session_type", session?.sessionType.ToString() }
                });
        }

        // ── Friend callbacks ─────────────────────────────────────────────────────

        /// <summary>Called by <see cref="FriendSystemController"/> when a friend is added.</summary>
        public static void OnFriendAdded(FriendData friend)
        {
            ReportAchievement(AchievSocialButterfly, 1);
            PostActivity("friend_added", $"Added {friend?.profile?.displayName} as a friend.");
            EnqueueTelemetry("friend_added", null);
        }

        // ── Event callbacks ──────────────────────────────────────────────────────

        /// <summary>Called by <see cref="CrossSessionEventManager"/> when the player joins an event.</summary>
        public static void OnEventJoined(CrossSessionEventData evt)
        {
            PostActivity("event_joined", $"Joined the event: {evt?.title}.");
            EnqueueTelemetry("event_joined",
                new Dictionary<string, object> { { "event_type", evt?.eventType.ToString() } });
        }

        /// <summary>Called by <see cref="CrossSessionEventManager"/> when the player completes an event.</summary>
        public static void OnEventCompleted(CrossSessionEventData evt)
        {
            AddXP(XpEventCompleted);
            ReportAchievement(AchievEventChampion, 1);
            PostActivity("event_completed", $"Completed the event: {evt?.title}!");
            EnqueueTelemetry("event_completed",
                new Dictionary<string, object> { { "event_id", evt?.eventId } });
        }

        // ── Waypoint callbacks ───────────────────────────────────────────────────

        /// <summary>Called by <see cref="SharedWaypointManager"/> when a waypoint is shared.</summary>
        public static void OnWaypointShared(SharedWaypointData wp)
        {
            AddXP(XpWaypointShared);
            PostActivity("waypoint_shared", $"Shared waypoint: {wp?.name}.");
            EnqueueTelemetry("waypoint_shared",
                new Dictionary<string, object> { { "category", wp?.category.ToString() } });
        }

        /// <summary>Called by <see cref="SharedWaypointManager"/> when a waypoint is visited/imported.</summary>
        public static void OnWaypointVisited(SharedWaypointData wp)
        {
            ReportAchievement(AchievWaypointExplorer, 1);
            EnqueueTelemetry("waypoint_visited",
                new Dictionary<string, object> { { "waypoint_id", wp?.waypointId } });
        }

        // ── Formation callbacks ──────────────────────────────────────────────────

        /// <summary>Called by <see cref="FriendFlightController"/> when a formation is formed.</summary>
        public static void OnFormationFormed(List<string> members)
        {
            AddXP(XpFormation);
            ReportAchievement(AchievFormationMaster, 1);
            PostActivity("formation_formed", $"Flying in formation with {members?.Count - 1} friend(s).");
        }

        /// <summary>Called by <see cref="FriendFlightController"/> when the formation breaks.</summary>
        public static void OnFormationBroken()
        {
            EnqueueTelemetry("formation_broken", null);
        }

        // ── Flight plan callbacks ────────────────────────────────────────────────

        /// <summary>Called by <see cref="CollaborativeFlightPlanner"/> when a plan is shared.</summary>
        public static void OnFlightPlanShared()
        {
            AddXP(XpFlightPlan);
            ReportAchievement(AchievCollabPlanner, 1);
            PostActivity("flight_plan_shared", "Shared a collaborative flight plan.");
            EnqueueTelemetry("flight_plan_shared", null);
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private static void AddXP(int amount)
        {
#if SWEF_PROGRESSION_AVAILABLE
            SWEF.Progression.ProgressionManager.Instance?.AddXP(amount);
#endif
        }

        private static void ReportAchievement(string id, int progress)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.ReportProgress(id, progress);
#endif
        }

        private static void PostActivity(string activityType, string detail)
        {
#if SWEF_SOCIAL_AVAILABLE
            SWEF.SocialHub.SocialActivityFeed.PostActivity(activityType, detail);
#endif
        }

        private static void EnqueueTelemetry(string eventName,
            Dictionary<string, object> properties)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent(eventName, properties);
#endif
        }

        private static string ExtractQueryParam(string url, string param)
        {
            if (string.IsNullOrEmpty(url)) return null;
            int q = url.IndexOf('?');
            if (q < 0) return null;
            string query = url.Substring(q + 1);
            foreach (string pair in query.Split('&'))
            {
                int eq = pair.IndexOf('=');
                if (eq < 0) continue;
                if (pair.Substring(0, eq) == param)
                    return System.Uri.UnescapeDataString(pair.Substring(eq + 1));
            }
            return null;
        }
    }
}
