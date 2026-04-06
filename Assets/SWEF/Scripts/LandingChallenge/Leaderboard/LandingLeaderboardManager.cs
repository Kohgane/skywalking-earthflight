// LandingLeaderboardManager.cs — Phase 120: Precision Landing Challenge System
// Leaderboards: per-challenge, per-airport, per-aircraft, global ranking.
// Namespace: SWEF.LandingChallenge

using System;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_MULTIPLAYER_AVAILABLE
// Remote leaderboard sync placeholder
#endif

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Singleton that manages all landing challenge leaderboards.
    /// Maintains per-challenge, per-airport, per-aircraft, and global rankings.
    /// Uses <c>#if SWEF_MULTIPLAYER_AVAILABLE</c> for remote sync.
    /// </summary>
    public class LandingLeaderboardManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        /// <summary>Shared singleton instance.</summary>
        public static LandingLeaderboardManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Leaderboard Settings")]
        [SerializeField] private int maxEntriesPerBoard = 100;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly Dictionary<string, List<LeaderboardEntry>> _challengeBoards =
            new Dictionary<string, List<LeaderboardEntry>>();
        private readonly Dictionary<string, List<LeaderboardEntry>> _airportBoards =
            new Dictionary<string, List<LeaderboardEntry>>();
        private readonly Dictionary<string, List<LeaderboardEntry>> _aircraftBoards =
            new Dictionary<string, List<LeaderboardEntry>>();
        private readonly List<LeaderboardEntry> _globalBoard = new List<LeaderboardEntry>();

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when any leaderboard is updated.</summary>
        public event Action<string> OnLeaderboardUpdated;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Submit an entry to all applicable leaderboards.</summary>
        public void SubmitEntry(LeaderboardEntry entry, string airportICAO)
        {
            if (entry == null) return;

            InsertEntry(_challengeBoards, entry.ChallengeId, entry);
            InsertEntry(_airportBoards,   airportICAO,        entry);
            InsertEntry(_aircraftBoards,  entry.AircraftType, entry);
            InsertGlobal(entry);

            OnLeaderboardUpdated?.Invoke(entry.ChallengeId);
        }

        /// <summary>Returns the top-N entries for a specific challenge.</summary>
        public IReadOnlyList<LeaderboardEntry> GetChallengeBoard(string challengeId, int top = 10)
        {
            if (!_challengeBoards.TryGetValue(challengeId, out var list)) return Array.Empty<LeaderboardEntry>();
            int count = Mathf.Min(top, list.Count);
            return list.GetRange(0, count);
        }

        /// <summary>Returns the top-N entries for a specific airport.</summary>
        public IReadOnlyList<LeaderboardEntry> GetAirportBoard(string icao, int top = 10)
        {
            if (!_airportBoards.TryGetValue(icao, out var list)) return Array.Empty<LeaderboardEntry>();
            int count = Mathf.Min(top, list.Count);
            return list.GetRange(0, count);
        }

        /// <summary>Returns the global top-N entries.</summary>
        public IReadOnlyList<LeaderboardEntry> GetGlobalBoard(int top = 10)
        {
            int count = Mathf.Min(top, _globalBoard.Count);
            return _globalBoard.GetRange(0, count);
        }

        /// <summary>Returns the rank of a player on the challenge leaderboard, or -1 if not found.</summary>
        public int GetPlayerRank(string challengeId, string playerId)
        {
            if (!_challengeBoards.TryGetValue(challengeId, out var list)) return -1;
            for (int i = 0; i < list.Count; i++)
                if (list[i].PlayerId == playerId) return i + 1;
            return -1;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void InsertEntry(Dictionary<string, List<LeaderboardEntry>> boards, string key, LeaderboardEntry entry)
        {
            if (!boards.TryGetValue(key, out var list))
            {
                list = new List<LeaderboardEntry>();
                boards[key] = list;
            }

            // Replace existing entry for this player if the new score is higher
            int existing = list.FindIndex(e => e.PlayerId == entry.PlayerId);
            if (existing >= 0)
            {
                if (entry.Score > list[existing].Score)
                    list.RemoveAt(existing);
                else
                    return;
            }

            list.Add(entry);
            list.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (list.Count > maxEntriesPerBoard)
                list.RemoveAt(list.Count - 1);

            // Assign ranks
            for (int i = 0; i < list.Count; i++)
                list[i].Rank = i + 1;
        }

        private void InsertGlobal(LeaderboardEntry entry)
        {
            int existing = _globalBoard.FindIndex(e => e.PlayerId == entry.PlayerId &&
                                                       e.ChallengeId == entry.ChallengeId);
            if (existing >= 0)
            {
                if (entry.Score > _globalBoard[existing].Score)
                    _globalBoard.RemoveAt(existing);
                else
                    return;
            }

            _globalBoard.Add(entry);
            _globalBoard.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (_globalBoard.Count > maxEntriesPerBoard)
                _globalBoard.RemoveAt(_globalBoard.Count - 1);

            for (int i = 0; i < _globalBoard.Count; i++)
                _globalBoard[i].Rank = i + 1;
        }
    }
}
