using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>Type of collaborative flight session.</summary>
    public enum SessionType
    {
        /// <summary>Open free-roam session with no objectives.</summary>
        FreeFlight,
        /// <summary>Structured close-formation flying practice.</summary>
        Formation,
        /// <summary>Competitive race using the Phase 88 Racing system.</summary>
        Race,
        /// <summary>Cooperative mission using the PassengerCargo system.</summary>
        Mission,
        /// <summary>Guided scenic tour along shared waypoints.</summary>
        Tour
    }

    /// <summary>Lifecycle state of a flight session.</summary>
    public enum SessionStatus
    {
        /// <summary>Waiting for players to join before the session starts.</summary>
        Lobby,
        /// <summary>Session is active and players are in the air.</summary>
        InProgress,
        /// <summary>Session has ended and results are available.</summary>
        Completed
    }

    /// <summary>
    /// Serializable record describing a multiplayer flight session.
    /// Persisted in <c>multiplayer_sessions.json</c> as session history.
    /// </summary>
    [Serializable]
    public class FlightSessionData
    {
        /// <summary>Unique session identifier (GUID string).</summary>
        [Tooltip("Unique session ID (GUID).")]
        public string sessionId;

        /// <summary>Player ID of the session host.</summary>
        [Tooltip("Player ID of the host who created this session.")]
        public string hostId;

        /// <summary>Player IDs of all participants (including host).</summary>
        [Tooltip("All participant player IDs.")]
        public List<string> participants = new List<string>();

        /// <summary>The type of activity this session is structured around.</summary>
        [Tooltip("Session activity type.")]
        public SessionType sessionType;

        /// <summary>UTC timestamp (ISO-8601) when the session started.</summary>
        [Tooltip("Session start time (UTC ISO-8601).")]
        public string startTime;

        /// <summary>UTC timestamp (ISO-8601) when the session ended (empty if in progress).</summary>
        [Tooltip("Session end time (UTC ISO-8601). Empty while in progress.")]
        public string endTime;

        /// <summary>Maximum number of players allowed in this session.</summary>
        [Tooltip("Maximum participant count.")]
        public int maxParticipants;

        /// <summary>Whether the session is discoverable by all players.</summary>
        [Tooltip("True if the session appears in public discovery listings.")]
        public bool isPublic;

        /// <summary>Shared route waypoints for Tour/Formation sessions.</summary>
        [Tooltip("Optional shared route waypoint IDs.")]
        public List<string> waypoints = new List<string>();

        /// <summary>Current lifecycle state of the session.</summary>
        [Tooltip("Session lifecycle status.")]
        public SessionStatus status;
    }
}
