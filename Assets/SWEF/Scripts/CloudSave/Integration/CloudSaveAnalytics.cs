// CloudSaveAnalytics.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Telemetry helpers for cloud save events.
// Namespace: SWEF.CloudSave

using System.Collections.Generic;
using UnityEngine;

#if SWEF_ANALYTICS_AVAILABLE
using SWEF.Analytics;
#endif

namespace SWEF.CloudSave
{
    /// <summary>
    /// Phase 111 — Static telemetry utility for the Cloud Save system.
    ///
    /// <para>All methods are no-ops when <c>SWEF_ANALYTICS_AVAILABLE</c> is not defined,
    /// so calls are always safe to make unconditionally.</para>
    /// </summary>
    public static class CloudSaveAnalytics
    {
        // ── Event names ────────────────────────────────────────────────────────

        private const string EvtProviderSelected   = "cloud_save_provider_selected";
        private const string EvtSyncStarted        = "cloud_save_sync_started";
        private const string EvtSyncCompleted      = "cloud_save_sync_completed";
        private const string EvtSyncFailed         = "cloud_save_sync_failed";
        private const string EvtConflictDetected   = "cloud_save_conflict_detected";
        private const string EvtConflictResolved   = "cloud_save_conflict_resolved";
        private const string EvtAccountLinked      = "cloud_save_account_linked";
        private const string EvtAccountUnlinked    = "cloud_save_account_unlinked";
        private const string EvtMigrationCompleted = "cloud_save_migration_completed";
        private const string EvtBackupExported     = "cloud_save_backup_exported";

        // ── Logging methods ────────────────────────────────────────────────────

        /// <summary>Logs that the user changed the cloud save provider.</summary>
        public static void LogProviderSelected(CloudProviderType provider)
        {
            Log(EvtProviderSelected, new Dictionary<string, object>
            {
                { "provider", provider.ToString() }
            });
        }

        /// <summary>Logs that a sync cycle started.</summary>
        public static void LogSyncStarted(int dirtyFileCount)
        {
            Log(EvtSyncStarted, new Dictionary<string, object>
            {
                { "dirty_files", dirtyFileCount }
            });
        }

        /// <summary>Logs that a sync cycle completed successfully.</summary>
        public static void LogSyncCompleted(int uploadedFiles, double durationSeconds)
        {
            Log(EvtSyncCompleted, new Dictionary<string, object>
            {
                { "uploaded_files",   uploadedFiles },
                { "duration_seconds", durationSeconds }
            });
        }

        /// <summary>Logs that a sync cycle failed.</summary>
        public static void LogSyncFailed(string reason)
        {
            Log(EvtSyncFailed, new Dictionary<string, object>
            {
                { "reason", reason ?? string.Empty }
            });
        }

        /// <summary>Logs that a save conflict was detected.</summary>
        public static void LogConflictDetected(string fileKey, ConflictResolutionStrategy strategy)
        {
            Log(EvtConflictDetected, new Dictionary<string, object>
            {
                { "file_key", fileKey   ?? string.Empty },
                { "strategy", strategy.ToString() }
            });
        }

        /// <summary>Logs how a conflict was resolved.</summary>
        public static void LogConflictResolved(string fileKey, ConflictChoice choice)
        {
            Log(EvtConflictResolved, new Dictionary<string, object>
            {
                { "file_key", fileKey ?? string.Empty },
                { "choice",   choice.ToString() }
            });
        }

        /// <summary>Logs that a platform account was linked.</summary>
        public static void LogAccountLinked(PlatformAccountType platform)
        {
            Log(EvtAccountLinked, new Dictionary<string, object>
            {
                { "platform", platform.ToString() }
            });
        }

        /// <summary>Logs that a platform account was unlinked.</summary>
        public static void LogAccountUnlinked(PlatformAccountType platform)
        {
            Log(EvtAccountUnlinked, new Dictionary<string, object>
            {
                { "platform", platform.ToString() }
            });
        }

        /// <summary>Logs that a data migration completed.</summary>
        public static void LogMigrationCompleted(int fromVersion, int toVersion, bool success)
        {
            Log(EvtMigrationCompleted, new Dictionary<string, object>
            {
                { "from_version", fromVersion },
                { "to_version",   toVersion   },
                { "success",      success      }
            });
        }

        /// <summary>Logs that a local backup was exported.</summary>
        public static void LogBackupExported(bool success)
        {
            Log(EvtBackupExported, new Dictionary<string, object>
            {
                { "success", success }
            });
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private static void Log(string eventName, Dictionary<string, object> parameters)
        {
#if SWEF_ANALYTICS_AVAILABLE
            AnalyticsManager.LogEvent(eventName, parameters);
#else
            // No-op when analytics are not available.
            _ = eventName;
            _ = parameters;
#endif
        }
    }
}
