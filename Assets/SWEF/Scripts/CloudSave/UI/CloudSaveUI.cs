// CloudSaveUI.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Settings panel: provider selection, sync status, manual trigger, storage usage,
// linked accounts, conflict prompt, export/import buttons.
// Namespace: SWEF.CloudSave

using System;
using System.IO;
using UnityEngine;

#if UNITY_UGUI_AVAILABLE || UNITY_2019_3_OR_NEWER
using UnityEngine.UI;
#endif

#if TMP_PRESENT
using TMPro;
#endif

namespace SWEF.CloudSave
{
    /// <summary>
    /// Phase 111 — Full Cloud Save settings panel.
    ///
    /// <para>Wire up the serialised fields in the Unity Inspector.
    /// All UI elements are optional — the script is safe to attach even if only
    /// a subset of UI elements are present.</para>
    /// </summary>
    public sealed class CloudSaveUI : MonoBehaviour
    {
        // ── Inspector references ───────────────────────────────────────────────

        [Header("Status")]
#if TMP_PRESENT
        [SerializeField] private TMP_Text _syncStatusLabel;
        [SerializeField] private TMP_Text _lastSyncLabel;
        [SerializeField] private TMP_Text _storageUsageLabel;
        [SerializeField] private TMP_Text _providerLabel;
#else
        [SerializeField] private Text _syncStatusLabel;
        [SerializeField] private Text _lastSyncLabel;
        [SerializeField] private Text _storageUsageLabel;
        [SerializeField] private Text _providerLabel;
#endif

        [Header("Storage")]
        [SerializeField] private Slider _storageBar;

        [Header("Controls")]
        [SerializeField] private Button _syncNowButton;
        [SerializeField] private Button _pullNowButton;
        [SerializeField] private Button _exportBackupButton;
        [SerializeField] private Button _importBackupButton;

        [Header("Provider Selection")]
        [SerializeField] private Dropdown _providerDropdown;

        [Header("Conflict Dialog")]
        [SerializeField] private GameObject _conflictDialogRoot;
#if TMP_PRESENT
        [SerializeField] private TMP_Text  _conflictFileLabel;
#else
        [SerializeField] private Text      _conflictFileLabel;
#endif
        [SerializeField] private Button    _conflictKeepLocalButton;
        [SerializeField] private Button    _conflictUseCloudButton;

        [Header("Export Path")]
        [Tooltip("File path for the local backup export. Defaults to persistentDataPath/swef_backup.json.")]
        [SerializeField] private string _exportPath;

        // ── Internal state ─────────────────────────────────────────────────────

        private string _pendingConflictKey;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (string.IsNullOrEmpty(_exportPath))
                _exportPath = Path.Combine(Application.persistentDataPath, "swef_backup.json");
        }

        private void OnEnable()
        {
            SubscribeEvents();
            RefreshUI();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        // ── Event wiring ───────────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            var engine = CloudSyncEngine.Instance;
            if (engine != null)
            {
                engine.OnSyncStarted    += HandleSyncStarted;
                engine.OnSyncCompleted  += HandleSyncCompleted;
                engine.OnSyncFailed     += HandleSyncFailed;
                engine.OnConflictDetected += HandleConflictDetected;
            }

            ConflictResolver.Instance.OnConflictPromptRequired += HandleConflictPrompt;

            // Buttons
            if (_syncNowButton)       _syncNowButton.onClick.AddListener(OnSyncNowClicked);
            if (_pullNowButton)       _pullNowButton.onClick.AddListener(OnPullNowClicked);
            if (_exportBackupButton)  _exportBackupButton.onClick.AddListener(OnExportClicked);
            if (_importBackupButton)  _importBackupButton.onClick.AddListener(OnImportClicked);
            if (_conflictKeepLocalButton) _conflictKeepLocalButton.onClick.AddListener(OnKeepLocalClicked);
            if (_conflictUseCloudButton)  _conflictUseCloudButton.onClick.AddListener(OnUseCloudClicked);

            if (_providerDropdown)    _providerDropdown.onValueChanged.AddListener(OnProviderChanged);
        }

        private void UnsubscribeEvents()
        {
            var engine = CloudSyncEngine.Instance;
            if (engine != null)
            {
                engine.OnSyncStarted      -= HandleSyncStarted;
                engine.OnSyncCompleted    -= HandleSyncCompleted;
                engine.OnSyncFailed       -= HandleSyncFailed;
                engine.OnConflictDetected -= HandleConflictDetected;
            }

            ConflictResolver.Instance.OnConflictPromptRequired -= HandleConflictPrompt;

            if (_syncNowButton)       _syncNowButton.onClick.RemoveListener(OnSyncNowClicked);
            if (_pullNowButton)       _pullNowButton.onClick.RemoveListener(OnPullNowClicked);
            if (_exportBackupButton)  _exportBackupButton.onClick.RemoveListener(OnExportClicked);
            if (_importBackupButton)  _importBackupButton.onClick.RemoveListener(OnImportClicked);
            if (_conflictKeepLocalButton) _conflictKeepLocalButton.onClick.RemoveListener(OnKeepLocalClicked);
            if (_conflictUseCloudButton)  _conflictUseCloudButton.onClick.RemoveListener(OnUseCloudClicked);

            if (_providerDropdown)    _providerDropdown.onValueChanged.RemoveListener(OnProviderChanged);
        }

