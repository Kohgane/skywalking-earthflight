// SpatialAudioUI.cs — Phase 118: Spatial Audio & 3D Soundscape
// Audio settings panel: master volume, category volumes, spatial audio toggle, quality presets.
// Namespace: SWEF.SpatialAudio

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// UI panel for spatial audio settings: master volume, per-category volumes,
    /// spatial audio on/off toggle, and quality preset selection.
    /// </summary>
    public class SpatialAudioUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Volume Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider engineVolumeSlider;
        [SerializeField] private Slider ambientVolumeSlider;
        [SerializeField] private Slider cockpitVolumeSlider;
        [SerializeField] private Slider warningVolumeSlider;

        [Header("Toggles")]
        [SerializeField] private Toggle spatialAudioToggle;
        [SerializeField] private Toggle hrtfToggle;

        [Header("Quality Dropdown")]
        [SerializeField] private Dropdown qualityDropdown;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            RefreshUI();
            RegisterListeners();
        }

        // ── UI Methods ────────────────────────────────────────────────────────────

        /// <summary>Refreshes all UI controls to match current config values.</summary>
        public void RefreshUI()
        {
            if (config == null) return;
            if (masterVolumeSlider  != null) masterVolumeSlider.value  = AudioListener.volume;
            if (engineVolumeSlider  != null) engineVolumeSlider.value  = config.engineVolume;
            if (ambientVolumeSlider != null) ambientVolumeSlider.value = config.ambientVolume;
            if (cockpitVolumeSlider != null) cockpitVolumeSlider.value = config.cockpitAmbientVolume;
            if (warningVolumeSlider != null) warningVolumeSlider.value = config.warningVolume;
            if (hrtfToggle          != null) hrtfToggle.isOn           = config.enableHRTF;
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void RegisterListeners()
        {
            masterVolumeSlider?.onValueChanged.AddListener(v => AudioListener.volume = v);
            hrtfToggle?.onValueChanged.AddListener(OnHRTFToggled);
            qualityDropdown?.onValueChanged.AddListener(OnQualityChanged);
        }

        private void OnHRTFToggled(bool enabled)
        {
            if (SpatialAudioManager.Instance != null)
                SpatialAudioManager.Instance.SetHRTF(enabled);
        }

        private void OnQualityChanged(int index)
        {
            string[] presets = { "Low", "Medium", "High", "Ultra" };
            string preset = index >= 0 && index < presets.Length ? presets[index] : "Medium";
            Debug.Log($"[SpatialAudioUI] Quality preset: {preset}");
        }
    }
}
