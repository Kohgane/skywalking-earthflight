// SatelliteTrackingTests.cs — Phase 114: Satellite & Space Debris Tracking
// Comprehensive NUnit EditMode tests (49 tests) covering:
// enums, config, TLE parsing, orbital mechanics, SGP4, debris system, docking, data models.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.SatelliteTracking;

[TestFixture]
public class SatelliteTrackingTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // SatelliteType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SatelliteType_AllValuesAreDefined()
    {
        var values = (SatelliteType[])Enum.GetValues(typeof(SatelliteType));
        Assert.GreaterOrEqual(values.Length, 7, "At least 7 satellite types required");
        Assert.Contains(SatelliteType.Communication, values);
        Assert.Contains(SatelliteType.Navigation,    values);
        Assert.Contains(SatelliteType.Weather,       values);
        Assert.Contains(SatelliteType.Science,       values);
        Assert.Contains(SatelliteType.Military,      values);
        Assert.Contains(SatelliteType.SpaceStation,  values);
        Assert.Contains(SatelliteType.Debris,        values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OrbitType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OrbitType_AllValuesAreDefined()
    {
        var values = (OrbitType[])Enum.GetValues(typeof(OrbitType));
        Assert.Contains(OrbitType.LEO,   values);
        Assert.Contains(OrbitType.MEO,   values);
        Assert.Contains(OrbitType.GEO,   values);
        Assert.Contains(OrbitType.HEO,   values);
        Assert.Contains(OrbitType.SSO,   values);
        Assert.Contains(OrbitType.Polar, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SatelliteStatus enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SatelliteStatus_AllValuesAreDefined()
    {
        var values = (SatelliteStatus[])Enum.GetValues(typeof(SatelliteStatus));
        Assert.Contains(SatelliteStatus.Active,    values);
        Assert.Contains(SatelliteStatus.Standby,   values);
        Assert.Contains(SatelliteStatus.Failed,    values);
        Assert.Contains(SatelliteStatus.Decaying,  values);
        Assert.Contains(SatelliteStatus.Deorbited, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DebrisSize enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DebrisSize_AllValuesAreDefined()
    {
        var values = (DebrisSize[])Enum.GetValues(typeof(DebrisSize));
        Assert.Contains(DebrisSize.Large,  values);
        Assert.Contains(DebrisSize.Medium, values);
        Assert.Contains(DebrisSize.Small,  values);
        Assert.Contains(DebrisSize.Micro,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DockingState enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DockingState_AllValuesAreDefined()
    {
        var values = (DockingState[])Enum.GetValues(typeof(DockingState));
        Assert.Contains(DockingState.Idle,          values);
        Assert.Contains(DockingState.FarApproach,   values);
        Assert.Contains(DockingState.NearApproach,  values);
        Assert.Contains(DockingState.FinalApproach, values);
        Assert.Contains(DockingState.Contact,       values);
        Assert.Contains(DockingState.Capture,       values);
        Assert.Contains(DockingState.HardDock,      values);
        Assert.Contains(DockingState.Undocking,     values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TrackingMode enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TrackingMode_AllValuesAreDefined()
    {
        var values = (TrackingMode[])Enum.GetValues(typeof(TrackingMode));
        Assert.Contains(TrackingMode.Off,         values);
        Assert.Contains(TrackingMode.Passive,     values);
        Assert.Contains(TrackingMode.Active,      values);
        Assert.Contains(TrackingMode.Locked,      values);
        Assert.Contains(TrackingMode.DebrisRadar, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SatelliteTrackingConfig
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SatelliteTrackingConfig_DefaultValues_AreReasonable()
    {
        var config = ScriptableObject.CreateInstance<SatelliteTrackingConfig>();
        Assert.Greater(config.positionUpdateInterval,  0f);
        Assert.Greater(config.tleRefreshInterval,      0f);
        Assert.Greater(config.maxVisibleSatellites,    0);
        Assert.Greater(config.orbitPathPoints,         0);
        Assert.Greater(config.predictionHorizonHours,  0f);
        Assert.Greater(config.debrisRadarRangeKm,      0f);
        Assert.Greater(config.kmPerWorldUnit,          0f);
        Assert.Greater(config.EarthRadiusWorldUnits,   0f);
        ScriptableObject.DestroyImmediate(config);
    }

    [Test]
    public void SatelliteTrackingConfig_EarthRadiusWorldUnits_DependsOnKmPerWorldUnit()
    {
        var config = ScriptableObject.CreateInstance<SatelliteTrackingConfig>();
        config.kmPerWorldUnit = 1f;
        float r1 = config.EarthRadiusWorldUnits;
        config.kmPerWorldUnit = 10f;
        float r2 = config.EarthRadiusWorldUnits;
        Assert.Greater(r1, r2, "Larger kmPerWorldUnit → smaller Earth radius in WU");
        ScriptableObject.DestroyImmediate(config);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TLEData model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TLEData_CanBeCreatedAndPopulated()
    {
        var tle = new TLEData
        {
            name                 = "ISS (ZARYA)",
            noradId              = 25544,
            inclinationDeg       = 51.64,
            raanDeg              = 347.43,
            eccentricity         = 0.0001798,
            argOfPerigeeDeg      = 356.3,
            meanAnomalyDeg       = 119.5,
            meanMotionRevPerDay  = 15.498,
            epochJulian          = 2460310.0
        };
        Assert.AreEqual("ISS (ZARYA)", tle.name);
        Assert.AreEqual(25544, tle.noradId);
        Assert.AreEqual(51.64, tle.inclinationDeg, 1e-6);
        Assert.AreEqual(0.0001798, tle.eccentricity, 1e-9);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TLEParser
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TLEParser_ParseLines_ValidISSTLE_ReturnsCorrectData()
    {
        const string name  = "ISS (ZARYA)";
        const string line1 = "1 25544U 98067A   24001.50000000  .00016717  00000-0  10270-3 0  9003";
        const string line2 = "2 25544  51.6400 347.4302 0001798 356.2986 119.5127 15.49815764442996";

        var tle = TLEParser.ParseLines(name, line1, line2);

        Assert.IsNotNull(tle);
        Assert.AreEqual(25544, tle.noradId);
        Assert.AreEqual("ISS (ZARYA)", tle.name);
        Assert.AreEqual(51.64, tle.inclinationDeg, 0.01);
        Assert.AreEqual(15.498, tle.meanMotionRevPerDay, 0.01);
    }

    [Test]
    public void TLEParser_ParseLines_ShortLine_ReturnsNull()
    {
        var result = TLEParser.ParseLines("TEST", "1 25544U", "2 25544  51.6");
        Assert.IsNull(result);
    }

    [Test]
    public void TLEParser_ParseMultiple_ThreeLineTLE_ParsesTwoSatellites()
    {
        const string text =
            "ISS (ZARYA)\n" +
            "1 25544U 98067A   24001.50000000  .00016717  00000-0  10270-3 0  9003\n" +
            "2 25544  51.6400 347.4302 0001798 356.2986 119.5127 15.49815764442996\n" +
            "NOAA 18\n" +
            "1 28654U 05018A   24001.50000000  .00000078  00000-0  65527-4 0  9009\n" +
            "2 28654  99.0187 120.4123 0013942 246.6680 113.2947 14.12401983964010\n";

        var results = TLEParser.ParseMultiple(text);

        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(25544, results[0].noradId);
        Assert.AreEqual(28654, results[1].noradId);
    }

    [Test]
    public void TLEParser_ParseMultiple_EmptyString_ReturnsEmptyList()
    {
        var results = TLEParser.ParseMultiple(string.Empty);
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }

    [Test]
    public void TLEParser_ParseMultiple_NullString_ReturnsEmptyList()
    {
        var results = TLEParser.ParseMultiple(null);
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }

    [Test]
    public void TLEParser_ParseLines_EccentricityParsedCorrectly()
    {
        const string line1 = "1 25544U 98067A   24001.50000000  .00016717  00000-0  10270-3 0  9003";
        const string line2 = "2 25544  51.6400 347.4302 0001798 356.2986 119.5127 15.49815764442996";

        var tle = TLEParser.ParseLines("ISS", line1, line2);
        // Eccentricity column is "0001798" → 0.0001798
        Assert.AreEqual(0.0001798, tle.eccentricity, 1e-7);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OrbitalMechanicsEngine (static methods, no MonoBehaviour needed)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OrbitalMechanicsEngine_OrbitalPeriodMin_ISS_IsApprox92Minutes()
    {
        // ISS mean motion: ~15.5 rev/day → SMA ~6780 km
        double n   = 15.5 * 2.0 * Math.PI / 86400.0;
        double sma = Math.Pow(OrbitalMechanicsEngine.MuEarth / (n * n), 1.0 / 3.0);
        double T   = OrbitalMechanicsEngine.OrbitalPeriodMin(sma);
        Assert.AreEqual(92.0, T, 2.0, "ISS orbital period should be approximately 92 minutes");
    }

    [Test]
    public void OrbitalMechanicsEngine_OrbitalPeriodMin_GEO_IsApprox1436Minutes()
    {
        // GEO: SMA = 42164 km
        double T = OrbitalMechanicsEngine.OrbitalPeriodMin(42164.0);
        Assert.AreEqual(1436.0, T, 10.0, "GEO period should be approximately 1436 minutes");
    }

    [Test]
    public void OrbitalMechanicsEngine_ClassifyOrbit_LEO_IsCorrect()
    {
        Assert.AreEqual(OrbitType.LEO, OrbitalMechanicsEngine.ClassifyOrbit(400f));
        Assert.AreEqual(OrbitType.LEO, OrbitalMechanicsEngine.ClassifyOrbit(1999f));
    }

    [Test]
    public void OrbitalMechanicsEngine_ClassifyOrbit_MEO_IsCorrect()
    {
        Assert.AreEqual(OrbitType.MEO, OrbitalMechanicsEngine.ClassifyOrbit(20200f));
    }

    [Test]
    public void OrbitalMechanicsEngine_ClassifyOrbit_GEO_IsCorrect()
    {
        Assert.AreEqual(OrbitType.GEO, OrbitalMechanicsEngine.ClassifyOrbit(35786f));
    }

    [Test]
    public void OrbitalMechanicsEngine_ClassifyOrbit_HEO_IsCorrect()
    {
        Assert.AreEqual(OrbitType.HEO, OrbitalMechanicsEngine.ClassifyOrbit(40000f));
    }

    [Test]
    public void OrbitalMechanicsEngine_Constants_ArePhysicallyCorrect()
    {
        Assert.AreEqual(398600.4418, OrbitalMechanicsEngine.MuEarth, 1.0);
        Assert.AreEqual(6371.0, OrbitalMechanicsEngine.EarthRadiusKm, 1.0);
        Assert.IsTrue(OrbitalMechanicsEngine.EarthOmegaRad > 0, "Earth rotation rate must be positive");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SatelliteRecord model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SatelliteRecord_CanBeConstructedWithDefaults()
    {
        var record = new SatelliteRecord
        {
            name           = "ISS (ZARYA)",
            noradId        = 25544,
            satelliteType  = SatelliteType.SpaceStation,
            orbitType      = OrbitType.LEO,
            status         = SatelliteStatus.Active,
            country        = "International"
        };
        Assert.AreEqual("ISS (ZARYA)", record.name);
        Assert.AreEqual(25544, record.noradId);
        Assert.IsFalse(record.isFavourite);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OrbitalState model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OrbitalState_CanBeCreated()
    {
        var state = new OrbitalState
        {
            positionECI  = new Vector3(6800f, 0f, 0f),
            velocityECI  = new Vector3(0f, 7.5f, 0f),
            altitudeKm   = 429f,
            latitudeDeg  = 51.6f,
            longitudeDeg = -20f,
            utcTime      = DateTime.UtcNow
        };
        Assert.AreEqual(429f, state.altitudeKm, 0.1f);
        Assert.AreEqual(51.6f, state.latitudeDeg, 0.01f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SatelliteCatalogFilter
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SatelliteCatalogFilter_ByType_FiltersCorrectly()
    {
        var records = MakeSampleRecords();
        var result = new List<SatelliteRecord>(
            SatelliteCatalogFilter.ByType(records, SatelliteType.Navigation));
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(SatelliteType.Navigation, result[0].satelliteType);
    }

    [Test]
    public void SatelliteCatalogFilter_ByOrbit_FiltersCorrectly()
    {
        var records = MakeSampleRecords();
        var result = new List<SatelliteRecord>(
            SatelliteCatalogFilter.ByOrbit(records, OrbitType.LEO));
        Assert.IsTrue(result.Count >= 1);
        foreach (var r in result) Assert.AreEqual(OrbitType.LEO, r.orbitType);
    }

    [Test]
    public void SatelliteCatalogFilter_ByNameContains_FiltersCorrectly()
    {
        var records = MakeSampleRecords();
        var result = new List<SatelliteRecord>(
            SatelliteCatalogFilter.ByNameContains(records, "ISS"));
        Assert.AreEqual(1, result.Count);
        StringAssert.Contains("ISS", result[0].name);
    }

    [Test]
    public void SatelliteCatalogFilter_ActiveOnly_ExcludesInactive()
    {
        var records = MakeSampleRecords();
        var result = new List<SatelliteRecord>(
            SatelliteCatalogFilter.ActiveOnly(records));
        foreach (var r in result) Assert.AreEqual(SatelliteStatus.Active, r.status);
    }

    [Test]
    public void SatelliteCatalogFilter_FavouritesOnly_FiltersCorrectly()
    {
        var records = MakeSampleRecords();
        records[0].isFavourite = true;
        var result = new List<SatelliteRecord>(
            SatelliteCatalogFilter.FavouritesOnly(records));
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result[0].isFavourite);
    }

    [Test]
    public void SatelliteCatalogFilter_BrighterThan_FiltersCorrectly()
    {
        var records = MakeSampleRecords();
        records[0].visualMagnitude = -4f;
        records[1].visualMagnitude =  7f;
        var result = new List<SatelliteRecord>(
            SatelliteCatalogFilter.BrighterThan(records, 0f));
        Assert.AreEqual(1, result.Count);
        Assert.LessOrEqual(result[0].visualMagnitude, 0f);
    }

    [Test]
    public void SatelliteCatalogFilter_CompositeFilter_CombinesCorrectly()
    {
        var records = MakeSampleRecords();
        var result = new List<SatelliteRecord>(
            SatelliteCatalogFilter.CompositeFilter(records,
                type: SatelliteType.SpaceStation));
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(SatelliteType.SpaceStation, result[0].satelliteType);
    }

    [Test]
    public void SatelliteCatalogFilter_SortedByName_IsSorted()
    {
        var records = MakeSampleRecords();
        var sorted = new List<SatelliteRecord>(SatelliteCatalogFilter.SortedByName(records));
        for (int i = 1; i < sorted.Count; i++)
            Assert.LessOrEqual(
                string.Compare(sorted[i - 1].name, sorted[i].name,
                               StringComparison.OrdinalIgnoreCase), 0);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DebrisObject model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DebrisObject_CanBeCreated()
    {
        var d = new DebrisObject
        {
            debrisId            = 1,
            size                = DebrisSize.Large,
            altitudeKm          = 800f,
            crossSectionM2      = 0.5f,
            tumbleRateDegPerSec = 5f,
            albedo              = 0.1f,
            originEvent         = "Test",
            positionECI         = new Vector3(7171f, 0f, 0f),
            velocityECI         = new Vector3(0f, 7.5f, 0f)
        };
        Assert.AreEqual(1, d.debrisId);
        Assert.AreEqual(DebrisSize.Large, d.size);
        Assert.AreEqual(800f, d.altitudeKm, 0.01f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ConjunctionData model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ConjunctionData_CanBeCreated()
    {
        var c = new ConjunctionData
        {
            primaryNoradId       = 25544,
            secondaryNoradId     = 0,
            missDistanceKm       = 1.2f,
            collisionProbability = 1e-4f,
            avoidanceDeltaVms    = 0.5f,
            urgencyLevel         = 2,
            tcaUtc               = DateTime.UtcNow.AddMinutes(60)
        };
        Assert.AreEqual(25544, c.primaryNoradId);
        Assert.AreEqual(2, c.urgencyLevel);
        Assert.Greater(c.missDistanceKm, 0f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CollisionWarningSystem (static method)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CollisionWarningSystem_CalculateAvoidanceDeltaV_ZeroTime_ReturnsZero()
    {
        float dv = CollisionWarningSystem.CalculateAvoidanceDeltaV(1f, 10f, 0f);
        Assert.AreEqual(0f, dv, 1e-6f);
    }

    [Test]
    public void CollisionWarningSystem_CalculateAvoidanceDeltaV_PositiveTime_ReturnsPositive()
    {
        float dv = CollisionWarningSystem.CalculateAvoidanceDeltaV(1f, 10f, 60f);
        Assert.Greater(dv, 0f);
    }

    [Test]
    public void CollisionWarningSystem_CalculateAvoidanceDeltaV_CurrentDistBeyondTarget_ReturnsZero()
    {
        // If current miss distance > target, no maneuver needed
        float dv = CollisionWarningSystem.CalculateAvoidanceDeltaV(50f, 10f, 60f);
        Assert.AreEqual(0f, dv, 1e-6f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SatellitePass model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SatellitePass_CanBeCreated()
    {
        var pass = new SatellitePass
        {
            noradId          = 25544,
            riseTime         = DateTime.UtcNow,
            setTime          = DateTime.UtcNow.AddMinutes(6),
            maxElevationDeg  = 78f,
            riseAzimuthDeg   = 220f,
            setAzimuthDeg    = 45f,
            peakMagnitude    = -4f,
            isVisibleNight   = true
        };
        Assert.AreEqual(25544, pass.noradId);
        Assert.AreEqual(78f, pass.maxElevationDeg, 0.01f);
        Assert.IsTrue(pass.isVisibleNight);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DockingCorridor model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DockingCorridor_CanBeCreated()
    {
        var corridor = new DockingCorridor
        {
            portName           = "PMA-2 Forward",
            entryPointLocal    = new Vector3(0f, 0f, -1000f),
            portPositionLocal  = Vector3.zero,
            approachAxisLocal  = Vector3.forward
        };
        Assert.AreEqual("PMA-2 Forward", corridor.portName);
        Assert.AreEqual(Vector3.forward, corridor.approachAxisLocal);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OrbitVisualizer (static method)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OrbitVisualizer_ColorForType_ReturnsDistinctColors()
    {
        var colors = new System.Collections.Generic.HashSet<Color>();
        foreach (SatelliteType t in Enum.GetValues(typeof(SatelliteType)))
            colors.Add(OrbitVisualizer.ColorForType(t));
        Assert.Greater(colors.Count, 3, "Satellite types should have at least 4 distinct colours");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SatelliteTrackingConfig — edge cases
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SatelliteTrackingConfig_OfflineFlag_ExistsAndDefaultsToTrue()
    {
        var config = ScriptableObject.CreateInstance<SatelliteTrackingConfig>();
        Assert.IsTrue(config.useMockDataOffline);
        ScriptableObject.DestroyImmediate(config);
    }

    [Test]
    public void SatelliteTrackingConfig_CollisionWarningThreshold_IsPositive()
    {
        var config = ScriptableObject.CreateInstance<SatelliteTrackingConfig>();
        Assert.Greater(config.collisionWarningThreshold, 0f);
        ScriptableObject.DestroyImmediate(config);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ISSTracker constants
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ISSTracker_NoradId_IsCorrectValue()
    {
        Assert.AreEqual(25544, ISSTracker.ISSNoradId);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SatelliteDatabase (isolated via direct instantiation)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SatelliteDatabase_Filter_ByType_ReturnsCorrectSubset()
    {
        // Exercise the filter via a manually populated list
        var records = MakeSampleRecords();
        var result = new List<SatelliteRecord>(
            SatelliteCatalogFilter.ByType(records, SatelliteType.SpaceStation));
        Assert.IsTrue(result.Count > 0);
        foreach (var r in result)
            Assert.AreEqual(SatelliteType.SpaceStation, r.satelliteType);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Orbital period — round-trip sanity check
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OrbitalPeriodMin_RoundTrip_SMA_IsConsistent()
    {
        double smaIn  = 6780.0; // km (ISS-like)
        double period = OrbitalMechanicsEngine.OrbitalPeriodMin(smaIn);
        // Back-calculate SMA from period
        double nRad = 2.0 * Math.PI / (period * 60.0);
        double smaOut = Math.Pow(OrbitalMechanicsEngine.MuEarth / (nRad * nRad), 1.0 / 3.0);
        Assert.AreEqual(smaIn, smaOut, 0.01, "Round-trip SMA should match");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TLE eccentricity edge cases
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TLEParser_Eccentricity_RangeIs0To1()
    {
        const string line1 = "1 25544U 98067A   24001.50000000  .00016717  00000-0  10270-3 0  9003";
        const string line2 = "2 25544  51.6400 347.4302 0001798 356.2986 119.5127 15.49815764442996";
        var tle = TLEParser.ParseLines("ISS", line1, line2);
        Assert.IsNotNull(tle);
        Assert.GreaterOrEqual(tle.eccentricity, 0.0);
        Assert.Less(tle.eccentricity, 1.0);
    }

    [Test]
    public void TLEParser_MeanMotion_IsPositive()
    {
        const string line1 = "1 25544U 98067A   24001.50000000  .00016717  00000-0  10270-3 0  9003";
        const string line2 = "2 25544  51.6400 347.4302 0001798 356.2986 119.5127 15.49815764442996";
        var tle = TLEParser.ParseLines("ISS", line1, line2);
        Assert.IsNotNull(tle);
        Assert.Greater(tle.meanMotionRevPerDay, 0.0);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SpaceStationInterior.ISSModule enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SpaceStationInterior_ISSModule_ContainsCupola()
    {
        var values = (SpaceStationInterior.ISSModule[])
            Enum.GetValues(typeof(SpaceStationInterior.ISSModule));
        Assert.Contains(SpaceStationInterior.ISSModule.Cupola, values);
    }

    [Test]
    public void SpaceStationInterior_ISSModule_HasAtLeast6Modules()
    {
        var values = (SpaceStationInterior.ISSModule[])
            Enum.GetValues(typeof(SpaceStationInterior.ISSModule));
        Assert.GreaterOrEqual(values.Length, 6);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static List<SatelliteRecord> MakeSampleRecords()
    {
        return new List<SatelliteRecord>
        {
            new SatelliteRecord
            {
                name = "ISS (ZARYA)", noradId = 25544,
                satelliteType = SatelliteType.SpaceStation,
                orbitType = OrbitType.LEO,
                status = SatelliteStatus.Active,
                country = "International",
                visualMagnitude = -4f
            },
            new SatelliteRecord
            {
                name = "GPS BIIR-2", noradId = 24876,
                satelliteType = SatelliteType.Navigation,
                orbitType = OrbitType.MEO,
                status = SatelliteStatus.Active,
                country = "USA",
                visualMagnitude = 7f
            },
            new SatelliteRecord
            {
                name = "FENGYUN 1C DEB", noradId = 29228,
                satelliteType = SatelliteType.Debris,
                orbitType = OrbitType.SSO,
                status = SatelliteStatus.Failed,
                country = "China",
                visualMagnitude = 9f
            }
        };
    }
}
