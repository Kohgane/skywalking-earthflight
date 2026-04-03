using System;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>Status of a player in the SWEF multiplayer network.</summary>
    public enum PlayerStatus
    {
        /// <summary>Player is logged in but not in-game.</summary>
        Online,
        /// <summary>Player is currently in a flight session.</summary>
        InFlight,
        /// <summary>Player is browsing the Aircraft Workshop.</summary>
        InWorkshop,
        /// <summary>Player is not connected.</summary>
        Offline
    }

    /// <summary>
    /// Serializable snapshot of a multiplayer player profile.
    /// Stored locally in <c>player_profile.json</c> and exchanged over the network.
    /// </summary>
    [Serializable]
    public class PlayerProfileData
    {
        /// <summary>Unique identifier for this player (GUID string).</summary>
        [Tooltip("Unique player identifier (GUID).")]
        public string playerId;

        /// <summary>Player-visible display name.</summary>
        [Tooltip("Display name shown in lobbies and HUD markers.")]
        public string displayName;

        /// <summary>URL of the player's avatar image.</summary>
        [Tooltip("Remote or cached avatar image URL.")]
        public string avatarUrl;

        /// <summary>Pilot rank label (e.g. Cadet, Ace, Legend).</summary>
        [Tooltip("Pilot rank derived from ProgressionManager.")]
        public string pilotRank;

        /// <summary>Cumulative flight hours logged by this player.</summary>
        [Tooltip("Total in-game flight hours.")]
        public float totalFlightHours;

        /// <summary>ID of the aircraft build currently equipped (from Workshop).</summary>
        [Tooltip("Active aircraft build ID from the Workshop system.")]
        public string activeBuildId;

        /// <summary>Current player availability status.</summary>
        [Tooltip("Online presence status.")]
        public PlayerStatus status;

        /// <summary>UTC timestamp of the player's last known activity.</summary>
        [Tooltip("ISO-8601 UTC timestamp of last seen.")]
        public string lastSeen;

        /// <summary>Latitude of the player's current in-world position.</summary>
        [Tooltip("Current latitude (degrees).")]
        public double currentLatitude;

        /// <summary>Longitude of the player's current in-world position.</summary>
        [Tooltip("Current longitude (degrees).")]
        public double currentLongitude;

        /// <summary>Altitude of the player's current in-world position (metres).</summary>
        [Tooltip("Current altitude above sea level (metres).")]
        public double currentAltitude;
    }
}
