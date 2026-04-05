// LocalFileProvider.cs — Phase 111: Cloud Save & Cross-Platform Sync
// JSON file-based provider — the always-available local fallback.
// Namespace: SWEF.CloudSave

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.CloudSave
{
    /// <summary>
    /// Phase 111 — <see cref="ICloudSaveProvider"/> that reads/writes files in a
    /// dedicated <c>CloudSaveLocal/</c> folder inside
    /// <see cref="Application.persistentDataPath"/>.
    ///
    /// <para>This provider never has connectivity issues and is used as the default
    /// fallback when no cloud backend is configured.</para>
    /// </summary>
    public sealed class LocalFileProvider : ICloudSaveProvider
    {
        // ── ICloudSaveProvider identity ────────────────────────────────────────

        /// <inheritdoc/>
        public CloudProviderType ProviderType => CloudProviderType.LocalFile;

        /// <inheritdoc/>
        public string ProviderName => "Local File";

        // ── Internal state ─────────────────────────────────────────────────────

        private string          _rootDir;
        private ProviderStatus  _status;
        private bool            _initialised;

        // ── ICloudSaveProvider lifecycle ───────────────────────────────────────

        /// <inheritdoc/>
        public IEnumerator InitialiseAsync(Action<bool, string> onComplete)
        {
            _rootDir = Path.Combine(Application.persistentDataPath, "CloudSaveLocal");

            try
            {
                Directory.CreateDirectory(_rootDir);
                _status = new ProviderStatus
                {
                    ProviderType      = CloudProviderType.LocalFile,
                    ConnectionStatus  = ProviderConnectionStatus.Connected,
                    SyncStatus        = SyncStatus.Synced,
                    LastSyncTime      = DateTime.UtcNow
                };
                _initialised = true;
                onComplete?.Invoke(true, null);
            }
            catch (Exception ex)
            {
                _status = new ProviderStatus
                {
                    ProviderType     = CloudProviderType.LocalFile,
                    ConnectionStatus = ProviderConnectionStatus.Error,
                    SyncStatus       = SyncStatus.Error,
                    LastError        = ex.Message
                };
                onComplete?.Invoke(false, ex.Message);
            }
            yield break;
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            _initialised = false;
            if (_status != null)
                _status.ConnectionStatus = ProviderConnectionStatus.Disconnected;
        }

        /// <inheritdoc/>
        public ProviderStatus GetStatus() => _status ?? new ProviderStatus
        {
            ProviderType     = CloudProviderType.LocalFile,
            ConnectionStatus = ProviderConnectionStatus.Disconnected,
            SyncStatus       = SyncStatus.Unavailable
        };

        // ── ICloudSaveProvider Save / Load ─────────────────────────────────────

        /// <inheritdoc/>
        public IEnumerator UploadAsync(string key, byte[] data, Action<bool, string> onComplete)
        {
            if (!_initialised) { onComplete?.Invoke(false, "Not initialised"); yield break; }

            try
            {
                string path = GetFilePath(key);
                File.WriteAllBytes(path, data);
                _status.LastSyncTime = DateTime.UtcNow;
                onComplete?.Invoke(true, null);
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(false, ex.Message);
            }
            yield break;
        }

        /// <inheritdoc/>
        public IEnumerator DownloadAsync(string key, Action<byte[], string> onComplete)
        {
            if (!_initialised) { onComplete?.Invoke(null, "Not initialised"); yield break; }

            try
            {
                string path = GetFilePath(key);
                if (!File.Exists(path))
                {
                    onComplete?.Invoke(null, $"Key '{key}' not found");
                }
                else
                {
                    byte[] data = File.ReadAllBytes(path);
                    onComplete?.Invoke(data, null);
                }
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(null, ex.Message);
            }
            yield break;
        }

        /// <inheritdoc/>
        public IEnumerator DeleteAsync(string key, Action<bool, string> onComplete)
        {
            if (!_initialised) { onComplete?.Invoke(false, "Not initialised"); yield break; }

            try
            {
                string path = GetFilePath(key);
                if (File.Exists(path)) File.Delete(path);
                onComplete?.Invoke(true, null);
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(false, ex.Message);
            }
            yield break;
        }

        /// <inheritdoc/>
        public IEnumerator ListKeysAsync(Action<Dictionary<string, DateTime>, string> onComplete)
        {
            if (!_initialised) { onComplete?.Invoke(null, "Not initialised"); yield break; }

            try
            {
                var result = new Dictionary<string, DateTime>(StringComparer.Ordinal);
                foreach (string file in Directory.GetFiles(_rootDir, "*.bin"))
                {
                    string key = Path.GetFileNameWithoutExtension(file);
                    result[key] = File.GetLastWriteTimeUtc(file);
                }
                onComplete?.Invoke(result, null);
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(null, ex.Message);
            }
            yield break;
        }

        /// <inheritdoc/>
        public IEnumerator GetCloudTimestampAsync(string key, Action<DateTime, string> onComplete)
        {
            if (!_initialised) { onComplete?.Invoke(DateTime.MinValue, "Not initialised"); yield break; }

            string path = GetFilePath(key);
            DateTime ts = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            onComplete?.Invoke(ts, null);
            yield break;
        }

        /// <inheritdoc/>
        public IEnumerator QueryQuotaAsync(Action<long, long, string> onComplete)
        {
            // Local storage: report used bytes; no hard cap.
            long used = 0;
            if (_initialised && Directory.Exists(_rootDir))
            {
                foreach (string file in Directory.GetFiles(_rootDir))
                    used += new FileInfo(file).Length;
            }
            onComplete?.Invoke(used, 0L, null);
            yield break;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private string GetFilePath(string key) => Path.Combine(_rootDir, key + ".bin");
    }
}
