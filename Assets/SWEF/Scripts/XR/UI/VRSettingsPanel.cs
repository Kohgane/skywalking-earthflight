// VRSettingsPanel.cs — Phase 112: VR/XR Flight Experience
// VR-specific settings: comfort, hand tracking, seated/standing, graphics quality.
// Namespace: SWEF.XR

using System;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.XR
{
    /// <summary>
    /// VR settings panel that exposes comfort level, hand tracking toggle,
    /// seated/standing mode, and graphics quality controls.
    /// Settings are immediately applied and persisted to PlayerPrefs.
    /// </summary>
    public class VRSettingsPanel : MonoBehaviour
    {
        // ── PlayerPrefs keys ──────────────────────────────────────────────────────
        private const string KeyComfortLevel   = "SWEF_VR_ComfortLevel";
        private const string KeyHandTracking   = "SWEF_VR_HandTracking";
        private const string KeyTrackingMode   = "SWEF_VR_TrackingMode";
        private const string KeyGraphicsQuality= "SWEF_VR_GraphicsQuality";

        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("UI References")]
        [SerializeField] private Dropdown comfortLevelDropdown;
        [SerializeField] private Toggle   handTrackingToggle;
        [SerializeField] private Toggle   standingModeToggle;
        [SerializeField] private Dropdown graphicsQualityDropdown;

        [Header("Systems")]
        [SerializeField] private VRComfortSystem      comfortSystem;
        [SerializeField] private HandTrackingController handTrackingController;
        [SerializeField] private VRRecenterController  recenterController;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when any setting changes.</summary>
        public event Action OnSettingsChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            LoadSettings();
            HookUICallbacks();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Applies the comfort level from the dropdown and persists it.</summary>
        public void ApplyComfortLevel(int dropdownIndex)
        {
            var level = (XRComfortLevel)dropdownIndex;
            comfortSystem?.SetComfortLevel(level);
            PlayerPrefs.SetInt(KeyComfortLevel, dropdownIndex);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>Applies hand tracking enabled/disabled state and persists it.</summary>
        public void ApplyHandTrackingToggle(bool enabled)
        {
            handTrackingController?.SetHandTrackingEnabled(enabled);
            PlayerPrefs.SetInt(KeyHandTracking, enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>Applies standing/seated tracking mode and persists it.</summary>
        public void ApplyStandingModeToggle(bool standing)
        {
            recenterController?.SetTrackingMode(standing ? VRTrackingMode.Standing : VRTrackingMode.Seated);
            PlayerPrefs.SetInt(KeyTrackingMode, standing ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>Applies graphics quality and persists it.</summary>
        public void ApplyGraphicsQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex, applyExpensiveChanges: true);
            PlayerPrefs.SetInt(KeyGraphicsQuality, qualityIndex);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void HookUICallbacks()
        {
            if (comfortLevelDropdown    != null) comfortLevelDropdown.onValueChanged.AddListener(ApplyComfortLevel);
            if (handTrackingToggle      != null) handTrackingToggle.onValueChanged.AddListener(ApplyHandTrackingToggle);
            if (standingModeToggle      != null) standingModeToggle.onValueChanged.AddListener(ApplyStandingModeToggle);
            if (graphicsQualityDropdown != null) graphicsQualityDropdown.onValueChanged.AddListener(ApplyGraphicsQuality);
        }

        private void LoadSettings()
        {
            int comfortIdx  = PlayerPrefs.GetInt(KeyComfortLevel,    (int)XRComfortLevel.Medium);
            bool handTrack  = PlayerPrefs.GetInt(KeyHandTracking,    1) == 1;
            bool standing   = PlayerPrefs.GetInt(KeyTrackingMode,    0) == 1;
            int gfxQuality  = PlayerPrefs.GetInt(KeyGraphicsQuality, 2);

            ApplyComfortLevel(comfortIdx);
            ApplyHandTrackingToggle(handTrack);
            ApplyStandingModeToggle(standing);
            ApplyGraphicsQuality(gfxQuality);

            if (comfortLevelDropdown    != null) comfortLevelDropdown.value    = comfortIdx;
            if (handTrackingToggle      != null) handTrackingToggle.isOn       = handTrack;
            if (standingModeToggle      != null) standingModeToggle.isOn       = standing;
            if (graphicsQualityDropdown != null) graphicsQualityDropdown.value = gfxQuality;
        }
    }
}
