// VRSpatialAudio.cs — Phase 112: VR/XR Flight Experience
// 3D positional audio for engine, wind, and environment sounds.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Manages 3D positional audio sources in VR: engine roar, wind rush,
    /// and ambient environment sounds are positioned relative to head tracking.
    /// </summary>
    public class VRSpatialAudio : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Audio Sources")]
        [SerializeField] private AudioSource engineAudioSource;
        [SerializeField] private AudioSource windAudioSource;
        [SerializeField] private AudioSource ambientAudioSource;

        [Header("Engine")]
        [SerializeField] private float engineMinPitch  = 0.6f;
        [SerializeField] private float engineMaxPitch  = 2.0f;
        [SerializeField] private float engineMinVolume = 0.2f;
        [SerializeField] private float engineMaxVolume = 1.0f;

        [Header("Wind")]
        [SerializeField] private float windMinVolume = 0f;
        [SerializeField] private float windMaxVolume = 0.8f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current engine throttle level [0..1].</summary>
        public float EngineThrottleLevel { get; private set; }

        /// <summary>Current wind speed factor [0..1].</summary>
        public float WindSpeedFactor { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            if (engineAudioSource  != null && !engineAudioSource.isPlaying)  engineAudioSource.Play();
            if (windAudioSource    != null && !windAudioSource.isPlaying)    windAudioSource.Play();
            if (ambientAudioSource != null && !ambientAudioSource.isPlaying) ambientAudioSource.Play();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets engine throttle level [0..1], adjusting pitch and volume.</summary>
        public void SetEngineThrottleLevel(float level)
        {
            EngineThrottleLevel = Mathf.Clamp01(level);
            if (engineAudioSource == null) return;
            engineAudioSource.pitch  = Mathf.Lerp(engineMinPitch,  engineMaxPitch,  EngineThrottleLevel);
            engineAudioSource.volume = Mathf.Lerp(engineMinVolume, engineMaxVolume, EngineThrottleLevel);
        }

        /// <summary>Sets wind speed factor [0..1], adjusting wind audio volume.</summary>
        public void SetWindSpeed(float factor)
        {
            WindSpeedFactor = Mathf.Clamp01(factor);
            if (windAudioSource == null) return;
            windAudioSource.volume = Mathf.Lerp(windMinVolume, windMaxVolume, WindSpeedFactor);
        }

        /// <summary>Mutes or unmutes all VR spatial audio.</summary>
        public void SetMuted(bool muted)
        {
            float vol = muted ? 0f : 1f;
            if (engineAudioSource  != null) engineAudioSource.mute  = muted;
            if (windAudioSource    != null) windAudioSource.mute    = muted;
            if (ambientAudioSource != null) ambientAudioSource.mute = muted;
        }
    }
}
