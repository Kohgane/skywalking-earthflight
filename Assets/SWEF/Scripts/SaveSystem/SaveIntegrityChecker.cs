using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SWEF.SaveSystem
{
    /// <summary>
    /// Phase 35 — Save integrity checker.
    /// Provides SHA-256 checksum generation and verification for save blobs,
    /// and can report on or quarantine corrupted slots.
    /// </summary>
    public class SaveIntegrityChecker : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SaveIntegrityChecker Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Raised when a corrupted slot is detected. Argument is the slot index.</summary>
        public event Action<int, string> OnCorruptionDetected;

        /// <summary>Raised when a full integrity scan completes. Arguments are total/corrupted slot counts.</summary>
        public event Action<int, int>    OnScanCompleted;

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

        // ── Static helpers (used by SaveManager without requiring a scene instance) ──

        /// <summary>
        /// Computes a SHA-256 hex digest of <paramref name="data"/>.
        /// Lower-case, no separators (64 chars).
        /// </summary>
        public static string ComputeChecksum(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(data);
                var    sb   = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// Computes a SHA-256 hex digest of UTF-8-encoded <paramref name="text"/>.
        /// </summary>
        public static string ComputeChecksumFromString(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return ComputeChecksum(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Verifies that the checksum stored in <paramref name="slotInfo"/> matches
        /// the actual contents of the save file on disk.
        /// Returns <c>true</c> when the slot is intact (or empty/no checksum to check).
        /// </summary>
        public bool VerifySlot(int slotIndex, SaveSlotInfo slotInfo)
        {
            if (slotInfo == null || slotInfo.isEmpty)
                return true;

            if (string.IsNullOrEmpty(slotInfo.checksum))
                return true; // legacy slot without checksum — pass through

            var mgr = SaveManager.Instance;
            if (mgr == null) return true;

            string path = mgr.GetSavePath(slotIndex);
            if (!File.Exists(path)) return false;

            try
            {
                byte[] bytes  = File.ReadAllBytes(path);
                string actual = ComputeChecksum(bytes);
                return string.Equals(actual, slotInfo.checksum, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] SaveIntegrityChecker: error reading slot {slotIndex} — {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks every populated slot and returns <c>true</c> if any are corrupt.
        /// Fires <see cref="OnCorruptionDetected"/> for each bad slot and
        /// <see cref="OnScanCompleted"/> when the scan finishes.
        /// </summary>
        public bool ScanAllSlots()
        {
            var mgr = SaveManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("[SWEF] SaveIntegrityChecker: SaveManager not found.");
                return false;
            }

            var infos = mgr.GetAllSlotInfos();
            int total     = 0;
            int corrupted = 0;

            for (int i = 0; i < SaveSystemConstants.TotalSlots; i++)
            {
                var info = infos[i];
                if (info == null || info.isEmpty) continue;

                total++;
                if (!VerifySlot(i, info))
                {
                    corrupted++;
                    string reason = $"checksum mismatch for slot {i}";
                    Debug.LogWarning($"[SWEF] SaveIntegrityChecker: {reason}");
                    OnCorruptionDetected?.Invoke(i, reason);
                }
            }

            Debug.Log($"[SWEF] SaveIntegrityChecker: scan complete — {corrupted}/{total} slot(s) corrupted.");
            OnScanCompleted?.Invoke(total, corrupted);
            return corrupted > 0;
        }

        /// <summary>
        /// Deletes the save blob for <paramref name="slotIndex"/> if its checksum is invalid,
        /// preventing a bad load from overwriting good runtime state.
        /// Returns <c>true</c> if the slot was quarantined.
        /// </summary>
        public bool QuarantineIfCorrupted(int slotIndex)
        {
            var mgr = SaveManager.Instance;
            if (mgr == null) return false;

            var info = mgr.GetSlotInfo(slotIndex);
            if (VerifySlot(slotIndex, info)) return false;

            // Delete the save file but keep the metadata so the UI can show "corrupted"
            string path = mgr.GetSavePath(slotIndex);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    Debug.LogWarning($"[SWEF] SaveIntegrityChecker: slot {slotIndex} quarantined (corrupt save deleted).");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SWEF] SaveIntegrityChecker: quarantine failed for slot {slotIndex} — {ex.Message}");
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a short human-readable health summary of all save slots.
        /// </summary>
        public string GetHealthReport()
        {
            var mgr = SaveManager.Instance;
            if (mgr == null) return "[SWEF] SaveIntegrityChecker: SaveManager not found.";

            var sb    = new StringBuilder();
            var infos = mgr.GetAllSlotInfos();
            sb.AppendLine("=== Save Integrity Report ===");

            for (int i = 0; i < SaveSystemConstants.TotalSlots; i++)
            {
                var info = infos[i];
                if (info == null || info.isEmpty)
                {
                    sb.AppendLine($"Slot {i}: empty");
                    continue;
                }

                bool ok = VerifySlot(i, info);
                string checksumPreview = (!string.IsNullOrEmpty(info.checksum) && info.checksum.Length >= 8)
                    ? info.checksum.Substring(0, 8) + "…"
                    : info.checksum ?? "none";
                sb.AppendLine($"Slot {i} ({info.displayName}): {(ok ? "OK" : "CORRUPTED")}  checksum={checksumPreview}");
            }

            return sb.ToString();
        }
    }
}
