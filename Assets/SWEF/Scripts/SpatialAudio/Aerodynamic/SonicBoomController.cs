// SonicBoomController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Sonic boom audio: shock wave sound for supersonic flight, distance propagation.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Generates sonic boom audio when an aircraft exceeds the speed of sound.
    /// Handles shock wave audio playback and distance-based boom propagation delay.
    /// </summary>
    public class SonicBoomController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Boom Audio")]
        [SerializeField] private AudioClip sonicBoomClip;
        [SerializeField] private AudioSource boomSource;

        [Header("Settings")]
        [Tooltip("Speed of sound in m/s (standard: 343 m/s at sea level).")]
        [Range(200f, 400f)] public float speedOfSound = 343f;

        [Tooltip("Minimum Mach number required before boom can trigger.")]
        [Range(0.95f, 1.5f)] public float boomMachThreshold = 1.0f;

        [Tooltip("Cooldown in seconds between boom triggers.")]
        [Range(1f, 60f)] public float boomCooldown = 5f;

        // ── State ─────────────────────────────────────────────────────────────────

        private float _lastBoomTime = -999f;
        private bool  _wasSupersonic;

        /// <summary>Whether the aircraft is currently supersonic.</summary>
        public bool IsSupersonic { get; private set; }

        /// <summary>Current Mach number.</summary>
        public float CurrentMach { get; private set; }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the sonic boom system with current airspeed.
        /// </summary>
        /// <param name="speedMs">Current airspeed in m/s.</param>
        /// <param name="listenerDistance">Distance from aircraft to listener in metres.</param>
        public void UpdateSpeed(float speedMs, float listenerDistance = 0f)
        {
            CurrentMach  = speedMs / Mathf.Max(1f, speedOfSound);
            IsSupersonic = CurrentMach >= boomMachThreshold;

            // Trigger boom on transition from subsonic to supersonic
            bool justWentSupersonic = IsSupersonic && !_wasSupersonic;
            _wasSupersonic = IsSupersonic;

            if (justWentSupersonic && Time.time - _lastBoomTime > boomCooldown)
            {
                float delay = listenerDistance / Mathf.Max(1f, speedOfSound);
                TriggerBoom(delay);
            }
        }

        /// <summary>
        /// Calculates the propagation delay of a sonic boom to the listener.
        /// </summary>
        public float CalculatePropagationDelay(float distanceMetres)
        {
            return distanceMetres / Mathf.Max(1f, speedOfSound);
        }

        /// <summary>
        /// Manually triggers a sonic boom sound (used by external systems).
        /// </summary>
        public void TriggerBoom(float delaySeconds = 0f)
        {
            _lastBoomTime = Time.time;
            if (boomSource == null || sonicBoomClip == null) return;

            if (delaySeconds <= 0f)
                boomSource.PlayOneShot(sonicBoomClip);
            else
                StartCoroutine(PlayDelayed(delaySeconds));
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private System.Collections.IEnumerator PlayDelayed(float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            if (boomSource != null && sonicBoomClip != null)
                boomSource.PlayOneShot(sonicBoomClip);
        }
    }
}
