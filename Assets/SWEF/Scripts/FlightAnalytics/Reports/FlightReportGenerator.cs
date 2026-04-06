// FlightReportGenerator.cs — Phase 116: Flight Analytics Dashboard
// Auto-generated flight reports: post-flight summary, performance score, highlights.
// Namespace: SWEF.FlightAnalytics

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Generates structured <see cref="FlightReport"/> objects from
    /// completed <see cref="FlightSessionRecord"/> data, including a narrative summary,
    /// highlights, and improvement suggestions.
    /// </summary>
    public class FlightReportGenerator : MonoBehaviour
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Generate a post-flight report for a single session.</summary>
        public FlightReport GeneratePostFlightReport(FlightSessionRecord session)
        {
            if (session == null) return null;

            var report = new FlightReport
            {
                reportId       = Guid.NewGuid().ToString("N"),
                title          = $"Flight Report — {FormatDate(session.startTimeUtc)}",
                generatedAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                overallScore   = session.performanceScore,
                summary        = BuildSummary(session)
            };

            AddHighlights(report, session);
            AddImprovements(report, session);

            report.metrics["durationMinutes"]    = session.durationSeconds / 60f;
            report.metrics["distanceNm"]         = session.distanceNm;
            report.metrics["performanceScore"]   = session.performanceScore;
            report.metrics["landingScore"]       = session.landingScore;
            report.metrics["fuelEfficiency"]     = session.fuelEfficiencyScore;
            report.metrics["airportsVisited"]    = session.airportsVisited.Count;

            return report;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string BuildSummary(FlightSessionRecord s)
        {
            string dep = string.IsNullOrEmpty(s.departureAirport) ? "unknown" : s.departureAirport;
            string arr = string.IsNullOrEmpty(s.arrivalAirport)   ? "unknown" : s.arrivalAirport;
            float mins = s.durationSeconds / 60f;
            return $"Flight from {dep} to {arr} lasting {mins:F0} min over {s.distanceNm:F0} nm. " +
                   $"Overall performance score: {s.performanceScore:F0}/100.";
        }

        private static void AddHighlights(FlightReport report, FlightSessionRecord s)
        {
            if (s.performanceScore >= 90f)
                report.highlights.Add("🏆 Excellent overall performance score!");
            if (s.landingScore >= 85f)
                report.highlights.Add("✅ Great landing quality.");
            if (s.fuelEfficiencyScore >= 80f)
                report.highlights.Add("⛽ Efficient fuel management.");
            if (s.airportsVisited.Count >= 3)
                report.highlights.Add($"🛫 Visited {s.airportsVisited.Count} airports.");
            if (report.highlights.Count == 0)
                report.highlights.Add("Flight completed successfully.");
        }

        private static void AddImprovements(FlightReport report, FlightSessionRecord s)
        {
            if (s.landingScore >= 0f && s.landingScore < 60f)
                report.improvements.Add("Work on landing technique — aim for a smoother touchdown.");
            if (s.fuelEfficiencyScore < 60f)
                report.improvements.Add("Improve fuel efficiency by reducing throttle during cruise.");
            if (s.performanceScore < 70f)
                report.improvements.Add("Review flight smoothness — reduce excessive control inputs.");
        }

        private static string FormatDate(long unixSeconds)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime.ToString("yyyy-MM-dd");
        }
    }
}
