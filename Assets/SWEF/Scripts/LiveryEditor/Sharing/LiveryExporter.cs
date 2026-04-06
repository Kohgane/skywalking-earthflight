// LiveryExporter.cs — Phase 115: Advanced Aircraft Livery Editor
// Export livery: PNG texture sheets, metadata JSON, thumbnail generation.
// Namespace: SWEF.LiveryEditor

using System;
using System.IO;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Exports the active livery to PNG texture sheets, metadata JSON,
    /// and optional thumbnail images.
    /// </summary>
    public class LiveryExporter : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised after a successful export; provides the export directory.</summary>
        public event Action<string> OnExportComplete;

        /// <summary>Raised when an export fails; provides an error message.</summary>
        public event Action<string> OnExportFailed;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Exports the given livery and its composited texture to the specified directory.
        /// </summary>
        /// <param name="livery">Livery data to export.</param>
        /// <param name="compositedTexture">Fully merged livery texture.</param>
        /// <param name="outputDirectory">Directory path where files will be written.</param>
        /// <param name="format">Export format.</param>
        public void Export(LiverySaveData livery, Texture2D compositedTexture,
            string outputDirectory, LiveryExportFormat format = LiveryExportFormat.PNG)
        {
            if (livery == null)          { OnExportFailed?.Invoke("Livery is null."); return; }
            if (compositedTexture == null) { OnExportFailed?.Invoke("Texture is null."); return; }
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                OnExportFailed?.Invoke("Output directory is empty.");
                return;
            }

            try
            {
                Directory.CreateDirectory(outputDirectory);
                string safeId = SanitiseName(livery.Metadata.LiveryId ?? "livery");

                // Export texture sheet.
                string texPath = Path.Combine(outputDirectory, safeId + GetExtension(format));
                byte[] texBytes = format == LiveryExportFormat.JPEG
                    ? compositedTexture.EncodeToJPG(85)
                    : compositedTexture.EncodeToPNG();
                File.WriteAllBytes(texPath, texBytes);

                // Export metadata JSON.
                string jsonPath = Path.Combine(outputDirectory, safeId + ".json");
                File.WriteAllText(jsonPath, JsonUtility.ToJson(livery.Metadata, prettyPrint: true));

                // Generate and export thumbnail.
                string thumbPath = Path.Combine(outputDirectory, safeId + "_thumb.png");
                var thumb = GenerateThumbnail(compositedTexture, 256, 256);
                File.WriteAllBytes(thumbPath, thumb.EncodeToPNG());

                Debug.Log($"[SWEF] LiveryExporter: exported to '{outputDirectory}'.");
                OnExportComplete?.Invoke(outputDirectory);
            }
            catch (Exception ex)
            {
                OnExportFailed?.Invoke(ex.Message);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string GetExtension(LiveryExportFormat format) => format switch
        {
            LiveryExportFormat.JPEG       => ".jpg",
            LiveryExportFormat.SWEFLivery => ".sweflivery",
            _                             => ".png"
        };

        private static string SanitiseName(string name) =>
            string.Concat(name.Split(Path.GetInvalidFileNameChars()));

        private static Texture2D GenerateThumbnail(Texture2D source, int width, int height)
        {
            var thumb = new Texture2D(width, height, TextureFormat.RGB24, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width  - 1);
                    float v = (float)y / (height - 1);
                    thumb.SetPixel(x, y, source.GetPixelBilinear(u, v));
                }
            }
            thumb.Apply();
            return thumb;
        }
    }
}
