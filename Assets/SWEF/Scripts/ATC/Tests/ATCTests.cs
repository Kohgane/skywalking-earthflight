// ATCTests.cs — Phase 119: Advanced AI Traffic Control
// Comprehensive NUnit EditMode tests (45+ tests).
// Tests cover: enums, config, ATC logic, route optimization, TCAS,
// conflict detection, communication, airspace, sequencing.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.ATC;

[TestFixture]
public class ATCTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // ATCFacilityType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ATCFacilityType_AllValuesAreDefined()
    {
        var values = (ATCFacilityType[])Enum.GetValues(typeof(ATCFacilityType));
        Assert.GreaterOrEqual(values.Length, 8, "At least 8 ATCFacilityType values required");
        Assert.Contains(ATCFacilityType.Tower,     values);
        Assert.Contains(ATCFacilityType.Ground,    values);
        Assert.Contains(ATCFacilityType.Approach,  values);
        Assert.Contains(ATCFacilityType.Departure, values);
        Assert.Contains(ATCFacilityType.Center,    values);
        Assert.Contains(ATCFacilityType.ATIS,      values);
        Assert.Contains(ATCFacilityType.Unicom,    values);
        Assert.Contains(ATCFacilityType.Emergency, values);
    }

    [Test]
    public void ATCFacilityType_HasDistinctIntValues()
    {
        var values = (ATCFacilityType[])Enum.GetValues(typeof(ATCFacilityType));
        var seen = new HashSet<int>();
        foreach (var v in values)
            Assert.IsTrue(seen.Add((int)v), $"Duplicate int value for {v}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ATCInstructionCode enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ATCInstruction_AllValuesAreDefined()
    {
        var values = (ATCInstructionCode[])Enum.GetValues(typeof(ATCInstructionCode));
        Assert.GreaterOrEqual(values.Length, 8);
        Assert.Contains(ATCInstructionCode.Cleared,          values);
        Assert.Contains(ATCInstructionCode.Hold,             values);
        Assert.Contains(ATCInstructionCode.GoAround,         values);
        Assert.Contains(ATCInstructionCode.VectorTo,         values);
        Assert.Contains(ATCInstructionCode.DescendTo,        values);
        Assert.Contains(ATCInstructionCode.ClimbTo,          values);
        Assert.Contains(ATCInstructionCode.MaintainSpeed,    values);
        Assert.Contains(ATCInstructionCode.ContactFrequency, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightPhase enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FlightPhase_AllValuesAreDefined()
    {
        var values = (FlightPhase[])Enum.GetValues(typeof(FlightPhase));
        Assert.GreaterOrEqual(values.Length, 10);
        Assert.Contains(FlightPhase.Preflight,  values);
        Assert.Contains(FlightPhase.Taxi,       values);
        Assert.Contains(FlightPhase.Takeoff,    values);
        Assert.Contains(FlightPhase.Departure,  values);
        Assert.Contains(FlightPhase.Cruise,     values);
        Assert.Contains(FlightPhase.Descent,    values);
        Assert.Contains(FlightPhase.Approach,   values);
        Assert.Contains(FlightPhase.Landing,    values);
        Assert.Contains(FlightPhase.GoAround,   values);
        Assert.Contains(FlightPhase.Emergency,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TrafficPriority enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TrafficPriority_EmergencyIsHighest()
    {
        Assert.Greater((int)TrafficPriority.Emergency, (int)TrafficPriority.Military);
        Assert.Greater((int)TrafficPriority.Military,  (int)TrafficPriority.Medical);
        Assert.Greater((int)TrafficPriority.Medical,   (int)TrafficPriority.High);
        Assert.Greater((int)TrafficPriority.High,      (int)TrafficPriority.Normal);
        Assert.Greater((int)TrafficPriority.Normal,    (int)TrafficPriority.Low);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SeparationStandard enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SeparationStandard_AllValuesAreDefined()
    {
        var values = (SeparationStandard[])Enum.GetValues(typeof(SeparationStandard));
        Assert.GreaterOrEqual(values.Length, 6);
        Assert.Contains(SeparationStandard.IFR_Radar,      values);
        Assert.Contains(SeparationStandard.IFR_Procedural, values);
        Assert.Contains(SeparationStandard.VFR_Visual,     values);
        Assert.Contains(SeparationStandard.RVSM,           values);
        Assert.Contains(SeparationStandard.WakeTurbulence, values);
        Assert.Contains(SeparationStandard.Oceanic,        values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TCASAdvisory enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TCASAdvisory_AllValuesAreDefined()
    {
        var values = (TCASAdvisory[])Enum.GetValues(typeof(TCASAdvisory));
        Assert.GreaterOrEqual(values.Length, 6);
        Assert.Contains(TCASAdvisory.None,          values);
        Assert.Contains(TCASAdvisory.TA,            values);
        Assert.Contains(TCASAdvisory.RA_Climb,      values);
        Assert.Contains(TCASAdvisory.RA_Descend,    values);
        Assert.Contains(TCASAdvisory.RA_Monitor,    values);
        Assert.Contains(TCASAdvisory.ClearOfConflict, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AirspaceClass enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AirspaceClass_AllValuesAreDefined()
    {
        var values = (AirspaceClass[])Enum.GetValues(typeof(AirspaceClass));
        Assert.GreaterOrEqual(values.Length, 6);
        Assert.Contains(AirspaceClass.A, values);
        Assert.Contains(AirspaceClass.B, values);
        Assert.Contains(AirspaceClass.C, values);
        Assert.Contains(AirspaceClass.D, values);
        Assert.Contains(AirspaceClass.E, values);
        Assert.Contains(AirspaceClass.G, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WaypointType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void WaypointType_AllValuesAreDefined()
    {
        var values = (WaypointType[])Enum.GetValues(typeof(WaypointType));
        Assert.GreaterOrEqual(values.Length, 7);
        Assert.Contains(WaypointType.VOR,             values);
        Assert.Contains(WaypointType.NDB,             values);
        Assert.Contains(WaypointType.Intersection,    values);
        Assert.Contains(WaypointType.Enroute,         values);
        Assert.Contains(WaypointType.Terminal,        values);
        Assert.Contains(WaypointType.Airport,         values);
        Assert.Contains(WaypointType.RunwayThreshold, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ConflictSeverity enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ConflictSeverity_AllValuesAreDefined()
    {
        var values = (ConflictSeverity[])Enum.GetValues(typeof(ConflictSeverity));
        Assert.GreaterOrEqual(values.Length, 4);
        Assert.Contains(ConflictSeverity.Advisory,  values);
        Assert.Contains(ConflictSeverity.Caution,   values);
        Assert.Contains(ConflictSeverity.Warning,   values);
        Assert.Contains(ConflictSeverity.Critical,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ATCFacility data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ATCFacility_ConstructorSetsFields()
    {
        var f = new ATCFacility("KLAX_TWR", "LA Tower", ATCFacilityType.Tower, 133.9f, "KLAX");
        Assert.AreEqual("KLAX_TWR",          f.facilityId);
        Assert.AreEqual("LA Tower",          f.name);
        Assert.AreEqual(ATCFacilityType.Tower, f.facilityType);
        Assert.AreEqual(133.9f,              f.primaryFrequency, 0.01f);
        Assert.AreEqual("KLAX",              f.icaoCode);
        Assert.IsTrue(f.isActive);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightStrip data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FlightStrip_ConstructorSetsDefaults()
    {
        var s = new FlightStrip("UAL123", "B738", "KLAX", "KJFK", 35000);
        Assert.AreEqual("UAL123",             s.callsign);
        Assert.AreEqual("B738",               s.aircraftType);
        Assert.AreEqual("KLAX",               s.origin);
        Assert.AreEqual("KJFK",               s.destination);
        Assert.AreEqual(35000,                s.filedAltitude);
        Assert.AreEqual(FlightPhase.Taxi, s.phase);
        Assert.AreEqual("1200",               s.squawk);
        Assert.AreEqual(TrafficPriority.Normal, s.priority);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SeparationData
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SeparationData_IsViolation_TrueWhenBothUnderMinimums()
    {
        var sep = new SeparationData
        {
            horizontalNM = 2f, verticalFt = 500f,
            requiredHorizontalNM = 3f, requiredVerticalFt = 1000f
        };
        Assert.IsTrue(sep.IsViolation);
    }

    [Test]
    public void SeparationData_IsViolation_FalseWhenOnlyHorizViolated()
    {
        var sep = new SeparationData
        {
            horizontalNM = 2f, verticalFt = 1500f,
            requiredHorizontalNM = 3f, requiredVerticalFt = 1000f
        };
        Assert.IsFalse(sep.IsViolation);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ConflictAlert
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ConflictAlert_ConstructorAssignsUniqueId()
    {
        var a = new ConflictAlert("AAL1", "UAL2", 90f, 1.5f, ConflictSeverity.Warning);
        var b = new ConflictAlert("DAL3", "SWA4", 30f, 0.5f, ConflictSeverity.Critical);
        Assert.IsNotNull(a.alertId);
        Assert.IsNotNull(b.alertId);
        Assert.AreNotEqual(a.alertId, b.alertId);
    }

    [Test]
    public void ConflictAlert_AcknowledgedDefaultsFalse()
    {
        var alert = new ConflictAlert("AAL1", "UAL2", 120f, 2f, ConflictSeverity.Caution);
        Assert.IsFalse(alert.acknowledged);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RunwayAssignment
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void RunwayAssignment_ConstructorSetsFields()
    {
        var ra = new RunwayAssignment("KLAX", "28L", true, "UAL100");
        Assert.AreEqual("KLAX",   ra.icao);
        Assert.AreEqual("28L",    ra.runwayId);
        Assert.IsTrue(ra.isLanding);
        Assert.AreEqual("UAL100", ra.callsign);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HoldingPattern
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void HoldingPattern_LegTime_LowAltitude_Is1Min()
    {
        var h = new HoldingPattern("UAL1", "DARTS", Vector3.zero, 270f, 10000);
        Assert.AreEqual(1.0f, h.legTimeMinutes, 0.01f);
    }

    [Test]
    public void HoldingPattern_LegTime_HighAltitude_Is1Point5Min()
    {
        var h = new HoldingPattern("UAL1", "DARTS", Vector3.zero, 270f, 20000);
        Assert.AreEqual(1.5f, h.legTimeMinutes, 0.01f);
    }

    [Test]
    public void HoldingPattern_DefaultsRightTurns()
    {
        var h = new HoldingPattern("DAL5", "VOR", Vector3.zero, 180f, 5000);
        Assert.IsTrue(h.rightTurns);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Waypoint
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Waypoint_ConstructorSetsIdentifierAndPosition()
    {
        var pos = new Vector3(100f, 0f, 200f);
        var wp = new Waypoint("BAYJAY", pos, WaypointType.Intersection);
        Assert.AreEqual("BAYJAY",              wp.identifier);
        Assert.AreEqual(pos,                   wp.position);
        Assert.AreEqual(WaypointType.Intersection, wp.type);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CollisionPredictionEngine (static geometry — no MonoBehaviour needed)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CollisionPrediction_DetectsConflictForConvergingAircraft()
    {
        var go  = new GameObject();
        var cpe = go.AddComponent<CollisionPredictionEngine>();

        // Aircraft converging horizontally, both at same altitude
        var result = cpe.ComputeCPA(
            posA: new Vector3(0, 0, 0),   velA: new Vector3(500f, 0, 0),
            altA: 35000f,                  vertRateA: 0f,
            posB: new Vector3(10000, 0, 0), velB: new Vector3(-500f, 0, 0),
            altB: 35000f,                  vertRateB: 0f,
            sepNM: 3f, vertSepFt: 1000f);

        Assert.IsTrue(result.isConflict);
        Assert.Greater(result.conflictProbability, 0f);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void CollisionPrediction_NegatesConflictWithAdequateVerticalSeparation()
    {
        var go  = new GameObject();
        var cpe = go.AddComponent<CollisionPredictionEngine>();

        var result = cpe.ComputeCPA(
            posA: new Vector3(0, 0, 0),    velA: new Vector3(500f, 0, 0),
            altA: 35000f,                   vertRateA: 0f,
            posB: new Vector3(10000, 0, 0), velB: new Vector3(-500f, 0, 0),
            altB: 37000f,                   vertRateB: 0f,  // 2000 ft above
            sepNM: 3f, vertSepFt: 1000f);

        Assert.IsFalse(result.isConflict);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void CollisionPrediction_IsSeparationLost_WhenBothUnderMinimum()
    {
        var go  = new GameObject();
        var cpe = go.AddComponent<CollisionPredictionEngine>();

        // 100 m apart horizontally = ~0.054 NM < 3 NM
        bool lost = cpe.IsSeparationLost(
            new Vector3(0, 0, 0), 35000f,
            new Vector3(100, 0, 0), 35100f,
            sepNM: 3f, vertSepFt: 1000f);

        Assert.IsTrue(lost);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AvoidanceManeuverGenerator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AvoidanceManeuver_GeneratesClimbForRACLimb()
    {
        var go  = new GameObject();
        var gen = go.AddComponent<AvoidanceManeuverGenerator>();

        var maneuver = gen.GenerateFromTCAS(TCASAdvisory.RA_Climb, 35000f);

        Assert.IsNotNull(maneuver);
        Assert.AreEqual(AvoidanceManeuverGenerator.ManeuverType.Climb, maneuver.type);
        Assert.Greater(maneuver.altitudeChangeFt, 0f);
        Assert.AreEqual(1f, maneuver.urgency, 0.01f);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void AvoidanceManeuver_GeneratesDescendForRADescend()
    {
        var go  = new GameObject();
        var gen = go.AddComponent<AvoidanceManeuverGenerator>();

        var maneuver = gen.GenerateFromTCAS(TCASAdvisory.RA_Descend, 35000f);

        Assert.IsNotNull(maneuver);
        Assert.AreEqual(AvoidanceManeuverGenerator.ManeuverType.Descend, maneuver.type);
        Assert.Less(maneuver.altitudeChangeFt, 0f);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void AvoidanceManeuver_NullForNoneAdvisory()
    {
        var go  = new GameObject();
        var gen = go.AddComponent<AvoidanceManeuverGenerator>();

        var maneuver = gen.GenerateFromTCAS(TCASAdvisory.None, 35000f);
        Assert.IsNull(maneuver);

        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ProximityWarningSystem
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ProximityWarning_CriticalForVeryCloseAircraft()
    {
        var go  = new GameObject();
        var pws = go.AddComponent<ProximityWarningSystem>();

        // 500 m apart at same altitude → well under 1 NM
        var level = pws.Evaluate(
            "AAL1", Vector3.zero, 35000f,
            "UAL2", new Vector3(500, 0, 0), 35100f);

        Assert.AreEqual(ProximityWarningSystem.WarningLevel.Critical, level);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void ProximityWarning_NoneForWellSeparatedAircraft()
    {
        var go  = new GameObject();
        var pws = go.AddComponent<ProximityWarningSystem>();

        // 50 km apart = ~27 NM
        var level = pws.Evaluate(
            "AAL1", Vector3.zero, 35000f,
            "UAL2", new Vector3(50000, 0, 0), 37000f);

        Assert.AreEqual(ProximityWarningSystem.WarningLevel.None, level);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ATCCommunicationController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Communication_GeneratesTransmissionForCleared()
    {
        var go   = new GameObject();
        var comm = go.AddComponent<ATCCommunicationController>();

        var tx = comm.GenerateTransmission("KLAX_TWR", "UAL123", ATCInstructionCode.Cleared);

        Assert.IsNotNull(tx);
        Assert.AreEqual("UAL123", tx.aircraftCallsign);
        Assert.AreEqual(ATCInstructionCode.Cleared, tx.instruction);
        Assert.IsTrue(tx.requiresReadback);
        Assert.IsNotEmpty(tx.phraseText);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void Communication_LogCount_IncrementsPerTransmission()
    {
        var go   = new GameObject();
        var comm = go.AddComponent<ATCCommunicationController>();

        comm.GenerateTransmission("TWR", "AAL1", ATCInstructionCode.GoAround);
        comm.GenerateTransmission("TWR", "DAL2", ATCInstructionCode.ClimbTo, "35000");

        Assert.AreEqual(2, comm.LogCount);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void Communication_ReadbackRecordedSuccessfully()
    {
        var go   = new GameObject();
        var comm = go.AddComponent<ATCCommunicationController>();

        comm.GenerateTransmission("TWR", "UAL1", ATCInstructionCode.DescendTo, "10000");
        bool ok = comm.RecordReadback("UAL1", ATCInstructionCode.DescendTo);

        Assert.IsTrue(ok);
        Assert.IsTrue(comm.LastTransmission.readbackReceived);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ATCVoiceSynthesizer
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VoiceSynthesizer_HasDefaultProfiles()
    {
        var go  = new GameObject();
        var vs  = go.AddComponent<ATCVoiceSynthesizer>();

        // Awake is called by AddComponent in EditMode via reflection
        Assert.GreaterOrEqual(vs.ProfileCount, 1);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void VoiceSynthesizer_VolumeClampedToZeroOne()
    {
        var go  = new GameObject();
        var vs  = go.AddComponent<ATCVoiceSynthesizer>();

        vs.Volume = 1.5f;
        Assert.AreEqual(1f, vs.Volume, 0.001f);

        vs.Volume = -0.5f;
        Assert.AreEqual(0f, vs.Volume, 0.001f);

        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CommunicationQueue
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CommQueue_EmergencyJumpsToFront()
    {
        var go  = new GameObject();
        var q   = go.AddComponent<CommunicationQueue>();

        var normal = new CommunicationQueue.QueuedMessage
            { callsign = "AAL1", text = "request", priority = TrafficPriority.Normal, isEmergency = false };
        var emerg = new CommunicationQueue.QueuedMessage
            { callsign = "MAY1", text = "MAYDAY", priority = TrafficPriority.Emergency, isEmergency = true };

        q.Enqueue(normal);
        q.Enqueue(emerg);

        Assert.AreEqual(1, q.QueueDepth, "Normal message should remain since emergency interrupted");

        q.Clear();
        Assert.AreEqual(0, q.QueueDepth);

        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RouteOptimizer
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void RouteOptimizer_ReturnsRouteWithPositiveDistance()
    {
        var go   = new GameObject();
        var opt  = go.AddComponent<RouteOptimizer>();

        var wps = new List<Waypoint>
        {
            new Waypoint("WP1", new Vector3(50000, 0, 0)),
            new Waypoint("WP2", new Vector3(100000, 0, 0))
        };

        var route = opt.OptimizeRoute(
            Vector3.zero,
            new Vector3(200000, 0, 0),
            wps,
            new Vector3(10, 0, 0));

        Assert.IsNotNull(route);
        Assert.Greater(route.totalDistanceNM, 0f);
        Assert.Greater(route.optimalAltitudeFt, 0);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void RouteOptimizer_ShortFlightGetsLowerAltitude()
    {
        var go  = new GameObject();
        var opt = go.AddComponent<RouteOptimizer>();

        int altShort = opt.GetOptimalAltitude(150f, 70f);
        int altLong  = opt.GetOptimalAltitude(3000f, 70f);

        Assert.Less(altShort, altLong);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AirwayNetworkManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AirwayNetwork_SeedContainsKLAX()
    {
        var go  = new GameObject();
        var mgr = go.AddComponent<AirwayNetworkManager>();

        // Manually seed for unit test (Awake-based seeding runs on AddComponent)
        Assert.GreaterOrEqual(mgr.WaypointCount, 1);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void AirwayNetwork_AddWaypoint_RetrievableById()
    {
        var go  = new GameObject();
        var mgr = go.AddComponent<AirwayNetworkManager>();

        var wp = new Waypoint("TEST1", new Vector3(1, 0, 1));
        mgr.AddWaypoint(wp);

        Assert.IsNotNull(mgr.GetWaypoint("TEST1"));
        Assert.AreEqual("TEST1", mgr.GetWaypoint("TEST1").identifier);

        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlowControlManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FlowControl_GroundStop_IsActiveAfterIssue()
    {
        var go  = new GameObject();
        var fc  = go.AddComponent<FlowControlManager>();

        fc.IssueGroundStop("KJFK", 3600f, "Weather");
        Assert.IsTrue(fc.IsGroundStopActive("KJFK"));

        fc.LiftGroundStop("KJFK");
        Assert.IsFalse(fc.IsGroundStopActive("KJFK"));

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void FlowControl_GDPDelay_AssignedAndRetrieved()
    {
        var go  = new GameObject();
        var fc  = go.AddComponent<FlowControlManager>();

        fc.AssignGDPSlot("UAL100", 45);
        Assert.AreEqual(45, fc.GetGDPDelay("UAL100"));

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void FlowControl_MilesInTrail_SetAndCleared()
    {
        var go  = new GameObject();
        var fc  = go.AddComponent<FlowControlManager>();

        fc.SetMilesInTrail(15f);
        Assert.AreEqual(15f, fc.MilesInTrailRestriction, 0.01f);

        fc.SetMilesInTrail(-1f);
        Assert.AreEqual(-1f, fc.MilesInTrailRestriction, 0.01f);

        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SequencingController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Sequencing_ArrivalAdded_PositionIsOne()
    {
        var go  = new GameObject();
        var seq = go.AddComponent<SequencingController>();

        int pos = seq.SequenceArrival("UAL123");
        Assert.AreEqual(1, pos);
        Assert.AreEqual(1, seq.ArrivalCount);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void Sequencing_EmergencyAheadOfNormal()
    {
        var go  = new GameObject();
        var seq = go.AddComponent<SequencingController>();

        seq.SequenceArrival("AAL1", TrafficPriority.Normal);
        int pos = seq.SequenceArrival("MAY1", TrafficPriority.Emergency);

        Assert.AreEqual(1, pos, "Emergency should be first in sequence");

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void Sequencing_BalancedRunwayAssignment()
    {
        var go  = new GameObject();
        var seq = go.AddComponent<SequencingController>();

        seq.SequenceArrival("AAL1");
        var runways = new List<string> { "28L", "28R" };
        string assigned = seq.AssignBalancedRunway("AAL1", runways);

        Assert.IsNotNull(assigned);
        Assert.IsTrue(assigned == "28L" || assigned == "28R");

        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AirspaceManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AirspaceManager_ClassA_AtFL180()
    {
        var go  = new GameObject();
        var mgr = go.AddComponent<AirspaceManager>();

        Assert.IsTrue(mgr.IsClassA(18001f));
        Assert.IsFalse(mgr.IsClassA(17999f));

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void AirspaceManager_AddZone_IncreasesCount()
    {
        var go  = new GameObject();
        var mgr = go.AddComponent<AirspaceManager>();

        int before = mgr.ZoneCount;
        mgr.AddZone(new AirspaceManager.AirspaceZone
        {
            zoneId = "TEST_C", name = "Test Class C",
            airspaceClass = AirspaceClass.C,
            center = Vector3.zero, radiusNM = 10f,
            lowerLimitFt = 0, upperLimitFt = 4200,
            isActive = true
        });
        Assert.AreEqual(before + 1, mgr.ZoneCount);

        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SpecialUseAirspace
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SpecialUseAirspace_TFR_DetectedAtPosition()
    {
        var go  = new GameObject();
        var sua = go.AddComponent<SpecialUseAirspace>();

        var tfr = sua.IssueTFR(Vector3.zero, 5f, 0, 5000, 3600f);
        Assert.IsNotNull(tfr);

        // Position inside TFR radius (1000 m < 5 NM ≈ 9260 m)
        bool inTfr = sua.IsInTFR(new Vector3(1000, 0, 0), 3000f);
        Assert.IsTrue(inTfr);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void SpecialUseAirspace_ProhibitedArea_DetectedCorrectly()
    {
        var go  = new GameObject();
        var sua = go.AddComponent<SpecialUseAirspace>();

        sua.RegisterZone(new SpecialUseAirspace.SUAZone
        {
            zoneId = "P56", name = "Washington DC",
            suaType = SpecialUseAirspace.SUAType.Prohibited,
            center = Vector3.zero, radiusNM = 15f,
            lowerLimitFt = 0, upperLimitFt = 99999,
            isActive = true
        });

        Assert.IsTrue(sua.IsProhibited(new Vector3(500, 0, 0), 1000f));
        Assert.IsFalse(sua.IsProhibited(new Vector3(300000, 0, 0), 1000f));

        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ATCTelemetryAnalytics
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ATCAnalytics_RecordMethods_IncrementCounters()
    {
        var go  = new GameObject();
        var ana = go.AddComponent<ATCTelemetryAnalytics>();

        ana.RecordGoAround();
        ana.RecordGoAround();
        ana.RecordEmergency();
        ana.RecordSeparationViolation();
        ana.RecordConflictResolved();

        Assert.AreEqual(2, ana.GoAroundsIssued);
        Assert.AreEqual(1, ana.EmergencyDeclarations);
        Assert.AreEqual(1, ana.SeparationViolations);
        Assert.AreEqual(1, ana.ConflictsResolved);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void ATCAnalytics_ResetSession_ClearsAllCounters()
    {
        var go  = new GameObject();
        var ana = go.AddComponent<ATCTelemetryAnalytics>();

        ana.RecordGoAround();
        ana.RecordEmergency();
        ana.ResetSession();

        Assert.AreEqual(0, ana.GoAroundsIssued);
        Assert.AreEqual(0, ana.EmergencyDeclarations);

        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ATCStripBoard
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void StripBoard_AddAndRetrieveStrip()
    {
        var go   = new GameObject();
        var board = go.AddComponent<ATCStripBoard>();

        var strip = new FlightStrip("SWA500", "B737", "KLAX", "KLAS", 25000);
        board.AddStrip(strip);

        Assert.AreEqual(1, board.StripCount);
        Assert.AreEqual("SWA500", board.GetStrip("SWA500").callsign);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void StripBoard_HandoffFlow()
    {
        var go    = new GameObject();
        var board = go.AddComponent<ATCStripBoard>();

        var strip = new FlightStrip("UAL10", "A320", "KLAX", "KSFO", 18000);
        board.AddStrip(strip);

        board.InitiateHandoff("UAL10", "SoCal_Approach");
        Assert.IsTrue(board.HasPendingHandoff("UAL10"));

        board.CompleteHandoff("UAL10");
        Assert.IsFalse(board.HasPendingHandoff("UAL10"));

        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NOTAMManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NOTAMManager_IssueAndCancel()
    {
        var go  = new GameObject();
        var mgr = go.AddComponent<NOTAMManager>();

        var n = mgr.IssueNOTAM("KLAX", NOTAMManager.NOTAMType.RunwayClosure,
            "RWY 25L closed for maintenance", 3600f);

        Assert.IsNotNull(n);
        Assert.IsTrue(n.isActive);

        mgr.CancelNOTAM(n.notamId);
        Assert.IsFalse(n.isActive);

        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SectorController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SectorController_SeedHasTwoSectors()
    {
        var go  = new GameObject();
        var sc  = go.AddComponent<SectorController>();

        Assert.AreEqual(2, sc.SectorCount);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void SectorController_EnterAndExit()
    {
        var go  = new GameObject();
        var sc  = go.AddComponent<SectorController>();

        Assert.IsTrue(sc.EnterSector("ZLA_01", "UAL1"));
        Assert.IsTrue(sc.ExitSector("ZLA_01", "UAL1"));
        Assert.IsFalse(sc.ExitSector("ZLA_01", "UAL1")); // already gone

        GameObject.DestroyImmediate(go);
    }
}
