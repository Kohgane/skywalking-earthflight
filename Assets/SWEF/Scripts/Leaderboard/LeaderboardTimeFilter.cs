namespace SWEF.Leaderboard
{
    /// <summary>
    /// 리더보드 시간 필터. 조회할 기간 범위를 정의합니다.
    /// Defines the time window for leaderboard queries.
    /// </summary>
    public enum LeaderboardTimeFilter
    {
        /// <summary>모든 시간 — All-time records.</summary>
        AllTime,

        /// <summary>이번 달 — Current calendar month.</summary>
        Monthly,

        /// <summary>이번 주 — Current calendar week.</summary>
        Weekly,

        /// <summary>오늘 — Current day (UTC).</summary>
        Daily
    }
}
