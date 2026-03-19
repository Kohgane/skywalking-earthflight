using System;
using UnityEngine;

namespace SWEF.Flight
{
    /// <summary>
    /// Handles sub-orbital and orbital flight mechanics above the Kármán line (~100 km).
    /// Calculates orbital/escape velocities, determines orbit regime, and provides
    /// simplified 2-body gravitational acceleration.
    /// </summary>
    public class OrbitalMechanics : MonoBehaviour
    {
        // ── Physical constants ───────────────────────────────────────────────
        private const double GM      = 3.986e14;        // m³/s², Earth gravitational parameter
        private const float  REarth  = 6_371_000f;      // m
        private const float  KarmanLine = 100_000f;     // m
        private const float  LowOrbitMaxAlt = 2_000_000f; // m (2,000 km threshold)
        private const float  G0      = 9.81f;           // m/s²

        // ── State ────────────────────────────────────────────────────────────
        private OrbitState _currentOrbitState = OrbitState.Atmospheric;

        /// <summary>Current orbital regime based on altitude and speed.</summary>
        public OrbitState CurrentOrbitState => _currentOrbitState;

        /// <summary>Circular orbital speed at the current altitude in m/s.</summary>
        public float OrbitalVelocityAtCurrentAlt { get; private set; }

        /// <summary>Escape speed at the current altitude in m/s.</summary>
        public float EscapeVelocityAtCurrentAlt { get; private set; }

        /// <summary>Estimated orbit apoapsis in metres above sea level. 0 when not in orbit.</summary>
        public float Apoapsis { get; private set; }

        /// <summary>Estimated orbit periapsis in metres above sea level. 0 when not in orbit.</summary>
        public float Periapsis { get; private set; }

        /// <summary>
        /// Fired whenever <see cref="CurrentOrbitState"/> changes.
        /// </summary>
        public event Action<OrbitState> OnOrbitStateChanged;

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Calculates the circular orbital velocity at the given altitude.
        /// v_orbital = √(GM / r)
        /// </summary>
        public float GetOrbitalVelocity(float altitudeMeters)
        {
            double r = REarth + altitudeMeters;
            return (float)Math.Sqrt(GM / r);
        }

        /// <summary>
        /// Calculates the escape velocity at the given altitude.
        /// v_escape = √(2 × GM / r)
        /// </summary>
        public float GetEscapeVelocity(float altitudeMeters)
        {
            double r = REarth + altitudeMeters;
            return (float)Math.Sqrt(2.0 * GM / r);
        }

        /// <summary>
        /// Returns the gravitational acceleration vector pointing toward Earth's centre
        /// for use in orbital integration.
        /// g(h) = g₀ × (R / (R + h))² — same formula as <see cref="AeroPhysicsModel.GetGravity"/>
        /// but returned as a world-space vector (assumed down = -Y for the game world).
        /// </summary>
        public Vector3 GetGravitationalAcceleration(float altitudeMeters)
        {
            float r    = REarth + altitudeMeters;
            float gMag = G0 * (REarth / r) * (REarth / r);
            return Vector3.down * gMag;
        }

        /// <summary>
        /// Updates orbit state properties and fires <see cref="OnOrbitStateChanged"/>
        /// if the regime has changed. Called each FixedUpdate by
        /// <see cref="FlightPhysicsIntegrator"/>.
        /// </summary>
        /// <param name="altitudeMeters">Current altitude above sea level in metres.</param>
        /// <param name="speed">Current scalar speed in m/s.</param>
        public void Evaluate(float altitudeMeters, float speed)
        {
            float vOrbit  = GetOrbitalVelocity(altitudeMeters);
            float vEscape = GetEscapeVelocity(altitudeMeters);

            OrbitalVelocityAtCurrentAlt = vOrbit;
            EscapeVelocityAtCurrentAlt  = vEscape;

            OrbitState newState = DetermineOrbitState(altitudeMeters, speed, vOrbit, vEscape);

            // Estimate simple apoapsis/periapsis when in orbit (vis-viva approximation)
            if (newState == OrbitState.LowOrbit || newState == OrbitState.HighOrbit)
            {
                // Vis-viva: a = -GM / (v² - 2GM/r)
                double r  = REarth + altitudeMeters;
                double v2 = (double)speed * speed;
                double denom = v2 - 2.0 * GM / r;
                if (Math.Abs(denom) > 1.0)
                {
                    double sma = -GM / denom;                   // semi-major axis (metres from Earth centre)
                    // For an ellipse the apoapsis and periapsis radii are:
                    //   r_apo  = sma * (1 + e),  r_peri = sma * (1 - e)
                    // In the simplified 2-body view we know current radius = r.
                    // Assuming current position is close to periapsis (typical for low-speed orbit entry):
                    //   r_apo = 2 * sma - r
                    double rApo  = 2.0 * sma - r;
                    double rPeri = r;                           // periapsis ≈ current radius
                    Apoapsis  = (float)Math.Max(rApo  - REarth, altitudeMeters);
                    Periapsis = (float)Math.Max(rPeri - REarth, 0.0);
                }
            }
            else
            {
                Apoapsis  = 0f;
                Periapsis = 0f;
            }

            if (newState != _currentOrbitState)
            {
                _currentOrbitState = newState;
                OnOrbitStateChanged?.Invoke(_currentOrbitState);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static OrbitState DetermineOrbitState(
            float alt, float speed, float vOrbit, float vEscape)
        {
            if (alt < KarmanLine)
                return OrbitState.Atmospheric;

            if (speed >= vEscape)
                return OrbitState.Escape;

            if (speed >= vOrbit)
                return alt < LowOrbitMaxAlt ? OrbitState.LowOrbit : OrbitState.HighOrbit;

            return OrbitState.SubOrbital;
        }
    }
}
