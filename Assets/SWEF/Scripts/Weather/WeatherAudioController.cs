using System.Collections;
using UnityEngine;

namespace SWEF.Weather
{
    /// <summary>
    /// Manages ambient weather audio, crossfading between weather states and scaling
    /// volume with intensity and altitude.
    ///
    /// <para>Requires at least one <see cref="AudioSource"/> component on the same
    /// GameObject (or child) for each weather type.  Sources are auto-assigned from
    /// <see cref="AudioClip"/> fields; extra sources are created at runtime if needed.</para>
    ///
    /// <para>Auto-subscribes to <see cref="WeatherStateManager.OnWeatherStateUpdated"/>.</para>
    /// </summary>
    public class WeatherAudioController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherAudioController Instance { get; private set; }

        // ── Inspector — Clips ─────────────────────────────────────────────────────
        [Header("Audio Clips")]
        [Tooltip("Looping light rain ambient.")]
        [SerializeField] private AudioClip clipRainLight;

        [Tooltip("Looping heavy rain ambient.")]
        [SerializeField] private AudioClip clipRainHeavy;

        [Tooltip("Looping wind howl (scales with wind speed).")]
        [SerializeField] private AudioClip clipWind;

        [Tooltip("Thunder one-shot SFX array (randomly selected).")]
        [SerializeField] private AudioClip[] clipThunder;

        [Tooltip("Looping snow / muffled silence ambient.")]
        [SerializeField] private AudioClip clipSnow;

        [Tooltip("Looping sandstorm ambient.")]
        [SerializeField] private AudioClip clipSandstorm;

        // ── Inspector — Config ────────────────────────────────────────────────────
        [Header("Crossfade")]
        [Tooltip("Duration in seconds for volume crossfades between weather states.")]
        [SerializeField] private float crossfadeDuration = 2f;

        [Header("Volume Caps")]
        [Tooltip("Maximum volume for rain loops.")]
        [SerializeField] private float rainMaxVolume = 0.7f;

        [Tooltip("Maximum volume for wind loop.")]
        [SerializeField] private float windMaxVolume = 0.6f;

        [Tooltip("Maximum volume for thunder one-shots.")]
        [SerializeField] private float thunderMaxVolume = 0.85f;

        [Tooltip("Maximum volume for snow ambient.")]
        [SerializeField] private float snowMaxVolume = 0.4f;

        [Tooltip("Maximum volume for sandstorm loop.")]
        [SerializeField] private float sandMaxVolume = 0.65f;

        [Header("Altitude Attenuation")]
        [Tooltip("Altitude above which weather audio starts fading (thinner atmosphere).")]
        [SerializeField] private float audioFadeStartAltitude = 8000f;

        [Tooltip("Altitude above which weather audio is completely silent.")]
        [SerializeField] private float audioSilenceAltitude   = 20000f;

        [Header("Thunder Timing")]
        [Tooltip("Minimum and maximum seconds between thunder claps during a thunderstorm.")]
        [SerializeField] private Vector2 thunderIntervalRange = new Vector2(4f, 18f);

        // ── Internal ──────────────────────────────────────────────────────────────
        private AudioSource _srcRain;
        private AudioSource _srcWind;
        private AudioSource _srcSnow;
        private AudioSource _srcSand;
        private AudioSource _srcThunder;

        private float _thunderTimer;
        private float _thunderNextInterval;
        private bool  _thunderActive;

