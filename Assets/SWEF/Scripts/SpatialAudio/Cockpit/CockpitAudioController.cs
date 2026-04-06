// CockpitAudioController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Cockpit-specific sounds: avionics hum, warning systems, radio chatter, switch clicks.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Manages cockpit-specific audio layers: avionics hum, gyro whir, switch clicks
    /// and pressurisation sounds. Runs continuously while in cockpit view.
    /// </summary>
    public class CockpitAudioController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Ambient Sources")]
        [SerializeField] private AudioSource avionicsHumSource;
        [SerializeField] private AudioSource gyroWhirSource;
        [SerializeField] private AudioSource pressurisationSource;

        [Header("One-Shot Clips")]
        [SerializeField] private AudioClip switchClickClip;
        [SerializeField] private AudioClip buttonPressClip;
        [SerializeField] private AudioClip landingGearLockClip;
        [SerializeField] private AudioSource oneShotSource;

        // ── State ─────────────────────────────────────────────────────────────────

        private bool _cockpitActive;

        /// <summary>Whether cockpit ambient audio is currently active.</summary>
        public bool IsCockpitActive => _cockpitActive;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            StartCockpitAmbience();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Starts all cockpit ambient audio layers.</summary>
        public void StartCockpitAmbience()
        {
            _cockpitActive = true;
            float vol = config != null ? config.cockpitAmbientVolume : 0.3f;
            StartSource(avionicsHumSource,    vol * 0.8f);
            StartSource(gyroWhirSource,       vol * 0.5f);
            StartSource(pressurisationSource, vol * 0.3f);
        }

        /// <summary>Stops all cockpit ambient audio layers.</summary>
        public void StopCockpitAmbience()
        {
            _cockpitActive = false;
            StopSource(avionicsHumSource);
            StopSource(gyroWhirSource);
            StopSource(pressurisationSource);
        }

        /// <summary>Plays an avionics switch click sound.</summary>
        public void PlaySwitchClick() => PlayOneShot(switchClickClip);

        /// <summary>Plays a cockpit button press sound.</summary>
        public void PlayButtonPress() => PlayOneShot(buttonPressClip);

        /// <summary>Plays the landing gear lock sound.</summary>
        public void PlayGearLock() => PlayOneShot(landingGearLockClip);

        // ── Private ───────────────────────────────────────────────────────────────

        private static void StartSource(AudioSource src, float vol)
        {
            if (src == null || src.clip == null) return;
            src.volume = vol;
            src.loop   = true;
            if (!src.isPlaying) src.Play();
        }

        private static void StopSource(AudioSource src)
        {
            if (src == null) return;
            src.Stop();
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (oneShotSource == null || clip == null) return;
            oneShotSource.PlayOneShot(clip);
        }
    }
}
