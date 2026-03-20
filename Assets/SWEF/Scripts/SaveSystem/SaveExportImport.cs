using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SWEF.SaveSystem
{
    /// <summary>
    /// Phase 35 — Save export and import system.
    /// Exports a save slot to a portable file that can be transferred between
    /// devices, and imports a previously exported file into a target slot.
    /// Exported files are plain JSON wrapped in a thin envelope (no
    /// device-specific encryption) so they remain portable.
    /// </summary>
    public class SaveExportImport : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SaveExportImport Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Raised when an export completes. Arguments are slot index and output file path.</summary>
        public event Action<int, string> OnExportCompleted;

        /// <summary>Raised when an import completes. Arguments are target slot index and success flag.</summary>
        public event Action<int, bool>   OnImportCompleted;

        /// <summary>Raised when an error occurs. Argument is the error message.</summary>
        public event Action<string>      OnExportImportError;

        // ── Constants ─────────────────────────────────────────────────────────
        private const string ExportExtension = ".swefsave";
        private const string EnvelopeMagic   = "SWEF_EXPORT_V1";

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
        }

        // ── Export ────────────────────────────────────────────────────────────

        /// <summary>
        /// Exports slot <paramref name="slotIndex"/> to <paramref name="outputPath"/>.
        /// The exported file is an unencrypted (but checksummed) JSON envelope so
        /// it can be inspected and transferred between devices/platforms.
        /// </summary>
        /// <param name="slotIndex">Source slot (0–4).</param>
        /// <param name="outputPath">
        /// Full output file path.  If <c>null</c> or empty the default export
        /// directory (<see cref="Application.persistentDataPath"/>/Exports/) is used.
        /// </param>
        /// <returns>The full path where the file was written, or <c>null</c> on failure.</returns>
        public string ExportSlot(int slotIndex, string outputPath = null)
        {
            var mgr = SaveManager.Instance;
            if (mgr == null)
            {
                RaiseError("SaveManager not found.");
                return null;
            }

            var info = mgr.GetSlotInfo(slotIndex);
            if (info == null || info.isEmpty)
            {
                RaiseError($"Slot {slotIndex} is empty — nothing to export.");
                return null;
            }

            string srcPath = mgr.GetSavePath(slotIndex);
            if (!File.Exists(srcPath))
            {
                RaiseError($"Slot {slotIndex} save file not found at '{srcPath}'.");
                return null;
            }

            try
            {
                byte[] rawBytes = File.ReadAllBytes(srcPath);

                // Attempt to decompress to produce a portable plain-JSON payload.
                // (The export format is device-independent — no device-specific encryption.)
                byte[] plainBytes = rawBytes;
                try
                {
                    plainBytes = SaveManager.Decompress(rawBytes);
                }
                catch
                {
                    // If decompress fails the bytes may already be plain; continue.
                    plainBytes = rawBytes;
                }

                string checksum = SaveIntegrityChecker.ComputeChecksum(plainBytes);
                var envelope = new ExportEnvelope
                {
                    magic        = EnvelopeMagic,
                    exportedAt   = DateTime.UtcNow.ToString("o"),
                    slotIndex    = slotIndex,
                    displayName  = info.displayName ?? $"Slot {slotIndex}",
                    gameVersion  = Application.version,
                    platform     = Application.platform.ToString(),
                    checksum     = checksum,
                    saveDataB64  = Convert.ToBase64String(plainBytes)
                };

                string json = JsonUtility.ToJson(envelope, prettyPrint: true);

                string finalPath = string.IsNullOrEmpty(outputPath)
                    ? BuildDefaultExportPath(slotIndex)
                    : outputPath;

                string dir = Path.GetDirectoryName(finalPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(finalPath, json, Encoding.UTF8);

                Debug.Log($"[SWEF] SaveExportImport: slot {slotIndex} exported to '{finalPath}'.");
                OnExportCompleted?.Invoke(slotIndex, finalPath);
                return finalPath;
            }
            catch (Exception ex)
            {
                RaiseError($"Export slot {slotIndex} failed — {ex.Message}");
                return null;
            }
        }

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary>
        /// Validates an export file before importing it.
        /// Returns <c>null</c> on success, or an error string describing the problem.
        /// </summary>
        public string ValidateExportFile(string filePath)
        {
            if (!File.Exists(filePath))
                return $"File not found: '{filePath}'";

            try
            {
                string json     = File.ReadAllText(filePath, Encoding.UTF8);
                var    envelope = JsonUtility.FromJson<ExportEnvelope>(json);

                if (envelope == null)
                    return "File could not be parsed as an export envelope.";

                if (envelope.magic != EnvelopeMagic)
                    return $"Invalid magic: expected '{EnvelopeMagic}', got '{envelope.magic}'.";

                if (string.IsNullOrEmpty(envelope.saveDataB64))
                    return "Export envelope contains no save data.";

                byte[] data   = Convert.FromBase64String(envelope.saveDataB64);
                string actual = SaveIntegrityChecker.ComputeChecksum(data);

                if (!string.Equals(actual, envelope.checksum, StringComparison.OrdinalIgnoreCase))
                    return "Checksum mismatch — export file may be corrupted or tampered.";

                return null; // valid
            }
            catch (Exception ex)
            {
                return $"Validation error: {ex.Message}";
            }
        }

        // ── Import ────────────────────────────────────────────────────────────

        /// <summary>
        /// Imports an export file into <paramref name="targetSlot"/>.
        /// Calls <see cref="ValidateExportFile"/> first; the import is aborted on failure.
        /// </summary>
        /// <param name="filePath">Full path to the export file.</param>
        /// <param name="targetSlot">Destination slot index (0–4).</param>
        public void ImportToSlot(string filePath, int targetSlot)
        {
            if (targetSlot < 0 || targetSlot >= SaveSystemConstants.TotalSlots)
            {
                RaiseError($"ImportToSlot: invalid target slot {targetSlot}.");
                OnImportCompleted?.Invoke(targetSlot, false);
                return;
            }

            string validationError = ValidateExportFile(filePath);
            if (validationError != null)
            {
                RaiseError($"Import validation failed — {validationError}");
                OnImportCompleted?.Invoke(targetSlot, false);
                return;
            }

            var mgr = SaveManager.Instance;
            if (mgr == null)
            {
                RaiseError("SaveManager not found.");
                OnImportCompleted?.Invoke(targetSlot, false);
                return;
            }

            try
            {
                string json     = File.ReadAllText(filePath, Encoding.UTF8);
                var    envelope = JsonUtility.FromJson<ExportEnvelope>(json);
                byte[] data     = Convert.FromBase64String(envelope.saveDataB64);

                File.WriteAllBytes(mgr.GetSavePath(targetSlot), data);

                // Build updated slot metadata
                string checksum = SaveIntegrityChecker.ComputeChecksum(data);
                var newInfo = new SaveSlotInfo
                {
                    slotIndex              = targetSlot,
                    displayName            = $"{envelope.displayName} (imported)",
                    timestamp              = DateTime.UtcNow.ToString("o"),
                    playTimeSec            = 0f,
                    thumbnailPath          = "",
                    saveVersion            = SaveSystemConstants.CurrentSaveVersion,
                    checksum               = checksum,
                    creationTimestampTicks = DateTime.UtcNow.Ticks,
                    cloudSyncStatus        = CloudSyncStatus.LocalAhead,
                    isEmpty                = false
                };

                File.WriteAllText(mgr.GetMetaPath(targetSlot),
                    JsonUtility.ToJson(newInfo, prettyPrint: true));

                Debug.Log($"[SWEF] SaveExportImport: import into slot {targetSlot} succeeded.");
                OnImportCompleted?.Invoke(targetSlot, true);
            }
            catch (Exception ex)
            {
                RaiseError($"Import into slot {targetSlot} failed — {ex.Message}");
                OnImportCompleted?.Invoke(targetSlot, false);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string BuildDefaultExportPath(int slotIndex)
        {
            string exportDir = Path.Combine(Application.persistentDataPath, "Exports");
            if (!Directory.Exists(exportDir))
                Directory.CreateDirectory(exportDir);

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(exportDir, $"swef_slot{slotIndex}_{timestamp}{ExportExtension}");
        }

        private void RaiseError(string msg)
        {
            Debug.LogError($"[SWEF] SaveExportImport: {msg}");
            OnExportImportError?.Invoke(msg);
        }

        // ── Nested types ──────────────────────────────────────────────────────
        [Serializable]
        private class ExportEnvelope
        {
            /// <summary>Magic string identifying the export format.</summary>
            public string magic;
            /// <summary>UTC ISO-8601 timestamp of the export.</summary>
            public string exportedAt;
            /// <summary>Original slot index this save came from.</summary>
            public int    slotIndex;
            /// <summary>Human-readable save name.</summary>
            public string displayName;
            /// <summary>Application version at export time.</summary>
            public string gameVersion;
            /// <summary>Runtime platform at export time.</summary>
            public string platform;
            /// <summary>SHA-256 checksum of the Base64-decoded save bytes.</summary>
            public string checksum;
            /// <summary>Base64-encoded raw save bytes.</summary>
            public string saveDataB64;
        }
    }
}
