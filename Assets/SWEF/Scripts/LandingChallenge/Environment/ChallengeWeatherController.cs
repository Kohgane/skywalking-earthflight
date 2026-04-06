// ChallengeWeatherController.cs — Phase 120: Precision Landing Challenge System
// Weather for challenges: fixed presets, progressive difficulty, random weather mode.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Controls weather conditions during a landing challenge.
    /// Applies fixed weather presets, progressive difficulty progression
    /// (Clear → Fog → Storm), and a random weather mode.
    /// </summary>
    public class ChallengeWeatherController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Weather Mode")]
        [SerializeField] private bool randomMode = false;

        [Header("Progressive Weather")]
        [SerializeField] private bool  progressiveMode    = false;
        [SerializeField] private float progressionTimeSec = 300f;

        // ── State ─────────────────────────────────────────────────────────────

        private WeatherPreset _currentPreset = WeatherPreset.Clear;
        private float         _elapsed;
        private bool          _isActive;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when the active weather preset changes.</summary>
        public event System.Action<WeatherPreset> OnWeatherChanged;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Currently active weather preset.</summary>
        public WeatherPreset CurrentPreset => _currentPreset;

        /// <summary>
        /// Severity value (0–1) for scoring bonus calculation.
        /// Higher-severity presets contribute a larger bonus.
        /// </summary>
        public float WeatherSeverity => GetSeverity(_currentPreset);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Apply a specific weather preset immediately.</summary>
        public void ApplyPreset(WeatherPreset preset)
        {
            if (_currentPreset == preset) return;
            _currentPreset = preset;
            OnWeatherChanged?.Invoke(_currentPreset);
        }

        /// <summary>Activate weather controller for a challenge.</summary>
        public void Activate(WeatherPreset startPreset)
        {
            _elapsed      = 0f;
            _isActive     = true;
            ApplyPreset(randomMode ? RandomPreset() : startPreset);
        }

        /// <summary>Deactivate the weather controller.</summary>
        public void Deactivate() => _isActive = false;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (!_isActive) return;

            if (progressiveMode)
            {
                _elapsed += Time.deltaTime;
                int stage = Mathf.FloorToInt(_elapsed / progressionTimeSec * 3f);
                WeatherPreset target = stage switch
                {
                    0 => WeatherPreset.Clear,
                    1 => WeatherPreset.Overcast,
                    2 => WeatherPreset.Fog,
                    _ => WeatherPreset.Thunderstorm
                };
                ApplyPreset(target);
            }
        }

        // ── Private ───────────────────────────────────────────────────────────

        private float GetSeverity(WeatherPreset preset)
        {
            switch (preset)
            {
                case WeatherPreset.Clear:        return 0f;
                case WeatherPreset.PartlyCloudy: return 0.1f;
                case WeatherPreset.Overcast:     return 0.2f;
                case WeatherPreset.LightRain:    return 0.3f;
                case WeatherPreset.Crosswind:    return 0.4f;
                case WeatherPreset.HeavyRain:    return 0.5f;
                case WeatherPreset.Gusting:      return 0.6f;
                case WeatherPreset.Fog:          return 0.7f;
                case WeatherPreset.Thunderstorm: return 0.9f;
                case WeatherPreset.Blizzard:     return 1.0f;
                default:                         return 0f;
            }
        }

        private WeatherPreset RandomPreset()
        {
            var values = (WeatherPreset[])System.Enum.GetValues(typeof(WeatherPreset));
            return values[Random.Range(0, values.Length)];
        }
    }
}
