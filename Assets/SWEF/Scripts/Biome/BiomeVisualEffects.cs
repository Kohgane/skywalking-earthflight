// BiomeVisualEffects.cs — SWEF Terrain Detail & Biome System
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Biome
{
    /// <summary>
    /// Singleton MonoBehaviour that applies and transitions biome-specific visual
    /// effects including post-processing hints, particle systems, and fog settings.
    /// </summary>
    public class BiomeVisualEffects : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static BiomeVisualEffects Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        #endregion

        #region Events

        /// <summary>Raised after a biome's visual settings have been fully applied.</summary>
        public event Action<BiomeType> OnBiomeVisualChanged;

        #endregion

        #region Inspector Fields

        [Header("Profile")]
        [Tooltip("BiomeProfile asset containing per-biome visual configuration.")]
        [SerializeField] private BiomeProfile biomeProfile;

        [Header("Particle Systems")]
        [Tooltip("Particle system used for desert dust and sandstorm effects.")]
        [SerializeField] private ParticleSystem dustParticles;

        [Tooltip("Particle system used for arctic and tundra snowfall.")]
        [SerializeField] private ParticleSystem snowParticles;

        [Tooltip("Particle system used for temperate spring pollen.")]
        [SerializeField] private ParticleSystem pollenParticles;

        [Tooltip("Particle system used for wetland and rainforest ground mist.")]
        [SerializeField] private ParticleSystem mistParticles;

        [Header("Heat Shimmer")]
        [Tooltip("Material used to render the heat-shimmer distortion effect.")]
        [SerializeField] private Material heatShimmerMaterial;

        [Tooltip("Intensity of the heat-shimmer distortion 0–1.")]
        [SerializeField, Range(0f, 1f)] private float heatShimmerIntensity = 0.5f;

        [Header("Transition")]
        [Tooltip("Default duration for biome visual cross-fade in seconds.")]
        [SerializeField] private float defaultTransitionDuration = 3f;

        #endregion

        #region Private State

        private BiomeType   _currentBiome      = BiomeType.Temperate;
        private float       _particleIntensity  = 1f;
        private Coroutine   _transitionRoutine;

        #endregion

        #region Public API

        /// <summary>
        /// Immediately applies visual effects for the specified biome.
        /// </summary>
        /// <param name="biome">Target biome.</param>
        /// <param name="blendFactor">0–1 blend factor (1 = fully this biome).</param>
        public void ApplyBiomeEffects(BiomeType biome, float blendFactor)
        {
            _currentBiome = biome;
            blendFactor   = Mathf.Clamp01(blendFactor);

            ApplyFog(biome, blendFactor);
            ApplyParticles(biome, blendFactor);
            ApplyHeatShimmer(biome, blendFactor);

            OnBiomeVisualChanged?.Invoke(biome);
        }

        /// <summary>
        /// Smoothly transitions visual effects from one biome to another over time.
        /// </summary>
        /// <param name="from">Source biome.</param>
        /// <param name="to">Destination biome.</param>
        /// <param name="duration">Transition duration in seconds.</param>
        public void TransitionBetweenBiomes(BiomeType from, BiomeType to, float duration)
        {
            if (_transitionRoutine != null)
                StopCoroutine(_transitionRoutine);
            _transitionRoutine = StartCoroutine(TransitionRoutine(from, to, duration > 0f ? duration : defaultTransitionDuration));
        }

        /// <summary>
        /// Sets the global particle intensity multiplier applied to all particle systems.
        /// </summary>
        /// <param name="intensity">0 = off, 1 = full intensity.</param>
        public void SetParticleIntensity(float intensity)
        {
            _particleIntensity = Mathf.Clamp01(intensity);
            ApplyParticles(_currentBiome, 1f);
        }

        #endregion

        #region Private Methods

        private void ApplyFog(BiomeType biome, float blendFactor)
        {
            if (biomeProfile == null) return;
            var cfg = biomeProfile.GetConfig(biome);

            RenderSettings.fogColor   = Color.Lerp(RenderSettings.fogColor, cfg.fogColor, blendFactor);
            float targetDensity       = Mathf.Lerp(cfg.fogDensityRange.x, cfg.fogDensityRange.y, 0.5f);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetDensity, blendFactor);
            RenderSettings.fog        = true;
        }

        private void ApplyParticles(BiomeType biome, float blendFactor)
        {
            float scaledIntensity = blendFactor * _particleIntensity;

            SetParticleEmission(dustParticles,   biome == BiomeType.Desert  || biome == BiomeType.Steppe,  scaledIntensity);
            SetParticleEmission(snowParticles,   biome == BiomeType.Arctic  || biome == BiomeType.Tundra || biome == BiomeType.Mountain, scaledIntensity);
            SetParticleEmission(pollenParticles, biome == BiomeType.Temperate || biome == BiomeType.Rainforest, scaledIntensity * 0.4f);
            SetParticleEmission(mistParticles,   biome == BiomeType.Wetland || biome == BiomeType.Rainforest, scaledIntensity);
        }

        private static void SetParticleEmission(ParticleSystem ps, bool active, float intensity)
        {
            if (ps == null) return;
            var emission = ps.emission;
            emission.enabled = active;
            if (active)
            {
                var rate        = emission.rateOverTime;
                rate.constant   = Mathf.Lerp(0f, 100f, intensity);
                emission.rateOverTime = rate;
            }
        }

        private void ApplyHeatShimmer(BiomeType biome, float blendFactor)
        {
            if (heatShimmerMaterial == null) return;
            bool active   = (biome == BiomeType.Desert || biome == BiomeType.Volcanic);
            float target  = active ? heatShimmerIntensity * blendFactor : 0f;
            heatShimmerMaterial.SetFloat("_Intensity", target);
        }

        private IEnumerator TransitionRoutine(BiomeType from, BiomeType to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / duration);
                ApplyBiomeEffects(to, t);
                yield return null;
            }
            ApplyBiomeEffects(to, 1f);
            _transitionRoutine = null;
        }

        #endregion
    }
}
