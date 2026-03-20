using System;
using UnityEngine;
using SWEF.Flight;
using SWEF.Util;

namespace SWEF.Audio
{
    /// <summary>
    /// Manages ambient soundscape layers that crossfade based on the player's altitude.
    /// Six altitude bands each have an independent looping AudioSource whose volume is
    /// smoothly interpolated using <see cref="ExpSmoothing"/>.
    /// </summary>
    public class AltitudeSoundscapeController : MonoBehaviour
    {
        // ── Nested types ──────────────────────────────────────────────────────────

        [Serializable]
        public struct SoundscapeLayer
        {
            [Tooltip("Ambient audio clip to loop for this altitude band.")]
            public AudioClip clip;
            public float minAltitude;
            public float maxAltitude;
            [Range(0f, 1f)]
            public float maxVolume;
            [Tooltip("Altitude range (m) over which volume fades in/out at band boundaries.")]
            public float fadeRange;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Layers (ordered low → high)")]
        [SerializeField] private SoundscapeLayer[] layers = new SoundscapeLayer[]
        {
            new SoundscapeLayer { minAltitude =        0f, maxAltitude =     500f, maxVolume = 1.0f, fadeRange =  200f },
            new SoundscapeLayer { minAltitude =      500f, maxAltitude =    5000f, maxVolume = 0.9f, fadeRange =  500f },
            new SoundscapeLayer { minAltitude =     5000f, maxAltitude =   20000f, maxVolume = 0.8f, fadeRange = 2000f },
            new SoundscapeLayer { minAltitude =    20000f, maxAltitude =   80000f, maxVolume = 0.5f, fadeRange = 5000f },
            new SoundscapeLayer { minAltitude =    80000f, maxAltitude =  120000f, maxVolume = 0.2f, fadeRange = 5000f },
            new SoundscapeLayer { minAltitude =   120000f, maxAltitude = 9999999f, maxVolume = 0.05f, fadeRange = 5000f },
        };

        [Header("Crossfade")]
        [SerializeField] private float fadeSpeed = 3f;

        [Header("Refs (auto-found if null)")]
        [SerializeField] private AltitudeController altitudeController;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private AudioSource[] _sources;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (altitudeController == null)
                altitudeController = FindFirstObjectByType<AltitudeController>();

            _sources = new AudioSource[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                var go  = new GameObject($"Soundscape_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.clip        = layers[i].clip;
                src.loop        = true;
                src.spatialBlend = 0f; // ambient — 2D
                src.volume      = 0f;
                src.playOnAwake = false;
                if (layers[i].clip != null) src.Play();
                _sources[i] = src;
            }
        }

        private void Update()
        {
            float alt = altitudeController != null ? altitudeController.CurrentAltitudeMeters : 0f;
            float dt  = Time.deltaTime;

            for (int i = 0; i < layers.Length; i++)
            {
                float target = LayerVolume(in layers[i], alt);
                _sources[i].volume = ExpSmoothing.ExpLerp(_sources[i].volume, target, fadeSpeed, dt);

                // Lazy-start clip if assigned but not yet playing
                if (layers[i].clip != null && !_sources[i].isPlaying && _sources[i].volume > 0.001f)
                {
                    _sources[i].clip = layers[i].clip;
                    _sources[i].Play();
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Returns the target volume for a layer at the given altitude.</summary>
        public static float LayerVolume(in SoundscapeLayer layer, float altitude)
        {
            if (altitude < layer.minAltitude - layer.fadeRange ||
                altitude > layer.maxAltitude + layer.fadeRange)
                return 0f;

            float fadeIn  = Mathf.InverseLerp(layer.minAltitude - layer.fadeRange, layer.minAltitude, altitude);
            float fadeOut = Mathf.InverseLerp(layer.maxAltitude + layer.fadeRange, layer.maxAltitude, altitude);
            return Mathf.Min(fadeIn, fadeOut) * layer.maxVolume;
        }

        /// <summary>Exposes per-layer current volumes for debug display.</summary>
        public float GetLayerVolume(int index)
        {
            if (index < 0 || index >= _sources.Length) return 0f;
            return _sources[index].volume;
        }
    }
}
