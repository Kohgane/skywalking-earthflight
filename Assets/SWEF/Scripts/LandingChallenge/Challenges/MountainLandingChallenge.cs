// MountainLandingChallenge.cs — Phase 120: Precision Landing Challenge System
// Mountain/STOL strip landing: terrain obstacles, one-way approach, altitude effects.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages a mountain or STOL-strip landing challenge.
    /// Models terrain obstacle clearance, one-way approach corridors,
    /// go-around impossibility, and high-altitude density-altitude effects.
    /// </summary>
    public class MountainLandingChallenge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Airport Parameters")]
        [SerializeField] private float airportElevationFeet = 9383f;
        [SerializeField] private float runwayLengthMetres   = 527f;
        [SerializeField] private float runwaySlopeDeg       = 11.7f;
        [SerializeField] private bool  oneWayApproach       = true;

        [Header("Obstacle Clearance")]
        [SerializeField] private float obstacleHeightFeet = 8660f;
        [SerializeField] private float obstacleDistanceNM = 1.2f;

        [Header("Density Altitude")]
        [SerializeField] private float temperatureCelsius  = 10f;

        // ── State ─────────────────────────────────────────────────────────────

        private bool  _isActive;
        private bool  _obstacleCleared;
        private float _densityAltitudeFeet;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Computed density altitude in feet.</summary>
        public float DensityAltitudeFeet => _densityAltitudeFeet;

        /// <summary>Whether the terrain obstacle has been cleared.</summary>
        public bool ObstacleCleared => _obstacleCleared;

        /// <summary>Whether a go-around is possible (false at one-way strips).</summary>
        public bool GoAroundPossible => !oneWayApproach;

        /// <summary>Whether this challenge is currently active.</summary>
        public bool IsActive => _isActive;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Activate the mountain landing challenge.</summary>
        public void Activate()
        {
            _isActive        = true;
            _obstacleCleared = false;
            ComputeDensityAltitude();
        }

        /// <summary>Deactivate the challenge.</summary>
        public void Deactivate() => _isActive = false;

        /// <summary>
        /// Report current aircraft position/altitude for obstacle check.
        /// Call each frame during the approach.
        /// </summary>
        public void UpdateAircraftState(float altitudeFeet, float distanceFromRunwayNM)
        {
            if (!_isActive) return;
            if (distanceFromRunwayNM >= obstacleDistanceNM && altitudeFeet > obstacleHeightFeet)
                _obstacleCleared = true;
        }

        /// <summary>
        /// Returns the performance penalty factor (0–1) due to density altitude.
        /// Higher density altitude = lower factor = worse aircraft performance.
        /// </summary>
        public float GetPerformancePenalty()
        {
            float excess = Mathf.Max(0f, _densityAltitudeFeet - 5000f);
            return Mathf.Clamp01(1f - excess / 20000f);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void ComputeDensityAltitude()
        {
            // ISA standard: DA = PA + 120 * (OAT - ISA temperature)
            float isaTemp = 15f - 1.98f * (airportElevationFeet / 1000f);
            _densityAltitudeFeet = airportElevationFeet + 120f * (temperatureCelsius - isaTemp);
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (!_isActive) return;
            // Obstacle and approach corridor checks are driven externally via UpdateAircraftState
        }
    }
}
