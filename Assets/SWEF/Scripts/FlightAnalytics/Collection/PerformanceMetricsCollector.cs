// PerformanceMetricsCollector.cs — Phase 116: Flight Analytics Dashboard
// Performance scoring: landing quality, fuel efficiency, navigation accuracy, smoothness.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Computes normalised performance scores (0–100) for each flight
    /// session across four dimensions: landing quality, fuel efficiency, navigation
    /// accuracy, and flight smoothness.
    /// </summary>
    public class PerformanceMetricsCollector : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [SerializeField] private FlightAnalyticsConfig config;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Compute a composite performance score for a completed session.
        /// </summary>
        public float ComputeOverallScore(FlightSessionRecord session)
        {
            if (session == null) return 0f;

            float landingW    = config != null ? config.landingScoreWeight    : 0.35f;
            float fuelW       = config != null ? config.fuelScoreWeight       : 0.25f;
            float navW        = config != null ? config.navigationScoreWeight : 0.25f;
            float smoothW     = config != null ? config.smoothnessScoreWeight : 0.15f;

            float landing     = session.landingScore >= 0f ? session.landingScore : 70f;
            float fuel        = ComputeFuelScore(session);
            float navigation  = ComputeNavigationScore(session);
            float smoothness  = ComputeSmoothnessScore(session);

            return Mathf.Clamp01(
                landing    * landingW +
                fuel       * fuelW   +
                navigation * navW    +
                smoothness * smoothW) * 100f;
        }

        /// <summary>
        /// Score fuel efficiency based on fuel remaining at end of session.
        /// More fuel remaining relative to flight distance = higher score.
        /// </summary>
        public float ComputeFuelScore(FlightSessionRecord session)
        {
            if (session == null || session.dataPoints == null || session.dataPoints.Count == 0)
                return 70f;

            int last = session.dataPoints.Count - 1;
            float fuelRemaining = session.dataPoints[last].fuelNormalised;
            // Simple heuristic: reward not running out of fuel
            return Mathf.Clamp01(fuelRemaining * 0.5f + 0.5f) * 100f;
        }

        /// <summary>
        /// Score navigation accuracy. Stub — real implementation queries route data.
        /// </summary>
        public float ComputeNavigationScore(FlightSessionRecord session) => 75f;

        /// <summary>
        /// Score flight smoothness by analysing G-force variance.
        /// Lower variance = smoother flight = higher score.
        /// </summary>
        public float ComputeSmoothnessScore(FlightSessionRecord session)
        {
            if (session == null || session.dataPoints == null || session.dataPoints.Count < 2)
                return 70f;

            float sumSq = 0f;
            float mean  = 0f;
            foreach (var p in session.dataPoints) mean += p.gForce;
            mean /= session.dataPoints.Count;
            foreach (var p in session.dataPoints) sumSq += (p.gForce - mean) * (p.gForce - mean);
            float variance = sumSq / session.dataPoints.Count;

            // Map variance 0→0 (perfect) to 4→100 (rough)
            float roughness = Mathf.Clamp01(variance / 4f);
            return (1f - roughness) * 100f;
        }
    }
}
