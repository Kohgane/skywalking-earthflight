// SaveFileEncryptor.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Static utility that provides AES-256-CBC encryption and decryption
    /// for SWEF JSON save files.
    ///
    /// <para>Key derivation uses a PBKDF2-like approach combining the device-unique
    /// identifier with an embedded app secret so that save files cannot be
    /// decrypted on a different device.</para>
    ///
    /// <para>If decryption fails (corrupt cipher-text, wrong key, etc.) the utility
    /// logs a warning and signals failure so the caller can invoke
    /// <see cref="SaveFileValidator.RestoreFromBackup"/>.</para>
    /// </summary>
    public static class SaveFileEncryptor
    {
        // ── Key derivation ────────────────────────────────────────────────────

        private const string AppSecret       = "SWEF_AES_APP_SECRET_v1";
        private const int    KeyIterations   = 10000;
        private const int    KeySizeBytes    = 32;   // AES-256
        private const int    IvSizeBytes     = 16;   // AES block size

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Encrypts <paramref name="plainJson"/> using AES-256-CBC.
        /// The returned string is a Base-64 encoded blob of [IV (16 bytes) | cipher-text].
        /// </summary>
        /// <param name="plainJson">UTF-8 JSON string to encrypt.</param>
        /// <returns>Base-64 encoded encrypted blob, or <c>null</c> on failure.</returns>
        public static string Encrypt(string plainJson)
        {
            if (string.IsNullOrEmpty(plainJson)) return string.Empty;

            try
            {
                byte[] key   = DeriveKey();
                byte[] plain = Encoding.UTF8.GetBytes(plainJson);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Key     = key;
                    aes.GenerateIV();

                    using (var ms        = new MemoryStream())
                    using (var encryptor = aes.CreateEncryptor())
                    using (var cs        = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        ms.Write(aes.IV, 0, IvSizeBytes); // prepend IV
                        cs.Write(plain, 0, plain.Length);
                        cs.FlushFinalBlock();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Security: Encrypt failed — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Decrypts a Base-64 encoded blob previously produced by <see cref="Encrypt"/>.
        /// </summary>
        /// <param name="cipherText">Base-64 encoded cipher-text blob.</param>
        /// <returns>
        /// Decrypted UTF-8 JSON string, or <c>null</c> if decryption fails
        /// (the caller should restore from backup in that case).
        /// </returns>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return null;

            try
            {
                byte[] blob = Convert.FromBase64String(cipherText);
                if (blob.Length <= IvSizeBytes) return null;

                byte[] iv         = new byte[IvSizeBytes];
                byte[] cipherData = new byte[blob.Length - IvSizeBytes];
                Array.Copy(blob, 0,          iv,         0, IvSizeBytes);
                Array.Copy(blob, IvSizeBytes, cipherData, 0, cipherData.Length);

                byte[] key = DeriveKey();

                using (var aes        = Aes.Create())
                using (var ms         = new MemoryStream(cipherData))
                using (var decryptor  = aes.CreateDecryptor(key, iv))
                using (var cs         = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr         = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Security: Decrypt failed — {ex.Message}. Caller should restore from backup.");
                return null;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>Derives a 256-bit AES key from the device ID and app secret.</summary>
        private static byte[] DeriveKey()
        {
            string seed = SystemInfo.deviceUniqueIdentifier + AppSecret;
            // Use PBKDF2 (Rfc2898DeriveBytes) with explicit SHA-256 and a fixed salt derived from the seed.
            byte[] salt = DeriveStaticSalt(seed);
            using (var rfc = new Rfc2898DeriveBytes(
                password:      Encoding.UTF8.GetBytes(seed),
                salt:          salt,
                iterations:    KeyIterations,
                hashAlgorithm: System.Security.Cryptography.HashAlgorithmName.SHA256))
            {
                return rfc.GetBytes(KeySizeBytes);
            }
        }

        /// <summary>Derives a deterministic 16-byte salt from a seed string via SHA-256.</summary>
        private static byte[] DeriveStaticSalt(string seed)
        {
            using (var sha = SHA256.Create())
            {
                byte[] full = sha.ComputeHash(Encoding.UTF8.GetBytes("SWEF_SALT:" + seed));
                byte[] salt = new byte[16];
                Array.Copy(full, salt, 16);
                return salt;
            }
        }
    }
}