        private WeatherCondition _lastCondition = WeatherCondition.Clear;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateSources();
            _thunderNextInterval = Random.Range(thunderIntervalRange.x, thunderIntervalRange.y);
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
            HandleThunder();
            ApplyAltitudeAttenuation();
        }

        // ── Event handler ─────────────────────────────────────────────────────────

        private void OnWeatherUpdated(WeatherData data)
        {
            if (data.condition == _lastCondition) return;
            _lastCondition = data.condition;

            float intensity = data.precipitationIntensity;

            // Stop all, then start what's needed
            CrossfadeAll(0f, crossfadeDuration);

            switch (data.condition)
            {
                case WeatherCondition.Rain:
                    StartCoroutine(FadeSource(_srcRain, clipRainLight,  rainMaxVolume * Mathf.Max(0.3f, intensity), crossfadeDuration));
                    StartCoroutine(FadeSource(_srcWind, clipWind,       windMaxVolume * 0.3f, crossfadeDuration));
                    _thunderActive = false;
                    break;

                case WeatherCondition.HeavyRain:
                    StartCoroutine(FadeSource(_srcRain, clipRainHeavy,  rainMaxVolume * intensity, crossfadeDuration));
                    StartCoroutine(FadeSource(_srcWind, clipWind,       windMaxVolume * 0.5f, crossfadeDuration));
                    _thunderActive = false;
                    break;

                case WeatherCondition.Thunderstorm:
                    StartCoroutine(FadeSource(_srcRain, clipRainHeavy,  rainMaxVolume, crossfadeDuration));
                    StartCoroutine(FadeSource(_srcWind, clipWind,       windMaxVolume, crossfadeDuration));
                    _thunderActive = true;
                    _thunderTimer  = 0f;
                    break;

                case WeatherCondition.Snow:
                case WeatherCondition.HeavySnow:
                    StartCoroutine(FadeSource(_srcSnow, clipSnow, snowMaxVolume * Mathf.Max(0.3f, intensity), crossfadeDuration));
                    _thunderActive = false;
                    break;

                case WeatherCondition.Sandstorm:
                    StartCoroutine(FadeSource(_srcSand, clipSandstorm, sandMaxVolume, crossfadeDuration));
                    StartCoroutine(FadeSource(_srcWind, clipWind,      windMaxVolume * 0.8f, crossfadeDuration));
                    _thunderActive = false;
                    break;

                case WeatherCondition.Windy:
                    StartCoroutine(FadeSource(_srcWind, clipWind,
                        windMaxVolume * Mathf.Clamp01(data.windSpeedMs / 20f), crossfadeDuration));
                    _thunderActive = false;
                    break;

                default:
                    _thunderActive = false;
                    break;
            }
        }

        // ── Thunder ───────────────────────────────────────────────────────────────

        private void HandleThunder()
        {
            if (!_thunderActive || clipThunder == null || clipThunder.Length == 0) return;

            _thunderTimer += Time.deltaTime;
            if (_thunderTimer >= _thunderNextInterval)
            {
                _thunderTimer        = 0f;
                _thunderNextInterval = Random.Range(thunderIntervalRange.x, thunderIntervalRange.y);
                PlayThunder();
            }
        }

        private void PlayThunder()
        {
            if (_srcThunder == null || clipThunder == null || clipThunder.Length == 0) return;
            var clip = clipThunder[Random.Range(0, clipThunder.Length)];
            if (clip == null) return;
            _srcThunder.PlayOneShot(clip, thunderMaxVolume * GetAltitudeAttenuation());
        }

        // ── Altitude attenuation ──────────────────────────────────────────────────

        private void ApplyAltitudeAttenuation()
        {
            float att = GetAltitudeAttenuation();
            if (_srcRain   != null) _srcRain.volume   = _srcRain.volume   * att;
            if (_srcWind   != null) _srcWind.volume   = _srcWind.volume   * att;
            if (_srcSnow   != null) _srcSnow.volume   = _srcSnow.volume   * att;
            if (_srcSand   != null) _srcSand.volume   = _srcSand.volume   * att;
        }

        private float GetAltitudeAttenuation()
        {
            float alt = WeatherStateManager.Instance != null
                ? WeatherStateManager.Instance.AltitudeMeters : 0f;
            return 1f - Mathf.Clamp01(
                (alt - audioFadeStartAltitude) / (audioSilenceAltitude - audioFadeStartAltitude));
        }

        // ── Crossfade helpers ─────────────────────────────────────────────────────

        private void CrossfadeAll(float targetVolume, float duration)
        {
            if (_srcRain   != null && _srcRain.isPlaying)   StartCoroutine(FadeVolume(_srcRain,   targetVolume, duration));
            if (_srcWind   != null && _srcWind.isPlaying)   StartCoroutine(FadeVolume(_srcWind,   targetVolume, duration));
            if (_srcSnow   != null && _srcSnow.isPlaying)   StartCoroutine(FadeVolume(_srcSnow,   targetVolume, duration));
            if (_srcSand   != null && _srcSand.isPlaying)   StartCoroutine(FadeVolume(_srcSand,   targetVolume, duration));
        }

        private IEnumerator FadeSource(AudioSource src, AudioClip clip, float targetVol, float duration)
        {
            if (src == null || clip == null) yield break;

            // Fade out current clip if playing a different one
            if (src.isPlaying && src.clip != clip)
            {
                yield return StartCoroutine(FadeVolume(src, 0f, duration * 0.5f));
                src.Stop();
            }

            if (!src.isPlaying || src.clip != clip)
            {
                src.clip   = clip;
                src.loop   = true;
                src.volume = 0f;
                src.Play();
            }

            yield return StartCoroutine(FadeVolume(src, targetVol, duration));
        }

        private IEnumerator FadeVolume(AudioSource src, float target, float duration)
        {
            if (src == null) yield break;
            float start   = src.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed    += Time.deltaTime;
                src.volume  = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            src.volume = target;
            if (Mathf.Approximately(target, 0f)) src.Stop();
        }

        // ── Source factory ────────────────────────────────────────────────────────

        private void CreateSources()
        {
            _srcRain    = CreateSource("WeatherAudio_Rain");
            _srcWind    = CreateSource("WeatherAudio_Wind");
            _srcSnow    = CreateSource("WeatherAudio_Snow");
            _srcSand    = CreateSource("WeatherAudio_Sand");
            _srcThunder = CreateSource("WeatherAudio_Thunder");
        }

        private AudioSource CreateSource(string goName)
        {
            var go  = new GameObject(goName) { };
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake     = false;
            src.spatialBlend    = 0f;  // 2D
            src.volume          = 0f;
            return src;
        }
    }
}
