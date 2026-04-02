// DisasterConfig.cs — SWEF Natural Disaster & Dynamic World Events (Phase 86)

namespace SWEF.NaturalDisaster
{
    /// <summary>
    /// Compile-time constants shared across the Natural Disaster system.
    /// All values are tuned for a global-scale flight simulator where distances
    /// are measured in metres and altitudes in metres above sea level.
    /// </summary>
    public static class DisasterConfig
    {
        // ── Warning Lead Times (seconds before onset per severity) ────────────────
        public const float WarningLeadMinor        = 30f;
        public const float WarningLeadModerate     = 60f;
        public const float WarningLeadSevere       = 120f;
        public const float WarningLeadCatastrophic = 180f;
        public const float WarningLeadApocalyptic  = 300f;

        // ── Hazard Zone Default Radii (metres) ────────────────────────────────────
        public const float RadiusNoFlyZone         = 5000f;
        public const float RadiusTurbulence        = 8000f;
        public const float RadiusReducedVisibility = 12000f;
        public const float RadiusThermalUpDraft    = 3000f;
        public const float RadiusAshCloud          = 15000f;
        public const float RadiusDebrisField       = 2000f;
        public const float RadiusFloodZone         = 6000f;
        public const float RadiusFireZone          = 4000f;

        // ── Disaster Duration Ranges (seconds) ────────────────────────────────────
        public const float MinDurationMinor        = 60f;
        public const float MaxDurationMinor        = 180f;
        public const float MinDurationModerate     = 180f;
        public const float MaxDurationModerate     = 360f;
        public const float MinDurationSevere       = 300f;
        public const float MaxDurationSevere       = 600f;
        public const float MinDurationCatastrophic = 600f;
        public const float MaxDurationCatastrophic = 1200f;
        public const float MinDurationApocalyptic  = 900f;
        public const float MaxDurationApocalyptic  = 1800f;

        // ── Atmospheric Effect Distances (metres) ─────────────────────────────────
        public const float AshCloudMaxDistance     = 20000f;
        public const float SmokeMaxDistance        = 10000f;
        public const float DebrisMaxDistance       = 5000f;
        public const float SteamMaxDistance        = 3000f;

        // ── Rescue Mission Auto-Generate Thresholds ───────────────────────────────
        public const float RescueMissionMinSeverity     = 1f;   // Moderate and above
        public const float RescueMissionMinChance       = 0.1f;
        public const float RescueMissionMaxChance       = 1.0f;
        public const float RescueMissionTimeLimitBase   = 300f;  // seconds at Moderate
        public const float RescueMissionTimeLimitScalar = 60f;   // added per severity level

        // ── Damage Tick Intervals (seconds) ──────────────────────────────────────
        public const float DamageTickInterval           = 1.0f;
        public const float DamageTickIntervalPeak       = 0.5f;

        // ── Screen Shake Intensity Ranges ─────────────────────────────────────────
        public const float ShakeMinorIntensity          = 0.05f;
        public const float ShakeModerateIntensity       = 0.15f;
        public const float ShakeSevereIntensity         = 0.30f;
        public const float ShakeCatastrophicIntensity   = 0.55f;
        public const float ShakeApocalypticIntensity    = 1.00f;
        public const float ShakeDuration                = 0.4f;

        // ── Minimap Blip Registration Distances (metres) ──────────────────────────
        public const float MinimapBlipRegistrationRange = 50000f;
        public const float MinimapBlipDeregistrationRange = 60000f;

        // ── Spawn Settings ────────────────────────────────────────────────────────
        public const int   MaxConcurrentDisasters       = 2;
        public const float DisasterCheckInterval        = 60f;
        public const float BaseSpawnChance              = 0.3f;

        // ── Flight modifier limits ────────────────────────────────────────────────
        public const float MaxTurbulenceMultiplier      = 3.0f;
        public const float MaxVisibilityReduction       = 0.95f;
        public const float MaxSpeedReductionAsh         = 0.4f;
        public const float MaxSpeedReductionDebris      = 0.5f;
    }
}
