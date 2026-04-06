// SpatialAudioData.cs — Phase 118: Spatial Audio & 3D Soundscape
// Enums and data models for the spatial audio simulation.
// Namespace: SWEF.SpatialAudio

using System;
using UnityEngine;

namespace SWEF.SpatialAudio
{
    // ── Audio Zone Type ───────────────────────────────────────────────────────────

    /// <summary>Classification of the acoustic environment zone.</summary>
    public enum AudioZoneType
    {
        /// <summary>Enclosed cockpit interior of the aircraft.</summary>
        Cockpit,
        /// <summary>Open exterior environment outside the aircraft.</summary>
        Exterior,
        /// <summary>Passenger cabin inside a commercial aircraft.</summary>
        Cabin,
        /// <summary>Large enclosed hangar building.</summary>
        Hangar,
        /// <summary>Open airport apron and terminal area.</summary>
        Airport,
        /// <summary>Dense urban city environment.</summary>
        City,
        /// <summary>Open ocean or large body of water.</summary>
        Ocean,
        /// <summary>High-altitude mountain terrain.</summary>
        Mountain,
        /// <summary>Forested woodland environment.</summary>
        Forest,
        /// <summary>Near-vacuum outer space environment.</summary>
        Space
    }

    // ── Sound Propagation Model ───────────────────────────────────────────────────

    /// <summary>Mathematical model used for sound distance attenuation.</summary>
    public enum SoundPropagationModel
    {
        /// <summary>Simple linear falloff from min to max distance.</summary>
        Linear,
        /// <summary>Logarithmic rolloff following the inverse-square law.</summary>
        Logarithmic,
        /// <summary>Physics-based model accounting for atmosphere and temperature.</summary>
        Realistic
    }

    // ── Audio Occlusion Type ──────────────────────────────────────────────────────

    /// <summary>Method used to simulate sound occlusion by obstacles.</summary>
    public enum AudioOcclusionType
    {
        /// <summary>No occlusion processing applied.</summary>
        None,
        /// <summary>Simple low-pass filter applied when obstructed.</summary>
        LowPass,
        /// <summary>Raycast-based geometry occlusion check.</summary>
        Raycast,
        /// <summary>Full volumetric occlusion with multiple sample rays.</summary>
        Volumetric
    }

    // ── Engine Sound Layer ────────────────────────────────────────────────────────

    /// <summary>Individual audio layer component of an engine sound.</summary>
    public enum EngineSoundLayer
    {
        /// <summary>Low RPM engine idle sound.</summary>
        Idle,
        /// <summary>Cruise power engine sound.</summary>
        Cruise,
        /// <summary>Full throttle engine sound.</summary>
        FullThrottle,
        /// <summary>Afterburner ignition and sustain.</summary>
        Afterburner,
        /// <summary>Air intake whoosh sound.</summary>
        Intake,
        /// <summary>Exhaust blow-down noise.</summary>
        Exhaust,
        /// <summary>High-frequency turbine whine.</summary>
        TurbineWhine,
        /// <summary>Propeller arc rotation noise.</summary>
        Propeller,
        /// <summary>Jet exhaust wash behind aircraft.</summary>
        JetWash
    }

    // ── Reverb Zone Preset ────────────────────────────────────────────────────────

    /// <summary>Reverb preset applied inside a specific audio zone.</summary>
    public enum ReverbZonePreset
    {
        /// <summary>Minimal reverb under open sky.</summary>
        OpenSky,
        /// <summary>Tight enclosed cockpit resonance.</summary>
        Cockpit,
        /// <summary>Large metal hangar with long tail.</summary>
        Hangar,
        /// <summary>Narrow canyon or gorge echo.</summary>
        Canyon,
        /// <summary>Outdoor airport apron ambient reverb.</summary>
        Airport,
        /// <summary>Dense urban street reverb.</summary>
        City,
        /// <summary>Dense forest absorption and scattering.</summary>
        Forest,
        /// <summary>High mountain sparse air reverb.</summary>
        Mountain,
        /// <summary>Near-dead acoustic space.</summary>
        Space
    }

    // ── Wildlife Zone ─────────────────────────────────────────────────────────────

    /// <summary>Type of wildlife audio zone.</summary>
    public enum WildlifeZone
    {
        /// <summary>Open sky bird calls at altitude.</summary>
        Altitude,
        /// <summary>Coastal seagull cries near shoreline.</summary>
        Coastal,
        /// <summary>Forest bird song.</summary>
        Forest,
        /// <summary>Nocturnal cricket and insect chorus.</summary>
        Night,
        /// <summary>Tropical rainforest soundscape.</summary>
        Tropical
    }

