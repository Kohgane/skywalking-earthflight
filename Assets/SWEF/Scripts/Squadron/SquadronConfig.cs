// SquadronConfig.cs — Phase 109: Clan/Squadron System
// Static configuration constants for the Squadron module.
// Namespace: SWEF.Squadron

using System.Collections.Generic;

namespace SWEF.Squadron
{
    /// <summary>
    /// Static configuration constants for the Clan/Squadron system.
    /// All tunable values are centralised here to keep other classes clean.
    /// </summary>
    public static class SquadronConfig
    {
        // ── Membership limits ────────────────────────────────────────────────

        /// <summary>Maximum members allowed per squadron.</summary>
        public const int MaxMembers = 50;

        /// <summary>Maximum officers allowed per squadron (excluding Leader).</summary>
        public const int MaxOfficers = 5;

        /// <summary>Maximum pending outgoing invites a squadron may have at once.</summary>
        public const int MaxPendingInvites = 20;

        // ── Name / tag constraints ───────────────────────────────────────────

        /// <summary>Minimum character length for a squadron name.</summary>
        public const int NameMinLength = 3;

        /// <summary>Maximum character length for a squadron name.</summary>
        public const int NameMaxLength = 30;

        /// <summary>Minimum character length for a squadron tag (e.g. "ACE").</summary>
        public const int TagMinLength = 2;

        /// <summary>Maximum character length for a squadron tag.</summary>
        public const int TagMaxLength = 6;

        /// <summary>Maximum character length for a squadron description.</summary>
        public const int DescriptionMaxLength = 500;

        /// <summary>Maximum character length for a member's role description.</summary>
        public const int RoleDescriptionMaxLength = 100;

        // ── Facility limits ──────────────────────────────────────────────────

        /// <summary>Maximum level a single base facility can reach.</summary>
        public const int FacilityMaxLevel = 5;

        /// <summary>
        /// XP cost to upgrade a facility from level (index) to level+1.
        /// Index 0 = upgrade from level 0 → 1, index 4 = level 4 → 5.
        /// </summary>
        public static readonly int[] FacilityUpgradeCosts = { 500, 1200, 2500, 5000, 10000 };

        // ── Mission settings ─────────────────────────────────────────────────

        /// <summary>Cooldown in seconds before the same mission template can be started again.</summary>
        public const int MissionCooldownSeconds = 3600; // 1 hour

        /// <summary>Maximum number of simultaneous active squadron missions.</summary>
        public const int MaxActiveMissions = 3;

        /// <summary>Maximum number of participants that can join a single mission.</summary>
        public const int MaxMissionParticipants = 20;

        // ── Event settings ───────────────────────────────────────────────────

        /// <summary>Minimum number of participants required to start a squadron event.</summary>
        public const int EventMinParticipants = 2;

        /// <summary>Maximum number of simultaneous upcoming squadron events.</summary>
        public const int MaxUpcomingEvents = 10;

        /// <summary>How many days before expiry an invite is considered "expiring soon".</summary>
        public const int InviteExpiryDays = 7;

        // ── XP / levelling ───────────────────────────────────────────────────

        /// <summary>
        /// Cumulative total XP required to reach each squadron level (index = level, 1-50).
        /// Index 0 is unused; index 1 = XP to reach level 1 (always 0 — starting level).
        /// </summary>
        public static readonly int[] LevelXPRequirements = BuildLevelXPTable();

        private static int[] BuildLevelXPTable()
        {
            // 50 levels; level 1 starts at 0 XP, each subsequent level requires
            // progressively more cumulative XP using a quadratic curve.
            var table = new int[51];
            table[0] = 0;
            table[1] = 0;
            for (int lvl = 2; lvl <= 50; lvl++)
                table[lvl] = (int)(500 * lvl * lvl * 0.5);
            return table;
        }

        // ── Persistence paths ────────────────────────────────────────────────

        /// <summary>File name for persisted squadron info.</summary>
        public const string SquadronDataFile = "squadron_data.json";

        /// <summary>File name for persisted squadron member list.</summary>
        public const string MembersDataFile = "squadron_members.json";

        /// <summary>File name for persisted active/completed missions.</summary>
        public const string MissionsDataFile = "squadron_missions.json";

        /// <summary>File name for persisted scheduled events.</summary>
        public const string EventsDataFile = "squadron_events.json";

        /// <summary>File name for persisted base configuration.</summary>
        public const string BaseDataFile = "squadron_base.json";

        /// <summary>File name for persisted chat history.</summary>
        public const string ChatDataFile = "squadron_chat.json";

        // ── Chat settings ────────────────────────────────────────────────────

        /// <summary>Maximum number of chat messages retained in history.</summary>
        public const int ChatHistoryMax = 200;

        /// <summary>Maximum character length of a single chat message.</summary>
        public const int ChatMessageMaxLength = 300;

        // ── Facility bonus identifiers ───────────────────────────────────────

        /// <summary>Extra aircraft slots provided per Hangar level.</summary>
        public const int HangarSlotsPerLevel = 2;

        /// <summary>Fuel efficiency bonus percent per FuelDepot level.</summary>
        public const float FuelDepotEfficiencyPerLevel = 0.05f; // 5 % per level

        /// <summary>Repair speed multiplier bonus per RepairBay level.</summary>
        public const float RepairBaySpeedPerLevel = 0.10f; // 10 % per level

        /// <summary>Mission XP bonus percent per BriefingRoom level.</summary>
        public const float BriefingRoomXPBonusPerLevel = 0.05f; // 5 % per level
    }
}
