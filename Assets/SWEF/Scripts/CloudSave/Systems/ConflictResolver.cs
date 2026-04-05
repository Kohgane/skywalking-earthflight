// ConflictResolver.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Handles merge strategies when local and cloud saves conflict.
// Namespace: SWEF.CloudSave

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CloudSave
{
    /// <summary>
    /// Represents a detected conflict between a local and cloud save file.
    /// </summary>
    public class SaveConflict
    {
        /// <summary>Logical key of the conflicting file.</summary>
        public string FileKey;

        /// <summary>Local modification timestamp.</summary>
        public DateTime LocalModifiedAt;

        /// <summary>Cloud modification timestamp.</summary>
        public DateTime CloudModifiedAt;

        /// <summary>Local file content (raw bytes).</summary>
        public byte[] LocalData;

        /// <summary>Cloud file content (raw bytes).</summary>
        public byte[] CloudData;
    }

    /// <summary>User's choice when prompted to resolve a conflict manually.</summary>
    public enum ConflictChoice
    {
        /// <summary>Keep the local copy, overwrite cloud.</summary>
        KeepLocal = 0,
        /// <summary>Use the cloud copy, overwrite local.</summary>
        UseCloud  = 1,
        /// <summary>Merge the two versions (if a merge function is registered).</summary>
        Merge     = 2
    }

    /// <summary>
    /// Phase 111 — Applies the configured <see cref="ConflictResolutionStrategy"/> to a
    /// detected save conflict and returns the winning byte array.
    ///
    /// <para>For <see cref="ConflictResolutionStrategy.PromptUser"/>, the resolver raises
    /// <see cref="OnConflictPromptRequired"/> and waits for
    /// <see cref="ResolveUserChoice"/> to be called externally (e.g. from UI).</para>
    /// </summary>
    public sealed class ConflictResolver
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        private static ConflictResolver _instance;

        /// <summary>Global singleton — lazily initialised on first access.</summary>
        public static ConflictResolver Instance =>
            _instance ?? (_instance = new ConflictResolver());

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when <see cref="ConflictResolutionStrategy.PromptUser"/> is active and
        /// user input is required.  Subscribe the <see cref="CloudSaveUI"/> to handle this.
        /// </summary>
        public event Action<SaveConflict> OnConflictPromptRequired;

        /// <summary>Raised when a conflict has been resolved.</summary>
        public event Action<string, ConflictChoice> OnConflictResolved;

        // ── Pending user choices ───────────────────────────────────────────────

        private readonly Dictionary<string, ConflictChoice?> _pendingChoices =
            new Dictionary<string, ConflictChoice?>(StringComparer.Ordinal);

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves <paramref name="conflict"/> according to the configured strategy.
        /// </summary>
        /// <returns>The winning byte array to write locally / upload to cloud.</returns>
        public byte[] Resolve(SaveConflict conflict, ConflictResolutionStrategy strategy)
        {
            if (conflict == null) throw new ArgumentNullException(nameof(conflict));

            switch (strategy)
            {
                case ConflictResolutionStrategy.LastWriteWins:
                    return ResolveLastWriteWins(conflict);

                case ConflictResolutionStrategy.MergeByTimestamp:
                    return ResolveMergeByTimestamp(conflict);

                case ConflictResolutionStrategy.PromptUser:
                    return ResolvePromptUser(conflict);

                default:
                    Debug.LogWarning(
                        $"[ConflictResolver] Unknown strategy '{strategy}' — falling back to LastWriteWins.");
                    return ResolveLastWriteWins(conflict);
            }
        }

        /// <summary>
        /// Call this from the UI to supply the user's choice for a pending conflict.
        /// </summary>
        public void ResolveUserChoice(string fileKey, ConflictChoice choice)
        {
            if (_pendingChoices.ContainsKey(fileKey))
            {
                _pendingChoices[fileKey] = choice;
                OnConflictResolved?.Invoke(fileKey, choice);
            }
        }

        /// <summary>
        /// Returns the pending user choice for <paramref name="fileKey"/>,
        /// or <c>null</c> if the user has not yet responded.
        /// </summary>
        public ConflictChoice? GetPendingChoice(string fileKey) =>
            _pendingChoices.TryGetValue(fileKey, out var choice) ? choice : (ConflictChoice?)null;

        // ── Strategy implementations ───────────────────────────────────────────

        private static byte[] ResolveLastWriteWins(SaveConflict conflict)
        {
            return conflict.CloudModifiedAt >= conflict.LocalModifiedAt
                ? conflict.CloudData
                : conflict.LocalData;
        }

        private static byte[] ResolveMergeByTimestamp(SaveConflict conflict)
        {
            // Without a domain-specific merge function, fall back to last-write-wins.
            // Developers can extend this by hooking into the resolver.
            Debug.Log(
                $"[ConflictResolver] MergeByTimestamp for '{conflict.FileKey}' — " +
                "using LastWriteWins as no merge function is registered.");
            return ResolveLastWriteWins(conflict);
        }

        private byte[] ResolvePromptUser(SaveConflict conflict)
        {
            // Register pending choice
            if (!_pendingChoices.ContainsKey(conflict.FileKey))
                _pendingChoices[conflict.FileKey] = null;

            // Raise event so the UI can show the dialog
            OnConflictPromptRequired?.Invoke(conflict);

            // Return local data as temporary placeholder until user decides.
            // The sync engine should re-check GetPendingChoice after the event.
            return conflict.LocalData;
        }
    }
}
