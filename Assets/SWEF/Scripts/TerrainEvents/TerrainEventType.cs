// TerrainEventType.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)

namespace SWEF.TerrainEvents
{
    /// <summary>Category of geological or atmospheric terrain event.</summary>
    public enum TerrainEventType
    {
        VolcanicEruption,
        Earthquake,
        Aurora,
        Tsunami,
        Geyser,
        MudVolcano,
        HotSpring,
        Landslide,
        IceStorm,
        MagneticAnomaly
    }

    /// <summary>Lifecycle phase of an active terrain event.</summary>
    public enum TerrainEventPhase
    {
        Dormant,
        BuildUp,
        Active,
        Peak,
        Subsiding,
        Aftermath
    }

    /// <summary>Intensity scale for a terrain event.</summary>
    public enum TerrainEventIntensity
    {
        Trace,
        Minor,
        Moderate,
        Strong,
        Major,
        Extreme
    }

    /// <summary>Hemisphere / polar region used to gate aurora events.</summary>
    public enum PolarRegion
    {
        Neither,
        Northern,
        Southern,
        Both
    }

    /// <summary>Trigger type for mission integration.</summary>
    public enum TerrainEventMissionType
    {
        Witness,
        FlyThrough,
        Photograph,
        Survive,
        Research
    }
}
