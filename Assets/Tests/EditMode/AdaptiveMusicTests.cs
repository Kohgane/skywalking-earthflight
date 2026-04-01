// AdaptiveMusicTests.cs — NUnit tests for Phase 83: Dynamic Soundtrack & Adaptive Music System
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.AdaptiveMusic;

/// <summary>
/// Edit-mode NUnit tests for the Adaptive Music System.
/// Covers MoodResolver, IntensityController, BeatSyncClock, FlightMusicContext defaults,
/// and MusicTransitionController anti-flicker logic.
/// </summary>
[TestFixture]
public class AdaptiveMusicTests
{
    // ─────────────────────────────────────────────────────────────────────────────
    // FlightMusicContext defaults
    // ─────────────────────────────────────────────────────────────────────────────

    [Test]
    public void FlightMusicContext_Default_HasExpectedValues()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();

        Assert.AreEqual(0f,       ctx.altitude,         1e-5f, "Default altitude");
        Assert.AreEqual(0f,       ctx.speed,            1e-5f, "Default speed");
        Assert.AreEqual(1f,       ctx.gForce,           1e-5f, "Default gForce");
        Assert.IsFalse(ctx.isFlying,                          "Default isFlying");
        Assert.AreEqual(0f,       ctx.weatherIntensity, 1e-5f, "Default weatherIntensity");
        Assert.IsFalse(ctx.inStorm,                           "Default inStorm");
        Assert.AreEqual(12f,      ctx.timeOfDay,        1e-5f, "Default timeOfDay");
        Assert.AreEqual(0f,       ctx.dangerLevel,      1e-5f, "Default dangerLevel");
        Assert.IsFalse(ctx.hasActiveEmergency,                "Default hasActiveEmergency");
        Assert.AreEqual(0f,       ctx.damageLevel,      1e-5f, "Default damageLevel");
        Assert.IsFalse(ctx.stallWarning,                      "Default stallWarning");
        Assert.IsFalse(ctx.isLanding,                         "Default isLanding");
        Assert.IsFalse(ctx.isInSpace,                         "Default isInSpace");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MoodResolver.ResolveMood — priority rules
    // ─────────────────────────────────────────────────────────────────────────────

