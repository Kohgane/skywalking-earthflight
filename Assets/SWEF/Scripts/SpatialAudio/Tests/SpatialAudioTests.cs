// SpatialAudioTests.cs — Phase 118: Spatial Audio & 3D Soundscape
// Comprehensive NUnit EditMode tests (64 tests).
// Tests cover: enums, config, engine audio layers, wind noise, propagation,
// occlusion, Doppler, reverb zones, data models, analytics.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.SpatialAudio;

[TestFixture]
public class SpatialAudioTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // AudioZoneType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AudioZoneType_AllValuesAreDefined()
    {
        var values = (AudioZoneType[])Enum.GetValues(typeof(AudioZoneType));
        Assert.GreaterOrEqual(values.Length, 10, "At least 10 AudioZoneType values required");
        Assert.Contains(AudioZoneType.Cockpit,  values);
        Assert.Contains(AudioZoneType.Exterior, values);
        Assert.Contains(AudioZoneType.Cabin,    values);
        Assert.Contains(AudioZoneType.Hangar,   values);
        Assert.Contains(AudioZoneType.Airport,  values);
        Assert.Contains(AudioZoneType.City,     values);
        Assert.Contains(AudioZoneType.Ocean,    values);
        Assert.Contains(AudioZoneType.Mountain, values);
        Assert.Contains(AudioZoneType.Forest,   values);
        Assert.Contains(AudioZoneType.Space,    values);
    }

    [Test]
    public void AudioZoneType_HasDistinctIntValues()
    {
        var values = (AudioZoneType[])Enum.GetValues(typeof(AudioZoneType));
        var intSet = new HashSet<int>();
        foreach (var v in values)
            Assert.IsTrue(intSet.Add((int)v), $"Duplicate int value for {v}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SoundPropagationModel enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SoundPropagationModel_AllValuesAreDefined()
    {
        var values = (SoundPropagationModel[])Enum.GetValues(typeof(SoundPropagationModel));
        Assert.GreaterOrEqual(values.Length, 3);
        Assert.Contains(SoundPropagationModel.Linear,      values);
        Assert.Contains(SoundPropagationModel.Logarithmic, values);
        Assert.Contains(SoundPropagationModel.Realistic,   values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AudioOcclusionType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AudioOcclusionType_AllValuesAreDefined()
    {
        var values = (AudioOcclusionType[])Enum.GetValues(typeof(AudioOcclusionType));
        Assert.GreaterOrEqual(values.Length, 4);
        Assert.Contains(AudioOcclusionType.None,       values);
        Assert.Contains(AudioOcclusionType.LowPass,    values);
        Assert.Contains(AudioOcclusionType.Raycast,    values);
        Assert.Contains(AudioOcclusionType.Volumetric, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EngineSoundLayer enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void EngineSoundLayer_AllValuesAreDefined()
    {
        var values = (EngineSoundLayer[])Enum.GetValues(typeof(EngineSoundLayer));
        Assert.GreaterOrEqual(values.Length, 9);
        Assert.Contains(EngineSoundLayer.Idle,         values);
        Assert.Contains(EngineSoundLayer.Cruise,       values);
        Assert.Contains(EngineSoundLayer.FullThrottle, values);
        Assert.Contains(EngineSoundLayer.Afterburner,  values);
        Assert.Contains(EngineSoundLayer.Intake,       values);
        Assert.Contains(EngineSoundLayer.Exhaust,      values);
        Assert.Contains(EngineSoundLayer.TurbineWhine, values);
        Assert.Contains(EngineSoundLayer.Propeller,    values);
        Assert.Contains(EngineSoundLayer.JetWash,      values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ReverbZonePreset enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ReverbZonePreset_AllValuesAreDefined()
    {
        var values = (ReverbZonePreset[])Enum.GetValues(typeof(ReverbZonePreset));
        Assert.GreaterOrEqual(values.Length, 9);
        Assert.Contains(ReverbZonePreset.OpenSky,  values);
        Assert.Contains(ReverbZonePreset.Cockpit,  values);
        Assert.Contains(ReverbZonePreset.Hangar,   values);
        Assert.Contains(ReverbZonePreset.Canyon,   values);
        Assert.Contains(ReverbZonePreset.Airport,  values);
        Assert.Contains(ReverbZonePreset.City,     values);
        Assert.Contains(ReverbZonePreset.Forest,   values);
        Assert.Contains(ReverbZonePreset.Mountain, values);
        Assert.Contains(ReverbZonePreset.Space,    values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WildlifeZone enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void WildlifeZone_AllValuesAreDefined()
    {
        var values = (WildlifeZone[])Enum.GetValues(typeof(WildlifeZone));
        Assert.GreaterOrEqual(values.Length, 5);
        Assert.Contains(WildlifeZone.Altitude, values);
        Assert.Contains(WildlifeZone.Coastal,  values);
        Assert.Contains(WildlifeZone.Forest,   values);
        Assert.Contains(WildlifeZone.Night,    values);
        Assert.Contains(WildlifeZone.Tropical, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EngineAudioProfile data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void EngineAudioProfile_DefaultConstruction_HasValidDefaults()
    {
        var profile = new EngineAudioProfile();
        Assert.IsNotNull(profile);
        Assert.AreEqual(0f, profile.idleRpm);
        Assert.AreEqual(0f, profile.maxRpm);
    }

    [Test]
    public void EngineAudioProfile_SetValues_AreRetained()
    {
        var profile = new EngineAudioProfile
        {
            profileId         = "CF6",
            engineName        = "GE CF6-80",
            idleRpm           = 2000f,
            maxRpm            = 12000f,
            idlePitch         = 0.7f,
            maxPitch          = 1.5f,
            idleVolume        = 0.3f,
            maxVolume         = 1f,
            hasAfterburner    = false,
            afterburnerVolumeBoost = 0f
        };

        Assert.AreEqual("CF6",      profile.profileId);
        Assert.AreEqual(2000f,      profile.idleRpm);
        Assert.AreEqual(12000f,     profile.maxRpm);
        Assert.AreEqual(1.5f,       profile.maxPitch, 0.001f);
        Assert.IsFalse(profile.hasAfterburner);
    }

    [Test]
    public void EngineAudioProfile_Afterburner_FlagsWork()
    {
        var profile = new EngineAudioProfile
        {
            hasAfterburner         = true,
            afterburnerVolumeBoost = 0.5f
        };
        Assert.IsTrue(profile.hasAfterburner);
        Assert.AreEqual(0.5f, profile.afterburnerVolumeBoost, 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EnvironmentAudioProfile data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void EnvironmentAudioProfile_DefaultConstruction_IsValid()
    {
        var profile = new EnvironmentAudioProfile();
        Assert.IsNotNull(profile);
    }

    [Test]
    public void EnvironmentAudioProfile_ZoneAndReverb_AreAssignable()
    {
        var profile = new EnvironmentAudioProfile
        {
            zoneType            = AudioZoneType.Hangar,
            reverbPreset        = ReverbZonePreset.Hangar,
            ambientVolume       = 0.6f,
            reverbWetMix        = 0.4f,
            muffledCutoffHz     = 1000f,
            muffleExteriorSounds = true,
            transitionDuration  = 1.5f
        };

        Assert.AreEqual(AudioZoneType.Hangar,    profile.zoneType);
        Assert.AreEqual(ReverbZonePreset.Hangar, profile.reverbPreset);
        Assert.IsTrue(profile.muffleExteriorSounds);
        Assert.AreEqual(1.5f, profile.transitionDuration, 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WindNoiseProfile data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void WindNoiseProfile_DefaultConstruction_IsValid()
    {
        var profile = new WindNoiseProfile();
        Assert.IsNotNull(profile);
    }

    [Test]
    public void WindNoiseProfile_SpeedThresholds_AreAscending()
    {
        var profile = new WindNoiseProfile
        {
            laminarOnsetSpeed   = 10f,
            turbulentOnsetSpeed = 80f,
            machOnsetSpeed      = 300f
        };
        Assert.Less(profile.laminarOnsetSpeed,   profile.turbulentOnsetSpeed);
        Assert.Less(profile.turbulentOnsetSpeed, profile.machOnsetSpeed);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DopplerResult data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DopplerResult_Fields_AreAssignable()
    {
        var result = new DopplerResult
        {
            originalFrequency = 440f,
            shiftedFrequency  = 480f,
            relativeVelocity  = 50f,
            speedOfSound      = 343f
        };
        Assert.AreEqual(440f, result.originalFrequency, 0.001f);
        Assert.AreEqual(480f, result.shiftedFrequency,  0.001f);
        Assert.AreEqual(50f,  result.relativeVelocity,  0.001f);
        Assert.AreEqual(343f, result.speedOfSound,      0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AudioZoneState data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AudioZoneState_DefaultConstruction_IsValid()
    {
        var state = new AudioZoneState();
        Assert.IsNotNull(state);
        Assert.AreEqual(0f, state.blendWeight);
        Assert.IsFalse(state.isTransitioning);
    }

    [Test]
    public void AudioZoneState_Fields_AreAssignable()
    {
        var state = new AudioZoneState
        {
            currentZone            = AudioZoneType.Cockpit,
            targetZone             = AudioZoneType.Exterior,
            blendWeight            = 0.5f,
            isTransitioning        = true,
            altitudeMetres         = 1000f,
            speedMetresPerSecond   = 250f
        };
        Assert.AreEqual(AudioZoneType.Cockpit,  state.currentZone);
        Assert.AreEqual(AudioZoneType.Exterior, state.targetZone);
        Assert.AreEqual(0.5f, state.blendWeight, 0.001f);
        Assert.IsTrue(state.isTransitioning);
        Assert.AreEqual(1000f, state.altitudeMetres, 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SpatialAudioAnalyticsRecord data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SpatialAudioAnalyticsRecord_DefaultConstruction_IsValid()
    {
        var record = new SpatialAudioAnalyticsRecord();
        Assert.IsNotNull(record);
    }

    [Test]
    public void SpatialAudioAnalyticsRecord_Fields_AreAssignable()
    {
        var now = DateTime.UtcNow;
        var record = new SpatialAudioAnalyticsRecord
        {
            timestamp         = now,
            hrtfEnabled       = true,
            qualityPreset     = "Ultra",
            peakActiveSources = 48,
            avgAudioCpuMs     = 1.2f
        };
        Assert.AreEqual(now,     record.timestamp);
        Assert.IsTrue(record.hrtfEnabled);
        Assert.AreEqual("Ultra", record.qualityPreset);
        Assert.AreEqual(48,      record.peakActiveSources);
        Assert.AreEqual(1.2f,    record.avgAudioCpuMs, 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EngineAudioLayerMixer — static LayerWeight method
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LayerMixer_IdleWeight_IsOneAtZeroThrottle()
    {
        float weight = EngineAudioLayerMixer.LayerWeight(EngineSoundLayer.Idle, 0f, 0f);
        Assert.AreEqual(1f, weight, 0.001f);
    }

    [Test]
    public void LayerMixer_IdleWeight_IsZeroAtFullThrottle()
    {
        float weight = EngineAudioLayerMixer.LayerWeight(EngineSoundLayer.Idle, 1f, 1f);
        Assert.AreEqual(0f, weight, 0.001f);
    }

    [Test]
    public void LayerMixer_TurbineWhine_ScalesWithRpm()
    {
        float low  = EngineAudioLayerMixer.LayerWeight(EngineSoundLayer.TurbineWhine, 0.5f, 0f);
        float high = EngineAudioLayerMixer.LayerWeight(EngineSoundLayer.TurbineWhine, 0.5f, 1f);
        Assert.Less(low, high);
    }

    [Test]
    public void LayerMixer_JetWash_IsZeroAtZeroThrottle()
    {
        float weight = EngineAudioLayerMixer.LayerWeight(EngineSoundLayer.JetWash, 0f, 0f);
        Assert.AreEqual(0f, weight, 0.001f);
    }

    [Test]
    public void LayerMixer_Afterburner_ActiveOnlyAtMaxThrottle()
    {
        float low  = EngineAudioLayerMixer.LayerWeight(EngineSoundLayer.Afterburner, 0.5f, 1f);
        float high = EngineAudioLayerMixer.LayerWeight(EngineSoundLayer.Afterburner, 1f,   1f);
        Assert.AreEqual(0f, low,  0.001f);
        Assert.AreEqual(1f, high, 0.001f);
    }

    [Test]
    public void LayerMixer_FullThrottle_ZeroAtLowThrottle()
    {
        float weight = EngineAudioLayerMixer.LayerWeight(EngineSoundLayer.FullThrottle, 0.1f, 0.5f);
        Assert.AreEqual(0f, weight, 0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WindNoiseController — volume calculations
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void WindNoise_LaminarVolume_IsZeroAtLowSpeed()
    {
        var go = new GameObject("WindNoise");
        var controller = go.AddComponent<WindNoiseController>();
        controller.UpdateWindNoise(0f);
        float vol = controller.CalculateLaminarVolume();
        Assert.AreEqual(0f, vol, 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WindNoise_TurbulentVolume_IsZeroAtLowSpeed()
    {
        var go = new GameObject("WindNoise");
        var controller = go.AddComponent<WindNoiseController>();
        controller.UpdateWindNoise(5f);
        float vol = controller.CalculateTurbulentVolume();
        Assert.AreEqual(0f, vol, 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WindNoise_MachVolume_IsZeroBelowMachOnset()
    {
        var go = new GameObject("WindNoise");
        var controller = go.AddComponent<WindNoiseController>();
        controller.UpdateWindNoise(100f);
        float vol = controller.CalculateMachVolume();
        Assert.AreEqual(0f, vol, 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WindNoise_CurrentSpeed_IsRetained()
    {
        var go = new GameObject("WindNoise");
        var controller = go.AddComponent<WindNoiseController>();
        controller.UpdateWindNoise(250f);
        Assert.AreEqual(250f, controller.CurrentSpeedMs, 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AudioPropagationEngine — attenuation and speed of sound
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Propagation_SpeedOfSound_AtStandardAtmosphere_IsApproximately343()
    {
        var go     = new GameObject("Propagation");
        var engine = go.AddComponent<AudioPropagationEngine>();
        engine.temperatureCelsius = 15f;
        engine.relativeHumidity  = 0.5f;
        float sos = engine.CalculateSpeedOfSound();
        Assert.AreEqual(343f, sos, 5f, "Speed of sound should be near 343 m/s at 15°C");
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Propagation_SpeedOfSound_IncreasesWithTemperature()
    {
        var go     = new GameObject("Propagation");
        var engine = go.AddComponent<AudioPropagationEngine>();
        engine.relativeHumidity  = 0f;

        engine.temperatureCelsius = 0f;
        float cold = engine.CalculateSpeedOfSound();

        engine.temperatureCelsius = 30f;
        float warm = engine.CalculateSpeedOfSound();

        Assert.Greater(warm, cold);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Propagation_LinearAttenuation_FullVolumeAtMinDistance()
    {
        var go     = new GameObject("Propagation");
        var engine = go.AddComponent<AudioPropagationEngine>();
        float atten = engine.CalculateAttenuation(5f, SoundPropagationModel.Linear);
        Assert.AreEqual(1f, atten, 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Propagation_LinearAttenuation_ZeroAtMaxDistance()
    {
        var go     = new GameObject("Propagation");
        var engine = go.AddComponent<AudioPropagationEngine>();
        float atten = engine.CalculateAttenuation(5000f, SoundPropagationModel.Linear);
        Assert.AreEqual(0f, atten, 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Propagation_LogarithmicAttenuation_DecreasesWith_Distance()
    {
        var go     = new GameObject("Propagation");
        var engine = go.AddComponent<AudioPropagationEngine>();
        float near = engine.CalculateAttenuation(10f,   SoundPropagationModel.Logarithmic);
        float far  = engine.CalculateAttenuation(1000f, SoundPropagationModel.Logarithmic);
        Assert.Greater(near, far);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Propagation_PropagationDelay_IsPositive()
    {
        var go     = new GameObject("Propagation");
        var engine = go.AddComponent<AudioPropagationEngine>();
        float delay = engine.CalculatePropagationDelay(343f);
        Assert.Greater(delay, 0f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Propagation_AtmosphericAbsorption_LessThanOne_ForLongDistance()
    {
        var go     = new GameObject("Propagation");
        var engine = go.AddComponent<AudioPropagationEngine>();
        engine.relativeHumidity = 0f; // dry air = more absorption
        float absorption = engine.CalculateAtmosphericAbsorption(5000f);
        Assert.Less(absorption, 1f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AudioOcclusionSystem — factor and frequency calculations
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Occlusion_NoneMode_IsNotOccluded()
    {
        var go     = new GameObject("Occlusion");
        var system = go.AddComponent<AudioOcclusionSystem>();
        // No config = default Raycast, but without geometry, not occluded
        bool occ = system.IsOccluded(Vector3.zero, new Vector3(0f, 0f, 100f));
        Assert.IsFalse(occ, "Should not be occluded when no geometry is present");
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Occlusion_CutoffHz_IsMaxWhenNotOccluded()
    {
        var go     = new GameObject("Occlusion");
        var system = go.AddComponent<AudioOcclusionSystem>();
        float hz = system.GetOcclusionCutoffHz(0f);
        Assert.AreEqual(22000f, hz, 1f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Occlusion_CutoffHz_IsLowWhenFullyOccluded()
    {
        var go     = new GameObject("Occlusion");
        var system = go.AddComponent<AudioOcclusionSystem>();
        float hz = system.GetOcclusionCutoffHz(1f);
        Assert.LessOrEqual(hz, 1000f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Occlusion_VolumeMultiplier_IsOneWhenNotOccluded()
    {
        var go     = new GameObject("Occlusion");
        var system = go.AddComponent<AudioOcclusionSystem>();
        float vol = system.GetOcclusionVolumeMultiplier(0f);
        Assert.AreEqual(1f, vol, 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Occlusion_VolumeMultiplier_ReducedWhenFullyOccluded()
    {
        var go     = new GameObject("Occlusion");
        var system = go.AddComponent<AudioOcclusionSystem>();
        float vol = system.GetOcclusionVolumeMultiplier(1f);
        Assert.Less(vol, 1f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DopplerEffectController — pitch calculations
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Doppler_StaticSourceAndListener_NoPitchShift()
    {
        var go         = new GameObject("Doppler");
        var controller = go.AddComponent<DopplerEffectController>();
        float pitch    = controller.CalculateDopplerPitch(Vector3.zero, Vector3.zero, Vector3.forward);
        Assert.AreEqual(1f, pitch, 0.05f, "No velocity should produce no Doppler shift");
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Doppler_ApproachingSource_PitchHigherThanOne()
    {
        var go         = new GameObject("Doppler");
        var controller = go.AddComponent<DopplerEffectController>();
        // Source moving toward listener (positive projection onto source→listener)
        Vector3 sourceVel   = new Vector3(0f, 0f, -50f);  // moving toward listener at +Z
        Vector3 listenerVel = Vector3.zero;
        Vector3 direction   = Vector3.forward;             // source→listener
        float   pitch       = controller.CalculateDopplerPitch(sourceVel, listenerVel, direction);
        // Approaching source should raise pitch
        Assert.Greater(pitch, 1f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Doppler_RecedingSource_PitchLowerThanOne()
    {
        var go         = new GameObject("Doppler");
        var controller = go.AddComponent<DopplerEffectController>();
        // Source moving away from listener
        Vector3 sourceVel   = new Vector3(0f, 0f, 50f);   // moving away
        Vector3 listenerVel = Vector3.zero;
        Vector3 direction   = Vector3.forward;
        float   pitch       = controller.CalculateDopplerPitch(sourceVel, listenerVel, direction);
        Assert.Less(pitch, 1f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Doppler_ComputeResult_FrequencyIsPositive()
    {
        var go         = new GameObject("Doppler");
        var controller = go.AddComponent<DopplerEffectController>();
        var result     = controller.ComputeDopplerResult(440f, Vector3.zero, Vector3.zero, Vector3.forward);
        Assert.Greater(result.shiftedFrequency, 0f);
        Assert.AreEqual(440f, result.originalFrequency, 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ReverbZoneManager — zone-to-preset mapping
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ReverbZone_CockpitZone_MapsTo_CockpitPreset()
    {
        Assert.AreEqual(ReverbZonePreset.Cockpit, ReverbZoneManager.ZoneToPreset(AudioZoneType.Cockpit));
    }

    [Test]
    public void ReverbZone_HangarZone_MapsTo_HangarPreset()
    {
        Assert.AreEqual(ReverbZonePreset.Hangar, ReverbZoneManager.ZoneToPreset(AudioZoneType.Hangar));
    }

    [Test]
    public void ReverbZone_SpaceZone_MapsTo_SpacePreset()
    {
        Assert.AreEqual(ReverbZonePreset.Space, ReverbZoneManager.ZoneToPreset(AudioZoneType.Space));
    }

    [Test]
    public void ReverbZone_ForestZone_MapsTo_ForestPreset()
    {
        Assert.AreEqual(ReverbZonePreset.Forest, ReverbZoneManager.ZoneToPreset(AudioZoneType.Forest));
    }

    [Test]
    public void ReverbZone_CityZone_MapsTo_CityPreset()
    {
        Assert.AreEqual(ReverbZonePreset.City, ReverbZoneManager.ZoneToPreset(AudioZoneType.City));
    }

    [Test]
    public void ReverbZone_ExteriorZone_MapsTo_OpenSkyPreset()
    {
        Assert.AreEqual(ReverbZonePreset.OpenSky, ReverbZoneManager.ZoneToPreset(AudioZoneType.Exterior));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SonicBoomController — Mach calculation
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SonicBoom_BelowMach_IsNotSupersonic()
    {
        var go         = new GameObject("SonicBoom");
        var controller = go.AddComponent<SonicBoomController>();
        controller.speedOfSound      = 343f;
        controller.boomMachThreshold = 1.0f;
        controller.UpdateSpeed(300f, 0f);
        Assert.IsFalse(controller.IsSupersonic);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void SonicBoom_AboveMach_IsSupersonic()
    {
        var go         = new GameObject("SonicBoom");
        var controller = go.AddComponent<SonicBoomController>();
        controller.speedOfSound      = 343f;
        controller.boomMachThreshold = 1.0f;
        controller.UpdateSpeed(400f, 0f);
        Assert.IsTrue(controller.IsSupersonic);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void SonicBoom_PropagationDelay_IsPositive()
    {
        var go         = new GameObject("SonicBoom");
        var controller = go.AddComponent<SonicBoomController>();
        controller.speedOfSound = 343f;
        float delay = controller.CalculatePropagationDelay(1000f);
        Assert.Greater(delay, 0f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void SonicBoom_MachNumber_CalculatedCorrectly()
    {
        var go         = new GameObject("SonicBoom");
        var controller = go.AddComponent<SonicBoomController>();
        controller.speedOfSound      = 343f;
        controller.boomMachThreshold = 1.0f;
        controller.UpdateSpeed(686f, 0f);   // exactly Mach 2
        Assert.AreEqual(2f, controller.CurrentMach, 0.05f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PropWashAudio — wash level
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PropWash_ZeroRpm_ZeroWashLevel()
    {
        var go         = new GameObject("PropWash");
        var controller = go.AddComponent<PropWashAudio>();
        controller.UpdatePropWash(0f, 100f);
        Assert.AreEqual(0f, controller.GetWashLevel(), 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void PropWash_MaxRpm_MaxWashLevel()
    {
        var go         = new GameObject("PropWash");
        var controller = go.AddComponent<PropWashAudio>();
        controller.maxRpm = 2700f;
        controller.UpdatePropWash(2700f, 100f);
        Assert.AreEqual(1f, controller.GetWashLevel(), 0.001f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CockpitWarningAudio — warning type enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CockpitWarning_AllWarningTypes_AreDefined()
    {
        var values = (CockpitWarningAudio.WarningType[])
            Enum.GetValues(typeof(CockpitWarningAudio.WarningType));
        Assert.GreaterOrEqual(values.Length, 6);
        Assert.Contains(CockpitWarningAudio.WarningType.StallHorn,           values);
        Assert.Contains(CockpitWarningAudio.WarningType.GearWarning,         values);
        Assert.Contains(CockpitWarningAudio.WarningType.GPWS,                values);
        Assert.Contains(CockpitWarningAudio.WarningType.Overspeed,           values);
        Assert.Contains(CockpitWarningAudio.WarningType.AltitudeAlert,       values);
        Assert.Contains(CockpitWarningAudio.WarningType.AutopilotDisconnect, values);
    }

    [Test]
    public void CockpitWarning_NoActiveWarning_Initially()
    {
        var go         = new GameObject("Warning");
        var controller = go.AddComponent<CockpitWarningAudio>();
        Assert.IsNull(controller.ActiveLoopWarning);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AudioDynamicRange — priority enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AudioPriority_WarningHigherThan_Radio()
    {
        Assert.Greater((int)AudioDynamicRange.AudioPriority.Warning,
                       (int)AudioDynamicRange.AudioPriority.Radio);
    }

    [Test]
    public void AudioPriority_RadioHigherThan_Flight()
    {
        Assert.Greater((int)AudioDynamicRange.AudioPriority.Radio,
                       (int)AudioDynamicRange.AudioPriority.Flight);
    }

    [Test]
    public void AudioPriority_FlightHigherThan_Ambient()
    {
        Assert.Greater((int)AudioDynamicRange.AudioPriority.Flight,
                       (int)AudioDynamicRange.AudioPriority.Ambient);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EngineStartupSequence — state machine
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void EngineStartup_InitialState_IsOff()
    {
        var go  = new GameObject("Startup");
        var seq = go.AddComponent<EngineStartupSequence>();
        Assert.AreEqual(EngineStartupSequence.SequenceState.Off, seq.CurrentState);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void EngineStartup_AllStates_AreDefined()
    {
        var values = (EngineStartupSequence.SequenceState[])
            Enum.GetValues(typeof(EngineStartupSequence.SequenceState));
        Assert.GreaterOrEqual(values.Length, 7);
        Assert.Contains(EngineStartupSequence.SequenceState.Off,           values);
        Assert.Contains(EngineStartupSequence.SequenceState.StarterMotor,  values);
        Assert.Contains(EngineStartupSequence.SequenceState.Ignition,      values);
        Assert.Contains(EngineStartupSequence.SequenceState.SpoolUp,       values);
        Assert.Contains(EngineStartupSequence.SequenceState.Running,       values);
        Assert.Contains(EngineStartupSequence.SequenceState.SpoolDown,     values);
        Assert.Contains(EngineStartupSequence.SequenceState.ShutdownWhine, values);
    }
}
