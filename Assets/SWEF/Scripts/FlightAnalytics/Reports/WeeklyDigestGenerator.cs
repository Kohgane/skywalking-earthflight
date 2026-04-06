// WeeklyDigestGenerator.cs — Phase 116: Flight Analytics Dashboard
// Weekly/monthly digest: flying hours, distance, achievements, personal records.
// Namespace: SWEF.FlightAnalytics

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Compiles a weekly or monthly digest report from a collection of
    /// <see cref="FlightSessionRecord"/> objects.
    /// </summary>
    public class WeeklyDigestGenerator : MonoBehaviour
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Generate a digest report covering the given list of sessions.</summary>
        /// <param name="sessions">Sessions included in the digest (pre-filtered by caller).</param>
        /// <param name="periodLabel">Human-readable label, e.g. "Week of Apr 1, 2026".</param>
        public FlightReport GenerateDigest(IList<FlightSessionRecord> sessions, string periodLabel)
        {
            var report = new FlightReport
            {
                reportId       = Guid.NewGuid().ToString("N"),
                title          = $"Digest: {periodLabel}",
                generatedAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            if (sessions == null || sessions.Count == 0)
            {
                report.summary = "No flights recorded in this period.";
                return report;
            }

            float totalHours = 0f, totalDist = 0f, bestScore = 0f;
            var airports = new HashSet<string>();

            foreach (var s in sessions)
            {
                totalHours += s.durationSeconds / 3600f;
                totalDist  += s.distanceNm;
                if (s.performanceScore > bestScore) bestScore = s.performanceScore;
                foreach (string a in s.airportsVisited) airports.Add(a);
            }

            float avgScore = 0f;
            foreach (var s in sessions) avgScore += s.performanceScore;
            avgScore /= sessions.Count;

            report.summary = $"You flew {totalHours:F1} hours over {totalDist:F0} nm " +
                             $"across {sessions.Count} flights this period.";

            report.metrics["totalFlights"]    = sessions.Count;
            report.metrics["totalHours"]      = totalHours;
            report.metrics["totalDistanceNm"] = totalDist;
            report.metrics["avgScore"]        = avgScore;
            report.metrics["bestScore"]       = bestScore;
            report.metrics["uniqueAirports"]  = airports.Count;

            if (bestScore >= 90f)
                report.highlights.Add($"🏆 Personal best score this period: {bestScore:F0}/100!");
            if (totalHours >= 5f)
                report.highlights.Add($"⏱️ Great consistency — {totalHours:F1} hours in the air!");
            if (airports.Count >= 5)
                report.highlights.Add($"🗺️ Explored {airports.Count} unique airports.");

            return report;
        }
    }
}
