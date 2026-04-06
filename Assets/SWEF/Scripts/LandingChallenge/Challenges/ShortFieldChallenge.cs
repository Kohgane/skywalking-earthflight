// ShortFieldChallenge.cs — Phase 120: Precision Landing Challenge System
// Short field operations: minimum runway length, obstacle clearance, maximum braking.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages a short-field landing challenge.
    /// Tracks obstacle clearance at 50 ft AGL, touchdown point precision,
    /// and stopping distance to assess maximum braking performance.
    /// </summary>
    public class ShortFieldChallenge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Runway")]
        [SerializeField] private float runwayLengthMetres = 600f;
        [SerializeField] private float safetyStopMarginMetres = 30f;

        [Header("Obstacle")]
        [SerializeField] private float obstacleHeightFeet = 50f;
        [SerializeField] private float obstacleDistanceMetres = 300f;

        [Header("Performance Targets")]
        [SerializeField] private float targetTouchdownMetres = 60f;
        [SerializeField] private float maxTouchdownSpeedKnots = 65f;

        // ── State ─────────────────────────────────────────────────────────────

        private bool  _isActive;
        private bool  _obstacleCleared;
        private float _stoppingDistanceMetres;
        private bool  _runwayExceeded;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Whether the 50 ft obstacle was cleared.</summary>
        public bool ObstacleCleared => _obstacleCleared;

        /// <summary>Stopping distance from touchdown point in metres.</summary>
        public float StoppingDistanceMetres => _stoppingDistanceMetres;

        /// <summary>Whether the aircraft ran off the end of the runway.</summary>
        public bool RunwayExceeded => _runwayExceeded;

        /// <summary>Whether this challenge is currently active.</summary>
        public bool IsActive => _isActive;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Activate the short-field challenge.</summary>
        public void Activate()
        {
            _isActive             = true;
            _obstacleCleared      = false;
            _stoppingDistanceMetres = 0f;
            _runwayExceeded       = false;
        }

        /// <summary>Deactivate the challenge.</summary>
        public void Deactivate() => _isActive = false;

        /// <summary>
        /// Check obstacle clearance during approach.
        /// </summary>
        public void CheckObstacleClearance(float altAGLFeet, float distanceFromRunwayMetres)
        {
            if (!_isActive) return;
            if (distanceFromRunwayMetres >= obstacleDistanceMetres && altAGLFeet >= obstacleHeightFeet)
                _obstacleCleared = true;
        }

        /// <summary>
        /// Record where the aircraft stopped on the runway.
        /// </summary>
        public void RecordStop(float touchdownMetres, float stopMetres)
        {
            if (!_isActive) return;
            _stoppingDistanceMetres = stopMetres - touchdownMetres;
            _runwayExceeded         = stopMetres > (runwayLengthMetres - safetyStopMarginMetres);
        }

        /// <summary>
        /// Calculate a short-field performance score (0–1).
        /// </summary>
        public float CalculatePerformanceScore(float touchdownMetres)
        {
            float tdScore   = Mathf.Clamp01(1f - Mathf.Abs(touchdownMetres - targetTouchdownMetres) / targetTouchdownMetres);
            float stopScore = Mathf.Clamp01(1f - _stoppingDistanceMetres / runwayLengthMetres);
            float obstScore = _obstacleCleared ? 1f : 0f;
            float runScore  = _runwayExceeded  ? 0f : 1f;
            return (tdScore * 0.3f + stopScore * 0.3f + obstScore * 0.2f + runScore * 0.2f);
        }
    }
}
