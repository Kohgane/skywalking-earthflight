// LandingHUD.cs — Phase 120: Precision Landing Challenge System
// In-flight challenge HUD: PAPI/VASI indicators, glideslope diamond, touchdown zone marker, speed trend.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — In-flight HUD overlay for landing challenges.
    /// Provides PAPI/VASI light indications, a glideslope/localiser deviation diamond,
    /// a touchdown zone aiming marker, and speed-trend indication.
    /// </summary>
    public class LandingHUD : MonoBehaviour
    {
        // ── PAPI State ────────────────────────────────────────────────────────

        public enum PAPIIndication { TooHigh, SlightlyHigh, OnGlideslope, SlightlyLow, TooLow }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Glideslope")]
        [SerializeField] private float targetGlideSlopeAngleDeg = 3f;

        [Header("Speed")]
        [SerializeField] private float targetApproachSpeedKnots = 135f;
        [SerializeField] private float speedTrendWindowSec       = 3f;

        // ── State ─────────────────────────────────────────────────────────────

        private float         _currentGlideSlopeDots;
        private float         _currentLocDots;
        private float         _currentSpeedKnots;
        private float         _speedTrend;
        private float         _prevSpeed;
        private float         _speedTimer;
        private PAPIIndication _papiIndication;
        private bool          _isVisible;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current glideslope deviation in dots (positive = above).</summary>
        public float GlideSlopeDots => _currentGlideSlopeDots;

        /// <summary>Current localiser deviation in dots (positive = right).</summary>
        public float LocaliserDots => _currentLocDots;

        /// <summary>Current PAPI indication based on glideslope position.</summary>
        public PAPIIndication PAPI => _papiIndication;

        /// <summary>Speed trend (positive = accelerating, negative = decelerating).</summary>
        public float SpeedTrend => _speedTrend;

        /// <summary>Whether the HUD is currently visible.</summary>
        public bool IsVisible => _isVisible;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Show the landing HUD.</summary>
        public void Show() { _isVisible = true; gameObject.SetActive(true); }

        /// <summary>Hide the landing HUD.</summary>
        public void Hide() { _isVisible = false; gameObject.SetActive(false); }

        /// <summary>Update HUD state with current flight data.</summary>
        public void UpdateData(float glideSlopeDots, float locDots, float speedKnots,
                               float altFeetAGL, float distNM)
        {
            _currentGlideSlopeDots = glideSlopeDots;
            _currentLocDots        = locDots;
            _currentSpeedKnots     = speedKnots;

            // PAPI
            if      (glideSlopeDots >  1.5f)  _papiIndication = PAPIIndication.TooHigh;
            else if (glideSlopeDots >  0.5f)  _papiIndication = PAPIIndication.SlightlyHigh;
            else if (glideSlopeDots < -1.5f)  _papiIndication = PAPIIndication.TooLow;
            else if (glideSlopeDots < -0.5f)  _papiIndication = PAPIIndication.SlightlyLow;
            else                              _papiIndication = PAPIIndication.OnGlideslope;

            // Speed trend
            _speedTimer += Time.deltaTime;
            if (_speedTimer >= speedTrendWindowSec)
            {
                _speedTrend = (_currentSpeedKnots - _prevSpeed) / speedTrendWindowSec;
                _prevSpeed  = _currentSpeedKnots;
                _speedTimer = 0f;
            }
        }
    }
}
