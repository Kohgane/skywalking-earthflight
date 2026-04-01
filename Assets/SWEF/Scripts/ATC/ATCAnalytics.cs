using System;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_ANALYTICS_AVAILABLE
using SWEF.Analytics;
#endif

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 78 — Telemetry bridge for ATC interaction metrics.
    ///
    /// <para>Tracks clearance compliance rate, emergency declaration frequency,
    /// average approach accuracy, go-around count, and total communication volume.
    /// Dispatches events to <c>SWEF.Analytics.TelemetryDispatcher</c> when
    /// <c>SWEF_ANALYTICS_AVAILABLE</c> is defined.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ATCAnalytics : MonoBehaviour
    {
        #region Private State

        private int _clearanceCompliantCount;
        private int _clearanceTotalCount;
        private int _emergencyDeclarations;
        private int _goAroundCount;
        private float _approachDeviationSum;
        private int _approachSampleCount;
        private int _communicationVolume;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            var mgr = ATCManager.Instance;
            if (mgr == null) return;
            mgr.OnClearanceReceived += HandleClearanceReceived;
            mgr.OnEmergencyDeclared += HandleEmergencyDeclared;
        }

        private void OnDisable()
        {
            var mgr = ATCManager.Instance;
            if (mgr == null) return;
            mgr.OnClearanceReceived -= HandleClearanceReceived;
            mgr.OnEmergencyDeclared -= HandleEmergencyDeclared;
        }

        #endregion

        #region Public API

        /// <summary>Records whether the player complied with the most recent clearance.</summary>
        /// <param name="complied">True if the player followed the clearance; false otherwise.</param>
        public void RecordClearanceCompliance(bool complied)
        {
            _clearanceTotalCount++;
            if (complied) _clearanceCompliantCount++;

#if SWEF_ANALYTICS_AVAILABLE
            var td = TelemetryDispatcher.Instance ?? FindFirstObjectByType<TelemetryDispatcher>();
            td?.Dispatch("atc_clearance_compliance", complied ? 1f : 0f);
#endif
        }

        /// <summary>Records a lateral approach deviation sample in degrees.</summary>
        /// <param name="deviation">Absolute deviation in degrees from runway centreline.</param>
        public void RecordApproachAccuracy(float deviation)
        {
            _approachDeviationSum  += Mathf.Abs(deviation);
            _approachSampleCount++;

#if SWEF_ANALYTICS_AVAILABLE
            var td = TelemetryDispatcher.Instance ?? FindFirstObjectByType<TelemetryDispatcher>();
            td?.Dispatch("atc_approach_deviation", Mathf.Abs(deviation));
#endif
        }

        /// <summary>Records a go-around event.</summary>
        public void RecordGoAround()
        {
            _goAroundCount++;

#if SWEF_ANALYTICS_AVAILABLE
            var td = TelemetryDispatcher.Instance ?? FindFirstObjectByType<TelemetryDispatcher>();
            td?.Dispatch("atc_go_around", 1f);
#endif
        }

        /// <summary>
        /// Returns a human-readable summary of the current session's ATC metrics.
        /// </summary>
        public string GetSessionSummary()
        {
            float compliance = _clearanceTotalCount > 0
                ? (float)_clearanceCompliantCount / _clearanceTotalCount * 100f : 0f;
            float avgDev = _approachSampleCount > 0
                ? _approachDeviationSum / _approachSampleCount : 0f;

            return $"Clearance compliance: {compliance:F1}%\n" +
                   $"Avg approach deviation: {avgDev:F2}°\n" +
                   $"Go-arounds: {_goAroundCount}\n" +
                   $"Emergency declarations: {_emergencyDeclarations}\n" +
                   $"Messages exchanged: {_communicationVolume}";
        }

        #endregion

        #region Event Handlers

        private void HandleClearanceReceived(ATCInstruction _)
        {
            _communicationVolume++;
        }

        private void HandleEmergencyDeclared()
        {
            _emergencyDeclarations++;

#if SWEF_ANALYTICS_AVAILABLE
            var td = TelemetryDispatcher.Instance ?? FindFirstObjectByType<TelemetryDispatcher>();
            td?.Dispatch("atc_emergency_declared", 1f);
#endif
        }

        #endregion
    }
}
