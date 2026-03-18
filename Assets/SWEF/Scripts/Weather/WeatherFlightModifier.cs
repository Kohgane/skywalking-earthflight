using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Weather
{
    /// <summary>
    /// Modifies flight behaviour based on current weather conditions.
    ///
    /// <para>Integrates with <see cref="SWEF.Flight.FlightController"/> by applying
    /// external force vectors and control-responsiveness multipliers rather than
    /// overriding the controller's core logic.</para>
    ///
    /// <para>Listens to <see cref="WeatherStateManager.OnWeatherStateUpdated"/> to keep
    /// its internal state up to date.</para>
    /// </summary>
    public class WeatherFlightModifier : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherFlightModifier Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Wind")]
        [Tooltip("Scale factor applied to the raw wind vector before adding to player velocity.")]
        [SerializeField] private float windForceScale = 0.3f;

        [Header("Turbulence")]
        [Tooltip("Maximum positional shake offset (metres) at full turbulence intensity.")]
        [SerializeField] private float turbulenceMaxOffset = 2.5f;

        [Tooltip("Frequency of turbulence noise (higher = faster shake).")]
        [SerializeField] private float turbulenceFrequency = 3f;

        [Header("Icing")]
        [Tooltip("Altitude above which icing can form (metres).")]
        [SerializeField] private float icingAltitudeMin = 3000f;

        [Tooltip("Temperature threshold below which icing accumulates (°C).")]
        [SerializeField] private float icingTemperatureThreshold = -5f;

        [Tooltip("Rate at which icing builds up (fraction per second).")]
        [SerializeField] private float icingAccumulationRate = 0.03f;

        [Tooltip("Rate at which icing melts once conditions are no longer met (fraction per second).")]
        [SerializeField] private float icingMeltRate = 0.08f;

        [Tooltip("Minimum control-responsiveness while fully iced (0 = uncontrollable, 1 = no effect).")]
        [SerializeField] private float icingMinResponsiveness = 0.4f;

        [Header("Thermals")]
        [Tooltip("Maximum upward force from thermals (m/s²).")]
        [SerializeField] private float thermalMaxForce = 3f;

        [Tooltip("Altitude ceiling for thermals (metres).")]
        [SerializeField] private float thermalMaxAltitude = 3000f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>
        /// Raised when turbulence spikes above a noticeable threshold.
        /// The float argument is turbulence intensity (0–1).
        /// Subscribe from <c>HapticFeedbackController</c> or audio triggers.
        /// </summary>
        public event Action<float> OnTurbulenceEvent;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Wind force vector in world-space (m/s), ready to be added to player velocity.</summary>
        public Vector3 WindForce           { get; private set; }

        /// <summary>Current turbulence intensity (0 = calm, 1 = violent).</summary>
        public float TurbulenceIntensity   { get; private set; }

        /// <summary>Visibility multiplier for the camera far-clip and fog (0 = zero visibility, 1 = clear).</summary>
        public float VisibilityMultiplier  { get; private set; } = 1f;

        /// <summary>Control responsiveness (0 = uncontrollable due to icing, 1 = full).</summary>
        public float ControlResponsiveness { get; private set; } = 1f;

        // ── Internal ──────────────────────────────────────────────────────────────
        private WeatherData _currentData;
        private float       _icingLevel;          // 0–1
        private float       _turbulenceTimer;
        private bool        _turbulenceEventFired;
        private const float TurbulenceEventThreshold = 0.5f;
        private const float MaxVisibility             = 10000f;

        // Cached transform for turbulence shake
        private Vector3 _basePosition;
        private bool    _basePositionSet;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _currentData = WeatherData.CreateClear();
        }

        private void Start()
        {
            if (WeatherStateManager.Instance != null)
                WeatherStateManager.Instance.OnWeatherStateUpdated += UpdateFromWeather;
        }

        private void OnDestroy()
        {
            if (WeatherStateManager.Instance != null)
                WeatherStateManager.Instance.OnWeatherStateUpdated -= UpdateFromWeather;
        }

        private void Update()
        {
            UpdateIcing();
            UpdateTurbulence();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies weather-based external forces to the given <see cref="SWEF.Flight.FlightController"/>.
        /// Call this from <c>FlightController.Step()</c> or an equivalent update hook.
        /// </summary>
        /// <param name="fc">The flight controller whose transform will receive wind / thermal forces.</param>
        public void ApplyToFlightController(SWEF.Flight.FlightController fc)
        {
            if (fc == null) return;

            float dt = Time.deltaTime;
            Vector3 totalForce = WindForce;

            // Thermals: upward force in suitable weather near the ground
            float alt = WeatherStateManager.Instance != null
                ? WeatherStateManager.Instance.AltitudeMeters : 0f;

            if (alt < thermalMaxAltitude &&
                (_currentData.condition == WeatherCondition.Clear ||
                 _currentData.condition == WeatherCondition.Cloudy))
            {
                float thermalNoise = Mathf.PerlinNoise(Time.time * 0.2f, 0f);
                float thermalFade  = 1f - (alt / thermalMaxAltitude);
                totalForce += Vector3.up * thermalNoise * thermalMaxForce * thermalFade;
            }

            // Turbulence shake
            if (TurbulenceIntensity > 0.05f)
            {
                float nx = Mathf.PerlinNoise(Time.time * turbulenceFrequency,       0f) - 0.5f;
                float ny = Mathf.PerlinNoise(Time.time * turbulenceFrequency + 10f, 0f) - 0.5f;
                float nz = Mathf.PerlinNoise(Time.time * turbulenceFrequency + 20f, 0f) - 0.5f;
                Vector3 turbulenceOffset = new Vector3(nx, ny, nz) * turbulenceMaxOffset * TurbulenceIntensity;
                fc.transform.position += turbulenceOffset * dt;
            }

            // Apply combined force as a position offset (kinematic controller)
            fc.transform.position += totalForce * dt;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void UpdateFromWeather(WeatherData data)
        {
            _currentData = data;

            // Wind force
            WindForce = data.WindVector * windForceScale;

            // Visibility
            VisibilityMultiplier = data.visibility >= MaxVisibility
                ? 1f
                : Mathf.Clamp01(data.visibility / MaxVisibility);

            // Turbulence base from weather severity
            TurbulenceIntensity = GetBaseTurbulence(data);
        }

        private void UpdateIcing()
        {
            float alt = WeatherStateManager.Instance != null
                ? WeatherStateManager.Instance.AltitudeMeters : 0f;

            bool icingConditions = alt > icingAltitudeMin &&
                                   _currentData.temperatureCelsius < icingTemperatureThreshold &&
                                   (_currentData.condition == WeatherCondition.Rain     ||
                                    _currentData.condition == WeatherCondition.HeavyRain ||
                                    _currentData.condition == WeatherCondition.Snow      ||
                                    _currentData.condition == WeatherCondition.HeavySnow ||
                                    _currentData.condition == WeatherCondition.Thunderstorm);

            if (icingConditions)
                _icingLevel = Mathf.Clamp01(_icingLevel + icingAccumulationRate * Time.deltaTime);
            else
                _icingLevel = Mathf.Clamp01(_icingLevel - icingMeltRate * Time.deltaTime);

            ControlResponsiveness = Mathf.Lerp(1f, icingMinResponsiveness, _icingLevel);
        }

        private void UpdateTurbulence()
        {
            // Turbulence intensity fluctuates randomly around the base value
            float noise = Mathf.PerlinNoise(Time.time * 0.5f, 42f);
            float base_ = GetBaseTurbulence(_currentData);
            TurbulenceIntensity = base_ * (0.7f + noise * 0.6f);

            if (TurbulenceIntensity >= TurbulenceEventThreshold && !_turbulenceEventFired)
            {
                _turbulenceEventFired = true;
                OnTurbulenceEvent?.Invoke(TurbulenceIntensity);
            }
            else if (TurbulenceIntensity < TurbulenceEventThreshold * 0.7f)
            {
                _turbulenceEventFired = false;
            }
        }

        private static float GetBaseTurbulence(WeatherData data)
        {
            return data.condition switch
            {
                WeatherCondition.Thunderstorm => 0.8f + data.precipitationIntensity * 0.2f,
                WeatherCondition.HeavyRain    => 0.5f + data.precipitationIntensity * 0.3f,
                WeatherCondition.Hail         => 0.7f,
                WeatherCondition.HeavySnow    => 0.3f,
                WeatherCondition.Sandstorm    => 0.6f,
                WeatherCondition.Windy        => 0.4f + data.windSpeedMs / 50f,
                WeatherCondition.Rain         => 0.2f,
                _                             => 0f
            };
        }
    }
}
