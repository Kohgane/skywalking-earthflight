// Phase 100 — AI Co-Pilot & Smart Assistant
// Assets/SWEF/Scripts/AICoPilot/AICoPilotUIPanel.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.AICoPilot
{
    /// <summary>
    /// Cockpit UI panel for the AI Co-Pilot.
    /// Shows the message log, quick-response buttons, and AI status indicator.
    /// Works on PC, Mobile, Tablet via responsive layout groups.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class AICoPilotUIPanel : MonoBehaviour
    {
        #region Inspector — Panel Layout

        [Header("Panel Root")]
        [Tooltip("Root panel container that is shown/hidden when collapsing.")]
        [SerializeField] private GameObject _panelRoot;

        [Tooltip("Toggle button to expand/collapse the panel.")]
        [SerializeField] private Button _toggleButton;

        [Header("Status")]
        [Tooltip("Text element showing current AI status (ACTIVE / PASSIVE / OFF).")]
        [SerializeField] private Text _statusLabel;

        [Tooltip("Image used as status indicator dot (green/yellow/red).")]
        [SerializeField] private Image _statusIndicator;

        [SerializeField] private Color _colorActive  = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color _colorPassive = new Color(0.9f, 0.8f, 0.1f);
        [SerializeField] private Color _colorOff     = new Color(0.5f, 0.5f, 0.5f);

        [Header("Message Display")]
        [Tooltip("Text element for the current (latest) AI message.")]
        [SerializeField] private Text _currentMessageText;

        [Tooltip("Parent transform that holds the message log entry prefabs.")]
        [SerializeField] private Transform _logContainer;

        [Tooltip("Prefab used to create log entry items (must contain a Text component).")]
        [SerializeField] private GameObject _logEntryPrefab;

        [Tooltip("Maximum number of log entries to display.")]
        [SerializeField] private int _maxLogEntries = 8;

        [Header("Quick-Response Buttons")]
        [SerializeField] private Button _rogerButton;
        [SerializeField] private Button _explainButton;
        [SerializeField] private Button _silenceButton;
        [SerializeField] private Button _repeatButton;

        [Header("Settings")]
        [Tooltip("Button that opens the AI Co-Pilot settings screen.")]
        [SerializeField] private Button _settingsButton;

        #endregion

        #region Private State

        private bool _expanded = true;
        private readonly List<Text> _logEntryPool = new List<Text>();
        private AICoPilotDialogueManager _dialogue;
        private AICoPilotManager _manager;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _dialogue = AICoPilotDialogueManager.Instance;
            _manager  = AICoPilotManager.Instance;

            BindButtons();
            BindEvents();
            RefreshStatusIndicator();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        #endregion

        #region Setup

        private void BindButtons()
        {
            if (_toggleButton  != null) _toggleButton.onClick.AddListener(TogglePanel);
            if (_rogerButton   != null) _rogerButton.onClick.AddListener(OnRoger);
            if (_explainButton != null) _explainButton.onClick.AddListener(OnExplain);
            if (_silenceButton != null) _silenceButton.onClick.AddListener(OnSilence);
            if (_repeatButton  != null) _repeatButton.onClick.AddListener(OnRepeat);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettings);
        }

        private void BindEvents()
        {
            if (_dialogue != null)
            {
                _dialogue.OnMessageDisplayed += HandleMessageDisplayed;
                _dialogue.OnMessageCleared   += HandleMessageCleared;
            }

            if (_manager != null)
                _manager.OnAssistanceLevelChanged += HandleAssistanceLevelChanged;
        }

        private void UnbindEvents()
        {
            if (_dialogue != null)
            {
                _dialogue.OnMessageDisplayed -= HandleMessageDisplayed;
                _dialogue.OnMessageCleared   -= HandleMessageCleared;
            }

            if (_manager != null)
                _manager.OnAssistanceLevelChanged -= HandleAssistanceLevelChanged;
        }

        #endregion

        #region Panel Toggle

        /// <summary>Expands or collapses the co-pilot panel.</summary>
        public void TogglePanel()
        {
            _expanded = !_expanded;
            if (_panelRoot != null)
                _panelRoot.SetActive(_expanded);
        }

        #endregion

        #region Event Handlers

        private void HandleMessageDisplayed(DialogueMessage msg)
        {
            if (_currentMessageText != null)
                _currentMessageText.text = $"[{msg.Category}] {msg.Text}";

            AddLogEntry(msg);
        }

        private void HandleMessageCleared()
        {
            if (_currentMessageText != null)
                _currentMessageText.text = string.Empty;
        }

        private void HandleAssistanceLevelChanged(AssistanceLevel level)
        {
            RefreshStatusIndicator();
        }

        #endregion

        #region Log Entries

        private void AddLogEntry(DialogueMessage msg)
        {
            if (_logContainer == null || _logEntryPrefab == null) return;

            // Trim old entries.
            while (_logEntryPool.Count >= _maxLogEntries && _logEntryPool.Count > 0)
            {
                var oldest = _logEntryPool[0];
                _logEntryPool.RemoveAt(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }

            var go = Instantiate(_logEntryPrefab, _logContainer);
            var txt = go.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = $"[{msg.Category}] {msg.Text}";
                _logEntryPool.Add(txt);
            }
        }

        #endregion

        #region Status Indicator

        private void RefreshStatusIndicator()
        {
            if (_manager == null) return;

            AssistanceLevel level = _manager.CurrentAssistanceLevel;
            Color color;
            string label;

            switch (level)
            {
                case AssistanceLevel.Full:
                case AssistanceLevel.Active:
                    color = _colorActive;
                    label = level == AssistanceLevel.Full ? "ARIA — FULL" : "ARIA — ACTIVE";
                    break;
                case AssistanceLevel.Passive:
                    color = _colorPassive;
                    label = "ARIA — PASSIVE";
                    break;
                default:
                    color = _colorOff;
                    label = "ARIA — OFF";
                    break;
            }

            if (_statusIndicator != null) _statusIndicator.color = color;
            if (_statusLabel     != null) _statusLabel.text = label;
        }

        #endregion

        #region Quick-Response Handlers

        private void OnRoger()
        {
            // Acknowledge current message — no action beyond dismissal.
            HandleMessageCleared();
        }

        private void OnExplain()
        {
            // Request elaboration: re-display last message with an explanatory suffix.
            if (_dialogue == null) return;
            var log = _dialogue.MessageLog;
            if (log.Count == 0) return;
            var last = log[log.Count - 1];
            _dialogue.EnqueueMessage("ARIA", $"To clarify: {last.Text}", MessagePriority.Info);
        }

        private void OnSilence()
        {
            _dialogue?.Silence();
            RefreshStatusIndicator();
        }

        private void OnRepeat()
        {
            _dialogue?.RepeatLast();
        }

        private void OnSettings()
        {
            // Open settings screen — integrate with SWEF.UI navigation system.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[AICoPilotUIPanel] Settings button pressed.");
#endif
        }

        #endregion
    }
}
