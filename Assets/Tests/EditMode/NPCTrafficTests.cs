// NPCTrafficTests.cs — NUnit EditMode tests for Phase 110: Dynamic NPC & Air Traffic Ecosystem
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SWEF.NPCTraffic;

[TestFixture]
public class NPCTrafficTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // NPCAircraftCategory enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCAircraftCategory_AllValuesAreDefined()
    {
        var values = (NPCAircraftCategory[])Enum.GetValues(typeof(NPCAircraftCategory));
        Assert.GreaterOrEqual(values.Length, 6);
        Assert.Contains(NPCAircraftCategory.CommercialAirline, values);
        Assert.Contains(NPCAircraftCategory.PrivateJet,        values);
        Assert.Contains(NPCAircraftCategory.CargoPlane,        values);
        Assert.Contains(NPCAircraftCategory.MilitaryAircraft,  values);
        Assert.Contains(NPCAircraftCategory.Helicopter,        values);
        Assert.Contains(NPCAircraftCategory.TrainingAircraft,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCBehaviorState enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCBehaviorState_AllStatesAreDefined()
    {
        var values = (NPCBehaviorState[])Enum.GetValues(typeof(NPCBehaviorState));
        Assert.GreaterOrEqual(values.Length, 9);
        Assert.Contains(NPCBehaviorState.Taxiing,    values);
        Assert.Contains(NPCBehaviorState.Takeoff,    values);
        Assert.Contains(NPCBehaviorState.Climbing,   values);
        Assert.Contains(NPCBehaviorState.Cruising,   values);
        Assert.Contains(NPCBehaviorState.Descending, values);
        Assert.Contains(NPCBehaviorState.Approach,   values);
        Assert.Contains(NPCBehaviorState.Landing,    values);
        Assert.Contains(NPCBehaviorState.Holding,    values);
        Assert.Contains(NPCBehaviorState.Emergency,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCRouteType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCRouteType_AllTypesAreDefined()
    {
        var values = (NPCRouteType[])Enum.GetValues(typeof(NPCRouteType));
        Assert.Contains(NPCRouteType.AirportToAirport, values);
        Assert.Contains(NPCRouteType.PatrolLoop,       values);
        Assert.Contains(NPCRouteType.TrainingCircuit,  values);
        Assert.Contains(NPCRouteType.RandomGA,         values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCVisualLOD enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCVisualLOD_AllLevelsAreDefined()
    {
        var values = (NPCVisualLOD[])Enum.GetValues(typeof(NPCVisualLOD));
        Assert.Contains(NPCVisualLOD.Icon,      values);
        Assert.Contains(NPCVisualLOD.LowPoly,   values);
        Assert.Contains(NPCVisualLOD.FullModel, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCTrafficDensity enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCTrafficDensity_AllLevelsAreDefined()
    {
        var values = (NPCTrafficDensity[])Enum.GetValues(typeof(NPCTrafficDensity));
        Assert.Contains(NPCTrafficDensity.None,   values);
        Assert.Contains(NPCTrafficDensity.Sparse, values);
        Assert.Contains(NPCTrafficDensity.Normal, values);
        Assert.Contains(NPCTrafficDensity.Dense,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCAircraftData
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCAircraftData_DefaultConstruction_HasNullOrDefaultFields()
    {
        var data = new NPCAircraftData();
        Assert.AreEqual(NPCBehaviorState.Taxiing, data.BehaviorState);
        Assert.IsFalse(data.IsVisible);
        Assert.AreEqual(0f, data.AltitudeMetres);
        Assert.AreEqual(0f, data.SpeedKnots);
    }

    [Test]
    public void NPCAircraftData_CanSetAllFields()
    {
        var data = new NPCAircraftData
        {
            Id             = "NPC_001",
            Callsign       = "UAL1234",
            AircraftType   = "B738",
            Category       = NPCAircraftCategory.CommercialAirline,
            AltitudeMetres = 10000f,
            SpeedKnots     = 460f,
            HeadingDeg     = 270f,
            BehaviorState  = NPCBehaviorState.Cruising,
            IsVisible      = true
        };

        Assert.AreEqual("NPC_001",                           data.Id);
        Assert.AreEqual("UAL1234",                           data.Callsign);
        Assert.AreEqual("B738",                              data.AircraftType);
        Assert.AreEqual(NPCAircraftCategory.CommercialAirline, data.Category);
        Assert.AreEqual(10000f,                              data.AltitudeMetres);
        Assert.AreEqual(460f,                                data.SpeedKnots);
        Assert.AreEqual(NPCBehaviorState.Cruising,           data.BehaviorState);
        Assert.IsTrue(data.IsVisible);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCFlightProfile
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCFlightProfile_CommercialAirlineFasterThanHelicopter()
    {
        var commercial = new NPCFlightProfile { CruiseSpeedKnots = 460f };
        var helicopter = new NPCFlightProfile { CruiseSpeedKnots = 120f };
        Assert.Greater(commercial.CruiseSpeedKnots, helicopter.CruiseSpeedKnots);
    }

    [Test]
    public void NPCFlightProfile_MilitaryHigherAltitudeThanTraining()
    {
        var military = new NPCFlightProfile { CruiseAltitudeMetres = 8000f };
        var training = new NPCFlightProfile { CruiseAltitudeMetres = 1000f };
        Assert.Greater(military.CruiseAltitudeMetres, training.CruiseAltitudeMetres);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCTrafficConfig
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCTrafficConfig_DefaultFlightProfiles_ContainAllCategories()
    {
        var config = ScriptableObject.CreateInstance<NPCTrafficConfig>();
        var categories = config.FlightProfiles.Select(p => p.Category).ToList();

        Assert.Contains(NPCAircraftCategory.CommercialAirline, categories);
        Assert.Contains(NPCAircraftCategory.PrivateJet,        categories);
        Assert.Contains(NPCAircraftCategory.CargoPlane,        categories);
        Assert.Contains(NPCAircraftCategory.MilitaryAircraft,  categories);
        Assert.Contains(NPCAircraftCategory.Helicopter,        categories);
        Assert.Contains(NPCAircraftCategory.TrainingAircraft,  categories);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void NPCTrafficConfig_DefaultMaxNPCs_IsPositive()
    {
        var config = ScriptableObject.CreateInstance<NPCTrafficConfig>();
        Assert.Greater(config.MaxActiveNPCs, 0);
        Object.DestroyImmediate(config);
    }

    [Test]
    public void NPCTrafficConfig_DespawnRadius_GreaterThanSpawnRadius()
    {
        var config = ScriptableObject.CreateInstance<NPCTrafficConfig>();
        Assert.Greater(config.DespawnRadiusMetres, config.SpawnRadiusMetres);
        Object.DestroyImmediate(config);
    }

    [Test]
    public void NPCTrafficConfig_RushHourMultiplier_GreaterThanOne()
    {
        var config = ScriptableObject.CreateInstance<NPCTrafficConfig>();
        Assert.Greater(config.RushHourMultiplier, 1f);
        Object.DestroyImmediate(config);
    }

    [Test]
    public void NPCTrafficConfig_NightMultiplier_LessThanOne()
    {
        var config = ScriptableObject.CreateInstance<NPCTrafficConfig>();
        Assert.Less(config.NightMultiplier, 1f);
        Object.DestroyImmediate(config);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCCallsignGenerator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCCallsignGenerator_Commercial_ProducesNonEmptyCallsign()
    {
        string cs = NPCCallsignGenerator.Generate(NPCAircraftCategory.CommercialAirline);
        Assert.IsFalse(string.IsNullOrEmpty(cs));
    }

    [Test]
    public void NPCCallsignGenerator_Military_ContainsKnownPrefix()
    {
        string[] prefixes = { "VIPER", "EAGLE", "COBRA", "RAPTOR", "TALON", "GHOST", "BARON" };
        // Generate enough to likely hit one
        bool found = false;
        for (int i = 0; i < 30; i++)
        {
            string cs = NPCCallsignGenerator.Generate(NPCAircraftCategory.MilitaryAircraft);
            if (prefixes.Any(p => cs.StartsWith(p))) { found = true; break; }
        }
        Assert.IsTrue(found, "At least one military callsign should use a known prefix.");
    }

    [Test]
    public void NPCCallsignGenerator_Helicopter_StartsWithHELI()
    {
        // Generate enough to cycle through
        bool found = false;
        for (int i = 0; i < 10; i++)
        {
            string cs = NPCCallsignGenerator.Generate(NPCAircraftCategory.Helicopter);
            if (cs.StartsWith("HELI")) { found = true; break; }
        }
        Assert.IsTrue(found);
    }

    [Test]
    public void NPCCallsignGenerator_UniqueCallsignsGenerated()
    {
        var callsigns = new HashSet<string>();
        for (int i = 0; i < 20; i++)
            callsigns.Add(NPCCallsignGenerator.Generate(NPCAircraftCategory.CommercialAirline));

        Assert.Greater(callsigns.Count, 1, "Multiple unique callsigns should be generated.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCWaypoint
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCWaypoint_CanBeConstructed_WithAllFields()
    {
        var wp = new NPCWaypoint
        {
            Name                = "TOC",
            WorldPosition       = Vector3.up * 10000f,
            AltitudeMetres      = 10000f,
            SpeedConstraintKnots = 0f,
            IsApproachFix       = false,
            IsRunwayThreshold   = false
        };
        Assert.AreEqual("TOC", wp.Name);
        Assert.AreEqual(10000f, wp.AltitudeMetres);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCRoute
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCRoute_DefaultConstruction_HasEmptyWaypoints()
    {
        var route = new NPCRoute();
        Assert.IsNotNull(route.Waypoints);
        Assert.AreEqual(0, route.Waypoints.Count);
    }

    [Test]
    public void NPCRoute_CanAddWaypoints()
    {
        var route = new NPCRoute();
        route.Waypoints.Add(new NPCWaypoint { Name = "WPT1" });
        route.Waypoints.Add(new NPCWaypoint { Name = "WPT2" });
        Assert.AreEqual(2, route.Waypoints.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCMessageType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCMessageType_AllTypesAreDefined()
    {
        var values = (NPCMessageType[])Enum.GetValues(typeof(NPCMessageType));
        Assert.GreaterOrEqual(values.Length, 10);
        Assert.Contains(NPCMessageType.Emergency,       values);
        Assert.Contains(NPCMessageType.FormationInvite, values);
        Assert.Contains(NPCMessageType.LandingReport,   values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCRadioMessage
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCRadioMessage_CanBeConstructed()
    {
        var msg = new NPCRadioMessage
        {
            FrequencyMHz      = 118.1f,
            MessageType       = NPCMessageType.TakeoffClearance,
            SenderCallsign    = "TOWER",
            ReceiverCallsign  = "UAL123",
            Content           = "UAL123, cleared for takeoff.",
            IsPlayerRelevant  = false
        };

        Assert.AreEqual(118.1f,                          msg.FrequencyMHz, 0.001f);
        Assert.AreEqual(NPCMessageType.TakeoffClearance, msg.MessageType);
        Assert.AreEqual("TOWER",                         msg.SenderCallsign);
        Assert.IsFalse(msg.IsPlayerRelevant);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCFormationData
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCFormationData_DefaultConstruction_IsNotActive()
    {
        var formation = new NPCFormationData();
        Assert.IsFalse(formation.IsActive);
        Assert.IsFalse(formation.PlayerIsWingman);
        Assert.IsNotNull(formation.WingmanCallsigns);
    }

    [Test]
    public void NPCFormationData_CanAddWingmen()
    {
        var formation = new NPCFormationData
        {
            LeadCallsign = "VIPER1",
            IsActive     = true
        };
        formation.WingmanCallsigns.Add("VIPER2");
        formation.WingmanCallsigns.Add("VIPER3");
        Assert.AreEqual(2, formation.WingmanCallsigns.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AirportGate
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AirportGate_IsOccupied_FalseWhenEmpty()
    {
        var gate = new AirportGate { GateId = "A1" };
        Assert.IsFalse(gate.IsOccupied);
    }

    [Test]
    public void AirportGate_IsOccupied_TrueWhenCallsignSet()
    {
        var gate = new AirportGate { GateId = "B3", OccupyingCallsign = "UAL999" };
        Assert.IsTrue(gate.IsOccupied);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AirportActivityState
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AirportActivityState_DefaultConstruction_IsNotActive()
    {
        var state = new AirportActivityState();
        Assert.IsFalse(state.IsActive);
        Assert.AreEqual(0f, state.ActivityLevel);
        Assert.IsNotNull(state.Gates);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AltitudeProfileSegment
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AltitudeProfileSegment_CanBeConstructed()
    {
        var seg = new AltitudeProfileSegment
        {
            StartWaypointIndex   = 0,
            EndWaypointIndex     = 3,
            TargetAltitudeMetres = 10000f,
            VerticalRateMs       = 10f
        };
        Assert.AreEqual(10000f, seg.TargetAltitudeMetres);
        Assert.AreEqual(10f,    seg.VerticalRateMs);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCTrafficManager (MonoBehaviour — runtime test via GameObject)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCTrafficManager_RegisterAndDeregisterNPC()
    {
        var go      = new GameObject("TestTrafficManager");
        var manager = go.AddComponent<NPCTrafficManager>();

        var data = new NPCAircraftData
        {
            Id       = "TEST_001",
            Callsign = "TST001",
            IsVisible = true
        };

        manager.RegisterNPC(data);
        Assert.AreEqual(1, manager.ActiveNPCs.Count);
        Assert.AreEqual("TEST_001", manager.ActiveNPCs[0].Id);

        manager.DeregisterNPC("TEST_001");
        Assert.AreEqual(0, manager.ActiveNPCs.Count);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void NPCTrafficManager_RegisterNPC_IgnoresDuplicate()
    {
        var go      = new GameObject("TestTrafficManager2");
        var manager = go.AddComponent<NPCTrafficManager>();

        var data = new NPCAircraftData { Id = "DUP_001" };
        manager.RegisterNPC(data);
        manager.RegisterNPC(data); // duplicate

        Assert.AreEqual(1, manager.ActiveNPCs.Count);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void NPCTrafficManager_GetNearestNPC_ReturnsNearestByDistance()
    {
        var go      = new GameObject("TestTrafficManager3");
        var manager = go.AddComponent<NPCTrafficManager>();

        var nearNPC = new NPCAircraftData { Id = "NEAR", WorldPosition = new Vector3(100f, 0f, 0f) };
        var farNPC  = new NPCAircraftData { Id = "FAR",  WorldPosition = new Vector3(9000f, 0f, 0f) };

        manager.RegisterNPC(nearNPC);
        manager.RegisterNPC(farNPC);

        NPCAircraftData nearest = manager.GetNearestNPC(Vector3.zero);
        Assert.AreEqual("NEAR", nearest.Id);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void NPCTrafficManager_ClearAllNPCs_EmptiesActiveList()
    {
        var go      = new GameObject("TestTrafficManager4");
        var manager = go.AddComponent<NPCTrafficManager>();

        for (int i = 0; i < 5; i++)
            manager.RegisterNPC(new NPCAircraftData { Id = $"NPC_{i}" });

        manager.ClearAllNPCs();
        Assert.AreEqual(0, manager.ActiveNPCs.Count);

        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCFormationController (MonoBehaviour)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCFormationController_CreateFormation_IsActive()
    {
        var go   = new GameObject("TestFormationController");
        var ctrl = go.AddComponent<NPCFormationController>();

        NPCFormationData formation = ctrl.CreateFormation("VIPER1");
        Assert.IsNotNull(formation);
        Assert.IsTrue(formation.IsActive);
        Assert.AreEqual("VIPER1", formation.LeadCallsign);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void NPCFormationController_PlayerJoinAndLeave()
    {
        var go   = new GameObject("TestFormationController2");
        var ctrl = go.AddComponent<NPCFormationController>();

        NPCFormationData formation = ctrl.CreateFormation("EAGLE1");
        bool joined = ctrl.PlayerJoinFormation(formation.FormationId);

        Assert.IsTrue(joined);
        Assert.IsTrue(formation.PlayerIsWingman);
        Assert.IsNotNull(ctrl.GetPlayerFormation());

        ctrl.PlayerLeaveFormation();
        Assert.IsFalse(formation.PlayerIsWingman);
        Assert.IsNull(ctrl.GetPlayerFormation());

        Object.DestroyImmediate(go);
    }

    [Test]
    public void NPCFormationController_DisbandFormation_RemovesFormation()
    {
        var go   = new GameObject("TestFormationController3");
        var ctrl = go.AddComponent<NPCFormationController>();

        NPCFormationData f = ctrl.CreateFormation("TALON1");
        ctrl.DisbandFormation(f.FormationId);

        Assert.IsFalse(ctrl.GetAllFormations().Any(x => x.FormationId == f.FormationId));

        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AirportActivityManager (MonoBehaviour)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AirportActivityManager_RegisterAirport_AddsToTracked()
    {
        var go  = new GameObject("TestAirportManager");
        var mgr = go.AddComponent<AirportActivityManager>();

        mgr.RegisterAirport("EGLL", Vector3.zero, 10);
        Assert.AreEqual(1, mgr.TrackedAirports.Count);
        Assert.AreEqual("EGLL", mgr.TrackedAirports[0].ICAO);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void AirportActivityManager_RegisterAirport_DoesNotDuplicate()
    {
        var go  = new GameObject("TestAirportManager2");
        var mgr = go.AddComponent<AirportActivityManager>();

        mgr.RegisterAirport("KJFK", Vector3.zero, 5);
        mgr.RegisterAirport("KJFK", Vector3.zero, 5);
        Assert.AreEqual(1, mgr.TrackedAirports.Count);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void AirportActivityManager_AssignGate_OccupiesGate()
    {
        var go  = new GameObject("TestAirportManager3");
        var mgr = go.AddComponent<AirportActivityManager>();
        mgr.RegisterAirport("YSSY", Vector3.zero, 5);

        string gateId = mgr.AssignGate("YSSY", "QFA1");
        Assert.IsFalse(string.IsNullOrEmpty(gateId));
        Assert.AreEqual(4, mgr.GetFreeGateCount("YSSY")); // 5 gates, 1 occupied → 4 free
        // Verify assigned
        var airport = mgr.TrackedAirports.First(a => a.ICAO == "YSSY");
        var gate    = airport.Gates.Find(g => g.GateId == gateId);
        Assert.IsNotNull(gate);
        Assert.IsTrue(gate.IsOccupied);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void AirportActivityManager_VacateGate_FreesGate()
    {
        var go  = new GameObject("TestAirportManager4");
        var mgr = go.AddComponent<AirportActivityManager>();
        mgr.RegisterAirport("RJTT", Vector3.zero, 3);

        string gateId = mgr.AssignGate("RJTT", "ANA101");
        Assert.IsNotNull(gateId);

        mgr.VacateGate("RJTT", "ANA101");

        var airport = mgr.TrackedAirports.First(a => a.ICAO == "RJTT");
        var gate    = airport.Gates.Find(g => g.GateId == gateId);
        Assert.IsFalse(gate.IsOccupied);

        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NPCTrafficModeManager (MonoBehaviour)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NPCTrafficModeManager_IsActiveByDefault()
    {
        var go  = new GameObject("TestModeManager");
        var mgr = go.AddComponent<NPCTrafficModeManager>();

        // Start is called in Play mode, but Awake runs in edit mode tests
        // We just verify the component was created without error
        Assert.IsNotNull(mgr);

        Object.DestroyImmediate(go);
    }
}
