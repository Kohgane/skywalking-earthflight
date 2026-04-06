// CrosswindLandingChallenge.cs — Phase 120: Precision Landing Challenge System
// Crosswind challenges: progressive wind strength, gusting, wind shear on short final.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages a crosswind landing challenge.
    /// Supports progressive wind buildup, gusty conditions, and wind shear
    /// events on short final.  Integrates with <see cref="ChallengeWeatherController"/>.
    /// </summary>
    public class CrosswindLandingChallenge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Wind Parameters")]
        [SerializeField] private float baseWindKnots      = 15f;
        [SerializeField] private float maxWindKnots       = 35f;
        [SerializeField] private float windDirectionDeg   = 270f;
        [SerializeField] private float runwayHeadingDeg   = 180f;

        [Header("Gusts")]
        [SerializeField] private bool  enableGusts        = true;
        [SerializeField] private float gustAmplitudeKnots = 10f;
        [SerializeField] private float gustPeriodSeconds  = 5f;

        [Header("Wind Shear")]
        [SerializeField] private bool  enableWindShear    = true;
        [SerializeField] private float shearAltitudeAGL   = 300f;
        [SerializeField] private float shearDropKnots     = 15f;

        // ── State ─────────────────────────────────────────────────────────────

        private bool  _isActive;
        private float _currentWindKnots;
        private float _crosswindComponent;
        private bool  _shearTriggered;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current total wind speed in knots.</summary>
        public float CurrentWindKnots => _currentWindKnots;

        /// <summary>Crosswind component relative to runway heading (knots).</summary>
        public float CrosswindComponent => _crosswindComponent;

        /// <summary>Whether a wind shear event has been triggered.</summary>
        public bool ShearTriggered => _shearTriggered;

        /// <summary>Whether the challenge is active.</summary>
        public bool IsActive => _isActive;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Activate the crosswind challenge.</summary>
        public void Activate()
        {
            _isActive       = true;
            _shearTriggered = false;
            _currentWindKnots = baseWindKnots;
            UpdateCrosswind();
        }

        /// <summary>Deactivate the challenge.</summary>
        public void Deactivate() => _isActive = false;

        /// <summary>
        /// Report aircraft AGL altitude to trigger wind shear if applicable.
        /// </summary>
        public void UpdateAircraftAltitude(float aglFeet)
        {
            if (!_isActive || !enableWindShear || _shearTriggered) return;
            if (aglFeet <= shearAltitudeAGL)
            {
                _currentWindKnots = Mathf.Max(0f, _currentWindKnots - shearDropKnots);
                _shearTriggered   = true;
                UpdateCrosswind();
                Debug.Log("[CrosswindLandingChallenge] Wind shear event triggered.");
            }
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (!_isActive) return;

            float progressive = Mathf.Lerp(baseWindKnots, maxWindKnots, Time.time / 300f);
            _currentWindKnots = Mathf.Clamp(progressive, baseWindKnots, maxWindKnots);

            if (enableGusts)
            {
                float gust = gustAmplitudeKnots * Mathf.PerlinNoise(Time.time / gustPeriodSeconds, 0f);
                _currentWindKnots = Mathf.Clamp(_currentWindKnots + gust, 0f, maxWindKnots + gustAmplitudeKnots);
            }

            UpdateCrosswind();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void UpdateCrosswind()
        {
            float angleDiff = windDirectionDeg - runwayHeadingDeg;
            _crosswindComponent = _currentWindKnots * Mathf.Sin(angleDiff * Mathf.Deg2Rad);
        }
    }
}
