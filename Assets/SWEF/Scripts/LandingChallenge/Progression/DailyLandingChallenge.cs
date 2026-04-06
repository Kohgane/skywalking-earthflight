// DailyLandingChallenge.cs — Phase 120: Precision Landing Challenge System
// Daily challenge: rotating airport/conditions, community participation, daily leaderboard.
// Namespace: SWEF.LandingChallenge

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages the daily rotating landing challenge.
    /// Selects a challenge definition based on the current UTC date, supports
    /// community participation tracking, and provides a daily leaderboard.
    /// </summary>
    public class DailyLandingChallenge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Daily Pool")]
        [SerializeField] private int resetHourUTC = 0;

        // ── State ─────────────────────────────────────────────────────────────

        private ChallengeDefinition _todaysChallenge;
        private DateTime            _lastReset;
        private int                 _participantCount;
        private LandingResult       _personalBest;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when the daily challenge resets to a new selection.</summary>
        public event Action<ChallengeDefinition> OnDailyChallengeReset;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Today's challenge definition, or <c>null</c> if not yet set.</summary>
        public ChallengeDefinition TodaysChallenge => _todaysChallenge;

        /// <summary>Number of community participants today.</summary>
        public int ParticipantCount => _participantCount;

        /// <summary>Personal best result for today's challenge, or <c>null</c>.</summary>
        public LandingResult PersonalBest => _personalBest;

        /// <summary>UTC time of the next daily reset.</summary>
        public DateTime NextResetUTC
        {
            get
            {
                var now  = DateTime.UtcNow;
                var next = new DateTime(now.Year, now.Month, now.Day, resetHourUTC, 0, 0, DateTimeKind.Utc);
                if (next <= now) next = next.AddDays(1);
                return next;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Initialise the daily challenge system, selecting today's challenge
        /// from <paramref name="availableChallenges"/>.
        /// </summary>
        public void Initialise(IReadOnlyList<ChallengeDefinition> availableChallenges)
        {
            SelectDailyChallenge(availableChallenges);
        }

        /// <summary>Tick — check for daily reset.</summary>
        public void Tick(IReadOnlyList<ChallengeDefinition> availableChallenges)
        {
            if (DateTime.UtcNow >= NextResetUTC && _lastReset.Date < DateTime.UtcNow.Date)
                SelectDailyChallenge(availableChallenges);
        }

        /// <summary>Record a result for today's daily challenge.</summary>
        public void RecordResult(LandingResult result)
        {
            _participantCount++;
            if (_personalBest == null || result.TotalScore > _personalBest.TotalScore)
                _personalBest = result;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void SelectDailyChallenge(IReadOnlyList<ChallengeDefinition> pool)
        {
            if (pool == null || pool.Count == 0) return;

            // Deterministic daily selection based on UTC date seed
            int seed  = DateTime.UtcNow.Year * 10000 + DateTime.UtcNow.Month * 100 + DateTime.UtcNow.Day;
            int index = Mathf.Abs(seed) % pool.Count;
            _todaysChallenge  = pool[index];
            _lastReset        = DateTime.UtcNow;
            _participantCount = 0;
            _personalBest     = null;

            OnDailyChallengeReset?.Invoke(_todaysChallenge);
        }
    }
}
