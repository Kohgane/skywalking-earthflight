// SaveDataRegistry.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Aggregates all known SWEF JSON persistence files into a single registry.
// Namespace: SWEF.CloudSave

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SWEF.CloudSave
{
    /// <summary>
    /// Phase 111 — Centralized registry of every JSON save file produced by SWEF.
    /// The registry is used by <see cref="CloudSyncEngine"/> to determine which files
    /// are dirty and need to be uploaded (delta sync).
    ///
    /// <para>All paths default to <see cref="Application.persistentDataPath"/>.</para>
    /// </summary>
    public sealed class SaveDataRegistry
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        private static SaveDataRegistry _instance;

        /// <summary>Global singleton — lazily initialised on first access.</summary>
        public static SaveDataRegistry Instance =>
            _instance ?? (_instance = new SaveDataRegistry());

        // ── Registry ──────────────────────────────────────────────────────────

        private readonly Dictionary<string, SaveFileRecord> _records =
            new Dictionary<string, SaveFileRecord>(StringComparer.Ordinal);

        // ── Constructor ───────────────────────────────────────────────────────

        private SaveDataRegistry()
        {
            RegisterBuiltInFiles();
        }

        // ── Registration ──────────────────────────────────────────────────────

        /// <summary>
        /// Registers all 20+ built-in SWEF JSON save files.
        /// Keys match the file stem (without extension).
        /// </summary>
        private void RegisterBuiltInFiles()
        {
            string root = Application.persistentDataPath;

            // Core player data
            Register("player_profile",      Path.Combine(root, "player_profile.json"));
            Register("player_progression",  Path.Combine(root, "progression.json"));
            Register("achievements",        Path.Combine(root, "achievements.json"));
            Register("flight_journal",      Path.Combine(root, "flight_journal.json"));
            Register("statistics",          Path.Combine(root, "statistics.json"));

            // Gameplay systems
            Register("settings",            Path.Combine(root, "settings.json"));
            Register("accessibility",       Path.Combine(root, "accessibility.json"));
            Register("keybinds",            Path.Combine(root, "keybinds.json"));
            Register("workshop_builds",     Path.Combine(root, "workshop_builds.json"));
            Register("paint_schemes",       Path.Combine(root, "paint_schemes.json"));
            Register("flight_plans",        Path.Combine(root, "flight_plans.json"));
            Register("favorites",           Path.Combine(root, "favorites.json"));
            Register("hidden_gems",         Path.Combine(root, "hidden_gems.json"));
            Register("daily_challenges",    Path.Combine(root, "daily_challenges.json"));

            // Social & multiplayer
            Register("squadron_data",       Path.Combine(root, "squadron_data.json"));
            Register("friend_list",         Path.Combine(root, "friend_list.json"));
            Register("social_profile",      Path.Combine(root, "social_profile.json"));

            // Progression systems
            Register("battle_pass",         Path.Combine(root, "battle_pass.json"));
            Register("academy_progress",    Path.Combine(root, "academy_progress.json"));
            Register("certificates",        Path.Combine(root, "certificates.json"));

            // Photography & creative
            Register("photo_album",         Path.Combine(root, "photo_album.json"));
            Register("ugc_content",         Path.Combine(root, "ugc_content.json"));

            // Cloud-specific
            Register("cross_platform_profile", Path.Combine(root, "cross_platform_profile.json"));
        }

        /// <summary>
        /// Manually registers a custom save file in addition to the built-in set.
        /// </summary>
        /// <param name="key">Unique key for the file.</param>
        /// <param name="absolutePath">Absolute filesystem path to the JSON file.</param>
        public void Register(string key, string absolutePath)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(absolutePath))
                throw new ArgumentNullException(nameof(absolutePath));

            if (!_records.ContainsKey(key))
            {
                _records[key] = new SaveFileRecord
                {
                    FileKey           = key,
                    LocalPath         = absolutePath,
                    LocalModifiedAt   = DateTime.MinValue,
                    CloudModifiedAt   = DateTime.MinValue,
                    LocalContentHash  = string.Empty,
                    IsDirty           = false
                };
            }
        }

        // ── Snapshot & dirty detection ────────────────────────────────────────

        /// <summary>
        /// Refreshes timestamps and content hashes for all registered files,
        /// marking records whose content has changed since the last snapshot.
        /// </summary>
        public void RefreshDirtyFlags()
        {
            foreach (var record in _records.Values)
            {
                if (!File.Exists(record.LocalPath))
                    continue;

                var    info    = new FileInfo(record.LocalPath);
                string newHash = ComputeFileHash(record.LocalPath);

                if (newHash != record.LocalContentHash)
                {
                    record.LocalModifiedAt  = info.LastWriteTimeUtc;
                    record.LocalContentHash = newHash;
                    record.IsDirty          = true;
                }
            }
        }

        /// <summary>
        /// Marks a specific file as clean (synced) and stores the cloud timestamp.
        /// </summary>
        public void MarkClean(string key, DateTime cloudModifiedAt)
        {
            if (_records.TryGetValue(key, out var record))
            {
                record.IsDirty         = false;
                record.CloudModifiedAt = cloudModifiedAt;
            }
        }

        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>Returns all registered <see cref="SaveFileRecord"/> instances.</summary>
        public IReadOnlyCollection<SaveFileRecord> AllRecords => _records.Values;

        /// <summary>Returns only records whose <see cref="SaveFileRecord.IsDirty"/> flag is set.</summary>
        public IEnumerable<SaveFileRecord> DirtyRecords =>
            _records.Values.Where(r => r.IsDirty);

        /// <summary>Retrieves a record by key, or <c>null</c> if not found.</summary>
        public SaveFileRecord GetRecord(string key) =>
            _records.TryGetValue(key, out var rec) ? rec : null;

        /// <summary>Returns <c>true</c> if any registered file is dirty.</summary>
        public bool HasPendingChanges => _records.Values.Any(r => r.IsDirty);

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string ComputeFileHash(string path)
        {
            try
            {
                using (var sha = SHA256.Create())
                using (var fs  = File.OpenRead(path))
                {
                    byte[] hash = sha.ComputeHash(fs);
                    return Convert.ToBase64String(hash);
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
