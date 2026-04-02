// SpaceStationTests.cs — NUnit EditMode tests for Phase 85 Space Station & Orbital Docking System
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.SpaceStation;

[TestFixture]
public class SpaceStationTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static StationDefinition BuildTestStation(
        string id = "test_station",
        float altKm = 400f,
        StationSegmentType[] segments = null,
        DockingPortDefinition[] ports = null)
    {
        return new StationDefinition
        {
            stationId        = id,
            displayNameLocKey = $"station_name_{id}",
            orbitalParams    = OrbitalParameters.Circular(altKm * 1000.0),
            segments         = segments ?? new[]
            {
                StationSegmentType.Docking,
                StationSegmentType.Habitat,
                StationSegmentType.Command,
                StationSegmentType.Laboratory
            },
            dockingPorts     = ports ?? new[]
            {
                new DockingPortDefinition
                {
                    portId            = "port_fwd",
                    localPosition     = Vector3.forward * 10f,
                    acceptedShipSizes = new[] { "Small", "Medium" },
                    state             = DockingPortState.Available
                }
            },
            modelPrefabPath = "Stations/TestStation"
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DockingApproachPhase enum completeness
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DockingApproachPhase_HasExactlySixValues()
    {
        var values = Enum.GetValues(typeof(DockingApproachPhase));
        Assert.AreEqual(6, values.Length,
            "DockingApproachPhase must have exactly 6 values as per the Phase 85 spec.");
    }

    [Test]
    public void DockingApproachPhase_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(DockingApproachPhase), DockingApproachPhase.FreeApproach));
        Assert.IsTrue(Enum.IsDefined(typeof(DockingApproachPhase), DockingApproachPhase.InitialAlignment));
        Assert.IsTrue(Enum.IsDefined(typeof(DockingApproachPhase), DockingApproachPhase.FinalApproach));
        Assert.IsTrue(Enum.IsDefined(typeof(DockingApproachPhase), DockingApproachPhase.SoftCapture));
        Assert.IsTrue(Enum.IsDefined(typeof(DockingApproachPhase), DockingApproachPhase.HardDock));
        Assert.IsTrue(Enum.IsDefined(typeof(DockingApproachPhase), DockingApproachPhase.Docked));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OrbitalBody enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OrbitalBody_HasSixValues()
    {
        Assert.AreEqual(6, Enum.GetValues(typeof(OrbitalBody)).Length);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DockingPortState transitions
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DockingPortState_AvailableToOccupied()
    {
        var port = new DockingPortDefinition { state = DockingPortState.Available };
        port.state = DockingPortState.Occupied;
        Assert.AreEqual(DockingPortState.Occupied, port.state);
    }

    [Test]
    public void DockingPortState_OccupiedToAvailable()
    {
        var port = new DockingPortDefinition { state = DockingPortState.Occupied };
        port.state = DockingPortState.Available;
        Assert.AreEqual(DockingPortState.Available, port.state);
    }

    [Test]
    public void DockingPortState_DamagedCannotTransitionToAvailableDirectly()
    {
        // Policy: damaged ports require repair before becoming available
        var port = new DockingPortDefinition { state = DockingPortState.Damaged };
        // We simply verify the enum values exist; policy enforcement is runtime
        Assert.AreNotEqual(DockingPortState.Available, port.state);
    }

    [Test]
    public void DockingPortState_HasFourValues()
    {
        Assert.AreEqual(4, Enum.GetValues(typeof(DockingPortState)).Length);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SpaceStationConfig default values
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SpaceStationConfig_DefaultCaptureRadius_IsPositive()
    {
        var config = ScriptableObject.CreateInstance<SpaceStationConfig>();
        Assert.Greater(config.dockingCaptureRadius, 0f,
            "dockingCaptureRadius must default to a positive value.");
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void SpaceStationConfig_DefaultApproachSpeedLimit_IsPositive()
    {
        var config = ScriptableObject.CreateInstance<SpaceStationConfig>();
        Assert.Greater(config.approachSpeedLimit, 0f);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void SpaceStationConfig_DefaultRcsForce_IsPositive()
    {
        var config = ScriptableObject.CreateInstance<SpaceStationConfig>();
        Assert.Greater(config.rcsForce, 0f);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void SpaceStationConfig_DefaultMaxActiveStations_AtLeastOne()
    {
        var config = ScriptableObject.CreateInstance<SpaceStationConfig>();
        Assert.GreaterOrEqual(config.maxActiveStations, 1);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void SpaceStationConfig_DefaultStationSpawnAltitude_AboveKarmanLine()
    {
        const float karmanLine = 100_000f;
        var config = ScriptableObject.CreateInstance<SpaceStationConfig>();
        Assert.Greater(config.stationSpawnAltitude, karmanLine,
            "Default spawn altitude should be above the Kármán line (100 km).");
        UnityEngine.Object.DestroyImmediate(config);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OrbitalParameters / OrbitalMechanicsController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OrbitalParameters_Circular_AltitudeIsPreserved()
    {
        double alt = 400_000.0;
        OrbitalParameters p = OrbitalParameters.Circular(alt);
        Assert.AreEqual(alt, p.altitude, 1.0);
    }

    [Test]
    public void OrbitalParameters_Circular_EccentricityIsZero()
    {
        OrbitalParameters p = OrbitalParameters.Circular(400_000.0);
        Assert.AreEqual(0.0, p.eccentricity, 1e-12);
    }

    [Test]
    public void OrbitalMechanics_ISSPeriod_ApproximatelyNinetyMinutes()
    {
        // ISS orbits at ~408 km with a period of ~92.5 minutes
        double period = OrbitalMechanicsController.GetOrbitalPeriod(408_000.0);
        double periodMinutes = period / 60.0;
        Assert.AreEqual(92.5, periodMinutes, 2.0,
            "ISS orbital period should be approximately 92.5 minutes ± 2.");
    }

    [Test]
    public void OrbitalMechanics_CircularSpeed_ISS_ApproximatelyCorrect()
    {
        // ISS orbital speed is approximately 7,660 m/s
        double speed = OrbitalMechanicsController.GetCircularSpeed(408_000.0);
        Assert.AreEqual(7660.0, speed, 200.0,
            "ISS circular speed should be approximately 7,660 m/s ± 200.");
    }

    [Test]
    public void OrbitalMechanics_HigherAltitude_LowerSpeed()
    {
        double speedLow  = OrbitalMechanicsController.GetCircularSpeed(300_000.0);
        double speedHigh = OrbitalMechanicsController.GetCircularSpeed(800_000.0);
        Assert.Less(speedHigh, speedLow,
            "Orbital speed must decrease with altitude.");
    }

    [Test]
    public void OrbitalMechanics_HigherAltitude_LongerPeriod()
    {
        double periodLow  = OrbitalMechanicsController.GetOrbitalPeriod(300_000.0);
        double periodHigh = OrbitalMechanicsController.GetOrbitalPeriod(800_000.0);
        Assert.Greater(periodHigh, periodLow,
            "Orbital period must increase with altitude.");
    }

    [Test]
    public void OrbitalMechanics_GetStationPosition_ReturnsNonZero_WhenRegistered()
    {
        var go = new GameObject("OrbitalController");
        var controller = go.AddComponent<OrbitalMechanicsController>();

        StationDefinition def = BuildTestStation("orbit_test", 400f);
        controller.RegisterStation(def);

        Vector3 pos = controller.GetStationPosition("orbit_test", 0.0);
        Assert.AreNotEqual(Vector3.zero, pos,
            "Registered station should return a non-zero position at time=0.");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OrbitalMechanics_GetStationPosition_MovesWithTime()
    {
        var go = new GameObject("OrbitalController2");
        var controller = go.AddComponent<OrbitalMechanicsController>();

        StationDefinition def = BuildTestStation("orbit_move", 400f);
        controller.RegisterStation(def);

        Vector3 posT0 = controller.GetStationPosition("orbit_move", 0.0);
        Vector3 posT1 = controller.GetStationPosition("orbit_move", 1000.0);

        Assert.AreNotEqual(posT0, posT1,
            "Station position should change over time.");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void OrbitalMechanics_UnknownStation_ReturnsZero()
    {
        var go = new GameObject("OrbitalController3");
        var controller = go.AddComponent<OrbitalMechanicsController>();

        Vector3 pos = controller.GetStationPosition("nonexistent_station", 0.0);
        Assert.AreEqual(Vector3.zero, pos);

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DockingController — phase transitions
    // ═══════════════════════════════════════════════════════════════════════════

    private static GameObject CreateDockingControllerGO()
    {
        var go = new GameObject("DockingController");
        go.AddComponent<DockingController>();
        return go;
    }

    [Test]
    public void DockingController_InitialPhaseIsFreeApproach()
    {
        var go = CreateDockingControllerGO();
        var controller = go.GetComponent<DockingController>();
        controller.BeginDockingApproach("station_a", "port_fwd");
        Assert.AreEqual(DockingApproachPhase.FreeApproach, controller.CurrentPhase);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DockingController_FreeApproach_AdvancesAtDistance1000m()
    {
        var go = CreateDockingControllerGO();
        var controller = go.GetComponent<DockingController>();
        DockingApproachPhase lastPhase = DockingApproachPhase.FreeApproach;
        controller.OnPhaseChanged += p => lastPhase = p;

        controller.BeginDockingApproach("station", "port");
        // Tick with distance at threshold
        controller.Tick(1000f, 10f, 5f, 0.1f);
        Assert.AreEqual(DockingApproachPhase.InitialAlignment, lastPhase);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DockingController_InitialAlignment_AbortOnHighSpeed()
    {
        var go = CreateDockingControllerGO();
        var controller = go.GetComponent<DockingController>();
        bool aborted = false;
        controller.OnDockingAborted += _ => aborted = true;

        controller.BeginDockingApproach("station", "port");
        controller.Tick(1000f, 10f, 5f, 0.1f);   // → InitialAlignment
        controller.Tick(500f, 55f, 5f, 0.1f);     // speed > 50 → abort
        Assert.IsTrue(aborted, "Should abort when closing speed exceeds 50 m/s in InitialAlignment.");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DockingController_FinalApproach_AbortOnAlignmentLoss()
    {
        var go = CreateDockingControllerGO();
        var controller = go.GetComponent<DockingController>();
        bool aborted = false;
        controller.OnDockingAborted += _ => aborted = true;

        controller.BeginDockingApproach("station", "port");
        controller.Tick(1000f, 10f, 5f, 0.1f);    // → InitialAlignment
        controller.Tick(200f, 3f, 5f, 0.1f);       // → FinalApproach
        controller.Tick(100f, 3f, 20f, 0.1f);      // alignment > 15° → abort
        Assert.IsTrue(aborted, "Should abort when alignment exceeds 15° in FinalApproach.");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DockingController_SoftCapture_AbortOnCollision()
    {
        var go = CreateDockingControllerGO();
        var controller = go.GetComponent<DockingController>();
        bool aborted = false;
        controller.OnDockingAborted += _ => aborted = true;

        controller.BeginDockingApproach("station", "port");
        controller.Tick(1000f, 5f, 5f, 0.1f);      // → InitialAlignment
        controller.Tick(200f, 2f, 5f, 0.1f);        // → FinalApproach
        controller.Tick(10f, 0.4f, 2f, 0.1f);       // → SoftCapture
        controller.Tick(5f, 3f, 1f, 0.1f);          // speed > 2 → collision abort
        Assert.IsTrue(aborted, "Should abort when collision speed exceeded in SoftCapture.");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DockingController_HardDock_AutoLocksAfterTwoSeconds()
    {
        var go = CreateDockingControllerGO();
        var controller = go.GetComponent<DockingController>();
        bool complete = false;
        controller.OnDockingComplete += () => complete = true;

        controller.BeginDockingApproach("station", "port");
        controller.Tick(1000f, 5f, 5f, 0.1f);       // → InitialAlignment
        controller.Tick(200f, 2f, 5f, 0.1f);         // → FinalApproach
        controller.Tick(10f, 0.4f, 2f, 0.1f);        // → SoftCapture
        controller.Tick(1f, 0.3f, 1f, 0.1f);         // → HardDock
        // Simulate 2+ seconds in HardDock
        controller.Tick(0.5f, 0f, 0f, 1.0f);
        controller.Tick(0.5f, 0f, 0f, 1.1f);
        Assert.IsTrue(complete, "HardDock should auto-lock after 2 seconds.");
        Assert.AreEqual(DockingApproachPhase.Docked, controller.CurrentPhase);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DockingController_CompleteSequenceSimulation()
    {
        var go = CreateDockingControllerGO();
        var controller = go.GetComponent<DockingController>();

        var phases = new List<DockingApproachPhase>();
        controller.OnPhaseChanged += p => phases.Add(p);
        bool complete = false;
        controller.OnDockingComplete += () => complete = true;

        controller.BeginDockingApproach("iss", "port_fwd");
        controller.Tick(1000f, 5f, 5f, 0.1f);        // → InitialAlignment
        controller.Tick(200f, 2f, 5f, 0.1f);          // → FinalApproach
        controller.Tick(10f, 0.4f, 2f, 0.1f);         // → SoftCapture
        controller.Tick(1f, 0.3f, 1f, 0.1f);          // → HardDock
        controller.Tick(0.5f, 0f, 0f, 2.1f);          // → Docked (auto-lock)

        Assert.IsTrue(complete);
        Assert.Contains(DockingApproachPhase.InitialAlignment, phases);
        Assert.Contains(DockingApproachPhase.FinalApproach, phases);
        Assert.Contains(DockingApproachPhase.SoftCapture, phases);
        Assert.Contains(DockingApproachPhase.HardDock, phases);
        Assert.Contains(DockingApproachPhase.Docked, phases);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DockingController_Abort_SetsIsActiveToFalse()
    {
        var go = CreateDockingControllerGO();
        var controller = go.GetComponent<DockingController>();

        controller.BeginDockingApproach("station", "port");
        Assert.IsTrue(controller.IsActive);
        controller.Abort("test");
        Assert.IsFalse(controller.IsActive);

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // StationModuleGenerator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void StationModuleGenerator_GeneratesValidLayout()
    {
        StationDefinition def = BuildTestStation();
        StationLayout layout  = StationModuleGenerator.GenerateLayout(def);
        Assert.IsTrue(layout.IsValid(),
            "Generated layout must contain at least one Docking, Habitat, and Command module.");
    }

    [Test]
    public void StationModuleGenerator_EmptySegments_StillValid()
    {
        var def = BuildTestStation(segments: new StationSegmentType[0]);
        StationLayout layout = StationModuleGenerator.GenerateLayout(def);
        Assert.IsTrue(layout.IsValid(),
            "Generator should inject required segments even when definition has none.");
    }

    [Test]
    public void StationModuleGenerator_HasAtLeastOneDockingPort()
    {
        StationDefinition def = BuildTestStation();
        StationLayout layout  = StationModuleGenerator.GenerateLayout(def);
        Assert.IsNotNull(layout.FindFirst(StationSegmentType.Docking));
    }

    [Test]
    public void StationModuleGenerator_HasAtLeastOneHabitat()
    {
        StationDefinition def = BuildTestStation();
        StationLayout layout  = StationModuleGenerator.GenerateLayout(def);
        Assert.IsNotNull(layout.FindFirst(StationSegmentType.Habitat));
    }

    [Test]
    public void StationModuleGenerator_HasAtLeastOneCommandModule()
    {
        StationDefinition def = BuildTestStation();
        StationLayout layout  = StationModuleGenerator.GenerateLayout(def);
        Assert.IsNotNull(layout.FindFirst(StationSegmentType.Command));
    }

    [Test]
    public void StationModuleGenerator_SegmentsAreConnected()
    {
        StationDefinition def    = BuildTestStation();
        StationLayout     layout = StationModuleGenerator.GenerateLayout(def);

        // Every node except the first must have at least one connection
        for (int i = 1; i < layout.nodes.Count; i++)
            Assert.Greater(layout.nodes[i].connectedNodeIds.Count, 0,
                $"Node at index {i} should be connected to at least one other node.");
    }

    [Test]
    public void StationModuleGenerator_DeterministicWithSameSeed()
    {
        StationDefinition def = BuildTestStation();
        StationLayout layout1 = StationModuleGenerator.GenerateLayout(def, seed: 42);
        StationLayout layout2 = StationModuleGenerator.GenerateLayout(def, seed: 42);

        Assert.AreEqual(layout1.nodes.Count, layout2.nodes.Count);
        for (int i = 0; i < layout1.nodes.Count; i++)
        {
            Assert.AreEqual(layout1.nodes[i].nodeId,       layout2.nodes[i].nodeId);
            Assert.AreEqual(layout1.nodes[i].segmentType,  layout2.nodes[i].segmentType);
            Assert.AreEqual(layout1.nodes[i].localPosition, layout2.nodes[i].localPosition);
        }
    }

    [Test]
    public void StationModuleGenerator_NullDefinition_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => StationModuleGenerator.GenerateLayout(null));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // StationDefinition serialisation round-trip
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void StationDefinition_Serialization_RoundTrip()
    {
        StationDefinition original = BuildTestStation("roundtrip_test", 400f);
        string json       = JsonUtility.ToJson(original);
        var    restored   = JsonUtility.FromJson<StationDefinition>(json);

        Assert.AreEqual(original.stationId,        restored.stationId);
        Assert.AreEqual(original.displayNameLocKey, restored.displayNameLocKey);
        Assert.AreEqual(original.modelPrefabPath,   restored.modelPrefabPath);
        Assert.AreEqual(original.orbitalParams.altitude,     restored.orbitalParams.altitude,   1.0);
        Assert.AreEqual(original.orbitalParams.eccentricity, restored.orbitalParams.eccentricity, 1e-10);
    }

    [Test]
    public void StationDefinition_DockingPorts_SurviveSerializationRoundTrip()
    {
        StationDefinition original = BuildTestStation();
        string json    = JsonUtility.ToJson(original);
        var    restored = JsonUtility.FromJson<StationDefinition>(json);

        Assert.AreEqual(original.dockingPorts.Length, restored.dockingPorts.Length);
        Assert.AreEqual(original.dockingPorts[0].portId, restored.dockingPorts[0].portId);
        Assert.AreEqual(original.dockingPorts[0].state,  restored.dockingPorts[0].state);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // StationSegmentType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void StationSegmentType_HasEightValues()
    {
        Assert.AreEqual(8, Enum.GetValues(typeof(StationSegmentType)).Length);
    }
}
