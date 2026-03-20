using System;
using System.Collections;
using UnityEngine;
using SWEF.Flight;

namespace SWEF.Weather
{
    /// <summary>
    /// Modifies flight behaviour based on current weather conditions.
    ///
    /// <para>Phase 32 additions: integrates with <see cref="WeatherManager"/> and
    /// <see cref="WindSystem"/> for wind push, turbulence shake, visibility-based speed
    /// reduction, and icing warnings.  Also retains Phase 9 compatibility with
    /// <see cref="WeatherStateManager"/> when the Phase 32 managers are absent.</para>
    ///
    /// <para>Integrates with <see cref="SWEF.Flight.FlightController"/> via
    /// <see cref="FlightController.ApplyExternalForce"/> and
    /// <see cref="FlightController.ExternalDragMultiplier"/>.</para>
    /// </summary>
    public class WeatherFlightModifier : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherFlightModifier Instance { get; private set; }

        // ── Inspector — Phase 32 ──────────────────────────────────────────────────
        [Header("Phase 32 — References")]
        [Tooltip("WeatherManager (Phase 32). Auto-found if null.")]
        [SerializeField] private WeatherManager weatherManager;

        [Tooltip("WindSystem (Phase 32). Auto-found if null.")]
        [SerializeField] private WindSystem windSystem;

        [Tooltip("FlightController reference. Auto-found if null.")]
        [SerializeField] private FlightController flightController;

        [Header("Phase 32 — Wind")]
        [Tooltip("Multiplier applied to WindSystem.CurrentWindForce before passing to FlightController.")]
        [SerializeField] private float windForceMultiplier = 1f;

        [Header("Phase 32 — Turbulence")]
        [Tooltip("Camera shake strength at full turbulence intensity (degrees rotation jitter).")]
        [SerializeField] private float turbulenceShakeStrength = 2f;

        [Header("Phase 32 — Visibility")]
        [Tooltip("When enabled, reduces FlightController max speed proportionally in low visibility.")]
        [SerializeField] private bool reduceSpeedInLowVisibility = true;

        [Tooltip("Minimum speed multiplier at zero visibility (e.g. 0.5 = half max speed in dense fog).")]
        [SerializeField] private float minSpeedMultiplierInFog = 0.5f;

        [Header("Phase 32 — Icing")]
        [Tooltip("Altitude above which icing can form (metres). Phase 32 spec: 2–8 km.")]
        [SerializeField] private float icingAltitudeMin = 2000f;

        [Tooltip("Altitude above which icing no longer forms (metres).")]
        [SerializeField] private float icingAltitudeMax = 8000f;

        [Tooltip("Temperature threshold below which icing is possible (°C).")]
        [SerializeField] private float icingTemperatureThreshold = 0f;

        [Tooltip("Minimum humidity for icing to occur.")]
        [SerializeField, Range(0f, 1f)] private float icingHumidityThreshold = 0.8f;

        // ── Inspector — Phase 9 compat ────────────────────────────────────────────
        [Header("Phase 9 Compat — Wind")]
        [Tooltip("Scale factor applied to the raw wind vector (Phase 9 path when WindSystem absent).")]
        [SerializeField] private float windForceScale = 0.3f;

        [Header("Phase 9 Compat — Turbulence")]
        [Tooltip("Maximum positional shake offset (metres) at full turbulence intensity.")]
        [SerializeField] private float turbulenceMaxOffset = 2.5f;

        [Tooltip("Frequency of turbulence noise (higher = faster shake).")]
        [SerializeField] private float turbulenceFrequency = 3f;

        [Header("Phase 9 Compat — Icing Detail")]
        [Tooltip("Rate at which icing builds up (fraction per second).")]
        [SerializeField] private float icingAccumulationRate = 0.03f;

        [Tooltip("Rate at which icing melts once conditions are no longer met (fraction per second).")]
        [SerializeField] private float icingMeltRate = 0.08f;

        [Tooltip("Minimum control-responsiveness while fully iced (0 = uncontrollable, 1 = no effect).")]
        [SerializeField] private float icingMinResponsiveness = 0.4f;

        [Header("Phase 9 Compat — Thermals")]
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

        /// <summary>
        /// Raised when icing conditions are detected (temperature &lt; threshold,
        /// humidity &gt; threshold, altitude within icing band).
        /// Subscribe from UI or warning systems.
        /// </summary>
        public event Action OnIcingWarning;

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
        private WeatherData _currentData;           // Phase 9 legacy data
        private WeatherConditionData _p32Weather;   // Phase 32 weather data
        private float       _icingLevel;            // 0–1
        private float       _turbulenceTimer;
        private bool        _turbulenceEventFired;
        private bool        _icingWarningFired;

