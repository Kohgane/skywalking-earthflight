// SpatialAudioConfig.cs — Phase 118: Spatial Audio & 3D Soundscape
// ScriptableObject configuration for the spatial audio system.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Phase 118 — ScriptableObject that holds all tunable parameters for the
    /// Spatial Audio &amp; 3D Soundscape system. Create via
    /// <em>Assets → Create → SWEF → SpatialAudio → Config</em>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/SpatialAudio/Config", fileName = "SpatialAudioConfig")]
    public class SpatialAudioConfig : ScriptableObject
    {
        // ── Source Pool ───────────────────────────────────────────────────────────

        [Header("Audio Source Pool")]
        [Tooltip("Maximum number of simultaneous 3D audio sources.")]
        [Range(8, 256)] public int maxAudioSources = 64;

        [Tooltip("Number of sources reserved for critical warning audio.")]
        [Range(1, 16)] public int reservedWarningSources = 4;

        // ── Distance Attenuation ──────────────────────────────────────────────────

        [Header("Distance Attenuation")]
        [Tooltip("Sound propagation model used for distance falloff.")]
        public SoundPropagationModel propagationModel = SoundPropagationModel.Logarithmic;

        [Tooltip("Minimum distance at which full volume is heard (metres).")]
        [Range(0.1f, 100f)] public float minDistance = 5f;

        [Tooltip("Maximum distance at which sound is fully attenuated (metres).")]
        [Range(100f, 50000f)] public float maxDistance = 5000f;

        [Tooltip("Custom attenuation curve overriding the propagation model.")]
        public AnimationCurve attenuationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        // ── Doppler ───────────────────────────────────────────────────────────────

        [Header("Doppler Effect")]
        [Tooltip("Global Doppler effect intensity factor (0 = off, 1 = realistic, >1 = exaggerated).")]
        [Range(0f, 5f)] public float dopplerFactor = 1f;

        [Tooltip("Speed of sound used in Doppler calculations (m/s). Standard: 343 m/s.")]
        [Range(200f, 500f)] public float speedOfSound = 343f;

        // ── Reverb ────────────────────────────────────────────────────────────────

        [Header("Reverb")]
        [Tooltip("Global reverb quality level (0 = low, 3 = ultra).")]
        [Range(0, 3)] public int reverbQuality = 2;

        [Tooltip("Enable dynamic reverb zone transitions.")]
        public bool enableDynamicReverb = true;

        [Tooltip("Reverb zone crossfade duration in seconds.")]
        [Range(0.1f, 5f)] public float reverbCrossfadeDuration = 1f;

        // ── Occlusion ─────────────────────────────────────────────────────────────

        [Header("Occlusion")]
        [Tooltip("Occlusion processing method.")]
        public AudioOcclusionType occlusionType = AudioOcclusionType.Raycast;

        [Tooltip("Layer mask used for occlusion raycasts.")]
        public LayerMask occlusionLayerMask = ~0;

        [Tooltip("Low-pass cutoff frequency when occluded (Hz).")]
        [Range(200f, 20000f)] public float occlusionCutoffHz = 800f;

        [Tooltip("Volume reduction factor when fully occluded (0–1).")]
        [Range(0f, 1f)] public float occlusionVolumeReduction = 0.5f;

        // ── HRTF ──────────────────────────────────────────────────────────────────

        [Header("HRTF / Binaural")]
        [Tooltip("Enable Head-Related Transfer Function processing for headphone users.")]
        public bool enableHRTF = false;

        [Tooltip("HRTF processing quality (0 = low, 2 = high).")]
        [Range(0, 2)] public int hrtfQuality = 1;

        // ── Engine Audio ──────────────────────────────────────────────────────────

        [Header("Engine Audio")]
        [Tooltip("Base engine sound volume (0–1).")]
        [Range(0f, 1f)] public float engineVolume = 0.85f;

        [Tooltip("Engine pitch change rate per second for smooth transitions.")]
        [Range(0.1f, 10f)] public float enginePitchSlewRate = 2f;

        [Tooltip("Engine startup/shutdown sequence duration in seconds.")]
        [Range(1f, 30f)] public float engineStartupDuration = 8f;

        // ── Wind Noise ────────────────────────────────────────────────────────────

        [Header("Wind Noise")]
        [Tooltip("Wind noise base volume (0–1).")]
        [Range(0f, 1f)] public float windNoiseVolume = 0.6f;

        [Tooltip("Speed (m/s) at which wind noise begins.")]
        [Range(0f, 50f)] public float windNoiseOnsetSpeed = 10f;

        // ── Environment Soundscape ────────────────────────────────────────────────

        [Header("Environment Soundscape")]
        [Tooltip("Ambient soundscape master volume (0–1).")]
        [Range(0f, 1f)] public float ambientVolume = 0.4f;

        [Tooltip("Altitude (metres AGL) above which city sounds fade out.")]
        [Range(100f, 5000f)] public float cityAudioFadeAltitude = 500f;

        [Tooltip("Altitude (metres AGL) above which all exterior ambient sounds fade out.")]
        [Range(500f, 20000f)] public float highAltitudeFadeAltitude = 3000f;

        // ── Cockpit Audio ─────────────────────────────────────────────────────────

        [Header("Cockpit Audio")]
        [Tooltip("Cockpit ambient (avionics, gyro) volume (0–1).")]
        [Range(0f, 1f)] public float cockpitAmbientVolume = 0.3f;

        [Tooltip("Warning audio volume (0–1). Always at least 0.8 for safety.")]
        [Range(0.5f, 1f)] public float warningVolume = 1f;

        // ── Dynamic Range ─────────────────────────────────────────────────────────

        [Header("Dynamic Range")]
        [Tooltip("Enable auto-ducking of non-priority audio during radio calls.")]
        public bool enableAutoDucking = true;

        [Tooltip("Duck volume multiplier applied to non-priority sources (0–1).")]
        [Range(0f, 1f)] public float duckingVolumeMultiplier = 0.3f;

        [Tooltip("Ducking fade-in duration in seconds.")]
        [Range(0.05f, 1f)] public float duckingFadeDuration = 0.15f;

        // ── Transition ────────────────────────────────────────────────────────────

        [Header("Zone Transitions")]
        [Tooltip("Interior/exterior audio transition duration in seconds.")]
        [Range(0.1f, 3f)] public float interiorExteriorTransitionDuration = 0.5f;

        [Tooltip("Altitude-based mix transition rate per second.")]
        [Range(0.01f, 1f)] public float altitudeMixTransitionRate = 0.1f;
    }
}
