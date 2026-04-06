// CloudSaveData.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Enumerations, configuration ScriptableObject, and core data classes.
// Namespace: SWEF.CloudSave

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CloudSave
{
    // ════════════════════════════════════════════════════════════════════════════
    // Provider type
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Identifies which cloud save backend is active.</summary>
    public enum CloudProviderType
    {
        /// <summary>Local JSON files — always-available fallback.</summary>
        LocalFile      = 0,
        /// <summary>Unity Gaming Services Cloud Save.</summary>
        UnityCloud     = 1,
        /// <summary>Firebase Realtime Database / Firestore.</summary>
        Firebase       = 2,
        /// <summary>Generic REST API endpoint provided by the developer.</summary>
        CustomREST     = 3
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Connection / sync status
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Current connection state of the active cloud provider.</summary>
    public enum ProviderConnectionStatus
    {
        /// <summary>Not yet attempted.</summary>
        Disconnected = 0,
        /// <summary>Handshake / authentication in progress.</summary>
        Connecting   = 1,
        /// <summary>Successfully authenticated and reachable.</summary>
        Connected    = 2,
        /// <summary>Authentication or network error.</summary>
        Error        = 3,
        /// <summary>Device has no network access.</summary>
        Offline      = 4
    }

    /// <summary>Current synchronisation state of the save data.</summary>
    public enum SyncStatus
    {
        /// <summary>Local data matches the cloud copy.</summary>
        Synced      = 0,
        /// <summary>A sync operation is in progress.</summary>
        Syncing     = 1,
        /// <summary>Local changes are pending upload.</summary>
        PendingUpload = 2,
        /// <summary>Cloud has newer data waiting to be downloaded.</summary>
        PendingDownload = 3,
        /// <summary>A conflict was detected that requires resolution.</summary>
        Conflict    = 4,
        /// <summary>The last sync attempt failed.</summary>
        Error       = 5,
        /// <summary>Sync is disabled or provider is unavailable.</summary>
        Unavailable = 6
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Conflict resolution strategy
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Determines how save conflicts between local and cloud are resolved.</summary>
    public enum ConflictResolutionStrategy
    {
        /// <summary>The save with the most-recent write timestamp wins.</summary>
        LastWriteWins     = 0,
        /// <summary>Each file's per-field timestamps are compared; newest value kept.</summary>
        MergeByTimestamp  = 1,
        /// <summary>Display a dialog and let the player choose which copy to keep.</summary>
        PromptUser        = 2
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Platform account type for cross-platform linking
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Third-party platform account that can be linked to a SWEF profile.</summary>
    public enum PlatformAccountType
    {
        Steam         = 0,
        AppleGameCenter = 1,
        GooglePlayGames = 2,
        Xbox          = 3,
        PlayStation   = 4,
        Custom        = 99
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Configuration ScriptableObject
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 111 — Runtime-configurable settings for the Cloud Save system.
    /// Create one instance via <em>Assets → Create → SWEF → CloudSave → Config</em>
    /// and assign it to <see cref="CloudSaveManager"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/CloudSave/Config", fileName = "CloudSaveConfig")]
    public sealed class CloudSaveConfig : ScriptableObject
    {
        [Header("Provider")]
        [Tooltip("Which cloud backend to use.")]
        public CloudProviderType providerType = CloudProviderType.LocalFile;

        [Header("Sync Engine")]
        [Tooltip("Minimum seconds between consecutive auto-syncs.")]
        [Range(10f, 300f)]
        public float autoSyncDebounceSeconds = 30f;

        [Tooltip("Automatically sync when the app launches.")]
        public bool syncOnLaunch = true;

        [Tooltip("Automatically sync after every save operation.")]
        public bool syncOnSave = true;

        [Tooltip("Reduce sync frequency when on a metered (mobile data) connection.")]
        public bool bandwidthAware = true;

        [Tooltip("Sync interval multiplier applied on metered connections.")]
        [Range(1f, 10f)]
        public float meteredConnectionMultiplier = 3f;

        [Header("Conflict Resolution")]
        [Tooltip("How save conflicts are resolved.")]
        public ConflictResolutionStrategy conflictStrategy = ConflictResolutionStrategy.LastWriteWins;

        [Header("Security")]
        [Tooltip("Encrypt save data before uploading to cloud storage.")]
        public bool encryptCloudData = true;

        [Tooltip("Compress save data (GZip) before upload to save bandwidth.")]
        public bool compressCloudData = true;

        [Header("REST Provider")]
        [Tooltip("Base URL for the CustomREST provider (e.g. https://api.example.com/saves/).")]
        public string restBaseUrl = string.Empty;

        [Tooltip("API key / bearer token for the REST endpoint (stored as plain text here; use Secrets in production).")]
        public string restApiKey = string.Empty;

        [Header("Device Limits")]
        [Tooltip("Maximum number of devices that may be registered to a single profile.")]
        [Range(1, 10)]
        public int maxDevices = 5;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Provider metadata
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Runtime status snapshot for the active cloud save provider.</summary>
    [Serializable]
    public class ProviderStatus
    {
        /// <summary>Which provider is described by this record.</summary>
        public CloudProviderType ProviderType;

        /// <summary>Current connection health.</summary>
        public ProviderConnectionStatus ConnectionStatus;

        /// <summary>Current synchronisation state.</summary>
        public SyncStatus SyncStatus;

        /// <summary>UTC timestamp of the last successful sync, or <see cref="DateTime.MinValue"/>.</summary>
        public DateTime LastSyncTime;

        /// <summary>Bytes used on the cloud backend (0 if unknown).</summary>
        public long QuotaUsedBytes;

        /// <summary>Total quota available in bytes (0 if unknown / unlimited).</summary>
        public long QuotaTotalBytes;

        /// <summary>Human-readable error message from the last failed operation, or null.</summary>
        public string LastError;

        /// <summary>Convenience: fraction in [0, 1] of used quota, or 0 if quota is unknown.</summary>
        public float QuotaFraction =>
            QuotaTotalBytes > 0 ? Mathf.Clamp01((float)QuotaUsedBytes / QuotaTotalBytes) : 0f;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Per-file save record used by the delta-sync engine
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Metadata for a single logical save file tracked by the registry.</summary>
    [Serializable]
    public class SaveFileRecord
    {
        /// <summary>Stable identifier for this file (e.g. "player_profile").</summary>
        public string FileKey;

        /// <summary>Absolute local path to the JSON file.</summary>
        public string LocalPath;

        /// <summary>UTC timestamp of the last local write.</summary>
        public DateTime LocalModifiedAt;

        /// <summary>UTC timestamp of the last cloud write for this file.</summary>
        public DateTime CloudModifiedAt;

        /// <summary>SHA-256 hash of the local file contents at the last snapshot.</summary>
        public string LocalContentHash;

        /// <summary>Whether this file has local changes not yet uploaded.</summary>
        public bool IsDirty;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Offline queue entry
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>A single pending upload that was deferred because the device was offline.</summary>
    [Serializable]
    public class OfflineQueueEntry
    {
        /// <summary>Save file key that needs to be synced.</summary>
        public string FileKey;

        /// <summary>UTC time when the change was queued.</summary>
        public DateTime QueuedAt;

        /// <summary>Number of failed upload attempts for this entry.</summary>
        public int RetryCount;
    }
}
