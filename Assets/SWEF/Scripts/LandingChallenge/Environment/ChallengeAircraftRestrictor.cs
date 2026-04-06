// ChallengeAircraftRestrictor.cs — Phase 120: Precision Landing Challenge System
// Aircraft restrictions: specific aircraft for challenges, handicap system.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Enforces aircraft eligibility rules for landing challenges.
    /// Allows or restricts specific aircraft, and applies a handicap score
    /// multiplier based on aircraft performance category.
    /// </summary>
    public class ChallengeAircraftRestrictor : MonoBehaviour
    {
        // ── Aircraft Category ─────────────────────────────────────────────────

        public enum AircraftCategory { Ultralight, GA_Single, GA_Twin, Turboprop, Regional, Narrowbody, Widebody, Fighter, Carrier }

        // ── Restriction Rule ──────────────────────────────────────────────────

        [System.Serializable]
        public class RestrictionRule
        {
            /// <summary>Challenge ID this rule applies to.</summary>
            public string ChallengeId;

            /// <summary>Allowed aircraft types (empty = all allowed).</summary>
            public List<string> AllowedAircraftTypes = new List<string>();

            /// <summary>Allowed aircraft categories (empty = all allowed).</summary>
            public List<AircraftCategory> AllowedCategories = new List<AircraftCategory>();

            /// <summary>Handicap multiplier applied per category.</summary>
            public float HandicapMultiplier = 1f;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [SerializeField] private List<RestrictionRule> rules = new List<RestrictionRule>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if <paramref name="aircraftType"/> is allowed for the challenge.
        /// </summary>
        public bool IsAllowed(string challengeId, string aircraftType, AircraftCategory category)
        {
            var rule = rules.Find(r => r.ChallengeId == challengeId);
            if (rule == null) return true; // no restriction

            bool typeOK     = rule.AllowedAircraftTypes.Count == 0 || rule.AllowedAircraftTypes.Contains(aircraftType);
            bool categoryOK = rule.AllowedCategories.Count == 0   || rule.AllowedCategories.Contains(category);
            return typeOK && categoryOK;
        }

        /// <summary>
        /// Returns the handicap score multiplier for the aircraft in this challenge.
        /// Heavier/faster aircraft receive a higher multiplier to normalise scoring.
        /// </summary>
        public float GetHandicapMultiplier(string challengeId, AircraftCategory category)
        {
            var rule = rules.Find(r => r.ChallengeId == challengeId);
            if (rule != null) return rule.HandicapMultiplier;

            // Default category handicaps
            switch (category)
            {
                case AircraftCategory.Ultralight:  return 0.9f;
                case AircraftCategory.GA_Single:   return 1.0f;
                case AircraftCategory.GA_Twin:     return 1.05f;
                case AircraftCategory.Turboprop:   return 1.1f;
                case AircraftCategory.Regional:    return 1.15f;
                case AircraftCategory.Narrowbody:  return 1.2f;
                case AircraftCategory.Widebody:    return 1.3f;
                case AircraftCategory.Fighter:     return 1.4f;
                case AircraftCategory.Carrier:     return 1.5f;
                default:                           return 1.0f;
            }
        }

        /// <summary>Add or update a restriction rule at runtime.</summary>
        public void SetRule(RestrictionRule rule)
        {
            int idx = rules.FindIndex(r => r.ChallengeId == rule.ChallengeId);
            if (idx >= 0) rules[idx] = rule;
            else          rules.Add(rule);
        }
    }
}