    [Test]
    public void ResolveMood_ActiveEmergency_ReturnsDanger()
    {
        FlightMusicContext ctx  = FlightMusicContext.Default();
        ctx.hasActiveEmergency  = true;
        Assert.AreEqual(MusicMood.Danger, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_HighDamage_ReturnsDanger()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.damageLevel        = 0.65f; // > 0.6 threshold
        Assert.AreEqual(MusicMood.Danger, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_DamageBelow60Percent_DoesNotTriggerDanger()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.damageLevel        = 0.5f; // below threshold
        MusicMood mood         = MoodResolver.ResolveMood(ctx);
        Assert.AreNotEqual(MusicMood.Danger, mood);
    }

    [Test]
    public void ResolveMood_HighGForce_ReturnsTense()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.gForce             = 3.5f;
        Assert.AreEqual(MusicMood.Tense, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_StallWarning_ReturnsTense()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.stallWarning       = true;
        Assert.AreEqual(MusicMood.Tense, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_StormWeather_ReturnsTense()
    {
        FlightMusicContext ctx   = FlightMusicContext.Default();
        ctx.weatherIntensity     = 0.8f; // > 0.7 threshold
        ctx.inStorm              = true;
        Assert.AreEqual(MusicMood.Tense, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_SpaceAltitude_ReturnsEpic()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.altitude           = 110_000f; // > 100km
        ctx.isInSpace          = true;
        Assert.AreEqual(MusicMood.Epic, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_MissionJustCompleted_ReturnsTriumphant()
    {
        FlightMusicContext ctx   = FlightMusicContext.Default();
        ctx.missionJustCompleted = true;
        Assert.AreEqual(MusicMood.Triumphant, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_GoldenHour_ReturnsSerene()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.sunAltitudeDeg     = 3f; // 0–6° = golden hour
        ctx.weatherIntensity   = 0.0f;
        Assert.AreEqual(MusicMood.Serene, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_NightClearWeather_ReturnsMysterious()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.timeOfDay          = 23f; // night
        ctx.sunAltitudeDeg     = -20f; // below horizon (not golden hour)
        ctx.weatherIntensity   = 0.1f; // clear
        Assert.AreEqual(MusicMood.Mysterious, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_HighSpeed_ReturnsAdventurous()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.speed              = 300f; // > Mach 0.8 ≈ 272 m/s
        ctx.timeOfDay          = 12f;  // midday, not night
        ctx.sunAltitudeDeg     = 45f;  // not golden hour
        Assert.AreEqual(MusicMood.Adventurous, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_SmoothCruise_ReturnsCruising()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.isFlying           = true;
        ctx.speed              = 150f;  // moderate speed, below Mach 0.8
        ctx.gForce             = 1.0f;  // normal G
        ctx.timeOfDay          = 12f;
        ctx.sunAltitudeDeg     = 45f;
        ctx.weatherIntensity   = 0.0f;
        Assert.AreEqual(MusicMood.Cruising, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_LowAltitudeCalmSlow_ReturnsPeaceful()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.altitude           = 50f;
        ctx.speed              = 20f;
        ctx.weatherIntensity   = 0.1f;
        ctx.isFlying           = false;
        ctx.sunAltitudeDeg     = 45f;
        ctx.timeOfDay          = 12f;
        Assert.AreEqual(MusicMood.Peaceful, MoodResolver.ResolveMood(ctx));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MoodResolver priority — higher priorities beat lower ones
    // ─────────────────────────────────────────────────────────────────────────────

    [Test]
    public void ResolveMood_DangerBeatsHighSpeed()
    {
        FlightMusicContext ctx  = FlightMusicContext.Default();
        ctx.hasActiveEmergency  = true;
        ctx.speed               = 500f; // would be Adventurous
        Assert.AreEqual(MusicMood.Danger, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_TenseBeatsHighSpeed()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.gForce             = 4f;
        ctx.speed              = 500f;
        Assert.AreEqual(MusicMood.Tense, MoodResolver.ResolveMood(ctx));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MoodResolver.ResolveIntensity
    // ─────────────────────────────────────────────────────────────────────────────

    [Test]
    public void ResolveIntensity_Danger_WithEmergency_IsHigh()
    {
        FlightMusicContext ctx  = FlightMusicContext.Default();
        ctx.hasActiveEmergency  = true;
        float intensity         = MoodResolver.ResolveIntensity(ctx, MusicMood.Danger);
        Assert.GreaterOrEqual(intensity, 0.7f, "Emergency should produce high intensity");
    }

    [Test]
    public void ResolveIntensity_AlwaysInRange()
    {
        FlightMusicContext ctx = FlightMusicContext.Default();
        ctx.altitude          = 5000f;
        ctx.speed             = 200f;
        ctx.gForce            = 2f;

        foreach (MusicMood mood in System.Enum.GetValues(typeof(MusicMood)))
        {
            float intensity = MoodResolver.ResolveIntensity(ctx, mood);
            Assert.GreaterOrEqual(intensity, 0f, $"{mood}: intensity below 0");
            Assert.LessOrEqual(intensity,   1f, $"{mood}: intensity above 1");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // IntensityController.GetActiveLayersForIntensity
    // ─────────────────────────────────────────────────────────────────────────────

    [Test]
    public void GetActiveLayers_ZeroIntensity_PadsOnly()
    {
        List<MusicLayer> layers = IntensityController.GetActiveLayersForIntensity(0f);
        Assert.Contains(MusicLayer.Pads, layers);
        Assert.IsFalse(layers.Contains(MusicLayer.Strings),    "No Strings at 0");
        Assert.IsFalse(layers.Contains(MusicLayer.Melody),     "No Melody at 0");
        Assert.IsFalse(layers.Contains(MusicLayer.Bass),       "No Bass at 0");
        Assert.IsFalse(layers.Contains(MusicLayer.Drums),      "No Drums at 0");
        Assert.IsFalse(layers.Contains(MusicLayer.Percussion), "No Percussion at 0");
        Assert.IsFalse(layers.Contains(MusicLayer.Choir),      "No Choir at 0");
        Assert.IsFalse(layers.Contains(MusicLayer.Synth),      "No Synth at 0");
    }

    [Test]
    public void GetActiveLayers_0_3_PadsAndStrings()
    {
        List<MusicLayer> layers = IntensityController.GetActiveLayersForIntensity(0.3f);
        Assert.Contains(MusicLayer.Pads,    layers);
        Assert.Contains(MusicLayer.Strings, layers);
        Assert.IsFalse(layers.Contains(MusicLayer.Melody), "No Melody at 0.3");
    }

    [Test]
    public void GetActiveLayers_0_5_IncludesMelodyAndBass()
    {
        List<MusicLayer> layers = IntensityController.GetActiveLayersForIntensity(0.5f);
        Assert.Contains(MusicLayer.Pads,    layers);
        Assert.Contains(MusicLayer.Strings, layers);
        Assert.Contains(MusicLayer.Melody,  layers);
        Assert.Contains(MusicLayer.Bass,    layers);
        Assert.IsFalse(layers.Contains(MusicLayer.Drums), "No Drums at 0.5");
    }

    [Test]
    public void GetActiveLayers_0_7_IncludesDrumsAndPercussion()
    {
        List<MusicLayer> layers = IntensityController.GetActiveLayersForIntensity(0.7f);
        Assert.Contains(MusicLayer.Drums,      layers);
        Assert.Contains(MusicLayer.Percussion, layers);
        Assert.IsFalse(layers.Contains(MusicLayer.Choir), "No Choir at 0.7");
    }

    [Test]
    public void GetActiveLayers_1_0_AllLayers()
    {
        List<MusicLayer> layers = IntensityController.GetActiveLayersForIntensity(1f);
        foreach (MusicLayer layer in System.Enum.GetValues(typeof(MusicLayer)))
            Assert.Contains(layer, layers, $"Missing layer {layer} at intensity 1.0");
    }

    [Test]
    public void GetActiveLayers_BoundaryAt0_2_IncludesStrings()
    {
        List<MusicLayer> layers = IntensityController.GetActiveLayersForIntensity(0.2f);
        Assert.Contains(MusicLayer.Strings, layers, "Strings should appear at exactly 0.2");
    }

    [Test]
    public void GetActiveLayers_BoundaryAt0_4_IncludesMelodyBass()
    {
        List<MusicLayer> layers = IntensityController.GetActiveLayersForIntensity(0.4f);
        Assert.Contains(MusicLayer.Melody, layers, "Melody should appear at exactly 0.4");
        Assert.Contains(MusicLayer.Bass,   layers, "Bass should appear at exactly 0.4");
    }

    [Test]
    public void GetActiveLayers_BoundaryAt0_8_IncludesChoirSynth()
    {
        List<MusicLayer> layers = IntensityController.GetActiveLayersForIntensity(0.8f);
        Assert.Contains(MusicLayer.Choir, layers, "Choir should appear at exactly 0.8");
        Assert.Contains(MusicLayer.Synth, layers, "Synth should appear at exactly 0.8");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // BeatSyncClock
    // ─────────────────────────────────────────────────────────────────────────────

    [Test]
    public void BeatSyncClock_InitialBeatCount_IsZero()
    {
        GameObject go    = new GameObject("Clock");
        BeatSyncClock clock = go.AddComponent<BeatSyncClock>();

        Assert.AreEqual(0, clock.GetCurrentBeat(), "Beat count should start at 0");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void BeatSyncClock_SetBPM_UpdatesBPM()
    {
        GameObject go    = new GameObject("Clock");
        BeatSyncClock clock = go.AddComponent<BeatSyncClock>();
        clock.SetBPM(90f);

        Assert.AreEqual(90f, clock.GetCurrentBPM(), 0.01f, "BPM should update");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void BeatSyncClock_SetBPM_ClampsToValidRange()
    {
        GameObject go    = new GameObject("Clock");
        BeatSyncClock clock = go.AddComponent<BeatSyncClock>();

        clock.SetBPM(0f);
        Assert.GreaterOrEqual(clock.GetCurrentBPM(), 20f, "BPM must be >= 20");

        clock.SetBPM(999f);
        Assert.LessOrEqual(clock.GetCurrentBPM(), 400f, "BPM must be <= 400");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void BeatSyncClock_GetBarProgress_IsZeroInitially()
    {
        GameObject go    = new GameObject("Clock");
        BeatSyncClock clock = go.AddComponent<BeatSyncClock>();

        float progress = clock.GetBarProgress01();
        Assert.GreaterOrEqual(progress, 0f);
        Assert.LessOrEqual(progress,   1f);

        Object.DestroyImmediate(go);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MusicTransitionController — anti-flicker (minimum mood hold time)
    // ─────────────────────────────────────────────────────────────────────────────

    [Test]
    public void TransitionController_InitialMood_IsPeaceful()
    {
        GameObject go      = new GameObject("Transition");
        MusicTransitionController ctrl = go.AddComponent<MusicTransitionController>();

        Assert.AreEqual(MusicMood.Peaceful, ctrl.CurrentMood,
            "Default mood should be Peaceful");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void TransitionController_RequestSameMood_DoesNotQueue()
    {
        GameObject go      = new GameObject("Transition");
        MusicTransitionController ctrl = go.AddComponent<MusicTransitionController>();

        // Request the same mood that's already active
        ctrl.RequestMood(MusicMood.Peaceful);
        Assert.AreEqual(MusicMood.Peaceful, ctrl.CurrentMood,
            "Mood should not change when requesting the same mood");

        Object.DestroyImmediate(go);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Enum completeness
    // ─────────────────────────────────────────────────────────────────────────────

    [Test]
    public void MusicMood_HasNineValues()
    {
        int count = System.Enum.GetValues(typeof(MusicMood)).Length;
        Assert.AreEqual(9, count, "MusicMood should have exactly 9 values");
    }

    [Test]
    public void MusicLayer_HasEightValues()
    {
        int count = System.Enum.GetValues(typeof(MusicLayer)).Length;
        Assert.AreEqual(8, count, "MusicLayer should have exactly 8 values");
    }
}
