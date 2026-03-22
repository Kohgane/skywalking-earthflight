using System;
using UnityEngine;
using SWEF.Weather;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Manages a secondary ambient-sound <see cref="AudioSource"/> whose content is driven
    /// by the current weather condition reported by <see cref="WeatherManager"/>.
    /// <para>
    /// Weather → audio behaviour:
    /// <list type="bullet">
    ///   <item>Rain        — rain ambience layer, slight music volume reduction.</item>
    ///   <item>Thunderstorm — brief volume ducks on lightning strikes.</item>
    ///   <item>Snow        — gentle wind layer, softer music.</item>
    ///   <item>Fog         — subtle reverb feel (volume reduction).</item>
    ///   <item>Clear       — no modification; ambient muted.</item>
    ///   <item>Wind/Windy  — wind layer scaled with flight speed.</item>
    /// </list>
    /// Ambient volume never exceeds 30 % of the current music volume.
    /// Toggled via <see cref="MusicPlayerConfig.weatherMixEnabled"/>.
    /// </para>
    /// </summary>
    public class MusicWeatherMixer : MonoBehaviour
    {
        // ── Inspector — Clips ─────────────────────────────────────────────────────
        [Header("Ambient Clips (optional — assign in Inspector)")]
        [Tooltip("Looping rain ambience clip.")]
        [SerializeField] private AudioClip rainAmbienceClip;

        [Tooltip("Looping snow/wind ambience clip.")]
        [SerializeField] private AudioClip snowAmbienceClip;

        [Tooltip("Looping wind ambience clip.")]
        [SerializeField] private AudioClip windAmbienceClip;

        [Tooltip("Looping fog/mystery ambience clip.")]
        [SerializeField] private AudioClip fogAmbienceClip;

        // ── Inspector — Mixing ────────────────────────────────────────────────────
        [Header("Mixing")]
        [Tooltip("Maximum fraction of music volume that ambient audio may reach (0–0.3 recommended).")]
        [Range(0f, 1f)]
        [SerializeField] private float maxAmbientFraction = 0.30f;

        [Tooltip("Seconds to crossfade from one ambient layer to another.")]
        [SerializeField] private float ambientFadeDuration = 3f;

        [Tooltip("Multiplier applied to music volume during thunderstorms (duck amount).")]
        [Range(0f, 1f)]
        [SerializeField] private float thunderDuckMultiplier = 0.5f;

        [Tooltip("Seconds a thunder duck lasts.")]
        [SerializeField] private float thunderDuckDuration = 0.8f;

        // ── Private state ─────────────────────────────────────────────────────────
        private AudioSource _ambientSource;
        private AudioClip   _currentAmbientClip;
        private float       _targetAmbientVolume;
        private float       _currentAmbientVolume;
        private bool        _thunderDucking;
        private float       _thunderTimer;
        private float       _musicVolumeMultiplier = 1f;
        private WeatherConditionData _lastWeather;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _ambientSource              = gameObject.AddComponent<AudioSource>();
            _ambientSource.playOnAwake  = false;
            _ambientSource.loop         = true;
            _ambientSource.spatialBlend = 0f;
            _ambientSource.volume       = 0f;
        }

        private void Start()
        {
            if (WeatherManager.Instance != null)
            {
                WeatherManager.Instance.OnWeatherChanged += OnWeatherChanged;
                ApplyWeather(WeatherManager.Instance.CurrentWeather);
            }
            else
            {
                Debug.LogWarning("[SWEF][MusicWeatherMixer] WeatherManager.Instance not found.");
            }
        }

        private void OnDestroy()
        {
            if (WeatherManager.Instance != null)
                WeatherManager.Instance.OnWeatherChanged -= OnWeatherChanged;
        }

        private void Update()
        {
            if (!IsEnabled()) return;

            // Smoothly fade ambient volume
            float musicVol   = GetCurrentMusicVolume();
            float maxAllowed = musicVol * maxAmbientFraction;
            float ambTarget  = Mathf.Min(_targetAmbientVolume, maxAllowed);

            _currentAmbientVolume = Mathf.MoveTowards(
                _currentAmbientVolume, ambTarget, Time.deltaTime / Mathf.Max(0.01f, ambientFadeDuration));

            if (_ambientSource != null)
                _ambientSource.volume = _currentAmbientVolume;

            // Apply music volume modifier
            ApplyMusicVolumeModifier(_musicVolumeMultiplier);

            // Handle thunder duck decay
            if (_thunderDucking)
            {
                _thunderTimer -= Time.deltaTime;
                if (_thunderTimer <= 0f)
                {
                    _thunderDucking         = false;
                    _musicVolumeMultiplier  = 1f;
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Triggers a brief thunder-duck effect on the music volume.</summary>
        public void TriggerThunderDuck()
        {
            if (!IsEnabled()) return;
            _thunderDucking         = true;
            _thunderTimer           = thunderDuckDuration;
            _musicVolumeMultiplier  = thunderDuckMultiplier;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void OnWeatherChanged(WeatherConditionData weather)
        {
            ApplyWeather(weather);
        }

        private void ApplyWeather(WeatherConditionData weather)
        {
            _lastWeather = weather;
            if (!IsEnabled())
            {
                FadeOutAmbient();
                _musicVolumeMultiplier = 1f;
                return;
            }

            switch (weather.type)
            {
                case WeatherType.Rain:
                    CrossfadeTo(rainAmbienceClip, weather.intensity * 0.8f);
                    _musicVolumeMultiplier = Mathf.Lerp(1f, 0.85f, weather.intensity);
                    break;

                case WeatherType.HeavyRain:
                    CrossfadeTo(rainAmbienceClip, 1f);
                    _musicVolumeMultiplier = 0.8f;
                    break;

                case WeatherType.Thunderstorm:
                    CrossfadeTo(rainAmbienceClip, 1f);
                    _musicVolumeMultiplier = 0.75f;
                    // Duck on a pseudo-random lightning schedule
                    if (!_thunderDucking && UnityEngine.Random.value < 0.002f)
                        TriggerThunderDuck();
                    break;

                case WeatherType.Snow:
                case WeatherType.HeavySnow:
                    CrossfadeTo(snowAmbienceClip, weather.intensity * 0.6f);
                    _musicVolumeMultiplier = Mathf.Lerp(1f, 0.9f, weather.intensity);
                    break;

                case WeatherType.Fog:
                case WeatherType.DenseFog:
                    CrossfadeTo(fogAmbienceClip, weather.intensity * 0.5f);
                    _musicVolumeMultiplier = Mathf.Lerp(1f, 0.88f, weather.intensity);
                    break;

                case WeatherType.Clear:
                case WeatherType.Cloudy:
                    FadeOutAmbient();
                    _musicVolumeMultiplier = 1f;
                    break;

                default:
                    // Windy / other — use wind layer
                    CrossfadeTo(windAmbienceClip, weather.windSpeed / 30f);
                    _musicVolumeMultiplier = 1f;
                    break;
            }
        }

        private void CrossfadeTo(AudioClip clip, float normalizedVolume)
        {
            if (clip == null)
            {
                FadeOutAmbient();
                return;
            }

            if (_ambientSource.clip != clip)
            {
                _ambientSource.clip = clip;
                _ambientSource.Play();
            }

            _targetAmbientVolume = Mathf.Clamp01(normalizedVolume);
        }

        private void FadeOutAmbient()
        {
            _targetAmbientVolume = 0f;
        }

        private void ApplyMusicVolumeModifier(float multiplier)
        {
            if (MusicPlayerManager.Instance == null) return;
            // Apply weather volume modifier directly to the music AudioSource without
            // permanently altering State.volume (which is the user-set preference).
            AudioSource src = MusicPlayerManager.Instance.GetComponent<AudioSource>();
            if (src != null)
                src.volume = Mathf.Clamp01(MusicPlayerManager.Instance.State.volume * multiplier);
        }

        private float GetCurrentMusicVolume()
        {
            if (MusicPlayerManager.Instance == null) return 0.8f;
            return MusicPlayerManager.Instance.State.volume;
        }

        private bool IsEnabled()
        {
            if (MusicPlayerManager.Instance == null) return false;
            return MusicPlayerManager.Instance.Config.weatherMixEnabled;
        }
    }
}
