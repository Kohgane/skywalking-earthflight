// AirportAmbientController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Airport environment audio: terminal announcements, jet noise, ground vehicles, ATC chatter.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Manages airport ambient soundscape: terminal PA announcements, distant jet noise,
    /// ground vehicle sounds and ATC radio chatter. Fades with altitude.
    /// </summary>
    public class AirportAmbientController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Ambient Sources")]
        [SerializeField] private AudioSource terminalAmbienceSource;
        [SerializeField] private AudioSource jetNoiseSource;
        [SerializeField] private AudioSource groundVehicleSource;
        [SerializeField] private AudioSource atcChatterSource;

        [Header("Altitude Fade")]
        [Tooltip("Altitude (m AGL) above which airport sounds begin to fade.")]
        [Range(0f, 2000f)] public float fadeStartAltitude = 200f;
        [Tooltip("Altitude (m AGL) at which airport sounds are fully silent.")]
        [Range(100f, 5000f)] public float fadeEndAltitude  = 800f;

        // ── State ─────────────────────────────────────────────────────────────────

        private float _altitudeAgl;
        private bool  _isActive;

        /// <summary>Whether airport ambient audio is currently active.</summary>
        public bool IsActive => _isActive;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Activates airport ambient audio at full volume.</summary>
        public void Activate() => _isActive = true;

        /// <summary>Deactivates airport ambient audio.</summary>
        public void Deactivate()
        {
            _isActive = false;
            SetAllVolume(0f);
        }

        /// <summary>
        /// Updates ambient volume based on current altitude above ground.
        /// </summary>
        public void UpdateAltitude(float altitudeAgl)
        {
            _altitudeAgl = Mathf.Max(0f, altitudeAgl);
            if (!_isActive)
            {
                SetAllVolume(0f);
                return;
            }

            float fade = 1f - Mathf.Clamp01(
                Mathf.InverseLerp(fadeStartAltitude, fadeEndAltitude, _altitudeAgl));

            float master = config != null ? config.ambientVolume : 0.4f;

            SetSourceVolume(terminalAmbienceSource, 0.6f * fade * master);
            SetSourceVolume(jetNoiseSource,         0.9f * fade * master);
            SetSourceVolume(groundVehicleSource,    0.5f * fade * master);
            SetSourceVolume(atcChatterSource,       0.4f * fade * master);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void SetAllVolume(float vol)
        {
            SetSourceVolume(terminalAmbienceSource, vol);
            SetSourceVolume(jetNoiseSource,         vol);
            SetSourceVolume(groundVehicleSource,    vol);
            SetSourceVolume(atcChatterSource,       vol);
        }

        private static void SetSourceVolume(AudioSource src, float vol)
        {
            if (src == null) return;
            src.volume = vol;
        }
    }
}
