// SquadronEnums.cs — Phase 109: Clan/Squadron System
// All enumerations used across the Squadron module.
// Namespace: SWEF.Squadron

namespace SWEF.Squadron
{
    // ════════════════════════════════════════════════════════════════════════════
    // Member rank within a squadron — controls permissions
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Rank of a squadron member, from highest authority to lowest.</summary>
    public enum SquadronRank
    {
        /// <summary>Sole leader of the squadron.</summary>
        Leader = 0,
        /// <summary>Officer with elevated management rights.</summary>
        Officer = 1,
        /// <summary>Long-serving member with trusted status.</summary>
        Veteran = 2,
        /// <summary>Standard squadron member.</summary>
        Member = 3,
        /// <summary>Newly joined, probationary rank.</summary>
        Recruit = 4
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Squadron type / focus area
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Primary activity focus of the squadron.</summary>
    public enum SquadronType
    {
        Casual       = 0,
        Competitive  = 1,
        Exploration  = 2,
        Training     = 3,
        Military     = 4,
        Aerobatics   = 5
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Lifecycle status of the squadron
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Operational status of a squadron.</summary>
    public enum SquadronStatus
    {
        Active    = 0,
        Inactive  = 1,
        Disbanded = 2,
        Suspended = 3
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Mission types available to squadrons
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Type of cooperative squadron mission.</summary>
    public enum SquadronMissionType
    {
        FormationFlight = 0,
        AreaExploration = 1,
        SpeedRun        = 2,
        RelayRace       = 3,
        PatrolRoute     = 4,
        SearchAndRescue = 5,
        AirShow         = 6,
        ConvoyEscort    = 7
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Scheduled event types
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Recurrence / category of a squadron event.</summary>
    public enum SquadronEventType
    {
        Weekly       = 0,
        Monthly      = 1,
        Seasonal     = 2,
        Special      = 3,
        Tournament   = 4,
        Anniversary  = 5
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Base facilities
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Facility types that can be built or upgraded at a squadron base.</summary>
    public enum SquadronFacility
    {
        Hangar         = 0,
        ControlTower   = 1,
        BriefingRoom   = 2,
        FuelDepot      = 3,
        RepairBay      = 4,
        RecRoom        = 5,
        TrainingGround = 6,
        TrophyRoom     = 7
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Invite lifecycle
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Current status of a squadron invite.</summary>
    public enum SquadronInviteStatus
    {
        Pending  = 0,
        Accepted = 1,
        Declined = 2,
        Expired  = 3
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Granular permissions assignable per rank
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Individual permission flags checked against a member's rank.</summary>
    public enum SquadronPermission
    {
        InviteMembers  = 0,
        KickMembers    = 1,
        EditBase       = 2,
        StartMission   = 3,
        ManageEvents   = 4,
        EditSettings   = 5,
        PromoteMembers = 6
    }

    // ════════════════════════════════════════════════════════════════════════════
    // RSVP response for squadron events
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>A member's RSVP response to a squadron event.</summary>
    public enum SquadronRSVP
    {
        Attending    = 0,
        Maybe        = 1,
        NotAttending = 2
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Leaderboard ranking category
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Metric used to rank squadrons on the leaderboard.</summary>
    public enum SquadronLeaderboardCategory
    {
        TotalXP            = 0,
        MissionCompletions = 1,
        MemberActivity     = 2,
        EventParticipation = 3
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Leaderboard time period
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Time window for a leaderboard snapshot.</summary>
    public enum SquadronLeaderboardPeriod
    {
        Weekly  = 0,
        Monthly = 1,
        AllTime = 2
    }
}
