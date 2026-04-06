// FirebaseProvider.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Firebase Realtime Database / Firestore backend.
// Namespace: SWEF.CloudSave

// Active only when SWEF_FIREBASE_AVAILABLE is defined.
// Add the Firebase Unity SDK and set the define in Player Settings.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_FIREBASE_AVAILABLE
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
#endif

namespace SWEF.CloudSave
{
    /// <summary>
    /// Phase 111 — <see cref="ICloudSaveProvider"/> backed by Firebase Realtime Database.
    ///
    /// <para><b>Activation:</b> add <c>SWEF_FIREBASE_AVAILABLE</c> to
    /// <em>Project Settings → Player → Scripting Define Symbols</em> and install
    /// the Firebase Unity SDK.</para>
    /// </summary>
    public sealed class FirebaseProvider : ICloudSaveProvider
    {
        // ── ICloudSaveProvider identity ────────────────────────────────────────

        /// <inheritdoc/>
        public CloudProviderType ProviderType => CloudProviderType.Firebase;

        /// <inheritdoc/>
        public string ProviderName => "Firebase";

        // ── Config ─────────────────────────────────────────────────────────────

        /// <summary>Firebase Realtime Database URL (e.g. https://&lt;project&gt;.firebaseio.com/).</summary>
        public string DatabaseUrl { get; set; } = string.Empty;

        /// <summary>Base node path in the database (e.g. "swef/saves/{userId}").</summary>
        public string BasePath { get; set; } = "swef/saves";

        // ── Internal state ─────────────────────────────────────────────────────

        private ProviderStatus _status = new ProviderStatus
        {
            ProviderType     = CloudProviderType.Firebase,
            ConnectionStatus = ProviderConnectionStatus.Disconnected,
            SyncStatus       = SyncStatus.Unavailable
        };

        // ── ICloudSaveProvider lifecycle ───────────────────────────────────────

        /// <inheritdoc/>
        public IEnumerator InitialiseAsync(Action<bool, string> onComplete)
        {
#if SWEF_FIREBASE_AVAILABLE
            bool done    = false;
            bool success = false;
            string error = null;

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    success = true;
                }
                else
                {
                    error   = $"Firebase dependency check failed: {task.Result}";
                    success = false;
                }
                done = true;
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
            _status.LastError        = "SWEF_FIREBASE_AVAILABLE not defined — Firebase SDK not installed.";
            onComplete?.Invoke(false, _status.LastError);
            yield break;
#endif
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
#if SWEF_FIREBASE_AVAILABLE
            bool done    = false;
            bool success = false;
            string error = null;

            string encoded = Convert.ToBase64String(data);
            string nodePath = $"{BasePath}/{key}";

            FirebaseDatabase.DefaultInstance.GetReference(nodePath)
                .SetValueAsync(encoded).ContinueWithOnMainThread(task =>
                {
                    success = !task.IsFaulted;
                    error   = task.IsFaulted ? task.Exception?.GetBaseException().Message : null;
                    done    = true;
                });

            yield return new WaitUntil(() => done);
            if (success) _status.LastSyncTime = DateTime.UtcNow;
            onComplete?.Invoke(success, error);
#else
            onComplete?.Invoke(false, "SWEF_FIREBASE_AVAILABLE not defined.");
            yield break;
#endif
        }

        /// <inheritdoc/>
        public IEnumerator DownloadAsync(string key, Action<byte[], string> onComplete)
        {
#if SWEF_FIREBASE_AVAILABLE
            bool done    = false;
            byte[] result = null;
            string error  = null;

            string nodePath = $"{BasePath}/{key}";
            FirebaseDatabase.DefaultInstance.GetReference(nodePath)
                .GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        error = task.Exception?.GetBaseException().Message;
                    }
                    else if (task.Result.Exists)
                    {
                        try   { result = Convert.FromBase64String(task.Result.Value.ToString()); }
                        catch (Exception ex) { error = ex.Message; }
                    }
                    else
                    {
                        error = $"Key '{key}' not found in Firebase.";
                    }
                    done = true;
                });

            yield return new WaitUntil(() => done);
            onComplete?.Invoke(result, error);
#else
            onComplete?.Invoke(null, "SWEF_FIREBASE_AVAILABLE not defined.");
            yield break;
#endif
        }

        /// <inheritdoc/>
        public IEnumerator DeleteAsync(string key, Action<bool, string> onComplete)
        {
#if SWEF_FIREBASE_AVAILABLE
            bool done    = false;
            bool success = false;
            string error = null;

            string nodePath = $"{BasePath}/{key}";
            FirebaseDatabase.DefaultInstance.GetReference(nodePath)
                .RemoveValueAsync().ContinueWithOnMainThread(task =>
                {
                    success = !task.IsFaulted;
                    error   = task.IsFaulted ? task.Exception?.GetBaseException().Message : null;
                    done    = true;
                });

            yield return new WaitUntil(() => done);
            onComplete?.Invoke(success, error);
#else
            onComplete?.Invoke(false, "SWEF_FIREBASE_AVAILABLE not defined.");
            yield break;
#endif
        }

        /// <inheritdoc/>
        public IEnumerator ListKeysAsync(Action<Dictionary<string, DateTime>, string> onComplete)
        {
#if SWEF_FIREBASE_AVAILABLE
            bool done   = false;
            Dictionary<string, DateTime> result = null;
            string error = null;

            FirebaseDatabase.DefaultInstance.GetReference(BasePath)
                .GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        error = task.Exception?.GetBaseException().Message;
                    }
                    else
                    {
                        result = new Dictionary<string, DateTime>(StringComparer.Ordinal);
                        foreach (var child in task.Result.Children)
                            result[child.Key] = DateTime.UtcNow;
                    }
                    done = true;
                });

            yield return new WaitUntil(() => done);
            onComplete?.Invoke(result, error);
#else
            onComplete?.Invoke(null, "SWEF_FIREBASE_AVAILABLE not defined.");
            yield break;
#endif
        }

        /// <inheritdoc/>
        public IEnumerator GetCloudTimestampAsync(string key, Action<DateTime, string> onComplete)
        {
            onComplete?.Invoke(DateTime.UtcNow, null);
            yield break;
        }

        /// <inheritdoc/>
        public IEnumerator QueryQuotaAsync(Action<long, long, string> onComplete)
        {
            onComplete?.Invoke(0L, 0L, null);
            yield break;
        }
    }
}
