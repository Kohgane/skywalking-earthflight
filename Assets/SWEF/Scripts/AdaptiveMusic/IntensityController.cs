// IntensityController.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Maps an intensity value (0–1) to the set of <see cref="MusicLayer"/>s that should
    /// be active, following the layering rules defined in the spec:
    ///
    ///   0.0–0.2 → Pads only
    ///   0.2–0.4 → + Strings
    ///   0.4–0.6 → + Melody + Bass
    ///   0.6–0.8 → + Drums + Percussion
    ///   0.8–1.0 → + Choir + Synth (all 8 layers)
    ///
    /// Per-layer volume curves are sourced from the active <see cref="AdaptiveMusicProfile"/>.
    /// </summary>
    public class IntensityController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Profile")]
        [SerializeField] private AdaptiveMusicProfile _profile;

        [Header("Smoothing")]
        [Tooltip("Rate at which displayed intensity lerps toward the target value (per second).")]
        [Range(0.01f, 10f)]
        [SerializeField] private float _smoothSpeed = 2f;

        // ── State ─────────────────────────────────────────────────────────────

        private float _smoothedIntensity;
        private float _targetIntensity;

        // Intensity thresholds
        private const float T1 = 0.2f;
        private const float T2 = 0.4f;
        private const float T3 = 0.6f;
        private const float T4 = 0.8f;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Update()
        {
            _smoothedIntensity = Mathf.Lerp(_smoothedIntensity, _targetIntensity,
                Time.deltaTime * _smoothSpeed);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Sets the target intensity (0–1). Actual intensity smoothly follows.</summary>
        public void SetIntensity(float intensity)
        {
            _targetIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>Returns the current smoothed intensity.</summary>
        public float GetCurrentIntensity() => _smoothedIntensity;

        /// <summary>
        /// Returns the list of <see cref="MusicLayer"/>s that should be active at the
        /// given <paramref name="intensity"/> level.
        /// </summary>
        public static List<MusicLayer> GetActiveLayersForIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            var layers = new List<MusicLayer>();

            // Tier 1 — always present above zero
            if (intensity >= 0f)
                layers.Add(MusicLayer.Pads);

            // Tier 2
            if (intensity >= T1)
                layers.Add(MusicLayer.Strings);

            // Tier 3
            if (intensity >= T2)
            {
                layers.Add(MusicLayer.Melody);
                layers.Add(MusicLayer.Bass);
            }

            // Tier 4
            if (intensity >= T3)
            {
                layers.Add(MusicLayer.Drums);
                layers.Add(MusicLayer.Percussion);
            }

            // Tier 5
            if (intensity >= T4)
            {
                layers.Add(MusicLayer.Choir);
                layers.Add(MusicLayer.Synth);
            }

            return layers;
        }

        /// <summary>
        /// Returns the target volume (0–1) for a specific <paramref name="layer"/> at
        /// the given <paramref name="intensity"/>, using per-layer curves from the profile
        /// if one is assigned.
        /// </summary>
        public float GetLayerVolume(MusicLayer layer, float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            if (_profile != null)
            {
                var curve = _profile.GetLayerCurve(layer);
                return Mathf.Clamp01(curve.Evaluate(intensity));
            }
            // Fallback: linear ramp above the layer's activation threshold
            return Mathf.Clamp01(intensity);
        }
    }
}
