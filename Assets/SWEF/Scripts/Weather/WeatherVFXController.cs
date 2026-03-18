using UnityEngine;

namespace SWEF.Weather
{
    /// <summary>
    /// Controls all weather-related particle systems and visual effects.
    /// Call <see cref="SetWeatherEffects"/> each frame (or on change) to keep
    /// all effects in sync with <see cref="WeatherStateManager.ActiveWeather"/>.
    ///
    /// <para>All <c>ParticleSystem</c> and <c>Light</c> references are optional —
    /// the controller degrades gracefully when they are absent.</para>
    ///
    /// <para>Particle counts scale with altitude and with the quality preset
    /// provided by <see cref="SWEF.Core.QualityPresetManager"/>.</para>
    /// </summary>
    public class WeatherVFXController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherVFXController Instance { get; private set; }

        // ── Inspector — particle systems ──────────────────────────────────────────
        [Header("Rain")]
        [Tooltip("Falling rain droplet particle system.")]
        [SerializeField] private ParticleSystem rainParticles;

        [Tooltip("Rain splash effect on surfaces.")]
        [SerializeField] private ParticleSystem rainSplashParticles;

        [Header("Snow")]
        [Tooltip("Falling snowflake particle system.")]
        [SerializeField] private ParticleSystem snowParticles;

        [Header("Fog")]
        [Tooltip("Volumetric/density fog controller particle (or managed via RenderSettings).")]
        [SerializeField] private ParticleSystem fogParticles;

        [Header("Thunderstorm")]
        [Tooltip("Lightning flash directional light (pulsed on lightning strike).")]
        [SerializeField] private Light lightningLight;

        [Tooltip("Base intensity of the lightning directional light.")]
        [SerializeField] private float lightningBaseIntensity = 8f;

        [Tooltip("Minimum/maximum seconds between automatic lightning flashes.")]
        [SerializeField] private Vector2 lightningIntervalRange = new Vector2(3f, 12f);

        [Header("Sandstorm")]
        [Tooltip("Sand/dust particle system for sandstorm conditions.")]
        [SerializeField] private ParticleSystem sandParticles;

        [Header("Hail")]
        [Tooltip("Ice pellet particle system.")]
        [SerializeField] private ParticleSystem hailParticles;

        [Header("Altitude")]
        [Tooltip("Altitude above which ALL weather VFX are fully faded out (metres).")]
        [SerializeField] private float vfxFadeOutAltitude = 10000f;

        // ── Inspector — quality ───────────────────────────────────────────────────
        [Header("Quality Scaling")]
        [Tooltip("Maximum rain particle count at highest quality.")]
        [SerializeField] private int rainMaxParticles = 1000;

        [Tooltip("Maximum snow particle count at highest quality.")]
        [SerializeField] private int snowMaxParticles = 600;

        [Tooltip("Maximum sand particle count at highest quality.")]
        [SerializeField] private int sandMaxParticles = 800;

        [Tooltip("Maximum hail particle count at highest quality.")]
        [SerializeField] private int hailMaxParticles = 300;

