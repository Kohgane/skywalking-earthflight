// AerodynamicSoundGenerator.cs — Phase 118: Spatial Audio & 3D Soundscape
// Aerodynamic event sounds: flap, gear, speed brake, buffet, stall warning.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Generates one-shot and looping aerodynamic event sounds including flap
    /// deployment, gear extension, speed brake deployment, control surface buffet
    /// and stall warning buffet.
    /// </summary>
    public class AerodynamicSoundGenerator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Event Clips")]
        [SerializeField] private AudioClip flapDeployClip;
        [SerializeField] private AudioClip flapRetractClip;
        [SerializeField] private AudioClip gearExtendClip;
        [SerializeField] private AudioClip gearRetractClip;
        [SerializeField] private AudioClip speedBrakeDeployClip;
        [SerializeField] private AudioClip speedBrakeRetractClip;

        [Header("Looping Clips")]
        [SerializeField] private AudioClip buffetLoopClip;
        [SerializeField] private AudioClip stallBuffetLoopClip;

        [Header("Audio Source")]
        [SerializeField] private AudioSource eventSource;
        [SerializeField] private AudioSource buffetSource;

        // ── State ─────────────────────────────────────────────────────────────────

        private bool _buffetActive;
        private bool _stallBuffetActive;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Plays flap deployment or retraction sound.</summary>
        /// <param name="deploying">True for deploy, false for retract.</param>
        public void PlayFlapSound(bool deploying)
        {
            PlayEvent(deploying ? flapDeployClip : flapRetractClip);
        }

        /// <summary>Plays gear extension or retraction sound.</summary>
        public void PlayGearSound(bool extending)
        {
            PlayEvent(extending ? gearExtendClip : gearRetractClip);
        }

        /// <summary>Plays speed brake deployment or retraction sound.</summary>
        public void PlaySpeedBrakeSound(bool deploying)
        {
            PlayEvent(deploying ? speedBrakeDeployClip : speedBrakeRetractClip);
        }

        /// <summary>
        /// Sets the aerodynamic buffet intensity (0 = none, 1 = maximum).
        /// </summary>
        public void SetBuffetIntensity(float intensity)
        {
            if (buffetSource == null) return;
            float vol = Mathf.Clamp01(intensity);
            buffetSource.volume = vol;

            if (vol > 0.01f && !buffetSource.isPlaying && buffetLoopClip != null)
            {
                buffetSource.clip = buffetLoopClip;
                buffetSource.loop = true;
                buffetSource.Play();
                _buffetActive = true;
            }
            else if (vol <= 0.01f && _buffetActive)
            {
                buffetSource.Stop();
                _buffetActive = false;
            }
        }

        /// <summary>
        /// Enables or disables the stall warning buffet loop.
        /// </summary>
        public void SetStallBuffet(bool active)
        {
            if (buffetSource == null || _stallBuffetActive == active) return;
            _stallBuffetActive = active;
            if (active && stallBuffetLoopClip != null)
            {
                buffetSource.clip   = stallBuffetLoopClip;
                buffetSource.volume = 0.9f;
                buffetSource.loop   = true;
                buffetSource.Play();
            }
            else
            {
                buffetSource.Stop();
            }
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private void PlayEvent(AudioClip clip)
        {
            if (eventSource == null || clip == null) return;
            eventSource.PlayOneShot(clip);
        }
    }
}
