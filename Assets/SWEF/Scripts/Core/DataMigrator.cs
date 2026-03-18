using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Core
{
    /// <summary>
    /// One-time migration from legacy PlayerPrefs keys to the JSON save managed by <see cref="SaveManager"/>.
    /// Sets the <c>SWEF_DataMigrated_v1</c> flag in PlayerPrefs on success so the migration only runs once.
    /// This class is idempotent: calling <see cref="Migrate"/> multiple times is safe.
    /// </summary>
    public class DataMigrator : MonoBehaviour
    {
        // ── Migration flag ───────────────────────────────────────────────────
        private const string MigratedKey = "SWEF_DataMigrated_v1";

        // ── Known legacy PlayerPrefs keys ────────────────────────────────────
        private static readonly List<string> FloatKeys = new List<string>
        {
            "SWEF_MasterVolume",
            "SWEF_SfxVolume",
            "SWEF_TouchSensitivity",
            "SWEF_MaxSpeed",
        };

        private static readonly List<string> IntKeys = new List<string>
        {
            "SWEF_ComfortMode",
            "SWEF_QualityPreset",
            "SWEF_CameraMode",
            "SWEF_TutorialCompleted",
            "SWEF_MiniMapVisible",
        };

        private static readonly List<string> StringKeys = new List<string>
        {
            "SWEF_MiniMapPosition",
            "SWEF_Favorites",
        };

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Start()
        {
            Migrate();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Runs the PlayerPrefs → JSON migration if it has not already been performed.
        /// </summary>
        public void Migrate()
        {
            if (PlayerPrefs.GetInt(MigratedKey, 0) == 1)
            {
                Debug.Log("[SWEF] DataMigrator: migration already completed, skipping.");
                return;
            }

            var save = FindFirstObjectByType<SaveManager>();
            if (save == null)
            {
                Debug.LogWarning("[SWEF] DataMigrator: SaveManager not found — cannot migrate.");
                return;
            }

            int migrated = 0;

            foreach (string key in FloatKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    save.SetFloat(key, PlayerPrefs.GetFloat(key));
                    migrated++;
                }
            }

            foreach (string key in IntKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    save.SetInt(key, PlayerPrefs.GetInt(key));
                    migrated++;
                }
            }

            foreach (string key in StringKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    save.SetString(key, PlayerPrefs.GetString(key));
                    migrated++;
                }
            }

            save.Save();

            // Mark as done so we never run again
            PlayerPrefs.SetInt(MigratedKey, 1);
            PlayerPrefs.Save();

            Debug.Log($"[SWEF] DataMigrator: migrated {migrated} PlayerPrefs key(s) to JSON save.");
        }
    }
}
