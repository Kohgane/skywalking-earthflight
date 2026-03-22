using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VoiceChat
{
    /// <summary>
    /// Configures per-participant <see cref="AudioSource"/> components for 3D spatial audio.
    /// <para>
    /// Features:
    /// <list type="bullet">
    ///   <item>Distance-based logarithmic attenuation up to a configurable maximum range.</item>
    ///   <item>Doppler effect scaling for fast-moving aircraft.</item>
    ///   <item>Occlusion detection via Physics raycast for terrain and building line-of-sight.</item>
    ///   <item>Position updates driven by multiplayer synchronisation data.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class VoiceSpatialAudio : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Attenuation")]
        [Tooltip("Default maximum audible range in metres (overridden per-source by ConfigureSpatialSource).")]
        [SerializeField] private float defaultMaxRange = 500f;

        [Tooltip("Minimum audible distance — full volume within this radius.")]
        [SerializeField] private float minDistance = 5f;

        [Tooltip("AudioRolloffMode applied to all spatial sources.")]
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Doppler")]
        [Tooltip("Unity doppler level applied to participant audio sources (0 = off).")]
        [SerializeField] private float dopplerLevel = 0.5f;

        [Header("Occlusion")]
        [Tooltip("Whether to cast rays to detect terrain/building occlusion.")]
        [SerializeField] private bool occlusionEnabled = true;

        [Tooltip("Layer mask used for occlusion raycasts (terrain and building layers).")]
        [SerializeField] private LayerMask occlusionLayerMask = Physics.DefaultRaycastLayers;

        [Tooltip("Volume multiplier applied to an occluded source.")]
        [SerializeField] private float occlusionVolumeMultiplier = 0.3f;

        [Tooltip("How frequently (seconds) occlusion raycasts are performed per participant.")]
        [SerializeField] private float occlusionCheckInterval = 0.25f;
        #endregion

        #region Internal State
        private readonly Dictionary<string, AudioSource>  _sources    = new Dictionary<string, AudioSource>();
        private readonly Dictionary<string, Vector3>      _velocities = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, float>        _baseVolumes = new Dictionary<string, float>();
        private readonly Dictionary<string, float>        _occlusionTimers = new Dictionary<string, float>();
        private readonly Dictionary<string, bool>         _occluded   = new Dictionary<string, bool>();
        #endregion

        #region Public API
        /// <summary>
        /// Configures an <see cref="AudioSource"/> for 3D spatial voice playback.
        /// </summary>
        /// <param name="source">The AudioSource to configure.</param>
        /// <param name="maxRange">Maximum audible range in metres.</param>
        public void ConfigureSpatialSource(AudioSource source, float maxRange)
        {
            if (source == null) return;
            source.spatialBlend     = 1f;
            source.rolloffMode      = rolloffMode;
            source.minDistance      = minDistance;
            source.maxDistance      = maxRange > 0f ? maxRange : defaultMaxRange;
            source.dopplerLevel     = dopplerLevel;
            source.spread           = 0f;
        }

        /// <summary>
        /// Registers an audio source with a participant ID for spatial updates.
        /// </summary>
        /// <param name="participantId">Unique participant identifier.</param>
        /// <param name="source">The AudioSource component to track.</param>
        /// <param name="maxRange">Maximum audible range in metres.</param>
        public void RegisterSource(string participantId, AudioSource source, float maxRange)
        {
            if (source == null) return;
            ConfigureSpatialSource(source, maxRange);
            _sources[participantId]    = source;
            _velocities[participantId] = Vector3.zero;
            _baseVolumes[participantId] = source.volume;
            _occlusionTimers[participantId] = 0f;
            _occluded[participantId] = false;
        }

        /// <summary>
        /// Removes a participant's spatial source from tracking.
        /// </summary>
        /// <param name="participantId">Unique participant identifier.</param>
        public void UnregisterSource(string participantId)
        {
            _sources.Remove(participantId);
            _velocities.Remove(participantId);
            _baseVolumes.Remove(participantId);
            _occlusionTimers.Remove(participantId);
            _occluded.Remove(participantId);
        }

        /// <summary>
        /// Updates the world-space position and velocity for a participant, enabling
        /// Doppler-correct spatial audio and occlusion detection.
        /// </summary>
        /// <param name="participantId">Unique participant identifier.</param>
        /// <param name="worldPos">Current world-space position.</param>
        /// <param name="velocity">Current velocity vector (metres per second).</param>
        public void UpdateParticipantPosition(string participantId, Vector3 worldPos, Vector3 velocity)
        {
            if (!_sources.TryGetValue(participantId, out AudioSource src) || src == null) return;

            src.transform.position = worldPos;
            _velocities[participantId] = velocity;
        }
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!occlusionEnabled) return;
            foreach (var kvp in _sources)
            {
                string id  = kvp.Key;
                AudioSource src = kvp.Value;
                if (src == null) continue;

                _occlusionTimers[id] -= Time.deltaTime;
                if (_occlusionTimers[id] > 0f) continue;
                _occlusionTimers[id] = occlusionCheckInterval;

                CheckOcclusion(id, src);
            }
        }
        #endregion

        #region Private Helpers
        private void CheckOcclusion(string participantId, AudioSource src)
        {
            Vector3 listenerPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            Vector3 sourcePos   = src.transform.position;
            Vector3 direction   = sourcePos - listenerPos;
            float   distance    = direction.magnitude;

            bool wasOccluded = _occluded.TryGetValue(participantId, out bool prev) && prev;
            bool isOccluded  = false;

            if (distance > 0.1f)
            {
                isOccluded = Physics.Raycast(listenerPos, direction.normalized, distance,
                                             occlusionLayerMask, QueryTriggerInteraction.Ignore);
            }

            _occluded[participantId] = isOccluded;

            if (isOccluded != wasOccluded)
            {
                float baseVol = _baseVolumes.TryGetValue(participantId, out float bv) ? bv : 1f;
                src.volume = isOccluded ? baseVol * occlusionVolumeMultiplier : baseVol;
            }
        }
        #endregion
    }
}
