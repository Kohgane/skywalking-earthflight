// CommunityStatsProvider.cs — Phase 116: Flight Analytics Dashboard
// Aggregate community statistics: global flight hours, popular routes, busiest airports.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Provides stub community statistics. When
    /// <c>SWEF_MULTIPLAYER_AVAILABLE</c> is defined, data is fetched from a backend;
    /// otherwise, placeholder values are returned for offline use.
    /// </summary>
    public class CommunityStatsProvider : MonoBehaviour
    {
        // ── Nested type ───────────────────────────────────────────────────────────

        /// <summary>Snapshot of community-wide aggregate statistics.</summary>
        [System.Serializable]
        public class CommunitySnapshot
        {
            /// <summary>Total flight hours logged by all players.</summary>
            public float globalFlightHours;
            /// <summary>Total flights ever recorded.</summary>
            public int   totalFlights;
            /// <summary>Most-flown route (dep–arr pair).</summary>
            public string popularRoute;
            /// <summary>Most-visited airport ICAO code.</summary>
            public string busiestAirport;
            /// <summary>Number of active pilots in the last 30 days.</summary>
            public int activePilots;
        }

        // ── State ─────────────────────────────────────────────────────────────────

        private CommunitySnapshot _cached;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Return the most recently fetched community snapshot (may be null).</summary>
        public CommunitySnapshot Snapshot => _cached;

        /// <summary>
        /// Refresh community stats.  When online (<c>SWEF_MULTIPLAYER_AVAILABLE</c>),
        /// this would initiate a network request; offline, returns placeholder data.
        /// </summary>
        public void Refresh()
        {
#if SWEF_MULTIPLAYER_AVAILABLE
            // TODO: fetch from backend API and populate _cached
            Debug.Log("[SWEF] CommunityStatsProvider: Fetching community stats...");
#else
            _cached = new CommunitySnapshot
            {
                globalFlightHours = 0f,
                totalFlights      = 0,
                popularRoute      = "N/A",
                busiestAirport    = "N/A",
                activePilots      = 0
            };
#endif
        }

        /// <summary>Get the global average performance score (stub — returns 0 when offline).</summary>
        public float GlobalAverageScore() => 0f;
    }
}
