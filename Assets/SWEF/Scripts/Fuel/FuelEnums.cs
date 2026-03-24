// FuelEnums.cs — SWEF Fuel & Energy Management System (Phase 69)

namespace SWEF.Fuel
{
    /// <summary>
    /// Phase 69 — Enumerations shared across the Fuel &amp; Energy Management system.
    /// </summary>

    // ── Fuel Type ─────────────────────────────────────────────────────────────

    /// <summary>Category of fuel a tank or refuel station uses.</summary>
    public enum FuelType
    {
        /// <summary>Standard aviation fuel (Jet-A / AvGas).</summary>
        Standard,

        /// <summary>High-octane racing or performance aviation fuel.</summary>
        HighOctane,

        /// <summary>Hydrogen-based fuel for experimental aircraft.</summary>
        Hydrogen,

        /// <summary>Electric energy storage (treated as fuel capacity in kWh).</summary>
        Electric,
    }

    // ── Tank State ────────────────────────────────────────────────────────────

    /// <summary>Operational state of a single fuel tank.</summary>
    public enum TankState
    {
        /// <summary>Tank is intact and functioning normally.</summary>
        Normal,

        /// <summary>Tank has been punctured and is losing fuel (set by Damage system).</summary>
        Leaking,

        /// <summary>Tank has sustained structural damage but is not currently leaking.</summary>
        Damaged,

        /// <summary>Tank is completely empty.</summary>
        Empty,

        /// <summary>Tank leak has been emergency-sealed; tank is functional but at reduced capacity.</summary>
        Sealed,
    }

    // ── Refuel State ──────────────────────────────────────────────────────────

    /// <summary>State of a refuelling operation at a <see cref="RefuelStation"/>.</summary>
    public enum RefuelState
    {
        /// <summary>Station is idle; no aircraft connected.</summary>
        Idle,

        /// <summary>Fuel hose is being connected to the aircraft.</summary>
        Connecting,

        /// <summary>Fuel is actively flowing into the aircraft's tanks.</summary>
        Refueling,

        /// <summary>Refuelling has completed successfully.</summary>
        Complete,

        /// <summary>Connection was severed before completion (aircraft moved away or manual stop).</summary>
        Disconnected,
    }

    // ── Fuel Warning Level ────────────────────────────────────────────────────

    /// <summary>Pilot-facing fuel warning level computed from total fuel percentage.</summary>
    public enum FuelWarningLevel
    {
        /// <summary>Fuel level is healthy (≥ 25 %).</summary>
        Normal,

        /// <summary>Fuel is below 25 % — low-fuel caution.</summary>
        Low,

        /// <summary>Fuel is below 10 % — critical warning, divert immediately.</summary>
        Critical,

        /// <summary>All tanks are empty; engine will flame out.</summary>
        Empty,
    }
}
