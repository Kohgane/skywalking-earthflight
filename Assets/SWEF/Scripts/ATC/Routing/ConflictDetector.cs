// ConflictDetector.cs — Phase 119: Advanced AI Traffic Control
// Conflict detection: predicted trajectory analysis, loss of separation
// warning, resolution advisory.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Detects and predicts conflicts between tracked aircraft
    /// by analysing projected trajectories over a configurable look-ahead time.
    /// </summary>
    public class ConflictDetector : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ATCConfig config;

        // ── Aircraft State ────────────────────────────────────────────────────────

        /// <summary>Snapshot of an aircraft's current state for conflict analysis.</summary>
        public class AircraftState
        {
            public string callsign;
            public Vector3 position;
            public Vector3 velocity;   // m/s
            public float altitude;     // feet
            public float verticalRate; // ft/min
        }

        private readonly List<AircraftState> _states = new List<AircraftState>();
        private readonly List<ConflictAlert> _detectedConflicts = new List<ConflictAlert>();

        /// <summary>Number of currently tracked aircraft states.</summary>
        public int TrackedCount => _states.Count;

        // ── State Management ──────────────────────────────────────────────────────

        /// <summary>Updates or registers an aircraft state.</summary>
        public void UpdateState(AircraftState state)
        {
            int idx = _states.FindIndex(s => s.callsign == state.callsign);
            if (idx >= 0) _states[idx] = state;
            else _states.Add(state);
        }

        /// <summary>Removes an aircraft from tracking.</summary>
        public bool RemoveState(string callsign)
        {
            return _states.RemoveAll(s => s.callsign == callsign) > 0;
        }

        // ── Conflict Scan ─────────────────────────────────────────────────────────

        /// <summary>Scans all tracked aircraft pairs for predicted conflicts.</summary>
        public List<ConflictAlert> ScanForConflicts()
        {
            _detectedConflicts.Clear();
            float lookAheadSec = (config != null ? config.conflictLookaheadMinutes : 15f) * 60f;
            float sepNM = config != null ? config.radarSeparationNM : 3f;
            float vertSep = config != null ? config.standardVerticalSeparationFt : 1000f;

            for (int i = 0; i < _states.Count - 1; i++)
            {
                for (int j = i + 1; j < _states.Count; j++)
                {
                    var a = _states[i];
                    var b = _states[j];

                    float timeToConflict;
                    float minSepNM;
                    if (PredictConflict(a, b, lookAheadSec, sepNM, vertSep, out timeToConflict, out minSepNM))
                    {
                        var severity = ClassifySeverity(timeToConflict);
                        var alert = new ConflictAlert(a.callsign, b.callsign, timeToConflict, minSepNM, severity);
                        _detectedConflicts.Add(alert);
                        ATCSystemManager.Instance?.AddConflictAlert(alert);
                    }
                }
            }
            return new List<ConflictAlert>(_detectedConflicts);
        }

        // ── Prediction ────────────────────────────────────────────────────────────

        private bool PredictConflict(
            AircraftState a, AircraftState b,
            float lookAheadSec, float sepNM, float vertSepFt,
            out float timeToConflict, out float minSepNM)
        {
            timeToConflict = float.MaxValue;
            minSepNM = float.MaxValue;

            int steps = 30;
            float dt = lookAheadSec / steps;

            for (int s = 1; s <= steps; s++)
            {
                float t = s * dt;
                Vector3 posA = a.position + a.velocity * t;
                Vector3 posB = b.position + b.velocity * t;
                float altA  = a.altitude + a.verticalRate * (t / 60f);
                float altB  = b.altitude + b.verticalRate * (t / 60f);

                float horizM  = Vector3.Distance(
                    new Vector3(posA.x, 0, posA.z),
                    new Vector3(posB.x, 0, posB.z));
                float horizNM = horizM / 1852f;
                float vertFt  = Mathf.Abs(altA - altB);

                if (horizNM < minSepNM) minSepNM = horizNM;

                if (horizNM < sepNM && vertFt < vertSepFt)
                {
                    timeToConflict = t;
                    return true;
                }
            }
            return false;
        }

        private static ConflictSeverity ClassifySeverity(float timeToConflictSec)
        {
            if (timeToConflictSec < 60f)  return ConflictSeverity.Critical;
            if (timeToConflictSec < 120f) return ConflictSeverity.Warning;
            if (timeToConflictSec < 300f) return ConflictSeverity.Caution;
            return ConflictSeverity.Advisory;
        }
    }
}
