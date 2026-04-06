// CloudSaveBridge.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Connects the CloudSave system to existing SWEF systems via compile-time feature guards.
// Namespace: SWEF.CloudSave

using UnityEngine;

namespace SWEF.CloudSave
{
    /// <summary>
    /// Phase 111 — Bridges the Cloud Save system with other SWEF modules.
    ///
    /// <para>All integration calls are wrapped in <c>#if SWEF_*_AVAILABLE</c>
    /// compile guards so this module compiles cleanly without any external
    /// dependency.</para>
    /// </summary>
    public sealed class CloudSaveBridge : MonoBehaviour
    {
        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnEnable()  => SubscribeEvents();
        private void OnDisable() => UnsubscribeEvents();

        // ── Event subscriptions ────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            var engine = CloudSyncEngine.Instance;
            if (engine != null)
            {
                engine.OnSyncCompleted += HandleSyncCompleted;
                engine.OnSyncFailed    += HandleSyncFailed;
            }

#if SWEF_SAVE_AVAILABLE
            var saveMgr = SWEF.SaveSystem.SaveManager.Instance;
            if (saveMgr != null)
                saveMgr.OnSaveCompleted += HandleLocalSaveCompleted;
#endif

#if SWEF_ANALYTICS_AVAILABLE
            // No subscription needed — push data on events below.
#endif
        }

        private void UnsubscribeEvents()
        {
            var engine = CloudSyncEngine.Instance;
            if (engine != null)
            {
                engine.OnSyncCompleted -= HandleSyncCompleted;
                engine.OnSyncFailed    -= HandleSyncFailed;
            }

#if SWEF_SAVE_AVAILABLE
            var saveMgr = SWEF.SaveSystem.SaveManager.Instance;
            if (saveMgr != null)
                saveMgr.OnSaveCompleted -= HandleLocalSaveCompleted;
#endif
        }

        // ── Handlers ──────────────────────────────────────────────────────────

#if SWEF_SAVE_AVAILABLE
        private void HandleLocalSaveCompleted(int slotIndex)
        {
            // Trigger a debounced cloud sync whenever a local save finishes.
            var cfg = CloudSaveManager.Instance?.Config;
            if (cfg != null && cfg.syncOnSave)
                CloudSyncEngine.Instance?.RequestSync();
        }
#endif

        private void HandleSyncCompleted()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("cloud_sync_success");
#endif
            Debug.Log("[CloudSaveBridge] Sync completed.");
        }

        private void HandleSyncFailed(string error)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.LogEvent("cloud_sync_failed",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "error", error }
                });
#endif
            Debug.LogWarning($"[CloudSaveBridge] Sync failed: {error}");
        }
    }
}
