using System;
using UnityEngine;

namespace SWEF.Atmosphere
{
    /// <summary>
    /// Simulates wind that affects the player's flight.
    /// Wind strength scales with the current <see cref="WeatherController.WeatherType"/>.
    /// Direction changes slowly over time using Perlin noise, with random gust bursts.
    /// Above 20,000 m altitude there is no wind.
    /// </summary>
    public class WindController : MonoBehaviour
    {
        [SerializeField] private Flight.FlightController flight;
        [SerializeField] private WeatherController weather;

        [Header("Wind Settings")]
        [SerializeField] private float maxWindForce = 5f;
        [SerializeField] private float gustIntervalMin = 3f;
        [SerializeField] private float gustIntervalMax = 10f;
        [SerializeField] private float gustDuration = 1.5f;

        // Wind strength multipliers per WeatherType index (Clear, Cloudy, Rain, Storm, Snow)
        private static readonly float[] WeatherWindScale = { 0.1f, 0.3f, 0.6f, 1.0f, 0.4f };

        /// <summary>Raised when wind direction or strength changes significantly.</summary>
        public event Action<Vector3, float> OnWindChanged;

        /// <summary>Current normalized wind direction on the XZ plane.</summary>
        public Vector3 CurrentWindDirection { get; private set; } = Vector3.right;

        /// <summary>Current wind strength in m/s (0–maxWindForce).</summary>
        public float CurrentWindStrength { get; private set; }

        private float _noiseOffset;
        private float _gustTimer;
        private float _nextGustTime;
        private float _gustElapsed;
        private bool _gustActive;
        private float _baseStrength;

        private void Awake()
        {
            if (flight == null)
                flight = FindFirstObjectByType<Flight.FlightController>();
            if (weather == null)
                weather = FindFirstObjectByType<WeatherController>();

            if (weather != null)
                weather.OnWeatherChanged += OnWeatherChanged;

            _noiseOffset = UnityEngine.Random.value * 1000f;
            ScheduleNextGust();
            UpdateBaseStrength();
        }

        private void OnDestroy()
        {
            if (weather != null)
                weather.OnWeatherChanged -= OnWeatherChanged;
        }

        private void Update()
        {
            float altitude = GetAltitude();

            // No wind above 20,000 m
            if (altitude >= 20000f)
            {
                CurrentWindStrength = 0f;
                return;
            }

            // Update Perlin-noise-based direction
            float noiseX = Mathf.PerlinNoise(_noiseOffset + Time.time * 0.05f, 0f) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(0f, _noiseOffset + Time.time * 0.05f) * 2f - 1f;
            Vector3 rawDir = new Vector3(noiseX, 0f, noiseZ);
            CurrentWindDirection = rawDir.sqrMagnitude > 0.0001f ? rawDir.normalized : Vector3.right;

            // Gust logic
            float gustMultiplier = 1f;
            _gustTimer += Time.deltaTime;
            if (!_gustActive && _gustTimer >= _nextGustTime)
            {
                _gustActive = true;
                _gustElapsed = 0f;
            }

            if (_gustActive)
            {
                _gustElapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(_gustElapsed / gustDuration);
                gustMultiplier = 1f + Mathf.SmoothStep(0f, 1f, progress < 0.5f ? progress * 2f : (1f - progress) * 2f);

                if (_gustElapsed >= gustDuration)
                {
                    _gustActive = false;
                    ScheduleNextGust();
                }
            }

            CurrentWindStrength = _baseStrength * gustMultiplier;

            // Apply to player transform
            if (flight != null)
            {
                Vector3 windForce = CurrentWindDirection * CurrentWindStrength;
                flight.transform.position += windForce * Time.deltaTime;
            }

            OnWindChanged?.Invoke(CurrentWindDirection, CurrentWindStrength);
        }

        private void OnWeatherChanged(WeatherController.WeatherType type)
        {
            UpdateBaseStrength();
        }

        private void UpdateBaseStrength()
        {
            WeatherController.WeatherType type = weather != null
                ? weather.CurrentWeather
                : WeatherController.WeatherType.Clear;
            _baseStrength = maxWindForce * WeatherWindScale[(int)type];
        }

        private float GetAltitude()
        {
            if (flight == null) return 0f;
            var altCtrl = flight.GetComponent<Flight.AltitudeController>();
            return altCtrl != null ? altCtrl.CurrentAltitudeMeters : 0f;
        }

        private void ScheduleNextGust()
        {
            _gustTimer = 0f;
            _nextGustTime = UnityEngine.Random.Range(gustIntervalMin, gustIntervalMax);
        }
    }
}