        // Turbulence shake restore — avoids accumulating rotation jitter
        private Quaternion  _pendingRotationRestore;
        private bool        _hasPendingRotationRestore;

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
            _p32Weather  = WeatherConditionData.CreateClear();
        }

        private void Start()
        {
            // Phase 32 references
            if (weatherManager  == null) weatherManager  = FindFirstObjectByType<WeatherManager>();
            if (windSystem      == null) windSystem      = FindFirstObjectByType<WindSystem>();
            if (flightController == null) flightController = FindFirstObjectByType<FlightController>();

            if (weatherManager != null)
                weatherManager.OnWeatherChanged += HandleP32WeatherChange;

            // Phase 9 compat
            if (WeatherStateManager.Instance != null)
                WeatherStateManager.Instance.OnWeatherStateUpdated += UpdateFromWeather;
        }

        private void OnDestroy()
        {
            if (weatherManager != null)
                weatherManager.OnWeatherChanged -= HandleP32WeatherChange;

            if (WeatherStateManager.Instance != null)
                WeatherStateManager.Instance.OnWeatherStateUpdated -= UpdateFromWeather;
        }

        private void Update()
        {
            // Restore rotation from previous turbulence shake before computing new frame
            if (_hasPendingRotationRestore && flightController != null)
            {
                flightController.transform.rotation = _pendingRotationRestore;
                _hasPendingRotationRestore = false;
            }

            // Phase 32: apply wind force via FlightController.ApplyExternalForce
            if (flightController != null && windSystem != null)
            {
                Vector3 windForce = windSystem.CurrentWindForce * windForceMultiplier;
                flightController.ApplyExternalForce(windForce);

                // Turbulence shake: apply a temporary rotation offset and immediately
                // restore the original rotation so the jitter does not accumulate.
                float turb = windSystem.CurrentTurbulence;
                TurbulenceIntensity = turb;

                if (turb > 0.05f)
                {
                    float jitter = turb * turbulenceShakeStrength;
                    float nx = (Mathf.PerlinNoise(Time.time * turbulenceFrequency,       0f) - 0.5f) * jitter;
                    float nz = (Mathf.PerlinNoise(Time.time * turbulenceFrequency + 20f, 0f) - 0.5f) * jitter;

                    // Save and restore so shake doesn't accumulate into the flight rotation.
                    Quaternion originalRot = flightController.transform.rotation;
                    flightController.transform.Rotate(nx, 0f, nz, Space.Self);
                    // The visual offset is seen for this frame; next frame restores base rotation.
                    // Store restored rotation to be applied at start of next frame.
                    _pendingRotationRestore = originalRot;
                    _hasPendingRotationRestore = true;
                }
                else if (_hasPendingRotationRestore)
                {
                    flightController.transform.rotation = _pendingRotationRestore;
                    _hasPendingRotationRestore = false;
                }

                // Visibility-based drag
                if (reduceSpeedInLowVisibility)
                {
                    float vis = _p32Weather.visibility;
                    float speedMult = Mathf.Lerp(minSpeedMultiplierInFog, 1f,
                        Mathf.InverseLerp(100f, 10000f, vis));
                    flightController.ExternalDragMultiplier = speedMult;
                }
            }
            else
            {
                // Phase 9 fallback
                UpdateTurbulence();
            }

            UpdateIcing();
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

        private void HandleP32WeatherChange(WeatherConditionData condition)
        {
            _p32Weather = condition;
            VisibilityMultiplier = Mathf.Clamp01(condition.visibility / MaxVisibility);
        }

        private void UpdateFromWeather(WeatherData data)
        {
            _currentData = data;

            // Wind force (Phase 9 path)
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
            float alt = (float)SWEF.Core.SWEFSession.Alt;

            // Phase 32 icing logic (uses WeatherManager data when available)
            bool p32Icing = weatherManager != null &&
                            alt >= icingAltitudeMin && alt <= icingAltitudeMax &&
                            _p32Weather.temperature < icingTemperatureThreshold &&
                            _p32Weather.humidity    > icingHumidityThreshold;

            // Phase 9 icing logic (fallback)
            bool p9Icing = weatherManager == null &&
                           WeatherStateManager.Instance != null &&
                           alt > icingAltitudeMin &&
                           _currentData.temperatureCelsius < icingTemperatureThreshold &&
                           (_currentData.condition == WeatherCondition.Rain     ||
                            _currentData.condition == WeatherCondition.HeavyRain ||
                            _currentData.condition == WeatherCondition.Snow      ||
                            _currentData.condition == WeatherCondition.HeavySnow ||
                            _currentData.condition == WeatherCondition.Thunderstorm);

            bool icingConditions = p32Icing || p9Icing;

            if (icingConditions)
            {
                _icingLevel = Mathf.Clamp01(_icingLevel + icingAccumulationRate * Time.deltaTime);

                if (!_icingWarningFired)
                {
                    _icingWarningFired = true;
                    OnIcingWarning?.Invoke();
                }
            }
            else
            {
                _icingLevel        = Mathf.Clamp01(_icingLevel - icingMeltRate * Time.deltaTime);
                _icingWarningFired = false;
            }

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
