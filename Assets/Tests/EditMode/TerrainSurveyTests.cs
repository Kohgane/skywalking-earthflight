// TerrainSurveyTests.cs — NUnit EditMode tests for Phase 81 Terrain Survey system
using System;
using NUnit.Framework;
using UnityEngine;
using SWEF.TerrainSurvey;

[TestFixture]
public class TerrainSurveyTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // GeologicalClassifier.Classify()
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Classify_HighAltitudeCold_ReturnsGlacier()
    {
        // altitude >= 3000, temperature < 0
        var result = GeologicalClassifier.Classify(3500f, 10f, 0, -10f);
        Assert.AreEqual(GeologicalFeatureType.Glacier, result);
    }

    [Test]
    public void Classify_HighAltitudeSteepSlope_ReturnsVolcano()
    {
        // altitude >= 2500, slope >= 25
        var result = GeologicalClassifier.Classify(3000f, 30f, 0, 5f);
        Assert.AreEqual(GeologicalFeatureType.Volcano, result);
    }

    [Test]
    public void Classify_HighAltitudeMildSlope_ReturnsMountain()
    {
        // altitude >= 2500, slope >= 5 (not steep enough for volcano)
        var result = GeologicalClassifier.Classify(2600f, 15f, 0, 5f);
        Assert.AreEqual(GeologicalFeatureType.Mountain, result);
    }

    [Test]
    public void Classify_MidAltitudeFlatSlope_ReturnsPlateau()
    {
        // altitude >= 1000, slope < 5
        var result = GeologicalClassifier.Classify(1500f, 2f, 0, 10f);
        Assert.AreEqual(GeologicalFeatureType.Plateau, result);
    }

    [Test]
    public void Classify_SteepSlopeLowAltitude_ReturnsRiftValley()
    {
        // slope >= 40, altitude < 300
        var result = GeologicalClassifier.Classify(200f, 45f, 0, 15f);
        Assert.AreEqual(GeologicalFeatureType.RiftValley, result);
    }

    [Test]
    public void Classify_SteepSlopeHighAltitude_ReturnsCanyon()
    {
        // slope >= 40, altitude >= 300
        var result = GeologicalClassifier.Classify(800f, 50f, 0, 12f);
        Assert.AreEqual(GeologicalFeatureType.Canyon, result);
    }

    [Test]
    public void Classify_VeryLowAltitudeNearSea_ReturnsCoastline()
    {
        // altitude <= 50, slope < 5, temperature temperate
        var result = GeologicalClassifier.Classify(20f, 2f, 0, 18f);
        Assert.AreEqual(GeologicalFeatureType.Coastline, result);
    }

    [Test]
    public void Classify_HotTemperature_ReturnsDesert()
    {
        // temperature >= 35, altitude low, slope flat
        var result = GeologicalClassifier.Classify(200f, 3f, 0, 40f);
        Assert.AreEqual(GeologicalFeatureType.Desert, result);
    }

    [Test]
    public void Classify_ColdTemperature_ReturnsTundra()
    {
        // temperature <= -5, altitude not glacier-range
        var result = GeologicalClassifier.Classify(500f, 3f, 0, -10f);
        Assert.AreEqual(GeologicalFeatureType.Tundra, result);
    }

    [Test]
    public void Classify_LowFlatTemperate_ReturnsWetland()
    {
        // altitude < 100, slope < 5, temperature mid-range
        var result = GeologicalClassifier.Classify(60f, 1f, 0, 20f);
        Assert.AreEqual(GeologicalFeatureType.Wetland, result);
    }

    [Test]
    public void Classify_MidAltitudeMildAll_ReturnsForest()
    {
        // mid altitude, mild temperature, moderate slope
        var result = GeologicalClassifier.Classify(600f, 8f, 0, 18f);
        Assert.AreEqual(GeologicalFeatureType.Forest, result);
    }

    [Test]
    public void Classify_DefaultCase_ReturnsPlains()
    {
        // Any combination not matching more specific rules → Plains
        // High altitude flat but not quite plateau-range, mild temperature
        var result = GeologicalClassifier.Classify(300f, 6f, 0, 20f);
        Assert.AreEqual(GeologicalFeatureType.Plains, result);
    }

    // ── Boundary value tests ──────────────────────────────────────────────────

    [Test]
    public void Classify_ExactGlacierAltBoundary_IsGlacier()
    {
        // Exactly at glacier threshold
        var result = GeologicalClassifier.Classify(3000f, 5f, 0, -1f);
        Assert.AreEqual(GeologicalFeatureType.Glacier, result);
    }

    [Test]
    public void Classify_JustBelowGlacierAlt_IsNotGlacier()
    {
        // Just below glacier threshold (2999m) — should be something else
        var result = GeologicalClassifier.Classify(2999f, 5f, 0, -1f);
        Assert.AreNotEqual(GeologicalFeatureType.Glacier, result);
    }

    [Test]
    public void Classify_ExactMountainAlt_IsMountain()
    {
        var result = GeologicalClassifier.Classify(2500f, 10f, 0, 5f);
        Assert.AreEqual(GeologicalFeatureType.Mountain, result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GeologicalClassifier metadata helpers
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GetFeatureDisplayName_AllTypes_ReturnNonEmptyString()
    {
        foreach (GeologicalFeatureType type in Enum.GetValues(typeof(GeologicalFeatureType)))
        {
            string name = GeologicalClassifier.GetFeatureDisplayName(type);
            Assert.IsFalse(string.IsNullOrEmpty(name),
                $"GetFeatureDisplayName({type}) returned null or empty");
        }
    }

    [Test]
    public void GetFeatureColor_AllTypes_ReturnNonDefaultColor()
    {
        foreach (GeologicalFeatureType type in Enum.GetValues(typeof(GeologicalFeatureType)))
        {
            Color c = GeologicalClassifier.GetFeatureColor(type);
            // white is the fallback — all registered types should have a unique color
            Assert.AreNotEqual(Color.white, c,
                $"GetFeatureColor({type}) returned fallback white");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SurveySample serialization round-trip
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SurveySample_JsonRoundTrip_PreservesAllFields()
    {
        var original = new SurveySample
        {
            position    = new Vector3(1234.5f, 567.8f, 910.1f),
            featureType = GeologicalFeatureType.Volcano,
            altitude    = 3200f,
            slope       = 28.5f,
            biomeId     = 7,
            timestamp   = 1700000000L,
        };

        // Unity's JsonUtility requires a wrapper class for structs
        var wrapper = new SurveySampleWrapper { sample = original };
        string json = JsonUtility.ToJson(wrapper);
        var deserialized = JsonUtility.FromJson<SurveySampleWrapper>(json);

        Assert.AreEqual(original.featureType,  deserialized.sample.featureType);
        Assert.AreEqual(original.altitude,     deserialized.sample.altitude,    0.001f);
        Assert.AreEqual(original.slope,        deserialized.sample.slope,       0.001f);
        Assert.AreEqual(original.biomeId,      deserialized.sample.biomeId);
        Assert.AreEqual(original.timestamp,    deserialized.sample.timestamp);
        Assert.AreEqual(original.position.x,   deserialized.sample.position.x,  0.001f);
        Assert.AreEqual(original.position.y,   deserialized.sample.position.y,  0.001f);
        Assert.AreEqual(original.position.z,   deserialized.sample.position.z,  0.001f);
    }

    [Serializable]
    private class SurveySampleWrapper { public SurveySample sample; }

    // ═══════════════════════════════════════════════════════════════════════════
    // TerrainSurveyConfig default values
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TerrainSurveyConfig_DefaultValues_AreReasonable()
    {
        var config = ScriptableObject.CreateInstance<TerrainSurveyConfig>();
        // Invoke Reset() via reflection to apply defaults
        var reset = typeof(TerrainSurveyConfig).GetMethod(
            "Reset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        reset?.Invoke(config, null);

        Assert.Greater(config.scanRadius,         0f,   "scanRadius must be > 0");
        Assert.Greater(config.scanResolution,      0,    "scanResolution must be > 0");
        Assert.Greater(config.cooldown,            0f,   "cooldown must be > 0");
        Assert.Greater(config.maxPOIs,             0,    "maxPOIs must be > 0");
        Assert.Greater(config.proximityThreshold,  0f,   "proximityThreshold must be > 0");
        Assert.Greater(config.mountainAltitudeMin, 0f,   "mountainAltitudeMin must be > 0");
        Assert.Greater(config.glacierAltitudeMin,  config.mountainAltitudeMin,
            "glacierAltitudeMin should be higher than mountainAltitudeMin");
        Assert.Less(config.slopeFlat, config.slopeSteep,
            "slopeFlat should be less than slopeSteep");

        ScriptableObject.DestroyImmediate(config);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SurveyPOI class
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SurveyPOI_Constructor_SetsFieldsCorrectly()
    {
        var pos     = new Vector3(100f, 500f, 200f);
        var type    = GeologicalFeatureType.Mountain;
        const string locKey = "survey_feature_mountain";

        var poi = new SurveyPOI(pos, type, locKey);

        Assert.IsFalse(string.IsNullOrEmpty(poi.id),     "id should not be empty");
        Assert.AreEqual(pos,     poi.position);
        Assert.AreEqual(type,    poi.featureType);
        Assert.AreEqual(locKey,  poi.nameLocKey);
        Assert.IsTrue(poi.isNew, "newly created POI should be marked as new");
        Assert.Greater(poi.discoveredTimestamp, 0L);
    }

    [Test]
    public void SurveyPOI_TwoInstances_HaveDifferentIds()
    {
        var a = new SurveyPOI(Vector3.zero, GeologicalFeatureType.Plains, "k");
        var b = new SurveyPOI(Vector3.zero, GeologicalFeatureType.Plains, "k");
        Assert.AreNotEqual(a.id, b.id);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Enum completeness
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GeologicalFeatureType_HasTwelveValues()
    {
        Assert.AreEqual(12, Enum.GetValues(typeof(GeologicalFeatureType)).Length);
    }

    [Test]
    public void SurveyMode_HasFiveValues()
    {
        Assert.AreEqual(5, Enum.GetValues(typeof(SurveyMode)).Length);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SurveyPOIManager — deduplication and cap (in-process simulation)
    // Note: SurveyPOIManager is a MonoBehaviour singleton; we test the
    // deduplication logic indirectly through DiscoverPOI on a fresh instance.
    // ═══════════════════════════════════════════════════════════════════════════

    private GameObject _managerObj;
    private SurveyPOIManager _manager;

    [SetUp]
    public void SetUpPOIManager()
    {
        _managerObj = new GameObject("SurveyPOIManager");
        _manager    = _managerObj.AddComponent<SurveyPOIManager>();
    }

    [TearDown]
    public void TearDownPOIManager()
    {
        if (_managerObj != null)
            UnityEngine.Object.DestroyImmediate(_managerObj);
    }

    [Test]
    public void SurveyPOIManager_Discover_CreatesNewPOI()
    {
        var sample = MakeSample(new Vector3(0f, 100f, 0f), GeologicalFeatureType.Mountain);
        SurveyPOI poi = _manager.DiscoverPOI(sample);

        Assert.IsNotNull(poi);
        Assert.AreEqual(1, _manager.GetAllPOIs().Count);
    }

    [Test]
    public void SurveyPOIManager_DuplicateWithinThreshold_NotAdded()
    {
        var sample1 = MakeSample(new Vector3(0f, 100f, 0f), GeologicalFeatureType.Mountain);
        var sample2 = MakeSample(new Vector3(10f, 100f, 0f), GeologicalFeatureType.Mountain); // 10m apart < 500m threshold

        _manager.DiscoverPOI(sample1);
        SurveyPOI second = _manager.DiscoverPOI(sample2);

        Assert.IsNull(second,  "POI within proximity threshold should be rejected");
        Assert.AreEqual(1, _manager.GetAllPOIs().Count);
    }

    [Test]
    public void SurveyPOIManager_BeyondThreshold_CreatesNewPOI()
    {
        var sample1 = MakeSample(new Vector3(0f,    100f, 0f), GeologicalFeatureType.Mountain);
        var sample2 = MakeSample(new Vector3(1000f, 100f, 0f), GeologicalFeatureType.Mountain); // 1000m apart > 500m threshold

        _manager.DiscoverPOI(sample1);
        SurveyPOI second = _manager.DiscoverPOI(sample2);

        Assert.IsNotNull(second, "POI beyond proximity threshold should be created");
        Assert.AreEqual(2, _manager.GetAllPOIs().Count);
    }

    [Test]
    public void SurveyPOIManager_MaxCapEnforced_OldestEvicted()
    {
        // Set a tiny cap via config
        var config = ScriptableObject.CreateInstance<TerrainSurveyConfig>();
        var resetMethod = typeof(TerrainSurveyConfig).GetMethod(
            "Reset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        resetMethod?.Invoke(config, null);
        config.maxPOIs            = 3;
        config.proximityThreshold = 1f; // very small so all points are unique

        // Inject config via reflection
        var configField = typeof(SurveyPOIManager).GetField(
            "config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        configField?.SetValue(_manager, config);

        SurveyPOI evicted = null;
        _manager.OnPOIRemoved += p => evicted = p;

        for (int i = 0; i < 4; i++)
        {
            var s = MakeSample(new Vector3(i * 100f, 100f, 0f), GeologicalFeatureType.Plains);
            _manager.DiscoverPOI(s);
        }

        Assert.AreEqual(3, _manager.GetAllPOIs().Count,
            "POI count should not exceed maxPOIs after cap enforcement");
        Assert.IsNotNull(evicted, "OnPOIRemoved should have fired for the evicted POI");

        ScriptableObject.DestroyImmediate(config);
    }

    [Test]
    public void SurveyPOIManager_OnPOIDiscovered_EventFired()
    {
        SurveyPOI received = null;
        _manager.OnPOIDiscovered += p => received = p;

        var sample = MakeSample(new Vector3(0f, 200f, 0f), GeologicalFeatureType.Desert);
        _manager.DiscoverPOI(sample);

        Assert.IsNotNull(received);
        Assert.AreEqual(GeologicalFeatureType.Desert, received.featureType);
    }

    [Test]
    public void SurveyPOIManager_GetPOIsByFeature_FiltersCorrectly()
    {
        var cfg = ScriptableObject.CreateInstance<TerrainSurveyConfig>();
        var resetMethod = typeof(TerrainSurveyConfig).GetMethod(
            "Reset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        resetMethod?.Invoke(cfg, null);
        cfg.proximityThreshold = 1f;
        var configField = typeof(SurveyPOIManager).GetField(
            "config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        configField?.SetValue(_manager, cfg);

        _manager.DiscoverPOI(MakeSample(new Vector3(0f,   100f, 0f), GeologicalFeatureType.Mountain));
        _manager.DiscoverPOI(MakeSample(new Vector3(100f, 100f, 0f), GeologicalFeatureType.Desert));
        _manager.DiscoverPOI(MakeSample(new Vector3(200f, 100f, 0f), GeologicalFeatureType.Mountain));

        var mountains = System.Linq.Enumerable.ToList(_manager.GetPOIsByFeature(GeologicalFeatureType.Mountain));
        var deserts   = System.Linq.Enumerable.ToList(_manager.GetPOIsByFeature(GeologicalFeatureType.Desert));

        Assert.AreEqual(2, mountains.Count);
        Assert.AreEqual(1, deserts.Count);

        ScriptableObject.DestroyImmediate(cfg);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static SurveySample MakeSample(Vector3 pos, GeologicalFeatureType type)
    {
        return new SurveySample
        {
            position    = pos,
            featureType = type,
            altitude    = pos.y,
            slope       = 5f,
            biomeId     = 0,
            timestamp   = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
    }
}
