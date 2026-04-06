// ShareableCardGenerator.cs — Phase 116: Flight Analytics Dashboard
// Social sharing cards: flight stats infographic, achievement showcase.
// Namespace: SWEF.FlightAnalytics

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Builds a shareable stats card data model from a
    /// <see cref="FlightSessionRecord"/> or <see cref="AggregatedStats"/>.
    /// Actual image rendering requires a UI screenshot pass (platform-dependent).
    /// </summary>
    public class ShareableCardGenerator : MonoBehaviour
    {
        // ── Nested type ───────────────────────────────────────────────────────────

        /// <summary>Data model for a shareable stats card.</summary>
        [Serializable]
        public class ShareableCard
        {
            /// <summary>Card title (e.g. "My Best Flight").</summary>
            public string title;
            /// <summary>Pilot display name.</summary>
            public string pilotName;
            /// <summary>Aircraft identifier.</summary>
            public string aircraftId;
            /// <summary>Route string (e.g. "EGLL → KJFK").</summary>
            public string route;
            /// <summary>Performance score (0–100).</summary>
            public float score;
            /// <summary>Flight duration label (e.g. "2h 15m").</summary>
            public string durationLabel;
            /// <summary>Distance label (e.g. "1,234 nm").</summary>
            public string distanceLabel;
            /// <summary>Up to 3 highlight badges to display.</summary>
            public List<string> badges = new List<string>();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Build a shareable card from a single flight session.</summary>
        public ShareableCard BuildFlightCard(FlightSessionRecord session, string pilotName)
        {
            if (session == null) return null;

            string dep = string.IsNullOrEmpty(session.departureAirport) ? "?" : session.departureAirport;
            string arr = string.IsNullOrEmpty(session.arrivalAirport)   ? "?" : session.arrivalAirport;
            float hours = session.durationSeconds / 3600f;
            int mins    = Mathf.RoundToInt((hours % 1f) * 60);

            var card = new ShareableCard
            {
                title         = "Flight Stats",
                pilotName     = pilotName ?? "Pilot",
                aircraftId    = session.aircraftId ?? "Unknown",
                route         = $"{dep} → {arr}",
                score         = session.performanceScore,
                durationLabel = $"{Mathf.FloorToInt(hours)}h {mins:D2}m",
                distanceLabel = $"{session.distanceNm:F0} nm"
            };

            if (session.performanceScore >= 90f) card.badges.Add("🏆 Top Score");
            if (session.landingScore >= 85f)     card.badges.Add("✅ Smooth Landing");
            if (session.fuelEfficiencyScore >= 80f) card.badges.Add("⛽ Eco Pilot");

            return card;
        }

        /// <summary>Build a lifetime stats card from aggregated data.</summary>
        public ShareableCard BuildLifetimeCard(AggregatedStats stats, string pilotName)
        {
            if (stats == null) return null;

            return new ShareableCard
            {
                title         = "Lifetime Stats",
                pilotName     = pilotName ?? "Pilot",
                score         = stats.avgPerformanceScore,
                durationLabel = $"{stats.totalHours:F0} hours",
                distanceLabel = $"{stats.totalDistanceNm:F0} nm",
                badges        = new List<string>
                {
                    $"✈️ {stats.flightCount} Flights",
                    $"🗺️ {stats.uniqueAirports} Airports"
                }
            };
        }
    }
}
