using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Core;

namespace SWEF.Settings
{
    /// <summary>
    /// UI panel that exposes all SettingsManager values to the player.
    /// Sliders and toggles feed directly into SettingsManager; changes are
    /// saved immediately. Reset restores defaults.
    /// Also exposes a quality preset dropdown wired to QualityPresetManager.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button     openButton;
        [SerializeField] private Button     closeButton;
        [SerializeField] private Button     resetButton;

        [Header("Controls")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle comfortToggle;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Slider speedSlider;

        [Header("Phase 13 — Notifications")]
        [SerializeField] private Toggle notificationsToggle;

        [Header("XR")]
        [SerializeField] private Button     xrSettingsButton;
        [SerializeField] private GameObject xrSettingsPanel;

        [Header("Quality Preset (Phase 8)")]
        [SerializeField] private QualityPresetManager qualityManager;
        [SerializeField] private Dropdown             qualityDropdown;

        [Header("Ref")]
        [SerializeField] private SettingsManager settings;

        private bool _ignoreCallbacks;

        private void Awake()
        {
            if (settings == null)
                settings = FindFirstObjectByType<SettingsManager>();

            if (qualityManager == null)
                qualityManager = FindFirstObjectByType<QualityPresetManager>();

            if (openButton  != null) openButton.onClick.AddListener(OpenPanel);
            if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
            if (resetButton != null) resetButton.onClick.AddListener(OnReset);

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.minValue = 0f;
                masterVolumeSlider.maxValue = 1f;
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }
            if (comfortToggle != null)
                comfortToggle.onValueChanged.AddListener(OnComfortToggleChanged);
            if (notificationsToggle != null)
                notificationsToggle.onValueChanged.AddListener(OnNotificationsToggleChanged);
            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = 0.5f;
                sensitivitySlider.maxValue = 3.0f;
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }
            if (speedSlider != null)
            {
                speedSlider.minValue = 50f;
                speedSlider.maxValue = 500f;
                speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            }

            // Populate quality dropdown with preset names
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new List<string>
                {
                    QualityPresetManager.QualityLevel.Low.ToString(),
                    QualityPresetManager.QualityLevel.Medium.ToString(),
                    QualityPresetManager.QualityLevel.High.ToString(),
                    QualityPresetManager.QualityLevel.Ultra.ToString(),
                });
                qualityDropdown.onValueChanged.AddListener(OnQualityDropdownChanged);
            }

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (settings != null)
                settings.OnSettingsChanged += RefreshUI;
            UpdateXRVisibility();
        }

        private void OnDisable()
        {
            if (settings != null)
                settings.OnSettingsChanged -= RefreshUI;
        }

        private void Start()
        {
            RefreshUI();

            // Sync dropdown to the currently active quality preset
            if (qualityDropdown != null && qualityManager != null)
            {
                _ignoreCallbacks          = true;
                qualityDropdown.value     = (int)qualityManager.CurrentQuality;
                _ignoreCallbacks          = false;
            }

            // Show XR settings button only when an XR device is active
            UpdateXRVisibility();
        }

        // ── Panel toggle ─────────────────────────────────────────────────────────
        private void OpenPanel()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            RefreshUI();
        }

        private void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Sync UI to current settings ──────────────────────────────────────────
        private void RefreshUI()
        {
            if (settings == null) return;

            _ignoreCallbacks = true;
            if (masterVolumeSlider != null) masterVolumeSlider.value = settings.MasterVolume;
            if (sfxVolumeSlider    != null) sfxVolumeSlider.value    = settings.SfxVolume;
            if (comfortToggle      != null) comfortToggle.isOn       = settings.ComfortMode;
            if (sensitivitySlider  != null) sensitivitySlider.value  = settings.TouchSensitivity;
            if (speedSlider        != null) speedSlider.value        = settings.MaxSpeed;
            if (notificationsToggle != null) notificationsToggle.isOn = settings.NotificationsEnabled;
            _ignoreCallbacks = false;
        }

        // ── Callbacks ────────────────────────────────────────────────────────────
        private void OnMasterVolumeChanged(float v)
        {
            if (_ignoreCallbacks || settings == null) return;
            settings.SetMasterVolume(v);
            settings.Save();
        }

        private void OnSfxVolumeChanged(float v)
        {
            if (_ignoreCallbacks || settings == null) return;
            settings.SetSfxVolume(v);
            settings.Save();
        }

        private void OnComfortToggleChanged(bool b)
        {
            if (_ignoreCallbacks || settings == null) return;
            settings.SetComfortMode(b);
            settings.Save();
        }

        private void OnNotificationsToggleChanged(bool b)
        {
            if (_ignoreCallbacks || settings == null) return;
            settings.SetNotificationsEnabled(b);
            settings.Save();

            // Propagate to NotificationManager immediately
            var nm = SWEF.Notification.NotificationManager.Instance;
            if (nm != null && !b) nm.CancelAll();
        }

        private void OnSensitivityChanged(float v)
        {
            if (_ignoreCallbacks || settings == null) return;
            settings.SetTouchSensitivity(v);
            settings.Save();
        }

        private void OnSpeedChanged(float v)
        {
            if (_ignoreCallbacks || settings == null) return;
            settings.SetMaxSpeed(v);
            settings.Save();
        }

        private void OnReset()
        {
            if (settings == null) return;
            settings.ResetToDefaults();
        }

        private void OnQualityDropdownChanged(int index)
        {
            if (_ignoreCallbacks || qualityManager == null) return;
            qualityManager.SetQuality((QualityPresetManager.QualityLevel)index);
        }

        private void UpdateXRVisibility()
        {
            bool xrActive = SWEF.XR.XRPlatformDetector.IsXRActive;
            if (xrSettingsButton != null) xrSettingsButton.gameObject.SetActive(xrActive);
            if (xrSettingsPanel  != null) xrSettingsPanel.SetActive(false); // panel starts hidden; opened via button
        }
    }
}
