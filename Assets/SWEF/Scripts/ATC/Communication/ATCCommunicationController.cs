// ATCCommunicationController.cs — Phase 119: Advanced AI Traffic Control
// ATC radio communication: realistic phraseology, readback/hearback,
// frequency management.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Manages ATC radio communications: phrase generation,
    /// transmission queuing, readback validation and frequency control.
    /// </summary>
    public class ATCCommunicationController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ATCConfig config;

        // ── Transmission ──────────────────────────────────────────────────────────

        /// <summary>A single ATC radio transmission.</summary>
        public class ATCTransmission
        {
            public string facilityCallsign;
            public string aircraftCallsign;
            public ATCInstructionCode instruction;
            public string phraseText;
            public float timestamp;
            public bool requiresReadback;
            public bool readbackReceived;
        }

        private readonly List<ATCTransmission> _log = new List<ATCTransmission>();

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a new transmission is generated.</summary>
        public event Action<ATCTransmission> OnTransmission;

        // ── Phraseology ───────────────────────────────────────────────────────────

        /// <summary>Generates and logs a standard ATC phrase for the given instruction.</summary>
        public ATCTransmission GenerateTransmission(
            string facility, string aircraft, ATCInstructionCode instruction, string parameter = "")
        {
            string phrase = BuildPhrase(aircraft, instruction, parameter);
            var tx = new ATCTransmission
            {
                facilityCallsign = facility,
                aircraftCallsign = aircraft,
                instruction      = instruction,
                phraseText       = phrase,
                timestamp        = Time.time,
                requiresReadback = RequiresReadback(instruction)
            };
            _log.Add(tx);
            OnTransmission?.Invoke(tx);
            return tx;
        }

        private string BuildPhrase(string callsign, ATCInstructionCode instr, string param)
        {
            return instr switch
            {
                ATCInstructionCode.Cleared         => $"{callsign}, cleared as filed.",
                ATCInstructionCode.Hold            => $"{callsign}, hold short, traffic.",
                ATCInstructionCode.GoAround        => $"{callsign}, go around, I say again, go around.",
                ATCInstructionCode.VectorTo        => $"{callsign}, fly heading {param}.",
                ATCInstructionCode.DescendTo       => $"{callsign}, descend and maintain {param}.",
                ATCInstructionCode.ClimbTo         => $"{callsign}, climb and maintain {param}.",
                ATCInstructionCode.MaintainSpeed   => $"{callsign}, maintain {param} knots.",
                ATCInstructionCode.ContactFrequency=> $"{callsign}, contact {param}.",
                _                              => $"{callsign}, standby."
            };
        }

        private bool RequiresReadback(ATCInstructionCode instr)
        {
            return instr switch
            {
                ATCInstructionCode.Cleared          => true,
                ATCInstructionCode.GoAround         => true,
                ATCInstructionCode.DescendTo        => true,
                ATCInstructionCode.ClimbTo          => true,
                ATCInstructionCode.ContactFrequency => true,
                _                               => false
            };
        }

        /// <summary>Records a readback for a transmission.</summary>
        public bool RecordReadback(string aircraftCallsign, ATCInstructionCode instruction)
        {
            for (int i = _log.Count - 1; i >= 0; i--)
            {
                var tx = _log[i];
                if (tx.aircraftCallsign == aircraftCallsign && tx.instruction == instruction)
                {
                    tx.readbackReceived = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Number of transmissions in the communication log.</summary>
        public int LogCount => _log.Count;

        /// <summary>Returns the most recent transmission, or null.</summary>
        public ATCTransmission LastTransmission => _log.Count > 0 ? _log[_log.Count - 1] : null;
    }
}
