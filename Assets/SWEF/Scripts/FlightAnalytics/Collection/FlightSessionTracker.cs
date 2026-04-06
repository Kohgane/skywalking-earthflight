// FlightSessionTracker.cs — Phase 116: Flight Analytics Dashboard
// Session management: start/end tracking, events, distance, airports.
// Namespace: SWEF.FlightAnalytics

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Manages the lifecycle of a single flight session: tracks start/end
    /// times, accumulates distance flown, records visited airports, and stores
    /// in-flight events for post-flight analysis.
    /// </summary>
    public class FlightSessionTracker : MonoBehaviour
    {
        // ── State ─────────────────────────────────────────────────────────────────

        private FlightSessionRecord _current;
        private Vector3 _lastPosition;
        private bool _active;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>The session currently being tracked (null when idle).</summary>
        public FlightSessionRecord CurrentSession => _current;

        /// <summary>Whether a session is actively being tracked.</summary>
        public bool IsActive => _active;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Begin a new session.</summary>
        public void BeginSession(string aircraftId, string departureIcao = null)
        {
            _current = new FlightSessionRecord
            {
                sessionId         = Guid.NewGuid().ToString("N"),
                startTimeUtc      = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                aircraftId        = aircraftId ?? "unknown",
                departureAirport  = departureIcao ?? string.Empty
            };

            if (!string.IsNullOrEmpty(departureIcao))
                _current.airportsVisited.Add(departureIcao);

            _lastPosition = Vector3.zero;
            _active = true;
            Debug.Log($"[SWEF] FlightSessionTracker: Session begun (id={_current.sessionId}).");
        }

        /// <summary>End the active session and return the completed record.</summary>
        public FlightSessionRecord EndSession(string arrivalIcao = null)
        {
            if (!_active || _current == null) return null;

            _current.endTimeUtc      = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _current.durationSeconds = (float)(_current.endTimeUtc - _current.startTimeUtc);
            _current.arrivalAirport  = arrivalIcao ?? string.Empty;

            if (!string.IsNullOrEmpty(arrivalIcao) && !_current.airportsVisited.Contains(arrivalIcao))
                _current.airportsVisited.Add(arrivalIcao);

            _active = false;
            Debug.Log($"[SWEF] FlightSessionTracker: Session ended (duration={_current.durationSeconds:F0}s, dist={_current.distanceNm:F1}nm).");
            return _current;
        }

        /// <summary>Record that the aircraft has moved to a new world position.</summary>
        public void UpdatePosition(Vector3 worldPos)
        {
            if (!_active || _current == null) return;

            if (_lastPosition != Vector3.zero)
            {
                float deltaM = Vector3.Distance(worldPos, _lastPosition);
                _current.distanceNm += deltaM / 1852f; // metres → nautical miles
            }

            _lastPosition = worldPos;
        }

        /// <summary>Mark an airport as visited in the current session.</summary>
        public void VisitAirport(string icao)
        {
            if (!_active || _current == null || string.IsNullOrEmpty(icao)) return;
            if (!_current.airportsVisited.Contains(icao))
                _current.airportsVisited.Add(icao);
        }

        /// <summary>Log a named in-flight event with optional payload.</summary>
        public void LogEvent(string eventName, string eventData = null)
        {
            if (!_active) return;
            Debug.Log($"[SWEF] FlightSessionTracker: Event '{eventName}' — {eventData}");
        }
    }
}
