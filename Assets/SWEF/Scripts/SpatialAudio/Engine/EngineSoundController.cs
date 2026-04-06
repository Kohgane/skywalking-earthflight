// EngineSoundController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Dynamic engine audio controller: RPM-based pitch/volume with multi-layer support.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Controls engine audio output driven by throttle and RPM inputs.
    /// Supports multi-layer engine sounds (idle, cruise, full throttle, afterburner)
    /// blended smoothly via the <see cref="EngineAudioLayerMixer"/>.
    /// </summary>
    public class EngineSoundController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Engine Profile")]
        [SerializeField] private EngineAudioProfile profile;

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource idleSource;
        [SerializeField] private AudioSource cruiseSource;
        [SerializeField] private AudioSource fullThrottleSource;
        [SerializeField] private AudioSource afterburnerSource;

        // ── State ─────────────────────────────────────────────────────────────────

        private float _currentThrottle;
        private float _currentRpm;
        private float _targetPitch;
        private float _targetVolume;
        private bool  _afterburnerActive;

        /// <summary>Current normalised throttle position (0–1).</summary>
        public float Throttle => _currentThrottle;

        /// <summary>Current RPM value.</summary>
        public float CurrentRpm => _currentRpm;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates engine audio driven by throttle input and simulated RPM.
        /// </summary>
        /// <param name="throttle">Normalised throttle (0–1).</param>
        /// <param name="rpm">Current engine RPM.</param>
        /// <param name="afterburner">Whether afterburner is engaged.</param>
        public void UpdateEngineAudio(float throttle, float rpm, bool afterburner = false)
        {
            _currentThrottle   = Mathf.Clamp01(throttle);
            _currentRpm        = rpm;
            _afterburnerActive = afterburner;

            if (profile == null) return;

            float t           = Mathf.InverseLerp(profile.idleRpm, profile.maxRpm, rpm);
            _targetPitch      = Mathf.Lerp(profile.idlePitch,  profile.maxPitch,  t);
            _targetVolume     = Mathf.Lerp(profile.idleVolume, profile.maxVolume, _currentThrottle);

            float slewRate = config != null ? config.enginePitchSlewRate : 2f;

            ApplyToSource(idleSource,        1f - _currentThrottle,  _targetPitch, slewRate);
            ApplyToSource(cruiseSource,      CruiseWeight(),          _targetPitch, slewRate);
            ApplyToSource(fullThrottleSource, FullThrottleWeight(),   _targetPitch, slewRate);

            if (afterburnerSource != null && profile.hasAfterburner)
            {
                float abVol = _afterburnerActive ? profile.afterburnerVolumeBoost : 0f;
                ApplyToSource(afterburnerSource, abVol, _targetPitch * 1.1f, slewRate);
            }
        }

        /// <summary>Mutes all engine audio layers instantly.</summary>
        public void MuteAll()
        {
            SetSourceVolume(idleSource,         0f);
            SetSourceVolume(cruiseSource,        0f);
            SetSourceVolume(fullThrottleSource,  0f);
            SetSourceVolume(afterburnerSource,   0f);
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private float CruiseWeight()
        {
            return Mathf.Clamp01(Mathf.InverseLerp(0.1f, 0.7f, _currentThrottle));
        }

        private float FullThrottleWeight()
        {
            return Mathf.Clamp01(Mathf.InverseLerp(0.6f, 1f, _currentThrottle));
        }

        private void ApplyToSource(AudioSource src, float vol, float pitch, float slewRate)
        {
            if (src == null) return;
            src.volume = Mathf.MoveTowards(src.volume, vol * _targetVolume, slewRate * Time.deltaTime);
            src.pitch  = Mathf.MoveTowards(src.pitch,  pitch,              slewRate * Time.deltaTime);
            if (!src.isPlaying && src.clip != null && vol > 0.01f) src.Play();
        }

        private void SetSourceVolume(AudioSource src, float vol)
        {
            if (src == null) return;
            src.volume = vol;
        }
    }
}
