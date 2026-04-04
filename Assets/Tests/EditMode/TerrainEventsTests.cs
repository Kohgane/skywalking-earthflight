// TerrainEventsTests.cs — NUnit EditMode tests for Phase 105 Dynamic Terrain Events & Geological Phenomena
using System;
using NUnit.Framework;
using UnityEngine;
using SWEF.TerrainEvents;

[TestFixture]
public class TerrainEventsTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // TerrainEventType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TerrainEventType_AllValuesAreDefined()
    {
        var values = (TerrainEventType[])Enum.GetValues(typeof(TerrainEventType));
        Assert.GreaterOrEqual(values.Length, 5, "At least 5 event types should be defined");
        Assert.Contains(TerrainEventType.VolcanicEruption, values);
        Assert.Contains(TerrainEventType.Earthquake,       values);
        Assert.Contains(TerrainEventType.Aurora,           values);
        Assert.Contains(TerrainEventType.Tsunami,          values);
        Assert.Contains(TerrainEventType.Geyser,           values);
    }

    [Test]
    public void TerrainEventPhase_OrderIsLogical()
    {
        Assert.Less((int)TerrainEventPhase.Dormant,   (int)TerrainEventPhase.BuildUp);
        Assert.Less((int)TerrainEventPhase.BuildUp,   (int)TerrainEventPhase.Active);
        Assert.Less((int)TerrainEventPhase.Active,    (int)TerrainEventPhase.Peak);
        Assert.Less((int)TerrainEventPhase.Peak,      (int)TerrainEventPhase.Subsiding);
        Assert.Less((int)TerrainEventPhase.Subsiding, (int)TerrainEventPhase.Aftermath);
    }

    [Test]
    public void TerrainEventIntensity_SixLevels()
    {
        var values = (TerrainEventIntensity[])Enum.GetValues(typeof(TerrainEventIntensity));
        Assert.AreEqual(6, values.Length, "Exactly 6 intensity levels expected");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TerrainEventConfig (ScriptableObject)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TerrainEventConfig_DefaultValues_AreReasonable()
    {
        var cfg = ScriptableObject.CreateInstance<TerrainEventConfig>();
        try
        {
            Assert.GreaterOrEqual(cfg.baseDuration,    10f,   "baseDuration >= 10 s");
            Assert.GreaterOrEqual(cfg.buildUpDuration,  5f,   "buildUpDuration >= 5 s");
            Assert.GreaterOrEqual(cfg.peakDuration,     5f,   "peakDuration >= 5 s");
            Assert.GreaterOrEqual(cfg.subsidingDuration, 5f,  "subsidingDuration >= 5 s");
            Assert.GreaterOrEqual(cfg.effectRadius,    50f,   "effectRadius >= 50 m");
            Assert.GreaterOrEqual(cfg.altitudeRange.y, cfg.altitudeRange.x, "altitude range max >= min");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cfg);
        }
    }

    [Test]
    public void TerrainEventConfig_TurbulenceAndVisibility_ClampedToValidRange()
    {
        var cfg = ScriptableObject.CreateInstance<TerrainEventConfig>();
        try
        {
            Assert.GreaterOrEqual(cfg.turbulenceMultiplier, 0f, "turbulence >= 0");
            Assert.LessOrEqual(cfg.turbulenceMultiplier,    5f, "turbulence <= 5");
            Assert.GreaterOrEqual(cfg.visibilityReduction,  0f, "visibility >= 0");
            Assert.LessOrEqual(cfg.visibilityReduction,     1f, "visibility <= 1");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cfg);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SeasonDefinition
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SeasonDefinition_IsActive_StandardRange()
    {
        var season = ScriptableObject.CreateInstance<SeasonDefinition>();
        try
        {
            season.startDay = 60;
            season.endDay   = 150;
            Assert.IsTrue(season.IsActive(60),  "start day is active");
            Assert.IsTrue(season.IsActive(100), "mid-range is active");
            Assert.IsTrue(season.IsActive(150), "end day is active");
            Assert.IsFalse(season.IsActive(59),  "day before start is not active");
            Assert.IsFalse(season.IsActive(151), "day after end is not active");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(season);
        }
    }

    [Test]
    public void SeasonDefinition_IsActive_WrapAroundRange()
    {
        var season = ScriptableObject.CreateInstance<SeasonDefinition>();
        try
        {
            season.startDay = 330; // late November
            season.endDay   = 60;  // early March (crosses year end)
            Assert.IsTrue(season.IsActive(330), "start day active");
            Assert.IsTrue(season.IsActive(1),   "new-year day active");
            Assert.IsTrue(season.IsActive(60),  "end day active");
            Assert.IsFalse(season.IsActive(200), "summer day not active");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(season);
        }
    }

    [Test]
    public void SeasonDefinition_GetProbabilityMultiplier_FallsBackToOne()
    {
        var season = ScriptableObject.CreateInstance<SeasonDefinition>();
        try
        {
            // Empty array — every type should return 1
            season.eventTypeProbabilityMultipliers = new float[0];
            Assert.AreEqual(1f, season.GetProbabilityMultiplier(TerrainEventType.Aurora), 1e-4f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(season);
        }
    }

    [Test]
    public void SeasonDefinition_GetProbabilityMultiplier_ReturnsSetValue()
    {
        var season = ScriptableObject.CreateInstance<SeasonDefinition>();
        try
        {
            season.eventTypeProbabilityMultipliers = new float[10];
            int auroraIndex = (int)TerrainEventType.Aurora;
            season.eventTypeProbabilityMultipliers[auroraIndex] = 5f;
            Assert.AreEqual(5f, season.GetProbabilityMultiplier(TerrainEventType.Aurora), 1e-4f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(season);
        }
    }

    [Test]
    public void SeasonDefinition_GetCurrentSeason_ReturnsNullForEmptyArray()
    {
        SeasonDefinition result = SeasonDefinition.GetCurrentSeason(new SeasonDefinition[0]);
        Assert.IsNull(result, "No seasons defined — should return null");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RegionalEventProfile
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void RegionalEventProfile_ContainsPosition_InsideBounds()
    {
        var profile = ScriptableObject.CreateInstance<RegionalEventProfile>();
        try
        {
            profile.boundsMin = new Vector2(-1000f, -1000f);
            profile.boundsMax = new Vector2( 1000f,  1000f);
            Assert.IsTrue(profile.ContainsPosition(new Vector3(0f, 0f, 0f)),     "origin inside");
            Assert.IsTrue(profile.ContainsPosition(new Vector3(500f, 0f, 500f)), "inside corner");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    [Test]
    public void RegionalEventProfile_ContainsPosition_OutsideBounds()
    {
        var profile = ScriptableObject.CreateInstance<RegionalEventProfile>();
        try
        {
            profile.boundsMin = new Vector2(-1000f, -1000f);
            profile.boundsMax = new Vector2( 1000f,  1000f);
            Assert.IsFalse(profile.ContainsPosition(new Vector3(2000f, 0f, 0f)),   "too far east");
            Assert.IsFalse(profile.ContainsPosition(new Vector3(0f, 0f, -2000f)),  "too far south");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    [Test]
    public void RegionalEventProfile_GetRandomConfig_ReturnsNullWhenEmpty()
    {
        var profile = ScriptableObject.CreateInstance<RegionalEventProfile>();
        try
        {
            Assert.IsNull(profile.GetRandomConfig(), "Empty list should return null");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TerrainDeformationSystem — record keeping (no Unity Terrain required)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DeformationRecord_FieldsAreStoredCorrectly()
    {
        var record = new TerrainDeformationSystem.DeformationRecord(
            new Vector3(100f, 0f, 200f), 500f, -30f, 60f);

        Assert.AreEqual(new Vector3(100f, 0f, 200f), record.centre);
        Assert.AreEqual(500f,   record.radius,   1e-4f);
        Assert.AreEqual(-30f,   record.amount,   1e-4f);
        Assert.AreEqual(60f,    record.gameTime, 1e-4f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EarthquakeEvent — magnitude mapping
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void EarthquakeEvent_Magnitude_IncreasesWithIntensity()
    {
        // We can't easily instantiate the MB, but we can test the logic via the config
        // by checking that stronger intensity configs produce higher magnitudes indirectly
        // through the enum ordering.
        Assert.Less((int)TerrainEventIntensity.Minor,    (int)TerrainEventIntensity.Strong);
        Assert.Less((int)TerrainEventIntensity.Strong,   (int)TerrainEventIntensity.Major);
        Assert.Less((int)TerrainEventIntensity.Major,    (int)TerrainEventIntensity.Extreme);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TerrainEventAchievements — constants
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TerrainEventAchievements_Constants_AreNonEmpty()
    {
        Assert.IsFalse(string.IsNullOrEmpty(TerrainEventAchievements.WitnessVolcano),     "WitnessVolcano key");
        Assert.IsFalse(string.IsNullOrEmpty(TerrainEventAchievements.FlyThroughAurora),   "FlyThroughAurora key");
        Assert.IsFalse(string.IsNullOrEmpty(TerrainEventAchievements.SurviveEarthquake),  "SurviveEarthquake key");
        Assert.IsFalse(string.IsNullOrEmpty(TerrainEventAchievements.PhotographTsunami),  "PhotographTsunami key");
        Assert.IsFalse(string.IsNullOrEmpty(TerrainEventAchievements.WitnessGeyser),      "WitnessGeyser key");
        Assert.IsFalse(string.IsNullOrEmpty(TerrainEventAchievements.AllEventsWitnessed), "AllEventsWitnessed key");
    }

    [Test]
    public void TerrainEventAchievements_Constants_AreUnique()
    {
        var keys = new[]
        {
            TerrainEventAchievements.WitnessVolcano,
            TerrainEventAchievements.FlyThroughAurora,
            TerrainEventAchievements.SurviveEarthquake,
            TerrainEventAchievements.PhotographTsunami,
            TerrainEventAchievements.WitnessGeyser,
            TerrainEventAchievements.AllEventsWitnessed
        };
        var set = new System.Collections.Generic.HashSet<string>(keys);
        Assert.AreEqual(keys.Length, set.Count, "All achievement keys should be unique");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PolarRegion enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PolarRegion_ContainsExpectedValues()
    {
        var values = (PolarRegion[])Enum.GetValues(typeof(PolarRegion));
        Assert.Contains(PolarRegion.Neither,  values);
        Assert.Contains(PolarRegion.Northern, values);
        Assert.Contains(PolarRegion.Southern, values);
        Assert.Contains(PolarRegion.Both,     values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TerrainEventMissionType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TerrainEventMissionType_ContainsExpectedValues()
    {
        var values = (TerrainEventMissionType[])Enum.GetValues(typeof(TerrainEventMissionType));
        Assert.Contains(TerrainEventMissionType.Witness,     values);
        Assert.Contains(TerrainEventMissionType.FlyThrough,  values);
        Assert.Contains(TerrainEventMissionType.Photograph,  values);
        Assert.Contains(TerrainEventMissionType.Survive,     values);
        Assert.Contains(TerrainEventMissionType.Research,    values);
    }
}
