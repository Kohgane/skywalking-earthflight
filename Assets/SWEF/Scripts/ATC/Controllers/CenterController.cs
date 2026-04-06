// CenterController.cs — Phase 119: Advanced AI Traffic Control
// En-route center: flight level assignments, direct-to routing,
// sector handoffs, oceanic clearances.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — En-route center controller managing high-altitude traffic:
    /// flight level assignments, direct routing, sector handoffs and oceanic clearances.
    /// </summary>
    public class CenterController : MonoBehaviour
    {
        [Header("Center Identity")]
        [SerializeField] private string centerName = "Los Angeles Center";
        [SerializeField] private string facilityId = "ZLA";

        private readonly Dictionary<string, int> _flightLevelAssignments = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _directToAssignments = new Dictionary<string, string>();
        private readonly HashSet<string> _oceanicClearances = new HashSet<string>();

        /// <summary>Name of this en-route center.</summary>
        public string CenterName => centerName;

        // ── Flight Level Management ───────────────────────────────────────────────

        /// <summary>Assigns a flight level to an aircraft.</summary>
        public void AssignFlightLevel(string callsign, int flightLevel)
        {
            _flightLevelAssignments[callsign] = flightLevel;
            bool climb = !_flightLevelAssignments.TryGetValue(callsign, out int current) || flightLevel > current;
            ATCSystemManager.Instance?.IssueInstruction(callsign,
                climb ? ATCInstructionCode.ClimbTo : ATCInstructionCode.DescendTo);
        }

        /// <summary>Returns the assigned flight level for a callsign, or -1.</summary>
        public int GetFlightLevel(string callsign)
        {
            _flightLevelAssignments.TryGetValue(callsign, out int fl);
            return fl == 0 ? -1 : fl;
        }

        // ── Direct-To Routing ─────────────────────────────────────────────────────

        /// <summary>Issues a direct-to clearance to a named waypoint.</summary>
        public void IssueDirect(string callsign, string waypointId)
        {
            _directToAssignments[callsign] = waypointId;
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.VectorTo);
        }

        /// <summary>Returns the direct-to waypoint for a callsign, or null.</summary>
        public string GetDirectTo(string callsign)
        {
            _directToAssignments.TryGetValue(callsign, out var wp);
            return wp;
        }

        // ── Sector Handoff ────────────────────────────────────────────────────────

        /// <summary>Hands off an aircraft to the next sector/facility.</summary>
        public void HandoffToSector(string callsign, string nextFacilityId)
        {
            ATCSystemManager.Instance?.InitiateHandoff(callsign, facilityId, nextFacilityId);
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.ContactFrequency);
        }

        // ── Oceanic Clearances ────────────────────────────────────────────────────

        /// <summary>Issues an oceanic clearance for trans-oceanic flight.</summary>
        public void IssueOceanicClearance(string callsign, int flightLevel, int machNumber)
        {
            _oceanicClearances.Add(callsign);
            _flightLevelAssignments[callsign] = flightLevel;
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.Cleared);
        }

        /// <summary>Returns whether an aircraft holds an oceanic clearance.</summary>
        public bool HasOceanicClearance(string callsign) => _oceanicClearances.Contains(callsign);
    }
}
