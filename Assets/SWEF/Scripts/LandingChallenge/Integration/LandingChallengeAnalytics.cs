// LandingChallengeAnalytics.cs — Phase 120: Precision Landing Challenge System
// Telemetry: challenge attempts, completion rates, average scores, popular challenges, grade distribution.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Telemetry and analytics for landing challenge events.
    /// Tracks challenge attempts, completions, score distributions, and
    /// popular challenge statistics.
    /// </summary>
    public class LandingChallengeAnalytics : MonoBehaviour
    {
        // ── Analytics Record ──────────────────────────────────────────────────

        [System.Serializable]
        private class ChallengeStats
        {
            public string ChallengeId;
            public int    Attempts;
            public int    Completions;
            public float  TotalScore;
            public Dictionary<LandingGrade, int> GradeDistribution = new Dictionary<LandingGrade, int>();
        }

        // ── State ─────────────────────────────────────────────────────────────

        private readonly Dictionary<string, ChallengeStats> _stats = new Dictionary<string, ChallengeStats>();
        private int _totalAttempts;
        private int _totalCompletions;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total challenge attempts across all challenges.</summary>
        public int TotalAttempts => _totalAttempts;

        /// <summary>Total challenge completions (non-crash results).</summary>
        public int TotalCompletions => _totalCompletions;

        /// <summary>Overall completion rate (0–1).</summary>
        public float CompletionRate =>
            _totalAttempts > 0 ? (float)_totalCompletions / _totalAttempts : 0f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Track a challenge start event.</summary>
        public void TrackChallengeStarted(string challengeId, ChallengeType type, DifficultyLevel difficulty)
        {
            var stats = GetOrCreate(challengeId);
            stats.Attempts++;
            _totalAttempts++;
        }

        /// <summary>Track a landing score result.</summary>
        public void TrackLandingScored(float score, LandingGrade grade)
        {
            // Score-level tracking (not tied to a specific challenge)
        }

        /// <summary>Track a challenge completion event.</summary>
        public void TrackChallengeCompleted(string challengeId, float score, int stars)
        {
            var stats = GetOrCreate(challengeId);
            stats.Completions++;
            stats.TotalScore += score;
            _totalCompletions++;
        }

        /// <summary>Track a challenge failure event.</summary>
        public void TrackChallengeFailed(string challengeId)
        {
            // Already counted in attempts
        }

        /// <summary>Track grade distribution for a challenge.</summary>
        public void TrackGrade(string challengeId, LandingGrade grade)
        {
            var stats = GetOrCreate(challengeId);
            if (!stats.GradeDistribution.ContainsKey(grade))
                stats.GradeDistribution[grade] = 0;
            stats.GradeDistribution[grade]++;
        }

        /// <summary>Returns the average score for a challenge, or 0 if no data.</summary>
        public float GetAverageScore(string challengeId)
        {
            if (!_stats.TryGetValue(challengeId, out var stats)) return 0f;
            return stats.Completions > 0 ? stats.TotalScore / stats.Completions : 0f;
        }

        /// <summary>Returns the most-attempted challenge ID, or empty string.</summary>
        public string GetMostPopularChallenge()
        {
            string topId = string.Empty;
            int    topCount = 0;
            foreach (var kvp in _stats)
            {
                if (kvp.Value.Attempts > topCount)
                {
                    topCount = kvp.Value.Attempts;
                    topId    = kvp.Key;
                }
            }
            return topId;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private ChallengeStats GetOrCreate(string challengeId)
        {
            if (!_stats.TryGetValue(challengeId, out var stats))
            {
                stats = new ChallengeStats { ChallengeId = challengeId };
                _stats[challengeId] = stats;
            }
            return stats;
        }
    }
}
