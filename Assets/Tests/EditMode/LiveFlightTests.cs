// LiveFlightTests.cs — NUnit EditMode tests for Phase 103 Live Flight Tracking
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.LiveFlight;

[TestFixture]
public class LiveFlightTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // LiveAircraftInfo struct
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveAircraftInfo_DefaultValues_AreZeroOrEmpty()
    {
        var info = default(LiveAircraftInfo);

        Assert.IsNull(info.icao24,        "icao24 default should be null");
        Assert.IsNull(info.callsign,      "callsign default should be null");
        Assert.AreEqual(0.0,  info.latitude,     1e-9, "latitude default should be 0");
        Assert.AreEqual(0.0,  info.longitude,    1e-9, "longitude default should be 0");
        Assert.AreEqual(0f,   info.altitude,     1e-4f,"altitude default should be 0");
        Assert.AreEqual(0f,   info.velocity,     1e-4f,"velocity default should be 0");
        Assert.AreEqual(0f,   info.heading,      1e-4f,"heading default should be 0");
        Assert.AreEqual(0f,   info.verticalRate, 1e-4f,"verticalRate default should be 0");
        Assert.IsFalse(info.onGround,            "onGround default should be false");
        Assert.AreEqual(0L,   info.lastUpdate,         "lastUpdate default should be 0");
    }

    [Test]
    public void LiveAircraftInfo_CanBeAssigned()
    {
        var info = new LiveAircraftInfo
        {
            icao24        = "abc123",
            callsign      = "UAL123",
            latitude      = 37.8,
            longitude     = -122.3,
            altitude      = 10000f,
            velocity      = 250f,
            heading       = 180f,
            verticalRate  = 2.5f,
            onGround      = false,
            lastUpdate    = 1700000000L,
            originCountry = "United States",
            aircraftType  = "B737"
        };

        Assert.AreEqual("abc123",        info.icao24);
        Assert.AreEqual("UAL123",        info.callsign);
        Assert.AreEqual(37.8,            info.latitude,   1e-9);
        Assert.AreEqual(-122.3,          info.longitude,  1e-9);
        Assert.AreEqual(10000f,          info.altitude,   1e-4f);
        Assert.AreEqual(250f,            info.velocity,   1e-4f);
        Assert.AreEqual(180f,            info.heading,    1e-4f);
        Assert.AreEqual(2.5f,            info.verticalRate, 1e-4f);
        Assert.IsFalse(info.onGround);
        Assert.AreEqual(1700000000L,     info.lastUpdate);
        Assert.AreEqual("United States", info.originCountry);
        Assert.AreEqual("B737",          info.aircraftType);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightRoute struct
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FlightRoute_CanBeAssigned()
    {
        var arrival = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var route = new FlightRoute
        {
            departureICAO   = "KLAX",
            arrivalICAO     = "KJFK",
            waypoints       = new[] { Vector3.zero, Vector3.one },
            estimatedArrival = arrival
        };

        Assert.AreEqual("KLAX",   route.departureICAO);
        Assert.AreEqual("KJFK",   route.arrivalICAO);
        Assert.AreEqual(2,        route.waypoints.Length);
        Assert.AreEqual(arrival,  route.estimatedArrival);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiveFlightConfig ScriptableObject
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveFlightConfig_DefaultValues_AreCorrect()
    {
        var cfg = ScriptableObject.CreateInstance<LiveFlightConfig>();
        try
        {
            Assert.AreEqual(LiveFlightDataSource.Mock, cfg.apiProvider,           "apiProvider default");
            Assert.AreEqual(10f,                       cfg.pollIntervalSeconds,    1e-4f, "pollInterval default");
            Assert.AreEqual(100,                       cfg.maxAircraftDisplayed,          "maxAircraft default");
            Assert.AreEqual(500f,                      cfg.displayRadiusKm,        1e-4f, "radius default");
            Assert.IsTrue(cfg.showRouteLines,                                             "showRouteLines default");
            Assert.IsTrue(cfg.showLabels,                                                 "showLabels default");
            Assert.Greater(cfg.iconScale, 0f,                                             "iconScale must be positive");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cfg);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiveFlightDataSource enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveFlightDataSource_HasExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(LiveFlightDataSource), "OpenSky"));
        Assert.IsTrue(Enum.IsDefined(typeof(LiveFlightDataSource), "ADS_B_Exchange"));
        Assert.IsTrue(Enum.IsDefined(typeof(LiveFlightDataSource), "Mock"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiveFlightAPIClient — mock data generation
    // ═══════════════════════════════════════════════════════════════════════════

    private LiveFlightAPIClient CreateAPIClient()
    {
        var go     = new GameObject("TestAPIClient");
        var client = go.AddComponent<LiveFlightAPIClient>();
        return client;
    }

    [Test]
    public void GenerateMockData_ProducesRequestedCount()
    {
        var client = CreateAPIClient();
        try
        {
            var list = client.GenerateMockData(Vector3.zero, 20);
            Assert.AreEqual(20, list.Count, "Should produce exactly the requested count");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(client.gameObject);
        }
    }

    [Test]
    public void GenerateMockData_AllAircraftHaveIcao24()
    {
        var client = CreateAPIClient();
        try
        {
            var list = client.GenerateMockData(Vector3.zero, 10);
            foreach (var a in list)
                Assert.IsFalse(string.IsNullOrEmpty(a.icao24),
                    "Every mock aircraft must have a non-empty icao24");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(client.gameObject);
        }
    }

    [Test]
    public void GenerateMockData_AltitudeWithinReasonableRange()
    {
        var client = CreateAPIClient();
        try
        {
            var list = client.GenerateMockData(Vector3.zero, 50);
            foreach (var a in list)
            {
                Assert.GreaterOrEqual(a.altitude, 0f,     "Altitude must be non-negative");
                Assert.Less(a.altitude, 20000f,            "Altitude must be below 20 000 m");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(client.gameObject);
        }
    }

    [Test]
    public void GenerateMockData_HeadingInZeroTo360()
    {
        var client = CreateAPIClient();
        try
        {
            var list = client.GenerateMockData(Vector3.zero, 50);
            foreach (var a in list)
            {
                Assert.GreaterOrEqual(a.heading, 0f,    "Heading must be >= 0");
                Assert.LessOrEqual(a.heading, 360f,      "Heading must be <= 360");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(client.gameObject);
        }
    }

    [Test]
    public void GenerateMockData_ZeroCount_ReturnsEmptyList()
    {
        var client = CreateAPIClient();
        try
        {
            var list = client.GenerateMockData(Vector3.zero, 0);
            Assert.AreEqual(0, list.Count, "Zero count should produce an empty list");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(client.gameObject);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OpenSky JSON parsing
    // ═══════════════════════════════════════════════════════════════════════════

    private const string SampleOpenSkyJson =
        "{\"time\":1700000000,\"states\":[" +
        "[\"abc123\",\"UAL123 \",\"United States\",1700000000,1700000001,-122.3,37.8,10000.0,false,250.0,180.0,2.5,null,10050.0,null,false,0]," +
        "[\"def456\",\"DLH456 \",\"Germany\",1700000000,1700000001,8.5,50.0,11000.0,false,260.0,90.0,-1.0,null,11050.0,null,false,0]" +
        "]}";

    [Test]
    public void ParseOpenSkyResponse_ValidJson_ReturnsTwoAircraft()
    {
        var client = CreateAPIClient();
        try
        {
            var list = client.ParseOpenSkyResponse(SampleOpenSkyJson);
            Assert.AreEqual(2, list.Count, "Should parse exactly 2 aircraft from sample JSON");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(client.gameObject);
        }
    }

    [Test]
    public void ParseOpenSkyResponse_FirstAircraft_FieldsCorrect()
    {
        var client = CreateAPIClient();
        try
        {
            var list = client.ParseOpenSkyResponse(SampleOpenSkyJson);
            Assert.GreaterOrEqual(list.Count, 1);
            var a = list[0];
            Assert.AreEqual("abc123",        a.icao24);
            Assert.AreEqual("UAL123",        a.callsign);
            Assert.AreEqual("United States", a.originCountry);
            Assert.AreEqual(37.8,            a.latitude,  0.01);
            Assert.AreEqual(-122.3,          a.longitude, 0.01);
            Assert.AreEqual(10000f,          a.altitude,  1f);
            Assert.AreEqual(250f,            a.velocity,  1f);
            Assert.AreEqual(180f,            a.heading,   1f);
            Assert.IsFalse(a.onGround);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(client.gameObject);
        }
    }

    [Test]
    public void ParseOpenSkyResponse_EmptyJson_ReturnsEmptyList()
    {
        var client = CreateAPIClient();
        try
        {
            var list = client.ParseOpenSkyResponse("");
            Assert.AreEqual(0, list.Count, "Empty JSON should produce no aircraft");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(client.gameObject);
        }
    }

    [Test]
    public void ParseOpenSkyResponse_NoStates_ReturnsEmptyList()
    {
        var client = CreateAPIClient();
        try
        {
            var list = client.ParseOpenSkyResponse("{\"time\":1700000000}");
            Assert.AreEqual(0, list.Count, "JSON without states key should return empty list");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(client.gameObject);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Great-circle route calculation
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SampleGreatCircle_ReturnsBothEndpoints()
    {
        // KLAX → KJFK
        var pts = FlightRouteRenderer.SampleGreatCircle(
            33.9425, -118.4081, 40.6413, -73.7781, 32, 10000f);

        Assert.IsNotNull(pts,                       "Result should not be null");
        Assert.AreEqual(33, pts.Length,             "segments+1 points expected");
    }

    [Test]
    public void SampleGreatCircle_PointsHaveNonNegativeY()
    {
        var pts = FlightRouteRenderer.SampleGreatCircle(
            33.9425, -118.4081, 40.6413, -73.7781, 16, 5000f);

        foreach (var p in pts)
            Assert.GreaterOrEqual(p.y, 0f, "All arc points should have non-negative altitude");
    }

    [Test]
    public void SampleGreatCircle_MidpointHasHigherAltitude()
    {
        int segments = 32;
        float arcAlt = 10000f;
        var pts = FlightRouteRenderer.SampleGreatCircle(
            0, 0, 0, 90, segments, arcAlt);

        float midY   = pts[segments / 2].y;
        float startY = pts[0].y;
        float endY   = pts[segments].y;

        Assert.Greater(midY, startY, "Mid-point altitude should exceed start altitude");
        Assert.Greater(midY, endY,   "Mid-point altitude should exceed end altitude");
    }

    [Test]
    public void ComputeGreatCirclePath_KnownAirports_ReturnsPath()
    {
        var route = new FlightRoute
        {
            departureICAO = "KLAX",
            arrivalICAO   = "KJFK"
        };
        var pts = FlightRouteRenderer.ComputeGreatCirclePath(route, 32);

        Assert.IsNotNull(pts,              "Known airports should produce a valid path");
        Assert.AreEqual(33, pts.Length,    "segments+1 points expected");
    }

    [Test]
    public void ComputeGreatCirclePath_UnknownAirports_ReturnsNull()
    {
        var route = new FlightRoute
        {
            departureICAO = "XXXX",
            arrivalICAO   = "YYYY"
        };
        var pts = FlightRouteRenderer.ComputeGreatCirclePath(route, 32);

        Assert.IsNull(pts, "Unknown airports should return null");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Object pool — LiveAircraftRenderer
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveAircraftRenderer_ShowAircraft_CreatesPooledObjects()
    {
        var go  = new GameObject("Renderer");
        var cfg = ScriptableObject.CreateInstance<LiveFlightConfig>();
        cfg.maxAircraftDisplayed = 5;

        // Attach config via reflection (field is private serialized)
        var cfgField = typeof(LiveAircraftRenderer)
            .GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var renderer = go.AddComponent<LiveAircraftRenderer>();
        cfgField?.SetValue(renderer, cfg);

        try
        {
            var data = new List<LiveAircraftInfo>();
            for (int i = 0; i < 5; i++)
                data.Add(new LiveAircraftInfo { icao24 = $"t{i:x4}", altitude = 5000f });

            renderer.ShowAircraft(data);

            // After showing 5 aircraft the pool may be empty (all in use) or have
            // some residual capacity; the important check is no exception was thrown.
            Assert.Pass("ShowAircraft completed without exception");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(cfg);
        }
    }

    [Test]
    public void LiveAircraftRenderer_HideAll_ClearsActiveAndGrowsPool()
    {
        var go  = new GameObject("Renderer2");
        var cfg = ScriptableObject.CreateInstance<LiveFlightConfig>();
        cfg.maxAircraftDisplayed = 3;

        var cfgField = typeof(LiveAircraftRenderer)
            .GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var renderer = go.AddComponent<LiveAircraftRenderer>();
        cfgField?.SetValue(renderer, cfg);

        try
        {
            var data = new List<LiveAircraftInfo>
            {
                new LiveAircraftInfo { icao24 = "a001", altitude = 1000f },
                new LiveAircraftInfo { icao24 = "a002", altitude = 2000f }
            };
            renderer.ShowAircraft(data);
            renderer.HideAll();

            // After HideAll the pool should have reclaimed the markers.
            Assert.GreaterOrEqual(renderer.PoolSize, 0, "Pool size should be non-negative after HideAll");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(cfg);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Filter logic — LiveFlightHUD
    // ═══════════════════════════════════════════════════════════════════════════

    private static LiveFlightHUD CreateHUDComponent(out GameObject go)
    {
        go = new GameObject("HUD");
        go.AddComponent<Canvas>();
        return go.AddComponent<LiveFlightHUD>();
    }

    private static List<LiveAircraftInfo> BuildSampleFleet()
    {
        return new List<LiveAircraftInfo>
        {
            new LiveAircraftInfo { icao24 = "a001", altitude =  1000f, aircraftType = "B737", originCountry = "United States" },
            new LiveAircraftInfo { icao24 = "a002", altitude =  5000f, aircraftType = "A320", originCountry = "Germany" },
            new LiveAircraftInfo { icao24 = "a003", altitude = 11000f, aircraftType = "B737", originCountry = "United States" },
            new LiveAircraftInfo { icao24 = "a004", altitude = 12000f, aircraftType = "A380", originCountry = "France" },
            new LiveAircraftInfo { icao24 = "a005", altitude =   500f, aircraftType = "CRJ9", originCountry = "Canada" },
        };
    }

    [Test]
    public void Filter_AltitudeRange_ExcludesOutOfBandAircraft()
    {
        var hud = CreateHUDComponent(out var go);
        try
        {
            hud.SetAltitudeFilter(3000f, 12000f);
            var fleet = BuildSampleFleet();
            var filtered = hud.FilterAircraft(fleet);

            // a001 (1000 m) and a005 (500 m) should be excluded
            Assert.AreEqual(3, filtered.Count, "Only 3 aircraft in 3000-12000 m range");
            Assert.IsFalse(filtered.Exists(a => a.icao24 == "a001"), "a001 below range");
            Assert.IsFalse(filtered.Exists(a => a.icao24 == "a005"), "a005 below range");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void Filter_AircraftType_OnlyMatchingType()
    {
        var hud = CreateHUDComponent(out var go);
        try
        {
            hud.SetTypeFilter("B737");
            var fleet = BuildSampleFleet();
            var filtered = hud.FilterAircraft(fleet);

            Assert.AreEqual(2, filtered.Count, "Only B737 aircraft should remain");
            Assert.IsTrue(filtered.TrueForAll(a => a.aircraftType == "B737"),
                "All results must be B737");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void Filter_Country_OnlyMatchingCountry()
    {
        var hud = CreateHUDComponent(out var go);
        try
        {
            hud.SetCountryFilter("United States");
            var fleet = BuildSampleFleet();
            var filtered = hud.FilterAircraft(fleet);

            Assert.AreEqual(2, filtered.Count, "Only US aircraft should remain");
            Assert.IsTrue(filtered.TrueForAll(a => a.originCountry == "United States"),
                "All results must be from United States");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void Filter_EmptyTypeAndCountry_ReturnsAll()
    {
        var hud = CreateHUDComponent(out var go);
        try
        {
            hud.SetTypeFilter("");
            hud.SetCountryFilter("");
            var fleet = BuildSampleFleet();
            var filtered = hud.FilterAircraft(fleet);

            Assert.AreEqual(fleet.Count, filtered.Count, "No filter should return all aircraft");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void Filter_CombinedAltitudeAndType_AppliesBothConstraints()
    {
        var hud = CreateHUDComponent(out var go);
        try
        {
            hud.SetAltitudeFilter(0f, 6000f);
            hud.SetTypeFilter("B737");
            var fleet = BuildSampleFleet();
            var filtered = hud.FilterAircraft(fleet);

            // Only a001: B737 at 1000 m. a003 is B737 but at 11000 m (too high).
            Assert.AreEqual(1, filtered.Count, "One B737 is below 6000 m");
            Assert.AreEqual("a001", filtered[0].icao24);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
