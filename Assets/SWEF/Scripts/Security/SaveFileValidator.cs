// SaveFileValidator.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Static utility that provides SHA-256 / HMAC-based integrity protection
    /// for all SWEF JSON persistence files.
    ///
    /// <para>Workflow per save:</para>
    /// <list type="number">
    ///   <item>Call <see cref="CreateBackup"/> before writing the new file.</item>
    ///   <item>Write the JSON payload.</item>
    ///   <item>Call <see cref="SignSaveFile"/> to append an HMAC footer.</item>
    /// </list>
    ///
    /// <para>Workflow per load:</para>
    /// <list type="number">
    ///   <item>Call <see cref="VerifySaveFile"/>; if <c>false</c> call <see cref="RestoreFromBackup"/>.</item>
    ///   <item>Alternatively call <see cref="DetectTampering"/> for a detailed <see cref="ValidationResult"/>.</item>
    /// </list>
    /// </summary>
    public static class SaveFileValidator
    {
        // ── Constants ─────────────────────────────────────────────────────────

        private const string SignatureSeparator = "\n<!-- SWEF_HMAC:";
        private const string SignatureSuffix    = " -->";
        private const string BackupExtension    = ".bak";

        // The HMAC key is derived at runtime; a static fallback is used in the
        // editor where SystemInfo fields may be empty.
        private static readonly byte[] FallbackKey =
            Encoding.UTF8.GetBytes("SWEF_SAVE_INTEGRITY_FALLBACK_KEY_v1");

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes a hex-encoded SHA-256 hash of <paramref name="jsonContent"/>.
        /// </summary>
        /// <param name="jsonContent">Raw JSON string.</param>
        /// <returns>Lower-case hex SHA-256 digest.</returns>
        public static string ComputeChecksum(string jsonContent)
        {
            if (jsonContent == null) return string.Empty;
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(jsonContent));
                return BytesToHex(bytes);
            }
        }

        /// <summary>
        /// Appends an HMAC-SHA-256 signature line to the file at <paramref name="path"/>.
        /// The file must already exist with its JSON content written.
        /// </summary>
        /// <param name="path">Absolute path to the save file.</param>
        public static void SignSaveFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SWEF] Security: SignSaveFile — file not found: {path}");
                return;
            }

            try
            {
                string content   = File.ReadAllText(path, Encoding.UTF8);
                string cleanJson = StripSignature(content);
                string hmac      = ComputeHmac(cleanJson);
                File.WriteAllText(path,
                    cleanJson + SignatureSeparator + hmac + SignatureSuffix,
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Security: SignSaveFile failed for {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies the HMAC signature stored in the file at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Absolute path to the save file.</param>
        /// <returns><c>true</c> if the file is intact and the signature matches.</returns>
        public static bool VerifySaveFile(string path)
        {
            return DetectTampering(path).isValid;
        }

        /// <summary>
        /// Inspects the file at <paramref name="path"/> and returns a detailed
        /// <see cref="ValidationResult"/> describing any tampering found.
        /// </summary>
        /// <param name="path">Absolute path to the save file.</param>
        /// <returns>Validation result with violation details when tampering is detected.</returns>
        public static ValidationResult DetectTampering(string path)
        {
            if (!File.Exists(path))
                return ValidationResult.Invalid($"Save file not found: {path}");

            try
            {
                string raw = File.ReadAllText(path, Encoding.UTF8);

                int sepIndex = raw.LastIndexOf(SignatureSeparator, StringComparison.Ordinal);
                if (sepIndex < 0)
                    return ValidationResult.Invalid("No HMAC signature found in save file.");

                string storedHmac = ExtractHmac(raw, sepIndex);
                if (string.IsNullOrEmpty(storedHmac))
                    return ValidationResult.Invalid("Malformed HMAC signature in save file.");

                string cleanJson     = raw.Substring(0, sepIndex);
                string expectedHmac  = ComputeHmac(cleanJson);

                if (!TimingSafeEquals(storedHmac, expectedHmac))
                    return ValidationResult.Invalid(
                        $"HMAC mismatch — save file may have been tampered with. Path: {path}");

                return ValidationResult.Valid();
            }
            catch (Exception ex)
            {
                return ValidationResult.Invalid($"Validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Copies the current file at <paramref name="path"/> to a <c>.bak</c> backup
        /// before it is overwritten by a new save.
        /// </summary>
        /// <param name="path">Absolute path to the save file.</param>
        public static void CreateBackup(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                string backupPath = path + BackupExtension;
                File.Copy(path, backupPath, overwrite: true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Security: CreateBackup failed for {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// Restores the <c>.bak</c> backup for the file at <paramref name="path"/>,
        /// overwriting the current (potentially corrupted) file.
        /// </summary>
        /// <param name="path">Absolute path to the save file.</param>
        /// <returns><c>true</c> if a backup was found and restored successfully.</returns>
        public static bool RestoreFromBackup(string path)
        {
            string backupPath = path + BackupExtension;
            if (!File.Exists(backupPath))
            {
                Debug.LogWarning($"[SWEF] Security: No backup found for {path}");
                return false;
            }

            try
            {
                File.Copy(backupPath, path, overwrite: true);
                Debug.LogWarning($"[SWEF] Security: Restored backup for {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Security: RestoreFromBackup failed for {path}: {ex.Message}");
                return false;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static byte[] GetHmacKey()
        {
            string seed = SystemInfo.deviceUniqueIdentifier + "SWEF_APP_SECRET_v1";
            using (var sha = SHA256.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
        }

        private static string ComputeHmac(string content)
        {
            byte[] key   = GetHmacKey();
            byte[] data  = Encoding.UTF8.GetBytes(content);
            using (var hmac = new HMACSHA256(key))
                return BytesToHex(hmac.ComputeHash(data));
        }

        private static string StripSignature(string raw)
        {
            int idx = raw.LastIndexOf(SignatureSeparator, StringComparison.Ordinal);
            return idx >= 0 ? raw.Substring(0, idx) : raw;
        }

        private static string ExtractHmac(string raw, int sepIndex)
        {
            int start = sepIndex + SignatureSeparator.Length;
            int end   = raw.IndexOf(SignatureSuffix, start, StringComparison.Ordinal);
            return end > start ? raw.Substring(start, end - start).Trim() : null;
        }

        private static bool TimingSafeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length)   return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static string BytesToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }
    }
}