        // ── Refresh ────────────────────────────────────────────────────────────

        private void RefreshUI()
        {
            var mgr    = CloudSaveManager.Instance;
            var engine = CloudSyncEngine.Instance;

            if (mgr == null) return;

            var status = mgr.GetProviderStatus();

            SetText(_syncStatusLabel,   status.SyncStatus.ToString());
            SetText(_providerLabel,     mgr.ActiveProvider?.ProviderName ?? "—");

            string lastSync = engine != null && engine.LastSyncTime > DateTime.MinValue
                ? engine.LastSyncTime.ToLocalTime().ToString("g")
                : "Never";
            SetText(_lastSyncLabel, $"Last sync: {lastSync}");

            float fraction = status.QuotaFraction;
            if (_storageBar) _storageBar.value = fraction;

            string usageText = status.QuotaTotalBytes > 0
                ? $"{BytesToHuman(status.QuotaUsedBytes)} / {BytesToHuman(status.QuotaTotalBytes)}"
                : "—";
            SetText(_storageUsageLabel, usageText);
        }

        // ── Sync event handlers ────────────────────────────────────────────────

        private void HandleSyncStarted()
        {
            SetText(_syncStatusLabel, "Syncing…");
            if (_syncNowButton) _syncNowButton.interactable = false;
        }

        private void HandleSyncCompleted()
        {
            SetText(_syncStatusLabel, "Synced");
            if (_syncNowButton) _syncNowButton.interactable = true;
            RefreshUI();
        }

        private void HandleSyncFailed(string err)
        {
            SetText(_syncStatusLabel, "Error");
            if (_syncNowButton) _syncNowButton.interactable = true;
            Debug.LogWarning($"[CloudSaveUI] Sync failed: {err}");
        }

        private void HandleConflictDetected(string key)
        {
            Debug.Log($"[CloudSaveUI] Conflict detected: {key}");
        }

        private void HandleConflictPrompt(SaveConflict conflict)
        {
            _pendingConflictKey = conflict.FileKey;
            SetText(_conflictFileLabel, $"Conflict: {conflict.FileKey}");
            if (_conflictDialogRoot) _conflictDialogRoot.SetActive(true);
        }

        // ── Button handlers ────────────────────────────────────────────────────

        private void OnSyncNowClicked()
        {
            CloudSyncEngine.Instance?.ForceSyncNow();
        }

        private void OnPullNowClicked()
        {
            CloudSyncEngine.Instance?.ForcePullNow();
        }

        private void OnExportClicked()
        {
            bool ok = SaveDataMigrator.Instance.ExportLocalBackup(_exportPath);
            Debug.Log(ok
                ? $"[CloudSaveUI] Backup exported to {_exportPath}"
                : "[CloudSaveUI] Backup export failed.");
        }

        private void OnImportClicked()
        {
            Debug.Log("[CloudSaveUI] Import not yet implemented — hook up a file picker here.");
        }

        private void OnProviderChanged(int index)
        {
            if (index >= 0 && index <= 3)
                CloudSaveManager.Instance?.SelectProvider((CloudProviderType)index);
        }

        private void OnKeepLocalClicked()
        {
            if (!string.IsNullOrEmpty(_pendingConflictKey))
                ConflictResolver.Instance.ResolveUserChoice(_pendingConflictKey, ConflictChoice.KeepLocal);
            HideConflictDialog();
        }

        private void OnUseCloudClicked()
        {
            if (!string.IsNullOrEmpty(_pendingConflictKey))
                ConflictResolver.Instance.ResolveUserChoice(_pendingConflictKey, ConflictChoice.UseCloud);
            HideConflictDialog();
        }

        private void HideConflictDialog()
        {
            _pendingConflictKey = null;
            if (_conflictDialogRoot) _conflictDialogRoot.SetActive(false);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void SetText(
#if TMP_PRESENT
            TMP_Text label,
#else
            Text label,
#endif
            string value)
        {
            if (label != null) label.text = value;
        }

        private static string BytesToHuman(long bytes)
        {
            if (bytes < 1024)         return $"{bytes} B";
            if (bytes < 1024 * 1024)  return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F1} MB";
        }
    }
}
