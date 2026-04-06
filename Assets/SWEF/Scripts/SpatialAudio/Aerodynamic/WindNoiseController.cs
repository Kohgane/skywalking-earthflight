// WindNoiseController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Speed-dependent wind noise: laminar flow, turbulent buffeting, Mach effects.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Generates speed-dependent aerodynamic wind noise.
    /// Three regimes: laminar flow (low speed), turbulent buffeting (medium-high speed),
    /// and Mach-related effects approaching the sound barrier.
    /// </summary>
    public class WindNoiseController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource laminarSource;
        [SerializeField] private AudioSource turbulentSource;
        [SerializeField] private AudioSource machEffectSource;

        [Header("Wind Profile")]
        [SerializeField] private WindNoiseProfile profile = new WindNoiseProfile
        {
            laminarOnsetSpeed   = 10f,
            turbulentOnsetSpeed = 80f,
            machOnsetSpeed      = 300f,
            maxWindVolume       = 1f,
            maxPitchShift       = 0.5f
        };

        // ── State ─────────────────────────────────────────────────────────────────

        private float _currentSpeedMs;

        /// <summary>Current aircraft airspeed in m/s.</summary>
        public float CurrentSpeedMs => _currentSpeedMs;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates wind noise audio based on current airspeed.
        /// </summary>
        /// <param name="speedMs">Current airspeed in m/s.</param>
        public void UpdateWindNoise(float speedMs)
        {
            _currentSpeedMs = Mathf.Max(0f, speedMs);

            float masterVol = config != null ? config.windNoiseVolume : 1f;

            float laminarVol   = CalculateLaminarVolume()   * masterVol;
            float turbulentVol = CalculateTurbulentVolume() * masterVol;
            float machVol      = CalculateMachVolume()      * masterVol;

            float pitchShift = 1f + Mathf.InverseLerp(profile.laminarOnsetSpeed, profile.machOnsetSpeed, _currentSpeedMs) * profile.maxPitchShift;

            SetSource(laminarSource,   laminarVol,   pitchShift);
            SetSource(turbulentSource, turbulentVol, pitchShift * 0.9f);
            SetSource(machEffectSource, machVol,     pitchShift * 1.2f);
        }

        /// <summary>
        /// Calculates laminar wind volume for the given speed.
        /// </summary>
        public float CalculateLaminarVolume()
        {
            return Mathf.Clamp01(
                Mathf.InverseLerp(profile.laminarOnsetSpeed, profile.turbulentOnsetSpeed, _currentSpeedMs));
        }

        /// <summary>
        /// Calculates turbulent buffeting volume for the given speed.
        /// </summary>
        public float CalculateTurbulentVolume()
        {
            float t = Mathf.InverseLerp(profile.turbulentOnsetSpeed, profile.machOnsetSpeed, _currentSpeedMs);
            return Mathf.Clamp01(t) * profile.maxWindVolume;
        }

        /// <summary>
        /// Calculates Mach-effect volume for the given speed.
        /// </summary>
        public float CalculateMachVolume()
        {
            return Mathf.Clamp01(
                Mathf.InverseLerp(profile.machOnsetSpeed, profile.machOnsetSpeed * 1.5f, _currentSpeedMs));
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private static void SetSource(AudioSource src, float vol, float pitch)
        {
            if (src == null) return;
            src.volume = vol;
            src.pitch  = pitch;
            if (!src.isPlaying && src.clip != null && vol > 0.001f) src.Play();
        }
    }
}
