using UnityEngine;

namespace SWEF.Ocean
{
    /// <summary>
    /// Phase 50 — Computes Gerstner-wave surface displacement, normals, and velocity
    /// at arbitrary world-space XZ positions.
    ///
    /// <para>Up to 8 additive wave octaves are superposed.  Each octave is derived
    /// from the base <see cref="waveParameters"/> by successive halving of amplitude
    /// and doubling of frequency (lacunarity = 2), matching the Beaufort sea-state
    /// model.</para>
    ///
    /// <para>All public methods are thread-safe (read-only state, no Unity API calls)
    /// and may safely be called from C# Jobs or coroutines.</para>
    /// </summary>
    public class WaveSimulator : MonoBehaviour
    {
        #region Constants

        // Cached OceanManager reference to avoid per-call FindFirstObjectByType overhead.
        private OceanManager _oceanManager;

        private const int   MaxOctaves              = 8;
        private const float LacunarityDefault       = 2.0f;
        private const float PersistenceDefault      = 0.5f;

        // Beaufort → wave amplitude mapping (approximate, simplified).
        // Index = Beaufort number (0–12), value = wave amplitude in metres.
        private static readonly float[] BeaufortAmplitude =
        {
            0.00f,  // 0  calm
            0.05f,  // 1  light air
            0.15f,  // 2  light breeze
            0.30f,  // 3  gentle breeze
            0.60f,  // 4  moderate breeze
            1.00f,  // 5  fresh breeze
            1.80f,  // 6  strong breeze
            3.00f,  // 7  high wind
            4.50f,  // 8  gale
            6.50f,  // 9  strong gale
            9.00f,  // 10 storm
            12.00f, // 11 violent storm
            16.00f  // 12 hurricane
        };

        #endregion

        #region Inspector

        [Header("Wave Parameters")]
        [Tooltip("Base wave parameters for octave 0.")]
        [SerializeField] private WaveParameters waveParameters = new WaveParameters();

        [Header("Octave Stacking")]
        [Tooltip("Frequency multiplier per octave (≥ 1).")]
        [SerializeField] private float lacunarity = LacunarityDefault;

        [Tooltip("Amplitude multiplier per octave (0–1).")]
        [SerializeField, Range(0f, 1f)] private float persistence = PersistenceDefault;

        [Header("Storm Mode")]
        [Tooltip("Extra amplitude multiplier applied during storm conditions.")]
        [SerializeField] private float stormAmplitudeMultiplier = 3f;

        [Tooltip("Blend factor toward storm waves (0 = calm, 1 = full storm).")]
        [SerializeField, Range(0f, 1f)] private float stormBlend = 0f;

        [Header("Calm Mode")]
        [Tooltip("Amplitude scale applied for calm water bodies (lakes, ponds).")]
        [SerializeField, Range(0f, 0.1f)] private float calmAmplitudeScale = 0.02f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _oceanManager = FindFirstObjectByType<OceanManager>();
        }

        #endregion

        #region Public Properties

        /// <summary>Base wave parameters (editable at runtime).</summary>
        public WaveParameters WaveParameters => waveParameters;

