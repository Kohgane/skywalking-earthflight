// CityAudioController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Urban soundscape: traffic, sirens, construction at low altitude, fading with altitude.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Generates urban city ambient audio that fades realistically with altitude.
    /// Includes layers for traffic noise, sirens, and construction sounds.
    /// </summary>
    public class CityAudioController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("City Audio Sources")]
        [SerializeField] private AudioSource trafficSource;
        [SerializeField] private AudioSource sirenSource;
        [SerializeField] private AudioSource constructionSource;
        [SerializeField] private AudioSource crowdSource;

        // ── State ─────────────────────────────────────────────────────────────────

        private float _altitudeAgl;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates city audio based on altitude above ground.
        /// </summary>
        /// <param name="altitudeAgl">Altitude above ground in metres.</param>
        public void UpdateAltitude(float altitudeAgl)
        {
            _altitudeAgl = Mathf.Max(0f, altitudeAgl);

            float fadeAlt = config != null ? config.cityAudioFadeAltitude : 500f;
            float master  = config != null ? config.ambientVolume : 0.4f;

            float fade = 1f - Mathf.Clamp01(_altitudeAgl / fadeAlt);

            SetSource(trafficSource,      0.8f * fade * master);
            SetSource(sirenSource,        0.5f * fade * master);
            SetSource(constructionSource, 0.4f * fade * master);
            SetSource(crowdSource,        0.3f * fade * master);
        }

        /// <summary>Temporarily boosts siren volume (e.g. emergency vehicle nearby).</summary>
        public void TriggerSirenEvent(float boostVolume = 0.9f, float duration = 10f)
        {
            if (sirenSource == null) return;
            sirenSource.volume = boostVolume;
        }

        // ── Private ───────────────────────────────────────────────────────────────

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
