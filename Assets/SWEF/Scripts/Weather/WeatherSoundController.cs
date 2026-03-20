using System.Collections;
using UnityEngine;

namespace SWEF.Weather
{
    /// <summary>
    /// Ambient weather sound controller for Phase 32.
    ///
    /// <para>Fades rain, wind, and thunder audio loops in and out based on the current
    /// <see cref="WeatherConditionData"/> from <see cref="WeatherManager"/>.  Thunder
    /// is played as a one-shot at random intervals during Thunderstorm weather.</para>
    ///
    /// <para>Respects the master volume set by
    /// <see cref="SWEF.Settings.SettingsManager"/> (auto-found at Start) and provides
    /// altitude-based attenuation above 15 km.</para>
    ///
    /// <para>AudioClips are user-provided assets assigned in the Inspector.
    /// All audio sources are optional — the component degrades gracefully.</para>
    /// </summary>
    public class WeatherSoundController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherSoundController Instance { get; private set; }

        // ── Inspector — Audio Sources ─────────────────────────────────────────────
        [Header("Audio Sources")]
        [Tooltip("Looping rain ambient (auto-plays / auto-stops).")]
        [SerializeField] private AudioSource rainAudioSource;

        [Tooltip("Looping wind ambient (auto-plays / auto-stops).")]
        [SerializeField] private AudioSource windAudioSource;

        [Tooltip("Used for one-shot thunder SFX during thunderstorms.")]
        [SerializeField] private AudioSource thunderAudioSource;

        // ── Inspector — Clips ─────────────────────────────────────────────────────
        [Header("Audio Clips (user-provided)")]
        [Tooltip("Looping light-to-heavy rain clip.")]
        [SerializeField] private AudioClip rainClip;

        [Tooltip("Looping wind howl clip.")]
        [SerializeField] private AudioClip windClip;

        [Tooltip("One-shot thunder SFX clips — randomly selected.")]
        [SerializeField] private AudioClip[] thunderClips;

        // ── Inspector — Volume ────────────────────────────────────────────────────
        [Header("Volume Limits")]
        [SerializeField, Range(0f, 1f)] private float rainMaxVolume    = 0.7f;
        [SerializeField, Range(0f, 1f)] private float windMaxVolume    = 0.6f;
        [SerializeField, Range(0f, 1f)] private float thunderMaxVolume = 0.85f;

        [Header("Crossfade")]
        [Tooltip("Volume fade speed (units per second).")]
        [SerializeField] private float fadeSpeed = 2f;

        [Header("Thunder Timing")]
        [Tooltip("Min/max seconds between thunder one-shots during Thunderstorm.")]
        [SerializeField] private Vector2 thunderIntervalRange = new Vector2(8f, 20f);

        [Header("Altitude Attenuation")]
        [Tooltip("Altitude (metres) above which weather sounds start fading.")]
        [SerializeField] private float audioFadeStartAltitude = 10000f;

        [Tooltip("Altitude (metres) above which weather sounds are fully silent.")]
        [SerializeField] private float audioSilenceAltitude = 15000f;

        // ── Internal ──────────────────────────────────────────────────────────────
        private WeatherManager _weatherManager;
        private float          _targetRainVolume;
        private float          _targetWindVolume;
        private bool           _thunderstormActive;
        private float          _thunderTimer;
        private float          _thunderNextInterval;
        private Coroutine      _thunderCoroutine;
        private float          _masterVolume = 1f;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _weatherManager = FindFirstObjectByType<WeatherManager>();
            if (_weatherManager != null)
                _weatherManager.OnWeatherChanged += ApplyWeather;

            // Try to read master SFX volume from SettingsManager
            var settings = FindFirstObjectByType<SWEF.Settings.SettingsManager>();
            if (settings != null)
                _masterVolume = settings.SfxVolume;

            _thunderNextInterval = Random.Range(thunderIntervalRange.x, thunderIntervalRange.y);

            // Assign clips and ensure sources are set to loop
            AssignClipAndLoop(rainAudioSource,  rainClip,  true);
            AssignClipAndLoop(windAudioSource,  windClip,  true);

            // Start silent
            SetVolume(rainAudioSource,   0f);
            SetVolume(windAudioSource,   0f);
            SetVolume(thunderAudioSource, 0f);
        }

        private void OnDestroy()
        {
            if (_weatherManager != null)
                _weatherManager.OnWeatherChanged -= ApplyWeather;
        }

        private void Update()
        {
            float altFade = ComputeAltitudeFade();
            float dt      = Time.deltaTime * fadeSpeed;

            FadeToward(rainAudioSource,  _targetRainVolume * altFade, dt);
            FadeToward(windAudioSource,  _targetWindVolume * altFade, dt);

            // Thunder one-shots
            if (_thunderstormActive && altFade > 0.01f)
            {
                _thunderTimer += Time.deltaTime;
                if (_thunderTimer >= _thunderNextInterval)
                {
                    _thunderTimer        = 0f;
                    _thunderNextInterval = Random.Range(thunderIntervalRange.x, thunderIntervalRange.y);
                    PlayThunder(altFade);
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Updates volume targets based on a new weather condition snapshot.</summary>
        public void ApplyWeather(WeatherConditionData condition)
        {
            // Rain volume
            _targetRainVolume = condition.type switch
            {
                WeatherType.Drizzle      => rainMaxVolume * 0.2f,
                WeatherType.Rain         => rainMaxVolume * condition.intensity,
                WeatherType.HeavyRain    => rainMaxVolume,
                WeatherType.Thunderstorm => rainMaxVolume * 0.8f,
                WeatherType.Sleet        => rainMaxVolume * 0.4f,
                _                        => 0f
            };
            _targetRainVolume *= _masterVolume;

            // Wind volume (scaled by wind speed, max ~30 m/s)
            float windFraction  = Mathf.Clamp01(condition.windSpeed / 30f);
            _targetWindVolume   = windMaxVolume * windFraction * _masterVolume;

            // Thunderstorm
            bool wasThunderstorm = _thunderstormActive;
            _thunderstormActive  = condition.type == WeatherType.Thunderstorm;
            if (_thunderstormActive && !wasThunderstorm)
                _thunderTimer = 0f;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private float ComputeAltitudeFade()
        {
            float alt = (float)SWEF.Core.SWEFSession.Alt;
            if (alt >= audioSilenceAltitude)   return 0f;
            if (alt <= audioFadeStartAltitude) return 1f;
            return 1f - (alt - audioFadeStartAltitude) /
                        (audioSilenceAltitude - audioFadeStartAltitude);
        }

        private static void AssignClipAndLoop(AudioSource src, AudioClip clip, bool loop)
        {
            if (src == null || clip == null) return;
            src.clip = clip;
            src.loop = loop;
            if (loop && !src.isPlaying) src.Play();
        }

        private static void SetVolume(AudioSource src, float vol)
        {
            if (src != null) src.volume = vol;
        }

        private static void FadeToward(AudioSource src, float target, float dt)
        {
            if (src == null) return;
            src.volume = Mathf.MoveTowards(src.volume, target, dt);
        }

        private void PlayThunder(float altFade)
        {
            if (thunderAudioSource == null || thunderClips == null || thunderClips.Length == 0) return;
            var clip = thunderClips[Random.Range(0, thunderClips.Length)];
            if (clip == null) return;
            thunderAudioSource.volume = thunderMaxVolume * altFade * _masterVolume;
            thunderAudioSource.PlayOneShot(clip);
        }
    }
}
