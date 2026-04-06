// AudioTransitionController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Smooth audio transitions: crossfade between zones, altitude mix changes, interior/exterior.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Orchestrates smooth audio transitions between zones, including interior/exterior
    /// crossfades, altitude-based mix changes, and zone blend management. Works in
    /// conjunction with <see cref="SpatialAudioManager"/> and <see cref="AudioEffectsProcessor"/>.
    /// </summary>
    public class AudioTransitionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Effects Processor")]
        [SerializeField] private AudioEffectsProcessor effectsProcessor;

        [Header("Environment Controller")]
        [SerializeField] private EnvironmentSoundscapeController soundscapeController;

        // ── State ─────────────────────────────────────────────────────────────────

        private float _interiorBlend;      // 0 = exterior, 1 = interior
        private float _altitudeMixWeight;  // 0 = low alt, 1 = high alt
        private AudioZoneType _activeZone = AudioZoneType.Exterior;

        /// <summary>Current interior/exterior blend (0 = exterior, 1 = interior).</summary>
        public float InteriorBlend => _interiorBlend;

        /// <summary>Current altitude mix weight (0 = low altitude, 1 = high altitude).</summary>
        public float AltitudeMixWeight => _altitudeMixWeight;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            if (effectsProcessor != null)
                effectsProcessor.ApplyInteriorBlend(_interiorBlend);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Transitions to the target audio zone with an optional duration override.
        /// </summary>
        public void TransitionToZone(AudioZoneType zone, float duration = -1f)
        {
            _activeZone = zone;

            if (SpatialAudioManager.Instance != null)
                SpatialAudioManager.Instance.TransitionToZone(zone, duration);

            if (soundscapeController != null)
                soundscapeController.SetZone(zone);

            bool isInterior = zone == AudioZoneType.Cockpit || zone == AudioZoneType.Cabin ||
                              zone == AudioZoneType.Hangar;
            float targetBlend = isInterior ? 1f : 0f;

            StartCoroutine(BlendInterior(targetBlend, duration > 0f ? duration :
                config != null ? config.interiorExteriorTransitionDuration : 0.5f));
        }

        /// <summary>
        /// Updates the altitude mix weight based on current altitude.
        /// Higher altitude = thinner sound.
        /// </summary>
        public void UpdateAltitudeMix(float altitudeMetres)
        {
            float fadeAlt = config != null ? config.highAltitudeFadeAltitude : 3000f;
            float target  = Mathf.Clamp01(altitudeMetres / fadeAlt);
            float rate    = config != null ? config.altitudeMixTransitionRate : 0.1f;
            _altitudeMixWeight = Mathf.MoveTowards(_altitudeMixWeight, target, rate * Time.deltaTime);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private System.Collections.IEnumerator BlendInterior(float target, float duration)
        {
            float start     = _interiorBlend;
            float elapsed   = 0f;
            float dur       = Mathf.Max(0.05f, duration);

            while (elapsed < dur)
            {
                elapsed         += Time.deltaTime;
                _interiorBlend   = Mathf.Lerp(start, target, elapsed / dur);
                yield return null;
            }
            _interiorBlend = target;
        }
    }
}
