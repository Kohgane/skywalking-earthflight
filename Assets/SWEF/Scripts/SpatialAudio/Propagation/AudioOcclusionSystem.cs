// AudioOcclusionSystem.cs — Phase 118: Spatial Audio & 3D Soundscape
// Occlusion/obstruction: terrain blocking, cockpit muffling, building occlusion.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Implements audio occlusion by raycasting from the listener to each audio
    /// source. Occluded sources receive a low-pass filter and volume reduction.
    /// Supports multiple <see cref="AudioOcclusionType"/> modes.
    /// </summary>
    public class AudioOcclusionSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether the line of sight between listener and source is obstructed.
        /// </summary>
        /// <param name="listenerPos">World position of the audio listener.</param>
        /// <param name="sourcePos">World position of the audio source.</param>
        /// <returns>True if an occlusion obstacle is present.</returns>
        public bool IsOccluded(Vector3 listenerPos, Vector3 sourcePos)
        {
            AudioOcclusionType mode = config != null ? config.occlusionType : AudioOcclusionType.Raycast;

            if (mode == AudioOcclusionType.None) return false;

            LayerMask mask = config != null ? config.occlusionLayerMask : ~0;
            Vector3 direction = sourcePos - listenerPos;
            float   distance  = direction.magnitude;

            if (mode == AudioOcclusionType.Raycast || mode == AudioOcclusionType.LowPass)
            {
                return Physics.Raycast(listenerPos, direction.normalized, distance, mask);
            }

            if (mode == AudioOcclusionType.Volumetric)
            {
                return CheckVolumetricOcclusion(listenerPos, sourcePos, mask);
            }

            return false;
        }

        /// <summary>
        /// Calculates the occlusion factor (0 = fully open, 1 = fully occluded)
        /// using multiple sample rays for volumetric mode.
        /// </summary>
        public float CalculateOcclusionFactor(Vector3 listenerPos, Vector3 sourcePos)
        {
            AudioOcclusionType mode = config != null ? config.occlusionType : AudioOcclusionType.Raycast;
            if (mode == AudioOcclusionType.None) return 0f;

            LayerMask mask      = config != null ? config.occlusionLayerMask : ~0;
            Vector3   direction = sourcePos - listenerPos;
            float     distance  = direction.magnitude;

            if (mode == AudioOcclusionType.LowPass || mode == AudioOcclusionType.Raycast)
            {
                return Physics.Raycast(listenerPos, direction.normalized, distance, mask) ? 1f : 0f;
            }

            // Volumetric: cast multiple offset rays
            int   hits  = 0;
            int   total = 5;
            float spread = 0.5f;
            for (int i = 0; i < total; i++)
            {
                Vector3 offset = Random.insideUnitSphere * spread;
                if (Physics.Raycast(listenerPos + offset, direction.normalized, distance, mask))
                    hits++;
            }
            return (float)hits / total;
        }

        /// <summary>
        /// Returns the low-pass cutoff frequency for the given occlusion factor.
        /// </summary>
        public float GetOcclusionCutoffHz(float occlusionFactor)
        {
            float maxHz    = 22000f;
            float minHz    = config != null ? config.occlusionCutoffHz : 800f;
            return Mathf.Lerp(maxHz, minHz, occlusionFactor);
        }

        /// <summary>
        /// Returns the volume multiplier for the given occlusion factor.
        /// </summary>
        public float GetOcclusionVolumeMultiplier(float occlusionFactor)
        {
            float reduction = config != null ? config.occlusionVolumeReduction : 0.5f;
            return Mathf.Lerp(1f, 1f - reduction, occlusionFactor);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private static bool CheckVolumetricOcclusion(Vector3 from, Vector3 to, LayerMask mask)
        {
            Vector3 dir  = to - from;
            float   dist = dir.magnitude;
            // Cast 3 rays in a triangle pattern
            Vector3 right = Vector3.Cross(dir.normalized, Vector3.up).normalized * 0.3f;
            return Physics.Raycast(from,         dir.normalized, dist, mask) &&
                   Physics.Raycast(from + right, dir.normalized, dist, mask) &&
                   Physics.Raycast(from - right, dir.normalized, dist, mask);
        }
    }
}
