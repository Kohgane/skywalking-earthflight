using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.UI
{
    /// <summary>
    /// Weather HUD widget — displays current weather condition, temperature, wind,
    /// and visibility in a small non-intrusive corner overlay.
    ///
    /// <para>Assign the UI references in the Inspector.  The widget auto-subscribes to
    /// <see cref="SWEF.Weather.WeatherStateManager.OnWeatherStateUpdated"/> so it always
    /// shows the current altitude-adjusted weather.</para>
    ///
    /// <para>Call <see cref="SetVisible"/> or bind the toggle button to control
    /// widget visibility from the HUD settings panel.</para>
    /// </summary>
    public class WeatherHUD : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherHUD Instance { get; private set; }

        // ── Inspector — Root ──────────────────────────────────────────────────────
        [Header("Root")]
        [Tooltip("Root CanvasGroup of the weather widget (used for show/hide transitions).")]
        [SerializeField] private CanvasGroup widgetGroup;

        [Tooltip("Show/hide animation duration in seconds.")]
        [SerializeField] private float toggleFadeDuration = 0.3f;

        // ── Inspector — Labels ────────────────────────────────────────────────────
        [Header("Labels")]
        [Tooltip("Condition name label (e.g. 'Heavy Rain').")]
        [SerializeField] private TextMeshProUGUI conditionLabel;

        [Tooltip("Temperature label (e.g. '18 °C').")]
        [SerializeField] private TextMeshProUGUI temperatureLabel;

        [Tooltip("Wind speed and direction label (e.g. '12 m/s ↗').")]
        [SerializeField] private TextMeshProUGUI windLabel;

        [Tooltip("Visibility distance label (e.g. '5.2 km').")]
        [SerializeField] private TextMeshProUGUI visibilityLabel;

        // ── Inspector — Icon ──────────────────────────────────────────────────────
        [Header("Condition Icon")]
        [Tooltip("Image component that displays the weather condition icon.")]
        [SerializeField] private Image conditionIcon;

        [Tooltip("Icons per condition — array index must match WeatherCondition enum order.")]
        [SerializeField] private Sprite[] conditionIcons;

        [Tooltip("Duration of icon cross-fade animation in seconds.")]
        [SerializeField] private float iconFadeDuration = 0.5f;

        // ── Inspector — Toggle ────────────────────────────────────────────────────
        [Header("Toggle")]
        [Tooltip("Optional button to toggle widget visibility.")]
        [SerializeField] private Button toggleButton;

        // ── State ─────────────────────────────────────────────────────────────────
        private bool _isVisible       = true;
        private Coroutine _fadeCoroutine;
        private Coroutine _iconCoroutine;
        private SWEF.Weather.WeatherCondition _lastCondition = SWEF.Weather.WeatherCondition.Clear;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (toggleButton != null)
                toggleButton.onClick.AddListener(() => SetVisible(!_isVisible));
        }

        private void Start()
        {
            if (SWEF.Weather.WeatherStateManager.Instance != null)
                SWEF.Weather.WeatherStateManager.Instance.OnWeatherStateUpdated += Refresh;
            else
                Debug.LogWarning("[SWEF][WeatherHUD] WeatherStateManager not found.");
        }

        private void OnDestroy()
        {
            if (SWEF.Weather.WeatherStateManager.Instance != null)
                SWEF.Weather.WeatherStateManager.Instance.OnWeatherStateUpdated -= Refresh;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Shows or hides the weather widget with a smooth fade.
        /// </summary>
        /// <param name="visible"><c>true</c> to show, <c>false</c> to hide.</param>
        public void SetVisible(bool visible)
        {
            _isVisible = visible;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeWidget(visible ? 1f : 0f, toggleFadeDuration));
        }

        /// <summary>
        /// Updates all HUD elements to reflect the provided <see cref="SWEF.Weather.WeatherData"/>.
        /// Called automatically via event subscription.
        /// </summary>
        /// <param name="data">Current altitude-adjusted weather snapshot.</param>
        public void Refresh(SWEF.Weather.WeatherData data)
        {
            // Temperature
            if (temperatureLabel != null)
                temperatureLabel.text = $"{data.temperatureCelsius:F0} °C";

            // Wind
            if (windLabel != null)
            {
                string dir = WindDirectionArrow(data.windDirectionDeg);
                windLabel.text = $"{data.windSpeedMs:F1} m/s {dir}";
            }

            // Visibility
            if (visibilityLabel != null)
            {
                string vis = data.visibility >= 1000f
                    ? $"{data.visibility / 1000f:F1} km"
                    : $"{data.visibility:F0} m";
                visibilityLabel.text = vis;
            }

            // Condition name
            if (conditionLabel != null)
                conditionLabel.text = FormatCondition(data.condition);

            // Icon (animate if condition changed)
            if (data.condition != _lastCondition)
            {
                _lastCondition = data.condition;
                if (_iconCoroutine != null) StopCoroutine(_iconCoroutine);
                _iconCoroutine = StartCoroutine(AnimateIconTransition(data.condition));
            }
        }

        // ── Coroutines ────────────────────────────────────────────────────────────

        private IEnumerator FadeWidget(float target, float duration)
        {
            if (widgetGroup == null) yield break;
            float start   = widgetGroup.alpha;
            float elapsed = 0f;
            widgetGroup.interactable   = target > 0f;
            widgetGroup.blocksRaycasts = target > 0f;
            while (elapsed < duration)
            {
                elapsed            += Time.deltaTime;
                widgetGroup.alpha   = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            widgetGroup.alpha = target;
        }

        private IEnumerator AnimateIconTransition(SWEF.Weather.WeatherCondition condition)
        {
            if (conditionIcon == null) yield break;

            // Fade out
            float elapsed = 0f;
            Color c = conditionIcon.color;
            while (elapsed < iconFadeDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                conditionIcon.color = new Color(c.r, c.g, c.b,
                    Mathf.Lerp(1f, 0f, elapsed / (iconFadeDuration * 0.5f)));
                yield return null;
            }

            // Swap sprite
            int idx = (int)condition;
            if (conditionIcons != null && idx < conditionIcons.Length && conditionIcons[idx] != null)
                conditionIcon.sprite = conditionIcons[idx];

            // Fade in
            elapsed = 0f;
            while (elapsed < iconFadeDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                conditionIcon.color = new Color(c.r, c.g, c.b,
                    Mathf.Lerp(0f, 1f, elapsed / (iconFadeDuration * 0.5f)));
                yield return null;
            }
            conditionIcon.color = new Color(c.r, c.g, c.b, 1f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string FormatCondition(SWEF.Weather.WeatherCondition cond) =>
            cond switch
            {
                SWEF.Weather.WeatherCondition.Clear       => "Clear",
                SWEF.Weather.WeatherCondition.Cloudy      => "Cloudy",
                SWEF.Weather.WeatherCondition.Overcast    => "Overcast",
                SWEF.Weather.WeatherCondition.Rain        => "Rain",
                SWEF.Weather.WeatherCondition.HeavyRain   => "Heavy Rain",
                SWEF.Weather.WeatherCondition.Snow        => "Snow",
                SWEF.Weather.WeatherCondition.HeavySnow   => "Heavy Snow",
                SWEF.Weather.WeatherCondition.Fog         => "Fog",
                SWEF.Weather.WeatherCondition.DenseFog    => "Dense Fog",
                SWEF.Weather.WeatherCondition.Thunderstorm => "Thunderstorm",
                SWEF.Weather.WeatherCondition.Hail        => "Hail",
                SWEF.Weather.WeatherCondition.Sandstorm   => "Sandstorm",
                SWEF.Weather.WeatherCondition.Windy       => "Windy",
                _                                         => cond.ToString()
            };

        /// <summary>Returns a Unicode arrow character for the given meteorological wind direction.</summary>
        private static string WindDirectionArrow(float degrees)
        {
            // Meteorological direction = from which wind blows → display as "toward" arrow
            string[] arrows = { "↓", "↙", "←", "↖", "↑", "↗", "→", "↘" };
            int idx = Mathf.RoundToInt(degrees / 45f) % 8;
            return arrows[idx];
        }
    }
}
