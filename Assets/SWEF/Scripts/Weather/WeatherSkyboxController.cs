using UnityEngine;

namespace SWEF.Weather
{
    /// <summary>
    /// Adjusts the skybox, ambient lighting, and the scene's main directional light
    /// to reflect the current weather state and altitude.
    ///
    /// <para>Works with Unity's URP lighting pipeline.  Assign the main
    /// <see cref="Light"/> (directional sun light) in the Inspector so that storm
    /// conditions can dim it.  All skybox property changes are made through
    /// <see cref="RenderSettings"/> and the material set on <c>RenderSettings.skybox</c>.</para>
    ///
    /// <para>Auto-subscribes to <see cref="WeatherStateManager.OnWeatherStateUpdated"/>.</para>
    /// </summary>
    public class WeatherSkyboxController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherSkyboxController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Lighting")]
        [Tooltip("Scene main directional light (sun). Auto-resolved if left empty.")]
        [SerializeField] private Light sunLight;

        [Tooltip("Maximum sun light intensity under clear skies.")]
        [SerializeField] private float clearSunIntensity = 1.2f;

        [Tooltip("Minimum sun light intensity during heavy storms.")]
        [SerializeField] private float stormSunIntensity = 0.15f;

        [Header("Sky Tint")]
        [Tooltip("Property name on the skybox material for the tint/exposure multiplier.")]
        [SerializeField] private string skyboxExposureProperty = "_Exposure";

        [Tooltip("Sky exposure under clear conditions.")]
        [SerializeField] private float clearSkyExposure = 1.3f;

        [Tooltip("Sky exposure during heavy overcast / storms.")]
        [SerializeField] private float stormSkyExposure = 0.4f;

        [Header("Ambient Light")]
        [Tooltip("Ambient intensity multiplier under clear skies.")]
        [SerializeField] private float clearAmbientIntensity = 1.0f;

        [Tooltip("Ambient intensity multiplier during dense fog or storms.")]
        [SerializeField] private float fogAmbientIntensity = 0.5f;

        [Header("Altitude Transition")]
        [Tooltip("Above this altitude (metres) the sky always shows full clear-sky settings " +
                 "with a cloud layer hinted below.")]
        [SerializeField] private float aboveCloudAltitude = 10000f;

        [Header("Snow Horizon")]
        [Tooltip("Fog colour blended toward white during snowfall.")]
        [SerializeField] private Color snowHorizonColor = new Color(0.9f, 0.95f, 1f);

        [Header("Smooth Speed")]
        [Tooltip("Speed at which lighting values interpolate toward their targets (units per second).")]
        [SerializeField] private float lerpSpeed = 2f;

        // ── Internal ──────────────────────────────────────────────────────────────
        private float _targetSunIntensity;
        private float _targetSkyExposure;
        private float _targetAmbient;
        private Color _targetFogColor;
        private bool  _fogEnabled;

        private Color _defaultFogColor;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (sunLight == null)
                sunLight = FindFirstObjectByType<Light>();

            _defaultFogColor    = RenderSettings.fogColor;
            _targetSunIntensity = clearSunIntensity;
            _targetSkyExposure  = clearSkyExposure;
            _targetAmbient      = clearAmbientIntensity;
            _targetFogColor     = _defaultFogColor;
        }

        private void Start()
        {
            if (WeatherStateManager.Instance != null)
                WeatherStateManager.Instance.OnWeatherStateUpdated += OnWeatherUpdated;
        }

        private void OnDestroy()
        {
            if (WeatherStateManager.Instance != null)
                WeatherStateManager.Instance.OnWeatherStateUpdated -= OnWeatherUpdated;
        }

        private void Update()
        {
            float dt = Time.deltaTime * lerpSpeed;

            // Smoothly interpolate sun intensity
            if (sunLight != null)
                sunLight.intensity = Mathf.Lerp(sunLight.intensity, _targetSunIntensity, dt);

            // Ambient lighting
            RenderSettings.ambientIntensity =
                Mathf.Lerp(RenderSettings.ambientIntensity, _targetAmbient, dt);

            // Fog colour
            if (_fogEnabled)
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, _targetFogColor, dt);

            // Skybox exposure
            if (RenderSettings.skybox != null &&
                RenderSettings.skybox.HasProperty(skyboxExposureProperty))
            {
                float current = RenderSettings.skybox.GetFloat(skyboxExposureProperty);
                RenderSettings.skybox.SetFloat(
                    skyboxExposureProperty,
                    Mathf.Lerp(current, _targetSkyExposure, dt));
            }
        }

        // ── Event handler ─────────────────────────────────────────────────────────

        private void OnWeatherUpdated(WeatherData data)
        {
            float alt = WeatherStateManager.Instance != null
                ? WeatherStateManager.Instance.AltitudeMeters : 0f;

            if (alt >= aboveCloudAltitude)
            {
                // Above clouds: force clear sky
                _targetSunIntensity = clearSunIntensity;
                _targetSkyExposure  = clearSkyExposure;
                _targetAmbient      = clearAmbientIntensity;
                _targetFogColor     = _defaultFogColor;
                _fogEnabled         = false;
                RenderSettings.fog  = false;
                return;
            }

            // Sun intensity scales with cloud/storm coverage
            float stormFactor = GetStormFactor(data);
            _targetSunIntensity = Mathf.Lerp(clearSunIntensity, stormSunIntensity, stormFactor);

            // Sky exposure
            _targetSkyExposure = Mathf.Lerp(clearSkyExposure, stormSkyExposure,
                data.cloudCoverage * 0.6f + stormFactor * 0.4f);

            // Ambient
            bool foggy = data.condition == WeatherCondition.Fog ||
                         data.condition == WeatherCondition.DenseFog;
            _targetAmbient = foggy
                ? Mathf.Lerp(clearAmbientIntensity, fogAmbientIntensity, 0.8f)
                : Mathf.Lerp(clearAmbientIntensity, fogAmbientIntensity, data.cloudCoverage * 0.5f);

            // Fog colour (whiten during snow)
            bool snowing = data.condition == WeatherCondition.Snow  ||
                           data.condition == WeatherCondition.HeavySnow;
            _targetFogColor = snowing
                ? Color.Lerp(_defaultFogColor, snowHorizonColor, data.precipitationIntensity)
                : _defaultFogColor;
            _fogEnabled = foggy || snowing;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static float GetStormFactor(WeatherData data)
        {
            return data.condition switch
            {
                WeatherCondition.Thunderstorm => 0.9f,
                WeatherCondition.HeavyRain    => 0.7f,
                WeatherCondition.HeavySnow    => 0.6f,
                WeatherCondition.Overcast     => 0.5f,
                WeatherCondition.Rain         => 0.4f,
                WeatherCondition.Sandstorm    => 0.6f,
                WeatherCondition.Hail         => 0.75f,
                WeatherCondition.DenseFog     => 0.5f,
                WeatherCondition.Fog          => 0.25f,
                WeatherCondition.Cloudy       => 0.2f,
                _                             => 0f
            };
        }
    }
}
