// TrendAnalyzer.cs — Phase 116: Flight Analytics Dashboard
// Trend detection: improving/declining performance, skill progression curves.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Analyses time-ordered series of scores to detect whether a pilot's
    /// performance is improving, declining, or stable, and estimates future milestones.
    /// </summary>
    public class TrendAnalyzer : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        /// <summary>Minimum slope magnitude to be classified as improving/declining.</summary>
        private const float MinSlopeThreshold = 0.05f;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Determine the trend direction of the given ordered score series.
        /// Uses least-squares linear regression on the series.
        /// </summary>
        public TrendDirection DetectTrend(IList<float> scores)
        {
            if (scores == null || scores.Count < 2) return TrendDirection.Stable;

            float slope = LinearRegressionSlope(scores);
            if (slope > MinSlopeThreshold)  return TrendDirection.Improving;
            if (slope < -MinSlopeThreshold) return TrendDirection.Declining;
            return TrendDirection.Stable;
        }

        /// <summary>
        /// Estimate how many more flights are required to reach <paramref name="targetScore"/>
        /// given the current rate of improvement.  Returns −1 if the target cannot be reached
        /// based on the current trend.
        /// </summary>
        public int EstimateFlightsToTarget(IList<float> scores, float targetScore)
        {
            if (scores == null || scores.Count < 2) return -1;

            float slope = LinearRegressionSlope(scores);
            if (slope <= 0f) return -1;

            float lastScore = scores[scores.Count - 1];
            float remaining = targetScore - lastScore;
            if (remaining <= 0f) return 0;

            return Mathf.CeilToInt(remaining / slope);
        }

        /// <summary>
        /// Compute a smoothed progression curve (moving average) suitable for chart rendering.
        /// </summary>
        public List<float> ProgressionCurve(IList<float> scores, int windowSize = 5)
        {
            var result = new List<float>(scores.Count);
            if (scores.Count == 0) return result;

            for (int i = 0; i < scores.Count; i++)
            {
                int start = Mathf.Max(0, i - windowSize + 1);
                float sum = 0f;
                for (int j = start; j <= i; j++) sum += scores[j];
                result.Add(sum / (i - start + 1));
            }
            return result;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static float LinearRegressionSlope(IList<float> y)
        {
            int n = y.Count;
            float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f;
            for (int i = 0; i < n; i++)
            {
                sumX  += i;
                sumY  += y[i];
                sumXY += i * y[i];
                sumX2 += i * i;
            }
            float denom = n * sumX2 - sumX * sumX;
            return Mathf.Approximately(denom, 0f) ? 0f : (n * sumXY - sumX * sumY) / denom;
        }
    }
}
