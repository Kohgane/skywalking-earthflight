// RaceResultData.cs — SWEF Competitive Racing & Time Trial System (Phase 88)
using System;
using System.Collections.Generic;

namespace SWEF.CompetitiveRacing
{
    // ── CheckpointSplit ───────────────────────────────────────────────────────────

    /// <summary>
    /// Split-time record for one checkpoint captured during a race.
    /// </summary>
    [Serializable]
    public class CheckpointSplit
    {
        /// <summary>Zero-based index of the captured checkpoint.</summary>
        public int checkpointIndex;

        /// <summary>Elapsed race time when this checkpoint was captured (seconds).</summary>
        public float elapsedTime;

        /// <summary>Time from the previous checkpoint to this one (seconds).</summary>
        public float splitTime;

        /// <summary>
        /// Difference between this split and the player's personal-best split at the
        /// same checkpoint index.  Negative = faster than PB.
        /// </summary>
        public float deltaToBest;
    }

    // ── RaceResult ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Complete result for a finished race session.  Saved locally and submitted to
    /// <c>GlobalLeaderboardService</c> by <see cref="RaceManager"/>.
    /// </summary>
    [Serializable]
    public class RaceResult
    {
        /// <summary>Unique identifier for this result record (GUID).</summary>
        public string resultId;

        /// <summary>Identifier of the course that was raced.</summary>
        public string courseId;

        /// <summary>Player account identifier.</summary>
        public string playerId;

        /// <summary>Display name of the player.</summary>
        public string playerName;

        /// <summary>Race mode that was used.</summary>
        public RaceMode mode;

        /// <summary>Total elapsed time for the race (seconds).</summary>
        public float totalTime;

        /// <summary>Number of laps completed (circuit mode).</summary>
        public int lapCount;

        /// <summary>Per-checkpoint split records in order.</summary>
        public List<CheckpointSplit> splits = new List<CheckpointSplit>();

        /// <summary>True if this run improves the player's previous best on this course.</summary>
        public bool isPersonalBest;

        /// <summary>True if this run sets a new global record for the course.</summary>
        public bool isNewRecord;

        /// <summary>UTC timestamp when the race finished.</summary>
        public DateTime raceDate;

        /// <summary>
        /// Identifier of the flight recording stored for ghost-replay purposes.
        /// Empty string if no replay was saved.
        /// </summary>
        public string replayId;
    }

    // ── SeasonEntry ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Metadata for one competition season managed by
    /// <see cref="SeasonalLeaderboardManager"/>.
    /// </summary>
    [Serializable]
    public class SeasonEntry
    {
        /// <summary>Unique identifier for this season (e.g. "2026-S1").</summary>
        public string seasonId;

        /// <summary>Calendar season type.</summary>
        public SeasonType season;

        /// <summary>Calendar year of this season.</summary>
        public int year;

        /// <summary>UTC start of the season window.</summary>
        public DateTime startDate;

        /// <summary>UTC end of the season window.</summary>
        public DateTime endDate;

        /// <summary>
        /// Course IDs curated as featured courses for this season.
        /// </summary>
        public List<string> featuredCourseIds = new List<string>();
    }
}
