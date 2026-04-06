// HoldingPatternController.cs — Phase 119: Advanced AI Traffic Control
// Holding pattern management: racetrack patterns, expected further clearance,
// fuel monitoring.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Manages aircraft holding patterns: racetrack geometry,
    /// lap tracking, EFC (expected further clearance) and fuel state monitoring.
    /// </summary>
    public class HoldingPatternController : MonoBehaviour
    {
        private readonly Dictionary<string, HoldingPattern> _holdings =
            new Dictionary<string, HoldingPattern>();

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a new holding pattern is assigned.</summary>
        public event Action<HoldingPattern> OnHoldingAssigned;

        /// <summary>Raised when an aircraft is released from the hold.</summary>
        public event Action<string> OnHoldingReleased;

        /// <summary>Raised when a holding aircraft reaches low fuel state.</summary>
        public event Action<string> OnLowFuel;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Assigns a holding pattern, forwarding to <see cref="ATCManager"/>.</summary>
        public void AssignHolding(HoldingPattern pattern)
        {
            if (pattern == null) return;
            _holdings[pattern.callsign] = pattern;
            ATCSystemManager.Instance?.AssignHoldingPattern(pattern);
            ATCSystemManager.Instance?.IssueInstruction(pattern.callsign, ATCInstructionCode.Hold);
            OnHoldingAssigned?.Invoke(pattern);
        }

        /// <summary>Releases an aircraft from its holding pattern.</summary>
        public bool ReleaseFromHolding(string callsign)
        {
            if (!_holdings.ContainsKey(callsign)) return false;
            _holdings.Remove(callsign);
            ATCSystemManager.Instance?.ReleaseFromHold(callsign);
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.Cleared);
            OnHoldingReleased?.Invoke(callsign);
            return true;
        }

        /// <summary>Updates the expected further clearance time for a holding aircraft.</summary>
        public void UpdateEFC(string callsign, float efcTime)
        {
            if (_holdings.TryGetValue(callsign, out var p))
                p.expectedFurtherClearance = efcTime;
        }

        /// <summary>Records a completed holding lap for tracking.</summary>
        public void RecordLap(string callsign)
        {
            if (_holdings.TryGetValue(callsign, out var p))
            {
                p.lapsCompleted++;
                // Trigger low fuel warning after excessive laps (simplified)
                if (p.lapsCompleted >= 6)
                    OnLowFuel?.Invoke(callsign);
            }
        }

        /// <summary>Returns the holding pattern for a callsign, or null.</summary>
        public HoldingPattern GetPattern(string callsign)
        {
            _holdings.TryGetValue(callsign, out var p);
            return p;
        }

        /// <summary>Number of aircraft currently in holding patterns.</summary>
        public int HoldingCount => _holdings.Count;

        /// <summary>Returns whether the given aircraft is currently holding.</summary>
        public bool IsHolding(string callsign) => _holdings.ContainsKey(callsign);
    }
}
