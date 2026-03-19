namespace SWEF.Flight
{
    /// <summary>
    /// Describes the current orbital regime of the vehicle.
    /// </summary>
    public enum OrbitState
    {
        /// <summary>Below the Kármán line (~100 km) — aerodynamic flight.</summary>
        Atmospheric,

        /// <summary>Above the Kármán line but below circular orbital velocity.</summary>
        SubOrbital,

        /// <summary>Speed ≥ circular orbital velocity and altitude &lt; 2,000 km.</summary>
        LowOrbit,

        /// <summary>Speed ≥ circular orbital velocity and altitude ≥ 2,000 km.</summary>
        HighOrbit,

        /// <summary>Speed ≥ escape velocity — leaving Earth's gravity well.</summary>
        Escape
    }
}
