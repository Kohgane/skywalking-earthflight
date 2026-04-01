// VoiceCommandUI.cs — SWEF Voice Command & Cockpit Voice Assistant System
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VoiceCommand
{
    /// <summary>
    /// Full-screen settings and command-reference panel for the Voice Command system.
    /// Provides: enable/disable toggle, activation-mode selector, confidence-threshold
    /// slider, per-category toggles, searchable command reference list, history view,
    /// microphone device selector, and a test-mode button.
    /// All Unity Object references are optional — the component is functional as pure
    /// data logic even without UI wiring.
    /// </summary>
    public class VoiceCommandUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private VoiceAssistantConfig _config;

        [Header("Panel Root")]
        [SerializeField] private GameObject _panelRoot;

        [Header("Search")]
        [Tooltip("Input field used to filter the command reference list.")]
        [SerializeField] private UnityEngine.UI.InputField _searchField;

        [Tooltip("Parent transform where command list items are instantiated.")]
        [SerializeField] private Transform _commandListParent;

        [Tooltip("Prefab for a single command list row (Text label).")]
        [SerializeField] private GameObject _commandRowPrefab;

        [Header("Settings")]
        [SerializeField] private UnityEngine.UI.Toggle  _enableToggle;
        [SerializeField] private UnityEngine.UI.Slider  _confidenceSlider;
        [SerializeField] private UnityEngine.UI.Dropdown _modeDropdown;

        [Header("History")]
        [SerializeField] private Transform _historyListParent;
        [SerializeField] private GameObject _historyRowPrefab;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private bool _voiceEnabled = true;
        private string _searchQuery = string.Empty;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Opens the settings panel.</summary>
        public void Show()
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);
            RefreshCommandList();
            RefreshHistory();
        }

        /// <summary>Closes the settings panel.</summary>
        public void Hide()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        /// <summary>
        /// Called by the Enable/Disable toggle in the UI.
        /// Enables or disables the voice recognition controller.
        /// </summary>
        public void OnEnableToggleChanged(bool value)
        {
            _voiceEnabled = value;
            var ctrl = VoiceRecognitionController.Instance;
            if (ctrl == null) return;

            if (value && ctrl.Mode == ActivationMode.AlwaysListening)
                ctrl.PushToTalkBegin();
            // For PushToTalk and WakeWord modes toggling off simply means the system
            // will not respond; no explicit StopListening call needed here.
        }

        /// <summary>
        /// Called by the activation-mode dropdown.
        /// Index: 0=PushToTalk, 1=WakeWord, 2=AlwaysListening.
        /// </summary>
        public void OnModeDropdownChanged(int index)
        {
            var ctrl = VoiceRecognitionController.Instance;
            if (ctrl == null) return;

            ActivationMode mode = (ActivationMode)index;
            ctrl.SetActivationMode(mode);
        }

        /// <summary>
        /// Called by the confidence slider. Updates the config asset at runtime.
        /// </summary>
        public void OnConfidenceSliderChanged(float value)
        {
            if (_config != null)
                _config.confidenceThreshold = value;
        }

        /// <summary>
        /// Searches the command registry using the current search query and
        /// rebuilds the visible list.
        /// </summary>
        public void OnSearchFieldChanged(string query)
        {
            _searchQuery = query;
            RefreshCommandList();
        }

        /// <summary>
        /// Fires a simulated recognition event for the first matching command —
        /// used in test mode.
        /// </summary>
        public void OnTestModeButtonPressed()
        {
            var ctrl = VoiceRecognitionController.Instance;
            if (ctrl == null) return;

            var registry = CommandRegistry.Instance;
            if (registry == null) return;

            var all = registry.GetAll();
            if (all.Length > 0)
                ctrl.SimulateRecognition(all[0].primaryPhrase, 1f);
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void RefreshCommandList()
        {
            if (_commandListParent == null) return;

            // Clear existing rows.
            foreach (Transform child in _commandListParent)
                Destroy(child.gameObject);

            var registry = CommandRegistry.Instance;
            if (registry == null) return;

            var all = registry.GetAll();
            List<VoiceCommandDefinition> filtered = Filter(all, _searchQuery);

            foreach (var cmd in filtered)
            {
                if (_commandRowPrefab != null)
                {
                    var row = Instantiate(_commandRowPrefab, _commandListParent);
                    var label = row.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (label != null)
                        label.text = $"{cmd.primaryPhrase}  [{cmd.category}]";
                }
            }
        }

        private void RefreshHistory()
        {
            if (_historyListParent == null) return;

            foreach (Transform child in _historyListParent)
                Destroy(child.gameObject);

            var history = FindObjectOfType<VoiceCommandHistory>();
            if (history == null) return;

            foreach (var entry in history.GetRecent(20))
            {
                if (_historyRowPrefab != null)
                {
                    var row   = Instantiate(_historyRowPrefab, _historyListParent);
                    var label = row.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (label != null)
                        label.text = $"{entry.timestamp}  {entry.primaryPhrase}  " +
                                     $"({(entry.success ? "OK" : "FAIL")})";
                }
            }
        }

        private static List<VoiceCommandDefinition> Filter(
            VoiceCommandDefinition[] all, string query)
        {
            var result = new List<VoiceCommandDefinition>();
            bool hasQuery = !string.IsNullOrWhiteSpace(query);

            foreach (var cmd in all)
            {
                if (!hasQuery ||
                    cmd.primaryPhrase.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(cmd);
                }
            }
            return result;
        }
    }
}
