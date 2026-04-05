// SquadronData.cs — Phase 109: Clan/Squadron System
// All serializable data classes used across the Squadron module.
// Namespace: SWEF.Squadron

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Squadron
{
    // ════════════════════════════════════════════════════════════════════════════
    // Squadron info record
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Core record describing a squadron.
    /// Persisted as part of <c>squadron_data.json</c>.
    /// </summary>
    [Serializable]
    public class SquadronInfo
    {
        /// <summary>Unique squadron identifier (GUID string).</summary>
        public string squadronId;

        /// <summary>Display name of the squadron.</summary>
        public string name;

        /// <summary>Short tag shown in brackets beside member names (e.g. "ACE").</summary>
        public string tag;

        /// <summary>Long-form description visible on the recruitment page.</summary>
        public string description;

        /// <summary>Primary focus area of this squadron.</summary>
        public SquadronType type;

        /// <summary>Current operational status.</summary>
        public SquadronStatus status;

        /// <summary>Member ID of the current leader.</summary>
        public string leaderId;

        /// <summary>UTC timestamp when the squadron was created.</summary>
        public long createdAt;

        /// <summary>Current squadron level (1–50).</summary>
        public int level;

        /// <summary>Total accumulated XP for the squadron.</summary>
        public int totalXP;

        /// <summary>Current number of members.</summary>
        public int memberCount;

        /// <summary>Maximum allowed members (may be &lt;= <see cref="SquadronConfig.MaxMembers"/>).</summary>
        public int maxMembers;

        /// <summary>Serialised emblem data (colours, icon, pattern).</summary>
        public string emblemData;

        /// <summary>Localisation key for the squadron motto.</summary>
        public string mottoLocKey;

        /// <summary>Whether the squadron is currently accepting applications.</summary>
        public bool isRecruiting;

        /// <summary>Minimum player level required to join (0 = no requirement).</summary>
        public int requirementMinLevel;

        /// <summary>Minimum total flight hours required to join (0 = no requirement).</summary>
        public float requirementMinFlightHours;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Member record
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Record for a single squadron member.
    /// Persisted as part of <c>squadron_members.json</c>.
    /// </summary>
    [Serializable]
    public class SquadronMember
    {
        /// <summary>Unique member / player identifier.</summary>
        public string memberId;

        /// <summary>Player's display name.</summary>
        public string displayName;

        /// <summary>Current rank within the squadron.</summary>
        public SquadronRank rank;

        /// <summary>UTC timestamp when the member joined.</summary>
        public long joinedAt;

        /// <summary>XP this member has contributed to the squadron.</summary>
        public int contributionXP;

        /// <summary>UTC timestamp of the member's last recorded activity.</summary>
        public long lastActive;

        /// <summary>Total number of squadron missions this member has participated in.</summary>
        public int totalSquadronFlights;

        /// <summary>Optional custom role description (e.g. "Wing Commander").</summary>
        public string roleDescription;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Mission record
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Squadron-specific cooperative mission.
    /// Persisted as part of <c>squadron_missions.json</c>.
    /// </summary>
    [Serializable]
    public class SquadronMission
    {
        /// <summary>Unique mission identifier (GUID string).</summary>
        public string missionId;

        /// <summary>Mission category / gameplay type.</summary>
        public SquadronMissionType missionType;

        /// <summary>Display title of the mission.</summary>
        public string title;

        /// <summary>Detailed description shown in the mission briefing.</summary>
        public string description;

        /// <summary>List of objective descriptions (shown as a checklist).</summary>
        public List<string> objectives = new List<string>();

        /// <summary>Minimum number of members needed to start the mission.</summary>
        public int requiredMembers;

        /// <summary>Serialised reward data (XP amounts, item IDs, etc.).</summary>
        public string rewards;

        /// <summary>Mission difficulty rating (1–5).</summary>
        public int difficulty;

        /// <summary>Time limit in seconds (0 = no limit).</summary>
        public float timeLimit;

        /// <summary>Whether this mission is currently in progress.</summary>
        public bool isActive;

        /// <summary>Member IDs of current/past participants.</summary>
        public List<string> participantIds = new List<string>();

        /// <summary>Indices of objectives that have been completed.</summary>
        public List<int> completedObjectives = new List<int>();

        /// <summary>UTC timestamp when the mission started.</summary>
        public long startedAt;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Scheduled event record
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A scheduled squadron event (training session, tournament, etc.).
    /// Persisted as part of <c>squadron_events.json</c>.
    /// </summary>
    [Serializable]
    public class SquadronEvent
    {
        /// <summary>Unique event identifier (GUID string).</summary>
        public string eventId;

        /// <summary>Recurrence / category of the event.</summary>
        public SquadronEventType eventType;

        /// <summary>Display title of the event.</summary>
        public string title;

        /// <summary>Detailed description of the event.</summary>
        public string description;

        /// <summary>UTC timestamp of the event start time.</summary>
        public long startTime;

        /// <summary>UTC timestamp of the event end time.</summary>
        public long endTime;

        /// <summary>Human-readable location / region name.</summary>
        public string location;

        /// <summary>Minimum participants required for the event to run.</summary>
        public int requiredMembers;

        /// <summary>Serialised reward data.</summary>
        public string rewards;

        /// <summary>Whether the event recurs on a schedule.</summary>
        public bool isRecurring;

        /// <summary>Cron-like or human-readable recurrence pattern (e.g. "every Saturday 20:00 UTC").</summary>
        public string recurrencePattern;

        /// <summary>Member RSVP responses keyed by member ID.</summary>
        public Dictionary<string, SquadronRSVP> rsvpMap = new Dictionary<string, SquadronRSVP>();

        /// <summary>Member IDs of participants that actually attended.</summary>
        public List<string> attendedMemberIds = new List<string>();
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Base configuration
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Configuration for a squadron's base.
    /// Persisted as <c>squadron_base.json</c>.
    /// </summary>
    [Serializable]
    public class SquadronBase
    {
        /// <summary>Unique base identifier (GUID string).</summary>
        public string baseId;

        /// <summary>Squadron that owns this base.</summary>
        public string squadronId;

        /// <summary>World/map location identifier (e.g. region code).</summary>
        public string location;

        /// <summary>Current level of each facility, keyed by <see cref="SquadronFacility"/>.</summary>
        public Dictionary<SquadronFacility, int> facilityLevels = new Dictionary<SquadronFacility, int>();

        /// <summary>Serialised decoration / placement data.</summary>
        public List<string> decorations = new List<string>();

        /// <summary>Serialised custom paint / signage data.</summary>
        public List<string> customizations = new List<string>();

        /// <summary>List of area identifiers that have been unlocked at this base.</summary>
        public List<string> unlockedAreas = new List<string>();
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Invite record
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A pending or resolved invite to join a squadron.
    /// </summary>
    [Serializable]
    public class SquadronInvite
    {
        /// <summary>Unique invite identifier (GUID string).</summary>
        public string inviteId;

        /// <summary>Squadron the invite is for.</summary>
        public string squadronId;

        /// <summary>Member ID of the player who sent the invite.</summary>
        public string inviterId;

        /// <summary>Member ID of the player who received the invite.</summary>
        public string inviteeId;

        /// <summary>Current status of the invite.</summary>
        public SquadronInviteStatus status;

        /// <summary>UTC timestamp when the invite was sent.</summary>
        public long sentAt;

        /// <summary>UTC timestamp when the invite expires.</summary>
        public long expiresAt;

        /// <summary>Optional personal message from the inviter.</summary>
        public string message;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Leaderboard entry
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A single entry in the squadron leaderboard.
    /// </summary>
    [Serializable]
    public class SquadronLeaderboardEntry
    {
        /// <summary>Squadron identifier.</summary>
        public string squadronId;

        /// <summary>Squadron display name (cached for UI).</summary>
        public string squadronName;

        /// <summary>Score for the chosen ranking metric.</summary>
        public long score;

        /// <summary>Current rank position (1-based).</summary>
        public int rank;

        /// <summary>Category used to compute this ranking.</summary>
        public SquadronLeaderboardCategory category;

        /// <summary>Time period this entry covers.</summary>
        public SquadronLeaderboardPeriod period;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Chat message
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>A single message in the squadron chat history.</summary>
    [Serializable]
    public class SquadronChatMessage
    {
        /// <summary>Unique message identifier (GUID string).</summary>
        public string messageId;

        /// <summary>Squadron this message belongs to.</summary>
        public string squadronId;

        /// <summary>Member ID of the sender.</summary>
        public string senderId;

        /// <summary>Display name of the sender (cached).</summary>
        public string senderName;

        /// <summary>Rank of the sender at send time.</summary>
        public SquadronRank senderRank;

        /// <summary>Message body text.</summary>
        public string text;

        /// <summary>UTC timestamp when the message was sent.</summary>
        public long sentAt;

        /// <summary>True if this is a pinned officer/leader announcement.</summary>
        public bool isPinned;

        /// <summary>True if this is a system-generated message (member joined, etc.).</summary>
        public bool isSystem;
    }
}
