// SatelliteTrackingAnalytics.cs — Phase 114: Satellite & Space Debris Tracking
// Telemetry: satellites tracked, docking attempts/successes, debris encounters, scenario completions.
// Namespace: SWEF.SatelliteTracking

using System;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Records and reports analytics telemetry for the Satellite Tracking system:
    /// satellites tracked, docking attempts and successes, debris proximity encounters,
    /// and scenario completions.
    /// </summary>
    public class SatelliteTrackingAnalytics : MonoBehaviour
    {
        // ── Telemetry counters ────────────────────────────────────────────────────
        /// <summary>Total number of satellites registered in this session.</summary>
        public int SatellitesTracked { get; private set; }

        /// <summary>Number of docking attempts this session.</summary>
        public int DockingAttempts { get; private set; }

        /// <summary>Number of successful hard-docks this session.</summary>
        public int DockingSuccesses { get; private set; }

        /// <summary>Number of debris proximity encounters (Yellow or Red conjunctions).</summary>
        public int DebrisEncounters { get; private set; }

        /// <summary>Number of ISS tracking scenarios started this session.</summary>
        public int ISSTrackingSessionsStarted { get; private set; }

        /// <summary>Total time spent in interior exploration (seconds).</summary>
        public float InteriorExplorationTimeSec { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────
        private float _interiorStartTime;
        private bool  _inInterior;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (_inInterior)
                InteriorExplorationTimeSec += Time.deltaTime;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns a summary string suitable for debug or logging.</summary>
        public string GetSummary()
            => $"SatTracking Analytics — Tracked:{SatellitesTracked} " +
               $"DockAttempts:{DockingAttempts} DockSuccesses:{DockingSuccesses} " +
               $"DebrisEncounters:{DebrisEncounters} " +
               $"InteriorTime:{InteriorExplorationTimeSec:F0}s";

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SubscribeToEvents()
        {
            var mgr = SatelliteTrackingManager.Instance;
            if (mgr != null)
            {
                mgr.OnSatelliteAdded    += _ => SatellitesTracked++;
                mgr.OnConjunctionWarning += c =>
                {
                    if (c.urgencyLevel >= 2) DebrisEncounters++;
                };
            }

            var docking = FindObjectOfType<DockingScenarioController>();
            if (docking != null)
            {
                docking.OnStateChanged += state =>
                {
                    if (state == DockingState.FarApproach) DockingAttempts++;
                    if (state == DockingState.HardDock)    DockingSuccesses++;
                };
            }

            var interior = FindObjectOfType<SpaceStationInterior>();
            if (interior != null)
            {
                interior.OnModuleEntered += _ =>
                {
                    if (!_inInterior) { _inInterior = true; _interiorStartTime = Time.time; }
                };
                interior.OnExplorationEnded += () => _inInterior = false;
            }
        }

        private void UnsubscribeFromEvents()
        {
            // Event lambdas captured at subscription time; no explicit unsubscribe needed
            // for non-persistent events in this pattern.
        }
    }
}
