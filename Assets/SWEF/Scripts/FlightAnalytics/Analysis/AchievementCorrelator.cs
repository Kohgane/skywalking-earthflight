// AchievementCorrelator.cs — Phase 116: Flight Analytics Dashboard
// Achievement analytics: progress, estimated completion, unlock rate.
// Namespace: SWEF.FlightAnalytics

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Analyses flight history to compute achievement progress,
    /// estimate completion dates, and measure unlock rate over time.
    /// </summary>
    public class AchievementCorrelator : MonoBehaviour
    {
        // ── Nested types ──────────────────────────────────────────────────────────

        /// <summary>Analytics data for a single achievement.</summary>
        [Serializable]
        public class AchievementProgress
        {
            /// <summary>Achievement identifier.</summary>
            public string achievementId;
            /// <summary>Current progress value.</summary>
            public float current;
            /// <summary>Target value required to unlock.</summary>
            public float target;
            /// <summary>Normalised completion ratio (0–1).</summary>
            public float ratio;
            /// <summary>Estimated number of flights to completion (−1 = unknown).</summary>
            public int estimatedFlightsRemaining;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Compute progress toward a flight-count-based achievement.
        /// </summary>
        public AchievementProgress FlightCountProgress(string achievementId, int current, int target)
        {
            return new AchievementProgress
            {
                achievementId             = achievementId,
                current                   = current,
                target                    = target,
                ratio                     = target > 0 ? Mathf.Clamp01((float)current / target) : 1f,
                estimatedFlightsRemaining = Mathf.Max(0, target - current)
            };
        }

        /// <summary>
        /// Compute progress toward a cumulative distance achievement (nautical miles).
        /// </summary>
        public AchievementProgress DistanceProgress(string achievementId, IList<FlightSessionRecord> sessions, float targetNm)
        {
            float total = 0f;
            if (sessions != null)
                foreach (var s in sessions) total += s.distanceNm;

            return new AchievementProgress
            {
                achievementId             = achievementId,
                current                   = total,
                target                    = targetNm,
                ratio                     = targetNm > 0 ? Mathf.Clamp01(total / targetNm) : 1f,
                estimatedFlightsRemaining = -1  // requires per-session distance average
            };
        }

        /// <summary>
        /// Compute the total achievement unlock rate: unlocks per flight.
        /// </summary>
        public float UnlockRate(int totalUnlocked, int totalFlights)
        {
            return totalFlights > 0 ? (float)totalUnlocked / totalFlights : 0f;
        }
    }
}
