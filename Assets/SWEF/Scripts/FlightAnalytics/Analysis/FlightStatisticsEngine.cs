// FlightStatisticsEngine.cs — Phase 116: Flight Analytics Dashboard
// Statistical computations: averages, trends, percentiles, standard deviation.
// Namespace: SWEF.FlightAnalytics

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Provides statistical analysis utilities for collections of
    /// <see cref="FlightSessionRecord"/> objects. Computes aggregates, moving averages,
    /// percentiles, and standard deviations.
    /// </summary>
    public class FlightStatisticsEngine : MonoBehaviour
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Build an <see cref="AggregatedStats"/> from a list of sessions.</summary>
        public AggregatedStats Aggregate(IList<FlightSessionRecord> sessions)
        {
            var stats = new AggregatedStats();
            if (sessions == null || sessions.Count == 0) return stats;

            stats.flightCount = sessions.Count;

            foreach (var s in sessions)
            {
                stats.totalHours      += s.durationSeconds / 3600f;
                stats.totalDistanceNm += s.distanceNm;
                stats.avgPerformanceScore += s.performanceScore;
                if (s.landingScore >= 0f) stats.avgLandingScore += s.landingScore;
                stats.avgFuelEfficiency   += s.fuelEfficiencyScore;

                if (s.performanceScore > stats.bestPerformanceScore)
                    stats.bestPerformanceScore = s.performanceScore;
                if (s.landingScore > stats.bestLandingScore)
                    stats.bestLandingScore = s.landingScore;

                foreach (string airport in s.airportsVisited)
                    stats.uniqueAirports++; // simple count; deduplication done elsewhere
            }

            stats.avgPerformanceScore /= sessions.Count;
            stats.avgLandingScore     /= sessions.Count;
            stats.avgFuelEfficiency   /= sessions.Count;

            return stats;
        }

        /// <summary>Apply the requested <see cref="StatAggregation"/> to a float list.</summary>
        public float Aggregate(IList<float> values, StatAggregation method)
        {
            if (values == null || values.Count == 0) return 0f;

            return method switch
            {
                StatAggregation.Average => values.Sum() / values.Count,
                StatAggregation.Sum     => values.Sum(),
                StatAggregation.Min     => values.Min(),
                StatAggregation.Max     => values.Max(),
                StatAggregation.Median  => Median(values),
                _                       => 0f
            };
        }

        /// <summary>Compute a simple moving average over <paramref name="windowSize"/> elements.</summary>
        public List<float> MovingAverage(IList<float> values, int windowSize)
        {
            var result = new List<float>(values.Count);
            if (values.Count == 0 || windowSize < 1) return result;

            for (int i = 0; i < values.Count; i++)
            {
                int start = Mathf.Max(0, i - windowSize + 1);
                float sum = 0f;
                for (int j = start; j <= i; j++) sum += values[j];
                result.Add(sum / (i - start + 1));
            }
            return result;
        }

        /// <summary>Compute the standard deviation of a list of values.</summary>
        public float StandardDeviation(IList<float> values)
        {
            if (values == null || values.Count < 2) return 0f;
            float mean = values.Sum() / values.Count;
            float sumSq = values.Sum(v => (v - mean) * (v - mean));
            return Mathf.Sqrt(sumSq / values.Count);
        }

        /// <summary>Compute the Nth percentile (0–100) of a list of values.</summary>
        public float Percentile(IList<float> values, float percentile)
        {
            if (values == null || values.Count == 0) return 0f;
            var sorted = new List<float>(values);
            sorted.Sort();
            float index = (percentile / 100f) * (sorted.Count - 1);
            int lo = Mathf.FloorToInt(index);
            int hi = Mathf.CeilToInt(index);
            if (lo == hi) return sorted[lo];
            return Mathf.Lerp(sorted[lo], sorted[hi], index - lo);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static float Median(IList<float> values)
        {
            var sorted = new List<float>(values);
            sorted.Sort();
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) * 0.5f
                : sorted[mid];
        }
    }
}
