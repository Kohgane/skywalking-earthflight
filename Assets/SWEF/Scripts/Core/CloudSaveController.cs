using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.Core
{
    /// <summary>
    /// Optional REST-API cloud backup for the SWEF save file.
    /// Uploads (PUT) and downloads (GET) the save JSON to a configurable endpoint.
    /// Requires <see cref="SaveManager"/> to be present in the scene.
    /// </summary>
    public class CloudSaveController : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────────────────
        [Header("Endpoint")]
        [SerializeField] private string cloudEndpointUrl = "";
        [SerializeField] private string authToken        = "";
        [SerializeField] private float  timeoutSec       = 15f;

        [Header("Auto-sync")]
        [Tooltip("When true, automatically uploads after every local save.")]
        [SerializeField] private bool autoSyncOnSave = false;

        // ── Public state ─────────────────────────────────────────────────────
        /// <summary>True while an upload is in progress.</summary>
        public bool IsUploading   { get; private set; }

        /// <summary>True while a download is in progress.</summary>
        public bool IsDownloading { get; private set; }

        /// <summary>UTC time of the last successful sync (upload or download).</summary>
        public DateTime LastSyncTime { get; private set; } = DateTime.MinValue;

        /// <summary>True when a non-empty endpoint URL is configured.</summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(cloudEndpointUrl);

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Raised when an upload completes successfully.</summary>
        public event Action OnUploadComplete;

        /// <summary>Raised when a download completes successfully.</summary>
        public event Action OnDownloadComplete;

        /// <summary>Raised when a cloud operation fails; argument is the error message.</summary>
        public event Action<string> OnCloudError;

        // ── Internal ─────────────────────────────────────────────────────────
        private SaveManager _save;

        private void Awake()
        {
            _save = FindFirstObjectByType<SaveManager>();
        }

        private void OnEnable()
        {
            if (_save != null && autoSyncOnSave)
                _save.OnSaveCompleted += HandleSaveCompleted;
        }

        private void OnDisable()
        {
            if (_save != null)
                _save.OnSaveCompleted -= HandleSaveCompleted;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Uploads the current local save JSON to the cloud endpoint (HTTP PUT).
        /// No-op when <see cref="IsConfigured"/> is false or a transfer is already running.
        /// </summary>
        public void Upload()
        {
            if (!IsConfigured)
            {
                Debug.LogWarning("[SWEF] CloudSaveController: endpoint not configured.");
                return;
            }
            if (IsUploading || IsDownloading)
            {
                Debug.LogWarning("[SWEF] CloudSaveController: transfer already in progress.");
                return;
            }
            StartCoroutine(UploadCoroutine());
        }

        /// <summary>
        /// Downloads the cloud save JSON and merges it into the local save (HTTP GET).
        /// No-op when <see cref="IsConfigured"/> is false or a transfer is already running.
        /// </summary>
        public void Download()
        {
            if (!IsConfigured)
            {
                Debug.LogWarning("[SWEF] CloudSaveController: endpoint not configured.");
                return;
            }
            if (IsUploading || IsDownloading)
            {
                Debug.LogWarning("[SWEF] CloudSaveController: transfer already in progress.");
                return;
            }
            StartCoroutine(DownloadCoroutine());
        }

        // ── Coroutines ───────────────────────────────────────────────────────

        private IEnumerator UploadCoroutine()
        {
            if (_save == null)
            {
                RaiseError("SaveManager not found.");
                yield break;
            }

            IsUploading = true;
            string json = JsonUtility.ToJson(_save.Data, prettyPrint: false);
            byte[] body = Encoding.UTF8.GetBytes(json);

            using (var req = new UnityWebRequest(cloudEndpointUrl, UnityWebRequest.kHttpVerbPUT))
            {
                req.uploadHandler   = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrWhiteSpace(authToken))
                    req.SetRequestHeader("Authorization", $"Bearer {authToken}");
                req.timeout = Mathf.RoundToInt(timeoutSec);

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    LastSyncTime = DateTime.UtcNow;
                    Debug.Log($"[SWEF] CloudSaveController: upload succeeded ({req.responseCode}).");
                    OnUploadComplete?.Invoke();
                }
                else
                {
                    RaiseError($"Upload failed: {req.error} (HTTP {req.responseCode})");
                }
            }

            IsUploading = false;
        }

        private IEnumerator DownloadCoroutine()
        {
            if (_save == null)
            {
                RaiseError("SaveManager not found.");
                yield break;
            }

            IsDownloading = true;

            using (var req = UnityWebRequest.Get(cloudEndpointUrl))
            {
                if (!string.IsNullOrWhiteSpace(authToken))
                    req.SetRequestHeader("Authorization", $"Bearer {authToken}");
                req.timeout = Mathf.RoundToInt(timeoutSec);

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var downloaded = JsonUtility.FromJson<SaveData>(req.downloadHandler.text);
                        if (downloaded != null)
                        {
                            // Replace local data with cloud data and persist
                            _save.Data.keyValues   = downloaded.keyValues  ?? _save.Data.keyValues;
                            _save.Data.favorites   = downloaded.favorites  ?? _save.Data.favorites;
                            _save.Data.journal     = downloaded.journal    ?? _save.Data.journal;
                            _save.Data.totalFlights          = downloaded.totalFlights;
                            _save.Data.totalFlightTimeSec    = downloaded.totalFlightTimeSec;
                            _save.Data.allTimeMaxAltitudeKm  = downloaded.allTimeMaxAltitudeKm;
                            _save.Data.totalDistanceKm       = downloaded.totalDistanceKm;
                            _save.Save();
                        }

                        LastSyncTime = DateTime.UtcNow;
                        Debug.Log("[SWEF] CloudSaveController: download succeeded.");
                        OnDownloadComplete?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        RaiseError($"Download parse error: {ex.Message}");
                    }
                }
                else
                {
                    RaiseError($"Download failed: {req.error} (HTTP {req.responseCode})");
                }
            }

            IsDownloading = false;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void HandleSaveCompleted() => Upload();

        private void RaiseError(string msg)
        {
            Debug.LogError($"[SWEF] CloudSaveController: {msg}");
            IsUploading   = false;
            IsDownloading = false;
            OnCloudError?.Invoke(msg);
        }
    }
}
