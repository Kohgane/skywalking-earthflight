// LeaderboardEntry.cs — Phase 120: Precision Landing Challenge System
// Entry data: player, score, grade, aircraft, weather, replay ID, timestamp.
// Namespace: SWEF.LandingChallenge

using System;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Represents a single entry in a landing challenge leaderboard.
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        /// <summary>Unique entry identifier.</summary>
        public string EntryId;

        /// <summary>Player/user identifier.</summary>
        public string PlayerId;

        /// <summary>Display name of the player.</summary>
        public string PlayerDisplayName;

        /// <summary>Challenge ID this entry belongs to.</summary>
        public string ChallengeId;

        /// <summary>Total score achieved (0–1000).</summary>
        public float Score;

        /// <summary>Landing grade awarded.</summary>
        public LandingGrade Grade;

        /// <summary>Aircraft type used (e.g. "Boeing 737-800").</summary>
        public string AircraftType;

        /// <summary>Weather preset in effect during the attempt.</summary>
        public WeatherPreset Weather;

        /// <summary>Replay recording identifier for ghost download.</summary>
        public string ReplayId;

        /// <summary>UTC timestamp of the attempt.</summary>
        public DateTime Timestamp;

        /// <summary>Number of stars earned.</summary>
        public int Stars;

        /// <summary>Rank position on the leaderboard (1 = top).</summary>
        public int Rank;

        /// <summary>
        /// Factory method: create an entry from a completed landing result.
        /// </summary>
        public static LeaderboardEntry FromResult(string playerId, string playerName,
                                                  string challengeId, LandingResult result,
                                                  string aircraftType, WeatherPreset weather,
                                                  string replayId = null)
        {
            return new LeaderboardEntry
            {
                EntryId            = Guid.NewGuid().ToString(),
                PlayerId           = playerId,
                PlayerDisplayName  = playerName,
                ChallengeId        = challengeId,
                Score              = result.TotalScore,
                Grade              = result.Grade,
                AircraftType       = aircraftType,
                Weather            = weather,
                ReplayId           = replayId ?? string.Empty,
                Timestamp          = result.Timestamp,
                Stars              = result.Stars
            };
        }
    }
}
