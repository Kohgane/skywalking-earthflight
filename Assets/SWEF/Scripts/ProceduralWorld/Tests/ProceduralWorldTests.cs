// ProceduralWorldTests.cs — NUnit EditMode tests for Phase 113: Procedural City & Airport Generation
// Covers: enums, config, city generation, airport generation, terrain analysis, LOD, streaming, data models.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.ProceduralWorld;

[TestFixture]
public class ProceduralWorldTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // CityType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CityType_AllValuesAreDefined()
    {
        var values = (CityType[])Enum.GetValues(typeof(CityType));
        Assert.GreaterOrEqual(values.Length, 5, "At least 5 city types required");
        Assert.Contains(CityType.Metropolis, values);
        Assert.Contains(CityType.Town,       values);
        Assert.Contains(CityType.Village,    values);
        Assert.Contains(CityType.Industrial, values);
        Assert.Contains(CityType.Coastal,    values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BuildingType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void BuildingType_AllValuesAreDefined()
    {
        var values = (BuildingType[])Enum.GetValues(typeof(BuildingType));
        Assert.Contains(BuildingType.Residential, values);
        Assert.Contains(BuildingType.Commercial,  values);
        Assert.Contains(BuildingType.Industrial,  values);
        Assert.Contains(BuildingType.Landmark,    values);
        Assert.Contains(BuildingType.Government,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AirportType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AirportType_AllValuesAreDefined()
    {
        var values = (AirportType[])Enum.GetValues(typeof(AirportType));
        Assert.Contains(AirportType.International, values);
        Assert.Contains(AirportType.Regional,      values);
        Assert.Contains(AirportType.Military,      values);
        Assert.Contains(AirportType.Helipad,       values);
        Assert.Contains(AirportType.Seaplane,      values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RoadType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void RoadType_AllValuesAreDefined()
    {
        var values = (RoadType[])Enum.GetValues(typeof(RoadType));
        Assert.Contains(RoadType.Highway,    values);
        Assert.Contains(RoadType.MainRoad,   values);
        Assert.Contains(RoadType.SideStreet, values);
        Assert.Contains(RoadType.Lane,       values);
        Assert.Contains(RoadType.Roundabout, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenerationState enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GenerationState_AllValuesAreDefined()
    {
        var values = (GenerationState[])Enum.GetValues(typeof(GenerationState));
        Assert.Contains(GenerationState.Idle,             values);
        Assert.Contains(GenerationState.AnalyzingTerrain, values);
        Assert.Contains(GenerationState.GeneratingLayout, values);
        Assert.Contains(GenerationState.PlacingObjects,   values);
        Assert.Contains(GenerationState.ConfiguringLOD,   values);
        Assert.Contains(GenerationState.Complete,         values);
        Assert.Contains(GenerationState.Failed,           values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LODLevel enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LODLevel_AllValuesAreDefined()
    {
        var values = (LODLevel[])Enum.GetValues(typeof(LODLevel));
        Assert.Contains(LODLevel.LOD0, values);
        Assert.Contains(LODLevel.LOD1, values);
        Assert.Contains(LODLevel.LOD2, values);
        Assert.Contains(LODLevel.LOD3, values);
    }

    [Test]
    public void LODLevel_OrderIsCorrect()
    {
        Assert.Less((int)LODLevel.LOD0, (int)LODLevel.LOD1);
        Assert.Less((int)LODLevel.LOD1, (int)LODLevel.LOD2);
        Assert.Less((int)LODLevel.LOD2, (int)LODLevel.LOD3);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ProceduralWorldConfig
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ProceduralWorldConfig_DefaultsAreValid()
    {
        var cfg = ScriptableObject.CreateInstance<ProceduralWorldConfig>();
        Assert.Greater(cfg.generationDensity, 0f, "generationDensity must be positive");
        Assert.Greater(cfg.airportFrequency,   0f, "airportFrequency must be positive");
        Assert.Less(cfg.airportFrequency,      1f, "airportFrequency must be < 1");
        Assert.Greater(cfg.roadGridDensity,    0f);
        Assert.Greater(cfg.maxFloors,          cfg.minFloors);
        Assert.Greater(cfg.lod1Distance,       0f);
        Assert.Greater(cfg.lod2Distance,       cfg.lod1Distance);
        Assert.Greater(cfg.lod3Distance,       cfg.lod2Distance);
        Assert.Greater(cfg.chunkUnloadDistance, cfg.lod3Distance);
        Assert.Greater(cfg.chunkSizeMetres,    0f);
        Assert.Greater(cfg.metresPerFloor,     0f);
        UnityEngine.Object.DestroyImmediate(cfg);
    }

    [Test]
    public void ProceduralWorldConfig_NoiseParametersAreValid()
    {
        var cfg = ScriptableObject.CreateInstance<ProceduralWorldConfig>();
        Assert.Greater(cfg.noiseScale,       0f);
        Assert.GreaterOrEqual(cfg.noiseOctaves,    1);
        Assert.Greater(cfg.noisePersistence, 0f);
        Assert.LessOrEqual(cfg.noisePersistence, 1f);
        UnityEngine.Object.DestroyImmediate(cfg);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BuildingInstance data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void BuildingInstance_ConstructorSetsFields()
    {
        var pos = new Vector3(100f, 5f, 200f);
        var b = new BuildingInstance(pos, BuildingType.Commercial, 20);
        Assert.AreEqual(pos, b.position);
        Assert.AreEqual(BuildingType.Commercial, b.buildingType);
        Assert.AreEqual(20, b.floorCount);
        Assert.AreEqual(LODLevel.LOD0, b.currentLOD, "Should default to LOD0");
    }

    [Test]
    public void BuildingInstance_FloorCountClampedToMinimumOne()
    {
        var b = new BuildingInstance(Vector3.zero, BuildingType.Residential, -5);
        Assert.GreaterOrEqual(b.floorCount, 1, "Floor count must be at least 1");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RunwayData data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void RunwayData_ReciprocalHeadingIsCorrect()
    {
        var rwy = new RunwayData { heading = 90f };
        Assert.AreEqual(270f, rwy.ReciprocalHeading, 0.001f);
    }

    [Test]
    public void RunwayData_ReciprocalHeadingWrapsAround360()
    {
        var rwy = new RunwayData { heading = 270f };
        Assert.AreEqual(90f, rwy.ReciprocalHeading, 0.001f);
    }

    [Test]
    public void RunwayData_ReciprocalHeadingZeroDegrees()
    {
        var rwy = new RunwayData { heading = 0f };
        Assert.AreEqual(180f, rwy.ReciprocalHeading, 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AirportLayout data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AirportLayout_DefaultRunwayListIsEmpty()
    {
        var layout = new AirportLayout();
        Assert.IsNotNull(layout.runways);
        Assert.AreEqual(0, layout.runways.Count);
    }

    [Test]
    public void AirportLayout_CanAddRunways()
    {
        var layout = new AirportLayout { icaoCode = "XTEST" };
        layout.runways.Add(new RunwayData { designator = "09L", heading = 90f });
        layout.runways.Add(new RunwayData { designator = "27R", heading = 270f });
        Assert.AreEqual(2, layout.runways.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CityDescription data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CityDescription_DefaultBuildingListIsEmpty()
    {
        var city = new CityDescription();
        Assert.IsNotNull(city.buildings);
        Assert.AreEqual(0, city.buildings.Count);
    }

    [Test]
    public void CityDescription_CanAddBuildings()
    {
        var city = new CityDescription { cityName = "TestCity" };
        city.buildings.Add(new BuildingInstance(Vector3.zero, BuildingType.Residential, 3));
        Assert.AreEqual(1, city.buildings.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CityLayout data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CityLayout_TotalPlotsIsBlocksXTimesZ()
    {
        var layout = new CityLayout { blocksX = 8, blocksZ = 6 };
        Assert.AreEqual(48, layout.TotalPlots);
    }

    [Test]
    public void CityLayout_TotalRoadLengthSumsSegments()
    {
        var layout = new CityLayout();
        layout.roadSegments.Add(new RoadSegmentData
        {
            start = Vector3.zero,
            end = new Vector3(100f, 0f, 0f),
            roadType = RoadType.MainRoad,
            widthMetres = 12f
        });
        layout.roadSegments.Add(new RoadSegmentData
        {
            start = Vector3.zero,
            end = new Vector3(0f, 0f, 200f),
            roadType = RoadType.SideStreet,
            widthMetres = 6f
        });
        Assert.AreEqual(300f, layout.TotalRoadLengthMetres, 0.01f);
    }

    [Test]
    public void CityLayout_GetZoneAt_ReturnsResidentialWhenNoMap()
    {
        var layout = new CityLayout { blocksX = 4, blocksZ = 4 };
        // zoneMap is null
        Assert.AreEqual(BuildingType.Residential, layout.GetZoneAt(0, 0));
    }

    [Test]
    public void CityLayout_GetZoneAt_ReturnsCorrectZone()
    {
        var layout = new CityLayout { blocksX = 2, blocksZ = 2 };
        layout.zoneMap = new int[2, 2];
        layout.zoneMap[0, 0] = (int)BuildingType.Commercial;
        layout.zoneMap[1, 1] = (int)BuildingType.Industrial;
        Assert.AreEqual(BuildingType.Commercial, layout.GetZoneAt(0, 0));
        Assert.AreEqual(BuildingType.Industrial, layout.GetZoneAt(1, 1));
    }

    [Test]
    public void CityLayout_GetDensityAt_ReturnsZeroWhenNoMap()
    {
        var layout = new CityLayout { blocksX = 4, blocksZ = 4 };
        Assert.AreEqual(0f, layout.GetDensityAt(0, 0), 0.001f);
    }

    [Test]
    public void CityLayout_GetDensityAt_ReturnsCorrectValue()
    {
        var layout = new CityLayout { blocksX = 2, blocksZ = 2 };
        layout.densityMap = new float[2, 2];
        layout.densityMap[1, 0] = 0.75f;
        Assert.AreEqual(0.75f, layout.GetDensityAt(1, 0), 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ChunkCoord data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ChunkCoord_EqualityIsCorrect()
    {
        var a = new ChunkCoord(3, -5);
        var b = new ChunkCoord(3, -5);
        var c = new ChunkCoord(3, 5);
        Assert.IsTrue(a.Equals(b));
        Assert.IsFalse(a.Equals(c));
    }

    [Test]
    public void ChunkCoord_GetHashCode_SameForEqualCoords()
    {
        var a = new ChunkCoord(10, 20);
        var b = new ChunkCoord(10, 20);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Test]
    public void ChunkCoord_WorksAsHashSetKey()
    {
        var set = new HashSet<ChunkCoord> { new ChunkCoord(1, 2), new ChunkCoord(3, 4) };
        Assert.IsTrue(set.Contains(new ChunkCoord(1, 2)));
        Assert.IsFalse(set.Contains(new ChunkCoord(5, 6)));
    }

    [Test]
    public void ChunkCoord_ToStringFormat()
    {
        var coord = new ChunkCoord(7, -3);
        Assert.AreEqual("Chunk(7,-3)", coord.ToString());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TerrainAnalysisResult data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TerrainAnalysisResult_CanBeCreated()
    {
        var result = new TerrainAnalysisResult
        {
            isSuitable = true,
            averageElevation = 150f,
            maxSlopeDegrees = 2.5f,
            hasCoastline = false,
            recommendedCityType = CityType.Metropolis,
            recommendedAirportType = AirportType.International
        };
        Assert.IsTrue(result.isSuitable);
        Assert.AreEqual(150f, result.averageElevation, 0.001f);
        Assert.AreEqual(CityType.Metropolis, result.recommendedCityType);
        Assert.AreEqual(AirportType.International, result.recommendedAirportType);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RunwayGenerator static method
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void RunwayGenerator_OptimalHeading_IntoWind()
    {
        // Wind from the north (360) → land heading 180
        float heading = RunwayGenerator.OptimalHeading(360f);
        Assert.AreEqual(180f, heading, 5f, "Should be roughly 180 for northerly wind");
    }

    [Test]
    public void RunwayGenerator_OptimalHeading_RoundedToTenDegrees()
    {
        float heading = RunwayGenerator.OptimalHeading(273f);
        Assert.AreEqual(0f, heading % 10f, 0.01f, "Should be a multiple of 10");
    }

    [Test]
    public void RunwayGenerator_OptimalHeading_AlwaysInRange()
    {
        for (int wind = 0; wind < 360; wind += 15)
        {
            float h = RunwayGenerator.OptimalHeading(wind);
            Assert.GreaterOrEqual(h, 0f);
            Assert.Less(h, 360f);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ProceduralWorldAnalytics
    // ═══════════════════════════════════════════════════════════════════════════

    [SetUp]
    public void SetUpAnalytics() => ProceduralWorldAnalytics.Reset();

    [Test]
    public void Analytics_InitialCountersAreZero()
    {
        ProceduralWorldAnalytics.Reset();
        Assert.AreEqual(0, ProceduralWorldAnalytics.CitiesGenerated);
        Assert.AreEqual(0, ProceduralWorldAnalytics.AirportsGenerated);
        Assert.AreEqual(0, ProceduralWorldAnalytics.ChunksLoaded);
        Assert.AreEqual(0, ProceduralWorldAnalytics.ChunksUnloaded);
    }

    [Test]
    public void Analytics_TrackCityGenerated_IncreasesCounter()
    {
        var city = new CityDescription { cityName = "TestCity", cityType = CityType.Town };
        ProceduralWorldAnalytics.TrackCityGenerated(city);
        Assert.AreEqual(1, ProceduralWorldAnalytics.CitiesGenerated);
    }

    [Test]
    public void Analytics_TrackAirportGenerated_IncreasesCounter()
    {
        var airport = new AirportLayout { icaoCode = "XTST", airportType = AirportType.Regional };
        ProceduralWorldAnalytics.TrackAirportGenerated(airport);
        Assert.AreEqual(1, ProceduralWorldAnalytics.AirportsGenerated);
    }

    [Test]
    public void Analytics_TrackChunkEvents()
    {
        ProceduralWorldAnalytics.TrackChunkLoaded(new ChunkCoord(0, 0));
        ProceduralWorldAnalytics.TrackChunkLoaded(new ChunkCoord(1, 0));
        ProceduralWorldAnalytics.TrackChunkUnloaded(new ChunkCoord(0, 0));
        Assert.AreEqual(2, ProceduralWorldAnalytics.ChunksLoaded);
        Assert.AreEqual(1, ProceduralWorldAnalytics.ChunksUnloaded);
    }

    [Test]
    public void Analytics_LODDistributionContainsAllLevels()
    {
        ProceduralWorldAnalytics.TrackLODAssignment(LODLevel.LOD0);
        ProceduralWorldAnalytics.TrackLODAssignment(LODLevel.LOD2);
        var dist = ProceduralWorldAnalytics.GetLODDistribution();
        Assert.IsTrue(dist.ContainsKey(LODLevel.LOD0));
        Assert.IsTrue(dist.ContainsKey(LODLevel.LOD1));
        Assert.IsTrue(dist.ContainsKey(LODLevel.LOD2));
        Assert.IsTrue(dist.ContainsKey(LODLevel.LOD3));
        Assert.AreEqual(1, dist[LODLevel.LOD0]);
        Assert.AreEqual(0, dist[LODLevel.LOD1]);
        Assert.AreEqual(1, dist[LODLevel.LOD2]);
    }

    [Test]
    public void Analytics_Reset_ClearsAllCounters()
    {
        var city = new CityDescription { cityName = "C" };
        ProceduralWorldAnalytics.TrackCityGenerated(city);
        ProceduralWorldAnalytics.TrackLODAssignment(LODLevel.LOD0);
        ProceduralWorldAnalytics.Reset();
        Assert.AreEqual(0, ProceduralWorldAnalytics.CitiesGenerated);
        foreach (var entry in ProceduralWorldAnalytics.GetLODDistribution())
            Assert.AreEqual(0, entry.Value);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RealWorldCityMapper
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void RealWorldCityMapper_CityTypeFromPopulation_Metropolis()
    {
        var go = new GameObject();
        var mapper = go.AddComponent<RealWorldCityMapper>();
        Assert.AreEqual(CityType.Metropolis, mapper.CityTypeFromPopulation(5_000_000));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void RealWorldCityMapper_CityTypeFromPopulation_Town()
    {
        var go = new GameObject();
        var mapper = go.AddComponent<RealWorldCityMapper>();
        Assert.AreEqual(CityType.Town, mapper.CityTypeFromPopulation(100_000));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void RealWorldCityMapper_CityTypeFromPopulation_Village()
    {
        var go = new GameObject();
        var mapper = go.AddComponent<RealWorldCityMapper>();
        Assert.AreEqual(CityType.Village, mapper.CityTypeFromPopulation(500));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void RealWorldCityMapper_DensityFromPopulation_IsNormalised()
    {
        var go = new GameObject();
        var mapper = go.AddComponent<RealWorldCityMapper>();
        float d1 = mapper.DensityFromPopulation(1000);
        float d2 = mapper.DensityFromPopulation(1_000_000);
        Assert.GreaterOrEqual(d1, 0f);
        Assert.LessOrEqual(d1, 1f);
        Assert.Greater(d2, d1, "Larger population should have higher density");
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void RealWorldCityMapper_BuildingStyleFromClimate()
    {
        var go = new GameObject();
        var mapper = go.AddComponent<RealWorldCityMapper>();
        Assert.AreEqual("Tropical",  mapper.BuildingStyleFromClimate("tropical"));
        Assert.AreEqual("Desert",    mapper.BuildingStyleFromClimate("desert"));
        Assert.AreEqual("Temperate", mapper.BuildingStyleFromClimate("temperate"));
        Assert.AreEqual("Temperate", mapper.BuildingStyleFromClimate(null));
        Assert.AreEqual("Temperate", mapper.BuildingStyleFromClimate("unknown_zone"));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void RealWorldCityMapper_MapRealCity_CoastalFlag()
    {
        var go = new GameObject();
        var mapper = go.AddComponent<RealWorldCityMapper>();
        var city = mapper.MapRealCity("Sydney", 5_000_000, Vector3.zero, true, "oceanic");
        Assert.AreEqual(CityType.Coastal, city.cityType,
            "Coastal flag should override population-based type");
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ElevationMapper static methods
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ElevationMapper_MinElevation_ReturnsSmallest()
    {
        float[,] map = { { 10f, 5f }, { 20f, 3f } };
        Assert.AreEqual(3f, ElevationMapper.MinElevation(map), 0.001f);
    }

    [Test]
    public void ElevationMapper_MaxElevation_ReturnsLargest()
    {
        float[,] map = { { 10f, 5f }, { 20f, 3f } };
        Assert.AreEqual(20f, ElevationMapper.MaxElevation(map), 0.001f);
    }

    [Test]
    public void ElevationMapper_MinElevation_SingleElement()
    {
        float[,] map = { { 42f } };
        Assert.AreEqual(42f, ElevationMapper.MinElevation(map), 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RoadSegmentData
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void RoadSegmentData_LengthComputedFromEndpoints()
    {
        var seg = new RoadSegmentData
        {
            start = new Vector3(0f, 0f, 0f),
            end = new Vector3(300f, 0f, 400f),
            roadType = RoadType.Highway,
            widthMetres = 20f
        };
        Assert.AreEqual(500f, seg.lengthMetres, 0.01f, "3-4-5 triangle = 500 m");
    }

    [Test]
    public void RoadSegmentData_ZeroLengthSegment()
    {
        var seg = new RoadSegmentData
        {
            start = new Vector3(10f, 5f, 3f),
            end = new Vector3(10f, 5f, 3f),
            roadType = RoadType.Lane
        };
        Assert.AreEqual(0f, seg.lengthMetres, 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AirportDatabaseProvider (builtin data)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AirportDatabaseProvider_BuiltinHasEntries()
    {
        var go = new GameObject();
        var db = go.AddComponent<AirportDatabaseProvider>();
        // Awake is called automatically when AddComponent is called in EditMode
        // with RuntimeInitializeOnLoad — validate via public Count
        Assert.GreaterOrEqual(db.Count, 1, "Builtin database should have at least one entry");
        UnityEngine.Object.DestroyImmediate(go);
    }
}
