// MaritimePatrolController.cs — Phase 117: Advanced Ocean & Maritime System
// Patrol missions: area surveillance, vessel identification, illegal fishing.
// Namespace: SWEF.OceanSystem

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Manages maritime patrol missions.
    /// Generates patrol waypoints, tracks vessel contacts, and detects
    /// illegal fishing vessels for reporting.
    /// </summary>
    public class MaritimePatrolController : MonoBehaviour
    {
        // ── Patrol State ──────────────────────────────────────────────────────────

        /// <summary>State of a patrol mission.</summary>
        public enum PatrolState { Inactive, Patrolling, Investigating, Reporting, Complete }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Patrol Area")]
        [SerializeField] private Vector3 patrolCentre;
        [SerializeField] private float   patrolRadius = 10000f;
        [SerializeField] private int     patrolWaypointCount = 6;

        [Header("Detection")]
        [SerializeField] private float vesselIdentificationRadius = 500f;
        [SerializeField] private float illegalFishingProbability  = 0.2f;

        // ── Private state ─────────────────────────────────────────────────────────

        private PatrolState       _state = PatrolState.Inactive;
        private List<Vector3>     _patrolWaypoints;
        private int               _currentWaypoint;
        private readonly List<string> _identifiedVessels = new List<string>();
        private int               _illegalFishingFound;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a vessel is identified.</summary>
        public event Action<string, bool> OnVesselIdentified; // (vesselId, isIllegal)

        /// <summary>Raised when the patrol mission is complete.</summary>
        public event Action<int> OnPatrolComplete; // (illegalFishingCount)

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Current patrol state.</summary>
        public PatrolState State => _state;

        /// <summary>Number of illegal fishing vessels detected.</summary>
        public int IllegalFishingFound => _illegalFishingFound;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Starts the patrol mission.</summary>
        public void StartPatrol(Vector3 centre, float radius)
        {
            patrolCentre  = centre;
            patrolRadius  = radius;
            _state        = PatrolState.Patrolling;
            _patrolWaypoints = GeneratePatrolRoute();
            _currentWaypoint = 0;
            _illegalFishingFound = 0;
            _identifiedVessels.Clear();
        }

        /// <summary>Updates patrol progress. Call every frame with the aircraft position.</summary>
        public void Tick(Vector3 aircraftPosition)
        {
            if (_state != PatrolState.Patrolling) return;

            // Advance waypoint
            if (_patrolWaypoints != null && _currentWaypoint < _patrolWaypoints.Count)
            {
                var wp = _patrolWaypoints[_currentWaypoint];
                wp.y = aircraftPosition.y;
                if (Vector3.Distance(aircraftPosition, wp) < 300f)
                    _currentWaypoint++;

                if (_currentWaypoint >= _patrolWaypoints.Count)
                {
                    _state = PatrolState.Complete;
                    OnPatrolComplete?.Invoke(_illegalFishingFound);
                }
            }
        }

        /// <summary>
        /// Simulates identification of a vessel with the given ID when the aircraft
        /// is within identification range.
        /// </summary>
        public void AttemptVesselIdentification(string vesselId, Vector3 vesselPosition, Vector3 aircraftPosition)
        {
            if (_identifiedVessels.Contains(vesselId)) return;
            float dist = Vector3.Distance(aircraftPosition, vesselPosition);
            if (dist > vesselIdentificationRadius) return;

            _identifiedVessels.Add(vesselId);
            bool isIllegal = UnityEngine.Random.value < illegalFishingProbability;
            if (isIllegal) _illegalFishingFound++;
            OnVesselIdentified?.Invoke(vesselId, isIllegal);
        }

        // ── Pattern Generation ────────────────────────────────────────────────────

        private List<Vector3> GeneratePatrolRoute()
        {
            var wps = new List<Vector3>();
            for (int i = 0; i < patrolWaypointCount; i++)
            {
                float angle = i * (360f / patrolWaypointCount) * Mathf.Deg2Rad;
                float r     = patrolRadius * UnityEngine.Random.Range(0.7f, 1f);
                wps.Add(patrolCentre + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * r);
            }
            return wps;
        }
    }
}
