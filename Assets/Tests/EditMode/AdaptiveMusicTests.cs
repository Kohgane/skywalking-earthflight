// AdaptiveMusicTests.cs — NUnit EditMode tests for Phase 83 Adaptive Music System
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.AdaptiveMusic;

[TestFixture]
public class AdaptiveMusicTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // MoodResolver — all 10 priority rules
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ResolveMood_CombatZone_ReturnsDanger()
    {
        var ctx = FlightMusicContext.Default();
        ctx.isInCombatZone = true;
        Assert.AreEqual(MusicMood.Danger, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_MaxDangerLevel_ReturnsDanger()
    {
        var ctx = FlightMusicContext.Default();
        ctx.dangerLevel = 1f;
        Assert.AreEqual(MusicMood.Danger, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_DamageAbove60Pct_ReturnsDanger()
    {
        var ctx = FlightMusicContext.Default();
        ctx.damageLevel = 0.65f;
        Assert.AreEqual(MusicMood.Danger, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_HighGForce_ReturnsTense()
    {
        var ctx = FlightMusicContext.Default();
        ctx.gForce = 3.5f;
        Assert.AreEqual(MusicMood.Tense, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_StallWarning_ReturnsTense()
    {
        var ctx = FlightMusicContext.Default();
        ctx.stallWarning = true;
        Assert.AreEqual(MusicMood.Tense, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_SevereWeather_ReturnsTense()
    {
        var ctx = FlightMusicContext.Default();
        ctx.weatherIntensity = 0.8f;
        Assert.AreEqual(MusicMood.Tense, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_SpaceAltitude_ReturnsEpic()
    {
        var ctx = FlightMusicContext.Default();
        ctx.altitude  = 110_000f;
        ctx.isInSpace = true;
        Assert.AreEqual(MusicMood.Epic, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_IsInSpaceFlag_ReturnsEpic()
    {
        var ctx = FlightMusicContext.Default();
        ctx.isInSpace = true;
        Assert.AreEqual(MusicMood.Epic, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_MissionJustCompleted_ReturnsTriumphant()
    {
        var ctx = FlightMusicContext.Default();
        ctx.missionJustCompleted = true;
        Assert.AreEqual(MusicMood.Triumphant, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_GoldenHour_ReturnsSerene()
    {
        var ctx = FlightMusicContext.Default();
        ctx.sunAltitudeDeg = 3f;   // within 0–6° golden hour band
        Assert.AreEqual(MusicMood.Serene, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_NightClear_ReturnsMysterious()
    {
        var ctx = FlightMusicContext.Default();
        ctx.timeOfDay        = 22f;
        ctx.weatherIntensity = 0.1f;
        ctx.sunAltitudeDeg   = -20f; // sun below horizon
        Assert.AreEqual(MusicMood.Mysterious, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_EarlyMorningClear_ReturnsMysterious()
    {
        var ctx = FlightMusicContext.Default();
        ctx.timeOfDay        = 2f;
        ctx.weatherIntensity = 0.05f;
        ctx.sunAltitudeDeg   = -30f;
        Assert.AreEqual(MusicMood.Mysterious, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_HighSpeed_ReturnsAdventurous()
    {
        var ctx = FlightMusicContext.Default();
        ctx.speed          = 300f;   // > 272 m/s (~Mach 0.8)
        ctx.timeOfDay      = 12f;    // midday — not night
        ctx.sunAltitudeDeg = 45f;    // high sun — not golden hour
        Assert.AreEqual(MusicMood.Adventurous, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_StableCruise_ReturnsCruising()
    {
        var ctx = FlightMusicContext.Default();
        ctx.altitude         = 8_000f;
        ctx.speed            = 200f;
        ctx.gForce           = 1f;
        ctx.weatherIntensity = 0f;
        ctx.timeOfDay        = 14f;
        ctx.sunAltitudeDeg   = 40f;
        Assert.AreEqual(MusicMood.Cruising, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_LowAltitudeCalmSlow_ReturnsPeaceful()
    {
        var ctx = FlightMusicContext.Default();
        ctx.altitude         = 100f;
        ctx.speed            = 20f;
        ctx.gForce           = 1f;
        ctx.weatherIntensity = 0f;
        ctx.timeOfDay        = 14f;
        ctx.sunAltitudeDeg   = 40f;
        Assert.AreEqual(MusicMood.Peaceful, MoodResolver.ResolveMood(ctx));
    }

    // ── Priority ordering ────────────────────────────────────────────────────

    [Test]
    public void ResolveMood_DangerBeatsAllOtherConditions()
    {
        var ctx = FlightMusicContext.Default();
        ctx.isInCombatZone       = true;
        ctx.missionJustCompleted = true;   // would be Triumphant
        ctx.isInSpace            = true;   // would be Epic
        Assert.AreEqual(MusicMood.Danger, MoodResolver.ResolveMood(ctx));
    }

    [Test]
    public void ResolveMood_TenseBeatsMissionComplete()
    {
        var ctx = FlightMusicContext.Default();
        ctx.gForce               = 4f;
        ctx.missionJustCompleted = true;
        Assert.AreEqual(MusicMood.Tense, MoodResolver.ResolveMood(ctx));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MoodResolver — ResolveIntensity range validation across all moods
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ResolveIntensity_AllMoods_ReturnValueInRange()
    {
        var ctx = FlightMusicContext.Default();
        foreach (MusicMood mood in Enum.GetValues(typeof(MusicMood)))
        {
            float intensity = MoodResolver.ResolveIntensity(ctx, mood);
            Assert.GreaterOrEqual(intensity, 0f, $"Mood {mood} intensity below 0");
            Assert.LessOrEqual(intensity, 1f,    $"Mood {mood} intensity above 1");
        }
    }

    [Test]
    public void ResolveIntensity_MaxDanger_IsHigh()
    {
        var ctx = FlightMusicContext.Default();
        ctx.dangerLevel    = 1f;
        ctx.damageLevel    = 1f;
        ctx.isInCombatZone = true;
        float intensity = MoodResolver.ResolveIntensity(ctx, MusicMood.Danger);
        Assert.Greater(intensity, 0.5f);
    }

    [Test]
    public void ResolveIntensity_Triumphant_AlwaysOne()
    {
        var ctx = FlightMusicContext.Default();
        float intensity = MoodResolver.ResolveIntensity(ctx, MusicMood.Triumphant);
        Assert.AreEqual(1f, intensity, 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IntensityController.GetActiveLayersForIntensity — boundary values
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GetActiveLayers_AtZero_PadsOnly()
    {
        var layers = IntensityController.GetActiveLayersForIntensity(0f);
        Assert.AreEqual(1, layers.Count);
        Assert.Contains(MusicLayer.Pads, layers);
    }

    [Test]
    public void GetActiveLayers_AtT1_PadsAndStrings()
    {
        var layers = IntensityController.GetActiveLayersForIntensity(0.2f);
        Assert.Contains(MusicLayer.Pads,    layers);
        Assert.Contains(MusicLayer.Strings, layers);
        Assert.IsFalse(layers.Contains(MusicLayer.Melody));
        Assert.IsFalse(layers.Contains(MusicLayer.Bass));
    }

    [Test]
    public void GetActiveLayers_AtT2_IncludesMelodyAndBass()
    {
        var layers = IntensityController.GetActiveLayersForIntensity(0.4f);
        Assert.Contains(MusicLayer.Melody, layers);
        Assert.Contains(MusicLayer.Bass,   layers);
        Assert.IsFalse(layers.Contains(MusicLayer.Drums));
    }

    [Test]
    public void GetActiveLayers_AtT3_IncludesDrumsAndPercussion()
    {
        var layers = IntensityController.GetActiveLayersForIntensity(0.6f);
        Assert.Contains(MusicLayer.Drums,      layers);
        Assert.Contains(MusicLayer.Percussion, layers);
        Assert.IsFalse(layers.Contains(MusicLayer.Choir));
    }

    [Test]
    public void GetActiveLayers_AtT4_AllEightLayers()
    {
        var layers = IntensityController.GetActiveLayersForIntensity(0.8f);
        Assert.Contains(MusicLayer.Choir, layers);
        Assert.Contains(MusicLayer.Synth, layers);
        Assert.AreEqual(8, layers.Count);
    }

    [Test]
    public void GetActiveLayers_AtOne_AllEightLayers()
    {
        var layers = IntensityController.GetActiveLayersForIntensity(1f);
        Assert.AreEqual(8, layers.Count);
        foreach (MusicLayer layer in Enum.GetValues(typeof(MusicLayer)))
            Assert.Contains(layer, layers);
    }

    [Test]
    public void GetActiveLayers_BelowT1_NoStrings()
    {
        var layers = IntensityController.GetActiveLayersForIntensity(0.19f);
        Assert.IsFalse(layers.Contains(MusicLayer.Strings));
    }

    [Test]
    public void GetActiveLayers_AboveT1BelowT2_NoMelodyOrBass()
    {
        var layers = IntensityController.GetActiveLayersForIntensity(0.35f);
        Assert.IsFalse(layers.Contains(MusicLayer.Melody));
        Assert.IsFalse(layers.Contains(MusicLayer.Bass));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BeatSyncClock — initial state and BPM clamping
    // ═══════════════════════════════════════════════════════════════════════════

    private GameObject _clockObj;
    private BeatSyncClock _clock;

    [SetUp]
    public void SetUpClock()
    {
        _clockObj = new GameObject("BeatSyncClock");
        _clock    = _clockObj.AddComponent<BeatSyncClock>();
    }

    [TearDown]
    public void TearDownClock()
    {
        if (_clockObj != null)
            UnityEngine.Object.DestroyImmediate(_clockObj);
    }

    [Test]
    public void BeatSyncClock_InitialBeat_IsZero()
    {
        Assert.AreEqual(0, _clock.GetCurrentBeat());
    }

    [Test]
    public void BeatSyncClock_InitialBeatInBar_IsZero()
    {
        Assert.AreEqual(0, _clock.GetBeatInBar());
    }

    [Test]
    public void BeatSyncClock_SetBPM_ClampsTooLow()
    {
        _clock.SetBPMImmediate(5f);
        Assert.AreEqual(BeatSyncClock.MinBpm, _clock.GetCurrentBPM(), 0.001f);
    }

    [Test]
    public void BeatSyncClock_SetBPM_ClampsTooHigh()
    {
        _clock.SetBPMImmediate(500f);
        Assert.AreEqual(BeatSyncClock.MaxBpm, _clock.GetCurrentBPM(), 0.001f);
    }

    [Test]
    public void BeatSyncClock_SetBPMImmediate_UpdatesCurrentBPM()
    {
        _clock.SetBPMImmediate(120f);
        Assert.AreEqual(120f, _clock.GetCurrentBPM(), 0.001f);
    }

    [Test]
    public void BeatSyncClock_BarProgress_IsInRange()
    {
        float progress = _clock.GetBarProgress01();
        Assert.GreaterOrEqual(progress, 0f);
        Assert.LessOrEqual(progress, 1f);
    }

    [Test]
    public void BeatSyncClock_GetNextBarTime_IsNonNegative()
    {
        Assert.GreaterOrEqual(_clock.GetNextBarTime(), 0.0);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightMusicContext.Default()
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FlightMusicContext_Default_AltitudeIsZero()
    {
        Assert.AreEqual(0f, FlightMusicContext.Default().altitude, 0.001f);
    }

    [Test]
    public void FlightMusicContext_Default_SpeedIsZero()
    {
        Assert.AreEqual(0f, FlightMusicContext.Default().speed, 0.001f);
    }

    [Test]
    public void FlightMusicContext_Default_GForceIsOne()
    {
        Assert.AreEqual(1f, FlightMusicContext.Default().gForce, 0.001f);
    }

    [Test]
    public void FlightMusicContext_Default_DangerLevelIsZero()
    {
        Assert.AreEqual(0f, FlightMusicContext.Default().dangerLevel, 0.001f);
    }

    [Test]
    public void FlightMusicContext_Default_WeatherIntensityIsZero()
    {
        Assert.AreEqual(0f, FlightMusicContext.Default().weatherIntensity, 0.001f);
    }

    [Test]
    public void FlightMusicContext_Default_TimeOfDayIsNoon()
    {
        Assert.AreEqual(12f, FlightMusicContext.Default().timeOfDay, 0.001f);
    }

    [Test]
    public void FlightMusicContext_Default_BoolFlagsAreFalse()
    {
        var ctx = FlightMusicContext.Default();
        Assert.IsFalse(ctx.isInMission);
        Assert.IsFalse(ctx.isInCombatZone);
        Assert.IsFalse(ctx.isLanding);
        Assert.IsFalse(ctx.isInSpace);
        Assert.IsFalse(ctx.missionJustCompleted);
        Assert.IsFalse(ctx.stallWarning);
    }

    [Test]
    public void FlightMusicContext_Default_DamageLevelIsZero()
    {
        Assert.AreEqual(0f, FlightMusicContext.Default().damageLevel, 0.001f);
    }

    [Test]
    public void FlightMusicContext_Default_SunAltitudeIsPositive()
    {
        Assert.Greater(FlightMusicContext.Default().sunAltitudeDeg, 0f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MusicTransitionController — same-mood no-op
    // ═══════════════════════════════════════════════════════════════════════════

    private GameObject _transCtrlObj;
    private MusicTransitionController _transCtrl;

    [SetUp]
    public void SetUpTransitionController()
    {
        _transCtrlObj = new GameObject("MusicTransitionController");
        _transCtrl    = _transCtrlObj.AddComponent<MusicTransitionController>();
    }

    [TearDown]
    public void TearDownTransitionController()
    {
        if (_transCtrlObj != null)
            UnityEngine.Object.DestroyImmediate(_transCtrlObj);
    }

    [Test]
    public void MusicTransitionController_SameMood_DoesNotFireEvent()
    {
        bool eventFired = false;
        _transCtrl.OnTransitionStarted += (_, __) => eventFired = true;

        MusicMood initial = _transCtrl.CurrentMood;
        _transCtrl.RequestTransition(initial); // same mood

        Assert.IsFalse(eventFired, "OnTransitionStarted should not fire when requesting same mood");
    }

    [Test]
    public void MusicTransitionController_ForceTransition_ChangesCurrentMood()
    {
        MusicMood initial = _transCtrl.CurrentMood;
        MusicMood target  = initial == MusicMood.Peaceful ? MusicMood.Epic : MusicMood.Peaceful;

        _transCtrl.ForceTransition(target);

        Assert.AreEqual(target, _transCtrl.CurrentMood);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Enum completeness
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void MusicMood_HasNineValues()
    {
        Assert.AreEqual(9, Enum.GetValues(typeof(MusicMood)).Length);
    }

    [Test]
    public void MusicLayer_HasEightValues()
    {
        Assert.AreEqual(8, Enum.GetValues(typeof(MusicLayer)).Length);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AdaptiveMusicProfile — GetCrossfadeDuration fallback
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AdaptiveMusicProfile_GetCrossfadeDuration_FallsBackToDefault()
    {
        var profile = ScriptableObject.CreateInstance<AdaptiveMusicProfile>();
        profile.defaultCrossfadeDuration = 5f;

        float duration = profile.GetCrossfadeDuration(MusicMood.Peaceful, MusicMood.Epic);
        Assert.AreEqual(5f, duration, 0.001f);

        ScriptableObject.DestroyImmediate(profile);
    }

    [Test]
    public void AdaptiveMusicProfile_GetCrossfadeDuration_UsesSpecificRule()
    {
        var profile = ScriptableObject.CreateInstance<AdaptiveMusicProfile>();
        profile.defaultCrossfadeDuration = 5f;
        profile.transitionRules = new System.Collections.Generic.List<MoodTransitionRule>
        {
            new MoodTransitionRule { from = MusicMood.Peaceful, to = MusicMood.Danger, crossfadeDuration = 1.5f }
        };

        float duration = profile.GetCrossfadeDuration(MusicMood.Peaceful, MusicMood.Danger);
        Assert.AreEqual(1.5f, duration, 0.001f);

        ScriptableObject.DestroyImmediate(profile);
    }

    [Test]
    public void AdaptiveMusicProfile_GetStingerPath_ReturnsEmpty_WhenNoRule()
    {
        var profile = ScriptableObject.CreateInstance<AdaptiveMusicProfile>();
        string path = profile.GetStingerPath(MusicMood.Serene, MusicMood.Danger);
        Assert.AreEqual(string.Empty, path);
        ScriptableObject.DestroyImmediate(profile);
    }
}
