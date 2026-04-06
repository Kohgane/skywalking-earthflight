// FlightDetailPanel.cs — Phase 116: Flight Analytics Dashboard
// Individual flight detail view: map, stats, events timeline, performance breakdown.
// Namespace: SWEF.FlightAnalytics

using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Displays detailed information for a single
    /// <see cref="FlightSessionRecord"/>: map replay, statistics breakdown, and
    /// performance score cards.
    /// </summary>
    public class FlightDetailPanel : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Sub-Components")]
        [SerializeField] private FlightPathVisualizer pathVisualizer;
        [SerializeField] private SpeedAltitudeGraph   speedAltGraph;

        // ── State ─────────────────────────────────────────────────────────────────

        private FlightSessionRecord _currentSession;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>The session currently displayed in this panel.</summary>
        public FlightSessionRecord CurrentSession => _currentSession;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Load and display a specific session.</summary>
        public void LoadSession(FlightSessionRecord session)
        {
            _currentSession = session;
            if (session == null) { Clear(); return; }

            pathVisualizer?.Visualize(session);
            speedAltGraph?.LoadSession(session);

            Debug.Log($"[SWEF] FlightDetailPanel: Loaded session {session.sessionId}.");
        }

        /// <summary>Clear all displayed data.</summary>
        public void Clear()
        {
            pathVisualizer?.Clear();
            speedAltGraph?.Clear();
            _currentSession = null;
        }
    }
}
