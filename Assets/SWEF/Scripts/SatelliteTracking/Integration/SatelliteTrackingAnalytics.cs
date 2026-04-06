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

        // Stored delegates for clean unsubscription
        private Action<SatelliteRecord> _onSatelliteAdded;
        private Action<ConjunctionData> _onConjunctionWarning;
        private Action<DockingState>    _onDockingStateChanged;
        private Action<SpaceStationInterior.ISSModule> _onModuleEntered;
        private Action                  _onExplorationEnded;

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
                _onSatelliteAdded    = _ => SatellitesTracked++;
                _onConjunctionWarning = c => { if (c.urgencyLevel >= 2) DebrisEncounters++; };
                mgr.OnSatelliteAdded     += _onSatelliteAdded;
                mgr.OnConjunctionWarning += _onConjunctionWarning;
            }

            var docking = FindObjectOfType<DockingScenarioController>();
            if (docking != null)
            {
                _onDockingStateChanged = state =>
                {
                    if (state == DockingState.FarApproach) DockingAttempts++;
                    if (state == DockingState.HardDock)    DockingSuccesses++;
                };
                docking.OnStateChanged += _onDockingStateChanged;
            }

            var interior = FindObjectOfType<SpaceStationInterior>();
            if (interior != null)
            {
                _onModuleEntered = _ =>
                {
                    if (!_inInterior) { _inInterior = true; _interiorStartTime = Time.time; }
                };
                _onExplorationEnded = () => _inInterior = false;
                interior.OnModuleEntered    += _onModuleEntered;
                interior.OnExplorationEnded += _onExplorationEnded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            var mgr = SatelliteTrackingManager.Instance;
            if (mgr != null)
            {
                if (_onSatelliteAdded    != null) mgr.OnSatelliteAdded     -= _onSatelliteAdded;
                if (_onConjunctionWarning != null) mgr.OnConjunctionWarning -= _onConjunctionWarning;
            }

            var docking = FindObjectOfType<DockingScenarioController>();
            if (docking != null && _onDockingStateChanged != null)
                docking.OnStateChanged -= _onDockingStateChanged;

            var interior = FindObjectOfType<SpaceStationInterior>();
            if (interior != null)
            {
                if (_onModuleEntered    != null) interior.OnModuleEntered    -= _onModuleEntered;
                if (_onExplorationEnded != null) interior.OnExplorationEnded -= _onExplorationEnded;
            }
        }
    }
}
