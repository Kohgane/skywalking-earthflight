using UnityEngine;

namespace SWEF.Weather
{
    /// <summary>
    /// Computes per-frame wind forces for use by <see cref="WeatherFlightModifier"/>.
    ///
    /// <para>Layered wind model:</para>
    /// <list type="bullet">
    ///   <item>Base vector from <see cref="WeatherManager.CurrentWind"/>.</item>
    ///   <item>Sinusoidal gust modulation at <see cref="WindData.gustFrequency"/>.</item>
    ///   <item>Perlin-noise turbulence scaled by <see cref="WindData.turbulenceIntensity"/>.</item>
    ///   <item>Altitude multiplier via <see cref="altitudeWindMultiplier"/> curve
    ///         (ground friction → free stream → jet-stream zone → thin atmosphere).</item>
    /// </list>
    /// </summary>
    public class WindSystem : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WindSystem Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private WeatherManager weatherManager;

        [Header("Altitude Multiplier")]
        [Tooltip("Maps altitude (km, X-axis) to wind speed multiplier (Y-axis).\n" +
                 "Default: ground friction (0.5) → free stream (1.0) → jet stream (2.0) → dropoff.")]
        [SerializeField] private AnimationCurve altitudeWindMultiplier = DefaultAltitudeCurve();

        [Header("Gust Model")]
        [Tooltip("Perlin noise speed for turbulence variation. Higher = faster variation.")]
        [SerializeField] private float turbulenceNoiseSpeed = 0.8f;

        [Tooltip("Additional random offset for turbulence noise seed.")]
        [SerializeField] private float noiseSeedOffset = 42f;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Current wind force in world space (m/s), incorporating gusts and turbulence.</summary>
        public Vector3 CurrentWindForce { get; private set; }

        /// <summary>Current turbulence intensity (0–1), updated each frame.</summary>
        public float CurrentTurbulence { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (weatherManager == null)
                weatherManager = FindFirstObjectByType<WeatherManager>();
        }

        private void Update()
        {
            if (weatherManager == null) return;

            var wind = weatherManager.CurrentWind;
            float altKm  = (float)(SWEF.Core.SWEFSession.Alt / 1000.0);
            float altMult = altitudeWindMultiplier.Evaluate(altKm);

            // Gust modulation: sinusoidal at gust frequency
            float gust = 0f;
            if (wind.gustFrequency > 0f)
                gust = Mathf.Sin(Time.time * wind.gustFrequency * Mathf.PI * 2f) * wind.gustSpeed;

            // Perlin turbulence
            float noiseX = Mathf.PerlinNoise(Time.time * turbulenceNoiseSpeed,        noiseSeedOffset)       - 0.5f;
            float noiseZ = Mathf.PerlinNoise(Time.time * turbulenceNoiseSpeed + 10f,  noiseSeedOffset + 10f) - 0.5f;
            float noiseY = Mathf.PerlinNoise(Time.time * turbulenceNoiseSpeed + 20f,  noiseSeedOffset + 20f) - 0.5f;

            float turbMag = wind.turbulenceIntensity * wind.speed;
            var turbulenceVec = new Vector3(noiseX, noiseY * 0.5f, noiseZ) * turbMag;

            // Compose final wind force
            Vector3 baseForce = wind.direction * ((wind.speed + gust) * altMult);
            CurrentWindForce  = baseForce + turbulenceVec;

            // Turbulence scalar for camera shake etc.
            CurrentTurbulence = Mathf.Clamp01(turbulenceVec.magnitude / Mathf.Max(1f, wind.speed + 0.01f));
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the wind force at a specific altitude, applying the <see cref="altitudeWindMultiplier"/>
        /// curve without modifying the current frame's cached value.
        /// </summary>
        /// <param name="altitudeMeters">Query altitude in metres.</param>
        public Vector3 GetWindAtAltitude(float altitudeMeters)
        {
            if (weatherManager == null) return Vector3.zero;
            var wind = weatherManager.CurrentWind;
            float mult = altitudeWindMultiplier.Evaluate(altitudeMeters / 1000f);
            return wind.direction * (wind.speed * mult);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static AnimationCurve DefaultAltitudeCurve()
        {
            // X = altitude in km, Y = wind multiplier
            // 0 km  → 0.5  (ground friction)
            // 2 km  → 1.0  (free-stream onset)
            // 10 km → 2.0  (jet-stream peak)
            // 15 km → 1.5  (jet-stream tailing)
            // 30 km → 0.5  (stratosphere thin air)
            // 80 km → 0.1  (near-space)
            return new AnimationCurve(
                new Keyframe(0f,   0.5f),
                new Keyframe(2f,   1.0f),
                new Keyframe(10f,  2.0f),
                new Keyframe(15f,  1.5f),
                new Keyframe(30f,  0.5f),
                new Keyframe(80f,  0.1f)
            );
        }
    }
}
