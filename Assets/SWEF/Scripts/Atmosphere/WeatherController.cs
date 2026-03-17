using System;
using System.Collections;
using UnityEngine;
using SWEF.Flight;

namespace SWEF.Atmosphere
{
    /// <summary>
    /// Simulates dynamic weather effects based on altitude and time.
    /// Manages particle systems, sun intensity, and fog density per weather type.
    /// Supports manual weather override via <see cref="SetWeather"/>.
    /// </summary>
    public class WeatherController : MonoBehaviour
    {
        public enum WeatherType { Clear, Cloudy, Rain, Storm, Snow }

        [SerializeField] private AltitudeController altitudeSource;
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [SerializeField] private Light sunLight;

        [Header("Auto Weather")]
        [SerializeField] private float weatherChangeIntervalSec = 120f;
        [SerializeField] private bool autoWeatherEnabled = true;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 3f;

        /// <summary>Raised whenever the active weather type changes.</summary>
        public event Action<WeatherType> OnWeatherChanged;

        /// <summary>The currently active weather type.</summary>
        public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;

        private float _autoTimer;
        private Coroutine _transitionCoroutine;

        // Sun intensity targets per weather type
        private static readonly float[] SunIntensities = { 1.0f, 0.6f, 0.4f, 0.2f, 0.5f };

        // Fog density targets per weather type
        private static readonly float[] FogDensities = { 0.0001f, 0.001f, 0.003f, 0.005f, 0.002f };

        private void Start()
        {
            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();
            if (sunLight == null)
                sunLight = FindFirstObjectByType<Light>();

            ApplyWeatherImmediate(CurrentWeather);
        }

        private void Update()
        {
            if (!autoWeatherEnabled) return;

            _autoTimer += Time.deltaTime;
            if (_autoTimer >= weatherChangeIntervalSec)
            {
                _autoTimer = 0f;
                ChangeWeatherForAltitude();
            }
        }

        /// <summary>Manually sets the weather type for the current cycle.</summary>
        public void SetWeather(WeatherType type)
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            float altitude = altitudeSource != null ? altitudeSource.CurrentAltitudeMeters : 0f;
            WeatherType clamped = ClampWeatherForAltitude(type, altitude);

            Debug.Log($"[SWEF] Weather changed: {clamped} at altitude {altitude:0}m");

            CurrentWeather = clamped;
            _transitionCoroutine = StartCoroutine(TransitionWeather(clamped));
            OnWeatherChanged?.Invoke(clamped);
        }

        private void ChangeWeatherForAltitude()
        {
            float altitude = altitudeSource != null ? altitudeSource.CurrentAltitudeMeters : 0f;
            WeatherType[] candidates = GetCandidatesForAltitude(altitude);
            WeatherType chosen = candidates[UnityEngine.Random.Range(0, candidates.Length)];
            SetWeather(chosen);
        }

        private WeatherType ClampWeatherForAltitude(WeatherType type, float altitude)
        {
            if (altitude >= 20000f) return WeatherType.Clear;
            if (altitude >= 10000f)
            {
                if (type == WeatherType.Rain || type == WeatherType.Storm || type == WeatherType.Snow)
                    return WeatherType.Cloudy;
                return type;
            }
            if (altitude >= 2000f)
            {
                if (type == WeatherType.Rain || type == WeatherType.Storm)
                    return WeatherType.Cloudy;
                return type;
            }
            return type;
        }

        private WeatherType[] GetCandidatesForAltitude(float altitude)
        {
            if (altitude >= 20000f) return new[] { WeatherType.Clear };
            if (altitude >= 10000f) return new[] { WeatherType.Clear, WeatherType.Cloudy };
            if (altitude >= 2000f)  return new[] { WeatherType.Clear, WeatherType.Cloudy, WeatherType.Snow };
            return (WeatherType[])Enum.GetValues(typeof(WeatherType));
        }

        private void ApplyWeatherImmediate(WeatherType type)
        {
            int idx = (int)type;
            if (sunLight != null)
                sunLight.intensity = SunIntensities[idx];
            RenderSettings.fogDensity = FogDensities[idx];
            UpdateParticles(type);
        }

        private IEnumerator TransitionWeather(WeatherType type)
        {
            int idx = (int)type;
            float targetSun = SunIntensities[idx];
            float targetFog = FogDensities[idx];

            float startSun = sunLight != null ? sunLight.intensity : 1f;
            float startFog = RenderSettings.fogDensity;
            float elapsed = 0f;

            UpdateParticles(type);

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                if (sunLight != null)
                    sunLight.intensity = Mathf.Lerp(startSun, targetSun, t);
                RenderSettings.fogDensity = Mathf.Lerp(startFog, targetFog, t);

                yield return null;
            }

            if (sunLight != null)
                sunLight.intensity = targetSun;
            RenderSettings.fogDensity = targetFog;
        }

        private void UpdateParticles(WeatherType type)
        {
            bool rain = type == WeatherType.Rain || type == WeatherType.Storm;
            bool snow = type == WeatherType.Snow;

            if (rainParticles != null)
            {
                if (rain) rainParticles.Play();
                else      rainParticles.Stop();
            }

            if (snowParticles != null)
            {
                if (snow) snowParticles.Play();
                else      snowParticles.Stop();
            }
        }
    }
}
