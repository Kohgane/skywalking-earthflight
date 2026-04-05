// HistoricalSciFiTests.cs — NUnit EditMode tests for Phase 106 Historical & Sci-Fi Flight Mode
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SWEF.HistoricalSciFi;

[TestFixture]
public class HistoricalSciFiTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // AircraftEra enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AircraftEra_AllValuesAreDefined()
    {
        var values = (AircraftEra[])Enum.GetValues(typeof(AircraftEra));
        Assert.GreaterOrEqual(values.Length, 6, "At least 6 eras should be defined");
        Assert.Contains(AircraftEra.Pioneer,    values);
        Assert.Contains(AircraftEra.GoldenAge,  values);
        Assert.Contains(AircraftEra.WorldWarII, values);
        Assert.Contains(AircraftEra.ColdWar,    values);
        Assert.Contains(AircraftEra.Supersonic, values);
        Assert.Contains(AircraftEra.SpaceAge,   values);
    }

    [Test]
    public void AircraftSpecialAbility_AllValuesAreDefined()
    {
        var values = (AircraftSpecialAbility[])Enum.GetValues(typeof(AircraftSpecialAbility));
        Assert.Contains(AircraftSpecialAbility.None,                    values);
        Assert.Contains(AircraftSpecialAbility.SupersonicCruise,        values);
        Assert.Contains(AircraftSpecialAbility.HighAltitudeRecon,       values);
        Assert.Contains(AircraftSpecialAbility.OrbitalReentry,          values);
        Assert.Contains(AircraftSpecialAbility.HistoricalIconStatus,    values);
        Assert.Contains(AircraftSpecialAbility.SpaceCapable,            values);
        Assert.Contains(AircraftSpecialAbility.SuperiorManeuverability, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HistoricalAircraftData — model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void HistoricalAircraftData_Create_SetsAllFields()
    {
        var abilities = new List<AircraftSpecialAbility> { AircraftSpecialAbility.SupersonicCruise };
        var data = HistoricalAircraftData.Create(
            id:                    "test_aircraft",
            displayName:           "Test Aircraft",
            year:                  2000,
            era:                   AircraftEra.ColdWar,
            description:           "A test aircraft.",
            maxSpeedKph:           1000f,
            maxAltitudeMetres:     15_000f,
            maneuverabilityRating: 7f,
            fuelEfficiency:        5f,
            unlockedByDefault:     true,
            specialAbilities:      abilities);

        Assert.AreEqual("test_aircraft",                     data.id);
        Assert.AreEqual("Test Aircraft",                     data.displayName);
        Assert.AreEqual(2000,                                data.year);
        Assert.AreEqual(AircraftEra.ColdWar,                 data.era);
        Assert.AreEqual(1000f,                               data.maxSpeedKph,           1e-3f);
        Assert.AreEqual(15_000f,                             data.maxAltitudeMetres,     1f);
        Assert.AreEqual(7f,                                  data.maneuverabilityRating, 1e-3f);
        Assert.AreEqual(5f,                                  data.fuelEfficiency,        1e-3f);
        Assert.IsTrue(data.unlockedByDefault);
    }

    [Test]
    public void HistoricalAircraftData_ManeuverabilityRating_IsClamped()
    {
        var over  = HistoricalAircraftData.Create("o", "O", 2000, AircraftEra.Pioneer, "", 100f, 100f, 15f, 5f);
        var under = HistoricalAircraftData.Create("u", "U", 2000, AircraftEra.Pioneer, "", 100f, 100f, -3f, 5f);

        Assert.AreEqual(10f, over.maneuverabilityRating,  1e-3f, "Rating should be clamped to 10");
        Assert.AreEqual(0f,  under.maneuverabilityRating, 1e-3f, "Rating should be clamped to 0");
    }

    [Test]
    public void HistoricalAircraftData_HasAbility_ReturnsTrueForPresentAbility()
    {
        var data = HistoricalAircraftData.Create(
            "a", "A", 1966, AircraftEra.ColdWar, "", 3_540f, 25_908f, 5f, 3f,
            specialAbilities: new List<AircraftSpecialAbility> { AircraftSpecialAbility.SupersonicCruise, AircraftSpecialAbility.HighAltitudeRecon });

        Assert.IsTrue(data.HasAbility(AircraftSpecialAbility.SupersonicCruise));
        Assert.IsTrue(data.HasAbility(AircraftSpecialAbility.HighAltitudeRecon));
        Assert.IsFalse(data.HasAbility(AircraftSpecialAbility.OrbitalReentry));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HistoricalAircraftRegistry
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void HistoricalAircraftRegistry_ContainsSixBuiltInAircraft()
    {
        var registry = HistoricalAircraftRegistry.Instance;
        Assert.GreaterOrEqual(registry.Count, 6, "Should have at least 6 built-in aircraft");
    }

    [Test]
    public void HistoricalAircraftRegistry_WrightFlyer_IsUnlockedByDefault()
    {
        var registry = HistoricalAircraftRegistry.Instance;
        Assert.IsTrue(registry.IsUnlocked(HistoricalAircraftRegistry.IdWrightFlyer),
            "Wright Flyer should be unlocked by default");
    }

    [Test]
    public void HistoricalAircraftRegistry_GetById_ReturnsCorrectAircraft()
    {
        var data = HistoricalAircraftRegistry.Instance.GetById(HistoricalAircraftRegistry.IdSpitfire);
        Assert.IsNotNull(data);
        Assert.AreEqual(HistoricalAircraftRegistry.IdSpitfire, data.id);
        Assert.AreEqual(AircraftEra.WorldWarII, data.era);
    }

    [Test]
    public void HistoricalAircraftRegistry_GetById_ReturnsNullForUnknownId()
    {
        var data = HistoricalAircraftRegistry.Instance.GetById("nonexistent_id");
        Assert.IsNull(data);
    }

    [Test]
    public void HistoricalAircraftRegistry_GetById_ReturnsNullForNullId()
    {
        var data = HistoricalAircraftRegistry.Instance.GetById(null);
        Assert.IsNull(data);
    }

    [Test]
    public void HistoricalAircraftRegistry_Unlock_EnablesLockedAircraft()
    {
        var registry = HistoricalAircraftRegistry.Instance;
        // Concorde starts locked.
        registry.Unlock(HistoricalAircraftRegistry.IdConcorde);
        Assert.IsTrue(registry.IsUnlocked(HistoricalAircraftRegistry.IdConcorde));
    }

    [Test]
    public void HistoricalAircraftRegistry_Unlock_ReturnsFalseForUnknownId()
    {
        var result = HistoricalAircraftRegistry.Instance.Unlock("does_not_exist");
        Assert.IsFalse(result);
    }

    [Test]
    public void HistoricalAircraftRegistry_GetByEra_ReturnsCorrectSubset()
    {
        var spaceAgeAircraft = HistoricalAircraftRegistry.Instance
            .GetByEra(AircraftEra.SpaceAge)
            .ToList();

        Assert.GreaterOrEqual(spaceAgeAircraft.Count, 1, "At least one SpaceAge aircraft expected");
        foreach (var a in spaceAgeAircraft)
            Assert.AreEqual(AircraftEra.SpaceAge, a.era);
    }

    [Test]
    public void HistoricalAircraftRegistry_Register_ThrowsForNullData()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HistoricalAircraftRegistry.Instance.Register(null));
    }

    [Test]
    public void HistoricalAircraftRegistry_SR71_HasSupersonicCruiseAbility()
    {
        var data = HistoricalAircraftRegistry.Instance.GetById(HistoricalAircraftRegistry.IdSR71Blackbird);
        Assert.IsNotNull(data);
        Assert.IsTrue(data.HasAbility(AircraftSpecialAbility.SupersonicCruise));
        Assert.IsTrue(data.HasAbility(AircraftSpecialAbility.HighAltitudeRecon));
    }

    [Test]
    public void HistoricalAircraftRegistry_SpaceShuttle_HasOrbitalReentryAbility()
    {
        var data = HistoricalAircraftRegistry.Instance.GetById(HistoricalAircraftRegistry.IdSpaceShuttle);
        Assert.IsNotNull(data);
        Assert.IsTrue(data.HasAbility(AircraftSpecialAbility.OrbitalReentry));
        Assert.IsTrue(data.HasAbility(AircraftSpecialAbility.SpaceCapable));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CelestialBody & SciFiEnvironmentData — model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CelestialBody_AllValuesAreDefined()
    {
        var values = (CelestialBody[])Enum.GetValues(typeof(CelestialBody));
        Assert.Contains(CelestialBody.Earth, values);
        Assert.Contains(CelestialBody.Space, values);
        Assert.Contains(CelestialBody.Moon,  values);
        Assert.Contains(CelestialBody.Mars,  values);
    }

    [Test]
    public void SciFiEnvironmentData_Create_SetsAllFields()
    {
        var env = SciFiEnvironmentData.Create(
            id:                  "test_env",
            displayName:         "Test Env",
            celestialBody:       CelestialBody.Moon,
            surfaceDescription:  "A test environment.",
            gravityMultiplier:   0.165f,
            atmosphereDensity:   0f,
            maxWindSpeedKph:     0f);

        Assert.AreEqual("test_env",         env.id);
        Assert.AreEqual(CelestialBody.Moon, env.celestialBody);
        Assert.AreEqual(0.165f,             env.gravityMultiplier, 1e-3f);
        Assert.AreEqual(0f,                 env.atmosphereDensity, 1e-3f);
        Assert.IsFalse(env.HasAtmosphere,   "Moon has no atmosphere");
        Assert.IsFalse(env.IsZeroGravity,   "Moon has some gravity");
    }

    [Test]
    public void SciFiEnvironmentData_Space_IsZeroGravityAndNoAtmosphere()
    {
        var env = SciFiEnvironmentData.Create("sp", "Space", CelestialBody.Space, "", 0f, 0f);
        Assert.IsTrue(env.IsZeroGravity);
        Assert.IsFalse(env.HasAtmosphere);
    }

    [Test]
    public void SciFiEnvironmentData_Earth_HasAtmosphereAndGravity()
    {
        var env = SciFiEnvironmentData.Create("e", "Earth", CelestialBody.Earth, "", 1f, 1f);
        Assert.IsTrue(env.HasAtmosphere);
        Assert.IsFalse(env.IsZeroGravity);
    }

    [Test]
    public void SciFiEnvironmentData_GravityMultiplier_IsClamped()
    {
        var env = SciFiEnvironmentData.Create("x", "X", CelestialBody.Earth, "", 99f, 0f);
        Assert.AreEqual(2f, env.gravityMultiplier, 1e-3f, "gravityMultiplier should be clamped to 2");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SciFiEnvironmentController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SciFiEnvironmentController_ContainsFourBuiltInEnvironments()
    {
        var ctrl = SciFiEnvironmentController.Instance;
        Assert.GreaterOrEqual(ctrl.All.Count, 4, "Should have at least 4 built-in environments");
    }

    [Test]
    public void SciFiEnvironmentController_DefaultEnvironment_IsEarth()
    {
        var ctrl = SciFiEnvironmentController.Instance;
        Assert.IsNotNull(ctrl.ActiveEnvironment);
        Assert.AreEqual(CelestialBody.Earth, ctrl.ActiveEnvironment.celestialBody);
    }

    [Test]
    public void SciFiEnvironmentController_GetById_ReturnsCorrectEnvironment()
    {
        var env = SciFiEnvironmentController.Instance.GetById(SciFiEnvironmentController.IdMoon);
        Assert.IsNotNull(env);
        Assert.AreEqual(CelestialBody.Moon, env.celestialBody);
    }

    [Test]
    public void SciFiEnvironmentController_GetById_ReturnsNullForUnknownId()
    {
        var env = SciFiEnvironmentController.Instance.GetById("totally_unknown");
        Assert.IsNull(env);
    }

    [Test]
    public void SciFiEnvironmentController_TransitionTo_ChangesActiveEnvironment()
    {
        var ctrl = SciFiEnvironmentController.Instance;
        ctrl.TransitionTo(SciFiEnvironmentController.IdMars);
        Assert.AreEqual(CelestialBody.Mars, ctrl.ActiveEnvironment.celestialBody);

        // Reset to Earth for subsequent tests.
        ctrl.TransitionTo(SciFiEnvironmentController.IdEarth);
    }

    [Test]
    public void SciFiEnvironmentController_TransitionTo_FiresEvent()
    {
        var ctrl = SciFiEnvironmentController.Instance;
        SciFiEnvironmentData capturedPrev = null;
        SciFiEnvironmentData capturedNext = null;

        void Handler(SciFiEnvironmentData prev, SciFiEnvironmentData next)
        {
            capturedPrev = prev;
            capturedNext = next;
        }

        ctrl.OnEnvironmentChanged += Handler;

        try
        {
            ctrl.TransitionTo(SciFiEnvironmentController.IdSpace);
            Assert.IsNotNull(capturedNext);
            Assert.AreEqual(CelestialBody.Space, capturedNext.celestialBody);
        }
        finally
        {
            ctrl.OnEnvironmentChanged -= Handler;
            ctrl.TransitionTo(SciFiEnvironmentController.IdEarth);
        }
    }

    [Test]
    public void SciFiEnvironmentController_TransitionToSameEnvironment_ReturnsFalse()
    {
        var ctrl = SciFiEnvironmentController.Instance;
        ctrl.TransitionTo(SciFiEnvironmentController.IdEarth);
        var result = ctrl.TransitionTo(SciFiEnvironmentController.IdEarth);
        Assert.IsFalse(result, "Transitioning to current environment should return false");
    }

    [Test]
    public void SciFiEnvironmentController_TransitionTo_ThrowsForUnknownId()
    {
        Assert.Throws<ArgumentException>(() =>
            SciFiEnvironmentController.Instance.TransitionTo("no_such_env"));
    }

    [Test]
    public void SciFiEnvironmentController_GetActiveGravity_EarthIsApproximately9_81()
    {
        var ctrl = SciFiEnvironmentController.Instance;
        ctrl.TransitionTo(SciFiEnvironmentController.IdEarth);
        Assert.AreEqual(9.81f, ctrl.GetActiveGravity(), 0.01f);
    }

    [Test]
    public void SciFiEnvironmentController_GetActiveGravity_MoonIsOneSixthEarth()
    {
        var ctrl = SciFiEnvironmentController.Instance;
        ctrl.TransitionTo(SciFiEnvironmentController.IdMoon);
        float moonGravity = ctrl.GetActiveGravity();
        Assert.Less(moonGravity, 2f,   "Moon gravity should be less than 2 m/s²");
        Assert.Greater(moonGravity, 0f, "Moon gravity should be positive");

        ctrl.TransitionTo(SciFiEnvironmentController.IdEarth);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MissionStatus & MissionCategory enums
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void MissionStatus_AllValuesAreDefined()
    {
        var values = (MissionStatus[])Enum.GetValues(typeof(MissionStatus));
        Assert.Contains(MissionStatus.Available,  values);
        Assert.Contains(MissionStatus.InProgress, values);
        Assert.Contains(MissionStatus.Completed,  values);
        Assert.Contains(MissionStatus.Failed,     values);
        Assert.Contains(MissionStatus.Locked,     values);
    }

    [Test]
    public void MissionCategory_ContainsHistoricalAndSciFi()
    {
        var values = (MissionCategory[])Enum.GetValues(typeof(MissionCategory));
        Assert.Contains(MissionCategory.Historical, values);
        Assert.Contains(MissionCategory.SciFi,      values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SpecialMissionData — model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SpecialMissionData_Create_SetsFields()
    {
        var objectives = new List<MissionObjective>
        {
            MissionObjective.Create("Obj A"),
            MissionObjective.Create("Obj B"),
        };
        var reward = new MissionReward { bonusPoints = 100 };

        var mission = SpecialMissionData.Create(
            "m1", "Mission 1", "Do a thing.",
            MissionCategory.Historical,
            "wright_flyer", "earth",
            objectives, reward);

        Assert.AreEqual("m1",                          mission.id);
        Assert.AreEqual("Mission 1",                   mission.title);
        Assert.AreEqual(MissionCategory.Historical,    mission.category);
        Assert.AreEqual("wright_flyer",                mission.requiredAircraftId);
        Assert.AreEqual("earth",                       mission.requiredEnvironmentId);
        Assert.AreEqual(2,                             mission.objectives.Count);
        Assert.AreEqual(MissionStatus.Locked,          mission.status);
    }

    [Test]
    public void SpecialMissionData_AllObjectivesComplete_ReturnsTrueOnlyWhenAllDone()
    {
        var objectives = new List<MissionObjective>
        {
            MissionObjective.Create("A"),
            MissionObjective.Create("B"),
        };
        var mission = SpecialMissionData.Create("x", "X", "", MissionCategory.Historical, "", "", objectives, null);

        Assert.IsFalse(mission.AllObjectivesComplete(), "Should be false before any completion");

        objectives[0].isCompleted = true;
        Assert.IsFalse(mission.AllObjectivesComplete(), "Should be false with only one done");

        objectives[1].isCompleted = true;
        Assert.IsTrue(mission.AllObjectivesComplete(), "Should be true when all done");
    }

    [Test]
    public void SpecialMissionData_AllObjectivesComplete_ReturnsFalseForEmptyObjectives()
    {
        var mission = SpecialMissionData.Create("e", "E", "", MissionCategory.SciFi, "", "", new List<MissionObjective>(), null);
        Assert.IsFalse(mission.AllObjectivesComplete(), "Empty objectives => false");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SpecialMissionManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SpecialMissionManager_ContainsSixBuiltInMissions()
    {
        Assert.GreaterOrEqual(SpecialMissionManager.Instance.All.Count, 6,
            "Should have at least 6 built-in missions");
    }

    [Test]
    public void SpecialMissionManager_GetById_ReturnsCorrectMission()
    {
        var mission = SpecialMissionManager.Instance.GetById("first_flight");
        Assert.IsNotNull(mission);
        Assert.AreEqual("first_flight", mission.id);
        Assert.AreEqual(MissionCategory.Historical, mission.category);
    }

    [Test]
    public void SpecialMissionManager_GetById_ReturnsNullForUnknownId()
    {
        Assert.IsNull(SpecialMissionManager.Instance.GetById("no_such_mission"));
    }

    [Test]
    public void SpecialMissionManager_GetByCategory_ReturnsCorrectSubset()
    {
        var sciFi = SpecialMissionManager.Instance.GetByCategory(MissionCategory.SciFi).ToList();
        Assert.GreaterOrEqual(sciFi.Count, 2, "At least 2 Sci-Fi missions expected");
        foreach (var m in sciFi)
            Assert.AreEqual(MissionCategory.SciFi, m.category);
    }

    [Test]
    public void SpecialMissionManager_UnlockMissionsForAircraft_UnlocksCorrectMissions()
    {
        var manager = SpecialMissionManager.Instance;
        int unlocked = manager.UnlockMissionsForAircraft(HistoricalAircraftRegistry.IdWrightFlyer);
        Assert.GreaterOrEqual(unlocked, 1, "At least one mission should be unlocked for Wright Flyer");

        var firstFlight = manager.GetById("first_flight");
        Assert.IsNotNull(firstFlight);
        Assert.AreNotEqual(MissionStatus.Locked, firstFlight.status);
    }

    [Test]
    public void SpecialMissionManager_StartMission_ChangesStatusToInProgress()
    {
        var manager = SpecialMissionManager.Instance;

        // Ensure mission is available.
        manager.UnlockMissionsForAircraft(HistoricalAircraftRegistry.IdWrightFlyer);
        var mission = manager.GetById("first_flight");
        if (mission.status == MissionStatus.InProgress)
        {
            manager.FailMission(); // reset if somehow in progress
        }
        if (mission.status != MissionStatus.Available)
            Assert.Ignore("first_flight is not in Available state; cannot test StartMission.");

        manager.StartMission("first_flight");
        Assert.AreEqual(MissionStatus.InProgress, mission.status);
        Assert.AreEqual(mission, manager.ActiveMission);

        // Clean up — complete to reset.
        manager.FailMission();
    }

    [Test]
    public void SpecialMissionManager_StartMission_ThrowsWhenAlreadyActive()
    {
        var manager = SpecialMissionManager.Instance;
        manager.UnlockMissionsForAircraft(HistoricalAircraftRegistry.IdWrightFlyer);

        var mission = manager.GetById("first_flight");
        if (mission.status == MissionStatus.InProgress)
            manager.FailMission();
        if (mission.status != MissionStatus.Available)
            Assert.Ignore("Cannot test — first_flight not available.");

        manager.StartMission("first_flight");

        Assert.Throws<InvalidOperationException>(() =>
            manager.StartMission("first_flight"));

        manager.FailMission();
    }

    [Test]
    public void SpecialMissionManager_CompleteObjective_MarksObjectiveComplete()
    {
        var manager = SpecialMissionManager.Instance;
        manager.UnlockMissionsForAircraft(HistoricalAircraftRegistry.IdWrightFlyer);

        var mission = manager.GetById("first_flight");
        if (mission.status == MissionStatus.InProgress)
            manager.FailMission();
        if (mission.status != MissionStatus.Available)
            Assert.Ignore("Cannot test — first_flight not available.");

        // Reset objectives.
        foreach (var obj in mission.objectives) obj.isCompleted = false;

        manager.StartMission("first_flight");
        manager.CompleteObjective(0);
        Assert.IsTrue(mission.objectives[0].isCompleted);
        Assert.AreEqual(MissionStatus.InProgress, mission.status); // not all done yet

        manager.FailMission();
    }

    [Test]
    public void SpecialMissionManager_FailMission_ResetsToAvailable()
    {
        var manager = SpecialMissionManager.Instance;
        manager.UnlockMissionsForAircraft(HistoricalAircraftRegistry.IdWrightFlyer);

        var mission = manager.GetById("first_flight");
        if (mission.status == MissionStatus.InProgress)
            manager.FailMission();
        if (mission.status != MissionStatus.Available)
            Assert.Ignore("Cannot test — first_flight not available.");

        manager.StartMission("first_flight");
        manager.FailMission();

        Assert.AreEqual(MissionStatus.Available, mission.status);
        Assert.IsNull(manager.ActiveMission);
    }

    [Test]
    public void SpecialMissionManager_Register_ThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SpecialMissionManager.Instance.Register(null));
    }
}
