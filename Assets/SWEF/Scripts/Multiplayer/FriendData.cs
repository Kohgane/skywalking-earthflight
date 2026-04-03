using System;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Serializable record of a friendship between two players.
    /// Persisted in <c>friends_list.json</c>.
    /// </summary>
    [Serializable]
    public class FriendData
    {
        /// <summary>The friend's unique player identifier.</summary>
        [Tooltip("Unique player ID of this friend.")]
        public string friendId;

        /// <summary>Cached profile data for this friend.</summary>
        [Tooltip("Last-cached profile snapshot for this friend.")]
        public PlayerProfileData profile;

        /// <summary>UTC timestamp (ISO-8601) when the friendship was established.</summary>
        [Tooltip("Date the friend request was accepted (UTC ISO-8601).")]
        public string friendSince;

        /// <summary>Whether this friend is marked as a favourite in the list.</summary>
        [Tooltip("True if the local player has starred this friend.")]
        public bool isFavorite;

        /// <summary>Number of flight sessions where both players were participants.</summary>
        [Tooltip("How many sessions have been shared with this friend.")]
        public int mutualFlightCount;
    }
}
