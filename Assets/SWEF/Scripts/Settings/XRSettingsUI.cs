using UnityEngine;
using UnityEngine.UI;
using SWEF.XR;

namespace SWEF.Settings
{
    /// <summary>
    /// Settings panel for XR-specific options (comfort level, snap turning,
    /// tunnel vision, UI distance, head follow, and recenter).
    /// The panel is only shown when an XR device is active.
    /// </summary>
    public class XRSettingsUI : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject xrSettingsPanel;

        [Header("Controls")]
        [SerializeField] private Dropdown comfortLevelDropdown;
        [SerializeField] private Toggle   snapTurningToggle;
        [SerializeField] private Toggle   tunnelVisionToggle;
        [SerializeField] private Slider   uiDistanceSlider;
        [SerializeField] private Toggle   followHeadToggle;
        [SerializeField] private Button   recenterButton;

        [Header("Info")]
        [SerializeField] private Text platformInfoText;

        // ── Private references ────────────────────────────────────────────────────
        private XRComfortSettings _comfortSettings;
        private XRUIAdapter       _uiAdapter;
        private bool              _ignoreCallbacks;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _comfortSettings = FindFirstObjectByType<XRComfortSettings>();
            _uiAdapter       = FindFirstObjectByType<XRUIAdapter>();

            // Populate comfort level dropdown from enum
            if (comfortLevelDropdown != null)
            {
                comfortLevelDropdown.ClearOptions();
                comfortLevelDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    XRComfortSettings.ComfortLevel.Low.ToString(),
                    XRComfortSettings.ComfortLevel.Medium.ToString(),
                    XRComfortSettings.ComfortLevel.High.ToString(),
                    XRComfortSettings.ComfortLevel.Custom.ToString(),
                });
                comfortLevelDropdown.onValueChanged.AddListener(OnComfortDropdownChanged);
            }

            if (snapTurningToggle  != null) snapTurningToggle.onValueChanged.AddListener(OnSnapTurningChanged);
            if (tunnelVisionToggle != null) tunnelVisionToggle.onValueChanged.AddListener(OnTunnelVisionChanged);

            if (uiDistanceSlider != null)
            {
                uiDistanceSlider.minValue = 1f;
                uiDistanceSlider.maxValue = 5f;
                uiDistanceSlider.onValueChanged.AddListener(OnUIDistanceChanged);
            }

            if (followHeadToggle != null) followHeadToggle.onValueChanged.AddListener(OnFollowHeadChanged);
            if (recenterButton   != null) recenterButton.onClick.AddListener(OnRecenterClicked);

            // Show or hide panel based on XR active state
            UpdatePanelVisibility();

            // Subscribe to comfort settings changes
            if (_comfortSettings != null)
                _comfortSettings.OnComfortLevelChanged += _ => RefreshUI();
        }

        private void OnEnable()
        {
            UpdatePanelVisibility();
            RefreshUI();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void UpdatePanelVisibility()
        {
            if (xrSettingsPanel != null)
                xrSettingsPanel.SetActive(XRPlatformDetector.IsXRActive);
        }

        private void RefreshUI()
        {
            _ignoreCallbacks = true;

            // Platform info
            if (platformInfoText != null)
                platformInfoText.text =
                    $"Platform: {XRPlatformDetector.CurrentPlatform}\nDevice: {XRPlatformDetector.DeviceName}";

            // Comfort settings
            if (_comfortSettings != null)
            {
                if (comfortLevelDropdown != null)
                    comfortLevelDropdown.value = (int)_comfortSettings.CurrentLevel;
            }

            _ignoreCallbacks = false;
        }

        // ── Callbacks ─────────────────────────────────────────────────────────────

        private void OnComfortDropdownChanged(int index)
        {
            if (_ignoreCallbacks || _comfortSettings == null) return;
            _comfortSettings.SetComfortLevel((XRComfortSettings.ComfortLevel)index);
        }

        private void OnSnapTurningChanged(bool value)
        {
            if (_ignoreCallbacks) return;
            // Snap turning is managed inside XRComfortSettings via Custom level
            if (_comfortSettings != null)
                _comfortSettings.SetComfortLevel(XRComfortSettings.ComfortLevel.Custom);
        }

        private void OnTunnelVisionChanged(bool value)
        {
            if (_ignoreCallbacks) return;
            if (_comfortSettings != null)
                _comfortSettings.SetComfortLevel(XRComfortSettings.ComfortLevel.Custom);
        }

        private void OnUIDistanceChanged(float value)
        {
            if (_ignoreCallbacks || _uiAdapter == null) return;
            _uiAdapter.SetUIDistance(value);
        }

        private void OnFollowHeadChanged(bool value)
        {
            if (_ignoreCallbacks || _uiAdapter == null) return;
            _uiAdapter.SetFollowHead(value);
        }

        private void OnRecenterClicked()
        {
            XRRigManager.Instance?.RecenterXR();
        }
    }
}
