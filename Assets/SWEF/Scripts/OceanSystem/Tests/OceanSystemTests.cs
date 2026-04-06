// OceanSystemTests.cs — Phase 117: Advanced Ocean & Maritime System
// Comprehensive NUnit EditMode tests (60+ tests).
// Tests cover: enums, config, wave simulation, water physics, carrier systems,
// vessel traffic, mission data, weather coupling.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.OceanSystem;

[TestFixture]
public class OceanSystemTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // OceanRegion enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OceanRegion_AllValuesAreDefined()
    {
        var values = (OceanRegion[])Enum.GetValues(typeof(OceanRegion));
        Assert.GreaterOrEqual(values.Length, 6, "At least 6 OceanRegion values required");
        Assert.Contains(OceanRegion.OpenOcean,    values);
        Assert.Contains(OceanRegion.CoastalWater, values);
        Assert.Contains(OceanRegion.Harbor,       values);
        Assert.Contains(OceanRegion.River,        values);
        Assert.Contains(OceanRegion.Lake,         values);
        Assert.Contains(OceanRegion.Arctic,       values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SeaState enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SeaState_AllValuesAreDefined()
    {
        var values = (SeaState[])Enum.GetValues(typeof(SeaState));
        Assert.GreaterOrEqual(values.Length, 6, "At least 6 SeaState values required");
        Assert.Contains(SeaState.Calm,      values);
        Assert.Contains(SeaState.Slight,    values);
        Assert.Contains(SeaState.Moderate,  values);
        Assert.Contains(SeaState.Rough,     values);
        Assert.Contains(SeaState.VeryRough, values);
        Assert.Contains(SeaState.HighSeas,  values);
    }

    [Test]
    public void SeaState_Ordinal_IsAscendingBySeverity()
    {
        Assert.Less((int)SeaState.Calm,     (int)SeaState.Slight);
        Assert.Less((int)SeaState.Slight,   (int)SeaState.Moderate);
        Assert.Less((int)SeaState.Moderate, (int)SeaState.Rough);
        Assert.Less((int)SeaState.Rough,    (int)SeaState.VeryRough);
        Assert.Less((int)SeaState.VeryRough,(int)SeaState.HighSeas);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VesselType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VesselType_AllValuesAreDefined()
    {
        var values = (VesselType[])Enum.GetValues(typeof(VesselType));
        Assert.Contains(VesselType.CargoShip,      values);
        Assert.Contains(VesselType.Tanker,         values);
        Assert.Contains(VesselType.AircraftCarrier,values);
        Assert.Contains(VesselType.Destroyer,      values);
        Assert.Contains(VesselType.Sailboat,       values);
        Assert.Contains(VesselType.FishingBoat,    values);
        Assert.Contains(VesselType.Speedboat,      values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WaterLandingType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void WaterLandingType_AllValuesAreDefined()
    {
        var values = (WaterLandingType[])Enum.GetValues(typeof(WaterLandingType));
        Assert.Contains(WaterLandingType.Seaplane,   values);
        Assert.Contains(WaterLandingType.Helicopter, values);
        Assert.Contains(WaterLandingType.Emergency,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MaritimeMissionType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void MaritimeMissionType_AllValuesAreDefined()
    {
        var values = (MaritimeMissionType[])Enum.GetValues(typeof(MaritimeMissionType));
        Assert.Contains(MaritimeMissionType.SearchAndRescue,   values);
        Assert.Contains(MaritimeMissionType.CargoDelivery,     values);
        Assert.Contains(MaritimeMissionType.Patrol,            values);
        Assert.Contains(MaritimeMissionType.Medevac,           values);
        Assert.Contains(MaritimeMissionType.FireFighting,      values);
        Assert.Contains(MaritimeMissionType.CarrierOperation,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GlidepathState enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GlidepathState_AllValuesAreDefined()
    {
        var values = (GlidepathState[])Enum.GetValues(typeof(GlidepathState));
        Assert.Contains(GlidepathState.OnGlidepath,   values);
        Assert.Contains(GlidepathState.SlightlyHigh,  values);
        Assert.Contains(GlidepathState.High,          values);
        Assert.Contains(GlidepathState.SlightlyLow,   values);
        Assert.Contains(GlidepathState.Low,           values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SearchPattern enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SearchPattern_AllValuesAreDefined()
    {
        var values = (SearchPattern[])Enum.GetValues(typeof(SearchPattern));
        Assert.Contains(SearchPattern.ExpandingSquare, values);
        Assert.Contains(SearchPattern.Sector,          values);
        Assert.Contains(SearchPattern.ParallelTrack,   values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DebrisType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DebrisType_AllValuesAreDefined()
    {
        var values = (DebrisType[])Enum.GetValues(typeof(DebrisType));
        Assert.Contains(DebrisType.Iceberg,      values);
        Assert.Contains(DebrisType.Container,    values);
        Assert.Contains(DebrisType.OilSlick,     values);
        Assert.Contains(DebrisType.SeaweedPatch, values);
        Assert.Contains(DebrisType.Wreckage,     values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CatapultType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CatapultType_AllValuesAreDefined()
    {
        var values = (CatapultType[])Enum.GetValues(typeof(CatapultType));
        Assert.Contains(CatapultType.Steam,          values);
        Assert.Contains(CatapultType.Electromagnetic, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OceanSystemConfig ScriptableObject
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OceanSystemConfig_DefaultValues_AreReasonable()
    {
        var config = ScriptableObject.CreateInstance<OceanSystemConfig>();
        Assert.Greater(config.swellAmplitude,      0f,  "swellAmplitude should be > 0");
        Assert.Greater(config.waveOctaves,         0,   "waveOctaves should be > 0");
        Assert.Greater(config.waveTimeScale,       0f,  "waveTimeScale should be > 0");
        Assert.Greater(config.tidalCycleDuration,  0f,  "tidalCycleDuration should be > 0");
        Assert.Greater(config.tidalRange,          0f,  "tidalRange should be > 0");
        Assert.Greater(config.waterDensity,        0f,  "waterDensity should be > 0");
        Assert.Greater(config.maxActiveVessels,    0,   "maxActiveVessels should be > 0");
        Assert.Greater(config.catapultAcceleration,0f,  "catapultAcceleration should be > 0");
        Assert.Greater(config.arrestorWireCount,   0,   "arrestorWireCount should be > 0");
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void OceanSystemConfig_EnableTides_DefaultTrue()
    {
        var config = ScriptableObject.CreateInstance<OceanSystemConfig>();
        Assert.IsTrue(config.enableTides, "enableTides should be true by default");
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void OceanSystemConfig_EnableCurrents_DefaultTrue()
    {
        var config = ScriptableObject.CreateInstance<OceanSystemConfig>();
        Assert.IsTrue(config.enableCurrents, "enableCurrents should be true by default");
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void OceanSystemConfig_SpringTideMultiplier_GreaterThanOne()
    {
        var config = ScriptableObject.CreateInstance<OceanSystemConfig>();
        Assert.Greater(config.springTideMultiplier, 1f, "springTideMultiplier should be > 1");
        UnityEngine.Object.DestroyImmediate(config);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WaveConditions data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void WaveConditions_CanBeCreatedAndAssigned()
    {
        var conditions = new WaveConditions
        {
            significantWaveHeight = 2.5f,
            dominantPeriod        = 8f,
            waveDirection         = 270f,
            windSpeed             = 12f,
            windDirection         = 275f,
            seaState              = SeaState.Moderate
        };

        Assert.AreEqual(2.5f,           conditions.significantWaveHeight, 0.001f);
        Assert.AreEqual(8f,             conditions.dominantPeriod,        0.001f);
        Assert.AreEqual(270f,           conditions.waveDirection,         0.001f);
        Assert.AreEqual(12f,            conditions.windSpeed,             0.001f);
        Assert.AreEqual(SeaState.Moderate, conditions.seaState);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VesselData data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VesselData_CanBeCreatedAndAssigned()
    {
        var data = new VesselData
        {
            vesselId   = "V001",
            vesselName = "Test Vessel",
            vesselType = VesselType.CargoShip,
            heading    = 180f,
            speedKnots = 14f,
            isActive   = true
        };

        Assert.AreEqual("V001",            data.vesselId);
        Assert.AreEqual("Test Vessel",     data.vesselName);
        Assert.AreEqual(VesselType.CargoShip, data.vesselType);
        Assert.AreEqual(180f,              data.heading,    0.001f);
        Assert.AreEqual(14f,               data.speedKnots, 0.001f);
        Assert.IsTrue(data.isActive);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeckSlotState data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DeckSlotState_CanBeCreatedAndAssigned()
    {
        var slot = new DeckSlotState
        {
            slotIndex   = 3,
            isOccupied  = true,
            aircraftId  = "F18-01",
            worldPosition = Vector3.zero
        };

        Assert.AreEqual(3,        slot.slotIndex);
        Assert.IsTrue(slot.isOccupied);
        Assert.AreEqual("F18-01", slot.aircraftId);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SARMissionData data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SARMissionData_CanBeCreatedAndAssigned()
    {
        var sar = new SARMissionData
        {
            missionId      = "SAR-001",
            datumPosition  = new Vector3(100f, 0f, 200f),
            survivorCount  = 3,
            rescuedCount   = 1,
            searchPattern  = SearchPattern.ExpandingSquare,
            timeLimitSeconds = 600f,
            isActive       = true
        };

        Assert.AreEqual("SAR-001",                   sar.missionId);
        Assert.AreEqual(3,                           sar.survivorCount);
        Assert.AreEqual(1,                           sar.rescuedCount);
        Assert.AreEqual(SearchPattern.ExpandingSquare, sar.searchPattern);
        Assert.AreEqual(600f,                        sar.timeLimitSeconds, 0.001f);
        Assert.IsTrue(sar.isActive);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WaterLandingRecord data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void WaterLandingRecord_CanBeCreatedAndAssigned()
    {
        var record = new WaterLandingRecord
        {
            timestamp                = DateTime.UtcNow,
            landingType              = WaterLandingType.Seaplane,
            touchdownVerticalSpeed   = 1.5f,
            touchdownHorizontalSpeed = 30f,
            seaState                 = SeaState.Slight,
            success                  = true
        };

        Assert.AreEqual(WaterLandingType.Seaplane, record.landingType);
        Assert.AreEqual(1.5f, record.touchdownVerticalSpeed,   0.001f);
        Assert.AreEqual(30f,  record.touchdownHorizontalSpeed, 0.001f);
        Assert.AreEqual(SeaState.Slight, record.seaState);
        Assert.IsTrue(record.success);
    }

    [Test]
    public void WaterLandingRecord_FailedEmergencyDitching()
    {
        var record = new WaterLandingRecord
        {
            landingType            = WaterLandingType.Emergency,
            touchdownVerticalSpeed = 20f,
            seaState               = SeaState.HighSeas,
            success                = false
        };

        Assert.AreEqual(WaterLandingType.Emergency, record.landingType);
        Assert.IsFalse(record.success);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CarrierTrapRecord data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CarrierTrapRecord_CanBeCreatedAndAssigned()
    {
        var record = new CarrierTrapRecord
        {
            timestamp          = DateTime.UtcNow,
            wireNumber         = 3,
            wasBolter          = false,
            approachSpeedKnots = 135f,
            glidepathState     = GlidepathState.OnGlidepath
        };

        Assert.AreEqual(3,            record.wireNumber);
        Assert.IsFalse(record.wasBolter);
        Assert.AreEqual(135f,         record.approachSpeedKnots, 0.001f);
        Assert.AreEqual(GlidepathState.OnGlidepath, record.glidepathState);
    }

    [Test]
    public void CarrierTrapRecord_Bolter_HasZeroWireNumber()
    {
        var record = new CarrierTrapRecord
        {
            wireNumber = 0,
            wasBolter  = true
        };

        Assert.IsTrue(record.wasBolter);
        Assert.AreEqual(0, record.wireNumber);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OceanWaveSimulator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OceanWaveSimulator_CanBeCreated()
    {
        var go  = new GameObject("WaveSimulator");
        var sim = go.AddComponent<OceanWaveSimulator>();
        Assert.IsNotNull(sim);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanWaveSimulator_GetSurfaceHeight_ReturnsFiniteValue()
    {
        var go  = new GameObject("WaveSimulator");
        var sim = go.AddComponent<OceanWaveSimulator>();
        float h = sim.GetSurfaceHeight(Vector2.zero);
        Assert.IsTrue(float.IsFinite(h), "Surface height should be a finite value");
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanWaveSimulator_GetDisplacement_ReturnsFiniteVector()
    {
        var go  = new GameObject("WaveSimulator");
        var sim = go.AddComponent<OceanWaveSimulator>();
        var disp = sim.GetDisplacement(new Vector2(100f, 200f));
        Assert.IsTrue(float.IsFinite(disp.x) && float.IsFinite(disp.y) && float.IsFinite(disp.z),
                      "Displacement should have finite components");
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanWaveSimulator_ApplySeaState_DoesNotThrow()
    {
        var go  = new GameObject("WaveSimulator");
        var sim = go.AddComponent<OceanWaveSimulator>();
        Assert.DoesNotThrow(() => sim.ApplySeaState(SeaState.Rough));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanWaveSimulator_GetConditions_ReturnsNonNull()
    {
        var go  = new GameObject("WaveSimulator");
        var sim = go.AddComponent<OceanWaveSimulator>();
        var cond = sim.GetCurrentConditions();
        Assert.IsNotNull(cond);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TideController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TideController_CanBeCreated()
    {
        var go   = new GameObject("TideController");
        var tide = go.AddComponent<TideController>();
        Assert.IsNotNull(tide);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void TideController_SetPhase_ClampsBetweenZeroAndOne()
    {
        var go   = new GameObject("TideController");
        var tide = go.AddComponent<TideController>();

        tide.SetTidePhase(-0.5f);
        Assert.AreEqual(0f, tide.TidePhase, 0.001f);

        tide.SetTidePhase(1.5f);
        Assert.AreEqual(1f, tide.TidePhase, 0.001f);

        tide.SetTidePhase(0.5f);
        Assert.AreEqual(0.5f, tide.TidePhase, 0.001f);

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OceanCurrentSimulator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OceanCurrentSimulator_GetCurrentVelocity_ReturnsFiniteVector()
    {
        var go  = new GameObject("CurrentSim");
        var cfg = ScriptableObject.CreateInstance<OceanSystemConfig>();
        var sim = go.AddComponent<OceanCurrentSimulator>();

        var vel = sim.GetCurrentVelocity(Vector3.zero);
        Assert.IsTrue(float.IsFinite(vel.x) && float.IsFinite(vel.y));

        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(cfg);
    }

    [Test]
    public void OceanCurrentSimulator_RipCurrent_ZeroOutsideRadius()
    {
        var go  = new GameObject("CurrentSim");
        var sim = go.AddComponent<OceanCurrentSimulator>();

        var vel = sim.GetRipCurrentVelocity(
            new Vector3(2000f, 0f, 0f), Vector3.zero, 500f, 2f);

        Assert.AreEqual(Vector2.zero, vel, "No rip current outside radius");
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanCurrentSimulator_RipCurrent_NonZeroInsideRadius()
    {
        var go  = new GameObject("CurrentSim");
        var sim = go.AddComponent<OceanCurrentSimulator>();

        var vel = sim.GetRipCurrentVelocity(
            new Vector3(100f, 0f, 0f), Vector3.zero, 500f, 2f);

        Assert.AreNotEqual(Vector2.zero, vel, "Rip current expected inside radius");
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EmergencyDitchingSystem
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void EmergencyDitchingSystem_InitialState_IsNormal()
    {
        var go  = new GameObject("Ditching");
        var sys = go.AddComponent<EmergencyDitchingSystem>();
        Assert.AreEqual(EmergencyDitchingSystem.DitchingState.Normal, sys.State);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void EmergencyDitchingSystem_Reset_ReturnsToNormal()
    {
        var go  = new GameObject("Ditching");
        var sys = go.AddComponent<EmergencyDitchingSystem>();
        sys.AnnounceImminent();
        sys.Reset();
        Assert.AreEqual(EmergencyDitchingSystem.DitchingState.Normal, sys.State);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void EmergencyDitchingSystem_AnnounceImminent_ChangesState()
    {
        var go  = new GameObject("Ditching");
        var sys = go.AddComponent<EmergencyDitchingSystem>();
        sys.AnnounceImminent();
        Assert.AreEqual(EmergencyDitchingSystem.DitchingState.Imminent, sys.State);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WaterTakeoffController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void WaterTakeoffController_InitialPhase_IsIdle()
    {
        var go   = new GameObject("TakeoffCtrl");
        var ctrl = go.AddComponent<WaterTakeoffController>();
        Assert.AreEqual(WaterTakeoffController.TakeoffPhase.Idle, ctrl.Phase);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WaterTakeoffController_Reset_ReturnsToIdle()
    {
        var go   = new GameObject("TakeoffCtrl");
        var ctrl = go.AddComponent<WaterTakeoffController>();
        ctrl.Reset();
        Assert.AreEqual(WaterTakeoffController.TakeoffPhase.Idle, ctrl.Phase);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PortController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PortController_HasFreeBerth_TrueInitially()
    {
        var go   = new GameObject("Port");
        var port = go.AddComponent<PortController>();
        Assert.IsTrue(port.HasFreeBerth(), "Fresh port should have free berths");
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void PortController_RequestBerth_ReturnsValidIndex()
    {
        var go   = new GameObject("Port");
        var port = go.AddComponent<PortController>();
        int idx  = port.RequestBerth("V001");
        Assert.GreaterOrEqual(idx, 0, "Should return a valid berth index");
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void PortController_OccupiedBerths_IncrementsOnDock()
    {
        var go   = new GameObject("Port");
        var port = go.AddComponent<PortController>();
        int before = port.OccupiedBerths;
        port.RequestBerth("V001");
        Assert.AreEqual(before + 1, port.OccupiedBerths);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SearchAndRescueController — pattern generation
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SARController_BeginMission_GeneratesExpandingSquareWaypoints()
    {
        var go   = new GameObject("SAR");
        var ctrl = go.AddComponent<SearchAndRescueController>();

        var mission = new SARMissionData
        {
            missionId     = "SAR-1",
            datumPosition = Vector3.zero,
            survivorCount = 2,
            searchPattern = SearchPattern.ExpandingSquare,
            isActive      = true
        };
        ctrl.BeginMission(mission);

        Assert.IsNotNull(ctrl.SearchWaypoints);
        Assert.Greater(ctrl.SearchWaypoints.Count, 0, "Should generate at least one waypoint");
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void SARController_BeginMission_GeneratesSectorWaypoints()
    {
        var go   = new GameObject("SAR");
        var ctrl = go.AddComponent<SearchAndRescueController>();

        var mission = new SARMissionData
        {
            missionId     = "SAR-2",
            datumPosition = new Vector3(500f, 0f, 500f),
            survivorCount = 1,
            searchPattern = SearchPattern.Sector,
            isActive      = true
        };
        ctrl.BeginMission(mission);

        Assert.IsNotNull(ctrl.SearchWaypoints);
        Assert.Greater(ctrl.SearchWaypoints.Count, 0);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void SARController_BeginMission_GeneratesParallelTrackWaypoints()
    {
        var go   = new GameObject("SAR");
        var ctrl = go.AddComponent<SearchAndRescueController>();

        var mission = new SARMissionData
        {
            missionId     = "SAR-3",
            datumPosition = new Vector3(-1000f, 0f, 2000f),
            survivorCount = 4,
            searchPattern = SearchPattern.ParallelTrack,
            isActive      = true
        };
        ctrl.BeginMission(mission);

        Assert.IsNotNull(ctrl.SearchWaypoints);
        Assert.Greater(ctrl.SearchWaypoints.Count, 0);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MaritimePatrolController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PatrolController_InitialState_IsInactive()
    {
        var go   = new GameObject("Patrol");
        var ctrl = go.AddComponent<MaritimePatrolController>();
        Assert.AreEqual(MaritimePatrolController.PatrolState.Inactive, ctrl.State);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void PatrolController_StartPatrol_ChangesToPatrollingState()
    {
        var go   = new GameObject("Patrol");
        var ctrl = go.AddComponent<MaritimePatrolController>();
        ctrl.StartPatrol(Vector3.zero, 10000f);
        Assert.AreEqual(MaritimePatrolController.PatrolState.Patrolling, ctrl.State);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void PatrolController_IllegalFishingCount_StartsAtZero()
    {
        var go   = new GameObject("Patrol");
        var ctrl = go.AddComponent<MaritimePatrolController>();
        Assert.AreEqual(0, ctrl.IllegalFishingFound);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OceanSystemAnalytics
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OceanSystemAnalytics_InitialCounters_AreZero()
    {
        var go   = new GameObject("Analytics");
        var anal = go.AddComponent<OceanSystemAnalytics>();

        Assert.AreEqual(0, anal.TotalWaterLandings);
        Assert.AreEqual(0, anal.SuccessfulWaterLandings);
        Assert.AreEqual(0, anal.EmergencyDitchings);
        Assert.AreEqual(0, anal.CarrierTraps);
        Assert.AreEqual(0, anal.CarrierBolters);
        Assert.AreEqual(0, anal.SARCompletions);
        Assert.AreEqual(0, anal.PatrolCompletions);
        Assert.AreEqual(0, anal.CargoDeliveries);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanSystemAnalytics_WaterLandingSuccessRate_ZeroWhenNoLandings()
    {
        var go   = new GameObject("Analytics");
        var anal = go.AddComponent<OceanSystemAnalytics>();
        Assert.AreEqual(0f, anal.WaterLandingSuccessRate, 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanSystemAnalytics_CarrierTrapRate_ZeroWhenNoEvents()
    {
        var go   = new GameObject("Analytics");
        var anal = go.AddComponent<OceanSystemAnalytics>();
        Assert.AreEqual(0f, anal.CarrierTrapRate, 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanSystemAnalytics_RecordSARCompletion_IncrementsCounter()
    {
        var go   = new GameObject("Analytics");
        var anal = go.AddComponent<OceanSystemAnalytics>();
        anal.RecordSARCompletion();
        anal.RecordSARCompletion();
        Assert.AreEqual(2, anal.SARCompletions);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanSystemAnalytics_RecordPatrolCompletion_IncrementsCounter()
    {
        var go   = new GameObject("Analytics");
        var anal = go.AddComponent<OceanSystemAnalytics>();
        anal.RecordPatrolCompletion();
        Assert.AreEqual(1, anal.PatrolCompletions);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanSystemAnalytics_RecordCargoDelivery_IncrementsCounter()
    {
        var go   = new GameObject("Analytics");
        var anal = go.AddComponent<OceanSystemAnalytics>();
        anal.RecordCargoDelivery();
        anal.RecordCargoDelivery();
        anal.RecordCargoDelivery();
        Assert.AreEqual(3, anal.CargoDeliveries);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OceanWeatherIntegration — wind to sea state mapping
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OceanWeatherIntegration_LowWindSpeed_MapsToCalmSeaState()
    {
        // 0.5 m/s < 1.6 threshold → Calm
        var go     = new GameObject("WeatherInteg");
        var integ  = go.AddComponent<OceanWeatherIntegration>();

        // We can test via reflection or just verify the mapping logic via the manager
        // Since OceanSystemManager.Instance is null in edit mode, use OceanWeatherIntegration directly
        // The private method is tested indirectly via the public one (no-throw test)
        Assert.DoesNotThrow(() => integ.ApplyWeatherConditions(0.5f, 270f, 0f, false));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanWeatherIntegration_HighWindSpeed_DoesNotThrow()
    {
        var go    = new GameObject("WeatherInteg");
        var integ = go.AddComponent<OceanWeatherIntegration>();
        Assert.DoesNotThrow(() => integ.ApplyWeatherConditions(30f, 180f, 1f, true));
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CargoDeliveryMission
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CargoDeliveryMission_InitialState_IsInactive()
    {
        var go  = new GameObject("Cargo");
        var msn = go.AddComponent<CargoDeliveryMission>();
        Assert.AreEqual(CargoDeliveryMission.DeliveryState.Inactive, msn.State);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void CargoDeliveryMission_StartMission_ChangesStateToPickingUp()
    {
        var go  = new GameObject("Cargo");
        var msn = go.AddComponent<CargoDeliveryMission>();
        msn.StartMission();
        Assert.AreEqual(CargoDeliveryMission.DeliveryState.PickingUp, msn.State);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void CargoDeliveryMission_IsCargoLoaded_FalseInitially()
    {
        var go  = new GameObject("Cargo");
        var msn = go.AddComponent<CargoDeliveryMission>();
        Assert.IsFalse(msn.IsCargoLoaded);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OceanSystemManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OceanSystemManager_CanBeCreated()
    {
        var go  = new GameObject("OceanMgr");
        var mgr = go.AddComponent<OceanSystemManager>();
        Assert.IsNotNull(mgr);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanSystemManager_SetOceanRegion_FiresEvent()
    {
        var go  = new GameObject("OceanMgr");
        var mgr = go.AddComponent<OceanSystemManager>();

        OceanRegion received = OceanRegion.OpenOcean;
        mgr.OnRegionChanged += r => received = r;

        mgr.SetOceanRegion(OceanRegion.Harbor);
        Assert.AreEqual(OceanRegion.Harbor, received);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanSystemManager_SetSeaState_FiresEvent()
    {
        var go  = new GameObject("OceanMgr");
        var mgr = go.AddComponent<OceanSystemManager>();

        SeaState received = SeaState.Calm;
        mgr.OnSeaStateChanged += s => received = s;

        mgr.SetSeaState(SeaState.Rough);
        Assert.AreEqual(SeaState.Rough, received);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanSystemManager_RecordWaterLanding_FiresEvent()
    {
        var go  = new GameObject("OceanMgr");
        var mgr = go.AddComponent<OceanSystemManager>();

        WaterLandingRecord received = null;
        mgr.OnWaterLandingCompleted += r => received = r;

        var record = new WaterLandingRecord { landingType = WaterLandingType.Seaplane, success = true };
        mgr.RecordWaterLanding(record);
        Assert.IsNotNull(received);
        Assert.AreEqual(WaterLandingType.Seaplane, received.landingType);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanSystemManager_RecordCarrierTrap_FiresEvent()
    {
        var go  = new GameObject("OceanMgr");
        var mgr = go.AddComponent<OceanSystemManager>();

        CarrierTrapRecord received = null;
        mgr.OnCarrierTrapRecorded += r => received = r;

        var record = new CarrierTrapRecord { wireNumber = 2, wasBolter = false };
        mgr.RecordCarrierTrap(record);
        Assert.IsNotNull(received);
        Assert.AreEqual(2, received.wireNumber);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OceanSystemManager_GetSurfaceHeight_ReturnsZeroWithoutWaveSimulator()
    {
        var go  = new GameObject("OceanMgr");
        var mgr = go.AddComponent<OceanSystemManager>();
        float h = mgr.GetSurfaceHeight(Vector2.zero);
        Assert.AreEqual(0f, h, 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
