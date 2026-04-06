// CloudSyncEngine.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Background sync manager — delta sync, offline queue, debouncing, and bandwidth awareness.
// Namespace: SWEF.CloudSave

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.CloudSave
{
    /// <summary>
    /// Phase 111 — Background synchronisation manager.
    ///
    /// <para>Responsibilities:</para>
    /// <list type="bullet">
    ///   <item>Auto-sync on save (debounced, minimum 30 s by default).</item>
    ///   <item>Auto-sync on app launch (pull latest from cloud).</item>
    ///   <item>Manual sync trigger.</item>
    ///   <item>Offline queue — saves changes locally when offline, drains when reconnected.</item>
    ///   <item>Delta sync — only uploads files whose content hash changed.</item>
    ///   <item>Bandwidth-aware — extends the debounce interval on metered connections.</item>
    /// </list>
    ///
    /// <para>Attach to a persistent scene object alongside <see cref="CloudSaveManager"/>.</para>
    /// </summary>
    public sealed class CloudSyncEngine : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static CloudSyncEngine Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Fired when a sync cycle begins.</summary>
        public event Action OnSyncStarted;

        /// <summary>Fired when a sync cycle completes successfully.</summary>
        public event Action OnSyncCompleted;

        /// <summary>Fired when a sync cycle fails.</summary>
        public event Action<string> OnSyncFailed;

        /// <summary>Fired when a save conflict is detected for a file key.</summary>
        public event Action<string> OnConflictDetected;

        // ── State ──────────────────────────────────────────────────────────────

        /// <summary><c>true</c> while a sync is running.</summary>
        public bool IsSyncing { get; private set; }

        /// <summary>UTC time of the last successful sync, or <see cref="DateTime.MinValue"/>.</summary>
        public DateTime LastSyncTime { get; private set; } = DateTime.MinValue;

        // ── Internal ───────────────────────────────────────────────────────────

        private readonly List<OfflineQueueEntry> _offlineQueue =
            new List<OfflineQueueEntry>();

        private float    _debounceTimer;
        private bool     _syncPending;
        private bool     _isMetered;

        private string OfflineQueuePath =>
            Path.Combine(Application.persistentDataPath, "cloud_save_offline_queue.json");

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadOfflineQueue();
        }

        private void Start()
        {
            var cfg = CloudSaveManager.Instance?.Config;
            if (cfg != null && cfg.syncOnLaunch)
                StartCoroutine(PullFromCloudCoroutine());
        }

        private void Update()
        {
            var cfg = CloudSaveManager.Instance?.Config;
            if (cfg == null) return;

            // Track metered connection status
            _isMetered = Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork;

            // Debounce timer
            if (_syncPending)
            {
                float interval = cfg.autoSyncDebounceSeconds;
                if (_isMetered && cfg.bandwidthAware)
                    interval *= cfg.meteredConnectionMultiplier;

                _debounceTimer += Time.unscaledDeltaTime;
                if (_debounceTimer >= interval)
                {
                    _syncPending   = false;
                    _debounceTimer = 0f;
                    StartCoroutine(PushToCloudCoroutine());
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Schedules a debounced sync. Call this after every save operation.
        /// </summary>
        public void RequestSync()
        {
            _syncPending   = true;
            _debounceTimer = 0f;
        }

        /// <summary>
        /// Immediately triggers an upload of all dirty files.
        /// </summary>
        public void ForceSyncNow() => StartCoroutine(PushToCloudCoroutine());

        /// <summary>
        /// Immediately pulls the latest cloud data and writes it locally.
        /// </summary>
        public void ForcePullNow() => StartCoroutine(PullFromCloudCoroutine());

        // ── Push (upload dirty files) ──────────────────────────────────────────

        private IEnumerator PushToCloudCoroutine()
        {
            if (IsSyncing) yield break;

            var mgr = CloudSaveManager.Instance;
            if (mgr == null || !mgr.IsReady)
            {
                EnqueueAllDirty();
                yield break;
            }

            IsSyncing = true;
            OnSyncStarted?.Invoke();

            // Drain offline queue first
            yield return StartCoroutine(DrainOfflineQueueCoroutine());

            // Refresh dirty flags
            var registry = SaveDataRegistry.Instance;
            registry.RefreshDirtyFlags();

            bool anyError = false;

            foreach (var record in registry.DirtyRecords)
            {
                if (!File.Exists(record.LocalPath)) continue;

                byte[] raw = null;
                try { raw = File.ReadAllBytes(record.LocalPath); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CloudSyncEngine] Cannot read {record.FileKey}: {ex.Message}");
                    anyError = true;
                    continue;
                }

                // Optional compression
                var cfg = mgr.Config;
                if (cfg.compressCloudData)
                    raw = GZipCompress(raw);

                // Optional encryption (reuse SWEF.Security AES-256 if available)
#if SWEF_SECURITY_AVAILABLE
                if (cfg.encryptCloudData)
                {
                    string encryptedB64 = SWEF.Security.SaveFileEncryptor.Encrypt(
                        System.Text.Encoding.UTF8.GetString(raw));
                    raw = System.Text.Encoding.UTF8.GetBytes(encryptedB64 ?? string.Empty);
                }
#endif

                bool uploadOk = false;
                string uploadErr = null;

                yield return StartCoroutine(
                    mgr.ActiveProvider.UploadAsync(record.FileKey, raw, (ok, err) =>
                    {
                        uploadOk  = ok;
                        uploadErr = err;
                    }));

                if (uploadOk)
                {
                    registry.MarkClean(record.FileKey, DateTime.UtcNow);
                }
                else
                {
                    anyError = true;
                    // Queue for retry when online
                    EnqueueFile(record.FileKey);
                    Debug.LogWarning(
                        $"[CloudSyncEngine] Upload failed for '{record.FileKey}': {uploadErr}");
                }
            }

            SaveOfflineQueue();

            IsSyncing    = false;
            LastSyncTime = DateTime.UtcNow;

            if (anyError)
                OnSyncFailed?.Invoke("One or more files failed to upload.");
            else
                OnSyncCompleted?.Invoke();
        }

        // ── Pull (download cloud → local) ──────────────────────────────────────

        private IEnumerator PullFromCloudCoroutine()
        {
            var mgr = CloudSaveManager.Instance;
            if (mgr == null || !mgr.IsReady) yield break;
            if (IsSyncing) yield break;

            IsSyncing = true;
            OnSyncStarted?.Invoke();

            var registry = SaveDataRegistry.Instance;
            registry.RefreshDirtyFlags();

            // Get list of cloud keys with timestamps
            Dictionary<string, DateTime> cloudKeys = null;
            string listErr = null;

            yield return StartCoroutine(
                mgr.ActiveProvider.ListKeysAsync((keys, err) =>
                {
                    cloudKeys = keys;
                    listErr   = err;
                }));

            if (cloudKeys == null)
            {
                IsSyncing = false;
                OnSyncFailed?.Invoke($"Failed to list cloud keys: {listErr}");
                yield break;
            }

            var cfg      = mgr.Config;
            bool anyError = false;

            foreach (var kv in cloudKeys)
            {
                string key = kv.Key;
                var    rec = registry.GetRecord(key);

                // Conflict detection: local is dirty AND cloud is newer
                if (rec != null && rec.IsDirty && kv.Value > rec.LocalModifiedAt)
                {
                    OnConflictDetected?.Invoke(key);

                    if (cfg.conflictStrategy == ConflictResolutionStrategy.LastWriteWins)
                    {
                        // Cloud wins — download
                        yield return StartCoroutine(DownloadAndWriteFile(mgr, cfg, key, rec, ref anyError));
                    }
                    // MergeByTimestamp and PromptUser are handled by ConflictResolver
                    continue;
                }

                // Only download if cloud version is newer than local
                if (rec == null || kv.Value > rec.CloudModifiedAt)
                {
                    yield return StartCoroutine(DownloadAndWriteFile(mgr, cfg, key, rec, ref anyError));
                }
            }

            IsSyncing    = false;
            LastSyncTime = DateTime.UtcNow;

            if (anyError) OnSyncFailed?.Invoke("One or more files failed to download.");
            else          OnSyncCompleted?.Invoke();
        }

        private IEnumerator DownloadAndWriteFile(
            CloudSaveManager mgr,
            CloudSaveConfig  cfg,
            string           key,
            SaveFileRecord   rec,
            ref bool         anyError)
        {
            byte[] downloaded = null;
            string dlErr      = null;

            yield return StartCoroutine(
                mgr.ActiveProvider.DownloadAsync(key, (data, err) =>
                {
                    downloaded = data;
                    dlErr      = err;
                }));

            if (downloaded == null)
            {
                Debug.LogWarning($"[CloudSyncEngine] Download failed for '{key}': {dlErr}");
                anyError = true;
                yield break;
            }

            // Decrypt
#if SWEF_SECURITY_AVAILABLE
            if (cfg.encryptCloudData)
            {
                string encB64 = System.Text.Encoding.UTF8.GetString(downloaded);
                string plain  = SWEF.Security.SaveFileEncryptor.Decrypt(encB64);
                downloaded    = plain != null
                    ? System.Text.Encoding.UTF8.GetBytes(plain)
                    : downloaded;
            }
#endif
            // Decompress
            if (cfg.compressCloudData)
            {
                byte[] decompressed = GZipDecompress(downloaded);
                if (decompressed != null) downloaded = decompressed;
            }

            if (rec != null)
            {
                try
                {
                    string dir = Path.GetDirectoryName(rec.LocalPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    File.WriteAllBytes(rec.LocalPath, downloaded);
                    SaveDataRegistry.Instance.MarkClean(key, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CloudSyncEngine] Write failed for '{key}': {ex.Message}");
                    anyError = true;
                }
            }
        }

        // ── Offline queue ─────────────────────────────────────────────────────

        private IEnumerator DrainOfflineQueueCoroutine()
        {
            if (_offlineQueue.Count == 0) yield break;

            var mgr = CloudSaveManager.Instance;
            if (mgr == null || !mgr.IsReady) yield break;

            var toRemove = new List<OfflineQueueEntry>();

            foreach (var entry in _offlineQueue)
            {
                var rec = SaveDataRegistry.Instance.GetRecord(entry.FileKey);
                if (rec == null || !File.Exists(rec.LocalPath)) { toRemove.Add(entry); continue; }

                byte[] raw = null;
                try { raw = File.ReadAllBytes(rec.LocalPath); }
                catch { continue; }

                bool ok = false;
                yield return StartCoroutine(
                    mgr.ActiveProvider.UploadAsync(entry.FileKey, raw, (success, _) => ok = success));

                if (ok)
                {
                    toRemove.Add(entry);
                    SaveDataRegistry.Instance.MarkClean(entry.FileKey, DateTime.UtcNow);
                }
                else
                {
                    entry.RetryCount++;
                }
            }

            foreach (var e in toRemove)
                _offlineQueue.Remove(e);

            SaveOfflineQueue();
        }

        private void EnqueueAllDirty()
        {
            SaveDataRegistry.Instance.RefreshDirtyFlags();
            foreach (var rec in SaveDataRegistry.Instance.DirtyRecords)
                EnqueueFile(rec.FileKey);
            SaveOfflineQueue();
        }

        private void EnqueueFile(string key)
        {
            if (_offlineQueue.Exists(e => e.FileKey == key)) return;
            _offlineQueue.Add(new OfflineQueueEntry
            {
                FileKey    = key,
                QueuedAt   = DateTime.UtcNow,
                RetryCount = 0
            });
        }

        // ── Persistence of offline queue ───────────────────────────────────────

        [Serializable]
        private class OfflineQueueWrapper { public List<OfflineQueueEntry> entries; }

        private void SaveOfflineQueue()
        {
            try
            {
                var wrapper = new OfflineQueueWrapper { entries = _offlineQueue };
                File.WriteAllText(OfflineQueuePath, JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSyncEngine] Failed to persist offline queue: {ex.Message}");
            }
        }

        private void LoadOfflineQueue()
        {
            if (!File.Exists(OfflineQueuePath)) return;
            try
            {
                string json    = File.ReadAllText(OfflineQueuePath);
                var    wrapper = JsonUtility.FromJson<OfflineQueueWrapper>(json);
                if (wrapper?.entries != null)
                    _offlineQueue.AddRange(wrapper.entries);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSyncEngine] Failed to load offline queue: {ex.Message}");
            }
        }

        // ── GZip helpers ──────────────────────────────────────────────────────

        private static byte[] GZipCompress(byte[] data)
        {
            try
            {
                using (var ms  = new MemoryStream())
                using (var gz  = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
                {
                    gz.Write(data, 0, data.Length);
                    gz.Close();
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSyncEngine] GZip compress failed: {ex.Message}");
                return data;
            }
        }

        private static byte[] GZipDecompress(byte[] data)
        {
            try
            {
                using (var input  = new MemoryStream(data))
                using (var gz     = new GZipStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    gz.CopyTo(output);
                    return output.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
