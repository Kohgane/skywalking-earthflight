namespace SWEF.Leaderboard
{
    /// <summary>
    /// 리더보드 카테고리. 어떤 기록을 기준으로 순위를 매길지 정의합니다.
    /// Defines the metric by which leaderboard entries are ranked.
    /// </summary>
    public enum LeaderboardCategory
    {
        /// <summary>Ranked by highest altitude reached (m).</summary>
        HighestAltitude,

        /// <summary>Ranked by fastest speed reached (m/s).</summary>
        FastestSpeed,

        /// <summary>Ranked by total flight duration (seconds).</summary>
        LongestFlight,

        /// <summary>Ranked by composite score.</summary>
        BestOverallScore,

        /// <summary>Ranked by number of flights.</summary>
        MostFlights,

        /// <summary>Special time-limited weekly challenge category.</summary>
        WeeklyChallenge
    }

    /// <summary>
    /// Static helper for <see cref="LeaderboardCategory"/> display names.
    /// </summary>
    public static class LeaderboardCategoryHelper
    {
        /// <summary>
        /// 카테고리에 대한 사용자 친화적인 이름을 반환합니다.
        /// Returns a localized display name for the given category.
        /// </summary>
        public static string GetDisplayName(LeaderboardCategory cat)
        {
            return cat switch
            {
                LeaderboardCategory.HighestAltitude  => "최고 고도 / Highest Altitude",
                LeaderboardCategory.FastestSpeed     => "최고 속도 / Fastest Speed",
                LeaderboardCategory.LongestFlight    => "최장 비행 / Longest Flight",
                LeaderboardCategory.BestOverallScore => "종합 점수 / Best Score",
                LeaderboardCategory.MostFlights      => "비행 횟수 / Most Flights",
                LeaderboardCategory.WeeklyChallenge  => "주간 챌린지 / Weekly Challenge",
                _                                    => cat.ToString()
            };
        }
    }
}
