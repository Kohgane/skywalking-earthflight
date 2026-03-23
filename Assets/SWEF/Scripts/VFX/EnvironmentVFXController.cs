// EnvironmentVFXController.cs — SWEF Particle Effects & VFX System
using UnityEngine;

namespace SWEF.VFX
{
    /// <summary>
    /// Manages world-environment particle effects driven by weather conditions,
    /// biome classification, latitude, and time of day.
    ///
    /// <para>Effects handled: rain splash on surfaces, snow accumulation, sand/dust storms,
    /// leaf scatter in forest biomes, pollen drift in temperate/rainforest, volcanic ash
    /// near volcanic terrain, and aurora borealis above 60° latitude.</para>
    ///
    /// <para>Integrates with <c>WeatherManager</c>, biome data, and
    /// <c>TimeOfDayManager</c> when the corresponding compile symbols are defined.
    /// All particle systems must be assigned via the inspector.</para>
    /// </summary>
    public sealed class EnvironmentVFXController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Weather Particle Systems")]
        [Tooltip("Rain splash particles emitted on ground surfaces.")]
        [SerializeField] private ParticleSystem rainSplashSystem;

        [Tooltip("Snow accumulation and flurry particles.")]
        [SerializeField] private ParticleSystem snowSystem;

        [Tooltip("Sand and dust storm particles.")]
        [SerializeField] private ParticleSystem sandStormSystem;

        [Tooltip("Lightning flash / arc visual effect.")]
        [SerializeField] private ParticleSystem lightningSystem;

        [Header("Biome Particle Systems")]
        [Tooltip("Leaf scatter particles used in temperate / boreal forest biomes.")]
        [SerializeField] private ParticleSystem leafScatterSystem;

        [Tooltip("Pollen drift particles used in spring / temperate / rainforest biomes.")]
        [SerializeField] private ParticleSystem pollenSystem;

        [Tooltip("Volcanic ash particles active near volcanic terrain.")]
        [SerializeField] private ParticleSystem volcanicAshSystem;

        [Header("Atmospheric Effects")]
        [Tooltip("Aurora borealis ribbon particle system.")]
        [SerializeField] private ParticleSystem auroraSystem;

        [Tooltip("Minimum absolute latitude (degrees) above which the aurora activates.")]
        [SerializeField, Range(0f, 90f)] private float auroraMinLatitude = 60f;

        [Header("External State (override when integrations unavailable)")]
        [Tooltip("Current weather type index. Matches WeatherType enum order.")]
        [SerializeField] private int currentWeatherIndex;

        [Tooltip("Current biome type index. Matches BiomeType enum order.")]
        [SerializeField] private int currentBiomeIndex;

        [Tooltip("Current latitude in decimal degrees.")]
        [SerializeField, Range(-90f, 90f)] private float currentLatitude;

        [Tooltip("Current time-of-day hour (0–23.99).")]
        [SerializeField, Range(0f, 24f)] private float currentHour = 12f;

        [Header("Emission Rate Multipliers")]
        [Tooltip("Rain splash emission rate at max precipitation.")]
        [SerializeField, Min(0f)] private float maxRainEmission = 80f;

        [Tooltip("Snow emission rate at max snowfall.")]
        [SerializeField, Min(0f)] private float maxSnowEmission = 60f;

        [Tooltip("Sand storm emission rate.")]
        [SerializeField, Min(0f)] private float maxSandEmission = 120f;

        // ── Private State ─────────────────────────────────────────────────────────

