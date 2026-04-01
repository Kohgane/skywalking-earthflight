// IntensityController.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Maps a normalised intensity value (0–1) to a set of active
    /// <see cref="MusicLayer"/>s and delegates per-layer volume changes to
    /// <see cref="StemMixer"/> using smooth interpolation.
    ///
    /// <para>
    /// Intensity → Layer mapping:
    /// <list type="bullet">
    ///   <item>0.0–0.2 → Pads</item>
    ///   <item>0.2–0.4 → + Strings</item>
    ///   <item>0.4–0.6 → + Melody, Bass</item>
    ///   <item>0.6–0.8 → + Drums, Percussion</item>
    ///   <item>0.8–1.0 → + Choir, Synth (all layers)</item>
    /// </list>
    /// </para>
    /// </summary>
    public class IntensityController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [Tooltip("StemMixer that controls AudioSource volumes.")]
        [SerializeField] private StemMixer stemMixer;

        [Header("Smoothing")]
        [Tooltip("Rate at which intensity smoothly follows the target (units/second).")]
        [SerializeField, Min(0.01f)] private float smoothSpeed = 0.5f;

        // ── State ─────────────────────────────────────────────────────────────────
        private float _smoothedIntensity;
        private float _targetIntensity;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (stemMixer == null)
                stemMixer = GetComponent<StemMixer>();
        }

        private void Update()
        {
            if (Mathf.Approximately(_smoothedIntensity, _targetIntensity))
                return;

            _smoothedIntensity = Mathf.MoveTowards(_smoothedIntensity, _targetIntensity,
                                                    smoothSpeed * Time.deltaTime);
            ApplyIntensity(_smoothedIntensity);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the target intensity (0–1). Volume changes are interpolated smoothly.</summary>
        public void SetIntensity(float intensity)
        {
            _targetIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Returns the list of layers that should be active at the given intensity.
        /// Boundary values are inclusive of the upper threshold.
        /// </summary>
        public static List<MusicLayer> GetActiveLayersForIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            List<MusicLayer> layers = new List<MusicLayer>();

            // Tier 1 (0.0+): Pads always present
            layers.Add(MusicLayer.Pads);

            if (intensity >= 0.2f)
                layers.Add(MusicLayer.Strings);

            if (intensity >= 0.4f)
            {
                layers.Add(MusicLayer.Melody);
                layers.Add(MusicLayer.Bass);
            }

            if (intensity >= 0.6f)
            {
                layers.Add(MusicLayer.Drums);
                layers.Add(MusicLayer.Percussion);
            }

            if (intensity >= 0.8f)
            {
                layers.Add(MusicLayer.Choir);
                layers.Add(MusicLayer.Synth);
            }

            return layers;
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private void ApplyIntensity(float intensity)
        {
            if (stemMixer == null)
                return;

            foreach (MusicLayer layer in System.Enum.GetValues(typeof(MusicLayer)))
            {
                // Calculate the per-layer target volume based on intensity thresholds
                float vol = GetLayerVolume(layer, intensity);
                stemMixer.SetStemVolume(layer, vol);
            }
        }

        private static float GetLayerVolume(MusicLayer layer, float intensity)
        {
            switch (layer)
            {
                case MusicLayer.Pads:
                    // Always audible; volume scales with intensity 0→1
                    return Mathf.Clamp01(Mathf.InverseLerp(0f, 0.2f, intensity) * 0.8f + 0.2f);

                case MusicLayer.Strings:
                    return Mathf.Clamp01(Mathf.InverseLerp(0.2f, 0.4f, intensity));

                case MusicLayer.Melody:
                    return Mathf.Clamp01(Mathf.InverseLerp(0.4f, 0.6f, intensity));

                case MusicLayer.Bass:
                    return Mathf.Clamp01(Mathf.InverseLerp(0.4f, 0.55f, intensity));

                case MusicLayer.Drums:
                    return Mathf.Clamp01(Mathf.InverseLerp(0.6f, 0.75f, intensity));

                case MusicLayer.Percussion:
                    return Mathf.Clamp01(Mathf.InverseLerp(0.6f, 0.8f, intensity));

                case MusicLayer.Choir:
                    return Mathf.Clamp01(Mathf.InverseLerp(0.8f, 0.9f, intensity));

                case MusicLayer.Synth:
                    return Mathf.Clamp01(Mathf.InverseLerp(0.8f, 1.0f, intensity));

                default:
                    return 0f;
            }
        }
    }
}