    // ── Data Classes ──────────────────────────────────────────────────────────────

    /// <summary>Audio profile for a single aircraft engine type.</summary>
    [Serializable]
    public class EngineAudioProfile
    {
        /// <summary>Unique identifier for this engine profile.</summary>
        public string profileId;
        /// <summary>Human-readable engine name.</summary>
        public string engineName;
        /// <summary>Idle RPM value for pitch baseline.</summary>
        public float idleRpm;
        /// <summary>Maximum RPM at full throttle.</summary>
        public float maxRpm;
        /// <summary>Pitch multiplier at idle RPM.</summary>
        public float idlePitch;
        /// <summary>Pitch multiplier at max RPM.</summary>
        public float maxPitch;
        /// <summary>Volume at idle throttle (0–1).</summary>
        public float idleVolume;
        /// <summary>Volume at full throttle (0–1).</summary>
        public float maxVolume;
        /// <summary>Whether this engine supports afterburner.</summary>
        public bool hasAfterburner;
        /// <summary>Extra volume added when afterburner is active.</summary>
        public float afterburnerVolumeBoost;
    }

    /// <summary>Ambient sound profile for an environmental biome.</summary>
    [Serializable]
    public class EnvironmentAudioProfile
    {
        /// <summary>The zone type this profile applies to.</summary>
        public AudioZoneType zoneType;
        /// <summary>Ambient base volume (0–1).</summary>
        public float ambientVolume;
        /// <summary>Reverb preset for this environment.</summary>
        public ReverbZonePreset reverbPreset;
        /// <summary>Reverb wet mix amount (0–1).</summary>
        public float reverbWetMix;
        /// <summary>Low-pass cutoff frequency when muffled (Hz).</summary>
        public float muffledCutoffHz;
        /// <summary>Whether exterior sounds are muffled in this zone.</summary>
        public bool muffleExteriorSounds;
        /// <summary>Crossfade duration when entering/leaving this zone (seconds).</summary>
        public float transitionDuration;
    }

    /// <summary>Wind noise profile describing aerodynamic noise characteristics.</summary>
    [Serializable]
    public class WindNoiseProfile
    {
        /// <summary>Speed at which laminar flow wind noise begins (m/s).</summary>
        public float laminarOnsetSpeed;
        /// <summary>Speed at which turbulent buffeting starts (m/s).</summary>
        public float turbulentOnsetSpeed;
        /// <summary>Speed at which Mach-related effects begin (m/s, ~340 m/s).</summary>
        public float machOnsetSpeed;
        /// <summary>Maximum wind noise volume at high speed (0–1).</summary>
        public float maxWindVolume;
        /// <summary>Pitch shift applied at maximum speed.</summary>
        public float maxPitchShift;
    }

    /// <summary>Result data from a Doppler frequency shift calculation.</summary>
    [Serializable]
    public class DopplerResult
    {
        /// <summary>Original sound frequency in Hz.</summary>
        public float originalFrequency;
        /// <summary>Frequency after Doppler shift in Hz.</summary>
        public float shiftedFrequency;
        /// <summary>Relative velocity along the listener-source axis (m/s).</summary>
        public float relativeVelocity;
        /// <summary>Speed of sound used in the calculation (m/s).</summary>
        public float speedOfSound;
    }

    /// <summary>Snapshot of the current audio zone and mix state.</summary>
    [Serializable]
    public class AudioZoneState
    {
        /// <summary>Current primary zone.</summary>
        public AudioZoneType currentZone;
        /// <summary>Zone being transitioned to, if any.</summary>
        public AudioZoneType targetZone;
        /// <summary>Transition blend weight (0 = fully in current, 1 = fully in target).</summary>
        public float blendWeight;
        /// <summary>Whether a zone transition is currently active.</summary>
        public bool isTransitioning;
        /// <summary>Altitude in metres above sea level.</summary>
        public float altitudeMetres;
        /// <summary>Aircraft speed in m/s.</summary>
        public float speedMetresPerSecond;
    }

    /// <summary>Analytics record for a spatial audio session.</summary>
    [Serializable]
    public class SpatialAudioAnalyticsRecord
    {
        /// <summary>UTC timestamp of the session.</summary>
        public DateTime timestamp;
        /// <summary>Whether HRTF binaural audio was enabled.</summary>
        public bool hrtfEnabled;
        /// <summary>Audio quality preset selected by the user.</summary>
        public string qualityPreset;
        /// <summary>Total number of audio sources active at peak.</summary>
        public int peakActiveSources;
        /// <summary>Average CPU time spent on audio processing (ms per frame).</summary>
        public float avgAudioCpuMs;
    }
}
