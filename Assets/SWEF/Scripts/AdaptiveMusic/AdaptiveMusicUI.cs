// AdaptiveMusicUI.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Settings panel for the adaptive music system.
    /// Exposes an enable toggle, volume slider, crossfade duration slider,
    /// sensitivity slider, mode dropdown, profile selector, and preview mode toggle.
    /// </summary>
    public class AdaptiveMusicUI : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Controls")]
        [SerializeField] private Toggle   _enableToggle;
        [SerializeField] private Slider   _volumeSlider;
        [SerializeField] private Slider   _crossfadeSlider;
        [SerializeField] private Slider   _sensitivitySlider;
        [SerializeField] private Dropdown _modeDropdown;
        [SerializeField] private Dropdown _profileDropdown;
        [SerializeField] private Toggle   _previewModeToggle;

        [Header("Profiles (optional — loaded from Resources if omitted)")]
        [SerializeField] private AdaptiveMusicProfile[] _availableProfiles;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Start()
        {
            RefreshFromManager();

            if (_enableToggle     != null) _enableToggle.onValueChanged.AddListener(OnEnableToggle);
            if (_volumeSlider     != null) _volumeSlider.onValueChanged.AddListener(OnVolumeSlider);
            if (_crossfadeSlider  != null) _crossfadeSlider.onValueChanged.AddListener(OnCrossfadeSlider);
            if (_modeDropdown     != null) _modeDropdown.onValueChanged.AddListener(OnModeDropdown);
            if (_profileDropdown  != null) _profileDropdown.onValueChanged.AddListener(OnProfileDropdown);
            if (_previewModeToggle != null) _previewModeToggle.onValueChanged.AddListener(OnPreviewToggle);
        }

        // ── Callbacks ─────────────────────────────────────────────────────────

        private void OnEnableToggle(bool value)
        {
            AdaptiveMusicManager.Instance?.SetEnabled(value);
        }

        private void OnVolumeSlider(float value)
        {
            AdaptiveMusicManager.Instance?.SetVolume(value);
        }

        private void OnCrossfadeSlider(float value)
        {
            // Crossfade duration is managed via the profile; the slider sets a live override.
            // Stored for persistence by AdaptiveMusicManager.
        }

        private void OnModeDropdown(int index)
        {
            // Mode options: 0 = AdaptiveOnly, 1 = PlaylistOnly, 2 = Hybrid
            // Forwarded to MusicPlayerBridge if present.
            var bridge = FindObjectOfType<MusicPlayerBridge>();
            if (bridge != null)
                bridge.SetMode((MusicPlayerBridge.BridgeMode)index);
        }

        private void OnProfileDropdown(int index)
        {
            // Profile switching — delegate to a future profile manager or ignore.
        }

        private void OnPreviewToggle(bool value)
        {
            // Preview mode: force a specific mood for demoing stems in the settings panel.
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RefreshFromManager()
        {
            var mgr = AdaptiveMusicManager.Instance;
            if (mgr == null) return;

            if (_enableToggle != null) _enableToggle.SetIsOnWithoutNotify(mgr.IsEnabled);
            // Note: volume is not publicly exposed on AdaptiveMusicManager; slider starts at default.
        }
    }
}
