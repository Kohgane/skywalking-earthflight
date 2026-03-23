using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Ocean
{
    /// <summary>
    /// Phase 50 — Bridges the SWEF Weather system with the Ocean system.
    ///
    /// <para>Responsibilities:
    /// <list type="bullet">
    ///   <item>Reads wind from <c>SWEF.Weather.WeatherManager</c> each frame and forwards it
    ///         to <see cref="OceanManager.SetWindParameters"/>.</item>
    ///   <item>Maps <c>WeatherType</c> → Beaufort sea state and transitions wave parameters
    ///         smoothly over <see cref="seaStateTransitionDuration"/> seconds.</item>
    ///   <item>Rain ripple effect: raises a shader parameter when it is raining.</item>
    ///   <item>Fog: adjusts ocean material transparency based on fog density.</item>
    ///   <item>Lightning: triggers a brief reflection flash on the water material during
    ///         thunderstorms.</item>
    ///   <item>Time-of-day colour adjustments (golden hour, night, moonlight).</item>
    ///   <item>Season ice-formation hint (visual only).</item>
    /// </list>
    /// </para>
    /// </summary>
    public class OceanWeatherIntegrator : MonoBehaviour
    {
        #region Constants

        private const float RainRippleMaxStrength = 0.6f;
        private const float LightningFlashDuration = 0.12f;

        private static readonly int ShaderPropRainRipple      = Shader.PropertyToID("_RainRippleStrength");
        private static readonly int ShaderPropLightningFlash  = Shader.PropertyToID("_LightningFlash");
        private static readonly int ShaderPropIceBlend        = Shader.PropertyToID("_IceBlend");
        private static readonly int ShaderPropWaterTint       = Shader.PropertyToID("_WaterTint");

        #endregion

        #region Sea State Definitions

        /// <summary>Describes a discrete sea state (wave parameters at a given Beaufort level).</summary>
        [Serializable]
        public class SeaState
        {
            public string label;
            [Range(0f, 20f)]  public float amplitude;
            [Range(0f, 2f)]   public float frequency;
            [Range(0f, 10f)]  public float speed;
            [Range(0f, 1f)]   public float steepness;
            [Range(0f, 1f)]   public float stormBlend;
        }

        #endregion

        #region Inspector

        [Header("Sea States")]
        [Tooltip("Ordered from calm (index 0) to storm (index N). Interpolated at runtime.")]
        [SerializeField] private SeaState[] seaStates = DefaultSeaStates();

        [Tooltip("Duration in seconds to interpolate between sea states.")]
        [SerializeField, Min(0.1f)] private float seaStateTransitionDuration = 10f;

        [Header("Rain")]
        [Tooltip("Rain ripple effect intensity at maximum rain intensity.")]
        [SerializeField, Range(0f, 1f)] private float maxRainRippleStrength = RainRippleMaxStrength;

        [Header("Time-of-Day")]
        [Tooltip("Water colour tint gradient over 24 hours (X = normalised hour 0–1).")]
        [SerializeField] private Gradient timeOfDayWaterTint;

        [Header("Season")]
        [Tooltip("Ice blend factor driven by the season controller (0 = summer, 1 = deep winter).")]
        [SerializeField, Range(0f, 1f)] private float iceBlend = 0f;

        [Header("Material")]
        [Tooltip("Ocean surface material that receives weather shader parameters.")]
        [SerializeField] private Material oceanMaterial;

        [Header("References")]
        [SerializeField] private OceanManager   oceanManager;
        [SerializeField] private WaveSimulator  waveSimulator;

        #endregion

        #region Events

        /// <summary>Fired whenever the sea state index changes.</summary>
        public event Action<int> OnSeaStateChanged;

        #endregion

        #region Private State

        private OceanManager   _mgr;
        private WaveSimulator  _waveSim;

        // Sea state animation.
        private int       _currentSeaStateIndex;
        private int       _targetSeaStateIndex;
        private float     _seaStateBlend = 1f;
        private Coroutine _seaStateCoroutine;

        // Lightning flash state.
        private bool      _lightningPending;
        private float     _lightningTime;
        private float     _nextLightningCheckTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _mgr     = oceanManager  != null ? oceanManager  : FindFirstObjectByType<OceanManager>();
            _waveSim = waveSimulator != null ? waveSimulator : FindFirstObjectByType<WaveSimulator>();
        }

        private void Update()
        {
            ReadWeatherSystem();
            UploadTimeOfDay();
            UploadIceBlend();
            UpdateLightningFlash();
        }

        #endregion

        #region Weather Reading

        private void ReadWeatherSystem()
        {
            // Try to read from SWEF.Weather.WeatherManager (namespace-safe reflection-free access).
#if SWEF_WEATHER_AVAILABLE
            var wm = Weather.WeatherManager.Instance;
            if (wm == null) return;

            var weather = wm.CurrentWeather;
            var wind    = wm.CurrentWind;

            // Forward wind.
            _mgr?.SetWindParameters(wind.speed, wind.windDirection);

            // Map weather type to sea-state index.
            int targetIdx = WeatherTypeToSeaStateIndex(weather.type, weather.intensity);
            if (targetIdx != _targetSeaStateIndex)
                TransitionToSeaState(targetIdx);

            // Rain ripple.
            float rainRipple = 0f;
            if (weather.type == Weather.WeatherType.Rain ||
                weather.type == Weather.WeatherType.HeavyRain ||
                weather.type == Weather.WeatherType.Drizzle)
                rainRipple = weather.intensity * maxRainRippleStrength;

            if (oceanMaterial != null)
                oceanMaterial.SetFloat(ShaderPropRainRipple, rainRipple);

            // Lightning flash.
            if (weather.type == Weather.WeatherType.Thunderstorm && !_lightningPending)
            {
                if (Time.time >= _nextLightningCheckTime)
                {
                    // Randomize next check interval (5–30 seconds) to avoid per-frame cost.
                    _nextLightningCheckTime = Time.time + Random.Range(5f, 30f);
                    if (Random.value < 0.5f)
                        _lightningPending = true;
                }
            }
#else
            // Standalone mode: no Weather system available — apply defaults.
            if (oceanMaterial != null)
                oceanMaterial.SetFloat(ShaderPropRainRipple, 0f);
#endif
        }

        #endregion

        #region Sea State Transitions

        private void TransitionToSeaState(int index)
        {
            if (index == _targetSeaStateIndex) return;
            _targetSeaStateIndex = Mathf.Clamp(index, 0, seaStates.Length - 1);

            if (_seaStateCoroutine != null)
                StopCoroutine(_seaStateCoroutine);
            _seaStateCoroutine = StartCoroutine(SeaStateTransitionRoutine());

            OnSeaStateChanged?.Invoke(_targetSeaStateIndex);
        }

        private IEnumerator SeaStateTransitionRoutine()
        {
            var from = seaStates[_currentSeaStateIndex];
            var to   = seaStates[_targetSeaStateIndex];
            float elapsed = 0f;

            while (elapsed < seaStateTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / seaStateTransitionDuration;

                if (_waveSim != null)
                {
                    _waveSim.WaveParameters.amplitude = Mathf.Lerp(from.amplitude,  to.amplitude,  t);
                    _waveSim.WaveParameters.frequency = Mathf.Lerp(from.frequency,  to.frequency,  t);
                    _waveSim.WaveParameters.speed     = Mathf.Lerp(from.speed,      to.speed,      t);
                    _waveSim.WaveParameters.steepness = Mathf.Lerp(from.steepness,  to.steepness,  t);
                    _waveSim.StormBlend               = Mathf.Lerp(from.stormBlend, to.stormBlend, t);
                }

                yield return null;
            }

            _currentSeaStateIndex = _targetSeaStateIndex;
        }

        #endregion

        #region Time-of-Day Tint

        private void UploadTimeOfDay()
        {
            if (oceanMaterial == null || timeOfDayWaterTint == null) return;

            float tod = (Time.time % 86400f) / 86400f; // fallback when TimeOfDay system absent
            Color tint = timeOfDayWaterTint.Evaluate(tod);
            oceanMaterial.SetColor(ShaderPropWaterTint, tint);
        }

        #endregion

        #region Ice Blend

        private void UploadIceBlend()
        {
            if (oceanMaterial == null) return;
            oceanMaterial.SetFloat(ShaderPropIceBlend, iceBlend);
        }

        /// <summary>Sets the ice formation blend factor (0 = none, 1 = fully iced).</summary>
        public void SetIceBlend(float value)
        {
            iceBlend = Mathf.Clamp01(value);
        }

        #endregion

        #region Lightning Flash

        private void UpdateLightningFlash()
        {
            if (oceanMaterial == null) return;

            if (_lightningPending)
            {
                _lightningPending = false;
                _lightningTime    = Time.time;
            }

            float elapsed = Time.time - _lightningTime;
            float flash   = elapsed < LightningFlashDuration
                              ? 1f - (elapsed / LightningFlashDuration)
                              : 0f;
            oceanMaterial.SetFloat(ShaderPropLightningFlash, flash);
        }

        #endregion

        #region Helpers

#if SWEF_WEATHER_AVAILABLE
        private int WeatherTypeToSeaStateIndex(Weather.WeatherType type, float intensity)
        {
            switch (type)
            {
                case Weather.WeatherType.Clear:
                case Weather.WeatherType.Cloudy:
                    return 0;
                case Weather.WeatherType.Overcast:
                case Weather.WeatherType.Mist:
                    return 1;
                case Weather.WeatherType.Rain:
                case Weather.WeatherType.Drizzle:
                case Weather.WeatherType.Fog:
                    return 2;
                case Weather.WeatherType.HeavyRain:
                case Weather.WeatherType.Hail:
                case Weather.WeatherType.DenseFog:
                    return Mathf.RoundToInt(Mathf.Lerp(2, 3, intensity));
                case Weather.WeatherType.Thunderstorm:
                case Weather.WeatherType.Sandstorm:
                    return seaStates.Length - 1;
                default:
                    return 0;
            }
        }
#endif

        private static SeaState[] DefaultSeaStates()
        {
            return new[]
            {
                new SeaState { label = "Calm",     amplitude = 0.1f, frequency = 0.05f, speed = 0.5f,  steepness = 0.2f, stormBlend = 0f   },
                new SeaState { label = "Moderate", amplitude = 0.5f, frequency = 0.10f, speed = 1.5f,  steepness = 0.4f, stormBlend = 0.1f },
                new SeaState { label = "Rough",    amplitude = 1.5f, frequency = 0.15f, speed = 3.0f,  steepness = 0.6f, stormBlend = 0.4f },
                new SeaState { label = "Storm",    amplitude = 4.0f, frequency = 0.20f, speed = 6.0f,  steepness = 0.8f, stormBlend = 1.0f },
            };
        }

        #endregion
    }
}
