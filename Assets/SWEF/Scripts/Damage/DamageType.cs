// DamageType.cs — SWEF Damage & Repair System (Phase 66)
namespace SWEF.Damage
{
    /// <summary>Identifies the origin of a damage event.</summary>
    public enum DamageSource
    {
        /// <summary>Physical collision with terrain or another object.</summary>
        Collision,
        /// <summary>Hit by a projectile.</summary>
        Projectile,
        /// <summary>Environmental hazard (turbulence, icing, etc.).</summary>
        Environment,
        /// <summary>Aircraft exceeded its maximum operating speed.</summary>
        Overspeed,
        /// <summary>Aircraft exceeded its maximum G-force limit.</summary>
        OverG,
        /// <summary>Lightning strike.</summary>
        Lightning,
        /// <summary>Bird-strike event.</summary>
        BirdStrike
    }

    /// <summary>Severity tier of damage applied to an aircraft part.</summary>
    public enum DamageLevel
    {
        /// <summary>No damage — part is fully healthy (&gt;90 %).</summary>
        None,
        /// <summary>Minor damage (70–90 % health).</summary>
        Minor,
        /// <summary>Moderate damage (50–70 % health).</summary>
        Moderate,
        /// <summary>Severe damage (25–50 % health).</summary>
        Severe,
        /// <summary>Critical damage (&gt;0–25 % health) — part barely functional.</summary>
        Critical,
        /// <summary>Part is completely destroyed (0 % health).</summary>
        Destroyed
    }

    /// <summary>Discrete structural parts of the aircraft that can be damaged independently.</summary>
    public enum AircraftPart
    {
        /// <summary>Main fuselage body.</summary>
        Fuselage,
        /// <summary>Left wing.</summary>
        LeftWing,
        /// <summary>Right wing.</summary>
        RightWing,
        /// <summary>Tail section (horizontal and vertical stabilizers combined).</summary>
        Tail,
        /// <summary>Propulsion engine(s).</summary>
        Engine,
        /// <summary>Cockpit and forward fuselage.</summary>
        Cockpit,
        /// <summary>Landing-gear assembly.</summary>
        LandingGear,
        /// <summary>Left aileron control surface.</summary>
        LeftAileron,
        /// <summary>Right aileron control surface.</summary>
        RightAileron,
        /// <summary>Rudder control surface.</summary>
        Rudder,
        /// <summary>Elevator control surface.</summary>
        Elevator
    }

    /// <summary>Operating mode of the repair system.</summary>
    public enum RepairMode
    {
        /// <summary>No repair in progress.</summary>
        None,
        /// <summary>Quick in-flight emergency repair burst.</summary>
        Emergency,
        /// <summary>Slow repair while stationary on the ground.</summary>
        FieldRepair,
        /// <summary>Fast repair at a designated repair station.</summary>
        FullRepair
    }
}
