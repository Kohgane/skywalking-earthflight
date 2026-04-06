// AudioMixerUI.cs — Phase 118: Spatial Audio & 3D Soundscape
// Advanced mixer panel: per-source volume, reverb amount, occlusion toggle, HRTF on/off.
// Namespace: SWEF.SpatialAudio

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Advanced audio mixer UI panel providing per-category volume sliders, reverb
    /// wet/dry control, occlusion enable/disable, and HRTF toggle for power users.
    /// </summary>
    public class AudioMixerUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Per-Category Sliders")]
        [SerializeField] private Slider engineMixSlider;
        [SerializeField] private Slider windMixSlider;
        [SerializeField] private Slider ambientMixSlider;
        [SerializeField] private Slider cockpitMixSlider;
        [SerializeField] private Slider radioMixSlider;

        [Header("Effect Controls")]
        [SerializeField] private Slider  reverbWetSlider;
        [SerializeField] private Toggle  occlusionToggle;
        [SerializeField] private Toggle  hrtfToggle;
        [SerializeField] private Toggle  dopplerToggle;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            RefreshMixerUI();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Refreshes mixer controls from current config values.</summary>
        public void RefreshMixerUI()
        {
            if (config == null) return;
            if (engineMixSlider  != null) engineMixSlider.value  = config.engineVolume;
            if (ambientMixSlider != null) ambientMixSlider.value = config.ambientVolume;
            if (cockpitMixSlider != null) cockpitMixSlider.value = config.cockpitAmbientVolume;
            if (hrtfToggle       != null) hrtfToggle.isOn        = config.enableHRTF;
            if (dopplerToggle    != null) dopplerToggle.isOn      = config.dopplerFactor > 0f;
            if (occlusionToggle  != null) occlusionToggle.isOn   = config.occlusionType != AudioOcclusionType.None;
        }

        /// <summary>Applies current slider/toggle values to the audio system.</summary>
        public void ApplyMixerSettings()
        {
            if (config == null) return;
            if (hrtfToggle != null && SpatialAudioManager.Instance != null)
                SpatialAudioManager.Instance.SetHRTF(hrtfToggle.isOn);
        }
    }
}
