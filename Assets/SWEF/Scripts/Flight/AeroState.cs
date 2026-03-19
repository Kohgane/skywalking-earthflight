using System;

namespace SWEF.Flight
{
    /// <summary>
    /// Immutable snapshot of aerodynamic and atmospheric state for a single physics tick.
    /// Fired via <see cref="AeroPhysicsModel.OnAeroStateUpdated"/> each fixed-update.
    /// </summary>
    public readonly struct AeroState
    {
        /// <summary>Air density at the current altitude in kg/m³.</summary>
        public readonly float AirDensity;

        /// <summary>Gravitational acceleration at the current altitude in m/s².</summary>
        public readonly float Gravity;

        /// <summary>Current Mach number (speed / speed of sound at altitude).</summary>
        public readonly float MachNumber;

        /// <summary>Dynamic pressure q = 0.5 × ρ × v² in Pascals.</summary>
        public readonly float DynamicPressure;

        /// <summary>Angle of attack in degrees (velocity vs. forward vector).</summary>
        public readonly float AngleOfAttack;

        /// <summary>True when altitude is below the Kármán line (~100 km).</summary>
        public readonly bool IsInAtmosphere;

        /// <summary>True when Mach number ≥ 1 (supersonic flight).</summary>
        public readonly bool IsSupersonic;

        /// <summary>True when Mach number ≥ 5 (hypersonic flight).</summary>
        public readonly bool IsHypersonic;

        /// <summary>Speed of sound at the current altitude in m/s.</summary>
        public readonly float SpeedOfSound;

        /// <summary>Altitude above sea level in metres.</summary>
        public readonly float AltitudeMeters;

        /// <summary>
        /// Constructs a fully populated <see cref="AeroState"/>.
        /// </summary>
        public AeroState(
            float airDensity,
            float gravity,
            float machNumber,
            float dynamicPressure,
            float angleOfAttack,
            bool  isInAtmosphere,
            bool  isSupersonic,
            bool  isHypersonic,
            float speedOfSound,
            float altitudeMeters)
        {
            AirDensity      = airDensity;
            Gravity         = gravity;
            MachNumber      = machNumber;
            DynamicPressure = dynamicPressure;
            AngleOfAttack   = angleOfAttack;
            IsInAtmosphere  = isInAtmosphere;
            IsSupersonic    = isSupersonic;
            IsHypersonic    = isHypersonic;
            SpeedOfSound    = speedOfSound;
            AltitudeMeters  = altitudeMeters;
        }
    }
}
