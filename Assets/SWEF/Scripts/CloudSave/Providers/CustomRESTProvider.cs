// CustomRESTProvider.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Generic REST API backend — upload/download raw bytes via HTTP PUT/GET.
// Namespace: SWEF.CloudSave

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.CloudSave
{
    /// <summary>
    /// Phase 111 — <see cref="ICloudSaveProvider"/> that communicates with a
    /// developer-supplied REST API.
    ///
    /// <para>Expected endpoints (configurable via <see cref="CloudSaveConfig"/>):</para>
    /// <list type="bullet">
    ///   <item><c>PUT  {baseUrl}/{key}</c>  — upload raw bytes (Content-Type: application/octet-stream)</item>
    ///   <item><c>GET  {baseUrl}/{key}</c>  — download raw bytes</item>
    ///   <item><c>DELETE {baseUrl}/{key}</c> — delete a key</item>
    ///   <item><c>GET  {baseUrl}/</c>        — list all keys (JSON array of strings)</item>
    /// </list>
    /// </summary>
    public sealed class CustomRESTProvider : ICloudSaveProvider
    {
        // ── ICloudSaveProvider identity ────────────────────────────────────────

        /// <inheritdoc/>
        public CloudProviderType ProviderType => CloudProviderType.CustomREST;

        /// <inheritdoc/>
        public string ProviderName => "Custom REST";

        // ── Config ─────────────────────────────────────────────────────────────

        private readonly string _baseUrl;
        private readonly string _apiKey;
        private const float     TimeoutSec = 30f;

        // ── Internal state ─────────────────────────────────────────────────────

        private ProviderStatus _status = new ProviderStatus
        {
            ProviderType     = CloudProviderType.CustomREST,
            ConnectionStatus = ProviderConnectionStatus.Disconnected,
            SyncStatus       = SyncStatus.Unavailable
        };

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>Creates the provider with the supplied base URL and optional API key.</summary>
        public CustomRESTProvider(string baseUrl, string apiKey)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? string.Empty;
            _apiKey  = apiKey ?? string.Empty;
        }

        // ── ICloudSaveProvider lifecycle ───────────────────────────────────────

        /// <inheritdoc/>
        public IEnumerator InitialiseAsync(Action<bool, string> onComplete)
        {
            if (string.IsNullOrEmpty(_baseUrl))
            {
                _status.ConnectionStatus = ProviderConnectionStatus.Error;
                _status.LastError        = "REST base URL is not configured.";
                onComplete?.Invoke(false, _status.LastError);
                yield break;
            }

            // Ping the base URL to verify connectivity.
            using (var req = UnityWebRequest.Head(_baseUrl + "/"))
            {
                ApplyAuth(req);
                req.timeout = Mathf.RoundToInt(TimeoutSec);
                yield return req.SendWebRequest();

                bool ok = req.result == UnityWebRequest.Result.Success ||
                          req.responseCode == 405; // Method Not Allowed is still reachable

                _status.ConnectionStatus = ok
                    ? ProviderConnectionStatus.Connected
                    : ProviderConnectionStatus.Error;
                _status.SyncStatus = ok ? SyncStatus.Synced : SyncStatus.Error;
                _status.LastError  = ok ? null : req.error;

                onComplete?.Invoke(ok, _status.LastError);
            }
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            _status.ConnectionStatus = ProviderConnectionStatus.Disconnected;
        }

        /// <inheritdoc/>
        public ProviderStatus GetStatus() => _status;

        // ── ICloudSaveProvider Save / Load ─────────────────────────────────────

        /// <inheritdoc/>
        public IEnumerator UploadAsync(string key, byte[] data, Action<bool, string> onComplete)
        {
            string url = $"{_baseUrl}/{Uri.EscapeUriString(key)}";
            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT))
            {
                req.uploadHandler   = new UploadHandlerRaw(data);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/octet-stream");
                ApplyAuth(req);
                req.timeout = Mathf.RoundToInt(TimeoutSec);

                yield return req.SendWebRequest();

                bool ok = req.result == UnityWebRequest.Result.Success;
                if (ok) _status.LastSyncTime = DateTime.UtcNow;
                onComplete?.Invoke(ok, ok ? null : req.error);
            }
        }

        /// <inheritdoc/>
        public IEnumerator DownloadAsync(string key, Action<byte[], string> onComplete)
        {
            string url = $"{_baseUrl}/{Uri.EscapeUriString(key)}";
            using (var req = UnityWebRequest.Get(url))
            {
                ApplyAuth(req);
                req.timeout = Mathf.RoundToInt(TimeoutSec);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                    onComplete?.Invoke(req.downloadHandler.data, null);
                else
                    onComplete?.Invoke(null, req.error);
            }
        }

        /// <inheritdoc/>
        public IEnumerator DeleteAsync(string key, Action<bool, string> onComplete)
        {
            string url = $"{_baseUrl}/{Uri.EscapeUriString(key)}";
            using (var req = UnityWebRequest.Delete(url))
            {
                ApplyAuth(req);
                req.timeout = Mathf.RoundToInt(TimeoutSec);
                yield return req.SendWebRequest();

                bool ok = req.result == UnityWebRequest.Result.Success;
                onComplete?.Invoke(ok, ok ? null : req.error);
            }
        }

        /// <inheritdoc/>
        public IEnumerator ListKeysAsync(Action<Dictionary<string, DateTime>, string> onComplete)
        {
            string url = _baseUrl + "/";
            using (var req = UnityWebRequest.Get(url))
            {
                ApplyAuth(req);
                req.timeout = Mathf.RoundToInt(TimeoutSec);
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(null, req.error);
                    yield break;
                }

                var result = new Dictionary<string, DateTime>(StringComparer.Ordinal);
                try
                {
                    // Expect a JSON array of strings: ["key1","key2",...]
                    string json   = req.downloadHandler.text;
                    var    parsed = JsonUtility.FromJson<StringArrayWrapper>("{\"items\":" + json + "}");
                    if (parsed?.items != null)
                    {
                        foreach (string k in parsed.items)
                            result[k] = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    onComplete?.Invoke(null, ex.Message);
                    yield break;
                }

                onComplete?.Invoke(result, null);
            }
        }

        /// <inheritdoc/>
        public IEnumerator GetCloudTimestampAsync(string key, Action<DateTime, string> onComplete)
        {
            // HEAD request — look for Last-Modified header.
            string url = $"{_baseUrl}/{Uri.EscapeUriString(key)}";
            using (var req = UnityWebRequest.Head(url))
            {
                ApplyAuth(req);
                req.timeout = Mathf.RoundToInt(TimeoutSec);
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(DateTime.MinValue, req.error);
                    yield break;
                }

                string lastModified = req.GetResponseHeader("Last-Modified");
                if (!string.IsNullOrEmpty(lastModified) &&
                    DateTime.TryParse(lastModified, out DateTime ts))
                {
                    onComplete?.Invoke(ts.ToUniversalTime(), null);
                }
                else
                {
                    onComplete?.Invoke(DateTime.UtcNow, null);
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerator QueryQuotaAsync(Action<long, long, string> onComplete)
        {
            onComplete?.Invoke(0L, 0L, null);
            yield break;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void ApplyAuth(UnityWebRequest req)
        {
            if (!string.IsNullOrEmpty(_apiKey))
                req.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
        }

        /// <summary>Helper wrapper for JsonUtility array deserialisation.</summary>
        [Serializable]
        private class StringArrayWrapper
        {
            public string[] items;
        }
    }
}
