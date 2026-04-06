// TowerController.cs — Phase 119: Advanced AI Traffic Control
// Tower control: runway assignment, takeoff/landing clearances, traffic
// pattern management, go-around commands.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Tower controller managing runway assignment, takeoff and landing
    /// clearances, traffic pattern and go-around commands.
    /// </summary>
    public class TowerController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ATCConfig config;

        [Header("Airport")]
        [SerializeField] private string icao = "KLAX";

        private readonly Dictionary<string, RunwayAssignment> _runwayAssignments = new Dictionary<string, RunwayAssignment>();
        private readonly HashSet<string> _runwaysOccupied = new HashSet<string>();
        private readonly List<string> _clearedForTakeoff = new List<string>();
        private readonly List<string> _clearedToLand = new List<string>();

        /// <summary>Number of currently occupied runways.</summary>
        public int OccupiedRunwayCount => _runwaysOccupied.Count;

        // ── Runway Assignment ─────────────────────────────────────────────────────

        /// <summary>Assigns a runway to an aircraft for departure or landing.</summary>
        public RunwayAssignment AssignRunway(string callsign, string runway, bool isLanding)
        {
            var assignment = new RunwayAssignment(icao, runway, isLanding, callsign);
            _runwayAssignments[callsign] = assignment;
            return assignment;
        }

        /// <summary>Returns the runway assignment for a given callsign, or null.</summary>
        public RunwayAssignment GetRunwayAssignment(string callsign)
        {
            _runwayAssignments.TryGetValue(callsign, out var a);
            return a;
        }

        // ── Takeoff Clearances ────────────────────────────────────────────────────

        /// <summary>Clears an aircraft for takeoff. Returns false if runway is occupied.</summary>
        public bool ClearForTakeoff(string callsign)
        {
            if (!_runwayAssignments.TryGetValue(callsign, out var assignment)) return false;
            if (_runwaysOccupied.Contains(assignment.runwayId)) return false;

            _runwaysOccupied.Add(assignment.runwayId);
            _clearedForTakeoff.Add(callsign);
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.Cleared);
            ATCSystemManager.Instance?.UpdateFlightPhase(callsign, FlightPhase.Takeoff);
            return true;
        }

        /// <summary>Notifies the tower that an aircraft has departed the runway.</summary>
        public void AircraftDeparted(string callsign)
        {
            _clearedForTakeoff.Remove(callsign);
            if (_runwayAssignments.TryGetValue(callsign, out var a))
                _runwaysOccupied.Remove(a.runwayId);
            ATCSystemManager.Instance?.UpdateFlightPhase(callsign, FlightPhase.Departure);
        }

        // ── Landing Clearances ────────────────────────────────────────────────────

        /// <summary>Clears an aircraft to land. Returns false if runway is occupied.</summary>
        public bool ClearToLand(string callsign)
        {
            if (!_runwayAssignments.TryGetValue(callsign, out var assignment)) return false;
            if (_runwaysOccupied.Contains(assignment.runwayId)) return false;

            _runwaysOccupied.Add(assignment.runwayId);
            _clearedToLand.Add(callsign);
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.Cleared);
            ATCSystemManager.Instance?.UpdateFlightPhase(callsign, FlightPhase.Landing);
            return true;
        }

        /// <summary>Notifies the tower that an aircraft has vacated the runway.</summary>
        public void RunwayVacated(string callsign)
        {
            _clearedToLand.Remove(callsign);
            if (_runwayAssignments.TryGetValue(callsign, out var a))
                _runwaysOccupied.Remove(a.runwayId);
        }

        // ── Go-Around ─────────────────────────────────────────────────────────────

        /// <summary>Commands an aircraft to go around.</summary>
        public void CommandGoAround(string callsign)
        {
            if (_runwayAssignments.TryGetValue(callsign, out var a))
                _runwaysOccupied.Remove(a.runwayId);
            _clearedToLand.Remove(callsign);
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.GoAround);
            ATCSystemManager.Instance?.UpdateFlightPhase(callsign, FlightPhase.GoAround);
        }

        /// <summary>Returns whether an aircraft currently holds a takeoff clearance.</summary>
        public bool HasTakeoffClearance(string callsign) => _clearedForTakeoff.Contains(callsign);

        /// <summary>Returns whether an aircraft currently holds a landing clearance.</summary>
        public bool HasLandingClearance(string callsign) => _clearedToLand.Contains(callsign);
    }
}
