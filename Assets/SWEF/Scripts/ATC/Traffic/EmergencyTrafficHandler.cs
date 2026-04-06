// EmergencyTrafficHandler.cs — Phase 119: Advanced AI Traffic Control
// Emergency handling: priority landing, fire/rescue notification, airspace
// clearing, diversion coordination.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Handles emergency traffic: assigns priority landing slots,
    /// coordinates fire/rescue notification and clears conflicting traffic.
    /// </summary>
    public class EmergencyTrafficHandler : MonoBehaviour
    {
        // ── Emergency Record ──────────────────────────────────────────────────────

        private class EmergencyRecord
        {
            public string callsign;
            public string emergencyType;
            public string divertedTo;
            public float declaredAt;
            public bool rescueNotified;
            public bool resolved;
        }

        private readonly List<EmergencyRecord> _emergencies = new List<EmergencyRecord>();

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when an emergency is declared.</summary>
        public event Action<string, string> OnEmergencyDeclared;

        /// <summary>Raised when rescue services are notified.</summary>
        public event Action<string> OnRescueNotified;

        /// <summary>Raised when an emergency is resolved.</summary>
        public event Action<string> OnEmergencyResolved;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Declares an emergency for the specified callsign.</summary>
        public void DeclareEmergency(string callsign, string emergencyType)
        {
            // Escalate priority on the flight strip
            var strip = ATCSystemManager.Instance?.GetStrip(callsign);
            if (strip != null)
            {
                strip.priority = TrafficPriority.Emergency;
                strip.phase = FlightPhase.Emergency;
            }

            _emergencies.Add(new EmergencyRecord
            {
                callsign = callsign,
                emergencyType = emergencyType,
                declaredAt = Time.time
            });

            // Issue vector-to / priority handling
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.VectorTo);
            OnEmergencyDeclared?.Invoke(callsign, emergencyType);
        }

        /// <summary>Notifies fire/rescue services for the given emergency.</summary>
        public bool NotifyRescue(string callsign)
        {
            var rec = _emergencies.Find(e => e.callsign == callsign && !e.resolved);
            if (rec == null) return false;
            rec.rescueNotified = true;
            OnRescueNotified?.Invoke(callsign);
            return true;
        }

        /// <summary>Diverts an emergency aircraft to an alternate airport.</summary>
        public void DivertTo(string callsign, string divertIcao)
        {
            var rec = _emergencies.Find(e => e.callsign == callsign && !e.resolved);
            if (rec != null) rec.divertedTo = divertIcao;
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.VectorTo);
        }

        /// <summary>Resolves an emergency for the given callsign.</summary>
        public bool ResolveEmergency(string callsign)
        {
            var rec = _emergencies.Find(e => e.callsign == callsign && !e.resolved);
            if (rec == null) return false;
            rec.resolved = true;
            OnEmergencyResolved?.Invoke(callsign);
            return true;
        }

        /// <summary>Returns whether an active (unresolved) emergency exists for a callsign.</summary>
        public bool HasActiveEmergency(string callsign)
            => _emergencies.Exists(e => e.callsign == callsign && !e.resolved);

        /// <summary>Number of active (unresolved) emergencies.</summary>
        public int ActiveEmergencyCount => Count(e => !e.resolved);

        private int Count(Func<EmergencyRecord, bool> pred)
        {
            int n = 0;
            foreach (var e in _emergencies) if (pred(e)) n++;
            return n;
        }
    }
}
