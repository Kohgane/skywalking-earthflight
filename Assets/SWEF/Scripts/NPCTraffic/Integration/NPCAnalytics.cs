// NPCAnalytics.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Telemetry / analytics integration for the NPC Traffic module.
// All calls are guarded with #if SWEF_ANALYTICS_AVAILABLE.
// Namespace: SWEF.NPCTraffic

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Records telemetry events for the NPC Traffic system and
    /// forwards them to the SWEF Analytics backend when available.
    /// All outbound calls are compiled out unless <c>SWEF_ANALYTICS_AVAILABLE</c> is defined.
    /// </summary>
    public static class NPCAnalytics
    {
        // ── Event names ───────────────────────────────────────────────────────

        private const string EvtNPCSpawned        = "npc_spawned";
        private const string EvtNPCDespawned      = "npc_despawned";
        private const string EvtNPCLanded         = "npc_landed";
        private const string EvtNPCEmergency      = "npc_emergency";
        private const string EvtFormationJoined   = "npc_formation_joined";
        private const string EvtFormationLeft     = "npc_formation_left";
        private const string EvtAirportActivated  = "npc_airport_activated";
        private const string EvtDensityChanged    = "npc_density_changed";

        // ── Public logging helpers ─────────────────────────────────────────────

        /// <summary>Records an NPC spawn event.</summary>
        public static void LogNPCSpawned(NPCAircraftData data)
        {
#if SWEF_ANALYTICS_AVAILABLE
            LogEvent(EvtNPCSpawned, new Dictionary<string, object>
            {
                { "callsign",  data.Callsign },
                { "category",  data.Category.ToString() },
                { "origin",    data.OriginICAO },
                { "dest",      data.DestinationICAO },
                { "altitude",  data.AltitudeMetres }
            });
#endif
        }

        /// <summary>Records an NPC despawn event.</summary>
        public static void LogNPCDespawned(string npcId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            LogEvent(EvtNPCDespawned, new Dictionary<string, object> { { "id", npcId } });
#endif
        }

        /// <summary>Records an NPC landing completion event.</summary>
        public static void LogNPCLanded(string callsign, string airportICAO)
        {
#if SWEF_ANALYTICS_AVAILABLE
            LogEvent(EvtNPCLanded, new Dictionary<string, object>
            {
                { "callsign", callsign },
                { "airport",  airportICAO }
            });
#endif
        }

        /// <summary>Records an NPC emergency declaration.</summary>
        public static void LogNPCEmergency(string callsign)
        {
#if SWEF_ANALYTICS_AVAILABLE
            LogEvent(EvtNPCEmergency, new Dictionary<string, object> { { "callsign", callsign } });
#endif
        }

        /// <summary>Records the player joining a formation.</summary>
        public static void LogFormationJoined(string formationId, string leadCallsign)
        {
#if SWEF_ANALYTICS_AVAILABLE
            LogEvent(EvtFormationJoined, new Dictionary<string, object>
            {
                { "formation_id",   formationId },
                { "lead_callsign",  leadCallsign }
            });
#endif
        }

        /// <summary>Records the player leaving a formation.</summary>
        public static void LogFormationLeft(string formationId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            LogEvent(EvtFormationLeft, new Dictionary<string, object> { { "formation_id", formationId } });
#endif
        }

        /// <summary>Records an airport activation event.</summary>
        public static void LogAirportActivated(string icao)
        {
#if SWEF_ANALYTICS_AVAILABLE
            LogEvent(EvtAirportActivated, new Dictionary<string, object> { { "icao", icao } });
#endif
        }

        /// <summary>Records a traffic density change.</summary>
        public static void LogDensityChanged(NPCTrafficDensity density)
        {
#if SWEF_ANALYTICS_AVAILABLE
            LogEvent(EvtDensityChanged, new Dictionary<string, object>
            {
                { "density", density.ToString() }
            });
#endif
        }

        // ── Private dispatch ───────────────────────────────────────────────────

        private static void LogEvent(string eventName, Dictionary<string, object> properties)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Instance?.LogEvent(eventName, properties);
#endif
            // Fallback: Unity debug log in development builds
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NPCAnalytics] {eventName}: {string.Join(", ", properties)}");
#endif
        }
    }
}
