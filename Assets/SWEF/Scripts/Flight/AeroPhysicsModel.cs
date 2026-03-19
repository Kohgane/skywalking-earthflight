using System;
using UnityEngine;

namespace SWEF.Flight
{
    /// <summary>
    /// Central aerodynamics model. Calculates atmospheric forces (drag, lift, gravity)
    /// using an exponential atmosphere and simplified aerodynamic coefficients.
    /// All calculations are lightweight and allocation-free for mobile performance.
    /// </summary>
    public class AeroPhysicsModel : MonoBehaviour
    {
        // ── Atmosphere constants ─────────────────────────────────────────────
        private const float RhoSL       = 1.225f;       // kg/m³, sea-level air density
        private const float ScaleHeight = 8500f;        // m, exponential scale height
        private const float KarmanLine  = 100000f;      // m, edge of atmosphere
        private const float TempSL      = 288.15f;      // K, sea-level temperature
        private const float TempLapse   = 0.0065f;      // K/m, temperature lapse rate (troposphere)
        private const float SoundSL     = 340f;         // m/s, sea-level speed of sound

        // ── Gravity constants ────────────────────────────────────────────────
        private const float G0      = 9.81f;            // m/s²
        private const float REarth  = 6_371_000f;       // m

        // ── Drag/Lift defaults ───────────────────────────────────────────────
        [Header("Drag")]
        [SerializeField] private float cd = 0.04f;      // drag coefficient
        [SerializeField] private float referenceArea = 12f; // m²

        [Header("Lift")]
        [SerializeField] private float clSlope  = 0.1f;   // lift slope per degree AoA
        [SerializeField] private float clMax    = 1.5f;   // max lift coefficient (stall)
        [SerializeField] private float stallAngle = 15f;  // degrees

        [Header("Thrust")]
        [SerializeField] private float maxThrustNewtons = 50000f; // N at sea level
        [SerializeField] private float rocketModeAltitude = 25000f; // m — above this, rocket mode
        [SerializeField] private float rocketModeMultiplier = 0.6f; // thrust fraction in rocket mode

        // ── Public event ─────────────────────────────────────────────────────
        /// <summary>
        /// Fired each physics tick with the current aerodynamic state.
        /// Subscribe to drive UI, effects, or gameplay logic.
        /// </summary>
        public event Action<AeroState> OnAeroStateUpdated;

        // ── Last computed state (accessible without subscribing) ─────────────
        /// <summary>The most recently computed aerodynamic state.</summary>
        public AeroState LastState { get; private set; }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns air density in kg/m³ using an exponential atmosphere model.
        /// Clamped to near-zero above the Kármán line.
        /// </summary>
        public float GetAirDensity(float altitudeMeters)
        {
            if (altitudeMeters >= KarmanLine) return 0f;
            return RhoSL * Mathf.Exp(-altitudeMeters / ScaleHeight);
        }

        /// <summary>
        /// Returns gravitational acceleration in m/s² at the given altitude.
        /// Uses the inverse-square law: g(h) = g₀ × (R/(R+h))².
        /// </summary>
        public float GetGravity(float altitudeMeters)
        {
            float r = REarth + altitudeMeters;
            return G0 * (REarth / r) * (REarth / r);
        }

        /// <summary>
        /// Calculates the drag force vector opposing the velocity direction.
        /// F_drag = 0.5 × ρ × v² × Cd × A
        /// </summary>
        public Vector3 CalculateDrag(Vector3 velocity, float altitude)
        {
            float rho = GetAirDensity(altitude);
            float v2  = velocity.sqrMagnitude;
            if (rho <= 0f || v2 <= 0f) return Vector3.zero;

            float dragMag = 0.5f * rho * v2 * cd * referenceArea;
            return -velocity.normalized * dragMag;
        }

