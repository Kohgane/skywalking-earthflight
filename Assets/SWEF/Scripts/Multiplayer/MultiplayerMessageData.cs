using System;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>Type of in-session message sent between players.</summary>
    public enum MessageType
    {
        /// <summary>Free-text chat message.</summary>
        Chat,
        /// <summary>Predefined pilot emote (wave, salute, etc.).</summary>
        Emote,
        /// <summary>Location ping that places a temporary map marker.</summary>
        Ping,
        /// <summary>Automated system notification (player joined, weather warning).</summary>
        SystemAlert,
        /// <summary>Flight session invitation from a friend.</summary>
        FlightInvite,
        /// <summary>Inline waypoint sharing within the chat.</summary>
        WaypointShare
    }

    /// <summary>
    /// Serializable record for a single multiplayer communication message.
    /// Recent messages are persisted in <c>chat_history.json</c> (last 100 entries).
    /// </summary>
    [Serializable]
    public class MultiplayerMessageData
    {
        /// <summary>Unique message identifier (GUID string).</summary>
        [Tooltip("Unique message ID (GUID).")]
        public string messageId;

        /// <summary>Player ID of the sender.</summary>
        [Tooltip("Player ID of the message sender.")]
        public string senderId;

        /// <summary>Classification of this message.</summary>
        [Tooltip("Message type determining how it is rendered and processed.")]
        public MessageType messageType;

        /// <summary>Text payload or serialised data depending on messageType.</summary>
        [Tooltip("Message body (plain text or serialised payload).")]
        public string content;

        /// <summary>UTC timestamp (ISO-8601) when the message was created.</summary>
        [Tooltip("Message creation time (UTC ISO-8601).")]
        public string timestamp;

        /// <summary>
        /// Optional target player ID (e.g. recipient of an invite or ping).
        /// Null/empty for broadcast messages.
        /// </summary>
        [Tooltip("Optional target player ID; empty for broadcast messages.")]
        public string targetId;
    }
}
