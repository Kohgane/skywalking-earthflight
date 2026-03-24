// ContrailEnums.cs — SWEF Contrail & Exhaust Trail System (Phase 71)
namespace SWEF.Contrail
{
    /// <summary>Classifies the source and visual character of a trail.</summary>
    public enum ContrailType
    {
        /// <summary>Ice-crystal condensation trail produced at high altitude and low temperature.</summary>
        Condensation,
        /// <summary>Hot gas exhaust trail emitted by engine nozzles.</summary>
        Exhaust,
        /// <summary>Low-pressure vortex trail shed from wingtips under high lift.</summary>
        WingtipVortex,
        /// <summary>Dense opaque smoke trail, e.g. from a damaged engine.</summary>
        Smoke,
        /// <summary>Bright flame cone produced during afterburner operation.</summary>
        AfterburnerFlame
    }

    /// <summary>Perceptual density / visibility level of a trail effect.</summary>
    public enum TrailIntensity
    {
        /// <summary>No visible trail.</summary>
        None,
        /// <summary>Thin, barely visible trail.</summary>
        Light,
        /// <summary>Clear, normally visible trail.</summary>
        Medium,
        /// <summary>Thick, prominent trail.</summary>
        Heavy,
        /// <summary>Dense maximum-width trail; highest performance cost.</summary>
        Maximum
    }

    /// <summary>How long a trail segment remains visible in the world before fading.</summary>
    public enum TrailPersistence
    {
        /// <summary>Fades within a few seconds.</summary>
        Short,
        /// <summary>Persists for tens of seconds.</summary>
        Medium,
        /// <summary>Persists for several minutes.</summary>
        Long,
        /// <summary>Effectively permanent for the duration of the flight session.</summary>
        Permanent
    }
}