        private float _precipitationIntensity;  // 0–1
        private bool  _isRaining;
        private bool  _isSnowing;
        private bool  _isThunderstorm;
        private bool  _isSandstorm;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            PollIntegrations();
            UpdateWeatherEffects();
            UpdateBiomeEffects();
            UpdateAtmosphericEffects();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Forces a specific weather state for testing or runtime override.</summary>
        /// <param name="weatherIndex">WeatherType enum value cast to int.</param>
        /// <param name="intensity">Precipitation intensity 0–1.</param>
        public void SetWeather(int weatherIndex, float intensity)
        {
            currentWeatherIndex  = weatherIndex;
            _precipitationIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>Forces the current biome for environment effect selection.</summary>
        /// <param name="biomeIndex">BiomeType enum value cast to int.</param>
        public void SetBiome(int biomeIndex) => currentBiomeIndex = biomeIndex;

        /// <summary>Sets the current latitude for aurora borealis evaluation.</summary>
        /// <param name="latitude">Latitude in decimal degrees (−90 to +90).</param>
        public void SetLatitude(float latitude) => currentLatitude = Mathf.Clamp(latitude, -90f, 90f);

        // ── Internal Updates ──────────────────────────────────────────────────────

        private void UpdateWeatherEffects()
        {
            // 0=Clear,1=PartlyCloudy,2=Overcast,3=Rain,4=HeavyRain,5=Thunderstorm,6=Snow,7=Blizzard,8=Fog,9=Sandstorm
            _isRaining      = currentWeatherIndex is 3 or 4;
            _isThunderstorm = currentWeatherIndex == 5;
            _isSnowing      = currentWeatherIndex is 6 or 7;
            _isSandstorm    = currentWeatherIndex == 9;

            SetEmission(rainSplashSystem, (_isRaining || _isThunderstorm)
                ? maxRainEmission * _precipitationIntensity : 0f);

            SetEmission(snowSystem, _isSnowing
                ? maxSnowEmission * _precipitationIntensity : 0f);

            SetEmission(sandStormSystem, _isSandstorm
                ? maxSandEmission : 0f);

            SetEmission(lightningSystem, _isThunderstorm ? 1f : 0f);
        }

        private void UpdateBiomeEffects()
        {
            // Leaf scatter: Temperate(2), Boreal(3), Rainforest(13), Steppe(14)
            bool leafBiome = currentBiomeIndex is 2 or 3 or 13 or 14;
            SetEmission(leafScatterSystem, leafBiome ? 20f : 0f);

            // Pollen: Temperate(2), Tropical(1), Rainforest(13), Savanna(12)
            bool pollenBiome = currentBiomeIndex is 1 or 2 or 12 or 13;
            // Pollen active in spring/summer (roughly hours 6–18)
            bool isDay = currentHour >= 6f && currentHour <= 18f;
            SetEmission(pollenSystem, (pollenBiome && isDay) ? 15f : 0f);

            // Volcanic ash: Volcanic(11)
            SetEmission(volcanicAshSystem, currentBiomeIndex == 11 ? 50f : 0f);
        }

        private void UpdateAtmosphericEffects()
        {
            // Aurora: high latitudes, active at night
            bool highLatitude = Mathf.Abs(currentLatitude) >= auroraMinLatitude;
            bool isNight      = currentHour < 5f || currentHour > 21f;
            SetEmission(auroraSystem, (highLatitude && isNight) ? 30f : 0f);
        }

        private static void SetEmission(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var emission = ps.emission;
            if (rate > 0f)
            {
                emission.rateOverTime = rate;
                if (!ps.isPlaying) ps.Play();
            }
            else
            {
                emission.rateOverTime = 0f;
                if (ps.isPlaying) ps.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void PollIntegrations()
        {
#if SWEF_WEATHER_AVAILABLE
            if (SWEF.Weather.WeatherManager.Instance != null)
            {
                var weather = SWEF.Weather.WeatherManager.Instance.CurrentWeather;
                currentWeatherIndex     = (int)weather.weatherType;
                _precipitationIntensity = weather.precipitationIntensity;
            }
#endif
#if SWEF_BIOME_AVAILABLE
            // Biome index is set externally by the scene controller that calls SetBiome()
#endif
#if SWEF_TIMEOFDAY_AVAILABLE
            if (SWEF.TimeOfDay.TimeOfDayManager.Instance != null)
                currentHour = SWEF.TimeOfDay.TimeOfDayManager.Instance.CurrentHour;
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Test Rain")]
        private void EditorTestRain() { currentWeatherIndex = 3; _precipitationIntensity = 0.8f; }

        [ContextMenu("Test Snow")]
        private void EditorTestSnow() { currentWeatherIndex = 6; _precipitationIntensity = 0.7f; }

        [ContextMenu("Test Sandstorm")]
        private void EditorTestSandstorm() => currentWeatherIndex = 9;

        [ContextMenu("Test Aurora")]
        private void EditorTestAurora() { currentLatitude = 70f; currentHour = 23f; }

        [ContextMenu("Test Volcanic Ash")]
        private void EditorTestVolcanic() => currentBiomeIndex = 11;
#endif
    }
}
