// AudioEffectsProcessor.cs — Phase 118: Spatial Audio & 3D Soundscape
// Real-time audio effects: low-pass (cockpit muffle), high-pass, EQ by environment.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Applies real-time audio DSP effects to a target AudioSource or the global
    /// audio listener: low-pass filtering for cockpit muffling, high-pass for
    /// thin atmosphere, and EQ shaping by environment zone.
    /// </summary>
    public class AudioEffectsProcessor : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Global Listener Filters")]
        [SerializeField] private AudioLowPassFilter  globalLowPassFilter;
        [SerializeField] private AudioHighPassFilter globalHighPassFilter;

        [Header("Default Frequencies")]
        [Tooltip("Default low-pass cutoff for open air (Hz).")]
        [Range(1000f, 22000f)] public float openAirLowPassHz  = 22000f;
        [Tooltip("Cockpit-muffled low-pass cutoff (Hz).")]
        [Range(200f,  8000f)]  public float cockpitLowPassHz  = 1500f;
        [Tooltip("High-altitude thin-air high-pass cutoff (Hz).")]
        [Range(0f,    500f)]   public float thinAirHighPassHz = 80f;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies audio effects appropriate for the given zone.
        /// </summary>
        public void ApplyZoneEffects(AudioZoneType zone)
        {
            switch (zone)
            {
                case AudioZoneType.Cockpit:
                case AudioZoneType.Cabin:
                    SetLowPass(cockpitLowPassHz);
                    SetHighPass(300f);
                    break;

                case AudioZoneType.Space:
                    SetLowPass(2000f);
                    SetHighPass(thinAirHighPassHz);
                    break;

                case AudioZoneType.Hangar:
                    SetLowPass(openAirLowPassHz);
                    SetHighPass(50f);
                    break;

                default:
                    SetLowPass(openAirLowPassHz);
                    SetHighPass(20f);
                    break;
            }
        }

        /// <summary>
        /// Applies a smooth low-pass filter blend for cockpit interior/exterior transitions.
        /// </summary>
        /// <param name="interiorBlend">0 = full exterior, 1 = full interior (muffled).</param>
        public void ApplyInteriorBlend(float interiorBlend)
        {
            float cutoff = Mathf.Lerp(openAirLowPassHz, cockpitLowPassHz, Mathf.Clamp01(interiorBlend));
            SetLowPass(cutoff);
        }

        /// <summary>Sets the global low-pass filter cutoff frequency.</summary>
        public void SetLowPass(float cutoffHz)
        {
            if (globalLowPassFilter != null)
                globalLowPassFilter.cutoffFrequency = Mathf.Clamp(cutoffHz, 10f, 22000f);
        }

        /// <summary>Sets the global high-pass filter cutoff frequency.</summary>
        public void SetHighPass(float cutoffHz)
        {
            if (globalHighPassFilter != null)
                globalHighPassFilter.cutoffFrequency = Mathf.Clamp(cutoffHz, 10f, 22000f);
        }

        /// <summary>Returns the current low-pass cutoff frequency.</summary>
        public float GetLowPassCutoff() =>
            globalLowPassFilter != null ? globalLowPassFilter.cutoffFrequency : openAirLowPassHz;

        /// <summary>Returns the current high-pass cutoff frequency.</summary>
        public float GetHighPassCutoff() =>
            globalHighPassFilter != null ? globalHighPassFilter.cutoffFrequency : 20f;
    }
}
