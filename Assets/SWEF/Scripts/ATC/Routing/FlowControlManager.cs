// FlowControlManager.cs — Phase 119: Advanced AI Traffic Control
// Traffic flow control: ground stops, ground delay programs,
// miles-in-trail restrictions, rerouting.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Manages national traffic flow control programs including
    /// ground stops, ground delay programs and miles-in-trail restrictions.
    /// </summary>
    public class FlowControlManager : MonoBehaviour
    {
        // ── Ground Stop ───────────────────────────────────────────────────────────

        private class GroundStop
        {
            public string icao;
            public float endTime;
            public string reason;
        }

        // ── GDP Entry ─────────────────────────────────────────────────────────────

        private class GDPEntry
        {
            public string callsign;
            public float controlledDepartureTime;
            public int delayMinutes;
        }

        private readonly Dictionary<string, GroundStop> _groundStops = new Dictionary<string, GroundStop>();
        private readonly List<GDPEntry> _gdpEntries = new List<GDPEntry>();
        private float _milesInTrailRestriction = -1f;

        // ── Ground Stop ───────────────────────────────────────────────────────────

        /// <summary>Issues a ground stop for the specified airport.</summary>
        public void IssueGroundStop(string icao, float durationSeconds, string reason)
        {
            _groundStops[icao] = new GroundStop
            {
                icao = icao,
                endTime = Time.time + durationSeconds,
                reason = reason
            };
        }

        /// <summary>Lifts the ground stop for the specified airport.</summary>
        public void LiftGroundStop(string icao)
        {
            _groundStops.Remove(icao);
        }

        /// <summary>Returns whether a ground stop is active for the given airport.</summary>
        public bool IsGroundStopActive(string icao)
        {
            if (!_groundStops.TryGetValue(icao, out var gs)) return false;
            if (Time.time > gs.endTime)
            {
                _groundStops.Remove(icao);
                return false;
            }
            return true;
        }

        // ── Ground Delay Program ──────────────────────────────────────────────────

        /// <summary>Assigns a controlled departure time (GDP slot) to a flight.</summary>
        public void AssignGDPSlot(string callsign, int delayMinutes)
        {
            _gdpEntries.RemoveAll(e => e.callsign == callsign);
            _gdpEntries.Add(new GDPEntry
            {
                callsign = callsign,
                delayMinutes = delayMinutes,
                controlledDepartureTime = Time.time + delayMinutes * 60f
            });
        }

        /// <summary>Returns the GDP delay in minutes for a flight, or 0 if none.</summary>
        public int GetGDPDelay(string callsign)
        {
            var entry = _gdpEntries.Find(e => e.callsign == callsign);
            return entry?.delayMinutes ?? 0;
        }

        // ── Miles-in-Trail ────────────────────────────────────────────────────────

        /// <summary>Sets a miles-in-trail restriction. -1 to clear.</summary>
        public void SetMilesInTrail(float miles)
        {
            _milesInTrailRestriction = miles;
        }

        /// <summary>Current miles-in-trail restriction, or -1 if none.</summary>
        public float MilesInTrailRestriction => _milesInTrailRestriction;

        /// <summary>Number of active ground stops.</summary>
        public int ActiveGroundStopCount => _groundStops.Count;

        /// <summary>Number of flights in the ground delay program.</summary>
        public int GDPCount => _gdpEntries.Count;
    }
}
