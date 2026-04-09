using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Atmosphere
{
    /// <summary>
    /// Displays current weather information and wind status in the HUD.
    /// Provides a dropdown for manual weather override via <see cref="WeatherController"/>.
    /// </summary>
    public class WeatherUI : MonoBehaviour
    {
        [SerializeField] private WeatherController weatherController;
        [SerializeField] private WindController windController;

        [Header("Display Elements")]
        [SerializeField] private Text weatherText;
        [SerializeField] private Text windText;
        [SerializeField] private Image weatherIcon;
        [SerializeField] private Sprite[] weatherIcons;  // 5 sprites: Clear, Cloudy, Rain, Storm, Snow

        [Header("Override")]
        [SerializeField] private Dropdown weatherOverrideDropdown;

        private void Awake()
        {
            if (weatherController == null)
                weatherController = FindFirstObjectByType<WeatherController>();
            if (windController == null)
                windController = FindFirstObjectByType<WindController>();

            if (weatherController != null)
                weatherController.OnWeatherChanged += OnWeatherChanged;

            if (windController != null)
                windController.OnWindChanged += OnWindChanged;

            SetupDropdown();
        }

        private void OnDestroy()
        {
            if (weatherController != null)
                weatherController.OnWeatherChanged -= OnWeatherChanged;
            if (windController != null)
                windController.OnWindChanged -= OnWindChanged;
        }

        private void Start()
        {
            if (weatherController != null)
                OnWeatherChanged(weatherController.CurrentWeather);
        }

        private void Update()
        {
            if (windController == null || windText == null) return;

            string cardinal = GetCardinalDirection(windController.CurrentWindDirection);
            windText.text = $"Wind: {cardinal} {windController.CurrentWindStrength:0.0} m/s";
        }

        private void OnWeatherChanged(WeatherController.WeatherType type)
        {
            if (weatherText != null)
                weatherText.text = type.ToString();

            if (weatherIcon != null && weatherIcons != null)
            {
                int idx = (int)type;
                if (idx >= 0 && idx < weatherIcons.Length && weatherIcons[idx] != null)
                    weatherIcon.sprite = weatherIcons[idx];
            }

            // Sync dropdown without triggering callback
            if (weatherOverrideDropdown != null)
                weatherOverrideDropdown.SetValueWithoutNotify((int)type);
        }

        private void OnWindChanged(Vector3 direction, float strength)
        {
            // Wind text is refreshed each frame in Update(); nothing extra needed here.
        }

        private void SetupDropdown()
        {
            if (weatherOverrideDropdown == null || weatherController == null) return;

            weatherOverrideDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            foreach (WeatherController.WeatherType t in System.Enum.GetValues(typeof(WeatherController.WeatherType)))
                options.Add(t.ToString());

            weatherOverrideDropdown.AddOptions(options);
            weatherOverrideDropdown.SetValueWithoutNotify((int)weatherController.CurrentWeather);
            weatherOverrideDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        private void OnDropdownChanged(int index)
        {
            weatherController?.SetWeather((WeatherController.WeatherType)index);
        }

        private static string GetCardinalDirection(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return "—";

            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            if (angle < 22.5f  || angle >= 337.5f) return "N";
            if (angle < 67.5f)  return "NE";
            if (angle < 112.5f) return "E";
            if (angle < 157.5f) return "SE";
            if (angle < 202.5f) return "S";
            if (angle < 247.5f) return "SW";
            if (angle < 292.5f) return "W";
            return "NW";
        }
    }
}
