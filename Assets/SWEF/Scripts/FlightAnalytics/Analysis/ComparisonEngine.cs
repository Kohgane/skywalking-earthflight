// ComparisonEngine.cs — Phase 116: Flight Analytics Dashboard
// Flight comparison: side-by-side analysis, personal best vs average.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Provides side-by-side comparison of flight sessions and
    /// comparison against community/personal benchmarks.
    /// </summary>
    public class ComparisonEngine : MonoBehaviour
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Compare two sessions and return a dictionary of metric deltas
        /// (positive = session A is better).
        /// </summary>
        public Dictionary<string, float> Compare(FlightSessionRecord sessionA, FlightSessionRecord sessionB)
        {
            var delta = new Dictionary<string, float>();
            if (sessionA == null || sessionB == null) return delta;

            delta["performanceScore"]   = sessionA.performanceScore   - sessionB.performanceScore;
            delta["landingScore"]       = sessionA.landingScore       - sessionB.landingScore;
            delta["fuelEfficiency"]     = sessionA.fuelEfficiencyScore - sessionB.fuelEfficiencyScore;
            delta["durationSeconds"]    = sessionA.durationSeconds    - sessionB.durationSeconds;
            delta["distanceNm"]         = sessionA.distanceNm         - sessionB.distanceNm;

            return delta;
        }

        /// <summary>
        /// Find the personal-best session for a specific metric from a list of sessions.
        /// </summary>
        public FlightSessionRecord PersonalBest(IList<FlightSessionRecord> sessions, string metric)
        {
            if (sessions == null || sessions.Count == 0) return null;

            return metric switch
            {
                "performanceScore"   => sessions.OrderByDescending(s => s.performanceScore).First(),
                "landingScore"       => sessions.OrderByDescending(s => s.landingScore).First(),
                "fuelEfficiency"     => sessions.OrderByDescending(s => s.fuelEfficiencyScore).First(),
                "distanceNm"         => sessions.OrderByDescending(s => s.distanceNm).First(),
                "durationSeconds"    => sessions.OrderByDescending(s => s.durationSeconds).First(),
                _                    => sessions[0]
            };
        }

        /// <summary>
        /// Compute the average value of a metric across a list of sessions.
        /// </summary>
        public float AverageMetric(IList<FlightSessionRecord> sessions, string metric)
        {
            if (sessions == null || sessions.Count == 0) return 0f;
            return metric switch
            {
                "performanceScore"   => sessions.Average(s => s.performanceScore),
                "landingScore"       => sessions.Average(s => s.landingScore),
                "fuelEfficiency"     => sessions.Average(s => s.fuelEfficiencyScore),
                "distanceNm"         => sessions.Average(s => s.distanceNm),
                "durationSeconds"    => sessions.Average(s => s.durationSeconds),
                _                    => 0f
            };
        }
    }
}
