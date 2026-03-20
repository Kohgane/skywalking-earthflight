using System.Collections;
using UnityEngine;

namespace SWEF.Weather
{
    /// <summary>
    /// Adjusts sun light intensity, ambient light, and triggers lightning flashes
    /// based on current weather conditions.
    ///
    /// <para>Designed to complement <see cref="WeatherSkyboxController"/>.
    /// Both can coexist: this component owns the sun <see cref="Light"/> intensity
    /// and ambient multiplier; <see cref="WeatherSkyboxController"/> owns the skybox
    /// material and fog colour.</para>
    /// </summary>
    public class WeatherLightingController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherLightingController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Sun Light")]
        [Tooltip("Scene directional light (sun). Auto-resolved if left empty.")]
        [SerializeField] private Light sunLight;

        [Tooltip("Sun intensity under clear skies.")]
        [SerializeField] private float clearSunIntensity = 1.2f;

        [Tooltip("Sun intensity during heavy overcast.")]
        [SerializeField] private float overcastSunIntensity = 0.4f;

        [Tooltip("Sun intensity during thunderstorm.")]
        [SerializeField] private float thunderstormSunIntensity = 0.3f;

        [Header("Sun Colour")]
        [Tooltip("Colour ramp from clear (time 0) to overcast (time 1).")]
        [SerializeField] private Gradient clearToOvercastGradient = DefaultGradient();

        [Header("Ambient Light")]
        [Tooltip("Ambient intensity multiplier under clear skies.")]
        [SerializeField] private float clearAmbientIntensity = 1.0f;

        [Tooltip("Ambient intensity multiplier during dense cloud cover.")]
        [SerializeField] private float overcastAmbientIntensity = 0.5f;

        [Header("Lightning")]
        [Tooltip("Peak intensity of the sun-light flash during a lightning strike.")]
        [SerializeField] private float lightningFlashIntensity = 3f;

        [Tooltip("Duration of the initial bright flash in seconds.")]
        [SerializeField] private float lightningFlashDuration = 0.1f;

        [Tooltip("Duration of the after-glow fade in seconds.")]
        [SerializeField] private float lightningAfterglow = 0.3f;

        [Tooltip("Minimum and maximum seconds between lightning flashes during a thunderstorm.")]
        [SerializeField] private Vector2 lightningIntervalRange = new Vector2(5f, 15f);

        [Header("Transition")]
        [Tooltip("Speed at which lighting values interpolate toward their targets.")]
        [SerializeField] private float lerpSpeed = 2f;

        // ── Internal ──────────────────────────────────────────────────────────────
        private float  _targetSunIntensity;
        private Color  _targetSunColor;
        private float  _targetAmbient;
        private bool   _thunderstormActive;
        private Coroutine _lightningCoroutine;
        private float  _baseSunIntensity;   // tracks non-flash intensity for lerping

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (sunLight == null)
                sunLight = FindFirstObjectByType<Light>();

            _targetSunIntensity = clearSunIntensity;
            _targetSunColor     = clearToOvercastGradient?.Evaluate(0f) ?? Color.white;
            _targetAmbient      = clearAmbientIntensity;
            _baseSunIntensity   = clearSunIntensity;
        }

        private void Update()
        {
            float dt = Time.deltaTime * lerpSpeed;

            if (sunLight != null)
            {
                // Only lerp toward base intensity if no lightning coroutine is running
                if (_lightningCoroutine == null)
                    sunLight.intensity = Mathf.Lerp(sunLight.intensity, _targetSunIntensity, dt);

                sunLight.color = Color.Lerp(sunLight.color, _targetSunColor, dt);
            }

            RenderSettings.ambientIntensity =
                Mathf.Lerp(RenderSettings.ambientIntensity, _targetAmbient, dt);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates lighting targets from the given weather condition.
        /// Called by <see cref="WeatherManager"/> during transitions.
        /// </summary>
        public void ApplyWeather(WeatherConditionData condition)
        {
            float cloudT = condition.cloudCover;

            // Sun intensity
            _baseSunIntensity = condition.type switch
            {
                WeatherType.Thunderstorm => thunderstormSunIntensity,
                WeatherType.Overcast or WeatherType.HeavyRain or WeatherType.HeavySnow =>
                    Mathf.Lerp(overcastSunIntensity, thunderstormSunIntensity, condition.intensity * 0.5f),
                _ => Mathf.Lerp(clearSunIntensity, overcastSunIntensity, cloudT)
            };
            _targetSunIntensity = _baseSunIntensity;

            // Sun colour
            _targetSunColor = clearToOvercastGradient?.Evaluate(cloudT) ?? Color.white;

            // Ambient
            _targetAmbient = Mathf.Lerp(clearAmbientIntensity, overcastAmbientIntensity, cloudT);

            // Lightning
            bool isThunderstorm = condition.type == WeatherType.Thunderstorm;
            if (isThunderstorm && !_thunderstormActive)
            {
                _thunderstormActive = true;
                _lightningCoroutine = StartCoroutine(LightningLoop());
            }
            else if (!isThunderstorm && _thunderstormActive)
            {
                _thunderstormActive = false;
                if (_lightningCoroutine != null)
                {
                    StopCoroutine(_lightningCoroutine);
                    _lightningCoroutine = null;
                }
            }
        }

        // ── Coroutines ────────────────────────────────────────────────────────────

        private IEnumerator LightningLoop()
        {
            while (_thunderstormActive)
            {
                float delay = Random.Range(lightningIntervalRange.x, lightningIntervalRange.y);
                yield return new WaitForSeconds(delay);

                if (!_thunderstormActive) yield break;

                yield return StartCoroutine(LightningFlash());
            }
        }

        private IEnumerator LightningFlash()
        {
            if (sunLight == null) yield break;

            // Bright flash
            sunLight.intensity = lightningFlashIntensity;
            yield return new WaitForSeconds(lightningFlashDuration);

            // Afterglow fade back to base
            float elapsed = 0f;
            float startIntensity = sunLight.intensity;
            while (elapsed < lightningAfterglow)
            {
                elapsed           += Time.deltaTime;
                sunLight.intensity = Mathf.Lerp(startIntensity, _baseSunIntensity, elapsed / lightningAfterglow);
                yield return null;
            }
            sunLight.intensity = _baseSunIntensity;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static Gradient DefaultGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.97f, 0.9f), 0f),    // clear — warm white
                        new GradientColorKey(new Color(0.7f, 0.75f, 0.8f), 1f) }, // overcast — cool grey
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            return g;
        }
    }
}
