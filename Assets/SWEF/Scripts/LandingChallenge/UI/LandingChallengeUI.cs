// LandingChallengeUI.cs — Phase 120: Precision Landing Challenge System
// Challenge browser: grid of challenges, difficulty filter, progress indicators.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

#if UNITY_UGUI || UNITY_2019_4_OR_NEWER
using UnityEngine.UI;
#endif

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Challenge browser UI.
    /// Displays a grid of landing challenges with difficulty filters,
    /// lock/unlock status, star progress indicators, and start buttons.
    /// </summary>
    public class LandingChallengeUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Filter")]
        [SerializeField] private DifficultyLevel filterDifficulty = DifficultyLevel.Beginner;
        [SerializeField] private bool            showLockedChallenges = true;

        // ── State ─────────────────────────────────────────────────────────────

        private LandingChallengeManager _manager;
        private List<ChallengeDefinition> _filtered = new List<ChallengeDefinition>();

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when the player selects a challenge to start.</summary>
        public event System.Action<string> OnChallengeSelected;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current difficulty filter applied to the grid.</summary>
        public DifficultyLevel FilterDifficulty
        {
            get => filterDifficulty;
            set { filterDifficulty = value; RefreshGrid(); }
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            _manager = LandingChallengeManager.Instance;
            RefreshGrid();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Refresh the challenge grid with current filter settings.</summary>
        public void RefreshGrid()
        {
            _filtered.Clear();
            if (_manager == null) return;

            foreach (var def in _manager.AllChallenges)
            {
                bool unlocked = _manager.IsChallengeUnlocked(def.ChallengeId);
                if (!showLockedChallenges && !unlocked) continue;
                _filtered.Add(def);
            }
        }

        /// <summary>Returns the filtered list of challenges for the current view.</summary>
        public IReadOnlyList<ChallengeDefinition> GetFilteredChallenges() => _filtered;

        /// <summary>Called when the player taps/clicks a challenge tile.</summary>
        public void SelectChallenge(string challengeId)
        {
            if (_manager != null && _manager.IsChallengeUnlocked(challengeId))
                OnChallengeSelected?.Invoke(challengeId);
        }

        /// <summary>Returns star count for display on a challenge tile.</summary>
        public int GetStarsForChallenge(string challengeId)
        {
            var progress = _manager?.GetProgress(challengeId);
            return progress?.StarsEarned ?? 0;
        }
    }
}
