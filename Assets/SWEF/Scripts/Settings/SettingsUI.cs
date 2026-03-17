using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Settings
{
    /// <summary>
    /// UI panel that exposes all SettingsManager values to the player.
    /// Sliders and toggles feed directly into SettingsManager; changes are
    /// saved immediately. Reset restores defaults.
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

        [Header("Ref")]
        [SerializeField] private SettingsManager settings;

        private bool _ignoreCallbacks;

        private void Awake()
        {
            if (settings == null)
                settings = FindFirstObjectByType<SettingsManager>();

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

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (settings != null)
                settings.OnSettingsChanged += RefreshUI;
        }

        private void OnDisable()
        {
            if (settings != null)
                settings.OnSettingsChanged -= RefreshUI;
        }

        private void Start()
        {
            RefreshUI();
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
    }
}
