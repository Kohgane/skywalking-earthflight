// NaturalDisasterTests.cs — NUnit EditMode tests for Phase 86 Natural Disaster & Dynamic World Events
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.NaturalDisaster;

[TestFixture]
public class NaturalDisasterTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static DisasterData BuildDisasterData(
        DisasterType type = DisasterType.Volcano,
        DisasterSeverity maxSev = DisasterSeverity.Severe,
        float baseDuration = 300f,
        float warningDuration = 60f,
        float peakDuration = 60f,
        float aftermathDuration = 120f,
        float hazardRadius = 5000f,
        float expansionRate = 5f,
        float turbulenceMultiplier = 1.5f,
        float visibilityReduction = 0.4f,
        bool canTrigger = true,
        float rescueChance = 0.8f)
    {
        var data = ScriptableObject.CreateInstance<DisasterData>();
        data.disasterId             = $"test_{type}";
        data.disasterName           = $"Test {type}";
        data.type                   = type;
        data.maxSeverity            = maxSev;
        data.baseDuration           = baseDuration;
        data.warningDuration        = warningDuration;
        data.peakDuration           = peakDuration;
        data.aftermathDuration      = aftermathDuration;
        data.hazardRadius           = hazardRadius;
        data.expansionRate          = expansionRate;
        data.turbulenceMultiplier   = turbulenceMultiplier;
        data.visibilityReduction    = visibilityReduction;
        data.hazardTypes            = new List<HazardZoneType>
        {
            HazardZoneType.Turbulence,
            HazardZoneType.ReducedVisibility,
            HazardZoneType.NoFlyZone
        };
        data.altitudeRange          = new Vector2(0f, 15000f);
        data.canTriggerRescueMission = canTrigger;
        data.rescueMissionChance    = rescueChance;
        data.rescueMissionDifficulty = 3;
        data.atmosphericEffects     = new[] { "ash", "smoke" };
        return data;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DisasterType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DisasterType_HasTenValues()
    {
        Assert.AreEqual(10, Enum.GetValues(typeof(DisasterType)).Length,
            "DisasterType must have exactly 10 values as per the Phase 86 spec.");
    }

    [Test]
    public void DisasterType_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterType), DisasterType.Volcano));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterType), DisasterType.Earthquake));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterType), DisasterType.Hurricane));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterType), DisasterType.Wildfire));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterType), DisasterType.Tsunami));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterType), DisasterType.Tornado));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterType), DisasterType.Avalanche));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterType), DisasterType.Sandstorm));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterType), DisasterType.Flood));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterType), DisasterType.Blizzard));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DisasterSeverity enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DisasterSeverity_HasFiveValues()
    {
        Assert.AreEqual(5, Enum.GetValues(typeof(DisasterSeverity)).Length);
    }

    [Test]
    public void DisasterSeverity_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterSeverity), DisasterSeverity.Minor));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterSeverity), DisasterSeverity.Moderate));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterSeverity), DisasterSeverity.Severe));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterSeverity), DisasterSeverity.Catastrophic));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterSeverity), DisasterSeverity.Apocalyptic));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DisasterPhase enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DisasterPhase_HasSixValues()
    {
        Assert.AreEqual(6, Enum.GetValues(typeof(DisasterPhase)).Length);
    }

    [Test]
    public void DisasterPhase_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterPhase), DisasterPhase.Dormant));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterPhase), DisasterPhase.Warning));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterPhase), DisasterPhase.Onset));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterPhase), DisasterPhase.Peak));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterPhase), DisasterPhase.Declining));
        Assert.IsTrue(Enum.IsDefined(typeof(DisasterPhase), DisasterPhase.Aftermath));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HazardZoneType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void HazardZoneType_HasEightValues()
    {
        Assert.AreEqual(8, Enum.GetValues(typeof(HazardZoneType)).Length);
    }

    [Test]
    public void HazardZoneType_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.NoFlyZone));
        Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.Turbulence));
        Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.ReducedVisibility));
        Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.ThermalUpDraft));
        Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.AshCloud));
        Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.DebrisField));
        Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.FloodZone));
        Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.FireZone));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RescueObjectiveType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void RescueObjectiveType_HasSixValues()
    {
        Assert.AreEqual(6, Enum.GetValues(typeof(RescueObjectiveType)).Length);
    }

    [Test]
    public void RescueObjectiveType_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(RescueObjectiveType), RescueObjectiveType.Evacuate));
        Assert.IsTrue(Enum.IsDefined(typeof(RescueObjectiveType), RescueObjectiveType.SupplyDrop));
        Assert.IsTrue(Enum.IsDefined(typeof(RescueObjectiveType), RescueObjectiveType.MedicalAid));
        Assert.IsTrue(Enum.IsDefined(typeof(RescueObjectiveType), RescueObjectiveType.Search));
        Assert.IsTrue(Enum.IsDefined(typeof(RescueObjectiveType), RescueObjectiveType.Escort));
        Assert.IsTrue(Enum.IsDefined(typeof(RescueObjectiveType), RescueObjectiveType.Extinguish));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DisasterConfig constants
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DisasterConfig_WarningLeadTimesAreOrdered()
    {
        Assert.Less(DisasterConfig.WarningLeadMinor,        DisasterConfig.WarningLeadModerate);
        Assert.Less(DisasterConfig.WarningLeadModerate,     DisasterConfig.WarningLeadSevere);
        Assert.Less(DisasterConfig.WarningLeadSevere,       DisasterConfig.WarningLeadCatastrophic);
        Assert.Less(DisasterConfig.WarningLeadCatastrophic, DisasterConfig.WarningLeadApocalyptic);
    }

    [Test]
    public void DisasterConfig_HazardRadiiArePositive()
    {
        Assert.Greater(DisasterConfig.RadiusNoFlyZone,         0f);
        Assert.Greater(DisasterConfig.RadiusTurbulence,        0f);
        Assert.Greater(DisasterConfig.RadiusReducedVisibility, 0f);
        Assert.Greater(DisasterConfig.RadiusThermalUpDraft,    0f);
        Assert.Greater(DisasterConfig.RadiusAshCloud,          0f);
        Assert.Greater(DisasterConfig.RadiusDebrisField,       0f);
        Assert.Greater(DisasterConfig.RadiusFloodZone,         0f);
        Assert.Greater(DisasterConfig.RadiusFireZone,          0f);
    }

    [Test]
    public void DisasterConfig_MaxConcurrentDisastersIsAtLeastOne()
    {
        Assert.GreaterOrEqual(DisasterConfig.MaxConcurrentDisasters, 1);
    }

    [Test]
    public void DisasterConfig_BaseSpawnChanceIsValidProbability()
    {
        Assert.GreaterOrEqual(DisasterConfig.BaseSpawnChance, 0f);
        Assert.LessOrEqual(DisasterConfig.BaseSpawnChance, 1f);
    }

    [Test]
    public void DisasterConfig_MaxTurbulenceMultiplierIsPositive()
    {
        Assert.Greater(DisasterConfig.MaxTurbulenceMultiplier, 0f);
    }

    [Test]
    public void DisasterConfig_MaxVisibilityReductionIsLessOrEqualOne()
    {
        Assert.LessOrEqual(DisasterConfig.MaxVisibilityReduction, 1f);
        Assert.Greater(DisasterConfig.MaxVisibilityReduction, 0f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DisasterData ScriptableObject
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DisasterData_DefaultsAreValid()
    {
        DisasterData d = BuildDisasterData();
        Assert.IsFalse(string.IsNullOrEmpty(d.disasterId));
        Assert.IsFalse(string.IsNullOrEmpty(d.disasterName));
        Assert.Greater(d.baseDuration,       0f);
        Assert.Greater(d.warningDuration,    0f);
        Assert.Greater(d.peakDuration,       0f);
        Assert.Greater(d.aftermathDuration,  0f);
        Assert.Greater(d.hazardRadius,       0f);
        Assert.GreaterOrEqual(d.visibilityReduction, 0f);
        Assert.LessOrEqual(d.visibilityReduction, 1f);
        Assert.GreaterOrEqual(d.rescueMissionChance, 0f);
        Assert.LessOrEqual(d.rescueMissionChance, 1f);
        Assert.GreaterOrEqual(d.rescueMissionDifficulty, 1);
        Assert.LessOrEqual(d.rescueMissionDifficulty, 5);
    }

    [Test]
    public void DisasterData_HazardTypesListNotNull()
    {
        DisasterData d = BuildDisasterData();
        Assert.IsNotNull(d.hazardTypes);
    }

    [Test]
    public void DisasterData_AtmosphericEffectsNotNull()
    {
        DisasterData d = BuildDisasterData();
        Assert.IsNotNull(d.atmosphericEffects);
    }

    [Test]
    public void DisasterData_ValidBiomesCanBeEmpty()
    {
        DisasterData d = BuildDisasterData();
        Assert.IsNotNull(d.validBiomes);
    }

    [Test]
    public void DisasterData_AltitudeRangeMinLessThanMax()
    {
        DisasterData d = BuildDisasterData();
        Assert.Less(d.altitudeRange.x, d.altitudeRange.y);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HazardZone
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void HazardZone_IsPlayerInside_TrueWhenInsideRadiusAndAltitude()
    {
        var zone = new HazardZone
        {
            center          = Vector3.zero,
            radius          = 1000f,
            maxRadius       = 2000f,
            intensity       = 0.8f,
            altitudeFloor   = 0f,
            altitudeCeiling = 5000f,
            isActive        = true
        };

        Assert.IsTrue(zone.IsPlayerInside(new Vector3(500f, 0f, 0f), 1000f));
    }

    [Test]
    public void HazardZone_IsPlayerInside_FalseWhenOutsideRadius()
    {
        var zone = new HazardZone
        {
            center          = Vector3.zero,
            radius          = 500f,
            maxRadius       = 1000f,
            intensity       = 1f,
            altitudeFloor   = 0f,
            altitudeCeiling = 10000f,
            isActive        = true
        };

        Assert.IsFalse(zone.IsPlayerInside(new Vector3(600f, 0f, 0f), 1000f));
    }

    [Test]
    public void HazardZone_IsPlayerInside_FalseWhenBelowAltitudeFloor()
    {
        var zone = new HazardZone
        {
            center          = Vector3.zero,
            radius          = 1000f,
            maxRadius       = 2000f,
            intensity       = 1f,
            altitudeFloor   = 1000f,
            altitudeCeiling = 5000f,
            isActive        = true
        };

        Assert.IsFalse(zone.IsPlayerInside(Vector3.zero, 500f));
    }

    [Test]
    public void HazardZone_IsPlayerInside_FalseWhenAboveAltitudeCeiling()
    {
        var zone = new HazardZone
        {
            center          = Vector3.zero,
            radius          = 1000f,
            maxRadius       = 2000f,
            intensity       = 1f,
            altitudeFloor   = 0f,
            altitudeCeiling = 3000f,
            isActive        = true
        };

        Assert.IsFalse(zone.IsPlayerInside(Vector3.zero, 5000f));
    }

    [Test]
    public void HazardZone_IsPlayerInside_FalseWhenInactive()
    {
        var zone = new HazardZone
        {
            center          = Vector3.zero,
            radius          = 1000f,
            maxRadius       = 2000f,
            intensity       = 1f,
            altitudeFloor   = 0f,
            altitudeCeiling = 10000f,
            isActive        = false
        };

        Assert.IsFalse(zone.IsPlayerInside(Vector3.zero, 100f));
    }

    [Test]
    public void HazardZone_GetIntensityAtPosition_OneAtCenter()
    {
        var zone = new HazardZone
        {
            center    = Vector3.zero,
            radius    = 1000f,
            maxRadius = 2000f,
            intensity = 1f,
            isActive  = true
        };

        float intensity = zone.GetIntensityAtPosition(Vector3.zero);
        Assert.AreEqual(1f, intensity, 0.001f);
    }

    [Test]
    public void HazardZone_GetIntensityAtPosition_ZeroAtEdge()
    {
        var zone = new HazardZone
        {
            center    = Vector3.zero,
            radius    = 1000f,
            maxRadius = 2000f,
            intensity = 1f,
            isActive  = true
        };

        float intensity = zone.GetIntensityAtPosition(new Vector3(1000f, 0f, 0f));
        Assert.AreEqual(0f, intensity, 0.001f);
    }

    [Test]
    public void HazardZone_GetIntensityAtPosition_ZeroWhenInactive()
    {
        var zone = new HazardZone
        {
            center    = Vector3.zero,
            radius    = 1000f,
            maxRadius = 2000f,
            intensity = 1f,
            isActive  = false
        };

        Assert.AreEqual(0f, zone.GetIntensityAtPosition(Vector3.zero), 0.001f);
    }

    [Test]
    public void HazardZone_Expand_IncreasesRadiusUpToMax()
    {
        var zone = new HazardZone
        {
            radius    = 100f,
            maxRadius = 500f
        };

        zone.Expand(10f, 50f);   // 10 s × 50 m/s = +500 → clamped to 500
        Assert.AreEqual(500f, zone.radius, 0.001f);
    }

    [Test]
    public void HazardZone_Contract_DecreasesRadiusToZero()
    {
        var zone = new HazardZone
        {
            radius    = 100f,
            maxRadius = 500f
        };

        zone.Contract(10f, 20f);  // 10 s × 20 m/s = −200 → clamped to 0
        Assert.AreEqual(0f, zone.radius, 0.001f);
    }

    [Test]
    public void HazardZone_Expand_DoesNotExceedMaxRadius()
    {
        var zone = new HazardZone
        {
            radius    = 400f,
            maxRadius = 500f
        };

        zone.Expand(1f, 200f);
        Assert.AreEqual(500f, zone.radius, 0.001f);
    }

    [Test]
    public void HazardZone_Contract_DoesNotGoBelowZero()
    {
        var zone = new HazardZone
        {
            radius    = 10f,
            maxRadius = 500f
        };

        zone.Contract(1f, 200f);
        Assert.AreEqual(0f, zone.radius, 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HazardZone intensity falloff
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void HazardZone_GetIntensityAtPosition_HalfwayIsHalfIntensity()
    {
        var zone = new HazardZone
        {
            center    = Vector3.zero,
            radius    = 1000f,
            maxRadius = 2000f,
            intensity = 1f,
            isActive  = true
        };

        float intensity = zone.GetIntensityAtPosition(new Vector3(500f, 0f, 0f));
        Assert.AreEqual(0.5f, intensity, 0.001f);
    }

    [Test]
    public void HazardZone_GetIntensityAtPosition_ZeroOutsideRadius()
    {
        var zone = new HazardZone
        {
            center    = Vector3.zero,
            radius    = 1000f,
            maxRadius = 2000f,
            intensity = 1f,
            isActive  = true
        };

        Assert.AreEqual(0f, zone.GetIntensityAtPosition(new Vector3(2000f, 0f, 0f)), 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DisasterConfig duration range ordering
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DisasterConfig_DurationRangesHaveMinLessThanMax()
    {
        Assert.Less(DisasterConfig.MinDurationMinor,        DisasterConfig.MaxDurationMinor);
        Assert.Less(DisasterConfig.MinDurationModerate,     DisasterConfig.MaxDurationModerate);
        Assert.Less(DisasterConfig.MinDurationSevere,       DisasterConfig.MaxDurationSevere);
        Assert.Less(DisasterConfig.MinDurationCatastrophic, DisasterConfig.MaxDurationCatastrophic);
        Assert.Less(DisasterConfig.MinDurationApocalyptic,  DisasterConfig.MaxDurationApocalyptic);
    }

    [Test]
    public void DisasterConfig_HigherSeverityHasLongerMinDuration()
    {
        Assert.Less(DisasterConfig.MinDurationMinor,        DisasterConfig.MinDurationModerate);
        Assert.Less(DisasterConfig.MinDurationModerate,     DisasterConfig.MinDurationSevere);
        Assert.Less(DisasterConfig.MinDurationSevere,       DisasterConfig.MinDurationCatastrophic);
        Assert.Less(DisasterConfig.MinDurationCatastrophic, DisasterConfig.MinDurationApocalyptic);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DisasterConfig shake intensities
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DisasterConfig_ShakeIntensitiesAreOrdered()
    {
        Assert.Less(DisasterConfig.ShakeMinorIntensity,        DisasterConfig.ShakeModerateIntensity);
        Assert.Less(DisasterConfig.ShakeModerateIntensity,     DisasterConfig.ShakeSevereIntensity);
        Assert.Less(DisasterConfig.ShakeSevereIntensity,       DisasterConfig.ShakeCatastrophicIntensity);
        Assert.Less(DisasterConfig.ShakeCatastrophicIntensity, DisasterConfig.ShakeApocalypticIntensity);
    }

    [Test]
    public void DisasterConfig_ApocalypticShakeIsOne()
    {
        Assert.AreEqual(1.0f, DisasterConfig.ShakeApocalypticIntensity, 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DisasterData severity clamping
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DisasterData_MaxSeverityCanBeMinor()
    {
        DisasterData d = BuildDisasterData(maxSev: DisasterSeverity.Minor);
        Assert.AreEqual(DisasterSeverity.Minor, d.maxSeverity);
    }

    [Test]
    public void DisasterData_MaxSeverityCanBeApocalyptic()
    {
        DisasterData d = BuildDisasterData(maxSev: DisasterSeverity.Apocalyptic);
        Assert.AreEqual(DisasterSeverity.Apocalyptic, d.maxSeverity);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RescueObjectiveType pick helpers (via DisasterType)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DisasterData_WildfireCanTriggerRescueMission()
    {
        DisasterData d = BuildDisasterData(type: DisasterType.Wildfire, canTrigger: true);
        Assert.IsTrue(d.canTriggerRescueMission);
    }

    [Test]
    public void DisasterData_RescueMissionChanceIsInRange()
    {
        DisasterData d = BuildDisasterData(rescueChance: 0.7f);
        Assert.GreaterOrEqual(d.rescueMissionChance, 0f);
        Assert.LessOrEqual(d.rescueMissionChance, 1f);
    }
}
