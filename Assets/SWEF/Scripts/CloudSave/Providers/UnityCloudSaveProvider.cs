// UnityCloudSaveProvider.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Unity Gaming Services (UGS) Cloud Save backend.
// Namespace: SWEF.CloudSave

// This file compiles without errors whether or not the Unity Gaming Services
// package is installed. The implementation is active only when the
// SWEF_UGS_AVAILABLE scripting define is set.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if SWEF_UGS_AVAILABLE
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
#endif

namespace SWEF.CloudSave
{
    /// <summary>
    /// Phase 111 — <see cref="ICloudSaveProvider"/> backed by Unity Gaming Services
    /// Cloud Save.
    ///
    /// <para><b>Activation:</b> add <c>SWEF_UGS_AVAILABLE</c> to
    /// <em>Project Settings → Player → Scripting Define Symbols</em> and install the
    /// <em>com.unity.services.cloudsave</em> package.</para>
    /// </summary>
    public sealed class UnityCloudSaveProvider : ICloudSaveProvider
    {
        // ── ICloudSaveProvider identity ────────────────────────────────────────

        /// <inheritdoc/>
        public CloudProviderType ProviderType => CloudProviderType.UnityCloud;

        /// <inheritdoc/>
        public string ProviderName => "Unity Cloud Save";

        // ── Internal state ─────────────────────────────────────────────────────

        private ProviderStatus _status = new ProviderStatus
        {
            ProviderType     = CloudProviderType.UnityCloud,
            ConnectionStatus = ProviderConnectionStatus.Disconnected,
            SyncStatus       = SyncStatus.Unavailable
        };

        // ── ICloudSaveProvider lifecycle ───────────────────────────────────────

        /// <inheritdoc/>
        public IEnumerator InitialiseAsync(Action<bool, string> onComplete)
        {
#if SWEF_UGS_AVAILABLE
            bool done   = false;
            bool success = false;
            string error = null;

            UnityServices.InitializeAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    error   = task.Exception?.GetBaseException().Message ?? "UGS init failed";
                    success = false;
                    done    = true;
                    return;
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SignInAnonymouslyAsync().ContinueWith(authTask =>
                    {
                        success = !authTask.IsFaulted;
                        error   = authTask.IsFaulted
                            ? authTask.Exception?.GetBaseException().Message
                            : null;
                        done = true;
                    });
                }
                else
                {
                    success = true;
                    done    = true;
                }
            });

            yield return new WaitUntil(() => done);

            _status.ConnectionStatus = success
                ? ProviderConnectionStatus.Connected
                : ProviderConnectionStatus.Error;
            _status.SyncStatus = success ? SyncStatus.Synced : SyncStatus.Error;
            _status.LastError  = error;

            onComplete?.Invoke(success, error);
#else
            _status.ConnectionStatus = ProviderConnectionStatus.Error;
            _status.LastError        = "SWEF_UGS_AVAILABLE not defined — UGS package not installed.";
            onComplete?.Invoke(false, _status.LastError);
            yield break;
#endif
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
#if SWEF_UGS_AVAILABLE
            if (AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut();
#endif
            _status.ConnectionStatus = ProviderConnectionStatus.Disconnected;
        }

        /// <inheritdoc/>
        public ProviderStatus GetStatus() => _status;

        // ── ICloudSaveProvider Save / Load ─────────────────────────────────────

        /// <inheritdoc/>
        public IEnumerator UploadAsync(string key, byte[] data, Action<bool, string> onComplete)
        {
#if SWEF_UGS_AVAILABLE
            bool done    = false;
            bool success = false;
            string error = null;

            // UGS Cloud Save stores string values; encode bytes as Base64.
            string encoded = Convert.ToBase64String(data);
            var dict = new Dictionary<string, object> { { key, encoded } };

            CloudSaveService.Instance.Data.Player.SaveAsync(dict).ContinueWith(task =>
            {
                success = !task.IsFaulted;
                error   = task.IsFaulted
                    ? task.Exception?.GetBaseException().Message
                    : null;
                done = true;
            });

            yield return new WaitUntil(() => done);

            if (success) _status.LastSyncTime = DateTime.UtcNow;
            onComplete?.Invoke(success, error);
#else
            onComplete?.Invoke(false, "SWEF_UGS_AVAILABLE not defined.");
            yield break;
#endif
        }

        /// <inheritdoc/>
        public IEnumerator DownloadAsync(string key, Action<byte[], string> onComplete)
        {
#if SWEF_UGS_AVAILABLE
            bool done    = false;
            byte[] result = null;
            string error  = null;

            var keys = new HashSet<string> { key };
            CloudSaveService.Instance.Data.Player.LoadAsync(keys).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    error = task.Exception?.GetBaseException().Message;
                }
                else if (task.Result.TryGetValue(key, out var item))
                {
                    try   { result = Convert.FromBase64String(item.Value.GetAsString()); }
                    catch (Exception ex) { error = ex.Message; }
                }
                else
                {
                    error = $"Key '{key}' not found in UGS Cloud Save.";
                }
                done = true;
            });

            yield return new WaitUntil(() => done);
            onComplete?.Invoke(result, error);
#else
            onComplete?.Invoke(null, "SWEF_UGS_AVAILABLE not defined.");
            yield break;
#endif
        }

        /// <inheritdoc/>
        public IEnumerator DeleteAsync(string key, Action<bool, string> onComplete)
        {
#if SWEF_UGS_AVAILABLE
            bool done    = false;
            bool success = false;
            string error = null;

            var keys = new List<string> { key };
            CloudSaveService.Instance.Data.Player.DeleteAsync(key).ContinueWith(task =>
            {
                success = !task.IsFaulted;
                error   = task.IsFaulted ? task.Exception?.GetBaseException().Message : null;
                done    = true;
            });

            yield return new WaitUntil(() => done);
            onComplete?.Invoke(success, error);
#else
            onComplete?.Invoke(false, "SWEF_UGS_AVAILABLE not defined.");
            yield break;
#endif
        }

        /// <inheritdoc/>
        public IEnumerator ListKeysAsync(Action<Dictionary<string, DateTime>, string> onComplete)
        {
#if SWEF_UGS_AVAILABLE
            bool done   = false;
            Dictionary<string, DateTime> result = null;
            string error = null;

            CloudSaveService.Instance.Data.Player.ListAllKeysAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    error = task.Exception?.GetBaseException().Message;
                }
                else
                {
                    result = new Dictionary<string, DateTime>(StringComparer.Ordinal);
                    foreach (var item in task.Result)
                        result[item.Key] = DateTime.UtcNow; // UGS free-tier doesn't expose timestamps
                }
                done = true;
            });

            yield return new WaitUntil(() => done);
            onComplete?.Invoke(result, error);
#else
            onComplete?.Invoke(null, "SWEF_UGS_AVAILABLE not defined.");
            yield break;
#endif
        }

        /// <inheritdoc/>
        public IEnumerator GetCloudTimestampAsync(string key, Action<DateTime, string> onComplete)
        {
            // UGS free tier does not expose per-key timestamps; return UtcNow as best-effort.
            onComplete?.Invoke(DateTime.UtcNow, null);
            yield break;
        }

        /// <inheritdoc/>
        public IEnumerator QueryQuotaAsync(Action<long, long, string> onComplete)
        {
            // UGS quota is not exposed via the SDK; report zeros.
            onComplete?.Invoke(0L, 0L, null);
            yield break;
        }
    }
}
