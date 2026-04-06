// AudioPropagationEngine.cs — Phase 118: Spatial Audio & 3D Soundscape
// Realistic sound propagation: distance attenuation, atmospheric absorption,
// temperature and humidity effects on speed of sound.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Calculates physically-based sound propagation parameters including distance
    /// attenuation, atmospheric absorption by frequency, and speed-of-sound
    /// variations due to temperature and humidity.
    /// </summary>
    public class AudioPropagationEngine : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Atmosphere")]
        [Tooltip("Air temperature in degrees Celsius.")]
        [Range(-60f, 50f)] public float temperatureCelsius = 15f;

        [Tooltip("Relative humidity as a fraction (0–1).")]
        [Range(0f, 1f)] public float relativeHumidity = 0.5f;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Calculates the speed of sound for the current temperature and humidity.
        /// Uses the simplified formula: c = 331.3 * sqrt(1 + T/273.15) + humidity correction.
        /// </summary>
        public float CalculateSpeedOfSound()
        {
            float c = 331.3f * Mathf.Sqrt(1f + temperatureCelsius / 273.15f);
            // Humidity adds approximately 0.6 m/s per 10% RH
            c += relativeHumidity * 0.6f;
            return c;
        }

        /// <summary>
        /// Calculates the volume attenuation factor for a given distance and propagation model.
        /// </summary>
        /// <param name="distance">Distance in metres from source to listener.</param>
        /// <param name="model">Attenuation model to apply.</param>
        /// <returns>Volume multiplier in range [0, 1].</returns>
        public float CalculateAttenuation(float distance, SoundPropagationModel model)
        {
            float minDist = config != null ? config.minDistance  : 5f;
            float maxDist = config != null ? config.maxDistance  : 5000f;

            if (distance <= minDist) return 1f;
            if (distance >= maxDist) return 0f;

            float t = (distance - minDist) / (maxDist - minDist);

            switch (model)
            {
                case SoundPropagationModel.Linear:
                    return 1f - t;

                case SoundPropagationModel.Logarithmic:
                    // Inverse-square approximation mapped 0→1
                    return minDist / Mathf.Max(minDist, distance);

                case SoundPropagationModel.Realistic:
                    float logAtten  = minDist / Mathf.Max(minDist, distance);
                    float atmosAbs  = CalculateAtmosphericAbsorption(distance);
                    return Mathf.Clamp01(logAtten * atmosAbs);

                default:
                    return 1f - t;
            }
        }

        /// <summary>
        /// Estimates atmospheric absorption factor for the given distance.
        /// Higher humidity and shorter distances yield less absorption.
        /// </summary>
        public float CalculateAtmosphericAbsorption(float distanceMetres)
        {
            // Simple model: absorption coefficient varies with humidity
            // Dry air absorbs more; humid air slightly less
            float alpha = Mathf.Lerp(0.002f, 0.0005f, relativeHumidity); // dB/m approx
            float dbLoss = alpha * distanceMetres;
            return Mathf.Pow(10f, -dbLoss / 20f); // convert dB to linear
        }

        /// <summary>
        /// Calculates the propagation delay in seconds for a sound to travel the given distance.
        /// </summary>
        public float CalculatePropagationDelay(float distanceMetres)
        {
            return distanceMetres / Mathf.Max(1f, CalculateSpeedOfSound());
        }
    }
}
