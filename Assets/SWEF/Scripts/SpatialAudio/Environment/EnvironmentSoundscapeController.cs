// EnvironmentSoundscapeController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Biome-based ambient environment soundscape: forest, ocean, city, desert, arctic, mountain.
// Namespace: SWEF.SpatialAudio

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Manages biome-based ambient environment soundscapes. Crossfades between
    /// zone profiles as the aircraft moves through different environments.
    /// </summary>
    public class EnvironmentSoundscapeController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Zone Audio Sources")]
        [SerializeField] private AudioSource primarySource;
        [SerializeField] private AudioSource secondarySource;

        [Header("Environment Profiles")]
        [SerializeField] private List<EnvironmentAudioProfile> profiles = new List<EnvironmentAudioProfile>();

        // ── State ─────────────────────────────────────────────────────────────────

        private AudioZoneType _currentZone = AudioZoneType.Exterior;
        private AudioZoneType _targetZone  = AudioZoneType.Exterior;
        private float _crossfadeTimer;
        private float _crossfadeDuration = 2f;
        private bool  _crossfading;

        /// <summary>Current active audio zone.</summary>
        public AudioZoneType CurrentZone => _currentZone;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Transitions the ambient soundscape to the given zone.
        /// </summary>
        public void SetZone(AudioZoneType zone)
        {
            if (zone == _currentZone && !_crossfading) return;
            _targetZone      = zone;
            _crossfading     = true;
            _crossfadeTimer  = 0f;
            _crossfadeDuration = config != null
                ? GetProfile(zone)?.transitionDuration ?? config.interiorExteriorTransitionDuration
                : 2f;
        }

        private void Update()
        {
            if (!_crossfading) return;
            _crossfadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_crossfadeTimer / Mathf.Max(0.05f, _crossfadeDuration));

            float masterVol = config != null ? config.ambientVolume : 0.4f;

            if (primarySource != null)   primarySource.volume   = (1f - t) * masterVol;
            if (secondarySource != null) secondarySource.volume = t * masterVol;

            if (t >= 1f)
            {
                _currentZone = _targetZone;
                _crossfading = false;
                // Swap sources
                (primarySource, secondarySource) = (secondarySource, primarySource);
            }
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private EnvironmentAudioProfile GetProfile(AudioZoneType zone)
        {
            foreach (var p in profiles)
                if (p.zoneType == zone) return p;
            return null;
        }
    }
}
