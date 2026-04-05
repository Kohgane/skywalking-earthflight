// ICloudSaveProvider.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Abstract interface that all cloud save backends must implement.
// Namespace: SWEF.CloudSave

using System;
using System.Collections;
using System.Collections.Generic;

namespace SWEF.CloudSave
{
    /// <summary>
    /// Phase 111 — Abstraction layer for cloud save backends.
    ///
    /// <para>All operations are asynchronous and use Unity coroutines so they
    /// are safe to call from MonoBehaviours without blocking the main thread.</para>
    ///
    /// <para>Implementations: <see cref="LocalFileProvider"/>,
    /// <see cref="UnityCloudSaveProvider"/>, <see cref="FirebaseProvider"/>,
    /// <see cref="CustomRESTProvider"/>.</para>
    /// </summary>
    public interface ICloudSaveProvider
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Type identifier for this provider.</summary>
        CloudProviderType ProviderType { get; }

        /// <summary>Human-readable name of this provider (e.g. "Unity Cloud Save").</summary>
        string ProviderName { get; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the provider (authenticate, verify connectivity, etc.).
        /// Must be called once before any other method.
        /// </summary>
        /// <param name="onComplete">
        /// Callback: <c>true</c> = success, <c>false</c> = failure with reason string.
        /// </param>
        IEnumerator InitialiseAsync(Action<bool, string> onComplete);

        /// <summary>
        /// Terminates the provider session and releases resources.
        /// </summary>
        void Shutdown();

        // ── Status ────────────────────────────────────────────────────────────

        /// <summary>Latest connection and sync status snapshot.</summary>
        ProviderStatus GetStatus();

        // ── Save / Load ───────────────────────────────────────────────────────

        /// <summary>
        /// Uploads the file identified by <paramref name="key"/> to cloud storage.
        /// </summary>
        /// <param name="key">Stable file key (see <see cref="SaveDataRegistry"/>).</param>
        /// <param name="data">Raw bytes to upload (may be compressed / encrypted).</param>
        /// <param name="onComplete">Callback: <c>true</c> = success.</param>
        IEnumerator UploadAsync(string key, byte[] data, Action<bool, string> onComplete);

        /// <summary>
        /// Downloads the cloud copy of <paramref name="key"/>.
        /// </summary>
        /// <param name="onComplete">Callback with downloaded bytes, or <c>null</c> on failure.</param>
        IEnumerator DownloadAsync(string key, Action<byte[], string> onComplete);

        /// <summary>
        /// Deletes the cloud copy of <paramref name="key"/>.
        /// </summary>
        IEnumerator DeleteAsync(string key, Action<bool, string> onComplete);

        // ── Metadata ──────────────────────────────────────────────────────────

        /// <summary>
        /// Lists all keys stored in the cloud for the current user, along with
        /// their last-modified timestamps.
        /// </summary>
        IEnumerator ListKeysAsync(Action<Dictionary<string, DateTime>, string> onComplete);

        /// <summary>
        /// Returns the server-side last-modified timestamp for <paramref name="key"/>,
        /// or <see cref="DateTime.MinValue"/> if the key does not exist.
        /// </summary>
        IEnumerator GetCloudTimestampAsync(string key, Action<DateTime, string> onComplete);

        // ── Quota ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Queries the current quota usage.
        /// </summary>
        /// <param name="onComplete">Callback: (usedBytes, totalBytes, errorMessage).</param>
        IEnumerator QueryQuotaAsync(Action<long, long, string> onComplete);
    }
}
