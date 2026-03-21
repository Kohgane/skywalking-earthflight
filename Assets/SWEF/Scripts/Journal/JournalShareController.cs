using System;
using System.IO;
using UnityEngine;
using SWEF.Localization;

namespace SWEF.Journal
{
    /// <summary>
    /// Handles export and social sharing of individual <see cref="FlightLogEntry"/> records.
    /// Follows the same pattern as <see cref="SWEF.Achievement.AchievementShareController"/>.
    /// Supports native share sheet, clipboard fallback, and RenderTexture image capture.
    /// </summary>
    public class JournalShareController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Image Capture")]
        [Tooltip("Camera used to render the flight-card composite image. Leave null to skip image capture.")]
        [SerializeField] private Camera captureCamera;

        [SerializeField] private int captureWidth  = 1024;
        [SerializeField] private int captureHeight =  512;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Shares the given <paramref name="entry"/> using the device's native share sheet,
        /// or falls back to copying formatted text to the clipboard.
        /// </summary>
        public void Share(FlightLogEntry entry)
        {
            if (entry == null) return;

            string text = BuildShareText(entry);

            if (captureCamera != null)
            {
                string imagePath = CaptureImage(entry.logId);
                if (!string.IsNullOrEmpty(imagePath))
                {
                    NativeShare(text, imagePath);
                    return;
                }
            }

            ShareTextFallback(text);
        }

        /// <summary>
        /// Returns a JSON export string of the given <paramref name="entry"/>
        /// suitable for import by another player.
        /// </summary>
        public string ExportJson(FlightLogEntry entry)
        {
            if (entry == null) return string.Empty;
            return JsonUtility.ToJson(entry, true);
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private static string BuildShareText(FlightLogEntry entry)
        {
            var loc = LocalizationManager.Instance;

            float distKm  = entry.distanceKm;
            float durMin  = entry.durationSeconds / 60f;
            float altM    = entry.maxAltitudeM;

            string body;
            if (loc != null)
            {
                string fmt = loc.GetText("journal_share_text");
                if (!string.IsNullOrEmpty(fmt))
                {
                    body = string.Format(fmt, distKm, durMin, altM);
                }
                else
                {
                    body = $"I flew {distKm:F1} km in {durMin:F0} min, reaching {altM:F0} m!";
                }
            }
            else
            {
                body = $"I flew {distKm:F1} km in {durMin:F0} min, reaching {altM:F0} m!";
            }

            return $"{body} #SWEF #Skywalking";
        }

        private string CaptureImage(string entryId)
        {
            try
            {
                var rt = new RenderTexture(captureWidth, captureHeight, 24);
                captureCamera.targetTexture = rt;
                captureCamera.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
                tex.Apply();

                captureCamera.targetTexture = null;
                RenderTexture.active        = null;
                Destroy(rt);

                string dir  = Path.Combine(Application.persistentDataPath, "JournalShares");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, $"flight_{entryId}.png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Destroy(tex);
                return path;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] JournalShareController: Image capture failed — {ex.Message}");
                return null;
            }
        }

        private static void NativeShare(string text, string imagePath)
        {
#if UNITY_IOS || UNITY_ANDROID
            // Platform-specific native share.
            // On real devices a plugin (e.g. NativeShare) would be invoked here.
            // For now fall back to clipboard with a log message.
            Debug.Log($"[SWEF] JournalShareController: NativeShare — '{text}' / image: {imagePath}");
            ShareTextFallback(text);
#else
            Debug.Log($"[SWEF] JournalShareController: NativeShare (editor) — '{text}'");
            ShareTextFallback(text);
#endif
        }

        private static void ShareTextFallback(string text)
        {
            GUIUtility.systemCopyBuffer = text;
            Debug.Log($"[SWEF] JournalShareController: Copied to clipboard — '{text}'");
        }
    }
}
