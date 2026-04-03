// AircraftPartType.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
namespace SWEF.Workshop
{
    /// <summary>
    /// Categorises every discrete customisable component that can be equipped
    /// to an aircraft build in the Workshop system.
    /// </summary>
    public enum AircraftPartType
    {
        /// <summary>Propulsion engine — primary thrust source.</summary>
        Engine,

        /// <summary>Main lifting surfaces (left and right wing combined slot).</summary>
        Wing,

        /// <summary>Central body structure that ties all parts together.</summary>
        Fuselage,

        /// <summary>Horizontal/vertical stabiliser assembly at the rear.</summary>
        Tail,

        /// <summary>Retractable or fixed gear for ground operations.</summary>
        LandingGear,

        /// <summary>Roll-control surfaces mounted on the wings.</summary>
        Aileron,

        /// <summary>Yaw-control surface on the vertical stabiliser.</summary>
        Rudder,

        /// <summary>Pitch-control surface on the horizontal stabiliser.</summary>
        Elevator,

        /// <summary>Forward pressure vessel housing avionics and the pilot.</summary>
        Cockpit,

        /// <summary>Rotating propeller blade assembly (piston / turboprop aircraft).</summary>
        Propeller,

        /// <summary>Air intake duct feeding the engine compressor.</summary>
        Intake,

        /// <summary>Exhaust nozzle or muffler at the engine outlet.</summary>
        Exhaust,

        /// <summary>Supplemental or replacement fuel reservoir.</summary>
        FuelTank
    }
}
