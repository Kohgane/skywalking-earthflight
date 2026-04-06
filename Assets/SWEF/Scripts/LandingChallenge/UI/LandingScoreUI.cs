// LandingScoreUI.cs — Phase 120: Precision Landing Challenge System
// Post-landing score screen: animated score breakdown, grade display, star award, personal best comparison.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Post-landing score screen controller.
    /// Presents an animated breakdown of scoring categories, grade display,
    /// star award animation, and comparison with the personal best.
    /// </summary>
    public class LandingScoreUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Animation")]
        [SerializeField] private float scoreRevealDuration = 2f;
        [SerializeField] private float starAnimationDelay  = 0.5f;

        // ── State ─────────────────────────────────────────────────────────────

        private LandingResult _currentResult;
        private float         _animTimer;
        private bool          _isAnimating;
        private float         _displayedScore;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when the score reveal animation completes.</summary>
        public event System.Action OnScoreRevealed;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current displayed score (animated).</summary>
        public float DisplayedScore => _displayedScore;

        /// <summary>Whether the score animation is running.</summary>
        public bool IsAnimating => _isAnimating;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Show the score screen for a completed landing result.</summary>
        public void ShowResult(LandingResult result, LandingResult personalBest)
        {
            _currentResult  = result;
            _animTimer      = 0f;
            _isAnimating    = true;
            _displayedScore = 0f;
            gameObject.SetActive(true);
        }

        /// <summary>Returns a formatted score summary string for each category.</summary>
        public Dictionary<string, float> GetCategoryBreakdown()
        {
            var breakdown = new Dictionary<string, float>();
            if (_currentResult == null) return breakdown;
            foreach (var kvp in _currentResult.CategoryScores)
                breakdown[kvp.Key.ToString()] = kvp.Value;
            return breakdown;
        }

        /// <summary>Returns the grade label string.</summary>
        public string GetGradeLabel() =>
            _currentResult != null ? LandingGradeCalculator.GradeLabel(_currentResult.Grade) : "--";

        /// <summary>Returns the star count for the current result.</summary>
        public int GetStars() => _currentResult?.Stars ?? 0;

        /// <summary>Hide the score screen.</summary>
        public void Hide() => gameObject.SetActive(false);

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (!_isAnimating || _currentResult == null) return;

            _animTimer      += Time.deltaTime;
            float t          = Mathf.Clamp01(_animTimer / scoreRevealDuration);
            _displayedScore  = Mathf.Lerp(0f, _currentResult.TotalScore, t);

            if (t >= 1f)
            {
                _isAnimating    = false;
                _displayedScore = _currentResult.TotalScore;
                OnScoreRevealed?.Invoke();
            }
        }
    }
}
