// ExtremeLandingChallenge.cs — Phase 120: Precision Landing Challenge System
// Extreme scenarios: ice runway, damaged aircraft, partial power, no instruments, turbulence.
// Namespace: SWEF.LandingChallenge

using System;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages extreme landing challenge scenarios.
    /// Combines multiple hazard types: contaminated runways, aircraft damage,
    /// partial power loss, instrument failures, and severe turbulence.
    /// </summary>
    public class ExtremeLandingChallenge : MonoBehaviour
    {
        // ── Hazard Flags ──────────────────────────────────────────────────────

        [Flags]
        public enum HazardFlags
        {
            None               = 0,
            IceRunway          = 1 << 0,
            DamagedAircraft    = 1 << 1,
            PartialPower       = 1 << 2,
            NoInstruments      = 1 << 3,
            SevereTurbulence   = 1 << 4,
            BirdStrike         = 1 << 5,
            HydraulicFailure   = 1 << 6,
            EngineOut          = 1 << 7
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Active Hazards")]
        [SerializeField] private HazardFlags activeHazards = HazardFlags.IceRunway;

        [Header("Ice Runway")]
        [SerializeField] private float iceBrakingFriction = 0.05f;

        [Header("Turbulence")]
        [SerializeField] private float turbulenceIntensity = 0.8f;
        [SerializeField] private float turbulenceFrequency = 2f;

        [Header("Power Settings")]
        [SerializeField] [Range(0f, 1f)] private float availablePowerFraction = 0.6f;

        // ── State ─────────────────────────────────────────────────────────────

        private bool  _isActive;
        private float _turbulenceOffset;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Currently active hazard combination.</summary>
        public HazardFlags ActiveHazards => activeHazards;

        /// <summary>Braking friction coefficient (reduced on ice).</summary>
        public float BrakingFriction =>
            (activeHazards & HazardFlags.IceRunway) != 0 ? iceBrakingFriction : 0.4f;

        /// <summary>Available thrust fraction (reduced on partial power / engine out).</summary>
        public float AvailablePowerFraction =>
            (activeHazards & (HazardFlags.PartialPower | HazardFlags.EngineOut)) != 0
                ? availablePowerFraction : 1f;

        /// <summary>Whether instruments are available.</summary>
        public bool InstrumentsAvailable =>
            (activeHazards & HazardFlags.NoInstruments) == 0;

        /// <summary>Whether this challenge is currently active.</summary>
        public bool IsActive => _isActive;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Activate the extreme challenge with the specified hazard combination.</summary>
        public void Activate(HazardFlags hazards)
        {
            activeHazards    = hazards;
            _isActive        = true;
            _turbulenceOffset = UnityEngine.Random.value * 100f;
        }

        /// <summary>Deactivate the challenge.</summary>
        public void Deactivate() => _isActive = false;

        /// <summary>
        /// Returns a turbulence force vector for the current frame.
        /// Non-zero only when <see cref="HazardFlags.SevereTurbulence"/> is active.
        /// </summary>
        public Vector3 GetTurbulenceForce()
        {
            if (!_isActive || (activeHazards & HazardFlags.SevereTurbulence) == 0)
                return Vector3.zero;

            float t = Time.time * turbulenceFrequency + _turbulenceOffset;
            return new Vector3(
                Mathf.PerlinNoise(t, 0f) - 0.5f,
                Mathf.PerlinNoise(t, 1f) - 0.5f,
                Mathf.PerlinNoise(t, 2f) - 0.5f
            ) * turbulenceIntensity * 2f;
        }

        /// <summary>
        /// Compute a difficulty multiplier based on active hazards.
        /// More hazards = higher multiplier for scoring bonus.
        /// </summary>
        public float GetDifficultyMultiplier()
        {
            int count = 0;
            var flags = (int)activeHazards;
            while (flags != 0) { count += flags & 1; flags >>= 1; }
            return 1f + count * 0.15f;
        }
    }
}
