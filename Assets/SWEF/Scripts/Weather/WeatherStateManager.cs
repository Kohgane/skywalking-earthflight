using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Weather
{
    /// <summary>
    /// Singleton MonoBehaviour that owns the authoritative weather state for the current flight session.
    ///
    /// <para>Responsibilities:</para>
    /// <list type="bullet">
    ///   <item>Subscribe to <see cref="WeatherDataService.OnWeatherUpdated"/> and initiate smooth transitions.</item>
    ///   <item>Apply altitude-based weather modification (ground layer → stratosphere → near-space).</item>
    ///   <item>Expose <see cref="CurrentWeather"/>, <see cref="TargetWeather"/>, and
    ///         <see cref="TransitionProgress"/> for use by VFX, audio, and physics controllers.</item>
    /// </list>
    /// </summary>
    public class WeatherStateManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherStateManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Transition")]
        [Tooltip("Duration in seconds to blend from one weather state to the next.")]
        [SerializeField] private float transitionDuration = 10f;

        [Header("Altitude Zones (metres)")]
        [Tooltip("Below this altitude weather effects are fully applied.")]
        [SerializeField] private float groundZoneMax   = 2000f;

        [Tooltip("Transition zone upper boundary — clouds thin out, precipitation stops.")]
        [SerializeField] private float cloudZoneMax    = 10000f;

        [Tooltip("Stratosphere upper boundary — always clear above clouds.")]
        [SerializeField] private float stratosphereMax = 30000f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised each frame with the current blended <see cref="WeatherData"/>.</summary>
        public event Action<WeatherData> OnWeatherStateUpdated;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>The weather state before the ongoing transition.</summary>
        public WeatherData CurrentWeather { get; private set; }

        /// <summary>The target weather state that is being transitioned toward.</summary>
        public WeatherData TargetWeather  { get; private set; }

        /// <summary>Normalised progress of the current transition (0 = start, 1 = complete).</summary>
        public float TransitionProgress { get; private set; } = 1f;

        /// <summary>Altitude-adjusted blend of current and target weather, re-evaluated each frame.</summary>
        public WeatherData ActiveWeather  { get; private set; }

        /// <summary>Current altitude in metres (sourced from <c>SWEFSession.Alt</c>).</summary>
        public float AltitudeMeters => (float)SWEF.Core.SWEFSession.Alt;

        // ── Internal ──────────────────────────────────────────────────────────────
        private Coroutine _transitionCoroutine;

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

            CurrentWeather = WeatherData.CreateClear();
            TargetWeather  = WeatherData.CreateClear();
            ActiveWeather  = WeatherData.CreateClear();
        }

        private void Start()
        {
            if (WeatherDataService.Instance != null)
                WeatherDataService.Instance.OnWeatherUpdated += HandleWeatherUpdate;
            else
                Debug.LogWarning("[SWEF][WeatherStateManager] WeatherDataService not found — manual updates only.");
        }

        private void OnDestroy()
        {
            if (WeatherDataService.Instance != null)
                WeatherDataService.Instance.OnWeatherUpdated -= HandleWeatherUpdate;
        }

        private void Update()
        {
            // Re-apply altitude modifiers every frame to handle continuous ascent/descent
            ActiveWeather = ApplyAltitudeModifiers(BlendWeather(CurrentWeather, TargetWeather, TransitionProgress));
            OnWeatherStateUpdated?.Invoke(ActiveWeather);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Immediately applies a new target weather state and starts a smooth transition.
        /// </summary>
        /// <param name="newWeather">The desired weather to transition to.</param>
        public void SetTargetWeather(WeatherData newWeather)
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                // Snap current to the partial blend to avoid a jump
                CurrentWeather = BlendWeather(CurrentWeather, TargetWeather, TransitionProgress);
            }

            TargetWeather      = newWeather;
            TransitionProgress = 0f;

            _transitionCoroutine = StartCoroutine(TransitionCoroutine());
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void HandleWeatherUpdate(WeatherData data) => SetTargetWeather(data);

        private IEnumerator TransitionCoroutine()
        {
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed            += Time.deltaTime;
                TransitionProgress  = Mathf.Clamp01(elapsed / transitionDuration);
                yield return null;
            }

            TransitionProgress = 1f;
            CurrentWeather     = TargetWeather;
        }

        /// <summary>
        /// Linearly interpolates all numeric fields between two <see cref="WeatherData"/> instances.
        /// The <c>condition</c> snaps to <paramref name="to"/> once <paramref name="t"/> ≥ 0.5.
        /// </summary>
        private static WeatherData BlendWeather(WeatherData from, WeatherData to, float t)
        {
            return new WeatherData
            {
                condition              = t >= 0.5f ? to.condition : from.condition,
                temperatureCelsius     = Mathf.Lerp(from.temperatureCelsius,   to.temperatureCelsius,   t),
                humidity               = Mathf.Lerp(from.humidity,             to.humidity,             t),
                windSpeedMs            = Mathf.Lerp(from.windSpeedMs,          to.windSpeedMs,          t),
                windDirectionDeg       = Mathf.LerpAngle(from.windDirectionDeg, to.windDirectionDeg,    t),
                visibility             = Mathf.Lerp(from.visibility,           to.visibility,           t),
                cloudCoverage          = Mathf.Lerp(from.cloudCoverage,        to.cloudCoverage,        t),
                precipitationIntensity = Mathf.Lerp(from.precipitationIntensity, to.precipitationIntensity, t),
                lastUpdated            = to.lastUpdated
            };
        }

        /// <summary>
        /// Adjusts a blended <see cref="WeatherData"/> snapshot based on the current altitude zone.
        ///
        /// <list type="table">
        ///   <item><term>0 – groundZoneMax</term><description>Full weather effects.</description></item>
        ///   <item><term>groundZoneMax – cloudZoneMax</term><description>Transition: clouds and reduced precipitation.</description></item>
        ///   <item><term>cloudZoneMax – stratosphereMax</term><description>Clear above clouds; extreme cold.</description></item>
        ///   <item><term>stratosphereMax+</term><description>Near-space; no weather; star visibility.</description></item>
        /// </list>
        /// </summary>
        private WeatherData ApplyAltitudeModifiers(WeatherData data)
        {
            float alt = AltitudeMeters;

            if (alt < groundZoneMax)
            {
                // Full weather — no modification needed
                return data;
            }

            if (alt < cloudZoneMax)
            {
                // Transition zone: precipitation fades, clouds thin
                float zoneT = Mathf.InverseLerp(groundZoneMax, cloudZoneMax, alt);
                var modified = new WeatherData();
                CopyFields(data, modified);
                modified.precipitationIntensity = Mathf.Lerp(data.precipitationIntensity, 0f, zoneT);
                modified.cloudCoverage          = Mathf.Lerp(data.cloudCoverage,          0.3f, zoneT);
                modified.visibility             = Mathf.Lerp(data.visibility,             10000f, zoneT);
                // Suppress heavy-ground conditions
                if (modified.condition == WeatherCondition.HeavyRain  ||
                    modified.condition == WeatherCondition.Thunderstorm ||
                    modified.condition == WeatherCondition.Hail         ||
                    modified.condition == WeatherCondition.HeavySnow)
                    modified.condition = WeatherCondition.Cloudy;
                return modified;
            }

            if (alt < stratosphereMax)
            {
                // Stratosphere: always clear above the cloud layer, extreme cold
                float zoneT = Mathf.InverseLerp(cloudZoneMax, stratosphereMax, alt);
                var modified = new WeatherData();
                CopyFields(data, modified);
                modified.condition              = WeatherCondition.Clear;
                modified.precipitationIntensity = 0f;
                modified.cloudCoverage          = Mathf.Lerp(0.1f, 0f, zoneT);
                modified.visibility             = 10000f;
                modified.temperatureCelsius     = Mathf.Lerp(data.temperatureCelsius, -60f, zoneT);
                return modified;
            }

            // Near-space: no weather at all
            var space = WeatherData.CreateClear();
            space.temperatureCelsius = -80f;
            space.windSpeedMs        = 0f;
            return space;
        }

        private static void CopyFields(WeatherData src, WeatherData dst)
        {
            dst.condition              = src.condition;
            dst.temperatureCelsius     = src.temperatureCelsius;
            dst.humidity               = src.humidity;
            dst.windSpeedMs            = src.windSpeedMs;
            dst.windDirectionDeg       = src.windDirectionDeg;
            dst.visibility             = src.visibility;
            dst.cloudCoverage          = src.cloudCoverage;
            dst.precipitationIntensity = src.precipitationIntensity;
            dst.lastUpdated            = src.lastUpdated;
        }
    }
}
