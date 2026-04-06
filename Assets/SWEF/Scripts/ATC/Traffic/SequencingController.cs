// SequencingController.cs — Phase 119: Advanced AI Traffic Control
// Arrival/departure sequencing: FCFS with priority, spacing adjustments,
// runway balancing.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Controls arrival and departure sequencing with first-come-first-served
    /// ordering modified by priority, balanced runway usage and spacing rules.
    /// </summary>
    public class SequencingController : MonoBehaviour
    {
        // ── Sequence Entry ────────────────────────────────────────────────────────

        private class SequenceEntry
        {
            public string callsign;
            public bool isArrival;
            public TrafficPriority priority;
            public float registeredAt;
            public string assignedRunway;
        }

        private readonly List<SequenceEntry> _arrivalSeq  = new List<SequenceEntry>();
        private readonly List<SequenceEntry> _departureSeq = new List<SequenceEntry>();

        private readonly Dictionary<string, int> _runwayUsage = new Dictionary<string, int>();

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a runway is assigned to a flight.</summary>
        public event Action<string, string> OnRunwayAssigned;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Adds an arriving flight to the arrival sequence.</summary>
        public int SequenceArrival(string callsign, TrafficPriority priority = TrafficPriority.Normal)
        {
            var entry = new SequenceEntry
            {
                callsign = callsign,
                isArrival = true,
                priority = priority,
                registeredAt = Time.time
            };
            InsertSorted(_arrivalSeq, entry);
            return _arrivalSeq.IndexOf(entry) + 1;
        }

        /// <summary>Adds a departing flight to the departure sequence.</summary>
        public int SequenceDeparture(string callsign, TrafficPriority priority = TrafficPriority.Normal)
        {
            var entry = new SequenceEntry
            {
                callsign = callsign,
                isArrival = false,
                priority = priority,
                registeredAt = Time.time
            };
            InsertSorted(_departureSeq, entry);
            return _departureSeq.IndexOf(entry) + 1;
        }

        private void InsertSorted(List<SequenceEntry> list, SequenceEntry entry)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if ((int)entry.priority > (int)list[i].priority)
                {
                    list.Insert(i, entry);
                    return;
                }
            }
            list.Add(entry);
        }

        /// <summary>Assigns the least-busy runway to the next flight in sequence.</summary>
        public string AssignBalancedRunway(string callsign, List<string> availableRunways)
        {
            if (availableRunways == null || availableRunways.Count == 0) return null;

            string best = availableRunways.OrderBy(r => _runwayUsage.GetValueOrDefault(r, 0)).First();
            _runwayUsage[best] = _runwayUsage.GetValueOrDefault(best, 0) + 1;

            var entry = FindEntry(callsign);
            if (entry != null) entry.assignedRunway = best;

            OnRunwayAssigned?.Invoke(callsign, best);
            return best;
        }

        /// <summary>Removes a flight from whichever sequence it is in.</summary>
        public bool RemoveFlight(string callsign)
        {
            bool removed = _arrivalSeq.RemoveAll(e => e.callsign == callsign) > 0;
            removed |= _departureSeq.RemoveAll(e => e.callsign == callsign) > 0;
            return removed;
        }

        /// <summary>Returns the arrival sequence position (1-based) or -1.</summary>
        public int GetArrivalPosition(string callsign)
        {
            int idx = _arrivalSeq.FindIndex(e => e.callsign == callsign);
            return idx >= 0 ? idx + 1 : -1;
        }

        /// <summary>Returns the departure sequence position (1-based) or -1.</summary>
        public int GetDeparturePosition(string callsign)
        {
            int idx = _departureSeq.FindIndex(e => e.callsign == callsign);
            return idx >= 0 ? idx + 1 : -1;
        }

        private SequenceEntry FindEntry(string callsign)
        {
            return _arrivalSeq.Find(e => e.callsign == callsign)
                ?? _departureSeq.Find(e => e.callsign == callsign);
        }

        /// <summary>Number of aircraft in the arrival sequence.</summary>
        public int ArrivalCount => _arrivalSeq.Count;

        /// <summary>Number of aircraft in the departure sequence.</summary>
        public int DepartureCount => _departureSeq.Count;
    }
}
