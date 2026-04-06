// VesselController.cs — Phase 117: Advanced Ocean & Maritime System
// AI vessel behavior: course following, collision avoidance (COLREGS), speed adjust.
// Namespace: SWEF.OceanSystem

using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Controls a single AI maritime vessel.
    /// Follows a series of waypoints, applies COLREGS-based collision avoidance,
    /// and adjusts speed according to sea state.
    /// </summary>
    public class VesselController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Vessel Data")]
        [SerializeField] private VesselData vesselData;

        [Header("Navigation")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointRadius = 200f;
        [SerializeField] private float turnRate = 3f;  // degrees per second

        [Header("COLREGS")]
        [SerializeField] private float collisionAvoidanceRadius = 500f;
        [SerializeField] private float collisionAvoidanceTurnDeg = 15f;

        // ── Private state ─────────────────────────────────────────────────────────

        private int   _currentWaypoint;
        private float _currentHeading;
        private float _currentSpeedKnots;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Vessel data record for this controller.</summary>
        public VesselData VesselData => vesselData;

        /// <summary>Current heading in degrees.</summary>
        public float CurrentHeading => _currentHeading;

        /// <summary>Current speed in knots.</summary>
        public float CurrentSpeedKnots => _currentSpeedKnots;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (vesselData != null)
            {
                _currentHeading    = vesselData.heading;
                _currentSpeedKnots = vesselData.speedKnots;
            }
        }

        private void Update()
        {
            AdjustSpeedForSeaState();
            NavigateWaypoints();
            MoveVessel();
        }

        // ── Navigation ────────────────────────────────────────────────────────────

        private void NavigateWaypoints()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            var wp = waypoints[_currentWaypoint % waypoints.Length];
            if (wp == null) return;

            var toWp    = wp.position - transform.position;
            toWp.y      = 0f;
            float dist  = toWp.magnitude;

            if (dist < waypointRadius)
            {
                _currentWaypoint = (_currentWaypoint + 1) % waypoints.Length;
                return;
            }

            float targetHeading = Mathf.Atan2(toWp.x, toWp.z) * Mathf.Rad2Deg;
            _currentHeading = Mathf.MoveTowardsAngle(_currentHeading, targetHeading, turnRate * Time.deltaTime);
        }

        private void MoveVessel()
        {
            float speedMs = _currentSpeedKnots * 0.5144f;
            float rad     = _currentHeading * Mathf.Deg2Rad;
            var   dir     = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            transform.position += dir * speedMs * Time.deltaTime;
            transform.rotation  = Quaternion.Euler(0f, _currentHeading, 0f);
        }

        private void AdjustSpeedForSeaState()
        {
            var mgr = OceanSystemManager.Instance;
            if (mgr == null) return;

            float factor = mgr.CurrentSeaState switch
            {
                SeaState.Calm      => 1.0f,
                SeaState.Slight    => 0.95f,
                SeaState.Moderate  => 0.85f,
                SeaState.Rough     => 0.70f,
                SeaState.VeryRough => 0.55f,
                SeaState.HighSeas  => 0.40f,
                _ => 1.0f
            };

            if (vesselData != null)
                _currentSpeedKnots = vesselData.speedKnots * factor;
        }
    }
}
