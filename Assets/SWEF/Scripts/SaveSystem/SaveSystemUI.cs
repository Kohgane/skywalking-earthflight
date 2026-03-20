using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.SaveSystem
{
    /// <summary>
    /// Phase 35 — Save system UI controller.
    /// Manages the in-game save-slot selection panel: renders slot cards,
    /// handles Save / Load / Delete / Export / Import actions, and shows a
    /// conflict-resolution prompt when needed.
    /// Assign the serialised fields in the Inspector.
    /// </summary>
    public class SaveSystemUI : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SaveSystemUI Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Raised when the save-slot panel opens.</summary>
        public event Action OnPanelOpened;

        /// <summary>Raised when the save-slot panel closes.</summary>
        public event Action OnPanelClosed;

        // ── Inspector references ─────────────────────────────────────────────
        [Header("Panels")]
        [Tooltip("Root GameObject for the save-slot panel.")]
        [SerializeField] private GameObject savePanel;

        [Tooltip("Root GameObject for the conflict-resolution prompt.")]
        [SerializeField] private GameObject conflictPanel;

        [Header("Slot cards (assign in order: slot 0, 1, 2, 3=auto, 4=quick)")]
        [SerializeField] private SaveSlotCard[] slotCards;

        [Header("Action buttons")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button exportButton;
        [SerializeField] private Button importButton;
        [SerializeField] private Button closeButton;

        [Header("Conflict-resolution buttons")]
        [SerializeField] private Button conflictUseLocalButton;
        [SerializeField] private Button conflictUseCloudButton;
        [SerializeField] private Button conflictMergeButton;
        [SerializeField] private Text   conflictDescriptionText;

        [Header("Feedback")]
        [SerializeField] private Text   statusText;
        [SerializeField] private float  statusClearDelaySec = 3f;

        // ── Internal state ────────────────────────────────────────────────────
        private int  _selectedSlot       = -1;
        private int  _pendingConflictSlot = -1;
        private bool _isSaveMode         = true;  // true = Save panel; false = Load panel

        private float _statusClearTimer;
        private bool  _showingStatus;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BindButtons();
        }

        private void OnEnable()
        {
            var sm = SaveManager.Instance;
            if (sm != null)
            {
                sm.OnSaveCompleted += HandleSaveCompleted;
                sm.OnLoadCompleted += HandleLoadCompleted;
                sm.OnSlotDeleted   += HandleSlotDeleted;
            }

            var cr = SaveConflictResolver.Instance;
            if (cr != null)
                cr.OnConflictDetected += HandleConflictDetected;
        }

        private void OnDisable()
        {
            var sm = SaveManager.Instance;
            if (sm != null)
            {
                sm.OnSaveCompleted -= HandleSaveCompleted;
                sm.OnLoadCompleted -= HandleLoadCompleted;
                sm.OnSlotDeleted   -= HandleSlotDeleted;
            }

            var cr = SaveConflictResolver.Instance;
            if (cr != null)
                cr.OnConflictDetected -= HandleConflictDetected;
        }

        private void Update()
        {
            if (!_showingStatus) return;
            _statusClearTimer -= Time.unscaledDeltaTime;
            if (_statusClearTimer <= 0f)
            {
                _showingStatus = false;
                if (statusText != null)
                    statusText.text = string.Empty;
            }
        }

        // ── Panel visibility ──────────────────────────────────────────────────

        /// <summary>Opens the panel in Save mode (active Save button).</summary>
        public void OpenSaveMode()
        {
            _isSaveMode = true;
            Open();
        }

        /// <summary>Opens the panel in Load mode (active Load button).</summary>
        public void OpenLoadMode()
        {
            _isSaveMode = false;
            Open();
        }

        /// <summary>Toggles panel visibility.</summary>
        public void Toggle()
        {
            if (savePanel != null && savePanel.activeSelf)
                Close();
            else
                Open();
        }

        /// <summary>Closes the save-slot panel.</summary>
        public void Close()
        {
            if (savePanel != null)
                savePanel.SetActive(false);

            _selectedSlot = -1;
            UpdateActionButtons();
            OnPanelClosed?.Invoke();
        }

        private void Open()
        {
            RefreshSlotCards();

            if (savePanel != null)
                savePanel.SetActive(true);

            if (conflictPanel != null)
                conflictPanel.SetActive(false);

            _selectedSlot = -1;
            UpdateActionButtons();
            OnPanelOpened?.Invoke();
        }

        // ── Slot selection ────────────────────────────────────────────────────

        /// <summary>Called by a <see cref="SaveSlotCard"/> when the player taps it.</summary>
        public void SelectSlot(int slotIndex)
        {
            _selectedSlot = slotIndex;
            UpdateActionButtons();

            for (int i = 0; slotCards != null && i < slotCards.Length; i++)
            {
                if (slotCards[i] != null)
                    slotCards[i].SetSelected(i == slotIndex);
            }

            Debug.Log($"[SWEF] SaveSystemUI: slot {slotIndex} selected.");
        }

        // ── Action handlers ───────────────────────────────────────────────────

        private void OnSaveClicked()
        {
            if (_selectedSlot < 0 || _selectedSlot >= SaveSystemConstants.ManualSlotCount)
            {
                ShowStatus($"Select a manual save slot (slot 1–{SaveSystemConstants.ManualSlotCount}) first.", isError: true);
                return;
            }
            ShowStatus("Saving…");
            SaveManager.Instance?.Save(_selectedSlot);
        }

        private void OnLoadClicked()
        {
            if (_selectedSlot < 0)
            {
                ShowStatus("Select a slot to load.", isError: true);
                return;
            }
            var info = SaveManager.Instance?.GetSlotInfo(_selectedSlot);
            if (info == null || info.isEmpty)
            {
                ShowStatus("This slot is empty.", isError: true);
                return;
            }
            ShowStatus("Loading…");
            SaveManager.Instance?.Load(_selectedSlot);
        }

        private void OnDeleteClicked()
        {
            if (_selectedSlot < 0)
            {
                ShowStatus("Select a slot to delete.", isError: true);
                return;
            }
            SaveManager.Instance?.Delete(_selectedSlot);
        }

        private void OnExportClicked()
        {
            if (_selectedSlot < 0)
            {
                ShowStatus("Select a slot to export.", isError: true);
                return;
            }
            var result = SaveExportImport.Instance?.ExportSlot(_selectedSlot);
            if (result != null)
                ShowStatus($"Exported to: {System.IO.Path.GetFileName(result)}");
            else
                ShowStatus("Export failed.", isError: true);
        }

        private void OnImportClicked()
        {
            // In a real project this would open a native file picker;
            // here we show a status message indicating the feature is ready.
            ShowStatus("Import: use SaveExportImport.Instance.ImportToSlot(path, slot) to import.");
        }

        private void OnCloseClicked() => Close();

        // ── Conflict-resolution handlers ──────────────────────────────────────

        private void HandleConflictDetected(int slotIndex)
        {
            _pendingConflictSlot = slotIndex;

            if (conflictPanel != null)
                conflictPanel.SetActive(true);

            if (conflictDescriptionText != null)
            {
                var info = SaveManager.Instance?.GetSlotInfo(slotIndex);
                conflictDescriptionText.text =
                    $"Conflict detected in slot {slotIndex} ({info?.displayName ?? "??"}).\n" +
                    "Both local and cloud copies have unsaved changes.\n" +
                    "Which version do you want to keep?";
            }

            Debug.Log($"[SWEF] SaveSystemUI: showing conflict resolution for slot {slotIndex}.");
        }

        private void OnConflictUseLocal()
        {
            SaveConflictResolver.Instance?.ResolveUseLocal(_pendingConflictSlot);
            HideConflictPanel();
            ShowStatus("Local save kept.");
        }

        private void OnConflictUseCloud()
        {
            SaveConflictResolver.Instance?.ResolveUseCloud(_pendingConflictSlot);
            HideConflictPanel();
            RefreshSlotCards();
            ShowStatus("Cloud save applied.");
        }

        private void OnConflictMerge()
        {
            SaveConflictResolver.Instance?.ResolveMerge(_pendingConflictSlot);
            HideConflictPanel();
            RefreshSlotCards();
            ShowStatus("Saves merged.");
        }

        private void HideConflictPanel()
        {
            if (conflictPanel != null)
                conflictPanel.SetActive(false);
            _pendingConflictSlot = -1;
        }

        // ── Save / load completion handlers ───────────────────────────────────

        private void HandleSaveCompleted(int slotIndex, bool success)
        {
            RefreshSlotCards();
            ShowStatus(success ? $"Slot {slotIndex + 1} saved." : $"Save failed.", !success);
        }

        private void HandleLoadCompleted(int slotIndex, bool success)
        {
            ShowStatus(success ? $"Slot {slotIndex + 1} loaded." : $"Load failed.", !success);
            if (success) Close();
        }

        private void HandleSlotDeleted(int slotIndex)
        {
            RefreshSlotCards();
            ShowStatus($"Slot {slotIndex + 1} deleted.");
            _selectedSlot = -1;
            UpdateActionButtons();
        }

        // ── UI helpers ─────────────────────────────────────────────────────────

        private void RefreshSlotCards()
        {
            if (slotCards == null) return;
            var mgr   = SaveManager.Instance;
            var infos = mgr?.GetAllSlotInfos();

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] == null) continue;
                var info = (infos != null && i < infos.Length) ? infos[i] : null;
                slotCards[i].Refresh(info);
            }
        }

        private void UpdateActionButtons()
        {
            bool hasSelection       = _selectedSlot >= 0;
            bool isManualSlot       = _selectedSlot >= 0 && _selectedSlot < SaveSystemConstants.ManualSlotCount;
            bool slotHasData        = hasSelection &&
                                      !(SaveManager.Instance?.GetSlotInfo(_selectedSlot)?.isEmpty ?? true);

            if (saveButton   != null) saveButton.interactable   = isManualSlot && _isSaveMode;
            if (loadButton   != null) loadButton.interactable   = hasSelection && slotHasData;
            if (deleteButton != null) deleteButton.interactable = hasSelection && slotHasData;
            if (exportButton != null) exportButton.interactable = hasSelection && slotHasData;
            if (importButton != null) importButton.interactable = isManualSlot;
        }

        private void ShowStatus(string message, bool isError = false)
        {
            if (statusText == null) return;
            statusText.text  = message;
            statusText.color = isError ? Color.red : Color.white;
            _statusClearTimer = statusClearDelaySec;
            _showingStatus    = true;
        }

        private void BindButtons()
        {
            if (saveButton   != null) saveButton.onClick.AddListener(OnSaveClicked);
            if (loadButton   != null) loadButton.onClick.AddListener(OnLoadClicked);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
            if (exportButton != null) exportButton.onClick.AddListener(OnExportClicked);
            if (importButton != null) importButton.onClick.AddListener(OnImportClicked);
            if (closeButton  != null) closeButton.onClick.AddListener(OnCloseClicked);

            if (conflictUseLocalButton != null) conflictUseLocalButton.onClick.AddListener(OnConflictUseLocal);
            if (conflictUseCloudButton != null) conflictUseCloudButton.onClick.AddListener(OnConflictUseCloud);
            if (conflictMergeButton    != null) conflictMergeButton.onClick.AddListener(OnConflictMerge);
        }
    }

    // ── SaveSlotCard helper component ─────────────────────────────────────────

    /// <summary>
    /// UI component representing a single save-slot card.
    /// Assign Text/Image/Button references in the Inspector.
    /// </summary>
    public class SaveSlotCard : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Text  slotNameText;
        [SerializeField] private Text  timestampText;
        [SerializeField] private Text  playTimeText;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Image selectedHighlight;
        [SerializeField] private Text  syncStatusText;
        [SerializeField] private Button cardButton;

        private int _slotIndex;

        private void Awake()
        {
            if (cardButton != null)
                cardButton.onClick.AddListener(() => SaveSystemUI.Instance?.SelectSlot(_slotIndex));
        }

        /// <summary>Populates the card with data from <paramref name="info"/>.</summary>
        public void Refresh(SaveSlotInfo info)
        {
            if (info != null) _slotIndex = info.slotIndex;

            if (info == null || info.isEmpty)
            {
                if (slotNameText  != null) slotNameText.text  = $"Slot {_slotIndex + 1} — Empty";
                if (timestampText != null) timestampText.text = string.Empty;
                if (playTimeText  != null) playTimeText.text  = string.Empty;
                if (syncStatusText != null) syncStatusText.text = string.Empty;
                if (thumbnailImage != null) thumbnailImage.enabled = false;
                return;
            }

            if (slotNameText != null)
                slotNameText.text = string.IsNullOrEmpty(info.displayName)
                    ? $"Slot {info.slotIndex + 1}"
                    : info.displayName;

            if (timestampText != null)
            {
                if (DateTime.TryParse(info.timestamp, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
                    timestampText.text = dt.ToLocalTime().ToString("g");
                else
                    timestampText.text = info.timestamp;
            }

            if (playTimeText != null)
            {
                float sec  = info.playTimeSec;
                int   hrs  = (int)(sec / 3600f);
                int   mins = (int)((sec % 3600f) / 60f);
                playTimeText.text = hrs > 0 ? $"{hrs}h {mins}m" : $"{mins}m";
            }

            if (syncStatusText != null)
                syncStatusText.text = info.cloudSyncStatus.ToString();

            if (thumbnailImage != null && !string.IsNullOrEmpty(info.thumbnailPath))
            {
                thumbnailImage.enabled = true;
                // A real implementation would load the texture from info.thumbnailPath
            }
        }

        /// <summary>Toggles the selection highlight on this card.</summary>
        public void SetSelected(bool selected)
        {
            if (selectedHighlight != null)
                selectedHighlight.enabled = selected;
        }
    }
}
