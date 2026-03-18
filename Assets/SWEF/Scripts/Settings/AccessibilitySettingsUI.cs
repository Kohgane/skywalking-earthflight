using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Haptic;

namespace SWEF.Settings
{
    /// <summary>
    /// UI panel for Phase 16 accessibility settings.
    /// Wires all controls to <see cref="SWEF.UI.AccessibilityController"/>,
    /// <see cref="SWEF.UI.OneHandedModeController"/>, <see cref="HapticManager"/>,
    /// and <see cref="SWEF.UI.VoiceCommandManager"/> at runtime.
    /// </summary>
    public class AccessibilitySettingsUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject accessibilityPanel;

        [Header("Colorblind")]
        [SerializeField] private Dropdown colorblindModeDropdown;

        [Header("Text Scale")]
        [SerializeField] private Slider textScaleSlider;

        [Header("Motion & Layout")]
        [SerializeField] private Toggle reducedMotionToggle;
        [SerializeField] private Toggle oneHandedModeToggle;
        [SerializeField] private Dropdown handPreferenceDropdown;

        [Header("Haptics")]
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Slider hapticIntensitySlider;

        [Header("Voice Commands")]
        [SerializeField] private Toggle voiceCommandsToggle;
        [SerializeField] private GameObject voiceComingSoonLabel;

        [Header("Screen Reader")]
        [SerializeField] private Toggle screenReaderToggle;

        [Header("Reset")]
        [SerializeField] private Button resetAccessibilityButton;

        // ── Runtime references ────────────────────────────────────────────────────
        private SWEF.UI.AccessibilityController  _accessCtrl;
        private SWEF.UI.OneHandedModeController  _oneHanded;
        private SWEF.UI.VoiceCommandManager      _voiceCmd;

        private bool _ignoreCallbacks;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _accessCtrl = FindFirstObjectByType<SWEF.UI.AccessibilityController>();
            _oneHanded  = FindFirstObjectByType<SWEF.UI.OneHandedModeController>();
            _voiceCmd   = FindFirstObjectByType<SWEF.UI.VoiceCommandManager>();

            WireControls();

            if (accessibilityPanel != null)
                accessibilityPanel.SetActive(false);
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void Start()
        {
            RefreshUI();
        }

        // ── Panel open/close ─────────────────────────────────────────────────────

        /// <summary>Opens the accessibility settings panel.</summary>
        public void OpenPanel()
        {
            if (accessibilityPanel != null)
                accessibilityPanel.SetActive(true);
            RefreshUI();
        }

        /// <summary>Closes the accessibility settings panel.</summary>
        public void ClosePanel()
        {
            if (accessibilityPanel != null)
                accessibilityPanel.SetActive(false);
        }

        // ── Internal wiring ───────────────────────────────────────────────────────

        private void WireControls()
        {
            // Colorblind dropdown
            if (colorblindModeDropdown != null)
            {
                colorblindModeDropdown.ClearOptions();
                colorblindModeDropdown.AddOptions(new List<string>
                {
                    "Normal", "Protanopia", "Deuteranopia", "Tritanopia", "Achromatopsia"
                });
                colorblindModeDropdown.onValueChanged.AddListener(OnColorblindModeChanged);
            }

            // Text scale slider
            if (textScaleSlider != null)
            {
                textScaleSlider.minValue = 0.8f;
                textScaleSlider.maxValue = 2.0f;
                textScaleSlider.onValueChanged.AddListener(OnTextScaleChanged);
            }

            // Reduced motion
            if (reducedMotionToggle != null)
                reducedMotionToggle.onValueChanged.AddListener(OnReducedMotionChanged);

            // One-handed mode
            if (oneHandedModeToggle != null)
                oneHandedModeToggle.onValueChanged.AddListener(OnOneHandedModeChanged);

            // Hand preference dropdown
            if (handPreferenceDropdown != null)
            {
                handPreferenceDropdown.ClearOptions();
                handPreferenceDropdown.AddOptions(new List<string> { "Right", "Left" });
                handPreferenceDropdown.onValueChanged.AddListener(OnHandPreferenceChanged);
            }

            // Haptics
            if (hapticsToggle != null)
                hapticsToggle.onValueChanged.AddListener(OnHapticsToggleChanged);

            if (hapticIntensitySlider != null)
            {
                hapticIntensitySlider.minValue = 0f;
                hapticIntensitySlider.maxValue = 1f;
                hapticIntensitySlider.onValueChanged.AddListener(OnHapticIntensityChanged);
            }

            // Voice commands
            if (voiceCommandsToggle != null)
                voiceCommandsToggle.onValueChanged.AddListener(OnVoiceCommandsToggleChanged);

            // Screen reader
            if (screenReaderToggle != null)
                screenReaderToggle.onValueChanged.AddListener(OnScreenReaderChanged);

            // Reset
            if (resetAccessibilityButton != null)
                resetAccessibilityButton.onClick.AddListener(OnResetAccessibility);
        }

