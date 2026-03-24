// MissionEnums.cs — SWEF Mission Briefing & Objective System (Phase 70)

namespace SWEF.Mission
{
    /// <summary>Classifies the gameplay style of a mission.</summary>
    public enum MissionType
    {
        /// <summary>Fly a defined route through an area.</summary>
        Patrol,
        /// <summary>Protect and accompany a friendly unit.</summary>
        Escort,
        /// <summary>Transport cargo or passengers to a destination.</summary>
        Delivery,
        /// <summary>Survey or photograph a target area.</summary>
        Recon,
        /// <summary>Complete a timed race course.</summary>
        Racing,
        /// <summary>Engage hostile targets.</summary>
        Combat,
        /// <summary>Locate and extract a person or object.</summary>
        Rescue,
        /// <summary>No structured objectives — open exploration.</summary>
        FreeRoam,
        /// <summary>Guided introduction to a mechanic or system.</summary>
        Tutorial,
        /// <summary>Designer-defined mission type.</summary>
        Custom,
    }

    /// <summary>Lifecycle state of a mission.</summary>
    public enum MissionStatus
    {
        /// <summary>Prerequisites not yet met; mission cannot be started.</summary>
        Locked,
        /// <summary>Mission can be loaded and started.</summary>
        Available,
        /// <summary>Briefing screen is displayed to the player.</summary>
        Briefing,
        /// <summary>Mission is actively running.</summary>
        InProgress,
        /// <summary>Mission timer and logic are suspended.</summary>
        Paused,
        /// <summary>All required objectives finished successfully.</summary>
        Completed,
        /// <summary>A required objective was failed or time expired.</summary>
        Failed,
        /// <summary>Player quit the mission before completion.</summary>
        Abandoned,
    }

    /// <summary>Completion state of a single mission objective.</summary>
    public enum ObjectiveStatus
    {
        /// <summary>Objective has not yet been activated.</summary>
        Pending,
        /// <summary>Objective is currently being tracked.</summary>
        Active,
        /// <summary>Objective was successfully finished.</summary>
        Completed,
        /// <summary>Objective was failed; mission may continue if objective is optional.</summary>
        Failed,
        /// <summary>Objective is optional (bonus).</summary>
        Optional,
    }

    /// <summary>Difficulty rating applied to a mission.</summary>
    public enum MissionDifficulty
    {
        /// <summary>Very gentle introduction; minimal time pressure.</summary>
        Beginner,
        /// <summary>Straightforward mission suitable for new players.</summary>
        Easy,
        /// <summary>Standard challenge for an average player.</summary>
        Normal,
        /// <summary>Requires solid flying skills and awareness.</summary>
        Hard,
        /// <summary>Tight tolerances and demanding objectives.</summary>
        Expert,
        /// <summary>Maximum challenge; near-perfect execution required.</summary>
        Legendary,
    }

    /// <summary>Performance rating awarded at mission completion.</summary>
    public enum MissionRating
    {
        /// <summary>Outstanding — 95 % score or above.</summary>
        S,
        /// <summary>Excellent — 85 % score or above.</summary>
        A,
        /// <summary>Good — 70 % score or above.</summary>
        B,
        /// <summary>Average — 55 % score or above.</summary>
        C,
        /// <summary>Poor — 40 % score or above.</summary>
        D,
        /// <summary>Failed — below 40 % score.</summary>
        F,
    }
}
