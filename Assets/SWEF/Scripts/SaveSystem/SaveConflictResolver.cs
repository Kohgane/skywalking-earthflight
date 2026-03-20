using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SWEF.SaveSystem
{
    /// <summary>
    /// Resolution policy the player or system chose when a save conflict was resolved.
    /// </summary>
    public enum ConflictResolution
    {
        /// <summary>No conflict was present or a resolution has not been made yet.</summary>
        None,
        /// <summary>The local save was kept; the cloud version was discarded.</summary>
        UseLocal,
        /// <summary>The cloud save was applied; the local version was overwritten.</summary>
        UseCloud,
        /// <summary>A merge of both saves was applied (best-effort, newer data wins).</summary>
        Merge
    }

    /// <summary>
    /// Phase 35 — Save conflict resolver.
    /// Detects, stores, and resolves conflicts between local and cloud save data
    /// for a given slot.  Works in tandem with <see cref="CloudSyncManager"/>.
    /// </summary>
    public class SaveConflictResolver : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SaveConflictResolver Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Raised when a conflict is detected for a slot. Argument is slot index.</summary>
        public event Action<int>                        OnConflictDetected;

        /// <summary>Raised after a conflict is resolved. Arguments are slot index and chosen policy.</summary>
        public event Action<int, ConflictResolution>    OnConflictResolved;

        // ── Internal ─────────────────────────────────────────────────────────
        // Pending cloud blobs awaiting resolution, keyed by slot index.
        private readonly Dictionary<int, byte[]> _pendingCloudBlobs =
            new Dictionary<int, byte[]>();

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
        }

        // ── Detection ─────────────────────────────────────────────────────────

        /// <summary>
        /// Compares the in-memory local slot metadata with the header embedded in
        /// <paramref name="cloudBlob"/> to determine whether a conflict exists.
        /// A conflict occurs when both the local and cloud copies have been modified
        /// since the last known sync point.
        /// </summary>
        /// <param name="slotIndex">Slot to check.</param>
        /// <param name="cloudBlob">Raw (possibly compressed/encrypted) cloud save bytes.</param>
        /// <returns><c>true</c> if local and cloud both have unsynchronised changes.</returns>
        public bool DetectConflict(int slotIndex, byte[] cloudBlob)
        {
            var  localInfo  = SaveManager.Instance?.GetSlotInfo(slotIndex);
            if (localInfo == null || localInfo.isEmpty) return false;

            // Already marked as Synced → no conflict
            if (localInfo.cloudSyncStatus == CloudSyncStatus.Synced) return false;

            // Try to peek at the cloud blob timestamp without a full load
            long cloudTicks = PeekTimestamp(cloudBlob);
            if (cloudTicks <= 0) return false;

            long localTicks = localInfo.creationTimestampTicks > 0
                ? GetLastModifiedTicks(localInfo)
                : 0;

            // Both have been modified → conflict
            bool localChanged = localInfo.cloudSyncStatus == CloudSyncStatus.LocalAhead;
            bool cloudNewer   = cloudTicks > localTicks;

            return localChanged && cloudNewer;
        }

        // ── Storage ───────────────────────────────────────────────────────────

        /// <summary>Caches a downloaded cloud blob for <paramref name="slotIndex"/> pending resolution.</summary>
        public void StoreCloudBlob(int slotIndex, byte[] blob)
        {
            _pendingCloudBlobs[slotIndex] = blob;
            Debug.Log($"[SWEF] SaveConflictResolver: cloud blob stored for slot {slotIndex} — awaiting resolution.");
            OnConflictDetected?.Invoke(slotIndex);
        }

        /// <summary>Returns <c>true</c> if there is a pending cloud blob for <paramref name="slotIndex"/>.</summary>
        public bool HasPendingConflict(int slotIndex) => _pendingCloudBlobs.ContainsKey(slotIndex);

        /// <summary>Returns all slot indices with pending conflicts.</summary>
        public IReadOnlyCollection<int> GetConflictingSlots() => _pendingCloudBlobs.Keys;

        // ── Resolution ────────────────────────────────────────────────────────

        /// <summary>
        /// Automatically resolves the conflict for <paramref name="slotIndex"/> by
        /// choosing whichever copy has the more recent <c>lastModifiedTimestamp</c>.
        /// </summary>
        public void ResolveByTimestamp(int slotIndex)
        {
            if (!_pendingCloudBlobs.TryGetValue(slotIndex, out byte[] cloudBlob))
            {
                Debug.LogWarning($"[SWEF] SaveConflictResolver: no pending conflict for slot {slotIndex}.");
                return;
            }

            var  mgr       = SaveManager.Instance;
            var  localInfo = mgr?.GetSlotInfo(slotIndex);
            long localTicks  = localInfo != null ? GetLastModifiedTicks(localInfo) : 0;
            long cloudTicks  = PeekTimestamp(cloudBlob);

            if (cloudTicks > localTicks)
                ResolveUseCloud(slotIndex);
            else
                ResolveUseLocal(slotIndex);
        }

        /// <summary>Keeps the local save and discards the pending cloud blob.</summary>
        public void ResolveUseLocal(int slotIndex)
        {
            if (!_pendingCloudBlobs.ContainsKey(slotIndex))
            {
                Debug.LogWarning($"[SWEF] SaveConflictResolver: no pending conflict for slot {slotIndex}.");
                return;
            }

            _pendingCloudBlobs.Remove(slotIndex);
            CloudSyncManager.Instance?.SetLocalSyncStatus(slotIndex, CloudSyncStatus.LocalAhead);
            Debug.Log($"[SWEF] SaveConflictResolver: slot {slotIndex} — kept local save.");
            OnConflictResolved?.Invoke(slotIndex, ConflictResolution.UseLocal);
        }

        /// <summary>Overwrites the local save with the pending cloud blob.</summary>
        public void ResolveUseCloud(int slotIndex)
        {
            if (!_pendingCloudBlobs.TryGetValue(slotIndex, out byte[] cloudBlob))
            {
                Debug.LogWarning($"[SWEF] SaveConflictResolver: no pending conflict for slot {slotIndex}.");
                return;
            }

            var mgr = SaveManager.Instance;
            if (mgr == null) return;

            try
            {
                File.WriteAllBytes(mgr.GetSavePath(slotIndex), cloudBlob);

                // Update checksum and sync status in metadata
                var info = mgr.GetSlotInfo(slotIndex);
                if (info != null)
                {
                    info.checksum        = SaveIntegrityChecker.ComputeChecksum(cloudBlob);
                    info.cloudSyncStatus = CloudSyncStatus.Synced;
                    info.isEmpty         = false;
                    File.WriteAllText(mgr.GetMetaPath(slotIndex),
                        JsonUtility.ToJson(info, prettyPrint: true));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] SaveConflictResolver: failed to apply cloud blob for slot {slotIndex} — {ex.Message}");
            }

            _pendingCloudBlobs.Remove(slotIndex);
            Debug.Log($"[SWEF] SaveConflictResolver: slot {slotIndex} — applied cloud save.");
            OnConflictResolved?.Invoke(slotIndex, ConflictResolution.UseCloud);
        }

        /// <summary>
        /// Performs a best-effort merge: newer per-key values win.
        /// Falls back to <see cref="ResolveByTimestamp"/> if the blobs cannot be decoded.
        /// </summary>
        public void ResolveMerge(int slotIndex)
        {
            if (!_pendingCloudBlobs.TryGetValue(slotIndex, out byte[] cloudBlob))
            {
                Debug.LogWarning($"[SWEF] SaveConflictResolver: no pending conflict for slot {slotIndex}.");
                return;
            }

            var mgr = SaveManager.Instance;
            if (mgr == null) return;

            try
            {
                // Load both files as raw JSON (decompress/decrypt if needed)
                byte[] localBytes = File.ReadAllBytes(mgr.GetSavePath(slotIndex));
                SaveFile local = DecodeSaveFile(localBytes);
                SaveFile cloud = DecodeSaveFile(cloudBlob);

                if (local == null || cloud == null)
                {
                    Debug.LogWarning("[SWEF] SaveConflictResolver: merge decode failed, falling back to timestamp.");
                    ResolveByTimestamp(slotIndex);
                    return;
                }

                // Merge: iterate cloud payload; keep the cloud entry when its
                // containing save is newer, otherwise keep local.
                bool cloudNewer = cloud.header.lastModifiedTimestamp > local.header.lastModifiedTimestamp;
                var  winner     = cloudNewer ? cloud : local;
                var  loser      = cloudNewer ? local : cloud;

                foreach (var key in loser.payload.keys)
                {
                    if (!winner.payload.Contains(key))
                        winner.payload.Set(key, loser.payload.Get(key));
                }

                // Write merged file back to disk.
                // The merge operates on decoded SaveFile objects, so we write
                // plain (uncompressed, unencrypted) JSON here.  SaveManager's
                // load pipeline has compression and encryption toggled independently
                // via Inspector flags; if both flags are false this file loads
                // normally.  If they are enabled the user should re-save via
                // SaveManager.Save() after the merge to re-apply the pipeline.
                // The cloudSyncStatus is set to LocalAhead so the next sync will
                // re-upload using the full pipeline.
                string mergedJson = JsonUtility.ToJson(winner, prettyPrint: false);
                byte[] mergedData = System.Text.Encoding.UTF8.GetBytes(mergedJson);
                File.WriteAllBytes(mgr.GetSavePath(slotIndex), mergedData);

                var info = mgr.GetSlotInfo(slotIndex);
                if (info != null)
                {
                    info.checksum        = SaveIntegrityChecker.ComputeChecksum(mergedData);
                    info.cloudSyncStatus = CloudSyncStatus.LocalAhead; // needs re-upload
                    File.WriteAllText(mgr.GetMetaPath(slotIndex),
                        JsonUtility.ToJson(info, prettyPrint: true));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] SaveConflictResolver: merge failed for slot {slotIndex} — {ex.Message}; falling back to timestamp.");
                ResolveByTimestamp(slotIndex);
                return;
            }

            _pendingCloudBlobs.Remove(slotIndex);
            Debug.Log($"[SWEF] SaveConflictResolver: slot {slotIndex} — merged.");
            OnConflictResolved?.Invoke(slotIndex, ConflictResolution.Merge);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static long GetLastModifiedTicks(SaveSlotInfo info)
        {
            if (DateTime.TryParse(info.timestamp,
                    null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
                return dt.Ticks;
            return 0;
        }

        private static long PeekTimestamp(byte[] blob)
        {
            if (blob == null || blob.Length == 0) return 0;
            try
            {
                // Attempt to decode without encryption/compression — may fail on
                // encrypted blobs; that is acceptable (returns 0 → treat as no-conflict).
                string json     = Encoding.UTF8.GetString(blob);
                var    saveFile = JsonUtility.FromJson<SaveFile>(json);
                return saveFile?.header?.lastModifiedTimestamp ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private SaveFile DecodeSaveFile(byte[] data)
        {
            try
            {
                // Try plain JSON first (e.g. exported files)
                string json = Encoding.UTF8.GetString(data);
                return JsonUtility.FromJson<SaveFile>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
