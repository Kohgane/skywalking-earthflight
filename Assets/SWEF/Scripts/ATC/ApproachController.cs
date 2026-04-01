using System;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_LANDING_AVAILABLE
using SWEF.Landing;
#endif

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 78 — Generates and tracks approach and departure procedures
    /// for the active runway.
    ///
    /// <para>Produces standard circuit waypoints (downwind → base → final)
    /// for arrivals and simplified SID waypoints for departures.</para>
    ///
    /// <para>Integrates with <c>SWEF.Landing.ApproachGuidance</c> (null-safe,
    /// <c>#define SWEF_LANDING_AVAILABLE</c>) for ILS deviation overlay.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ApproachController : MonoBehaviour
    {
        #region Inspector

        [Header("Circuit Parameters")]
        [Tooltip("Pattern altitude above aerodrome elevation in feet.")]
        [SerializeField] private float patternAltitudeFt = 1000f;

        [Tooltip("Distance from runway threshold to the downwind leg in metres.")]
        [SerializeField] private float downwindDistanceM = 1852f;  // 1 nm

        [Tooltip("Distance from runway threshold to the base-turn fix in metres.")]
        [SerializeField] private float baseDistanceM = 926f;   // 0.5 nm

        [Tooltip("Distance from runway threshold to the final fix in metres.")]
        [SerializeField] private float finalDistanceM = 5556f;  // 3 nm

        #endregion

        #region Public Properties

        /// <summary>Current approach waypoints in sequence (downwind, base, final, threshold).</summary>
        public IReadOnlyList<Vector3> ApproachWaypoints => _approachWaypoints;

        /// <summary>Current departure waypoints in sequence.</summary>
        public IReadOnlyList<Vector3> DepartureWaypoints => _departureWaypoints;

        /// <summary>The runway for which an approach/departure is currently active.</summary>
        public RunwayInfo ActiveRunway { get; private set; }

        #endregion

        #region Private State

        private readonly List<Vector3> _approachWaypoints  = new List<Vector3>();
        private readonly List<Vector3> _departureWaypoints = new List<Vector3>();
        private Transform _playerTransform;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _playerTransform = Camera.main != null ? Camera.main.transform : transform;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Generates standard circuit waypoints for an approach to the specified runway.
        /// </summary>
        /// <param name="runway">The target runway.</param>
        /// <returns>List of world-space waypoints: downwind, base, final, threshold.</returns>
        public List<Vector3> InitiateApproach(RunwayInfo runway)
        {
            if (runway == null) return new List<Vector3>();

            ActiveRunway = runway;
            _approachWaypoints.Clear();

            float hdgRad    = runway.heading * Mathf.Deg2Rad;
            float perpRad   = (runway.heading + 90f) * Mathf.Deg2Rad;   // left-hand circuit
            Vector3 thr     = runway.position;

            // Downwind: parallel to runway, offset laterally
            Vector3 downwind = thr
                + new Vector3(Mathf.Sin(perpRad),  0f, Mathf.Cos(perpRad))  * downwindDistanceM
                + new Vector3(Mathf.Sin(hdgRad),   0f, Mathf.Cos(hdgRad))   * baseDistanceM;
            downwind.y = patternAltitudeFt;

            // Base: perpendicular turn inbound
            Vector3 baseFix = thr
                + new Vector3(Mathf.Sin(perpRad),  0f, Mathf.Cos(perpRad))  * downwindDistanceM;
            baseFix.y = patternAltitudeFt * 0.75f;

            // Final: straight-in aligned with runway
            Vector3 final = thr
                - new Vector3(Mathf.Sin(hdgRad), 0f, Mathf.Cos(hdgRad)) * finalDistanceM;
            final.y = patternAltitudeFt * 0.4f;

            _approachWaypoints.Add(downwind);
            _approachWaypoints.Add(baseFix);
            _approachWaypoints.Add(final);
            _approachWaypoints.Add(thr);

#if SWEF_LANDING_AVAILABLE
            ActivateILSOverlay(runway);
#endif
            return new List<Vector3>(_approachWaypoints);
        }

        /// <summary>
        /// Generates SID-style departure waypoints from the specified runway.
        /// </summary>
        /// <param name="runway">The departure runway.</param>
        /// <returns>List of world-space departure waypoints.</returns>
        public List<Vector3> InitiateDeparture(RunwayInfo runway)
        {
            if (runway == null) return new List<Vector3>();

            ActiveRunway = runway;
            _departureWaypoints.Clear();

            float hdgRad = runway.heading * Mathf.Deg2Rad;
            Vector3 dep  = runway.position;

            // Simple straight-out departure: 3 waypoints at increasing distance/altitude
            for (int i = 1; i <= 3; i++)
            {
                float dist = 3704f * i;  // 2 nm increments
                var wp = dep + new Vector3(Mathf.Sin(hdgRad), 0f, Mathf.Cos(hdgRad)) * dist;
                wp.y = patternAltitudeFt * i;
                _departureWaypoints.Add(wp);
            }

            return new List<Vector3>(_departureWaypoints);
        }

        /// <summary>
        /// Returns normalised (0–1) approach progress where 1 = over the threshold.
        /// </summary>
        public float GetApproachProgress01()
        {
            if (_approachWaypoints.Count == 0 || _playerTransform == null) return 0f;
            Vector3 final = _approachWaypoints[_approachWaypoints.Count - 1];
            Vector3 first = _approachWaypoints[0];
            float totalDist = Vector3.Distance(first, final);
            if (totalDist < 0.01f) return 1f;
            float remaining = Vector3.Distance(_playerTransform.position, final);
            return Mathf.Clamp01(1f - remaining / totalDist);
        }

        /// <summary>Returns true if the player is within glidepath tolerance (±200 ft / ±1°).</summary>
        public bool IsOnGlidepath()
        {
            if (ActiveRunway == null || _playerTransform == null) return false;
            float deviation = Mathf.Abs(GetDeviationFromCenterline());
            float altDev = Mathf.Abs(_playerTransform.position.y -
                GetTargetAltitudeAtCurrentDistance());
            return deviation < 1f && altDev < 200f;
        }

        /// <summary>Returns lateral deviation from runway centreline in degrees (negative = left).</summary>
        public float GetDeviationFromCenterline()
        {
            if (ActiveRunway == null || _playerTransform == null) return 0f;
            Vector3 toPlayer = _playerTransform.position - ActiveRunway.position;
            float bearing = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(ActiveRunway.heading, bearing);
        }

        #endregion

        #region Helpers

        private float GetTargetAltitudeAtCurrentDistance()
        {
            if (ActiveRunway == null || _playerTransform == null) return 0f;
            float dist = Vector3.Distance(_playerTransform.position, ActiveRunway.position);
            float glideAngleDeg = 3f;
            return dist * Mathf.Tan(glideAngleDeg * Mathf.Deg2Rad) * 3.28084f;  // metres → feet
        }

#if SWEF_LANDING_AVAILABLE
        private void ActivateILSOverlay(RunwayInfo runway)
        {
            var guidance = FindFirstObjectByType<ApproachGuidance>();
            if (guidance == null) return;
            guidance.SetTargetRunway(runway.position, runway.heading);
        }
#endif

        #endregion
    }
}