        /// <summary>
        /// Calculates the lift force vector perpendicular to the velocity in the
        /// craft's lift plane. F_lift = 0.5 × ρ × v² × Cl × A.
        /// Cl is linear with AoA and clamped at ±Cl_max (stall).
        /// </summary>
        public Vector3 CalculateLift(Vector3 velocity, float altitude, float angleOfAttackDeg)
        {
            float rho = GetAirDensity(altitude);
            float v2  = velocity.sqrMagnitude;
            if (rho <= 0f || v2 <= 0f) return Vector3.zero;

            float cl = Mathf.Clamp(clSlope * angleOfAttackDeg, -clMax, clMax);
            float liftMag = 0.5f * rho * v2 * cl * referenceArea;

            // Lift is perpendicular to velocity and in the plane containing world-up.
            // Guard against velocity being nearly parallel to world-up to avoid zero-vector normalisation.
            Vector3 velNorm = velocity.normalized;
            Vector3 right   = Vector3.Cross(velNorm, Vector3.up);
            if (right.sqrMagnitude < 0.001f)
            {
                // Velocity is (anti-)parallel to up — use world-forward as fallback axis
                right = Vector3.Cross(velNorm, Vector3.forward);
            }
            Vector3 liftDir = Vector3.Cross(right.normalized, velNorm).normalized;
            return liftDir * liftMag;
        }

        /// <summary>
        /// Returns the maximum available thrust in Newtons scaled by air density.
        /// Above <see cref="rocketModeAltitude"/> a rocket-mode multiplier is applied
        /// so the craft can still accelerate toward space.
        /// </summary>
        public float GetMaxThrust(float altitude)
        {
            float rho    = GetAirDensity(altitude);
            float rhoRatio = rho / RhoSL;           // 0..1

            if (altitude >= rocketModeAltitude)
            {
                // Rocket mode: independent of air density, fixed fraction
                return maxThrustNewtons * rocketModeMultiplier;
            }

            // Jet mode: thrust scales with air density
            return maxThrustNewtons * rhoRatio;
        }

        /// <summary>
        /// Returns the approximate speed of sound in m/s at the given altitude.
        /// Uses a linear temperature-lapse model up to ~11 km (troposphere),
        /// constant in the stratosphere.
        /// </summary>
        public float GetSpeedOfSound(float altitude)
        {
            // Temperature decreases linearly in troposphere (0–11 km)
            float h = Mathf.Min(altitude, 11000f);
            float tempK = TempSL - TempLapse * h;
            // c = 340 * sqrt(T / T_SL)
            return SoundSL * Mathf.Sqrt(Mathf.Max(tempK, 216.65f) / TempSL);
        }

        /// <summary>
        /// Returns the current Mach number (speed / local speed of sound).
        /// </summary>
        public float GetMachNumber(float speed, float altitude)
        {
            float c = GetSpeedOfSound(altitude);
            return c > 0f ? speed / c : 0f;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Per-tick update — call this from FlightPhysicsIntegrator
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes and caches the current <see cref="AeroState"/> then fires
        /// <see cref="OnAeroStateUpdated"/>. Called by <see cref="FlightPhysicsIntegrator"/>
        /// each <c>FixedUpdate</c>.
        /// </summary>
        /// <param name="velocity">Current vehicle velocity (m/s, world space).</param>
        /// <param name="forwardDir">Vehicle forward direction (world space).</param>
        /// <param name="altitude">Altitude above sea level in metres.</param>
        public AeroState Evaluate(Vector3 velocity, Vector3 forwardDir, float altitude)
        {
            float speed   = velocity.magnitude;
            float rho     = GetAirDensity(altitude);
            float g       = GetGravity(altitude);
            float sos     = GetSpeedOfSound(altitude);
            float mach    = speed > 0f ? speed / sos : 0f;
            float dynQ    = 0.5f * rho * speed * speed;
            float aoa     = CalculateAoA(velocity, forwardDir);
            bool  inAtmo  = altitude < KarmanLine;

            var state = new AeroState(
                airDensity:     rho,
                gravity:        g,
                machNumber:     mach,
                dynamicPressure: dynQ,
                angleOfAttack:  aoa,
                isInAtmosphere: inAtmo,
                isSupersonic:   mach >= 1f,
                isHypersonic:   mach >= 5f,
                speedOfSound:   sos,
                altitudeMeters: altitude
            );

            LastState = state;
            OnAeroStateUpdated?.Invoke(state);
            return state;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the angle of attack in degrees: the angle between the velocity
        /// vector and the craft's forward direction.
        /// </summary>
        public static float CalculateAoA(Vector3 velocity, Vector3 forward)
        {
            if (velocity.sqrMagnitude < 0.001f) return 0f;
            return Vector3.Angle(velocity.normalized, forward);
        }
    }
}
