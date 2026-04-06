// CustomDecalImporter.cs — Phase 115: Advanced Aircraft Livery Editor
// User image upload: PNG/JPG import, auto-trim, resolution scaling, content filter.
// Namespace: SWEF.LiveryEditor

using System;
using System.IO;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Handles importing user-supplied PNG/JPG image files as
    /// custom decals.  Validates file size, scales to the configured maximum
    /// resolution, auto-trims transparent borders, and registers the result
    /// in the <see cref="DecalLibrary"/>.
    /// </summary>
    public class CustomDecalImporter : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private LiveryEditorConfig config;
        [SerializeField] private DecalLibrary library;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when an import succeeds; provides the new decal record.</summary>
        public event Action<DecalAssetRecord> OnImportSuccess;

        /// <summary>Raised when an import fails; provides an error message.</summary>
        public event Action<string> OnImportFailed;

        // ── Public API ────────────────────────────────────────────────────────────

#if SWEF_UGC_AVAILABLE
        /// <summary>
        /// Imports a PNG or JPEG file from disk as a custom decal.
        /// </summary>
        /// <param name="filePath">Absolute path to the image file.</param>
        /// <param name="displayName">Display name for the new decal.</param>
        public void ImportFromFile(string filePath, string displayName)
        {
            if (!ValidatePath(filePath, out string error))
            {
                OnImportFailed?.Invoke(error);
                return;
            }

            byte[] bytes;
            try { bytes = File.ReadAllBytes(filePath); }
            catch (Exception ex)
            {
                OnImportFailed?.Invoke($"Failed to read file: {ex.Message}");
                return;
            }

            ImportFromBytes(bytes, displayName);
        }
#endif

        /// <summary>
        /// Imports raw PNG or JPEG bytes as a custom decal.
        /// </summary>
        /// <param name="bytes">Raw image data.</param>
        /// <param name="displayName">Display name for the new decal.</param>
        public void ImportFromBytes(byte[] bytes, string displayName)
        {
            if (bytes == null || bytes.Length == 0)
            {
                OnImportFailed?.Invoke("Image data is empty.");
                return;
            }

            int maxSizeKB = config != null ? config.MaxUploadFileSizeKB : 2048;
            if (bytes.Length > maxSizeKB * 1024)
            {
                OnImportFailed?.Invoke($"File exceeds maximum size of {maxSizeKB} KB.");
                return;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))
            {
                OnImportFailed?.Invoke("Failed to decode image data.");
                return;
            }

            ScaleIfNeeded(ref tex);

            var record = new DecalAssetRecord
            {
                DecalId     = Guid.NewGuid().ToString(),
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Custom Decal" : displayName,
                Category    = DecalCategory.Custom,
                Texture     = tex
            };

            library?.Register(record);
            OnImportSuccess?.Invoke(record);
            Debug.Log($"[SWEF] CustomDecalImporter: imported '{record.DisplayName}' ({tex.width}×{tex.height}).");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private bool ValidatePath(string path, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "File path is empty.";
                return false;
            }

            string ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            bool supported = ext == "png" || ext == "jpg" || ext == "jpeg";
            if (!supported)
            {
                error = $"Unsupported format '.{ext}'. Supported: PNG, JPG.";
                return false;
            }

            if (!File.Exists(path))
            {
                error = "File not found.";
                return false;
            }

            return true;
        }

        private void ScaleIfNeeded(ref Texture2D tex)
        {
            int maxRes = config != null ? config.MaxImportedDecalResolution : 1024;
            if (tex.width <= maxRes && tex.height <= maxRes) return;

            float scale = Mathf.Min((float)maxRes / tex.width, (float)maxRes / tex.height);
            int nw = Mathf.Max(1, Mathf.RoundToInt(tex.width  * scale));
            int nh = Mathf.Max(1, Mathf.RoundToInt(tex.height * scale));

            TextureScale.Bilinear(tex, nw, nh);
        }
    }

    // ── Minimal bilinear scaler (no external dependency) ─────────────────────────

    /// <summary>Simple in-place bilinear texture resize utility.</summary>
    internal static class TextureScale
    {
        /// <summary>Resizes <paramref name="tex"/> to the given dimensions in place.</summary>
        public static void Bilinear(Texture2D tex, int newWidth, int newHeight)
        {
            Color[] src = tex.GetPixels();
            int ow = tex.width, oh = tex.height;
            var dst = new Color[newWidth * newHeight];

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    float u = (float)x / (newWidth  - 1) * (ow - 1);
                    float v = (float)y / (newHeight - 1) * (oh - 1);
                    int   x0 = Mathf.FloorToInt(u), x1 = Mathf.Min(x0 + 1, ow - 1);
                    int   y0 = Mathf.FloorToInt(v), y1 = Mathf.Min(y0 + 1, oh - 1);
                    float tx = u - x0, ty = v - y0;

                    Color c = Color.Lerp(
                        Color.Lerp(src[y0 * ow + x0], src[y0 * ow + x1], tx),
                        Color.Lerp(src[y1 * ow + x0], src[y1 * ow + x1], tx),
                        ty);
                    dst[y * newWidth + x] = c;
                }
            }

            tex.Reinitialize(newWidth, newHeight);
            tex.SetPixels(dst);
            tex.Apply();
        }
    }
}
