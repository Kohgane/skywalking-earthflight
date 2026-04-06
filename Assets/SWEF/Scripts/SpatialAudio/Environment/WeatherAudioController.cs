// WeatherAudioController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Weather sounds: rain intensity layers, thunder with distance delay, hail, wind gusts.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Controls weather-based audio: rain intensity layers, thunder with propagation
    /// delay, hail impacts, and wind gusts. Integrates with the weather system.
    /// </summary>
    public class WeatherAudioController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Rain Sources")]
        [SerializeField] private AudioSource lightRainSource;
        [SerializeField] private AudioSource heavyRainSource;

        [Header("Thunder")]
        [SerializeField] private AudioSource thunderSource;
        [SerializeField] private AudioClip   thunderClip;
        [Tooltip("Speed of sound used for thunder delay calculation (m/s).")]
        [Range(200f, 400f)] public float speedOfSound = 343f;

        [Header("Hail")]
        [SerializeField] private AudioSource hailSource;

        [Header("Wind Gusts")]
        [SerializeField] private AudioSource windGustSource;

        // ── State ─────────────────────────────────────────────────────────────────

        private float _rainIntensity;

        /// <summary>Current rain intensity (0–1).</summary>
        public float RainIntensity => _rainIntensity;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets rain audio intensity (0 = none, 1 = heavy downpour).
        /// </summary>
        public void SetRainIntensity(float intensity)
        {
            _rainIntensity = Mathf.Clamp01(intensity);
            float lightVol  = Mathf.InverseLerp(0f, 0.5f, _rainIntensity);
            float heavyVol  = Mathf.InverseLerp(0.4f, 1f, _rainIntensity);

            SetSource(lightRainSource, lightVol);
            SetSource(heavyRainSource, heavyVol);
        }

        /// <summary>
        /// Triggers a thunder clap with a distance-based propagation delay.
        /// </summary>
        /// <param name="distanceMetres">Distance to lightning strike in metres.</param>
        public void TriggerThunder(float distanceMetres)
        {
            float delay = distanceMetres / Mathf.Max(1f, speedOfSound);
            StartCoroutine(PlayThunderDelayed(delay));
        }

        /// <summary>
        /// Sets hail impact intensity (0 = none, 1 = severe hail).
        /// </summary>
        public void SetHailIntensity(float intensity)
        {
            SetSource(hailSource, Mathf.Clamp01(intensity));
        }

        /// <summary>
        /// Sets wind gust audio intensity (0 = calm, 1 = storm-force gusts).
        /// </summary>
        public void SetWindGustIntensity(float intensity)
        {
            if (windGustSource == null) return;
            windGustSource.volume = Mathf.Clamp01(intensity);
            windGustSource.pitch  = 0.9f + intensity * 0.3f;
            if (!windGustSource.isPlaying && windGustSource.clip != null && intensity > 0.01f)
                windGustSource.Play();
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private System.Collections.IEnumerator PlayThunderDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (thunderSource != null && thunderClip != null)
                thunderSource.PlayOneShot(thunderClip);
        }

        private static void SetSource(AudioSource src, float vol)
        {
            if (src == null) return;
            src.volume = vol;
            if (!src.isPlaying && src.clip != null && vol > 0.001f)
            {
                src.loop = true;
                src.Play();
            }
        }
    }
}
