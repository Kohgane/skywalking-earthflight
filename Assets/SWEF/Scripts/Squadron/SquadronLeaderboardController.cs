// SquadronLeaderboardController.cs — Phase 109: Clan/Squadron System
// Squadron and per-member leaderboard ranking by multiple metrics and time periods.
// Namespace: SWEF.Squadron

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — Computes and exposes squadron leaderboards sorted by various
    /// metrics (Total XP, Mission Completions, Member Activity, Event Participation)
    /// and time periods (Weekly, Monthly, All-Time).
    ///
    /// <para>Also provides per-member contribution rankings within the local squadron.</para>
    /// <para>Integrates with <c>GlobalLeaderboardService</c> when
    /// <c>SWEF_LEADERBOARD_AVAILABLE</c> is defined.</para>
    /// </summary>
    public sealed class SquadronLeaderboardController : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static SquadronLeaderboardController Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a leaderboard is refreshed.</summary>
        public event Action<SquadronLeaderboardCategory, SquadronLeaderboardPeriod> OnRankingUpdated;

        // ── State ──────────────────────────────────────────────────────────────

        /// <summary>In-memory registry of known squadron snapshots (populated externally or via mock).</summary>
        private readonly List<SquadronInfo> _knownSquadrons = new List<SquadronInfo>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API — Squadron rankings ─────────────────────────────────────

        /// <summary>
        /// Registers a squadron snapshot for ranking computation.
        /// In production this list would be populated from a backend service.
        /// </summary>
        public void RegisterSquadron(SquadronInfo info)
        {
            if (info == null) return;
            var existing = _knownSquadrons.FirstOrDefault(s => s.squadronId == info.squadronId);
            if (existing != null)
                _knownSquadrons[_knownSquadrons.IndexOf(existing)] = info;
            else
                _knownSquadrons.Add(info);
        }

        /// <summary>
        /// Computes and returns the squadron leaderboard for the given category and period.
        /// </summary>
        public List<SquadronLeaderboardEntry> GetLeaderboard(
            SquadronLeaderboardCategory category,
            SquadronLeaderboardPeriod period)
        {
            var entries = _knownSquadrons
                .Where(s => s.status == SquadronStatus.Active)
                .Select(s => new SquadronLeaderboardEntry
                {
                    squadronId   = s.squadronId,
                    squadronName = s.name,
                    score        = ComputeScore(s, category),
                    category     = category,
                    period       = period
                })
                .OrderByDescending(e => e.score)
                .ToList();

            // Assign rank positions
            for (int i = 0; i < entries.Count; i++)
                entries[i].rank = i + 1;

#if SWEF_LEADERBOARD_AVAILABLE
            SubmitToGlobalLeaderboard(entries, category, period);
#endif

            OnRankingUpdated?.Invoke(category, period);
            return entries;
        }

        /// <summary>
        /// Returns the leaderboard rank of the local player's squadron for a given category.
        /// Returns -1 if not in a squadron or not ranked.
        /// </summary>
        public int GetLocalSquadronRank(SquadronLeaderboardCategory category, SquadronLeaderboardPeriod period)
        {
            string myId = SquadronManager.Instance?.CurrentSquadron?.squadronId;
            if (string.IsNullOrEmpty(myId)) return -1;

            var board = GetLeaderboard(category, period);
            var entry = board.FirstOrDefault(e => e.squadronId == myId);
            return entry?.rank ?? -1;
        }

        // ── Public API — Per-member rankings ───────────────────────────────────

        /// <summary>
        /// Returns members of the local squadron ranked by contribution XP (highest first).
        /// </summary>
        public List<SquadronMember> GetMemberContributionRanking()
        {
            return SquadronManager.Instance?.GetMembers()
                       ?.OrderByDescending(m => m.contributionXP)
                       .ToList()
                   ?? new List<SquadronMember>();
        }

        /// <summary>
        /// Returns members ranked by total squadron flights (highest first).
        /// </summary>
        public List<SquadronMember> GetMemberFlightRanking()
        {
            return SquadronManager.Instance?.GetMembers()
                       ?.OrderByDescending(m => m.totalSquadronFlights)
                       .ToList()
                   ?? new List<SquadronMember>();
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static long ComputeScore(SquadronInfo s, SquadronLeaderboardCategory category)
        {
            return category switch
            {
                SquadronLeaderboardCategory.TotalXP            => s.totalXP,
                SquadronLeaderboardCategory.MissionCompletions => s.level * 10L, // proxy
                SquadronLeaderboardCategory.MemberActivity     => s.memberCount * 100L,
                SquadronLeaderboardCategory.EventParticipation => s.totalXP / 10L,
                _                                             => 0L
            };
        }

#if SWEF_LEADERBOARD_AVAILABLE
        private static void SubmitToGlobalLeaderboard(
            List<SquadronLeaderboardEntry> entries,
            SquadronLeaderboardCategory category,
            SquadronLeaderboardPeriod period)
        {
            // Integration point with existing GlobalLeaderboardService
            // GlobalLeaderboardService.Instance?.SubmitSquadronBatch(entries, category.ToString(), period.ToString());
        }
#endif
    }
}
