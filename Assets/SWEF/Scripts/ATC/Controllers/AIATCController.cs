// AIATCController.cs — Phase 119: Advanced AI Traffic Control
// AI-driven ATC logic: traffic sequencing, separation assurance, conflict detection,
// clearance generation.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — AI-driven ATC controller that sequences traffic, assures separation,
    /// detects conflicts and automatically generates clearances.
    /// </summary>
    public class AIATCController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ATCConfig config;

        private readonly List<FlightStrip> _sequence = new List<FlightStrip>();
        private float _lastUpdateTime;

        /// <summary>Current number of aircraft in the AI sequence.</summary>
        public int SequenceCount => _sequence.Count;

        private void Update()
        {
            if (Time.time - _lastUpdateTime >= 1f)
            {
                _lastUpdateTime = Time.time;
                ProcessSequence();
            }
        }

        /// <summary>Adds a flight strip to the AI sequencer.</summary>
        public void EnqueueFlight(FlightStrip strip)
        {
            if (strip == null || _sequence.Contains(strip)) return;
            InsertByPriority(strip);
        }

        /// <summary>Removes a flight strip from the AI sequencer.</summary>
        public bool DequeueFlight(string callsign)
        {
            return _sequence.RemoveAll(s => s.callsign == callsign) > 0;
        }

        private void InsertByPriority(FlightStrip strip)
        {
            for (int i = 0; i < _sequence.Count; i++)
            {
                if ((int)strip.priority > (int)_sequence[i].priority)
                {
                    _sequence.Insert(i, strip);
                    return;
                }
            }
            _sequence.Add(strip);
        }

        private void ProcessSequence()
        {
            for (int i = 0; i < _sequence.Count; i++)
            {
                var strip = _sequence[i];
                GenerateClearance(strip);
            }
        }

        /// <summary>
        /// Generates the appropriate clearance for the current flight phase.
        /// </summary>
        public ATCInstructionCode GenerateClearance(FlightStrip strip)
        {
            if (strip == null) return ATCInstructionCode.Hold;

            switch (strip.phase)
            {
                case FlightPhase.Preflight:
                    return ATCInstructionCode.Cleared;
                case FlightPhase.Taxi:
                    return ATCInstructionCode.Cleared;
                case FlightPhase.Takeoff:
                    return ATCInstructionCode.Cleared;
                case FlightPhase.Departure:
                    return ATCInstructionCode.ClimbTo;
                case FlightPhase.Cruise:
                    return ATCInstructionCode.MaintainSpeed;
                case FlightPhase.Descent:
                    return ATCInstructionCode.DescendTo;
                case FlightPhase.Approach:
                    return ATCInstructionCode.Cleared;
                case FlightPhase.Landing:
                    return ATCInstructionCode.Cleared;
                case FlightPhase.GoAround:
                    return ATCInstructionCode.GoAround;
                case FlightPhase.Emergency:
                    return ATCInstructionCode.VectorTo;
                default:
                    return ATCInstructionCode.Hold;
            }
        }

        /// <summary>
        /// Checks whether two aircraft breach the configured separation minimum.
        /// </summary>
        public bool IsSeparationViolated(Vector3 posA, float altA, Vector3 posB, float altB)
        {
            float sep = config != null ? config.radarSeparationNM : 3f;
            float vertSep = config != null ? config.standardVerticalSeparationFt : 1000f;

            float horizDist = Vector3.Distance(
                new Vector3(posA.x, 0, posA.z),
                new Vector3(posB.x, 0, posB.z));
            float vertDist = Mathf.Abs(altA - altB);

            // Convert Unity units to nautical miles (1 NM ≈ 1852 m assumed 1 unit = 1 m)
            float horizNM = horizDist / 1852f;
            return horizNM < sep && vertDist < vertSep;
        }
    }
}
