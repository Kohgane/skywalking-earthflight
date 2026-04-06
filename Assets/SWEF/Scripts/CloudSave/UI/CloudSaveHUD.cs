// CloudSaveHUD.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Compact heads-up display element that shows sync status in the game HUD.
// Namespace: SWEF.CloudSave

using System;
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
    /// Phase 111 — Compact sync-status indicator for the in-game HUD.
    ///
    /// <para>Shows an icon and short status string (Synced / Syncing… / Offline / Error).
    /// Wire up the serialised Image and Text references in the Inspector.</para>
    /// </summary>
    public sealed class CloudSaveHUD : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Icon")]
        [SerializeField] private Image _statusIcon;

        [Header("Label")]
#if TMP_PRESENT
        [SerializeField] private TMP_Text _statusLabel;
#else
        [SerializeField] private Text _statusLabel;
#endif

        [Header("Icon Sprites")]
        [SerializeField] private Sprite _syncedSprite;
        [SerializeField] private Sprite _syncingSprite;
        [SerializeField] private Sprite _offlineSprite;
        [SerializeField] private Sprite _errorSprite;
        [SerializeField] private Sprite _pendingSprite;

        [Header("Auto-hide")]
        [Tooltip("Hide the HUD element when sync status is Synced.")]
        [SerializeField] private bool _hideWhenSynced = false;

        [Tooltip("Seconds to keep the indicator visible after a sync completes.")]
        [SerializeField] private float _showDurationAfterSync = 3f;

        // ── Internal state ─────────────────────────────────────────────────────

        private float _hideTimer;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            var engine = CloudSyncEngine.Instance;
            if (engine != null)
            {
                engine.OnSyncStarted   += HandleSyncStarted;
                engine.OnSyncCompleted += HandleSyncCompleted;
                engine.OnSyncFailed    += HandleSyncFailed;
            }

            RefreshHUD();
        }

        private void OnDisable()
        {
            var engine = CloudSyncEngine.Instance;
            if (engine != null)
            {
                engine.OnSyncStarted   -= HandleSyncStarted;
                engine.OnSyncCompleted -= HandleSyncCompleted;
                engine.OnSyncFailed    -= HandleSyncFailed;
            }
        }

        private void Update()
        {
            if (_hideTimer > 0f)
            {
                _hideTimer -= Time.unscaledDeltaTime;
                if (_hideTimer <= 0f && _hideWhenSynced)
                    gameObject.SetActive(false);
            }
        }

        // ── Sync event handlers ────────────────────────────────────────────────

        private void HandleSyncStarted()
        {
            gameObject.SetActive(true);
            _hideTimer = 0f;
            ApplyStatus(SyncStatus.Syncing, "Syncing…");
        }

        private void HandleSyncCompleted()
        {
            ApplyStatus(SyncStatus.Synced, "Synced");
            _hideTimer = _showDurationAfterSync;
        }

        private void HandleSyncFailed(string _)
        {
            ApplyStatus(SyncStatus.Error, "Error");
            _hideTimer = 0f;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void RefreshHUD()
        {
            var mgr = CloudSaveManager.Instance;
            if (mgr == null) return;

            var ps = mgr.GetProviderStatus();
            bool isOffline = Application.internetReachability == NetworkReachability.NotReachable;

            if (isOffline)
                ApplyStatus(SyncStatus.Unavailable, "Offline");
            else
                ApplyStatus(ps.SyncStatus, ps.SyncStatus.ToString());
        }

        private void ApplyStatus(SyncStatus status, string label)
        {
            if (_statusLabel != null) _statusLabel.text = label;

            Sprite sprite = status switch
            {
                SyncStatus.Synced           => _syncedSprite,
                SyncStatus.Syncing          => _syncingSprite,
                SyncStatus.Unavailable      => _offlineSprite,
                SyncStatus.Error            => _errorSprite,
                SyncStatus.PendingUpload    => _pendingSprite,
                SyncStatus.PendingDownload  => _pendingSprite,
                _                           => _offlineSprite
            };

            if (_statusIcon != null && sprite != null)
                _statusIcon.sprite = sprite;
        }
    }
}
