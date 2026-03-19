using UnityEngine;

namespace SWEF.Flight
{
    /// <summary>
    /// Immutable data snapshot emitted by <see cref="FlightPhysicsIntegrator"/> every physics tick.
    /// Used for HUD display and telemetry.
    /// </summary>
    public readonly struct FlightPhysicsSnapshot
    {
        /// <summary>Full aerodynamic state (density, Mach, AoA, etc.).</summary>
        public readonly AeroState Aero;

        /// <summary>Current orbital regime.</summary>
        public readonly OrbitState Orbit;

        /// <summary>Net force vector acting on the vehicle in Newtons (world space).</summary>
        public readonly Vector3 NetForce;

        /// <summary>Current velocity vector in m/s (world space).</summary>
        public readonly Vector3 Velocity;

        /// <summary>Thrust as a fraction of maximum available thrust (0–1).</summary>
        public readonly float ThrustPercent;

        /// <summary>Lift force divided by weight (L/W ratio).</summary>
        public readonly float LiftToWeightRatio;

        /// <summary>Experienced G-force (net acceleration / g₀).</summary>
        public readonly float GForce;

        /// <summary>
        /// Constructs a fully populated <see cref="FlightPhysicsSnapshot"/>.
        /// </summary>
        public FlightPhysicsSnapshot(
            AeroState   aero,
            OrbitState  orbit,
            Vector3     netForce,
            Vector3     velocity,
            float       thrustPercent,
            float       liftToWeightRatio,
            float       gForce)
        {
            Aero              = aero;
            Orbit             = orbit;
            NetForce          = netForce;
            Velocity          = velocity;
            ThrustPercent     = thrustPercent;
            LiftToWeightRatio = liftToWeightRatio;
            GForce            = gForce;
        }
    }
}
