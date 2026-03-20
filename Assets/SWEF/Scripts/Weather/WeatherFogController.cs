using UnityEngine;
using SWEF.Atmosphere;

namespace SWEF.Weather
{
    /// <summary>
    /// Dynamically adjusts scene fog parameters based on <see cref="WeatherConditionData.visibility"/>
    /// and weather type.
    ///
    /// <para>Integrates with <see cref="AtmosphereController"/> by reading
    /// <see cref="AtmosphereController.BaseFogDensity"/> and
    /// <see cref="AtmosphereController.BaseFogColor"/> as baselines, then calling
    /// <see cref="AtmosphereController.SetWeatherOverride"/> to apply weather fog
    /// on top of altitude-based fog.  Call <see cref="AtmosphereController.ClearWeatherOverride"/>
    /// when weather clears.</para>
    /// </summary>
    public class WeatherFogController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherFogController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private AtmosphereController atmosphereController;

        [Header("Fog Parameters")]
        [Tooltip("Speed at which fog density and colour transition between states.")]
        [SerializeField] private float fogTransitionSpeed = 2f;

        [Tooltip("Fog density when visibility is at maximum (50 km = clear sky).")]
        [SerializeField] private float clearFogDensity = 0.0f;

        [Tooltip("Fog density for dense fog (visibility ≤ 100 m).")]
        [SerializeField] private float denseFogDensity = 0.08f;

        [Header("Fog Colours")]
        [SerializeField] private Color rainFogColor       = new Color(0.6f, 0.65f, 0.7f);
        [SerializeField] private Color snowFogColor       = new Color(0.9f, 0.92f, 0.95f);
        [SerializeField] private Color sandstormFogColor  = new Color(0.85f, 0.75f, 0.4f);
        [SerializeField] private Color defaultFogColor    = new Color(0.7f, 0.75f, 0.8f);

        // ── Internal ──────────────────────────────────────────────────────────────
        private float _currentDensity;
        private Color _currentColor;
        private float _targetDensity;
        private Color _targetColor;
        private bool  _weatherFogActive;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (atmosphereController == null)
                atmosphereController = FindFirstObjectByType<AtmosphereController>();

            _currentDensity = RenderSettings.fogDensity;
            _currentColor   = RenderSettings.fogColor;
            _targetDensity  = _currentDensity;
            _targetColor    = _currentColor;
        }

        private void Update()
        {
            float dt = Time.deltaTime * fogTransitionSpeed;
            _currentDensity = Mathf.Lerp(_currentDensity, _targetDensity, dt);
            _currentColor   = Color.Lerp(_currentColor,   _targetColor,   dt);

            if (_weatherFogActive)
            {
                if (atmosphereController != null)
                    atmosphereController.SetWeatherOverride(_currentDensity, _currentColor);
                else
                {
                    RenderSettings.fog        = true;
                    RenderSettings.fogDensity = _currentDensity;
                    RenderSettings.fogColor   = _currentColor;
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Recalculates fog targets from the given weather condition.
        /// Called by <see cref="WeatherManager"/> during transitions.
        /// </summary>
        public void ApplyWeather(WeatherConditionData condition)
        {
            _targetDensity     = VisibilityToDensity(condition.visibility);
            _targetColor       = SelectFogColor(condition);
            _weatherFogActive  = condition.type != WeatherType.Clear;

            if (!_weatherFogActive && atmosphereController != null)
                atmosphereController.ClearWeatherOverride();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private float VisibilityToDensity(float visibilityMeters)
        {
            // Map visibility range [100 m, 50 000 m] to density [denseFogDensity, clearFogDensity]
            float t = Mathf.InverseLerp(100f, 50000f, Mathf.Clamp(visibilityMeters, 100f, 50000f));
            return Mathf.Lerp(denseFogDensity, clearFogDensity, t);
        }

        private Color SelectFogColor(WeatherConditionData c)
        {
            return c.type switch
            {
                WeatherType.Snow      or WeatherType.HeavySnow => snowFogColor,
                WeatherType.Sandstorm                          => sandstormFogColor,
                WeatherType.Fog       or WeatherType.DenseFog  or
                WeatherType.Mist      or WeatherType.Drizzle   or
                WeatherType.Rain      or WeatherType.HeavyRain or
                WeatherType.Thunderstorm or WeatherType.Sleet  => rainFogColor,
                _                                              => defaultFogColor
            };
        }
    }
}
