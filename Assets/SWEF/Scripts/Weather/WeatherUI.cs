using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Weather
{
    /// <summary>
    /// HUD panel displaying current weather conditions: type, temperature, wind, and visibility.
    ///
    /// <para>Updates once per second (not every frame) for performance.  Subscribes to
    /// <see cref="WeatherManager.OnWeatherChanged"/> and also polls on a 1-second timer
    /// for smooth numeric updates during transitions.</para>
    ///
    /// <para>All UI fields are optional — the component degrades gracefully when they
    /// are unassigned.</para>
    /// </summary>
    public class WeatherUI : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherUI Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private WeatherManager weatherManager;
        [SerializeField] private WeatherFlightModifier flightModifier;

        [Header("Text Fields")]
        [Tooltip("Shows weather type and temperature (e.g. 'Rain  12°C').")]
        [SerializeField] private Text weatherText;

        [Tooltip("Shows wind speed and direction (e.g. 'Wind: 15 m/s NW').")]
        [SerializeField] private Text windText;

        [Tooltip("Shows horizontal visibility in kilometres (e.g. 'Vis: 4.2 km').")]
        [SerializeField] private Text visibilityText;

        [Header("Icons")]
        [Tooltip("Image component to display the weather-type icon sprite.")]
        [SerializeField] private Image weatherIcon;

        [Tooltip("Sprites indexed by WeatherType enum value (array length must match enum count).")]
        [SerializeField] private Sprite[] weatherIcons;

        [Header("Icing Warning")]
        [Tooltip("Panel shown (and pulsed) when icing is detected.")]
        [SerializeField] private GameObject icingWarningPanel;

        [Tooltip("Flashing speed for the icing warning panel (on/off cycles per second).")]
        [SerializeField] private float icingFlashRate = 2f;

        [Header("Update Rate")]
        [Tooltip("Seconds between UI refreshes (lower = smoother but more expensive).")]
        [SerializeField] private float updateInterval = 1f;

        // ── Internal ──────────────────────────────────────────────────────────────
        private float _updateTimer;
        private bool  _icingActive;
        private float _icingFlashTimer;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (weatherManager   == null) weatherManager   = FindFirstObjectByType<WeatherManager>();
            if (flightModifier   == null) flightModifier   = FindFirstObjectByType<WeatherFlightModifier>();

            if (weatherManager != null)
                weatherManager.OnWeatherChanged += OnWeatherChanged;

            if (flightModifier != null)
                flightModifier.OnIcingWarning += OnIcingWarning;

            if (icingWarningPanel != null)
                icingWarningPanel.SetActive(false);

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (weatherManager != null)
                weatherManager.OnWeatherChanged -= OnWeatherChanged;

            if (flightModifier != null)
                flightModifier.OnIcingWarning -= OnIcingWarning;
        }

        private void Update()
        {
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= updateInterval)
            {
                _updateTimer = 0f;
                RefreshUI();
            }

            // Icing warning flash
            if (_icingActive && icingWarningPanel != null)
            {
                _icingFlashTimer += Time.deltaTime;
                icingWarningPanel.SetActive(
                    Mathf.Sin(_icingFlashTimer * icingFlashRate * Mathf.PI * 2f) >= 0f);
            }
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void OnWeatherChanged(WeatherConditionData condition)
        {
            RefreshUI();
        }

        private void OnIcingWarning()
        {
            _icingActive     = true;
            _icingFlashTimer = 0f;
        }

        private void RefreshUI()
        {
            if (weatherManager == null) return;

            var cw = weatherManager.CurrentWeather;

            // Weather type + temperature
            if (weatherText != null)
                weatherText.text = $"{FormatWeatherType(cw.type)}  {cw.temperature:F0}°C";

            // Wind
            if (windText != null)
            {
                string dir = DegreesToCardinal(cw.windDirection);
                windText.text = $"Wind: {cw.windSpeed:F1} m/s {dir}";
            }

            // Visibility
            if (visibilityText != null)
            {
                float visKm = cw.visibility / 1000f;
                // Only show '>' prefix when at the maximum measurable range (50 km = unlimited)
                visibilityText.text = cw.visibility >= 50000f
                    ? "Vis: >50 km"
                    : $"Vis: {visKm:F1} km";
            }

            // Icon
            if (weatherIcon != null && weatherIcons != null)
            {
                int idx = (int)cw.type;
                if (idx >= 0 && idx < weatherIcons.Length && weatherIcons[idx] != null)
                    weatherIcon.sprite = weatherIcons[idx];
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string FormatWeatherType(WeatherType t) => t switch
        {
            WeatherType.Clear        => "Clear",
            WeatherType.Cloudy       => "Cloudy",
            WeatherType.Overcast     => "Overcast",
            WeatherType.Rain         => "Rain",
            WeatherType.HeavyRain    => "Heavy Rain",
            WeatherType.Snow         => "Snow",
            WeatherType.HeavySnow    => "Heavy Snow",
            WeatherType.Fog          => "Fog",
            WeatherType.DenseFog     => "Dense Fog",
            WeatherType.Thunderstorm => "Thunderstorm",
            WeatherType.Hail         => "Hail",
            WeatherType.Sandstorm    => "Sandstorm",
            WeatherType.Drizzle      => "Drizzle",
            WeatherType.Sleet        => "Sleet",
            WeatherType.Mist         => "Mist",
            _                        => "Unknown"
        };

        private static string DegreesToCardinal(float deg)
        {
            string[] dirs = { "N","NNE","NE","ENE","E","ESE","SE","SSE",
                               "S","SSW","SW","WSW","W","WNW","NW","NNW" };
            int idx = Mathf.RoundToInt(deg / 22.5f) % 16;
            return dirs[idx < 0 ? idx + 16 : idx];
        }
    }
}
