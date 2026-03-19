using System;
using UnityEngine;

namespace SWEF.Flight
{
    /// <summary>
    /// Warning states produced by <see cref="StallWarningSystem"/>.
    /// </summary>
    public enum StallWarningState
    {
        /// <summary>All parameters within safe limits.</summary>
        None,
        /// <summary>Angle of attack is approaching the stall angle.</summary>
        StallImminent,
        /// <summary>Stall conditions met (high AoA at low speed).</summary>
        Stalling,
        /// <summary>Dynamic pressure exceeds the structural limit.</summary>
        Overspeed,
        /// <summary>G-force is outside the safe envelope.</summary>
        ExcessiveGForce
    }

    /// <summary>
    /// Monitors angle of attack, dynamic pressure, and G-force to detect dangerous
    /// flight conditions and fire warning events.
    /// </summary>
    public class StallWarningSystem : MonoBehaviour
    {
        [Header("Stall")]
        [Tooltip("AoA threshold in degrees for stall-imminent warning.")]
        [SerializeField] private float stallImminent    = 12f;   // deg
        [Tooltip("AoA threshold in degrees for full stall.")]
        [SerializeField] private float stallFull        = 15f;   // deg
        [Tooltip("Minimum speed at sea level (m/s) below which stall is considered.")]
        [SerializeField] private float stallSpeedSL     = 30f;   // m/s
        [Tooltip("Sea-level air density used to scale stall speed with altitude.")]
        private const float RhoSL = 1.225f;

        [Header("Overspeed")]
        [Tooltip("Structural dynamic-pressure limit in Pascals.")]
        [SerializeField] private float maxDynamicPressure = 50000f; // Pa

        [Header("G-Force")]
        [Tooltip("Maximum positive G before warning.")]
        [SerializeField] private float maxPositiveG = 4f;
        [Tooltip("Maximum negative G before warning.")]
        [SerializeField] private float maxNegativeG = -1f;

        // ── State ────────────────────────────────────────────────────────────
        private StallWarningState _currentState = StallWarningState.None;

        /// <summary>Most recently determined warning state.</summary>
        public StallWarningState CurrentState => _currentState;

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Fired when stall conditions are first met.</summary>
        public event Action OnStallWarning;

        /// <summary>Fired when dynamic pressure exceeds the structural limit.</summary>
        public event Action OnOverspeedWarning;

        /// <summary>Fired when G-force exits the safe envelope.</summary>
        public event Action OnGForceWarning;

        /// <summary>Fired whenever the warning state changes.</summary>
        public event Action<StallWarningState> OnWarningStateChanged;

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the current flight parameters and updates warning state.
        /// Call this each FixedUpdate from <see cref="FlightPhysicsIntegrator"/>.
        /// </summary>
        /// <param name="aero">Current aerodynamic state.</param>
        /// <param name="gForce">Experienced G-force (net accel / 9.81).</param>
        /// <param name="speed">Current scalar speed in m/s (avoids redundant sqrt).</param>
        public void Evaluate(in AeroState aero, float gForce, float speed = -1f)
        {
            StallWarningState newState = DetermineState(aero, gForce, speed);

            if (newState == _currentState) return;

            _currentState = newState;

            // Fire specific events when crossing into a danger state
            if (newState == StallWarningState.Stalling)
                OnStallWarning?.Invoke();
            else if (newState == StallWarningState.Overspeed)
                OnOverspeedWarning?.Invoke();
            else if (newState == StallWarningState.ExcessiveGForce)
                OnGForceWarning?.Invoke();

            OnWarningStateChanged?.Invoke(_currentState);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private StallWarningState DetermineState(in AeroState aero, float gForce, float speed)
        {
            // G-force overrides everything (highest priority)
            if (gForce > maxPositiveG || gForce < maxNegativeG)
                return StallWarningState.ExcessiveGForce;

            // Overspeed (structural)
            if (aero.DynamicPressure > maxDynamicPressure)
                return StallWarningState.Overspeed;

            // Stall checks — only relevant inside the atmosphere
            if (aero.IsInAtmosphere && aero.AirDensity > 0f)
            {
                float stallSpd = GetStallSpeed(aero.AirDensity);
                // Use provided speed if valid, otherwise derive from dynamic pressure
                float currentSpd = speed >= 0f
                    ? speed
                    : Mathf.Sqrt(2f * aero.DynamicPressure / aero.AirDensity);

                bool highAoA  = aero.AngleOfAttack >= stallFull;
                bool lowSpeed = currentSpd < stallSpd;

                if (highAoA && lowSpeed)
                    return StallWarningState.Stalling;

                if (aero.AngleOfAttack >= stallImminent && lowSpeed)
                    return StallWarningState.StallImminent;
            }

            return StallWarningState.None;
        }

        /// <summary>
        /// Scales sea-level stall speed by density altitude:
        /// v_stall(h) = v_stall_SL × sqrt(ρ_SL / ρ(h)).
        /// Returns sea-level value when density is near zero to avoid division by zero.
        /// </summary>
        private float GetStallSpeed(float airDensity)
        {
            if (airDensity <= 0.001f) return stallSpeedSL;
            return stallSpeedSL * Mathf.Sqrt(RhoSL / airDensity);
        }
    }
}
