using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.SaveSystem
{
    /// <summary>
    /// Phase 35 — Cross-platform cloud synchronisation manager.
    /// Uploads save slots to and downloads them from a configurable REST endpoint.
    /// Works alongside <see cref="SaveConflictResolver"/> to handle divergent saves.
    /// </summary>
    public class CloudSyncManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static CloudSyncManager Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Raised when a sync operation starts. Argument is the slot index (-1 for all).</summary>
        public event Action<int>         OnSyncStarted;

        /// <summary>Raised when a sync operation completes. Arguments are slot index and success flag.</summary>
        public event Action<int, bool>   OnSyncCompleted;

        /// <summary>Raised when a cloud error occurs. Argument is the error message.</summary>
        public event Action<string>      OnSyncError;

        /// <summary>Raised when a conflict is detected during download. Argument is slot index.</summary>
        public event Action<int>         OnConflictDetected;

        // ── Inspector config ─────────────────────────────────────────────────
        [Header("Endpoint")]
        [Tooltip("Base URL of the cloud save API (e.g. https://api.example.com/saves).")]
        [SerializeField] private string cloudBaseUrl = "";

        [Tooltip("Bearer token or API key for authentication.")]
        [SerializeField] private string authToken    = "";

        [Tooltip("Request timeout in seconds.")]
        [SerializeField] private float  timeoutSec   = 20f;

        [Header("Auto-sync")]
        [Tooltip("Automatically upload after every successful local save.")]
        [SerializeField] private bool autoUploadOnSave = false;

        [Tooltip("Automatically check for newer cloud saves on startup.")]
        [SerializeField] private bool autoCheckOnStart = false;

        // ── Public state ─────────────────────────────────────────────────────
        /// <summary><c>true</c> while any cloud operation is in progress.</summary>
        public bool IsSyncing { get; private set; }

        /// <summary>UTC time of the last successful sync, or <see cref="DateTime.MinValue"/>.</summary>
        public DateTime LastSyncTime { get; private set; } = DateTime.MinValue;

        /// <summary><c>true</c> when a non-empty endpoint URL is configured.</summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(cloudBaseUrl);

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

        private void Start()
        {
            if (autoCheckOnStart && IsConfigured)
                StartCoroutine(CheckAllSlotsCoroutine());
        }

        private void OnEnable()
        {
            if (SaveManager.Instance != null && autoUploadOnSave)
                SaveManager.Instance.OnSaveCompleted += HandleSaveCompleted;
        }

        private void OnDisable()
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.OnSaveCompleted -= HandleSaveCompleted;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Uploads slot <paramref name="slotIndex"/> to the cloud.</summary>
        public void UploadSlot(int slotIndex)
        {
            if (!CheckReady()) return;
            StartCoroutine(UploadCoroutine(slotIndex));
        }

        /// <summary>Downloads the cloud copy of slot <paramref name="slotIndex"/> to disk.</summary>
        public void DownloadSlot(int slotIndex)
        {
            if (!CheckReady()) return;
            StartCoroutine(DownloadCoroutine(slotIndex));
        }

        /// <summary>
        /// Compares cloud metadata with the local slot info for <paramref name="slotIndex"/>
        /// and fires <see cref="OnConflictDetected"/> if a conflict is found.
        /// </summary>
        public void CheckSlot(int slotIndex)
        {
            if (!CheckReady()) return;
            StartCoroutine(CheckSlotCoroutine(slotIndex));
        }

        /// <summary>Uploads all populated local slots to the cloud.</summary>
        public void SyncAll()
        {
            if (!CheckReady()) return;
            StartCoroutine(SyncAllCoroutine());
        }

        /// <summary>
        /// Updates the cloud sync status for <paramref name="slotIndex"/> in the local
        /// slot-info sidecar (does not trigger an upload/download).
        /// </summary>
        public void SetLocalSyncStatus(int slotIndex, CloudSyncStatus status)
        {
            var mgr  = SaveManager.Instance;
            var info = mgr?.GetSlotInfo(slotIndex);
            if (info == null) return;

            info.cloudSyncStatus = status;
            // Persist the updated metadata
            try
            {
                File.WriteAllText(mgr.GetMetaPath(slotIndex),
                    JsonUtility.ToJson(info, prettyPrint: true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] CloudSyncManager: could not persist sync status for slot {slotIndex} — {ex.Message}");
            }
        }

        // ── Coroutines ────────────────────────────────────────────────────────
        private IEnumerator UploadCoroutine(int slotIndex)
        {
            while (IsSyncing) yield return null;
            IsSyncing = true;
            OnSyncStarted?.Invoke(slotIndex);

            bool   success = false;
            string errMsg  = null;

            var mgr = SaveManager.Instance;
            if (mgr == null)
            {
                errMsg = "SaveManager not found";
            }
            else
            {
                string filePath = mgr.GetSavePath(slotIndex);
                if (!File.Exists(filePath))
                {
                    errMsg = $"slot {slotIndex} has no local save file";
                }
                else
                {
                    byte[] data = null;
                    try   { data = File.ReadAllBytes(filePath); }
                    catch (Exception ex) { errMsg = ex.Message; }

                    if (data != null)
                    {
                        string url = BuildSlotUrl(slotIndex);
                        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT))
                        {
                            req.uploadHandler   = new UploadHandlerRaw(data);
                            req.downloadHandler = new DownloadHandlerBuffer();
                            req.SetRequestHeader("Content-Type", "application/octet-stream");
                            ApplyAuth(req);
                            req.timeout = Mathf.RoundToInt(timeoutSec);

                            yield return req.SendWebRequest();

                            if (req.result == UnityWebRequest.Result.Success)
                            {
                                success = true;
                                LastSyncTime = DateTime.UtcNow;
                                SetLocalSyncStatus(slotIndex, CloudSyncStatus.Synced);
                                Debug.Log($"[SWEF] CloudSyncManager: slot {slotIndex} uploaded (HTTP {req.responseCode}).");
                            }
                            else
                            {
                                errMsg = $"HTTP {req.responseCode} — {req.error}";
                            }
                        }
                    }
                }
            }

            IsSyncing = false;
            OnSyncCompleted?.Invoke(slotIndex, success);
            if (!success)
            {
                SetLocalSyncStatus(slotIndex, CloudSyncStatus.Error);
                RaiseError($"upload slot {slotIndex}: {errMsg}");
            }
        }

        private IEnumerator DownloadCoroutine(int slotIndex)
        {
            while (IsSyncing) yield return null;
            IsSyncing = true;
            OnSyncStarted?.Invoke(slotIndex);

            bool   success = false;
            string errMsg  = null;

            var mgr = SaveManager.Instance;
            if (mgr == null)
            {
                errMsg = "SaveManager not found";
            }
            else
            {
                string url = BuildSlotUrl(slotIndex);
                using (var req = UnityWebRequest.Get(url))
                {
                    ApplyAuth(req);
                    req.timeout = Mathf.RoundToInt(timeoutSec);
                    yield return req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            byte[] data = req.downloadHandler.data;
                            // Check for conflict before overwriting
                            var conflict = SaveConflictResolver.Instance;
                            bool isConflict = conflict != null &&
                                             conflict.DetectConflict(slotIndex, data);

                            if (isConflict)
                            {
                                conflict.StoreCloudBlob(slotIndex, data);
                                OnConflictDetected?.Invoke(slotIndex);
                                SetLocalSyncStatus(slotIndex, CloudSyncStatus.Conflict);
                                success = true; // downloaded; awaiting resolution
                            }
                            else
                            {
                                File.WriteAllBytes(mgr.GetSavePath(slotIndex), data);
                                // Recompute and update checksum in metadata
                                var  info     = mgr.GetSlotInfo(slotIndex);
                                if (info != null)
                                {
                                    info.checksum         = SaveIntegrityChecker.ComputeChecksum(data);
                                    info.cloudSyncStatus  = CloudSyncStatus.Synced;
                                    info.isEmpty          = false;
                                    File.WriteAllText(mgr.GetMetaPath(slotIndex),
                                        JsonUtility.ToJson(info, prettyPrint: true));
                                }
                                success = true;
                            }

                            LastSyncTime = DateTime.UtcNow;
                            Debug.Log($"[SWEF] CloudSyncManager: slot {slotIndex} downloaded.");
                        }
                        catch (Exception ex)
                        {
                            errMsg = ex.Message;
                        }
                    }
                    else
                    {
                        errMsg = $"HTTP {req.responseCode} — {req.error}";
                    }
                }
            }

            IsSyncing = false;
            OnSyncCompleted?.Invoke(slotIndex, success);
            if (!success)
            {
                SetLocalSyncStatus(slotIndex, CloudSyncStatus.Error);
                RaiseError($"download slot {slotIndex}: {errMsg}");
            }
        }

        private IEnumerator CheckSlotCoroutine(int slotIndex)
        {
            string url = BuildMetaUrl(slotIndex);
            using (var req = UnityWebRequest.Get(url))
            {
                ApplyAuth(req);
                req.timeout = Mathf.RoundToInt(timeoutSec);
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[SWEF] CloudSyncManager: meta check for slot {slotIndex} failed — {req.error}");
                    yield break;
                }

                // Parse cloud timestamp from JSON response {"timestamp":"...","checksum":"..."}
                var cloudMeta = JsonUtility.FromJson<CloudSlotMeta>(req.downloadHandler.text);
                if (cloudMeta == null) yield break;

                var localInfo = SaveManager.Instance?.GetSlotInfo(slotIndex);
                if (localInfo == null || localInfo.isEmpty)
                {
                    SetLocalSyncStatus(slotIndex, CloudSyncStatus.CloudAhead);
                    OnConflictDetected?.Invoke(slotIndex);
                    yield break;
                }

                bool cloudNewer = string.Compare(cloudMeta.timestamp, localInfo.timestamp,
                    StringComparison.Ordinal) > 0;
                bool localNewer = string.Compare(localInfo.timestamp, cloudMeta.timestamp,
                    StringComparison.Ordinal) > 0;

                if (cloudNewer)
                    SetLocalSyncStatus(slotIndex, CloudSyncStatus.CloudAhead);
                else if (localNewer)
                    SetLocalSyncStatus(slotIndex, CloudSyncStatus.LocalAhead);
                else
                    SetLocalSyncStatus(slotIndex, CloudSyncStatus.Synced);

                Debug.Log($"[SWEF] CloudSyncManager: slot {slotIndex} status = {SaveManager.Instance.GetSlotInfo(slotIndex)?.cloudSyncStatus}");
            }
        }

        private IEnumerator CheckAllSlotsCoroutine()
        {
            for (int i = 0; i < SaveSystemConstants.TotalSlots; i++)
                yield return CheckSlotCoroutine(i);
        }

        private IEnumerator SyncAllCoroutine()
        {
            IsSyncing = true;
            OnSyncStarted?.Invoke(-1);

            var mgr   = SaveManager.Instance;
            var infos = mgr?.GetAllSlotInfos();
            if (infos != null)
            {
                for (int i = 0; i < infos.Length; i++)
                {
                    if (infos[i] != null && !infos[i].isEmpty &&
                        infos[i].cloudSyncStatus == CloudSyncStatus.LocalAhead)
                    {
                        IsSyncing = false;
                        yield return UploadCoroutine(i);
                        IsSyncing = true;
                    }
                }
            }

            IsSyncing = false;
            LastSyncTime = DateTime.UtcNow;
            OnSyncCompleted?.Invoke(-1, true);
            Debug.Log("[SWEF] CloudSyncManager: SyncAll complete.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private bool CheckReady()
        {
            if (!IsConfigured)
            {
                Debug.LogWarning("[SWEF] CloudSyncManager: no endpoint configured.");
                return false;
            }
            if (IsSyncing)
            {
                Debug.LogWarning("[SWEF] CloudSyncManager: sync already in progress.");
                return false;
            }
            return true;
        }

        private string BuildSlotUrl(int slotIndex) =>
            $"{cloudBaseUrl.TrimEnd('/')}/slot_{slotIndex}{SaveSystemConstants.SaveExtension}";

        private string BuildMetaUrl(int slotIndex) =>
            $"{cloudBaseUrl.TrimEnd('/')}/slot_{slotIndex}{SaveSystemConstants.MetaExtension}";

        private void ApplyAuth(UnityWebRequest req)
        {
            if (!string.IsNullOrWhiteSpace(authToken))
                req.SetRequestHeader("Authorization", $"Bearer {authToken}");
        }

        private void HandleSaveCompleted(int slotIndex, bool success)
        {
            if (success && IsConfigured)
                StartCoroutine(UploadCoroutine(slotIndex));
        }

        private void RaiseError(string msg)
        {
            Debug.LogError($"[SWEF] CloudSyncManager: {msg}");
            OnSyncError?.Invoke(msg);
        }

        // ── Nested types ──────────────────────────────────────────────────────
        [Serializable]
        private class CloudSlotMeta
        {
            public string timestamp;
            public string checksum;
        }
    }
}
