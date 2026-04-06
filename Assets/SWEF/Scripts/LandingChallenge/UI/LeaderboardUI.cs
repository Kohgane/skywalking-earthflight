// LeaderboardUI.cs — Phase 120: Precision Landing Challenge System
// Leaderboard display: global/friends tabs, score details, replay watch button, share button.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Leaderboard display controller.
    /// Presents global and friends leaderboard tabs with score details,
    /// replay watch links, and share functionality.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        // ── Tab Enum ──────────────────────────────────────────────────────────

        public enum LeaderboardTab { Global, Friends, Personal }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Display")]
        [SerializeField] private int    entriesPerPage = 20;
        [SerializeField] private string activeChallengeId;

        // ── State ─────────────────────────────────────────────────────────────

        private LeaderboardTab _activeTab = LeaderboardTab.Global;
        private int            _currentPage;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when the player requests to watch a ghost replay.</summary>
        public event System.Action<string> OnWatchReplay;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Currently displayed tab.</summary>
        public LeaderboardTab ActiveTab => _activeTab;

        /// <summary>Currently displayed page index (0-based).</summary>
        public int CurrentPage => _currentPage;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Switch to a different leaderboard tab.</summary>
        public void SwitchTab(LeaderboardTab tab)
        {
            _activeTab   = tab;
            _currentPage = 0;
            RefreshDisplay();
        }

        /// <summary>Navigate to the next page of entries.</summary>
        public void NextPage() { _currentPage++; RefreshDisplay(); }

        /// <summary>Navigate to the previous page of entries.</summary>
        public void PreviousPage() { if (_currentPage > 0) { _currentPage--; RefreshDisplay(); } }

        /// <summary>Set the challenge ID whose leaderboard is displayed.</summary>
        public void SetChallenge(string challengeId)
        {
            activeChallengeId = challengeId;
            _currentPage      = 0;
            RefreshDisplay();
        }

        /// <summary>Returns leaderboard entries for the current view.</summary>
        public IReadOnlyList<LeaderboardEntry> GetCurrentEntries()
        {
            var manager = LandingLeaderboardManager.Instance;
            if (manager == null) return System.Array.Empty<LeaderboardEntry>();

            int startRank = _currentPage * entriesPerPage + 1;
            var board     = manager.GetChallengeBoard(activeChallengeId, (_currentPage + 1) * entriesPerPage);
            var page      = new List<LeaderboardEntry>();
            for (int i = (_currentPage * entriesPerPage); i < Mathf.Min(board.Count, (_currentPage + 1) * entriesPerPage); i++)
                page.Add(board[i]);
            return page;
        }

        /// <summary>Request to watch the ghost replay for an entry.</summary>
        public void WatchReplay(LeaderboardEntry entry)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.ReplayId))
                OnWatchReplay?.Invoke(entry.ReplayId);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void RefreshDisplay()
        {
            // In a real implementation this would rebuild the UGUI scroll list
        }
    }
}
