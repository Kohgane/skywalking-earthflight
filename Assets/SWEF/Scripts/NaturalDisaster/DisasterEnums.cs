// DisasterEnums.cs — SWEF Natural Disaster & Dynamic World Events (Phase 86)

namespace SWEF.NaturalDisaster
{
    /// <summary>Category of natural disaster.</summary>
    public enum DisasterType
    {
        Volcano,
        Earthquake,
        Hurricane,
        Wildfire,
        Tsunami,
        Tornado,
        Avalanche,
        Sandstorm,
        Flood,
        Blizzard
    }

    /// <summary>Intensity scale for a disaster event.</summary>
    public enum DisasterSeverity
    {
        Minor,
        Moderate,
        Severe,
        Catastrophic,
        Apocalyptic
    }

    /// <summary>Lifecycle phase of an active disaster.</summary>
    public enum DisasterPhase
    {
        Dormant,
        Warning,
        Onset,
        Peak,
        Declining,
        Aftermath
    }

    /// <summary>Type of hazard zone produced by a disaster.</summary>
    public enum HazardZoneType
    {
        NoFlyZone,
        Turbulence,
        ReducedVisibility,
        ThermalUpDraft,
        AshCloud,
        DebrisField,
        FloodZone,
        FireZone
    }

    /// <summary>Objective categories for auto-generated rescue missions.</summary>
    public enum RescueObjectiveType
    {
        Evacuate,
        SupplyDrop,
        MedicalAid,
        Search,
        Escort,
        Extinguish
    }
}
