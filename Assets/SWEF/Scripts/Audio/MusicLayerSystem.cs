using System;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Flight;
using SWEF.Util;

namespace SWEF.Audio
{
    /// <summary>
    /// Dynamic layered music system. Multiple audio layers play simultaneously and their
    /// volumes crossfade based on game state (altitude, takeoff, achievements, etc.).
    /// Integrates with <see cref="AltitudeController"/> for altitude-driven layer changes.
    /// </summary>
    public class MusicLayerSystem : MonoBehaviour
    {
        // ── Nested types ──────────────────────────────────────────────────────────

        public enum MusicLayerType
        {
            Base,
            Tension,
            Wonder,
            Triumph,
            Ambient,
        }

        [Serializable]
        public class MusicLayer
        {
            public MusicLayerType type;
            public AudioClip      clip;
            [Range(0f, 1f)]
            public float targetVolume;
            [HideInInspector]
            public float currentVolume;
            [Range(0.1f, 10f)]
            public float fadeSpeed = 2f;

            [HideInInspector] public AudioSource source;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Music Layers")]
        [SerializeField] private List<MusicLayer> layers = new List<MusicLayer>();

        [Header("Altitude Thresholds")]
        [SerializeField] private float takeoffAltitude  =   200f;   // m — begin Tension
        [SerializeField] private float wonderAltitude   = 10000f;   // 10 km  — Wonder
        [SerializeField] private float triumphAltitude  = 100000f;  // Kármán line — Triumph
        [SerializeField] private float cruiseAltitude   =  5000f;   // Ambient at cruise

        [Header("Stinger")]
        [SerializeField] private AudioClip teleportStingerClip;

        [Header("BPM sync (optional)")]
        [SerializeField] private bool  bpmSync = false;
        [SerializeField] private float bpm     = 120f;

        [Header("Refs (auto-found if null)")]
        [SerializeField] private AltitudeController altitudeController;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private AudioSource _stingerSource;
        private float       _previousAltitude;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (altitudeController == null)
                altitudeController = FindFirstObjectByType<AltitudeController>();

            foreach (var layer in layers)
            {
                var go  = new GameObject($"Music_{layer.type}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.clip        = layer.clip;
                src.loop        = true;
                src.spatialBlend = 0f;
                src.volume      = 0f;
                src.playOnAwake = false;
                if (layer.clip != null) src.Play();
                layer.source        = src;
                layer.currentVolume = 0f;
            }

            // Stinger source
            var stingerGo = new GameObject("Music_Stinger");
            stingerGo.transform.SetParent(transform);
            _stingerSource = stingerGo.AddComponent<AudioSource>();
            _stingerSource.spatialBlend = 0f;
            _stingerSource.loop         = false;
        }

        private void Update()
        {
            float alt = altitudeController != null ? altitudeController.CurrentAltitudeMeters : 0f;
            float dt  = Time.deltaTime;

            UpdateTargetVolumes(alt);
            FadeLayers(dt);

            _previousAltitude = alt;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Forces the target volume for a specific layer (0–1).</summary>
        public void SetLayerTargetVolume(MusicLayerType type, float volume)
        {
            foreach (var layer in layers)
            {
                if (layer.type == type)
                {
                    layer.targetVolume = Mathf.Clamp01(volume);
                    return;
                }
            }
        }

        /// <summary>Returns current volume for the given layer (for debug display).</summary>
        public float GetLayerCurrentVolume(MusicLayerType type)
        {
            foreach (var layer in layers)
                if (layer.type == type) return layer.currentVolume;
            return 0f;
        }

        /// <summary>Plays the teleport stinger one-shot.</summary>
        public void PlayTeleportStinger()
        {
            if (teleportStingerClip != null)
                _stingerSource.PlayOneShot(teleportStingerClip);
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void UpdateTargetVolumes(float alt)
        {
            bool isTakingOff = alt >= takeoffAltitude;
            bool isWondering = alt >= wonderAltitude;
            bool isTriumphing = alt >= triumphAltitude;

            foreach (var layer in layers)
            {
                layer.targetVolume = layer.type switch
                {
                    MusicLayerType.Base     => isTakingOff  ? 0.6f : 0.8f,
                    MusicLayerType.Tension  => isTakingOff && !isWondering ? 0.5f : 0f,
                    MusicLayerType.Wonder   => isWondering  && !isTriumphing ? 0.6f : 0f,
                    MusicLayerType.Triumph  => isTriumphing ? 0.8f : 0f,
                    MusicLayerType.Ambient  => !isTakingOff ? 0.4f : 0f,
                    _ => layer.targetVolume,
                };
            }
        }

        private void FadeLayers(float dt)
        {
            foreach (var layer in layers)
            {
                if (layer.source == null) continue;
                layer.currentVolume = ExpSmoothing.ExpLerp(
                    layer.currentVolume, layer.targetVolume, layer.fadeSpeed, dt);
                layer.source.volume = layer.currentVolume;
            }
        }
    }
}
