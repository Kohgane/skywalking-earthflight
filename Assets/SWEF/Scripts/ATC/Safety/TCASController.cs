// TCASController.cs — Phase 119: Advanced AI Traffic Control
// Traffic Collision Avoidance System: TA, RA, coordinated RAs.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Simulates TCAS II: generates Traffic Advisories (TA) and
    /// Resolution Advisories (RA), and coordinates RAs between aircraft.
    /// </summary>
    public class TCASController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ATCConfig config;

        // ── TCAS Target ───────────────────────────────────────────────────────────

        private class TCASTarget
        {
            public string callsign;
            public Vector3 position;
            public float altitude;
            public float verticalRate;
            public TCASAdvisory currentAdvisory;
        }

        private readonly Dictionary<string, TCASTarget> _targets = new Dictionary<string, TCASTarget>();

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a TA or RA is issued for the ownship.</summary>
        public event Action<TCASAdvisory, string> OnAdvisoryIssued;

        /// <summary>Raised when all traffic is clear.</summary>
        public event Action OnClearOfConflict;

        // ── Target Management ─────────────────────────────────────────────────────

        /// <summary>Updates a tracked TCAS target.</summary>
        public void UpdateTarget(string callsign, Vector3 position, float altitude, float vertRate)
        {
            if (!_targets.TryGetValue(callsign, out var t))
            {
                t = new TCASTarget { callsign = callsign };
                _targets[callsign] = t;
            }
            t.position = position;
            t.altitude = altitude;
            t.verticalRate = vertRate;
        }

        /// <summary>Removes a target from TCAS tracking.</summary>
        public bool RemoveTarget(string callsign)
        {
            return _targets.Remove(callsign);
        }

        // ── Advisory Generation ───────────────────────────────────────────────────

        /// <summary>
        /// Evaluates TCAS advisories for the ownship (identified by callsign).
        /// Returns the highest advisory issued.
        /// </summary>
        public TCASAdvisory EvaluateAdvisories(
            string ownCallsign, Vector3 ownPos, float ownAlt, float ownVertRate)
        {
            float taRange  = config != null ? config.tcasTARange  : 20f;
            float raRange  = config != null ? config.tcasRARange  : 5f;
            float taAltDif = config != null ? config.tcasTAAltDiffFt : 1200f;
            float raAltDif = config != null ? config.tcasRAAltDiffFt : 600f;

            TCASAdvisory worst = TCASAdvisory.None;

            foreach (var kvp in _targets)
            {
                if (kvp.Key == ownCallsign) continue;
                var t = kvp.Value;

                float horizM = Vector3.Distance(
                    new Vector3(ownPos.x, 0, ownPos.z),
                    new Vector3(t.position.x, 0, t.position.z));
                float horizNM = horizM / 1852f;
                float altDiff = Mathf.Abs(ownAlt - t.altitude);

                if (horizNM <= raRange && altDiff < raAltDif)
                {
                    var ra = ownAlt < t.altitude ? TCASAdvisory.RA_Climb : TCASAdvisory.RA_Descend;
                    t.currentAdvisory = ra;
                    if ((int)ra > (int)worst) worst = ra;
                    OnAdvisoryIssued?.Invoke(ra, t.callsign);
                }
                else if (horizNM <= taRange && altDiff < taAltDif)
                {
                    if ((int)TCASAdvisory.TA > (int)worst)
                    {
                        worst = TCASAdvisory.TA;
                        t.currentAdvisory = TCASAdvisory.TA;
                        OnAdvisoryIssued?.Invoke(TCASAdvisory.TA, t.callsign);
                    }
                }
            }

            if (worst == TCASAdvisory.None)
                OnClearOfConflict?.Invoke();

            return worst;
        }

        /// <summary>Returns the current TCAS advisory for a tracked target.</summary>
        public TCASAdvisory GetTargetAdvisory(string callsign)
        {
            _targets.TryGetValue(callsign, out var t);
            return t?.currentAdvisory ?? TCASAdvisory.None;
        }

        /// <summary>Number of targets currently tracked by TCAS.</summary>
        public int TargetCount => _targets.Count;
    }
}
