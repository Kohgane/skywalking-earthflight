// ATCAnalytics.cs — Phase 119: Advanced AI Traffic Control (Integration)
// Telemetry: communications count, conflicts resolved, go-arounds,
// emergency declarations, separation violations.
// Namespace: SWEF.ATC

using System;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Collects and exposes ATC session analytics including
    /// communication counts, conflict statistics and safety metrics.
    /// Works alongside the Phase 78 <see cref="ATCAnalytics"/> (root ATC directory) for extended
    /// Phase 119 telemetry.
    /// </summary>
    public class ATCTelemetryAnalytics : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared instance of <see cref="ATCTelemetryAnalytics"/>.</summary>
        public static ATCTelemetryAnalytics Instance { get; private set; }

        // ── Counters ──────────────────────────────────────────────────────────────

        private int _communicationsCount;
        private int _conflictsDetected;
        private int _conflictsResolved;
        private int _goAroundsIssued;
        private int _emergencyDeclarations;
        private int _separationViolations;
        private int _handoffsCompleted;
        private float _sessionStartTime;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>Total ATC communications this session.</summary>
        public int CommunicationsCount => _communicationsCount;

        /// <summary>Total conflicts detected this session.</summary>
        public int ConflictsDetected => _conflictsDetected;

        /// <summary>Total conflicts resolved this session.</summary>
        public int ConflictsResolved => _conflictsResolved;

        /// <summary>Total go-around commands issued this session.</summary>
        public int GoAroundsIssued => _goAroundsIssued;

        /// <summary>Total emergency declarations this session.</summary>
        public int EmergencyDeclarations => _emergencyDeclarations;

        /// <summary>Total separation violations recorded this session.</summary>
        public int SeparationViolations => _separationViolations;

        /// <summary>Total controller handoffs completed this session.</summary>
        public int HandoffsCompleted => _handoffsCompleted;

        /// <summary>Session duration in seconds.</summary>
        public float SessionDurationSeconds => Time.time - _sessionStartTime;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _sessionStartTime = Time.time;
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void SubscribeEvents()
        {
            if (ATCSystemManager.Instance == null) return;
            ATCSystemManager.Instance.OnInstructionIssued += (_, __) => _communicationsCount++;
            ATCSystemManager.Instance.OnConflictAlert      += _       => _conflictsDetected++;
            ATCSystemManager.Instance.OnHandoffInitiated   += (_, __, ___) => _handoffsCompleted++;
        }

        // ── Manual Record ─────────────────────────────────────────────────────────

        /// <summary>Records a conflict resolved event.</summary>
        public void RecordConflictResolved() => _conflictsResolved++;

        /// <summary>Records a go-around issued event.</summary>
        public void RecordGoAround() => _goAroundsIssued++;

        /// <summary>Records an emergency declaration.</summary>
        public void RecordEmergency() => _emergencyDeclarations++;

        /// <summary>Records a separation violation.</summary>
        public void RecordSeparationViolation() => _separationViolations++;

        /// <summary>Resets all counters for a new session.</summary>
        public void ResetSession()
        {
            _communicationsCount = _conflictsDetected = _conflictsResolved = 0;
            _goAroundsIssued = _emergencyDeclarations = _separationViolations = _handoffsCompleted = 0;
            _sessionStartTime = Time.time;
        }

        /// <summary>Logs a session summary.</summary>
        public void LogSessionSummary()
        {
            Debug.Log($"[ATCTelemetry] {SessionDurationSeconds:F0}s | " +
                      $"Comms:{_communicationsCount} Conflicts:{_conflictsDetected}/{_conflictsResolved} " +
                      $"GoArounds:{_goAroundsIssued} Emergencies:{_emergencyDeclarations} " +
                      $"SepViolations:{_separationViolations} Handoffs:{_handoffsCompleted}");
        }
    }
}
