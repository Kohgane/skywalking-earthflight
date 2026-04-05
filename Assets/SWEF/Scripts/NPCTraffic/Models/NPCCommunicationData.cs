// NPCCommunicationData.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Radio communication and formation flight data models.
// Namespace: SWEF.NPCTraffic

using System;
using System.Collections.Generic;

namespace SWEF.NPCTraffic
{
    // ════════════════════════════════════════════════════════════════════════════
    // Message types
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Category of an NPC radio communication message.</summary>
    public enum NPCMessageType
    {
        /// <summary>Initial contact / frequency check-in.</summary>
        Startup          = 0,
        /// <summary>Taxi clearance request or readback.</summary>
        TaxiClearance    = 1,
        /// <summary>Takeoff clearance request or readback.</summary>
        TakeoffClearance = 2,
        /// <summary>Departure report or frequency change.</summary>
        Departure        = 3,
        /// <summary>Cruise check-in on a new sector frequency.</summary>
        CruiseCheckIn    = 4,
        /// <summary>Descent / approach clearance request or readback.</summary>
        DescentClearance = 5,
        /// <summary>ILS / visual approach establishment report.</summary>
        ApproachReport   = 6,
        /// <summary>Landing report or runway vacated call.</summary>
        LandingReport    = 7,
        /// <summary>TCAS resolution advisory or traffic advisories.</summary>
        TrafficAdvisory  = 8,
        /// <summary>Emergency declaration or urgency message.</summary>
        Emergency        = 9,
        /// <summary>Formation invitation sent to player or another NPC.</summary>
        FormationInvite  = 10,
        /// <summary>Formation acceptance or rejection reply.</summary>
        FormationReply   = 11,
        /// <summary>Generic position report.</summary>
        PositionReport   = 12
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Single radio message
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>A single radio message exchanged between NPC and ATC (or player).</summary>
    [Serializable]
    public class NPCRadioMessage
    {
        /// <summary>Radio frequency in MHz on which the message was transmitted.</summary>
        public float FrequencyMHz;

        /// <summary>Type / context of the message.</summary>
        public NPCMessageType MessageType;

        /// <summary>Callsign of the transmitting station.</summary>
        public string SenderCallsign;

        /// <summary>Callsign of the intended recipient (or "ALL" for broadcasts).</summary>
        public string ReceiverCallsign;

        /// <summary>Plain-text content of the transmission.</summary>
        public string Content;

        /// <summary>Game-time timestamp of the transmission.</summary>
        public float TimestampSeconds;

        /// <summary>Duration of the audio playback in seconds.</summary>
        public float AudioDurationSeconds;

        /// <summary>Whether this message was addressed to or heard by the player.</summary>
        public bool IsPlayerRelevant;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Formation flight data
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Describes a formation flight group.</summary>
    [Serializable]
    public class NPCFormationData
    {
        /// <summary>Unique identifier for this formation.</summary>
        public string FormationId;

        /// <summary>Callsign of the lead aircraft.</summary>
        public string LeadCallsign;

        /// <summary>Callsigns of all wingmen (excluding lead).</summary>
        public List<string> WingmanCallsigns = new List<string>();

        /// <summary>Whether the player has joined this formation.</summary>
        public bool PlayerIsWingman;

        /// <summary>Desired separation distance in metres between formation members.</summary>
        public float SeparationMetres;

        /// <summary>Current active state of the formation.</summary>
        public bool IsActive;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Callsign generation helpers
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Static helper that generates realistic airline callsigns.
    /// </summary>
    public static class NPCCallsignGenerator
    {
        private static readonly string[] _airlinePrefixes =
        {
            "UAL", "DAL", "AAL", "SWA", "BAW", "DLH", "AFR", "KLM",
            "QFA", "SIA", "ANA", "JAL", "CPA", "EZY", "RYR", "IBE",
            "TAM", "GOL", "AMX", "EIN"
        };

        private static readonly string[] _militaryPrefixes =
        {
            "VIPER", "EAGLE", "COBRA", "RAPTOR", "TALON", "GHOST", "BARON"
        };

        private static readonly string[] _cargoCallsigns =
        {
            "FDX", "UPS", "ABX", "ATN", "GTI", "PAC", "CLX"
        };

        private static int _counter;

        /// <summary>
        /// Generates a unique callsign appropriate for the given aircraft category.
        /// </summary>
        /// <param name="category">NPC aircraft category.</param>
        /// <returns>Callsign string such as "UAL4231" or "VIPER11".</returns>
        public static string Generate(NPCAircraftCategory category)
        {
            _counter++;
            switch (category)
            {
                case NPCAircraftCategory.MilitaryAircraft:
                {
                    string prefix = _militaryPrefixes[_counter % _militaryPrefixes.Length];
                    return $"{prefix}{_counter % 99 + 1}";
                }
                case NPCAircraftCategory.CargoPlane:
                {
                    string prefix = _cargoCallsigns[_counter % _cargoCallsigns.Length];
                    return $"{prefix}{100 + _counter % 900}";
                }
                case NPCAircraftCategory.Helicopter:
                    return $"HELI{_counter % 999 + 1}";
                case NPCAircraftCategory.TrainingAircraft:
                    return $"STUDENT{_counter % 99 + 1}";
                default:
                {
                    string prefix = _airlinePrefixes[_counter % _airlinePrefixes.Length];
                    return $"{prefix}{1000 + _counter % 9000}";
                }
            }
        }
    }
}
