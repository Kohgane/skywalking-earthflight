// ATCStripBoard.cs — Phase 119: Advanced AI Traffic Control
// Flight strip board: active flights, pending clearances, handoff status.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Electronic flight strip board tracking all active strips,
    /// pending clearances and handoff states.
    /// </summary>
    public class ATCStripBoard : MonoBehaviour
    {
        // ── Strip State ───────────────────────────────────────────────────────────

        private class StripState
        {
            public FlightStrip strip;
            public bool pendingClearance;
            public bool pendingHandoff;
            public string handoffTarget;
        }

        private readonly Dictionary<string, StripState> _board = new Dictionary<string, StripState>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Adds a strip to the board.</summary>
        public void AddStrip(FlightStrip strip)
        {
            if (strip == null) return;
            _board[strip.callsign] = new StripState { strip = strip };
        }

        /// <summary>Removes a strip from the board.</summary>
        public bool RemoveStrip(string callsign) => _board.Remove(callsign);

        /// <summary>Marks a strip as having a pending clearance.</summary>
        public bool SetPendingClearance(string callsign, bool pending)
        {
            if (!_board.TryGetValue(callsign, out var s)) return false;
            s.pendingClearance = pending;
            return true;
        }

        /// <summary>Initiates a handoff for the given callsign to a target facility.</summary>
        public bool InitiateHandoff(string callsign, string targetFacility)
        {
            if (!_board.TryGetValue(callsign, out var s)) return false;
            s.pendingHandoff = true;
            s.handoffTarget = targetFacility;
            return true;
        }

        /// <summary>Completes a handoff for the given callsign.</summary>
        public bool CompleteHandoff(string callsign)
        {
            if (!_board.TryGetValue(callsign, out var s)) return false;
            s.pendingHandoff = false;
            s.handoffTarget = null;
            return true;
        }

        /// <summary>Returns whether the given callsign has a pending clearance.</summary>
        public bool HasPendingClearance(string callsign)
            => _board.TryGetValue(callsign, out var s) && s.pendingClearance;

        /// <summary>Returns whether the given callsign has a pending handoff.</summary>
        public bool HasPendingHandoff(string callsign)
            => _board.TryGetValue(callsign, out var s) && s.pendingHandoff;

        /// <summary>Number of strips on the board.</summary>
        public int StripCount => _board.Count;

        /// <summary>Returns the strip for a given callsign, or null.</summary>
        public FlightStrip GetStrip(string callsign)
        {
            _board.TryGetValue(callsign, out var s);
            return s?.strip;
        }
    }
}
