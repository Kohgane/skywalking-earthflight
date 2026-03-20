using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SaveSystem
{
    /// <summary>
    /// Phase 35 — Save-format version migration system.
    /// When <see cref="SaveManager"/> detects that a loaded save file's format
    /// version is older than <see cref="SaveSystemConstants.CurrentSaveVersion"/>,
    /// it calls <see cref="Migrate"/> which runs each registered step in order.
    /// </summary>
    public class SaveMigrationSystem : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SaveMigrationSystem Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Raised when migration completes. Arguments are (fromVersion, toVersion).</summary>
        public event Action<int, int> OnMigrationCompleted;

        /// <summary>Raised when a migration step encounters an error.</summary>
        public event Action<int, string> OnMigrationError;

        // ── Migration steps registry ──────────────────────────────────────────
        // Each step is keyed by the *source* version it upgrades FROM.
        // e.g. steps[1] upgrades a v1 save to v2.
        private readonly Dictionary<int, Action<SaveFile>> _steps =
            new Dictionary<int, Action<SaveFile>>();

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

            RegisterBuiltInSteps();
        }

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>
        /// Migrates <paramref name="saveFile"/> from <paramref name="fromVersion"/> to
        /// <paramref name="toVersion"/> by running each registered migration step in order.
        /// Missing steps are skipped with a warning (forward-compatible).
        /// </summary>
        public void Migrate(SaveFile saveFile, int fromVersion, int toVersion)
        {
            if (saveFile == null)
            {
                Debug.LogError("[SWEF] SaveMigrationSystem: Migrate() called with null SaveFile.");
                return;
            }

            if (fromVersion >= toVersion)
            {
                Debug.Log($"[SWEF] SaveMigrationSystem: save is already at version {fromVersion}, no migration needed.");
                return;
            }

            Debug.Log($"[SWEF] SaveMigrationSystem: migrating v{fromVersion} → v{toVersion}.");

            for (int v = fromVersion; v < toVersion; v++)
            {
                if (_steps.TryGetValue(v, out var step))
                {
                    try
                    {
                        step(saveFile);
                        Debug.Log($"[SWEF] SaveMigrationSystem: step v{v}→v{v + 1} applied.");
                    }
                    catch (Exception ex)
                    {
                        string msg = $"step v{v}→v{v + 1} failed — {ex.Message}";
                        Debug.LogError($"[SWEF] SaveMigrationSystem: {msg}");
                        OnMigrationError?.Invoke(v, msg);
                        // Abort chain on failure to avoid cascading bad state
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning($"[SWEF] SaveMigrationSystem: no step registered for v{v}→v{v + 1}; skipping.");
                }
            }

            saveFile.header.formatVersion = toVersion;
            OnMigrationCompleted?.Invoke(fromVersion, toVersion);
            Debug.Log($"[SWEF] SaveMigrationSystem: migration to v{toVersion} complete.");
        }

        /// <summary>
        /// Registers a custom migration step for saves at <paramref name="fromVersion"/>.
        /// The step should mutate the <see cref="SaveFile"/> in-place to be valid at
        /// <c>fromVersion + 1</c>.
        /// </summary>
        public void RegisterStep(int fromVersion, Action<SaveFile> step)
        {
            if (step == null) return;
            _steps[fromVersion] = step;
            Debug.Log($"[SWEF] SaveMigrationSystem: registered step for v{fromVersion}→v{fromVersion + 1}.");
        }

        /// <summary>Returns <c>true</c> if a step is registered for <paramref name="fromVersion"/>.</summary>
        public bool HasStep(int fromVersion) => _steps.ContainsKey(fromVersion);

        // ── Built-in migration steps ──────────────────────────────────────────
        private void RegisterBuiltInSteps()
        {
            // v1 → v2 (placeholder for future schema changes)
            RegisterStep(1, MigrateV1ToV2);
        }

        /// <summary>
        /// Placeholder migration from save format v1 to v2.
        /// Extend this method when the v2 schema is finalised.
        /// </summary>
        private static void MigrateV1ToV2(SaveFile save)
        {
            // Example: ensure the progress object exists (it was added in v2)
            if (save.progress == null)
                save.progress = new PlayerProgressData();

            // Carry forward legacy progress data stored in the Core/SaveManager's key-value
            // payload under well-known keys, if present.
            const string keyFlights      = "subsystem.legacy.totalFlights";
            const string keyFlightTime   = "subsystem.legacy.totalFlightTimeSec";
            const string keyDistance     = "subsystem.legacy.totalDistanceKm";
            const string keyAlt          = "subsystem.legacy.allTimeMaxAltitudeKm";

            if (save.payload.Contains(keyFlights) &&
                int.TryParse(save.payload.Get(keyFlights), out int flights))
                save.progress.flightsCompleted = flights;

            if (save.payload.Contains(keyFlightTime) &&
                float.TryParse(save.payload.Get(keyFlightTime),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float ft))
                save.progress.totalFlightTimeSec = ft;

            if (save.payload.Contains(keyDistance) &&
                float.TryParse(save.payload.Get(keyDistance),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float dist))
                save.progress.totalDistanceKm = dist;

            if (save.payload.Contains(keyAlt) &&
                float.TryParse(save.payload.Get(keyAlt),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float alt))
                save.progress.furthestAltitudeKm = alt;
        }
    }
}
