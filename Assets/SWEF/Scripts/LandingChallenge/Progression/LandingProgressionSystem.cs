// LandingProgressionSystem.cs — Phase 120: Precision Landing Challenge System
// Progression: challenge tiers unlock sequentially, star system (1-3 stars), mastery badges.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages the landing challenge progression system.
    /// Tracks tier completions, star totals, mastery badge awards, and
    /// sequential tier unlocking.
    /// </summary>
    public class LandingProgressionSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Tier Configuration")]
        [SerializeField] private int starsRequiredPerTier = 3;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly Dictionary<string, int>        _challengeStars = new Dictionary<string, int>();
        private readonly HashSet<string>                _masteryBadges  = new HashSet<string>();
        private int                                     _totalStars;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when a mastery badge is earned.</summary>
        public event System.Action<string> OnMasteryBadgeEarned;

        /// <summary>Raised when a new tier is unlocked.</summary>
        public event System.Action<int> OnTierUnlocked;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total stars earned across all challenges.</summary>
        public int TotalStars => _totalStars;

        /// <summary>Currently unlocked tier (0-based).</summary>
        public int UnlockedTier => _totalStars / Mathf.Max(1, starsRequiredPerTier);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Record stars earned for a challenge.
        /// Only updates if the new star count is higher than the previous best.
        /// </summary>
        public void RecordStars(string challengeId, int stars)
        {
            if (!_challengeStars.TryGetValue(challengeId, out int previous) || stars > previous)
            {
                int diff = stars - (previous > 0 ? previous : 0);
                _challengeStars[challengeId] = stars;
                int prevTier = UnlockedTier;
                _totalStars += diff;
                int newTier  = UnlockedTier;

                if (newTier > prevTier)
                    OnTierUnlocked?.Invoke(newTier);

                CheckMasteryBadge(challengeId, stars);
            }
        }

        /// <summary>Returns stars earned for a specific challenge (0–3).</summary>
        public int GetStars(string challengeId)
        {
            _challengeStars.TryGetValue(challengeId, out int s);
            return s;
        }

        /// <summary>Returns <c>true</c> if the player has earned the mastery badge for a challenge.</summary>
        public bool HasMasteryBadge(string challengeId) => _masteryBadges.Contains(challengeId);

        /// <summary>Returns all earned mastery badge IDs.</summary>
        public IReadOnlyCollection<string> AllMasteryBadges => _masteryBadges;

        // ── Private ───────────────────────────────────────────────────────────

        private void CheckMasteryBadge(string challengeId, int stars)
        {
            if (stars == 3 && !_masteryBadges.Contains(challengeId))
            {
                _masteryBadges.Add(challengeId);
                OnMasteryBadgeEarned?.Invoke(challengeId);
            }
        }
    }
}
