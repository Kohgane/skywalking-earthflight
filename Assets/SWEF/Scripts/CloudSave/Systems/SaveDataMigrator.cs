// SaveDataMigrator.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Handles save-format version detection and sequential migration pipeline.
// Namespace: SWEF.CloudSave

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SWEF.CloudSave
{
    // ════════════════════════════════════════════════════════════════════════════
    // Migration step interface
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Represents a single version-to-version schema migration step.</summary>
    public interface ISaveMigrationStep
    {
        /// <summary>Source schema version (e.g. 1).</summary>
        int FromVersion { get; }

        /// <summary>Target schema version (e.g. 2).</summary>
        int ToVersion { get; }

        /// <summary>
        /// Transforms <paramref name="jsonData"/> from <see cref="FromVersion"/> to
        /// <see cref="ToVersion"/> format and returns the new JSON string.
        /// Returns <c>null</c> on failure.
        /// </summary>
        string Migrate(string jsonData);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Built-in migration steps
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Example migration: v1 → v2.
    /// Adds a <c>"schemaVersion": 2</c> field to any file that lacks it.
    /// </summary>
    public sealed class MigrationV1ToV2 : ISaveMigrationStep
    {
        /// <inheritdoc/>
        public int FromVersion => 1;
        /// <inheritdoc/>
        public int ToVersion   => 2;

        /// <inheritdoc/>
        public string Migrate(string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData)) return jsonData;

            // Insert schemaVersion field before the closing brace
            int lastBrace = jsonData.LastIndexOf('}');
            if (lastBrace < 0) return jsonData;

            string insert = "\"schemaVersion\": 2";
            // Avoid duplicate insertion
            if (jsonData.Contains("\"schemaVersion\"")) return jsonData;

            // Add a comma separator if the object is non-empty
            string trimmed   = jsonData.Substring(0, lastBrace).TrimEnd();
            bool   needComma = trimmed.Length > 1 && trimmed[trimmed.Length - 1] != '{';

            return trimmed + (needComma ? ", " : " ") + insert +
                   jsonData.Substring(lastBrace);
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Migrator
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 111 — Orchestrates save-format version detection and sequential
    /// migration, with pre-migration backup and rollback support.
    ///
    /// <para>Call <see cref="MigrateAll"/> at app start, before any save data is
    /// consumed by game systems.</para>
    /// </summary>
    public sealed class SaveDataMigrator
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        private static SaveDataMigrator _instance;

        /// <summary>Global singleton — lazily initialised on first access.</summary>
        public static SaveDataMigrator Instance =>
            _instance ?? (_instance = new SaveDataMigrator());

        // ── Constants ─────────────────────────────────────────────────────────

        /// <summary>Current schema version expected by this build.</summary>
        public const int CurrentVersion = 2;

        private const string BackupSuffix  = ".pre_migration_backup";
        private const string VersionField  = "\"schemaVersion\"";

        // ── Steps ─────────────────────────────────────────────────────────────

        private readonly List<ISaveMigrationStep> _steps = new List<ISaveMigrationStep>
        {
            new MigrationV1ToV2()
        };

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Registers a custom migration step in addition to the built-in ones.</summary>
        public void RegisterStep(ISaveMigrationStep step)
        {
            if (step != null) _steps.Add(step);
        }

        /// <summary>
        /// Iterates all registered save files and migrates each one whose
        /// detected schema version is below <see cref="CurrentVersion"/>.
        /// </summary>
        /// <returns><c>true</c> if all files are at the current version after the call.</returns>
        public bool MigrateAll()
        {
            bool allOk = true;

            foreach (var record in SaveDataRegistry.Instance.AllRecords)
            {
                if (!File.Exists(record.LocalPath)) continue;

                string json = null;
                try { json = File.ReadAllText(record.LocalPath); }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[SaveDataMigrator] Cannot read '{record.FileKey}': {ex.Message}");
                    allOk = false;
                    continue;
                }

                int detectedVersion = DetectVersion(json);
                if (detectedVersion >= CurrentVersion) continue;

                // Create backup before migrating
                string backupPath = record.LocalPath + BackupSuffix;
                try { File.Copy(record.LocalPath, backupPath, overwrite: true); }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[SaveDataMigrator] Backup failed for '{record.FileKey}': {ex.Message}");
                }

                bool migrated = MigrateFile(record.FileKey, record.LocalPath,
                                            json, detectedVersion, out string newJson);
                if (migrated)
                {
                    try { File.WriteAllText(record.LocalPath, newJson, Encoding.UTF8); }
                    catch (Exception ex)
                    {
                        Debug.LogError(
                            $"[SaveDataMigrator] Write failed for '{record.FileKey}': {ex.Message}. " +
                            "Attempting rollback.");
                        Rollback(record.LocalPath, backupPath);
                        allOk = false;
                    }
                }
                else
                {
                    Debug.LogError(
                        $"[SaveDataMigrator] Migration failed for '{record.FileKey}'. Rolling back.");
                    Rollback(record.LocalPath, backupPath);
                    allOk = false;
                }
            }

            return allOk;
        }

        /// <summary>
        /// Exports all registered save files into a single zip-like JSON bundle
        /// at <paramref name="exportPath"/> for manual backup.
        /// </summary>
        public bool ExportLocalBackup(string exportPath)
        {
            var bundle = new Dictionary<string, string>();
            foreach (var record in SaveDataRegistry.Instance.AllRecords)
            {
                if (!File.Exists(record.LocalPath)) continue;
                try { bundle[record.FileKey] = File.ReadAllText(record.LocalPath); }
                catch { /* skip unreadable files */ }
            }

            try
            {
                // Simple JSON envelope: {"files":{"key":"json",...}}
                var sb = new StringBuilder("{\"files\":{");
                bool first = true;
                foreach (var kv in bundle)
                {
                    if (!first) sb.Append(',');
                    sb.Append('"');
                    sb.Append(kv.Key);
                    sb.Append("\":");
                    sb.Append(kv.Value);
                    first = false;
                }
                sb.Append("}}");

                File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveDataMigrator] Export failed: {ex.Message}");
                return false;
            }
        }

        // ── Version detection ─────────────────────────────────────────────────

        private static int DetectVersion(string json)
        {
            int idx = json.IndexOf(VersionField, StringComparison.Ordinal);
            if (idx < 0) return 1; // no field → treat as v1

            int colon = json.IndexOf(':', idx + VersionField.Length);
            if (colon < 0) return 1;

            int start = colon + 1;
            while (start < json.Length && (json[start] == ' ' || json[start] == '\t' ||
                                           json[start] == '\r' || json[start] == '\n'))
                start++;

            int end = start;
            while (end < json.Length && char.IsDigit(json[end])) end++;

            if (end > start && int.TryParse(json.Substring(start, end - start), out int ver))
                return ver;

            return 1;
        }

        // ── Sequential migration ───────────────────────────────────────────────

        private bool MigrateFile(string key, string path,
                                  string json, int fromVersion,
                                  out string resultJson)
        {
            resultJson = json;
            int currentVer = fromVersion;

            while (currentVer < CurrentVersion)
            {
                var step = _steps.Find(s => s.FromVersion == currentVer);
                if (step == null)
                {
                    Debug.LogWarning(
                        $"[SaveDataMigrator] No migration step from v{currentVer} for '{key}'.");
                    return false;
                }

                string migrated = null;
                try { migrated = step.Migrate(resultJson); }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"[SaveDataMigrator] Step v{step.FromVersion}→v{step.ToVersion} threw: {ex.Message}");
                    return false;
                }

                if (migrated == null) return false;

                resultJson = migrated;
                currentVer = step.ToVersion;
            }

            return true;
        }

        // ── Rollback ──────────────────────────────────────────────────────────

        private static void Rollback(string targetPath, string backupPath)
        {
            if (!File.Exists(backupPath)) return;
            try { File.Copy(backupPath, targetPath, overwrite: true); }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveDataMigrator] Rollback failed: {ex.Message}");
            }
        }
    }
}
