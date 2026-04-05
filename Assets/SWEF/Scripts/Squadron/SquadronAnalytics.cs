// SquadronAnalytics.cs — Phase 109: Clan/Squadron System
// Telemetry events for the Squadron module. All calls guarded by SWEF_ANALYTICS_AVAILABLE.
// Namespace: SWEF.Squadron

using UnityEngine;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — Static analytics helper for the Squadron system.
    /// Emits telemetry events for all key squadron interactions.
    /// All calls are compiled out unless <c>SWEF_ANALYTICS_AVAILABLE</c> is defined.
    /// </summary>
    public static class SquadronAnalytics
    {
        // ── Squadron lifecycle ─────────────────────────────────────────────────

        /// <summary>Records that a new squadron was created.</summary>
        public static void TrackSquadronCreated(string squadronId, SquadronType type)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Track("squadron_created", new System.Collections.Generic.Dictionary<string, object>
            {
                { "squadron_id", squadronId },
                { "type", type.ToString() }
            });
#endif
        }

        /// <summary>Records that a player joined a squadron.</summary>
        public static void TrackSquadronJoined(string squadronId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Track("squadron_joined", new System.Collections.Generic.Dictionary<string, object>
            {
                { "squadron_id", squadronId }
            });
#endif
        }

        /// <summary>Records that a player left a squadron.</summary>
        public static void TrackSquadronLeft(string squadronId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Track("squadron_left", new System.Collections.Generic.Dictionary<string, object>
            {
                { "squadron_id", squadronId }
            });
#endif
        }

        /// <summary>Records that a squadron was disbanded.</summary>
        public static void TrackSquadronDisbanded(string squadronId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Track("squadron_disbanded", new System.Collections.Generic.Dictionary<string, object>
            {
                { "squadron_id", squadronId }
            });
#endif
        }

        // ── Mission events ─────────────────────────────────────────────────────

        /// <summary>Records that a squadron mission was started.</summary>
        public static void TrackMissionStarted(string missionId, SquadronMissionType missionType, int participantCount)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Track("squadron_mission_started", new System.Collections.Generic.Dictionary<string, object>
            {
                { "mission_id",        missionId },
                { "mission_type",      missionType.ToString() },
                { "participant_count", participantCount }
            });
#endif
        }

        /// <summary>Records that a squadron mission was completed.</summary>
        public static void TrackMissionCompleted(string missionId, int duration)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Track("squadron_mission_completed", new System.Collections.Generic.Dictionary<string, object>
            {
                { "mission_id", missionId },
                { "duration",   duration }
            });
#endif
        }

        // ── Event events ───────────────────────────────────────────────────────

        /// <summary>Records that a squadron event was created.</summary>
        public static void TrackEventCreated(string eventId, SquadronEventType eventType)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Track("squadron_event_created", new System.Collections.Generic.Dictionary<string, object>
            {
                { "event_id",   eventId },
                { "event_type", eventType.ToString() }
            });
#endif
        }

        // ── Base events ────────────────────────────────────────────────────────

        /// <summary>Records that a base facility was upgraded.</summary>
        public static void TrackFacilityUpgraded(SquadronFacility facility, int newLevel)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Track("squadron_facility_upgraded", new System.Collections.Generic.Dictionary<string, object>
            {
                { "facility",  facility.ToString() },
                { "new_level", newLevel }
            });
#endif
        }

        // ── Member events ──────────────────────────────────────────────────────

        /// <summary>Records that a member was promoted.</summary>
        public static void TrackMemberPromoted(string memberId, SquadronRank newRank)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Track("squadron_member_promoted", new System.Collections.Generic.Dictionary<string, object>
            {
                { "member_id", memberId },
                { "new_rank",  newRank.ToString() }
            });
#endif
        }

        // ── Chat events ────────────────────────────────────────────────────────

        /// <summary>Records that a chat message was sent.</summary>
        public static void TrackChatSent(bool isPinned)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Track("squadron_chat_sent", new System.Collections.Generic.Dictionary<string, object>
            {
                { "is_pinned", isPinned }
            });
#endif
        }
    }
}
