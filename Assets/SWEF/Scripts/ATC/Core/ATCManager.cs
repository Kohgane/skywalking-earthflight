// ATCManager.cs — Phase 119: Advanced AI Traffic Control
// Phase 119 central controller: flight strips, facilities, conflict alerts,
// handoffs and holding patterns — built on top of the Phase 78 ATCManager.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Provides the full Phase 119 ATC management layer:
    /// flight strip registry, facility lookup, conflict alert management and
    /// controller handoffs.  Operates as a companion to the Phase 78
    /// <see cref="ATCManager"/>.
    /// Persists across scenes via <see cref="DontDestroyOnLoad"/>.
    /// </summary>
    public class ATCSystemManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared instance of <see cref="ATCSystemManager"/>.</summary>
        public static ATCSystemManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private ATCConfig config;

        // ── State ─────────────────────────────────────────────────────────────────

        private readonly Dictionary<string, ATCFacility> _facilities =
            new Dictionary<string, ATCFacility>();
        private readonly Dictionary<string, FlightStrip> _activeStrips =
            new Dictionary<string, FlightStrip>();
        private readonly List<ConflictAlert> _activeAlerts =
            new List<ConflictAlert>();
        private readonly Dictionary<string, HoldingPattern> _holdingAircraft =
            new Dictionary<string, HoldingPattern>();

        /// <summary>Whether the Phase 119 ATC system is operational.</summary>
        public bool IsOperational { get; private set; }

        /// <summary>Read-only view of active flight strips.</summary>
        public IReadOnlyDictionary<string, FlightStrip> ActiveStrips => _activeStrips;

        /// <summary>Read-only view of registered facilities.</summary>
        public IReadOnlyDictionary<string, ATCFacility> Facilities => _facilities;

        /// <summary>Read-only list of active conflict alerts.</summary>
        public IReadOnlyList<ConflictAlert> ActiveAlerts => _activeAlerts;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a new flight strip is created.</summary>
        public event Action<FlightStrip> OnFlightStripCreated;

        /// <summary>Raised when a flight strip is removed.</summary>
        public event Action<string> OnFlightStripRemoved;

        /// <summary>Raised when an ATC instruction is issued.</summary>
        public event Action<string, ATCInstructionCode> OnInstructionIssued;

        /// <summary>Raised when a conflict alert is generated.</summary>
        public event Action<ConflictAlert> OnConflictAlert;

        /// <summary>Raised when a handoff is initiated.</summary>
        public event Action<string, string, string> OnHandoffInitiated;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SeedDefaultFacilities();
            IsOperational = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Seed ──────────────────────────────────────────────────────────────────

        private void SeedDefaultFacilities()
        {
            RegisterFacility(new ATCFacility("KLAX_TWR", "Los Angeles Tower",   ATCFacilityType.Tower,     133.9f,  "KLAX"));
            RegisterFacility(new ATCFacility("KLAX_GND", "Los Angeles Ground",  ATCFacilityType.Ground,    121.65f, "KLAX"));
            RegisterFacility(new ATCFacility("KLAX_APP", "SoCal Approach",      ATCFacilityType.Approach,  124.5f,  "KLAX"));
            RegisterFacility(new ATCFacility("KLAX_DEP", "SoCal Departure",     ATCFacilityType.Departure, 125.2f,  "KLAX"));
            RegisterFacility(new ATCFacility("KLAX_CTR", "Los Angeles Center",  ATCFacilityType.Center,    134.5f,  "KLAX"));
            RegisterFacility(new ATCFacility("KJFK_TWR", "New York Tower",      ATCFacilityType.Tower,     119.1f,  "KJFK"));
            RegisterFacility(new ATCFacility("KJFK_APP", "New York TRACON",     ATCFacilityType.Approach,  127.4f,  "KJFK"));
            RegisterFacility(new ATCFacility("EGLL_TWR", "Heathrow Tower",      ATCFacilityType.Tower,     118.5f,  "EGLL"));
            RegisterFacility(new ATCFacility("EMERGENCY","Guard",               ATCFacilityType.Emergency, 121.5f,  "ZZZZ"));
        }

        // ── Facility Management ───────────────────────────────────────────────────

        /// <summary>Registers an ATC facility.</summary>
        public void RegisterFacility(ATCFacility facility)
        {
            if (facility != null) _facilities[facility.facilityId] = facility;
        }

        /// <summary>Returns the facility with the given ID, or null.</summary>
        public ATCFacility GetFacility(string facilityId)
        {
            _facilities.TryGetValue(facilityId, out var f);
            return f;
        }

        // ── Flight Strip Management ───────────────────────────────────────────────

        /// <summary>Creates and registers a flight strip.</summary>
        public FlightStrip CreateFlightStrip(string callsign, string type,
            string origin, string dest, int altitude)
        {
            var strip = new FlightStrip(callsign, type, origin, dest, altitude);
            _activeStrips[callsign] = strip;
            OnFlightStripCreated?.Invoke(strip);
            return strip;
        }

        /// <summary>Updates the flight phase for a tracked aircraft.</summary>
        public void UpdateFlightPhase(string callsign, FlightPhase phase)
        {
            if (_activeStrips.TryGetValue(callsign, out var strip))
                strip.phase = phase;
        }

        /// <summary>Removes a flight strip.</summary>
        public void RemoveFlightStrip(string callsign)
        {
            if (_activeStrips.Remove(callsign))
                OnFlightStripRemoved?.Invoke(callsign);
        }

        /// <summary>Returns the strip for a callsign, or null.</summary>
        public FlightStrip GetStrip(string callsign)
        {
            _activeStrips.TryGetValue(callsign, out var s);
            return s;
        }

        // ── Instruction Issuance ──────────────────────────────────────────────────

        /// <summary>Issues an ATC instruction to the specified aircraft.</summary>
        public void IssueInstruction(string callsign, ATCInstructionCode instruction)
        {
            if (_activeStrips.TryGetValue(callsign, out var strip))
                strip.lastInstruction = instruction;
            OnInstructionIssued?.Invoke(callsign, instruction);
        }

        // ── Conflict Alert Management ─────────────────────────────────────────────

        /// <summary>Adds a conflict alert.</summary>
        public void AddConflictAlert(ConflictAlert alert)
        {
            if (alert == null) return;
            _activeAlerts.Add(alert);
            OnConflictAlert?.Invoke(alert);
        }

        /// <summary>Acknowledges an alert by ID.</summary>
        public bool AcknowledgeAlert(string alertId)
        {
            foreach (var a in _activeAlerts)
                if (a.alertId == alertId) { a.acknowledged = true; return true; }
            return false;
        }

        /// <summary>Removes acknowledged alerts.</summary>
        public void ClearResolvedAlerts() => _activeAlerts.RemoveAll(a => a.acknowledged);

        // ── Handoff Management ────────────────────────────────────────────────────

        /// <summary>Initiates a controller handoff.</summary>
        public void InitiateHandoff(string callsign, string from, string to)
        {
            if (_facilities.ContainsKey(to))
                OnHandoffInitiated?.Invoke(callsign, from, to);
        }

        // ── Holding Management ────────────────────────────────────────────────────

        /// <summary>Assigns a holding pattern.</summary>
        public void AssignHoldingPattern(HoldingPattern pattern)
        {
            if (pattern != null) _holdingAircraft[pattern.callsign] = pattern;
        }

        /// <summary>Releases an aircraft from holding.</summary>
        public bool ReleaseFromHold(string callsign) => _holdingAircraft.Remove(callsign);

        /// <summary>Returns the holding pattern for a callsign, or null.</summary>
        public HoldingPattern GetHoldingPattern(string callsign)
        {
            _holdingAircraft.TryGetValue(callsign, out var h);
            return h;
        }

        // ── Statistics ────────────────────────────────────────────────────────────

        /// <summary>Number of aircraft currently tracked.</summary>
        public int TrackedAircraftCount => _activeStrips.Count;

        /// <summary>Number of aircraft currently holding.</summary>
        public int HoldingAircraftCount => _holdingAircraft.Count;

        /// <summary>Number of unacknowledged alerts.</summary>
        public int UnacknowledgedAlertCount
        {
            get { int n = 0; foreach (var a in _activeAlerts) if (!a.acknowledged) n++; return n; }
        }
    }
}
