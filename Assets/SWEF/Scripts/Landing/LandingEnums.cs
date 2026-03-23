// LandingEnums.cs — SWEF Landing & Airport System (Phase 68)

namespace SWEF.Landing
{
    /// <summary>State machine states for the landing sequence.</summary>
    public enum LandingState
    {
        /// <summary>Normal cruise; not in approach context.</summary>
        InFlight,
        /// <summary>Within approach range of an airport.</summary>
        Approaching,
        /// <summary>Aligned with runway below decision altitude.</summary>
        OnFinal,
        /// <summary>Flare manoeuvre active; below flare altitude.</summary>
        Flaring,
        /// <summary>Main gear has contacted the runway surface.</summary>
        Touchdown,
        /// <summary>Rolling to a stop on the runway.</summary>
        Rolling,
        /// <summary>Aircraft has come to a full stop.</summary>
        Stopped,
        /// <summary>Taxiing to or from a stand/gate.</summary>
        Taxiing,
        /// <summary>Approach aborted; executing go-around.</summary>
        Aborted
    }

    /// <summary>State machine states for landing gear.</summary>
    public enum GearState
    {
        /// <summary>Gear fully retracted into the wheel wells.</summary>
        Retracted,
        /// <summary>Gear in the process of extending.</summary>
        Deploying,
        /// <summary>Gear fully extended and locked down.</summary>
        Deployed,
        /// <summary>Gear in the process of retracting.</summary>
        Retracting,
        /// <summary>Gear sustained damage and may not operate correctly.</summary>
        Damaged
    }

    /// <summary>Type of instrument or visual approach in use.</summary>
    public enum ApproachType
    {
        /// <summary>Pilot navigates visually with no electronic guidance.</summary>
        Visual,
        /// <summary>Instrument Landing System — localizer and glide slope.</summary>
        ILS,
        /// <summary>GPS-based approach with VNAV guidance.</summary>
        GPS,
        /// <summary>Visual circle to align with a specific runway end.</summary>
        CircleToLand
    }

    /// <summary>Surface condition of a runway.</summary>
    public enum RunwayCondition
    {
        /// <summary>Dry, clean pavement — best braking action.</summary>
        Dry,
        /// <summary>Wet surface — reduced friction.</summary>
        Wet,
        /// <summary>Ice-covered — severely reduced braking.</summary>
        Icy,
        /// <summary>Snow on the runway — variable braking action.</summary>
        Snow,
        /// <summary>Standing water — hydroplaning risk.</summary>
        Flooded
    }

    /// <summary>Classification of an airport by size and traffic capacity.</summary>
    public enum AirportSize
    {
        /// <summary>Small general aviation field.</summary>
        Small,
        /// <summary>Regional airport with limited commercial service.</summary>
        Medium,
        /// <summary>Major domestic / cargo airport.</summary>
        Large,
        /// <summary>International gateway with full services.</summary>
        International
    }
}
