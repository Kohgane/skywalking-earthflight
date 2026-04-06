// LeaderboardManager.cs — Phase 116: Flight Analytics Dashboard
// Global/friends leaderboards: total distance, landing accuracy, fuel efficiency.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Manages in-memory leaderboard data and submits scores to a backend
    /// when <c>SWEF_MULTIPLAYER_AVAILABLE</c> is defined.
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared singleton instance.</summary>
        public static LeaderboardManager Instance { get; private set; }

        // ── Leaderboard categories ────────────────────────────────────────────────

        /// <summary>Supported leaderboard category identifiers.</summary>
        public static class Categories
        {
            public const string TotalDistance    = "total_distance_nm";
            public const string LandingAccuracy  = "landing_accuracy";
            public const string FuelEfficiency   = "fuel_efficiency";
            public const string PerformanceScore = "performance_score";
        }

        // ── State ─────────────────────────────────────────────────────────────────

        private readonly Dictionary<string, List<LeaderboardEntry>> _boards
            = new Dictionary<string, List<LeaderboardEntry>>();

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Submit a player score to a leaderboard category.</summary>
        public void SubmitScore(string category, string playerId, string displayName,
                                 float score, PilotTier tier)
        {
#if SWEF_MULTIPLAYER_AVAILABLE
            // TODO: call backend API
            Debug.Log($"[SWEF] LeaderboardManager: Submitting score {score} to {category} for {displayName}.");
#endif
            AddLocalEntry(category, playerId, displayName, score, tier);
        }

        /// <summary>Retrieve the local leaderboard for a category, sorted by score descending.</summary>
        public List<LeaderboardEntry> GetBoard(string category)
        {
            if (!_boards.TryGetValue(category, out var board))
                return new List<LeaderboardEntry>();
            return new List<LeaderboardEntry>(board);
        }

        /// <summary>Get the local rank of a player in a category (1-based; 0 if not found).</summary>
        public int GetPlayerRank(string category, string playerId)
        {
            var board = GetBoard(category);
            for (int i = 0; i < board.Count; i++)
                if (board[i].playerId == playerId) return i + 1;
            return 0;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void AddLocalEntry(string category, string playerId, string displayName,
                                    float score, PilotTier tier)
        {
            if (!_boards.TryGetValue(category, out var board))
                _boards[category] = board = new List<LeaderboardEntry>();

            // Update existing or add new
            var existing = board.Find(e => e.playerId == playerId);
            if (existing != null)
            {
                if (score > existing.score) existing.score = score;
            }
            else
            {
                board.Add(new LeaderboardEntry
                {
                    playerId    = playerId,
                    displayName = displayName,
                    score       = score,
                    tier        = tier
                });
            }

            // Sort descending and re-assign ranks
            board.Sort((a, b) => b.score.CompareTo(a.score));
            for (int i = 0; i < board.Count; i++) board[i].rank = i + 1;
        }
    }
}
