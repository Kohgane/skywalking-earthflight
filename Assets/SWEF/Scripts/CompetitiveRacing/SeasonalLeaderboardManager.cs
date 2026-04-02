// SeasonalLeaderboardManager.cs — SWEF Competitive Racing & Time Trial System (Phase 88)
using System;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_LEADERBOARD_AVAILABLE
using SWEF.Leaderboard;
#endif

#if SWEF_ACHIEVEMENT_AVAILABLE
using SWEF.Achievement;
#endif

namespace SWEF.CompetitiveRacing
{
    /// <summary>
    /// Phase 88 — Singleton MonoBehaviour that manages seasonal racing competition
    /// cycles.  Derives the current <see cref="SeasonType"/> from the real-world date
    /// and rotates featured courses accordingly.
    ///
    /// <para>Attach to a persistent scene object — uses DontDestroyOnLoad.</para>
    /// </summary>
    public sealed class SeasonalLeaderboardManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static SeasonalLeaderboardManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Featured Courses per Season")]
        [Tooltip("Courses featured during Spring. Set course IDs.")]
        [SerializeField] private List<string> _springCourseIds  = new List<string>();

        [Tooltip("Courses featured during Summer.")]
        [SerializeField] private List<string> _summerCourseIds  = new List<string>();

        [Tooltip("Courses featured during Autumn.")]
        [SerializeField] private List<string> _autumnCourseIds  = new List<string>();

        [Tooltip("Courses featured during Winter.")]
        [SerializeField] private List<string> _winterCourseIds  = new List<string>();

        [Header("Season Rewards")]
        [Tooltip("XP bonus awarded when a season ends.")]
        [SerializeField] [Min(0)] private int _seasonEndXPBonus = 500;

        #endregion

        #region Public State

        /// <summary>Metadata for the currently active season.</summary>
        public SeasonEntry currentSeason { get; private set; }

        #endregion

        #region Events

        /// <summary>Raised when the season rotates to a new <see cref="SeasonType"/>.</summary>
        public event Action<SeasonEntry>   OnSeasonChanged;

        /// <summary>Raised when the player earns a seasonal reward. Arg = reward description.</summary>
        public event Action<string>        OnSeasonalRewardEarned;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RefreshSeason();
            Debug.Log("[SWEF] SeasonalLeaderboardManager: initialised.");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns the ordered list of course IDs featured during the current season.
        /// </summary>
        public IReadOnlyList<string> GetFeaturedCourses()
        {
            return currentSeason?.featuredCourseIds ?? new List<string>();
        }

        /// <summary>
        /// Queries the leaderboard service for seasonal entries for
        /// <paramref name="courseId"/> filtered to <paramref name="season"/>.
        /// </summary>
        public void GetSeasonalLeaderboard(string courseId, SeasonType season,
            Action<List<object>> onComplete)
        {
#if SWEF_LEADERBOARD_AVAILABLE
            string categoryId = $"{courseId}_{season}_{currentSeason?.year}";
            GlobalLeaderboardService.Instance?.GetLeaderboard(
                categoryId,
                LeaderboardTimeFilter.AllTime,
                1,
                CompetitiveRacingConfig.LeaderboardPageSize,
                page => onComplete?.Invoke(new List<object>(page.entries as System.Collections.IEnumerable)),
                err  => Debug.LogWarning($"[SWEF] SeasonalLeaderboard fetch failed: {err}"));
#else
            onComplete?.Invoke(new List<object>());
#endif
        }

        /// <summary>
        /// Call at season end to distribute XP and unlock seasonal achievements.
        /// </summary>
        public void AwardSeasonRewards(string playerId)
        {
            string reward = $"+{_seasonEndXPBonus} XP (Season {currentSeason?.seasonId})";

#if SWEF_ACHIEVEMENT_AVAILABLE
            AchievementManager.Instance?.TryUnlock("season_complete");
#endif
            OnSeasonalRewardEarned?.Invoke(reward);
            Debug.Log($"[SWEF] SeasonalLeaderboardManager: Season reward earned — {reward}");
        }

        /// <summary>Forces a re-evaluation of which season is currently active.</summary>
        public void RefreshSeason()
        {
            var now        = DateTime.UtcNow;
            var season     = DateToSeasonType(now);
            int year       = now.Year;
            string id      = $"{year}-{season}";

            if (currentSeason != null && currentSeason.seasonId == id) return;

            var entry = new SeasonEntry
            {
                seasonId         = id,
                season           = season,
                year             = year,
                startDate        = SeasonStart(season, year),
                endDate          = SeasonEnd(season, year),
                featuredCourseIds = GetFeaturedForSeason(season)
            };

            currentSeason = entry;
            OnSeasonChanged?.Invoke(currentSeason);
            Debug.Log($"[SWEF] SeasonalLeaderboardManager: Season changed → {id}");
        }

        #endregion

        #region Private Helpers

        private static SeasonType DateToSeasonType(DateTime d)
        {
            return d.Month switch
            {
                3 or 4 or 5   => SeasonType.Spring,
                6 or 7 or 8   => SeasonType.Summer,
                9 or 10 or 11 => SeasonType.Autumn,
                _             => SeasonType.Winter
            };
        }

        private static DateTime SeasonStart(SeasonType s, int year)
        {
            return s switch
            {
                SeasonType.Spring => new DateTime(year, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                SeasonType.Summer => new DateTime(year, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                SeasonType.Autumn => new DateTime(year, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                _                 => new DateTime(year, 12, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        private static DateTime SeasonEnd(SeasonType s, int year)
        {
            return s switch
            {
                SeasonType.Spring => new DateTime(year, 5, 31, 23, 59, 59, DateTimeKind.Utc),
                SeasonType.Summer => new DateTime(year, 8, 31, 23, 59, 59, DateTimeKind.Utc),
                SeasonType.Autumn => new DateTime(year, 11, 30, 23, 59, 59, DateTimeKind.Utc),
                // Winter ends at the last day of February next year (accounts for leap years)
                _                 => new DateTime(year + 1, 2,
                                         DateTime.DaysInMonth(year + 1, 2),
                                         23, 59, 59, DateTimeKind.Utc)
            };
        }

        private List<string> GetFeaturedForSeason(SeasonType season)
        {
            return season switch
            {
                SeasonType.Spring => new List<string>(_springCourseIds),
                SeasonType.Summer => new List<string>(_summerCourseIds),
                SeasonType.Autumn => new List<string>(_autumnCourseIds),
                _                 => new List<string>(_winterCourseIds)
            };
        }

        #endregion
    }
}
