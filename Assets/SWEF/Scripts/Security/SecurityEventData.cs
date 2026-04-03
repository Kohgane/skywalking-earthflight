// SecurityEventData.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Classifies the type of security violation that was detected.
    /// </summary>
    public enum SecurityEventType
    {
        /// <summary>A save file was modified outside the game.</summary>
        SaveTamper,
        /// <summary>Time-scale or frame-delta manipulation was detected.</summary>
        SpeedHack,
        /// <summary>An impossible position jump was detected.</summary>
        TeleportHack,
        /// <summary>Currency value changed without a matching transaction.</summary>
        CurrencyManipulation,
        /// <summary>A player exceeded the allowed message or action rate.</summary>
        RateLimit,
        /// <summary>A malformed or unrecognised network packet was received.</summary>
        InvalidPacket,
        /// <summary>A previously seen packet was replayed by a bad actor.</summary>
        ReplayAttack,
        /// <summary>An inventory item appeared without a matching unlock or purchase.</summary>
        PartDuplication
    }

    /// <summary>
    /// Severity tier for a security event; used for filtering and alerting.
    /// </summary>
    public enum SecuritySeverity
    {
        /// <summary>Informational; no immediate action required.</summary>
        Low,
        /// <summary>Suspicious pattern; worth monitoring.</summary>
        Medium,
        /// <summary>Confirmed violation; action recommended.</summary>
        High,
        /// <summary>Severe violation; immediate action required.</summary>
        Critical
    }

    /// <summary>
    /// The automated action taken in response to the security event.
    /// </summary>
    public enum SecurityAction
    {
        /// <summary>Event was written to the security log only.</summary>
        Logged,
        /// <summary>The player received an in-game warning.</summary>
        Warned,
        /// <summary>The player was removed from the current session.</summary>
        Kicked,
        /// <summary>The player's account was temporarily suspended.</summary>
        Banned,
        /// <summary>The affected data was reverted to its last valid state.</summary>
        Reverted
    }

    /// <summary>
    /// Serializable record describing a single security event.
    /// Stored in <c>security_log.json</c> and forwarded to telemetry.
    /// </summary>
    [Serializable]
    public class SecurityEventData
    {
        /// <summary>Unique identifier for this security event.</summary>
        [Tooltip("Unique identifier for this security event.")]
        public string eventId;

        /// <summary>The category of the detected violation.</summary>
        [Tooltip("The category of the detected violation.")]
        public SecurityEventType eventType;

        /// <summary>How severe this violation is considered.</summary>
        [Tooltip("How severe this violation is considered.")]
        public SecuritySeverity severity;

        /// <summary>Identifier of the player associated with this event.</summary>
        [Tooltip("Identifier of the player associated with this event.")]
        public string playerId;

        /// <summary>UTC timestamp (ISO-8601) when the event was recorded.</summary>
        [Tooltip("UTC timestamp (ISO-8601) when the event was recorded.")]
        public string timestamp;

        /// <summary>Human-readable description of the violation.</summary>
        [Tooltip("Human-readable description of the violation.")]
        public string details;

        /// <summary>The automated action taken as a result of this event.</summary>
        [Tooltip("The automated action taken as a result of this event.")]
        public SecurityAction actionTaken;

        /// <summary>
        /// Creates a new <see cref="SecurityEventData"/> pre-populated with an ID and timestamp.
        /// </summary>
        /// <param name="type">Violation category.</param>
        /// <param name="severity">Severity tier.</param>
        /// <param name="playerId">Affected player ID.</param>
        /// <param name="details">Description of what was detected.</param>
        /// <param name="action">Action taken.</param>
        public static SecurityEventData Create(
            SecurityEventType type,
            SecuritySeverity severity,
            string playerId,
            string details,
            SecurityAction action = SecurityAction.Logged)
        {
            return new SecurityEventData
            {
                eventId     = Guid.NewGuid().ToString("N"),
                eventType   = type,
                severity    = severity,
                playerId    = playerId ?? string.Empty,
                timestamp   = DateTime.UtcNow.ToString("o"),
                details     = details ?? string.Empty,
                actionTaken = action
            };
        }
    }
}
