// FlightAnalyticsManager.cs — Phase 116: Flight Analytics Dashboard
// Central singleton manager. DontDestroyOnLoad.
// Namespace: SWEF.FlightAnalytics

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Central singleton for the Flight Analytics Dashboard.
    /// Coordinates data collection, statistical analysis, heatmap generation,
    /// report creation, and leaderboard integration.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class FlightAnalyticsManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared singleton instance.</summary>
        public static FlightAnalyticsManager Instance { get; private set; }

        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private FlightAnalyticsConfig config;

        [Header("Sub-Systems")]
        [SerializeField] private FlightDataRecorder dataRecorder;
        [SerializeField] private FlightSessionTracker sessionTracker;
        [SerializeField] private HistoricalDataStore dataStore;
        [SerializeField] private FlightStatisticsEngine statisticsEngine;

        // ── Public state ──────────────────────────────────────────────────────────

        /// <summary>Runtime configuration (read-only access).</summary>
        public FlightAnalyticsConfig Config => config;

        /// <summary>Whether a flight session is currently being recorded.</summary>
        public bool IsRecording => dataRecorder != null && dataRecorder.IsRecording;

        /// <summary>The current active session record (null if not recording).</summary>
        public FlightSessionRecord CurrentSession => sessionTracker != null ? sessionTracker.CurrentSession : null;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a new flight session begins recording.</summary>
        public event Action<FlightSessionRecord> OnSessionStarted;

        /// <summary>Raised when the active flight session ends.</summary>
        public event Action<FlightSessionRecord> OnSessionEnded;

        /// <summary>Raised when a new flight report has been generated.</summary>
        public event Action<FlightReport> OnReportGenerated;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (config == null)
                Debug.LogWarning("[SWEF] FlightAnalyticsManager: No config assigned — using defaults.");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Start recording a new flight session.
        /// </summary>
        /// <param name="aircraftId">Identifier of the aircraft being flown.</param>
        /// <param name="departureIcao">ICAO code of the departure airport (optional).</param>
        public void StartSession(string aircraftId, string departureIcao = null)
        {
            if (IsRecording)
            {
                Debug.LogWarning("[SWEF] FlightAnalyticsManager: Session already active — EndSession() first.");
                return;
            }

            if (sessionTracker != null)
            {
                sessionTracker.BeginSession(aircraftId, departureIcao);
                OnSessionStarted?.Invoke(sessionTracker.CurrentSession);
            }

            if (dataRecorder != null)
                dataRecorder.StartRecording();

            Debug.Log($"[SWEF] FlightAnalyticsManager: Session started (aircraft={aircraftId}).");
        }

        /// <summary>
        /// End the current flight session, persist it, and generate a report.
        /// </summary>
        /// <param name="arrivalIcao">ICAO code of the arrival airport (optional).</param>
        /// <returns>The completed session record.</returns>
        public FlightSessionRecord EndSession(string arrivalIcao = null)
        {
            if (!IsRecording)
            {
                Debug.LogWarning("[SWEF] FlightAnalyticsManager: No active session to end.");
                return null;
            }

            if (dataRecorder != null)
                dataRecorder.StopRecording();

            FlightSessionRecord session = null;
            if (sessionTracker != null)
            {
                session = sessionTracker.EndSession(arrivalIcao);
                OnSessionEnded?.Invoke(session);
            }

            if (session != null && dataStore != null)
                dataStore.SaveSession(session);

            Debug.Log("[SWEF] FlightAnalyticsManager: Session ended and saved.");
            return session;
        }

        /// <summary>
        /// Retrieve all stored session records filtered by the given time range.
        /// </summary>
        public List<FlightSessionRecord> GetSessions(TimeRange range)
        {
            return dataStore != null ? dataStore.GetSessions(range) : new List<FlightSessionRecord>();
        }

        /// <summary>
        /// Compute aggregated statistics for the given time range.
        /// </summary>
        public AggregatedStats GetAggregatedStats(TimeRange range)
        {
            var sessions = GetSessions(range);
            return statisticsEngine != null
                ? statisticsEngine.Aggregate(sessions)
                : new AggregatedStats();
        }

        /// <summary>
        /// Notify sub-systems of an in-flight event (e.g., airport visited).
        /// </summary>
        /// <param name="eventName">Human-readable event label.</param>
        /// <param name="eventData">Optional payload string.</param>
        public void LogEvent(string eventName, string eventData = null)
        {
            if (sessionTracker != null)
                sessionTracker.LogEvent(eventName, eventData);
        }
    }
}
