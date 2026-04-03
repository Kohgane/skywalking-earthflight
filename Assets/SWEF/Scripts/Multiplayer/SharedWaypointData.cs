using System;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>Category that classifies a community-shared waypoint.</summary>
    public enum WaypointCategory
    {
        /// <summary>Visually stunning location worth visiting.</summary>
        Scenic,
        /// <summary>Real-world airport or airfield.</summary>
        Airport,
        /// <summary>Skill-based challenge location.</summary>
        Challenge,
        /// <summary>Player-defined custom point of interest.</summary>
        Custom,
        /// <summary>Waypoint associated with an active cross-session event.</summary>
        Event
    }

    /// <summary>
    /// Serializable record for a waypoint shared within the SWEF community.
    /// Persisted in <c>shared_waypoints.json</c> and exchanged via deep link
    /// <c>swef://waypoint?id=xxx</c>.
    /// </summary>
    [Serializable]
    public class SharedWaypointData
    {
        /// <summary>Unique identifier for this waypoint (GUID string).</summary>
        [Tooltip("Unique waypoint ID (GUID).")]
        public string waypointId;

        /// <summary>Human-readable waypoint name.</summary>
        [Tooltip("Display name shown on the map and in lists.")]
        public string name;

        /// <summary>Latitude of the waypoint (decimal degrees, WGS-84).</summary>
        [Tooltip("Latitude (decimal degrees).")]
        public double latitude;

        /// <summary>Longitude of the waypoint (decimal degrees, WGS-84).</summary>
        [Tooltip("Longitude (decimal degrees).")]
        public double longitude;

        /// <summary>Altitude above sea level in metres.</summary>
        [Tooltip("Altitude above sea level (metres).")]
        public double altitude;

        /// <summary>Player ID of the player who shared this waypoint.</summary>
        [Tooltip("Player ID of the sharer.")]
        public string sharedBy;

        /// <summary>UTC timestamp (ISO-8601) when the waypoint was shared.</summary>
        [Tooltip("Sharing date/time (UTC ISO-8601).")]
        public string sharedAt;

        /// <summary>Optional description or tip about this waypoint.</summary>
        [Tooltip("Optional player-written description.")]
        public string description;

        /// <summary>Category classifying what kind of location this is.</summary>
        [Tooltip("Waypoint category.")]
        public WaypointCategory category;

        /// <summary>Total number of likes received from the community.</summary>
        [Tooltip("Cumulative community likes.")]
        public int likes;

        /// <summary>Whether this waypoint is visible to all players (vs. friends-only).</summary>
        [Tooltip("True if visible to every player; false if friends-only.")]
        public bool isPublic;
    }
}
