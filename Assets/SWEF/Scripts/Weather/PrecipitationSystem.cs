using UnityEngine;
using SWEF.Flight;

namespace SWEF.Weather
{
    /// <summary>
    /// Particle-based precipitation (rain, snow, hail) that follows the player camera
    /// and scales with weather intensity and altitude.
    ///
    /// <para>Particle systems are referenced via Inspector; create child GameObjects under
    /// this component with a <see cref="ParticleSystem"/> attached.  All particle systems
    /// are optional — the controller degrades gracefully when they are absent.</para>
    ///
    /// <para>Call <see cref="UpdatePrecipitation"/> each frame (or on weather change)
    /// with the current <see cref="WeatherConditionData"/>.</para>
    /// </summary>
    public class PrecipitationSystem : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static PrecipitationSystem Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [SerializeField] private ParticleSystem hailParticles;

        [Header("Follow Target")]
        [Tooltip("Transform to follow (usually the player camera). Auto-found if null.")]
        [SerializeField] private Transform followTarget;

        [Header("Emission Limits")]
        [Tooltip("Maximum rain particles per second at full intensity.")]
        [SerializeField] private float rainMaxEmission = 5000f;

        [Tooltip("Maximum snow particles per second at full intensity.")]
        [SerializeField] private float snowMaxEmission = 2000f;

        [Tooltip("Maximum hail particles per second at full intensity.")]
        [SerializeField] private float hailMaxEmission = 1000f;

        [Header("Altitude Fade")]
        [Tooltip("Altitude (metres) at which precipitation starts fading.")]
        [SerializeField] private float precipFadeStartAltitude = 10000f;

        [Tooltip("Altitude (metres) above which precipitation is fully disabled.")]
        [SerializeField] private float maxAltitudeForPrecipitation = 15000f;

        [Header("Wind Tilt")]
        [Tooltip("Scales wind influence on particle direction. Higher = more tilt in wind.")]
        [SerializeField] private float windTiltStrength = 0.5f;

        // ── Internal ──────────────────────────────────────────────────────────────
        private AltitudeController _altitudeSource;
        private WeatherConditionData _lastCondition;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _altitudeSource = FindFirstObjectByType<AltitudeController>();

            if (followTarget == null)
            {
                var cam = Camera.main;
                if (cam != null) followTarget = cam.transform;
            }

            StopAll();
        }

        private void LateUpdate()
        {
            if (followTarget != null)
                transform.position = followTarget.position;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates active particle systems to match the given <see cref="WeatherConditionData"/>.
        /// Should be called each frame or on weather change.
        /// </summary>
        public void UpdatePrecipitation(WeatherConditionData condition)
        {
            _lastCondition = condition;

            float altFactor = ComputeAltitudeFactor();
            float effectiveIntensity = condition.intensity * altFactor;

            // Disable all first, then enable appropriate system
            SetEmission(rainParticles, 0f);
            SetEmission(snowParticles, 0f);
            SetEmission(hailParticles, 0f);

            switch (condition.type)
            {
                case WeatherType.Drizzle:
                    SetEmission(rainParticles, rainMaxEmission * 0.15f * altFactor);
                    TiltParticles(rainParticles, condition);
                    break;

                case WeatherType.Rain:
                    SetEmission(rainParticles, rainMaxEmission * 0.4f * effectiveIntensity);
                    TiltParticles(rainParticles, condition);
                    break;

                case WeatherType.HeavyRain:
                case WeatherType.Thunderstorm:
                    SetEmission(rainParticles, rainMaxEmission * effectiveIntensity);
                    TiltParticles(rainParticles, condition);
                    break;

                case WeatherType.Sleet:
                    SetEmission(rainParticles, rainMaxEmission * 0.3f * effectiveIntensity);
                    SetEmission(snowParticles, snowMaxEmission * 0.3f * effectiveIntensity);
                    TiltParticles(rainParticles, condition);
                    TiltParticles(snowParticles, condition);
                    break;

                case WeatherType.Snow:
                    SetEmission(snowParticles, snowMaxEmission * 0.5f * effectiveIntensity);
                    TiltParticles(snowParticles, condition);
                    break;

                case WeatherType.HeavySnow:
                    SetEmission(snowParticles, snowMaxEmission * effectiveIntensity);
                    TiltParticles(snowParticles, condition);
                    break;

                case WeatherType.Hail:
                    SetEmission(hailParticles, hailMaxEmission * effectiveIntensity);
                    break;

                default:
                    // Clear, Fog, Cloudy, Mist, etc. — no precipitation
                    break;
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private float ComputeAltitudeFactor()
        {
            float alt = _altitudeSource != null
                ? _altitudeSource.CurrentAltitudeMeters
                : (float)SWEF.Core.SWEFSession.Alt;

            if (alt >= maxAltitudeForPrecipitation) return 0f;
            if (alt <= precipFadeStartAltitude)     return 1f;
            return 1f - (alt - precipFadeStartAltitude) /
                        (maxAltitudeForPrecipitation - precipFadeStartAltitude);
        }

        private static void SetEmission(ParticleSystem ps, float rate)
        {
            if (ps == null) return;

            var emission = ps.emission;
            if (rate <= 0f)
            {
                emission.enabled = false;
                if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            else
            {
                emission.enabled = true;
                emission.rateOverTime = rate;
                if (!ps.isPlaying) ps.Play();
            }
        }

        private void TiltParticles(ParticleSystem ps, WeatherConditionData condition)
        {
            if (ps == null) return;

            float rad = condition.windDirection * Mathf.Deg2Rad;
            // Wind direction is FROM, so flip for particle travel direction
            var windDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = windTiltStrength > 0f;
            velocity.x       = windDir.x * condition.windSpeed * windTiltStrength;
            velocity.z       = windDir.z * condition.windSpeed * windTiltStrength;
        }

        private void StopAll()
        {
            SetEmission(rainParticles, 0f);
            SetEmission(snowParticles, 0f);
            SetEmission(hailParticles, 0f);
        }
    }
}
