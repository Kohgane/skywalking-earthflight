// SpaceStationAnalytics.cs — SWEF Space Station & Orbital Docking System
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpaceStation
{
    /// <summary>
    /// Emits telemetry events for the Space Station system via
    /// <c>TelemetryDispatcher.EnqueueEvent()</c>.
    /// Compiled only when <c>SWEF_ANALYTICS_AVAILABLE</c> is defined; otherwise
    /// the class is a no-op stub with debug logging in development builds.
    /// </summary>
    public class SpaceStationAnalytics : MonoBehaviour
    {
        // ── Session tracking ──────────────────────────────────────────────────────

        private float  _sessionStartTime;
        private string _currentStationId = string.Empty;
        private int    _phaseChanges;
        private bool   _dockingComplete;
        private float  _rcsConsumedThisSession;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (DockingController.Instance != null)
            {
                DockingController.Instance.OnPhaseChanged    += OnPhaseChanged;
                DockingController.Instance.OnDockingComplete += OnDockingComplete;
                DockingController.Instance.OnDockingAborted  += OnDockingAborted;
            }
        }

        private void OnDisable()
        {
            if (DockingController.Instance != null)
            {
                DockingController.Instance.OnPhaseChanged    -= OnPhaseChanged;
                DockingController.Instance.OnDockingComplete -= OnDockingComplete;
                DockingController.Instance.OnDockingAborted  -= OnDockingAborted;
            }

            EmitSessionSummary();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Records the start of a docking approach for telemetry purposes.</summary>
        public void TrackApproachStarted(string stationId)
        {
            _currentStationId  = stationId;
            _sessionStartTime  = Time.time;
            _phaseChanges      = 0;
            _dockingComplete   = false;

            Emit("station_approach_started", new Dictionary<string, object>
            {
                { "station_id", stationId }
            });
        }

        /// <summary>Records RCS fuel consumed for telemetry.</summary>
        public void TrackRcsFuelConsumed(float amount)
        {
            _rcsConsumedThisSession += amount;
            Emit("rcs_fuel_consumed", new Dictionary<string, object>
            {
                { "amount", amount },
                { "total",  _rcsConsumedThisSession }
            });
        }

        /// <summary>Records entry into a station interior.</summary>
        public void TrackInteriorEntered(string stationId)
        {
            Emit("station_interior_entered", new Dictionary<string, object>
            {
                { "station_id", stationId }
            });
        }

        /// <summary>Records exit from a station interior.</summary>
        public void TrackInteriorExited(string stationId, float durationSeconds)
        {
            Emit("station_interior_exited", new Dictionary<string, object>
            {
                { "station_id",      stationId },
                { "duration_seconds", durationSeconds }
            });
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void OnPhaseChanged(DockingApproachPhase phase)
        {
            _phaseChanges++;
            Emit("docking_phase_changed", new Dictionary<string, object>
            {
                { "station_id", _currentStationId },
                { "phase",      phase.ToString() }
            });
        }

        private void OnDockingComplete()
        {
            _dockingComplete = true;
            Emit("docking_completed", new Dictionary<string, object>
            {
                { "station_id",       _currentStationId },
                { "phase_changes",    _phaseChanges },
                { "duration_seconds", Time.time - _sessionStartTime }
            });
        }

        private void OnDockingAborted(string reason)
        {
            Emit("docking_aborted", new Dictionary<string, object>
            {
                { "station_id", _currentStationId },
                { "reason",     reason },
                { "phase",      DockingController.Instance?.CurrentPhase.ToString() ?? "unknown" }
            });
        }

        private void EmitSessionSummary()
        {
            if (string.IsNullOrEmpty(_currentStationId)) return;

            Emit("station_session_summary", new Dictionary<string, object>
            {
                { "station_id",         _currentStationId },
                { "docking_complete",   _dockingComplete },
                { "phase_changes",      _phaseChanges },
                { "rcs_fuel_consumed",  _rcsConsumedThisSession },
                { "duration_seconds",   Time.time - _sessionStartTime }
            });
        }

        private void Emit(string eventName, Dictionary<string, object> properties)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent(
                new SWEF.Analytics.TelemetryEvent(eventName, properties));
#else
            if (Debug.isDebugBuild)
                Debug.Log($"[SpaceStationAnalytics] Event: {eventName}");
#endif
        }
    }
}