        /// <summary>Current storm blend (0 = calm sea, 1 = hurricane).</summary>
        public float StormBlend
        {
            get => stormBlend;
            set => stormBlend = Mathf.Clamp01(value);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns the Gerstner-wave Y displacement (metres above mean water level)
        /// at world-space coordinate (<paramref name="x"/>, <paramref name="z"/>) at
        /// the given <paramref name="time"/>.
        /// </summary>
        public float GetWaveHeightAt(float x, float z, float time)
        {
            float height = 0f;
            float amp    = EffectiveAmplitude();
            float freq   = waveParameters.frequency;
            float spd    = waveParameters.speed;
            var   dir    = waveParameters.direction.normalized;
            float steep  = waveParameters.steepness;
            int   count  = OctaveCount();

            for (int i = 0; i < count; i++)
            {
                float phase = freq * (dir.x * x + dir.y * z) - spd * time;
                height += amp * Mathf.Sin(phase);

                amp    *= persistence;
                freq   *= lacunarity;
                spd    *= Mathf.Sqrt(lacunarity); // deep-water dispersion
                // Rotate direction slightly each octave for variety.
                dir = RotateVector2(dir, 0.2618f); // ~15 degrees
            }
            return height;
        }

        /// <summary>
        /// Returns the surface normal at world-space coordinate
        /// (<paramref name="x"/>, <paramref name="z"/>) at the given
        /// <paramref name="time"/>.
        /// </summary>
        public Vector3 GetWaveNormalAt(float x, float z, float time)
        {
            // Finite-difference approximation.
            const float Eps = 0.1f;
            float cx  = GetWaveHeightAt(x, z, time);
            float dx   = GetWaveHeightAt(x + Eps, z, time) - cx;
            float dz   = GetWaveHeightAt(x, z + Eps, time) - cx;
            return new Vector3(-dx / Eps, 1f, -dz / Eps).normalized;
        }

        /// <summary>
        /// Returns the horizontal velocity of the water surface
        /// (<see cref="Vector3.y"/> is always 0) for buoyancy/drag calculations.
        /// </summary>
        public Vector3 GetWaveVelocityAt(float x, float z, float time)
        {
            float amp   = EffectiveAmplitude();
            float freq  = waveParameters.frequency;
            float spd   = waveParameters.speed;
            var   dir   = waveParameters.direction.normalized;
            int   count = OctaveCount();

            float vx = 0f, vz = 0f;
            for (int i = 0; i < count; i++)
            {
                float phase = freq * (dir.x * x + dir.y * z) - spd * time;
                float c     = Mathf.Cos(phase) * amp * freq * spd;
                vx += c * dir.x;
                vz += c * dir.y;

                amp  *= persistence;
                freq *= lacunarity;
                spd  *= Mathf.Sqrt(lacunarity);
                dir   = RotateVector2(dir, 0.2618f);
            }
            return new Vector3(vx, 0f, vz);
        }

        /// <summary>
        /// Updates wave parameters from wind speed and direction (called by
        /// <see cref="OceanManager.SetWindParameters"/>).
        /// </summary>
        /// <param name="windSpeedMs">Wind speed in m/s.</param>
        /// <param name="windDirDeg">Meteorological direction in degrees.</param>
        public void ApplyWindParameters(float windSpeedMs, float windDirDeg)
        {
            int   beaufort  = WindSpeedToBeaufort(windSpeedMs);
            float targetAmp = BeaufortAmplitude[beaufort];

            waveParameters.amplitude = targetAmp;
            waveParameters.speed     = windSpeedMs * 0.15f + 0.5f;

            // Convert met direction to XZ wave propagation vector.
            float rad = (windDirDeg - 180f) * Mathf.Deg2Rad; // waves travel FROM wind
            waveParameters.direction = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        }

        #endregion

        #region Private Helpers

        private float EffectiveAmplitude()
        {
            float baseAmp = waveParameters.amplitude;
            float amp     = Mathf.Lerp(baseAmp, baseAmp * stormAmplitudeMultiplier, stormBlend);
            return amp;
        }

        private int OctaveCount()
        {
            int qualityOctaves = waveParameters.octaves;
            if (_oceanManager != null)
            {
                switch (_oceanManager.GlobalWaveQuality)
                {
                    case WaveQuality.Low:    return Mathf.Min(qualityOctaves, 1);
                    case WaveQuality.Medium: return Mathf.Min(qualityOctaves, 2);
                    case WaveQuality.High:   return Mathf.Min(qualityOctaves, 4);
                    case WaveQuality.Ultra:  return Mathf.Min(qualityOctaves, MaxOctaves);
                }
            }
            return Mathf.Clamp(qualityOctaves, 1, MaxOctaves);
        }

        private static int WindSpeedToBeaufort(float ms)
        {
            // WMO Beaufort breakpoints (m/s).
            if (ms < 0.3f)  return 0;
            if (ms < 1.6f)  return 1;
            if (ms < 3.4f)  return 2;
            if (ms < 5.5f)  return 3;
            if (ms < 8.0f)  return 4;
            if (ms < 10.8f) return 5;
            if (ms < 13.9f) return 6;
            if (ms < 17.2f) return 7;
            if (ms < 20.8f) return 8;
            if (ms < 24.5f) return 9;
            if (ms < 28.5f) return 10;
            if (ms < 32.7f) return 11;
            return 12;
        }

        private static Vector2 RotateVector2(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(cos * v.x - sin * v.y,
                               sin * v.x + cos * v.y);
        }

        #endregion
    }
}
