// SearchAndRescueController.cs — Phase 117: Advanced Ocean & Maritime System
// SAR missions: expanding square, sector, parallel track patterns.
// Namespace: SWEF.OceanSystem

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Guides the player through a Search and Rescue (SAR) mission.
    /// Generates search-pattern waypoints, detects survivors within detection radius,
    /// and manages rescue hoist operations.
    /// </summary>
    public class SearchAndRescueController : MonoBehaviour
    {
        // ── SAR State ─────────────────────────────────────────────────────────────

        /// <summary>State of the SAR operation.</summary>
        public enum SARState { Idle, Searching, HoistingRescue, Complete }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Mission")]
        [SerializeField] private SARMissionData missionData;

        [Header("Detection")]
        [SerializeField] private float survivorDetectionRadius = 150f;
        [SerializeField] private float hoistOperationTime      = 30f;

        [Header("Pattern")]
        [SerializeField] private float trackSpacing = 400f;   // metres between lanes
        [SerializeField] private int   patternLegs  = 8;

        // ── Private state ─────────────────────────────────────────────────────────

        private SARState              _state = SARState.Idle;
        private List<Vector3>         _searchWaypoints;
        private int                   _currentWaypoint;
        private float                 _hoistTimer;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a survivor has been detected.</summary>
        public event Action<Vector3> OnSurvivorDetected;

        /// <summary>Raised when hoist rescue is complete for one survivor.</summary>
        public event Action OnRescueComplete;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Current SAR state.</summary>
        public SARState State => _state;

        /// <summary>Generated search pattern waypoints.</summary>
        public IReadOnlyList<Vector3> SearchWaypoints => _searchWaypoints;

        /// <summary>Index of current active waypoint.</summary>
        public int CurrentWaypointIndex => _currentWaypoint;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Initialises the controller with a mission and generates waypoints.</summary>
        public void BeginMission(SARMissionData mission)
        {
            missionData       = mission;
            _state            = SARState.Searching;
            _currentWaypoint  = 0;
            _searchWaypoints  = GeneratePattern(mission);
        }

        /// <summary>
        /// Call each frame with the aircraft position to check for survivor detection.
        /// </summary>
        public void Tick(Vector3 aircraftPosition)
        {
            if (_state == SARState.Idle || missionData == null) return;

            if (_state == SARState.HoistingRescue)
            {
                _hoistTimer -= Time.deltaTime;
                if (_hoistTimer <= 0f)
                {
                    var mgr = MaritimeMissionManager.Instance;
                    mgr?.RecordRescue(missionData.missionId);
                    OnRescueComplete?.Invoke();
                    _state = missionData.rescuedCount >= missionData.survivorCount
                           ? SARState.Complete
                           : SARState.Searching;
                }
                return;
            }

            // Advance waypoints
            if (_searchWaypoints != null && _currentWaypoint < _searchWaypoints.Count)
            {
                var wp  = _searchWaypoints[_currentWaypoint];
                wp.y    = aircraftPosition.y;
                if (Vector3.Distance(aircraftPosition, wp) < 200f)
                    _currentWaypoint++;
            }

            // Survivor proximity check
            float dist = Vector3.Distance(aircraftPosition, missionData.datumPosition);
            if (dist < survivorDetectionRadius)
            {
                OnSurvivorDetected?.Invoke(missionData.datumPosition);
                StartHoist();
            }
        }

        private void StartHoist()
        {
            _state      = SARState.HoistingRescue;
            _hoistTimer = hoistOperationTime;
        }

        // ── Pattern Generation ────────────────────────────────────────────────────

        private List<Vector3> GeneratePattern(SARMissionData mission)
        {
            return mission.searchPattern switch
            {
                SearchPattern.ExpandingSquare => GenerateExpandingSquare(mission.datumPosition),
                SearchPattern.Sector          => GenerateSector(mission.datumPosition),
                SearchPattern.ParallelTrack   => GenerateParallelTrack(mission.datumPosition),
                _ => GenerateExpandingSquare(mission.datumPosition)
            };
        }

        private List<Vector3> GenerateExpandingSquare(Vector3 datum)
        {
            var wps = new List<Vector3>();
            float legLength = trackSpacing;
            int   turns     = 0;

            var   pos       = datum;
            float heading   = 0f; // North

            for (int i = 0; i < patternLegs; i++)
            {
                if (i > 0 && i % 2 == 0) legLength += trackSpacing;
                heading += 90f;
                float rad = heading * Mathf.Deg2Rad;
                pos += new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * legLength;
                wps.Add(pos);
            }
            return wps;
        }

        private List<Vector3> GenerateSector(Vector3 datum)
        {
            var wps   = new List<Vector3>();
            float r   = trackSpacing * 2f;
            for (int i = 0; i < patternLegs; i++)
            {
                float angle = i * (360f / patternLegs) * Mathf.Deg2Rad;
                wps.Add(datum + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * r);
                wps.Add(datum);
            }
            return wps;
        }

        private List<Vector3> GenerateParallelTrack(Vector3 datum)
        {
            var wps = new List<Vector3>();
            float halfWidth = patternLegs * trackSpacing * 0.5f;
            for (int i = 0; i < patternLegs; i++)
            {
                float xOffset = -halfWidth + i * trackSpacing;
                wps.Add(datum + new Vector3(xOffset, 0f,  halfWidth));
                wps.Add(datum + new Vector3(xOffset, 0f, -halfWidth));
            }
            return wps;
        }
    }
}
