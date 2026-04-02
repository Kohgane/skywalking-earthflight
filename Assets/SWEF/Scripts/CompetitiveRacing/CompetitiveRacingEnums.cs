// CompetitiveRacingEnums.cs — SWEF Competitive Racing & Time Trial System (Phase 88)

namespace SWEF.CompetitiveRacing
{
    /// <summary>Race format / mode selector.</summary>
    public enum RaceMode
    {
        TimeTrial,
        Sprint,
        Circuit,
        Endurance,
        Relay,
        Elimination
    }

    /// <summary>Lifecycle state of a race session.</summary>
    public enum RaceStatus
    {
        Setup,
        Countdown,
        Racing,
        Paused,
        Finished,
        Abandoned
    }

    /// <summary>Functional role of a checkpoint gate in the course.</summary>
    public enum CheckpointType
    {
        Standard,
        Split,
        Sector,
        Bonus,
        Penalty,
        Start,
        Finish
    }

    /// <summary>Terrain/biome environment category of a race course.</summary>
    public enum CourseEnvironment
    {
        Urban,
        Mountain,
        Coastal,
        Desert,
        Arctic,
        Canyon,
        Space,
        Mixed
    }

    /// <summary>Skill level required to complete the course cleanly.</summary>
    public enum CourseDifficulty
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert,
        Extreme
    }

    /// <summary>Calendar season used for seasonal leaderboard cycles.</summary>
    public enum SeasonType
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    /// <summary>Scope filter for leaderboard queries.</summary>
    public enum LeaderboardScope
    {
        Global,
        Friends,
        Regional,
        Weekly,
        Seasonal,
        AllTime
    }

    /// <summary>Runtime alert types broadcast to the player during a race.</summary>
    public enum RaceAlertType
    {
        CheckpointMissed,
        WrongWay,
        NewPersonalBest,
        NewRecord,
        LapComplete,
        RaceFinished,
        Elimination,
        BonusCheckpoint
    }
}
