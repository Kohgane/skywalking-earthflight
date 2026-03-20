using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Weather
{
    /// <summary>
    /// Central Phase 32 weather orchestrator.
    ///
    /// <para>Subscribes to <see cref="WeatherAPIClient.OnWeatherUpdated"/> and smoothly
    /// interpolates between the previous and new <see cref="WeatherConditionData"/> over
    /// <see cref="transitionDuration"/> seconds.  Other sub-systems (
    /// <see cref="PrecipitationSystem"/>, <see cref="WindSystem"/>,
    /// <see cref="WeatherFogController"/>, <see cref="WeatherLightingController"/>) read
    /// <see cref="CurrentWeather"/> and <see cref="CurrentWind"/> each frame.</para>
    ///
    /// <para>Provides <see cref="ForceWeather"/> for in-Editor testing and
    /// <see cref="ResetToLive"/> to resume API-driven weather.</para>
    /// </summary>
    public class WeatherManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private WeatherAPIClient apiClient;
        [SerializeField] private PrecipitationSystem precipitationSystem;
        [SerializeField] private WindSystem windSystem;
        [SerializeField] private WeatherFogController fogController;
        [SerializeField] private WeatherLightingController lightingController;

        [Header("Transition")]
        [Tooltip("Seconds to smoothly blend from one weather state to the next.")]
        [SerializeField] private float transitionDuration = 10f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the weather state has finished transitioning to a new value.</summary>
        public event Action<WeatherConditionData> OnWeatherChanged;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>The current (interpolated) weather condition read by sub-systems.</summary>
        public WeatherConditionData CurrentWeather { get; private set; }

        /// <summary>The current (interpolated) wind data read by <see cref="WeatherFlightModifier"/>.</summary>
        public WindData CurrentWind { get; private set; }

        // ── Internal ──────────────────────────────────────────────────────────────
        private WeatherConditionData _fromWeather;
        private WeatherConditionData _targetWeather;
        private float                _transitionProgress = 1f;   // 0→1
        private bool                 _liveMode           = true;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            var clear = WeatherConditionData.CreateClear();
            CurrentWeather      = clear;
            _fromWeather        = clear;
            _targetWeather      = clear;
            CurrentWind         = WindData.FromSpeedAndDirection(clear.windSpeed, clear.windDirection);
        }

        private void Start()
        {
            // Auto-find sub-system references when not set via Inspector
            if (apiClient           == null) apiClient           = FindFirstObjectByType<WeatherAPIClient>();
            if (precipitationSystem == null) precipitationSystem = FindFirstObjectByType<PrecipitationSystem>();
            if (windSystem          == null) windSystem          = FindFirstObjectByType<WindSystem>();
            if (fogController       == null) fogController       = FindFirstObjectByType<WeatherFogController>();
            if (lightingController  == null) lightingController  = FindFirstObjectByType<WeatherLightingController>();

            if (apiClient != null)
                apiClient.OnWeatherUpdated += HandleAPIWeatherUpdate;
            else
                Debug.LogWarning("[SWEF][WeatherManager] WeatherAPIClient not found — no live weather.");

            // Trigger an initial fetch using the player's last known position
            apiClient?.FetchWeather(SWEF.Core.SWEFSession.Lat, SWEF.Core.SWEFSession.Lon);
        }

        private void OnDestroy()
        {
            if (apiClient != null)
                apiClient.OnWeatherUpdated -= HandleAPIWeatherUpdate;
        }

        private void Update()
        {
            if (_transitionProgress >= 1f) return;

            _transitionProgress += Time.deltaTime / Mathf.Max(0.01f, transitionDuration);
            _transitionProgress  = Mathf.Clamp01(_transitionProgress);

            CurrentWeather = WeatherConditionData.Lerp(_fromWeather, _targetWeather, _transitionProgress);
            CurrentWind    = WindData.FromSpeedAndDirection(CurrentWeather.windSpeed, CurrentWeather.windDirection);

            // Propagate to sub-systems each frame during transition
            precipitationSystem?.UpdatePrecipitation(CurrentWeather);
            fogController?.ApplyWeather(CurrentWeather);
            lightingController?.ApplyWeather(CurrentWeather);

            if (_transitionProgress >= 1f)
                OnWeatherChanged?.Invoke(CurrentWeather);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Immediately begins a transition toward the specified weather type and intensity.
        /// Disables live API updates until <see cref="ResetToLive"/> is called.
        /// </summary>
        /// <param name="type">Target weather type.</param>
        /// <param name="intensity">Target intensity (0–1).</param>
        public void ForceWeather(WeatherType type, float intensity)
        {
            _liveMode = false;
            var forced = WeatherConditionData.CreateClear();
            forced.type      = type;
            forced.intensity = Mathf.Clamp01(intensity);
            ApplyCondition(forced, immediate: false);
            Debug.Log($"[SWEF][WeatherManager] Forced weather: {type} @ {intensity:F2}");
        }

        /// <summary>Resumes API-driven weather updates and triggers an immediate re-fetch.</summary>
        public void ResetToLive()
        {
            _liveMode = true;
            apiClient?.FetchWeather(SWEF.Core.SWEFSession.Lat, SWEF.Core.SWEFSession.Lon);
            Debug.Log("[SWEF][WeatherManager] Resumed live weather.");
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void HandleAPIWeatherUpdate(WeatherConditionData condition)
        {
            if (!_liveMode) return;
            ApplyCondition(condition, immediate: false);
        }

        private void ApplyCondition(WeatherConditionData target, bool immediate)
        {
            _fromWeather        = CurrentWeather;
            _targetWeather      = target;
            _transitionProgress = immediate ? 1f : 0f;

            if (immediate)
            {
                CurrentWeather = target;
                CurrentWind    = WindData.FromSpeedAndDirection(target.windSpeed, target.windDirection);
                precipitationSystem?.UpdatePrecipitation(CurrentWeather);
                fogController?.ApplyWeather(CurrentWeather);
                lightingController?.ApplyWeather(CurrentWeather);
                OnWeatherChanged?.Invoke(CurrentWeather);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Force Rain (test)")]
        private void EditorForceRain()   => ForceWeather(WeatherType.Rain,        0.6f);

        [ContextMenu("Force Thunderstorm (test)")]
        private void EditorForceThunder() => ForceWeather(WeatherType.Thunderstorm, 0.9f);

        [ContextMenu("Force Snow (test)")]
        private void EditorForceSnow()   => ForceWeather(WeatherType.Snow,        0.5f);

        [ContextMenu("Force Clear (test)")]
        private void EditorForceClear()  => ForceWeather(WeatherType.Clear,       0f);

        [ContextMenu("Reset To Live Weather")]
        private void EditorResetToLive() => ResetToLive();
#endif
    }
}