        private void RefreshUI()
        {
            _ignoreCallbacks = true;

            if (_accessCtrl != null)
            {
                if (colorblindModeDropdown != null)
                    colorblindModeDropdown.value = (int)_accessCtrl.ActiveColorblindMode;

                if (textScaleSlider != null)
                    textScaleSlider.value = _accessCtrl.TextScaleMultiplier;

                if (reducedMotionToggle != null)
                    reducedMotionToggle.isOn = _accessCtrl.ReducedMotionEnabled;

                if (screenReaderToggle != null)
                    screenReaderToggle.isOn = _accessCtrl.ScreenReaderEnabled;
            }

            if (_oneHanded != null)
            {
                if (oneHandedModeToggle != null)
                    oneHandedModeToggle.isOn = _oneHanded.IsOneHandedModeEnabled;

                if (handPreferenceDropdown != null)
                {
                    handPreferenceDropdown.value = (int)_oneHanded.ActiveHandPreference;
                    handPreferenceDropdown.gameObject.SetActive(_oneHanded.IsOneHandedModeEnabled);
                }
            }

            if (HapticManager.Instance != null)
            {
                if (hapticsToggle != null)
                    hapticsToggle.isOn = HapticManager.Instance.HapticsEnabled;

                if (hapticIntensitySlider != null)
                    hapticIntensitySlider.value = HapticManager.Instance.HapticIntensity;
            }

            if (_voiceCmd != null && voiceCommandsToggle != null)
                voiceCommandsToggle.isOn = _voiceCmd.VoiceCommandsEnabled;

            _ignoreCallbacks = false;
        }

        // ── Callbacks ────────────────────────────────────────────────────────────

        private void OnColorblindModeChanged(int index)
        {
            if (_ignoreCallbacks || _accessCtrl == null) return;
            _accessCtrl.SetColorblindMode((SWEF.UI.ColorblindMode)index);
        }

        private void OnTextScaleChanged(float value)
        {
            if (_ignoreCallbacks || _accessCtrl == null) return;
            _accessCtrl.SetTextScale(value);
        }

        private void OnReducedMotionChanged(bool value)
        {
            if (_ignoreCallbacks || _accessCtrl == null) return;
            _accessCtrl.SetReducedMotion(value);
        }

        private void OnOneHandedModeChanged(bool value)
        {
            if (_ignoreCallbacks || _oneHanded == null) return;
            _oneHanded.SetOneHandedMode(value);
            if (handPreferenceDropdown != null)
                handPreferenceDropdown.gameObject.SetActive(value);
        }

        private void OnHandPreferenceChanged(int index)
        {
            if (_ignoreCallbacks || _oneHanded == null) return;
            _oneHanded.SetHandPreference((SWEF.UI.OneHandedModeController.HandPreference)index);
        }

        private void OnHapticsToggleChanged(bool value)
        {
            if (_ignoreCallbacks) return;
            HapticManager.Instance?.SetHapticsEnabled(value);
        }

        private void OnHapticIntensityChanged(float value)
        {
            if (_ignoreCallbacks) return;
            HapticManager.Instance?.SetHapticIntensity(value);
        }

        private void OnVoiceCommandsToggleChanged(bool value)
        {
            if (_ignoreCallbacks || _voiceCmd == null) return;
            _voiceCmd.SetVoiceCommandsEnabled(value);
            // Show "Coming Soon" label when toggled on
            if (voiceComingSoonLabel != null)
                voiceComingSoonLabel.SetActive(value);
        }

        private void OnScreenReaderChanged(bool value)
        {
            if (_ignoreCallbacks || _accessCtrl == null) return;
            _accessCtrl.SetScreenReaderEnabled(value);
        }

        private void OnResetAccessibility()
        {
            _accessCtrl?.SetColorblindMode(SWEF.UI.ColorblindMode.Normal);
            _accessCtrl?.SetTextScale(SWEF.UI.AccessibilityController.DefaultTextScale);
            _accessCtrl?.SetReducedMotion(false);
            _accessCtrl?.SetScreenReaderEnabled(false);
            _oneHanded?.SetOneHandedMode(false);
            HapticManager.Instance?.SetHapticsEnabled(true);
            HapticManager.Instance?.SetHapticIntensity(1.0f);
            _voiceCmd?.SetVoiceCommandsEnabled(false);

            RefreshUI();
            Debug.Log("[SWEF] Accessibility settings reset to defaults.");
        }
    }
}
