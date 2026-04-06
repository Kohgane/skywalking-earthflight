// SeasonalLeaderboard.cs — Phase 120: Precision Landing Challenge System
// Seasonal rankings: monthly/seasonal resets, rewards for top performers, tier promotions.
// Namespace: SWEF.LandingChallenge

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages seasonal/monthly leaderboard resets and top-performer rewards.
    /// Tracks cumulative seasonal scores and issues tier promotions.
    /// </summary>
    public class SeasonalLeaderboard : MonoBehaviour
    {
        // ── Tier Enum ─────────────────────────────────────────────────────────

        public enum SeasonTier { Bronze, Silver, Gold, Platinum, Diamond }

        // ── Season Entry ──────────────────────────────────────────────────────

        [Serializable]
        public class SeasonEntry
        {
            public string     PlayerId;
            public string     PlayerName;
            public float      SeasonScore;
            public SeasonTier Tier;
            public int        Rank;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Season Configuration")]
        [SerializeField] private bool  monthlyReset = true;
        [SerializeField] private int   topRewardCount = 100;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly List<SeasonEntry> _entries   = new List<SeasonEntry>();
        private int                        _seasonYear;
        private int                        _seasonMonth;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when the season resets.</summary>
        public event Action<int, int> OnSeasonReset;

        /// <summary>Raised when a player's tier changes.</summary>
        public event Action<string, SeasonTier> OnTierPromotion;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current season year.</summary>
        public int SeasonYear => _seasonYear;

        /// <summary>Current season month.</summary>
        public int SeasonMonth => _seasonMonth;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _seasonYear  = DateTime.UtcNow.Year;
            _seasonMonth = DateTime.UtcNow.Month;
        }

        private void Update()
        {
            if (!monthlyReset) return;
            var now = DateTime.UtcNow;
            if (now.Year != _seasonYear || now.Month != _seasonMonth)
                ResetSeason(now.Year, now.Month);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Add or update a player's seasonal score.</summary>
        public void RecordScore(string playerId, string playerName, float score)
        {
            CheckSeasonRollover();
            var entry = _entries.Find(e => e.PlayerId == playerId);
            if (entry == null)
            {
                entry = new SeasonEntry { PlayerId = playerId, PlayerName = playerName };
                _entries.Add(entry);
            }

            SeasonTier prev = entry.Tier;
            entry.SeasonScore += score;
            entry.Tier         = ComputeTier(entry.SeasonScore);

            if (entry.Tier > prev)
                OnTierPromotion?.Invoke(playerId, entry.Tier);

            SortAndRank();
        }

        /// <summary>Returns top-N season entries.</summary>
        public IReadOnlyList<SeasonEntry> GetTopEntries(int top = 10)
        {
            int count = Mathf.Min(top, _entries.Count);
            return _entries.GetRange(0, count);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void CheckSeasonRollover()
        {
            var now = DateTime.UtcNow;
            if (now.Year != _seasonYear || now.Month != _seasonMonth)
                ResetSeason(now.Year, now.Month);
        }

        private void ResetSeason(int year, int month)
        {
            OnSeasonReset?.Invoke(_seasonYear, _seasonMonth);
            _entries.Clear();
            _seasonYear  = year;
            _seasonMonth = month;
        }

        private void SortAndRank()
        {
            _entries.Sort((a, b) => b.SeasonScore.CompareTo(a.SeasonScore));
            for (int i = 0; i < _entries.Count; i++)
                _entries[i].Rank = i + 1;
        }

        private SeasonTier ComputeTier(float score)
        {
            if (score >= 10000f) return SeasonTier.Diamond;
            if (score >= 5000f)  return SeasonTier.Platinum;
            if (score >= 2000f)  return SeasonTier.Gold;
            if (score >= 500f)   return SeasonTier.Silver;
            return SeasonTier.Bronze;
        }
    }
}
