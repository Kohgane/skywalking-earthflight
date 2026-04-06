// CarrierLandingChallenge.cs — Phase 120: Precision Landing Challenge System
// Aircraft carrier landing: pitching deck, meatball tracking, wire engagement, LSO grading.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages an aircraft carrier landing challenge.
    /// Simulates pitching deck motion, IFLOLS meatball tracking, arresting wire
    /// engagement, and Landing Signal Officer (LSO) grade assignment.
    /// </summary>
    public class CarrierLandingChallenge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Carrier Deck Motion")]
        [SerializeField] private float pitchAmplitudeDeg = 2.5f;
        [SerializeField] private float pitchPeriodSeconds = 8f;
        [SerializeField] private float rollAmplitudeDeg = 1.5f;

        [Header("Glideslope")]
        [SerializeField] private float carrierGlideSlopeAngleDeg = 3.5f;
        [SerializeField] private float meatballSensitivity = 1.0f;

        [Header("Wires")]
        [SerializeField] private int targetWire = 3;
        [SerializeField] private float wireSpacingMetres = 12f;

        // ── State ─────────────────────────────────────────────────────────────

        private bool  _isActive;
        private float _deckPitch;
        private float _deckRoll;
        private int   _wireEngaged = -1;
        private LSOGrade _lsoGrade = LSOGrade.NoGrade;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current deck pitch angle in degrees.</summary>
        public float DeckPitchDeg => _deckPitch;

        /// <summary>Current deck roll angle in degrees.</summary>
        public float DeckRollDeg => _deckRoll;

        /// <summary>Wire number engaged at touchdown (-1 = bolter/no wire).</summary>
        public int WireEngaged => _wireEngaged;

        /// <summary>LSO grade awarded for this pass.</summary>
        public LSOGrade LSOGrade => _lsoGrade;

        /// <summary>Whether this challenge session is active.</summary>
        public bool IsActive => _isActive;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Activate the carrier challenge.</summary>
        public void Activate()
        {
            _isActive     = true;
            _wireEngaged  = -1;
            _lsoGrade     = LSOGrade.NoGrade;
        }

        /// <summary>Deactivate the carrier challenge.</summary>
        public void Deactivate() => _isActive = false;

        /// <summary>
        /// Compute IFLOLS meatball position.
        /// Positive value = above glideslope, negative = below.
        /// </summary>
        public float ComputeMeatball(float aircraftAltFt, float distanceNM)
        {
            float idealAltFt = distanceNM * 6076f * Mathf.Tan(carrierGlideSlopeAngleDeg * Mathf.Deg2Rad);
            return (aircraftAltFt - idealAltFt) * meatballSensitivity;
        }

        /// <summary>Record a touchdown event and determine wire engagement and LSO grade.</summary>
        public void ProcessTouchdown(TouchdownData td, float meatballAtCross)
        {
            // Wire engagement: determine which wire based on touchdown position
            float wireZone = td.Position.z % wireSpacingMetres;
            if (wireZone < wireSpacingMetres * 0.25f)       _wireEngaged = 1;
            else if (wireZone < wireSpacingMetres * 0.50f)  _wireEngaged = 2;
            else if (wireZone < wireSpacingMetres * 0.75f)  _wireEngaged = targetWire;
            else                                             _wireEngaged = 4;

            // LSO grading based on meatball tracking and approach quality
            float absMeat = Mathf.Abs(meatballAtCross);
            if (absMeat < 0.3f && _wireEngaged == targetWire)
                _lsoGrade = LSOGrade.OK;
            else if (absMeat < 0.6f && _wireEngaged >= 2 && _wireEngaged <= 4)
                _lsoGrade = LSOGrade.Fair;
            else if (_wireEngaged == -1)
                _lsoGrade = LSOGrade.WaveOff;
            else if (td.SinkRateFPM < -800f)
                _lsoGrade = LSOGrade.CutPass;
            else
                _lsoGrade = LSOGrade.NoGrade;
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (!_isActive) return;
            float t = Time.time;
            _deckPitch = pitchAmplitudeDeg * Mathf.Sin(2f * Mathf.PI * t / pitchPeriodSeconds);
            _deckRoll  = rollAmplitudeDeg  * Mathf.Sin(2f * Mathf.PI * t / pitchPeriodSeconds * 0.7f);
        }
    }
}
