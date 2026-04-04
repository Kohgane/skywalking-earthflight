// LicenseTier.cs — SWEF Flight Academy & Certification System (Phase 104)
namespace SWEF.Academy
{
    /// <summary>
    /// Ordered tiers of pilot certification available through the Flight Academy.
    /// Higher values represent greater expertise and privilege.
    /// </summary>
    public enum LicenseTier
    {
        /// <summary>No certification yet — the default starting state.</summary>
        None = 0,

        /// <summary>Student Pilot Certificate — basic flight fundamentals.</summary>
        StudentPilot = 1,

        /// <summary>Private Pilot License (PPL) — solo and recreational flying.</summary>
        PrivatePilot = 2,

        /// <summary>Commercial Pilot License (CPL) — advanced cross-country and IFR operations.</summary>
        CommercialPilot = 3,

        /// <summary>Airline Transport Pilot (ATP) — the highest civil aviation certification.</summary>
        AirlineTransportPilot = 4
    }
}
