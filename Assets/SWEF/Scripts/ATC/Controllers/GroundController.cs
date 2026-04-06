// GroundController.cs — Phase 119: Advanced AI Traffic Control
// Ground control: taxi routing, hold short instructions, runway crossing
// clearances, gate assignments.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Ground controller responsible for surface movement: taxi routes,
    /// hold-short instructions, runway crossing clearances and gate assignments.
    /// </summary>
    public class GroundController : MonoBehaviour
    {
        [Header("Airport")]
        [SerializeField] private string icao = "KLAX";

        // ── Taxi Route ────────────────────────────────────────────────────────────

        private class TaxiRoute
        {
            public string callsign;
            public List<string> taxiways = new List<string>();
            public string destinationRunwayOrGate;
            public bool holdShort;
            public string holdShortPoint;
        }

        private readonly Dictionary<string, TaxiRoute> _taxiRoutes = new Dictionary<string, TaxiRoute>();
        private readonly Dictionary<string, string> _gateAssignments = new Dictionary<string, string>();
        private readonly HashSet<string> _runwayCrossingClearances = new HashSet<string>();

        // ── Gate Assignment ───────────────────────────────────────────────────────

        /// <summary>Assigns a gate to an arriving aircraft.</summary>
        public void AssignGate(string callsign, string gateId)
        {
            _gateAssignments[callsign] = gateId;
        }

        /// <summary>Returns the gate assigned to a callsign, or null.</summary>
        public string GetGate(string callsign)
        {
            _gateAssignments.TryGetValue(callsign, out var g);
            return g;
        }

        // ── Taxi Routing ──────────────────────────────────────────────────────────

        /// <summary>Issues a taxi clearance with a route via named taxiways.</summary>
        public void IssueTaxiClearance(string callsign, List<string> taxiways, string destination)
        {
            _taxiRoutes[callsign] = new TaxiRoute
            {
                callsign = callsign,
                taxiways = new List<string>(taxiways),
                destinationRunwayOrGate = destination,
                holdShort = false
            };
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.Cleared);
        }

        /// <summary>Issues a hold-short instruction at a specified point.</summary>
        public void IssueHoldShort(string callsign, string holdShortPoint)
        {
            if (_taxiRoutes.TryGetValue(callsign, out var route))
            {
                route.holdShort = true;
                route.holdShortPoint = holdShortPoint;
            }
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.Hold);
        }

        /// <summary>Issues a runway crossing clearance.</summary>
        public bool IssueRunwayCrossing(string callsign, string runway)
        {
            if (_runwayCrossingClearances.Contains(callsign)) return false;
            _runwayCrossingClearances.Add(callsign);
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.Cleared);
            return true;
        }

        /// <summary>Clears a runway crossing when the aircraft has crossed.</summary>
        public void RunwayCrossingComplete(string callsign)
        {
            _runwayCrossingClearances.Remove(callsign);
        }

        /// <summary>Returns the taxi route for a callsign, or null if none assigned.</summary>
        public IReadOnlyList<string> GetTaxiRoute(string callsign)
        {
            _taxiRoutes.TryGetValue(callsign, out var route);
            return route?.taxiways;
        }

        /// <summary>Returns whether a given aircraft has an active hold-short instruction.</summary>
        public bool IsHolding(string callsign)
        {
            return _taxiRoutes.TryGetValue(callsign, out var r) && r.holdShort;
        }
    }
}
