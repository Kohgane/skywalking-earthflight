// WildlifeAudioController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Wildlife sounds: birds at altitude, seagulls near coast, crickets at night.
// Namespace: SWEF.SpatialAudio

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Plays altitude- and time-of-day-sensitive wildlife ambient sounds:
    /// bird calls during transitions, seagulls near coast, crickets at night.
    /// </summary>
    public class WildlifeAudioController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Wildlife Sources")]
        [SerializeField] private AudioSource altitudeBirdsSource;
        [SerializeField] private AudioSource coastalSeagullSource;
        [SerializeField] private AudioSource nightCricketsSource;
        [SerializeField] private AudioSource forestBirdsSource;
        [SerializeField] private AudioSource tropicalSource;

        [Header("Altitude Thresholds")]
        [Tooltip("Maximum altitude (m AGL) for bird calls.")]
        [Range(100f, 3000f)] public float birdMaxAltitude = 500f;
        [Tooltip("Maximum altitude (m AGL) for seagull calls.")]
        [Range(10f, 500f)]  public float seagullMaxAltitude = 200f;

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        // ── State ─────────────────────────────────────────────────────────────────

        private float _altitudeAgl;
        private float _timeOfDayNorm; // 0 = midnight, 0.5 = noon
        private bool  _nearCoast;
        private WildlifeZone _activeZone = WildlifeZone.Altitude;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates wildlife audio based on altitude, time and coastal proximity.
        /// </summary>
        /// <param name="altitudeAgl">Altitude above ground in metres.</param>
        /// <param name="timeOfDayNorm">Normalised time of day (0–1, 0=midnight).</param>
        /// <param name="nearCoast">Whether the aircraft is near a coastline.</param>
        public void UpdateWildlifeAudio(float altitudeAgl, float timeOfDayNorm, bool nearCoast)
        {
            _altitudeAgl  = Mathf.Max(0f, altitudeAgl);
            _timeOfDayNorm = Mathf.Clamp01(timeOfDayNorm);
            _nearCoast    = nearCoast;

            float master = config != null ? config.ambientVolume : 0.4f;
            bool  isNight = _timeOfDayNorm < 0.2f || _timeOfDayNorm > 0.8f;

            float birdFade     = 1f - Mathf.Clamp01(_altitudeAgl / birdMaxAltitude);
            float seagullFade  = _nearCoast ? 1f - Mathf.Clamp01(_altitudeAgl / seagullMaxAltitude) : 0f;
            float cricketVol   = isNight ? 0.6f * master : 0f;
            float altBirdVol   = birdFade * (isNight ? 0f : 0.5f) * master;
            float seagullVol   = seagullFade * (isNight ? 0f : 0.7f) * master;

            SetSource(altitudeBirdsSource,   altBirdVol);
            SetSource(coastalSeagullSource,  seagullVol);
            SetSource(nightCricketsSource,   cricketVol);
        }

        /// <summary>Sets the active wildlife zone (for forest/tropical biomes).</summary>
        public void SetWildlifeZone(WildlifeZone zone)
        {
            _activeZone = zone;
            float master = config != null ? config.ambientVolume : 0.4f;
            SetSource(forestBirdsSource, zone == WildlifeZone.Forest  ? 0.6f * master : 0f);
            SetSource(tropicalSource,    zone == WildlifeZone.Tropical ? 0.7f * master : 0f);
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
