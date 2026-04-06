// DopplerEffectController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Doppler shift: flyby effect, approaching/receding sirens, realistic pitch shifting.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Calculates and applies Doppler frequency shift to audio sources based on
    /// relative velocity between source and listener. Supports per-source tracking
    /// for flyby and approaching/receding sound scenarios.
    /// </summary>
    public class DopplerEffectController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Calculates the Doppler-shifted pitch for a moving audio source.
        /// </summary>
        /// <param name="sourceVelocity">Velocity of the audio source (world space, m/s).</param>
        /// <param name="listenerVelocity">Velocity of the listener (world space, m/s).</param>
        /// <param name="sourceToListener">Direction from source to listener (normalised).</param>
        /// <returns>Pitch multiplier to apply to the audio source.</returns>
        public float CalculateDopplerPitch(Vector3 sourceVelocity, Vector3 listenerVelocity, Vector3 sourceToListener)
        {
            float speedOfSound  = config != null ? config.speedOfSound  : 343f;
            float dopplerFactor = config != null ? config.dopplerFactor : 1f;

            if (Mathf.Approximately(dopplerFactor, 0f)) return 1f;

            // Project velocities onto the source→listener axis
            float vs = Vector3.Dot(sourceVelocity,   sourceToListener);  // source approaching = positive
            float vl = Vector3.Dot(listenerVelocity, sourceToListener);  // listener approaching = negative

            vs *= dopplerFactor;
            vl *= dopplerFactor;

            float denominator = Mathf.Max(1f, speedOfSound + vs);
            float numerator   = speedOfSound - vl;

            return Mathf.Clamp(numerator / denominator, 0.01f, 10f);
        }

        /// <summary>
        /// Computes a full <see cref="DopplerResult"/> for the given source/listener state.
        /// </summary>
        public DopplerResult ComputeDopplerResult(
            float   originalFrequency,
            Vector3 sourceVelocity,
            Vector3 listenerVelocity,
            Vector3 sourceToListenerNorm)
        {
            float speedOfSound = config != null ? config.speedOfSound : 343f;
            float pitch        = CalculateDopplerPitch(sourceVelocity, listenerVelocity, sourceToListenerNorm);
            float vs           = Vector3.Dot(sourceVelocity,   sourceToListenerNorm);
            float vl           = Vector3.Dot(listenerVelocity, sourceToListenerNorm);

            return new DopplerResult
            {
                originalFrequency = originalFrequency,
                shiftedFrequency  = originalFrequency * pitch,
                relativeVelocity  = vs - vl,
                speedOfSound      = speedOfSound
            };
        }

        /// <summary>
        /// Applies a computed Doppler pitch to an AudioSource.
        /// </summary>
        public void ApplyDopplerToSource(AudioSource source, float dopplerPitch)
        {
            if (source == null) return;
            source.pitch = Mathf.Clamp(dopplerPitch, 0.01f, 10f);
        }
    }
}
