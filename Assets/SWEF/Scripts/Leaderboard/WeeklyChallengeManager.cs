using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Leaderboard
{
    /// <summary>
    /// 주간 챌린지 데이터.
    /// </summary>
    [Serializable]
    public class WeeklyChallenge
    {
        public string             id;
        public string             title;
        public string             description;
        public LeaderboardCategory category;
        public float              targetValue;
        public string             startDate;
        public string             endDate;
    }

    /// <summary>
    /// 주간 챌린지 관리자 (MonoBehaviour Singleton).
    /// 서버 없이도 주 번호 기반으로 Mock 챌린지를 생성합니다.
    /// Manages weekly challenges, generates mock challenges based on the current week number.
    /// </summary>
    public class WeeklyChallengeManager : MonoBehaviour
    {
        private static WeeklyChallengeManager _instance;

        /// <summary>Singleton instance.</summary>
        public static WeeklyChallengeManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<WeeklyChallengeManager>();
                return _instance;
            }
        }

        /// <summary>새 챌린지가 활성화될 때 발생합니다.</summary>
        public event Action<WeeklyChallenge> OnNewChallengeAvailable;

        private WeeklyChallenge _currentChallenge;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            GetCurrentChallenge(c => OnNewChallengeAvailable?.Invoke(c));
        }

        /// <summary>
        /// 현재 주간 챌린지를 가져옵니다. Mock 구현: 주 번호 기반으로 생성.
        /// Gets the current weekly challenge. Mock implementation generates based on week number.
        /// </summary>
        public void GetCurrentChallenge(Action<WeeklyChallenge> onComplete)
        {
            StartCoroutine(FetchChallengeCoroutine(onComplete));
        }

        /// <summary>
        /// 챌린지 점수를 제출합니다.
        /// Submits a value for the given challenge.
        /// </summary>
        public void SubmitChallengeScore(string challengeId, float value, Action<SubmitResult> onComplete)
        {
            var service = GlobalLeaderboardService.Instance;
            if (service == null)
            {
                onComplete?.Invoke(new SubmitResult { success = false });
                return;
            }

            var profile = Social.PlayerProfileManager.Instance;
            var entry = new GlobalLeaderboardEntry
            {
                playerId       = profile?.PlayerId ?? "unknown",
                displayName    = profile?.DisplayName ?? "Pilot",
                score          = value,
                region         = profile?.Region ?? string.Empty,
                date           = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            service.SubmitScore(entry, onComplete, err =>
            {
                Debug.LogWarning($"[SWEF] WeeklyChallengeManager: submit failed — {err}");
                onComplete?.Invoke(new SubmitResult { success = false });
            });
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private IEnumerator FetchChallengeCoroutine(Action<WeeklyChallenge> onComplete)
        {
            yield return null; // one frame for service init

            _currentChallenge = GenerateMockChallenge();
            onComplete?.Invoke(_currentChallenge);
        }

        private static WeeklyChallenge GenerateMockChallenge()
        {
            // 주 번호를 seed로 사용하여 재현 가능한 챌린지 생성
            int weekOfYear = System.Globalization.ISOWeek.GetWeekOfYear(DateTime.UtcNow);

            LeaderboardCategory[] cats = {
                LeaderboardCategory.HighestAltitude,
                LeaderboardCategory.FastestSpeed,
                LeaderboardCategory.LongestFlight,
                LeaderboardCategory.BestOverallScore
            };

            var cat = cats[weekOfYear % cats.Length];

            (string title, string desc, float target) = cat switch
            {
                LeaderboardCategory.HighestAltitude => (
                    "성층권 돌파! / Pierce the Stratosphere",
                    "이번 주 최고 고도 50 km 달성 / Reach 50 km altitude this week",
                    50000f),
                LeaderboardCategory.FastestSpeed => (
                    "마하 탈출! / Break the Sound Barrier",
                    "이번 주 마하 10 돌파 / Exceed Mach 10 (3,430 m/s) this week",
                    3430f),
                LeaderboardCategory.LongestFlight => (
                    "마라톤 비행 / Marathon Flight",
                    "이번 주 1시간 이상 비행 / Stay airborne for 60 minutes this week",
                    3600f),
                _ => (
                    "전설 점수 / Legend Score",
                    "이번 주 100,000점 달성 / Score 100,000 points this week",
                    100000f)
            };

            // 이번 주 월요일 ~ 일요일
            DateTime now   = DateTime.UtcNow;
            int daysToMon  = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            DateTime start = now.AddDays(-daysToMon).Date;
            DateTime end   = start.AddDays(7);

            return new WeeklyChallenge
            {
                id          = $"week_{weekOfYear}_{DateTime.UtcNow.Year}",
                title       = title,
                description = desc,
                category    = cat,
                targetValue = target,
                startDate   = start.ToString("yyyy-MM-dd"),
                endDate     = end.ToString("yyyy-MM-dd")
            };
        }
    }
}
