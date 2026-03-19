using System;

namespace SWEF.Leaderboard
{
    /// <summary>
    /// 글로벌 리더보드 항목. 서버 응답 및 로컬 캐시에 사용됩니다.
    /// Serializable data class for a single global leaderboard entry.
    /// </summary>
    [Serializable]
    public class GlobalLeaderboardEntry
    {
        /// <summary>Unique player identifier (UUID).</summary>
        public string playerId;

        /// <summary>Player display name (2–20 chars).</summary>
        public string displayName;

        /// <summary>Optional URL to the player's avatar image.</summary>
        public string avatarUrl;

        /// <summary>Maximum altitude reached during the flight (meters).</summary>
        public float maxAltitude;

        /// <summary>Maximum speed reached during the flight (m/s).</summary>
        public float maxSpeed;

        /// <summary>Total flight duration (seconds).</summary>
        public float flightDuration;

        /// <summary>Calculated leaderboard score.</summary>
        public float score;

        /// <summary>Global rank position (1-based).</summary>
        public int rank;

        /// <summary>ISO 3166-1 alpha-2 country code (e.g. "KR", "US").</summary>
        public string region;

        /// <summary>ISO 8601 date string of the recorded flight.</summary>
        public string date;

        /// <summary>Optional serialized flight path summary.</summary>
        public string flightPath;

        /// <summary>
        /// 점수 계산 공식: 고도 * 1.0 + 속도 * 0.5 + 비행시간 * 0.3
        /// Weighted scoring formula: altitude × 1.0 + speed × 0.5 + duration × 0.3.
        /// </summary>
        /// <param name="altitude">Max altitude in meters.</param>
        /// <param name="speed">Max speed in m/s.</param>
        /// <param name="duration">Flight duration in seconds.</param>
        /// <returns>Composite score value.</returns>
        public static float CalculateScore(float altitude, float speed, float duration)
        {
            return altitude * 1.0f + speed * 0.5f + duration * 0.3f;
        }
    }
}
