// LiveryImporter.cs — Phase 115: Advanced Aircraft Livery Editor
// Import livery: validate format, apply to compatible aircraft, version check.
// Namespace: SWEF.LiveryEditor

using System;
using System.IO;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Handles importing livery packages created by <see cref="LiveryExporter"/>.
    /// Validates the format version, checks aircraft compatibility, and loads the data.
    /// </summary>
    public class LiveryImporter : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        private const int SupportedFormatVersion = 1;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised after a successful import; provides the loaded livery data.</summary>
        public event Action<LiverySaveData> OnImportSuccess;

        /// <summary>Raised when an import fails; provides an error message.</summary>
        public event Action<string> OnImportFailed;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Imports a livery from a metadata JSON file and an associated texture.
        /// </summary>
        /// <param name="jsonPath">Absolute path to the livery metadata JSON file.</param>
        /// <param name="texturePath">Absolute path to the livery texture image.</param>
        /// <param name="targetAircraftId">Aircraft that will receive the livery.</param>
        public void ImportFromFiles(string jsonPath, string texturePath, string targetAircraftId)
        {
            if (!File.Exists(jsonPath))
            {
                OnImportFailed?.Invoke($"Metadata file not found: {jsonPath}");
                return;
            }
            if (!File.Exists(texturePath))
            {
                OnImportFailed?.Invoke($"Texture file not found: {texturePath}");
                return;
            }

            LiveryMetadata metadata;
            try
            {
                string json = File.ReadAllText(jsonPath);
                metadata = JsonUtility.FromJson<LiveryMetadata>(json);
            }
            catch (Exception ex)
            {
                OnImportFailed?.Invoke($"Failed to parse metadata: {ex.Message}");
                return;
            }

            if (!ValidateMetadata(metadata, targetAircraftId, out string validationError))
            {
                OnImportFailed?.Invoke(validationError);
                return;
            }

            byte[] texBytes;
            try { texBytes = File.ReadAllBytes(texturePath); }
            catch (Exception ex)
            {
                OnImportFailed?.Invoke($"Failed to read texture: {ex.Message}");
                return;
            }

            ImportFromData(metadata, texBytes);
        }

        /// <summary>
        /// Imports a livery from already-loaded bytes (for use without file system access).
        /// </summary>
        public void ImportFromData(LiveryMetadata metadata, byte[] textureBytes)
        {
            if (metadata == null)
            {
                OnImportFailed?.Invoke("Metadata is null.");
                return;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (textureBytes != null && textureBytes.Length > 0 && !tex.LoadImage(textureBytes))
            {
                OnImportFailed?.Invoke("Failed to decode livery texture.");
                return;
            }

            var livery = new LiverySaveData { Metadata = metadata };
            OnImportSuccess?.Invoke(livery);
            Debug.Log($"[SWEF] LiveryImporter: imported '{metadata.Name}'.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static bool ValidateMetadata(LiveryMetadata metadata, string aircraftId, out string error)
        {
            error = null;

            if (metadata == null)
            {
                error = "Metadata is null.";
                return false;
            }

            if (metadata.FormatVersion > SupportedFormatVersion)
            {
                error = $"Livery format version {metadata.FormatVersion} is not supported (max {SupportedFormatVersion}).";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(aircraftId) &&
                metadata.CompatibleAircraftIds != null &&
                metadata.CompatibleAircraftIds.Count > 0 &&
                !metadata.CompatibleAircraftIds.Contains(aircraftId))
            {
                error = $"Livery is not compatible with aircraft '{aircraftId}'.";
                return false;
            }

            return true;
        }
    }
}
