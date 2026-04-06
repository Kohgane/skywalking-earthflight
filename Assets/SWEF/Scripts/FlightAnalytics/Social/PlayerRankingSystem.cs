// PlayerRankingSystem.cs — Phase 116: Flight Analytics Dashboard
// Pilot ranking: ELO-style rating, skill tiers, seasonal rankings.
// Namespace: SWEF.FlightAnalytics

using System;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Maintains an ELO-style pilot rating and maps it to a
    /// <see cref="PilotTier"/> (Student → Ace).
    /// </summary>
    public class PlayerRankingSystem : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        private const float InitialRating = 1000f;
        private const float KFactor       = 32f;   // ELO K-factor

        // ── Tier thresholds ───────────────────────────────────────────────────────

        // Rating → Tier mapping
        private static readonly (float min, PilotTier tier)[] TierThresholds =
        {
            (0f,    PilotTier.Student),
            (1100f, PilotTier.Private),
            (1300f, PilotTier.Commercial),
            (1500f, PilotTier.Captain),
            (1800f, PilotTier.Ace)
        };

        // ── State ─────────────────────────────────────────────────────────────────

        private float _rating = InitialRating;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>Current ELO-style rating.</summary>
        public float Rating => _rating;

        /// <summary>Derived skill tier from the current rating.</summary>
        public PilotTier Tier => ResolveTier(_rating);

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Initialise from a saved rating value.</summary>
        public void LoadRating(float savedRating)
        {
            _rating = Mathf.Max(0f, savedRating);
        }

        /// <summary>
        /// Update the rating after a flight using ELO logic.
        /// <paramref name="actualScore"/> is the normalised performance (0–1).
        /// <paramref name="expectedScore"/> is the difficulty-adjusted expected performance (0–1).
        /// </summary>
        public void UpdateRating(float actualScore, float expectedScore)
        {
            float delta = KFactor * (actualScore - expectedScore);
            _rating = Mathf.Max(0f, _rating + delta);
            Debug.Log($"[SWEF] PlayerRankingSystem: Rating updated to {_rating:F0} ({Tier}).");
        }

        /// <summary>
        /// Expected performance score against a virtual opponent with <paramref name="opponentRating"/>.
        /// Uses the standard ELO expected-score formula.
        /// </summary>
        public float ExpectedScore(float opponentRating)
        {
            return 1f / (1f + Mathf.Pow(10f, (opponentRating - _rating) / 400f));
        }

        /// <summary>Points required to advance to the next tier (0 if already at Ace).</summary>
        public float PointsToNextTier()
        {
            for (int i = TierThresholds.Length - 1; i >= 0; i--)
            {
                if (_rating >= TierThresholds[i].min && i < TierThresholds.Length - 1)
                    return TierThresholds[i + 1].min - _rating;
            }
            return 0f;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static PilotTier ResolveTier(float rating)
        {
            PilotTier tier = PilotTier.Student;
            foreach (var (min, t) in TierThresholds)
                if (rating >= min) tier = t;
            return tier;
        }
    }
}