        // ── Internal ──────────────────────────────────────────────────────────────
        private float _lightningTimer;
        private float _lightningNextInterval;
        private float _lightningFlashTimer;
        private bool  _isThunderstorm;
        private float _currentIntensity;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _lightningNextInterval = Random.Range(lightningIntervalRange.x, lightningIntervalRange.y);
        }

        private void Start()
        {
            if (WeatherStateManager.Instance != null)
                WeatherStateManager.Instance.OnWeatherStateUpdated += SetWeatherEffects;
        }

        private void OnDestroy()
        {
            if (WeatherStateManager.Instance != null)
                WeatherStateManager.Instance.OnWeatherStateUpdated -= SetWeatherEffects;
        }

        private void Update()
        {
            HandleLightningFlash();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies visual effects that match the supplied <see cref="WeatherData"/> snapshot.
        /// Called automatically when subscribed to <see cref="WeatherStateManager.OnWeatherStateUpdated"/>.
        /// </summary>
        /// <param name="data">Current (altitude-adjusted) weather data.</param>
        public void SetWeatherEffects(WeatherData data)
        {
            float altitude      = WeatherStateManager.Instance != null
                ? WeatherStateManager.Instance.AltitudeMeters : 0f;
            float altitudeFade  = 1f - Mathf.Clamp01(altitude / vfxFadeOutAltitude);
            float intensity     = data.precipitationIntensity * altitudeFade;
            _currentIntensity   = intensity;

            // Quality multiplier (0.25 = Low, 0.5 = Medium, 0.75 = High, 1.0 = Ultra)
            float quality = GetQualityMultiplier();

            switch (data.condition)
            {
                case WeatherCondition.Rain:
                    SetRain(intensity * 0.5f, data.windSpeedMs, data.windDirectionDeg, quality);
                    StopSnow(); StopSand(); StopHail(); StopFog();
                    _isThunderstorm = false;
                    break;

                case WeatherCondition.HeavyRain:
                    SetRain(intensity, data.windSpeedMs, data.windDirectionDeg, quality);
                    StopSnow(); StopSand(); StopHail(); StopFog();
                    _isThunderstorm = false;
                    break;

                case WeatherCondition.Snow:
                    SetSnow(intensity * 0.5f, data.windSpeedMs, data.windDirectionDeg, quality);
                    StopRain(); StopSand(); StopHail(); StopFog();
                    _isThunderstorm = false;
                    break;

                case WeatherCondition.HeavySnow:
                    SetSnow(intensity, data.windSpeedMs, data.windDirectionDeg, quality);
                    StopRain(); StopSand(); StopHail(); StopFog();
                    _isThunderstorm = false;
                    break;

                case WeatherCondition.Fog:
                    SetFog(0.004f * altitudeFade);
                    StopRain(); StopSnow(); StopSand(); StopHail();
                    _isThunderstorm = false;
                    break;

                case WeatherCondition.DenseFog:
                    SetFog(0.01f * altitudeFade);
                    StopRain(); StopSnow(); StopSand(); StopHail();
                    _isThunderstorm = false;
                    break;

                case WeatherCondition.Thunderstorm:
                    SetRain(intensity, data.windSpeedMs, data.windDirectionDeg, quality);
                    StopSnow(); StopSand(); StopHail(); StopFog();
                    _isThunderstorm = altitudeFade > 0.1f;
                    break;

                case WeatherCondition.Hail:
                    SetHail(intensity, data.windSpeedMs, data.windDirectionDeg, quality);
                    StopRain(); StopSnow(); StopSand(); StopFog();
                    _isThunderstorm = false;
                    break;

                case WeatherCondition.Sandstorm:
                    SetSandstorm(intensity, data.windSpeedMs, data.windDirectionDeg, quality);
                    StopRain(); StopSnow(); StopHail(); StopFog();
                    _isThunderstorm = false;
                    break;

                default:
                    StopRain(); StopSnow(); StopSand(); StopHail();
                    SetFog(0f);
                    _isThunderstorm = false;
                    break;
            }
        }

        // ── Particle helpers ──────────────────────────────────────────────────────

        private void SetRain(float intensity, float windSpeed, float windDir, float quality)
        {
            if (rainParticles == null) return;

            int maxCount = Mathf.RoundToInt(rainMaxParticles * quality * intensity);
            SetParticleMaxCount(rainParticles, Mathf.Max(1, maxCount));
            ApplyWindToParticles(rainParticles, windSpeed, windDir);
            if (!rainParticles.isPlaying) rainParticles.Play();

            if (rainSplashParticles != null)
            {
                SetParticleMaxCount(rainSplashParticles, Mathf.Max(1, maxCount / 4));
                if (!rainSplashParticles.isPlaying) rainSplashParticles.Play();
            }
        }

        private void StopRain()
        {
            SafeStop(rainParticles);
            SafeStop(rainSplashParticles);
        }

        private void SetSnow(float intensity, float windSpeed, float windDir, float quality)
        {
            if (snowParticles == null) return;

            int maxCount = Mathf.RoundToInt(snowMaxParticles * quality * intensity);
            SetParticleMaxCount(snowParticles, Mathf.Max(1, maxCount));
            ApplyWindToParticles(snowParticles, windSpeed * 0.5f, windDir);
            if (!snowParticles.isPlaying) snowParticles.Play();
        }

        private void StopSnow() => SafeStop(snowParticles);

        private void SetFog(float density)
        {
            RenderSettings.fog        = density > 0f;
            RenderSettings.fogDensity = density;

            if (fogParticles != null)
            {
                if (density > 0f && !fogParticles.isPlaying) fogParticles.Play();
                else if (density <= 0f)                       SafeStop(fogParticles);
            }
        }

        private void SetSandstorm(float intensity, float windSpeed, float windDir, float quality)
        {
            if (sandParticles == null) return;

            int maxCount = Mathf.RoundToInt(sandMaxParticles * quality * intensity);
            SetParticleMaxCount(sandParticles, Mathf.Max(1, maxCount));
            ApplyWindToParticles(sandParticles, windSpeed, windDir);
            if (!sandParticles.isPlaying) sandParticles.Play();

            // Sandstorm reduces visibility via fog
            SetFog(0.005f * intensity);
        }

        private void StopSand() => SafeStop(sandParticles);

        private void SetHail(float intensity, float windSpeed, float windDir, float quality)
        {
            if (hailParticles == null) return;

            int maxCount = Mathf.RoundToInt(hailMaxParticles * quality * intensity);
            SetParticleMaxCount(hailParticles, Mathf.Max(1, maxCount));
            ApplyWindToParticles(hailParticles, windSpeed, windDir);
            if (!hailParticles.isPlaying) hailParticles.Play();
        }

        private void StopHail() => SafeStop(hailParticles);
        private void StopFog()  => SetFog(0f);

        // ── Lightning flash ───────────────────────────────────────────────────────

        private void HandleLightningFlash()
        {
            if (!_isThunderstorm || lightningLight == null) return;

            _lightningTimer += Time.deltaTime;
            if (_lightningTimer >= _lightningNextInterval)
            {
                _lightningTimer        = 0f;
                _lightningNextInterval = Random.Range(lightningIntervalRange.x, lightningIntervalRange.y);
                _lightningFlashTimer   = 0.15f;
                lightningLight.intensity = lightningBaseIntensity;
            }

            if (_lightningFlashTimer > 0f)
            {
                _lightningFlashTimer -= Time.deltaTime;
                if (_lightningFlashTimer <= 0f)
                    lightningLight.intensity = 0f;
            }
        }

        // ── Utilities ─────────────────────────────────────────────────────────────

        private static void SetParticleMaxCount(ParticleSystem ps, int count)
        {
            var main = ps.main;
            main.maxParticles = count;
        }

        private static void ApplyWindToParticles(ParticleSystem ps, float windSpeed, float windDir)
        {
            float rad   = windDir * Mathf.Deg2Rad;
            float forceX = -Mathf.Sin(rad) * windSpeed * 0.5f;
            float forceZ = -Mathf.Cos(rad) * windSpeed * 0.5f;

            var forceOverLifetime = ps.forceOverLifetime;
            forceOverLifetime.enabled = windSpeed > 0.5f;
            forceOverLifetime.x       = new ParticleSystem.MinMaxCurve(forceX);
            forceOverLifetime.z       = new ParticleSystem.MinMaxCurve(forceZ);
        }

        private static void SafeStop(ParticleSystem ps)
        {
            if (ps != null && ps.isPlaying) ps.Stop();
        }

        private float GetQualityMultiplier()
        {
            var qpm = SWEF.Core.QualityPresetManager.Instance;
            if (qpm == null) return 1f;

            // Map QualitySettings level to a multiplier
            int lvl = QualitySettings.GetQualityLevel();
            return lvl switch
            {
                0 => 0.25f,
                1 => 0.5f,
                2 => 0.75f,
                _ => 1.0f
            };
        }
    }
}
