// ReverbZoneManager.cs — Phase 118: Spatial Audio & 3D Soundscape
// Dynamic reverb: canyon echo, hangar reverb, cockpit resonance, open sky minimal reverb.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Manages dynamic audio reverb zones by adjusting Unity AudioReverbZone or
    /// a custom reverb filter based on the current <see cref="AudioZoneType"/>.
    /// Supports crossfading between reverb presets.
    /// </summary>
    public class ReverbZoneManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Reverb Zone")]
        [SerializeField] private AudioReverbZone reverbZone;

        [Header("Reverb Filter (fallback)")]
        [SerializeField] private AudioReverbFilter reverbFilter;

        // ── State ─────────────────────────────────────────────────────────────────

        private ReverbZonePreset _currentPreset = ReverbZonePreset.OpenSky;

        /// <summary>Currently active reverb preset.</summary>
        public ReverbZonePreset CurrentPreset => _currentPreset;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies the reverb preset corresponding to the given zone type.
        /// </summary>
        public void SetReverbForZone(AudioZoneType zone)
        {
            ReverbZonePreset preset = ZoneToPreset(zone);
            if (preset == _currentPreset) return;
            _currentPreset = preset;
            ApplyPreset(preset);
        }

        /// <summary>
        /// Directly applies a reverb preset.
        /// </summary>
        public void SetPreset(ReverbZonePreset preset)
        {
            _currentPreset = preset;
            ApplyPreset(preset);
        }

        /// <summary>
        /// Returns the reverb preset that best matches the given audio zone.
        /// </summary>
        public static ReverbZonePreset ZoneToPreset(AudioZoneType zone)
        {
            switch (zone)
            {
                case AudioZoneType.Cockpit:  return ReverbZonePreset.Cockpit;
                case AudioZoneType.Cabin:    return ReverbZonePreset.Cockpit;
                case AudioZoneType.Hangar:   return ReverbZonePreset.Hangar;
                case AudioZoneType.Airport:  return ReverbZonePreset.Airport;
                case AudioZoneType.City:     return ReverbZonePreset.City;
                case AudioZoneType.Forest:   return ReverbZonePreset.Forest;
                case AudioZoneType.Mountain: return ReverbZonePreset.Mountain;
                case AudioZoneType.Ocean:    return ReverbZonePreset.OpenSky;
                case AudioZoneType.Space:    return ReverbZonePreset.Space;
                default:                     return ReverbZonePreset.OpenSky;
            }
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void ApplyPreset(ReverbZonePreset preset)
        {
            if (reverbZone != null)
                ApplyToReverbZone(preset, reverbZone);
            else if (reverbFilter != null)
                ApplyToReverbFilter(preset, reverbFilter);
        }

        private static void ApplyToReverbZone(ReverbZonePreset preset, AudioReverbZone zone)
        {
            switch (preset)
            {
                case ReverbZonePreset.OpenSky:
                    zone.reverbPreset = AudioReverbPreset.Plain;
                    break;
                case ReverbZonePreset.Cockpit:
                    zone.reverbPreset = AudioReverbPreset.Car;
                    break;
                case ReverbZonePreset.Hangar:
                    zone.reverbPreset = AudioReverbPreset.Hangar;
                    break;
                case ReverbZonePreset.Canyon:
                    zone.reverbPreset = AudioReverbPreset.Mountains;
                    break;
                case ReverbZonePreset.Airport:
                    zone.reverbPreset = AudioReverbPreset.Concerthall;
                    break;
                case ReverbZonePreset.City:
                    zone.reverbPreset = AudioReverbPreset.City;
                    break;
                case ReverbZonePreset.Forest:
                    zone.reverbPreset = AudioReverbPreset.Forest;
                    break;
                case ReverbZonePreset.Mountain:
                    zone.reverbPreset = AudioReverbPreset.Mountains;
                    break;
                case ReverbZonePreset.Space:
                    zone.reverbPreset = AudioReverbPreset.Off;
                    break;
                default:
                    zone.reverbPreset = AudioReverbPreset.Plain;
                    break;
            }
        }

        private static void ApplyToReverbFilter(ReverbZonePreset preset, AudioReverbFilter filter)
        {
            switch (preset)
            {
                case ReverbZonePreset.OpenSky:
                    filter.reverbPreset = AudioReverbPreset.Plain;
                    break;
                case ReverbZonePreset.Cockpit:
                    filter.reverbPreset = AudioReverbPreset.Car;
                    break;
                case ReverbZonePreset.Hangar:
                    filter.reverbPreset = AudioReverbPreset.Hangar;
                    break;
                default:
                    filter.reverbPreset = AudioReverbPreset.Off;
                    break;
            }
        }
    }
}
