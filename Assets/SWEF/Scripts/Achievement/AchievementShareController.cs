using System.IO;
using UnityEngine;
using SWEF.Localization;

namespace SWEF.Achievement
{
    /// <summary>
    /// Handles social sharing of achievement unlocks.
    /// Captures the achievement card as a PNG and invokes the device's native share sheet.
    /// Falls back to copying achievement text to the system clipboard.
    /// </summary>
    public class AchievementShareController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Capture")]
        [Tooltip("Optional Camera used to render the achievement card thumbnail.")]
        [SerializeField] private Camera captureCamera;

        [SerializeField] private int captureWidth  = 512;
        [SerializeField] private int captureHeight = 512;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Shares the given achievement using the device's native share sheet.
        /// Captures a thumbnail if a capture camera is assigned; otherwise falls back
        /// to text-only sharing.
        /// </summary>
        public void ShareAchievement(AchievementDefinition def)
        {
            if (def == null) return;

            var loc   = LocalizationManager.Instance;
            string title = loc != null ? loc.Get(def.titleKey) : def.titleKey;

            string shareText = BuildShareText(title);

            if (captureCamera != null)
            {
                string imagePath = CaptureImage();
                if (!string.IsNullOrEmpty(imagePath))
                {
                    NativeShare(shareText, imagePath);
                    return;
                }
            }

            // Fallback — text only / clipboard.
            ShareTextFallback(shareText);
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private static string BuildShareText(string achievementTitle)
        {
            var loc = LocalizationManager.Instance;
            string fmt = loc != null
                ? loc.Get("achievement_share_text")
                : "I earned '{0}' in Skywalking: Earth Flight! 🚀✈️";

            string body = string.Format(fmt, achievementTitle);
            return $"{body} #SWEF #SkywalkingEarthFlight";
        }

        private string CaptureImage()
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

                byte[] png  = tex.EncodeToPNG();
                Destroy(tex);

                string path = Path.Combine(Application.temporaryCachePath, "achievement_share.png");
                File.WriteAllBytes(path, png);
                return path;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SWEF] AchievementShareController: Image capture failed — {ex.Message}");
                return null;
            }
        }

        private static void NativeShare(string text, string imagePath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var intentClass = new AndroidJavaClass("android.content.Intent");
                var intentObj   = new AndroidJavaObject("android.content.Intent");

                intentObj.Call<AndroidJavaObject>("setAction",  intentClass.GetStatic<string>("ACTION_SEND"));
                intentObj.Call<AndroidJavaObject>("setType",    "text/plain");
                intentObj.Call<AndroidJavaObject>("putExtra",   intentClass.GetStatic<string>("EXTRA_TEXT"), text);

                // Note: sharing image files via URI on Android 7+ requires a FileProvider,
                // which needs a native plugin. We share text-only to maintain compatibility.
                var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity");
                var chooser  = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObj, "Share Achievement");
                activity.Call("startActivity", chooser);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SWEF] Android share failed — {ex.Message}");
                ShareTextFallback(text);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // On iOS we fall back to URL-based sharing; a native plugin can extend this.
            ShareTextFallback(text);
#else
            // Editor / other platforms — copy to clipboard.
            ShareTextFallback(text);
#endif
        }

        private static void ShareTextFallback(string text)
        {
            GUIUtility.systemCopyBuffer = text;
            Debug.Log($"[SWEF] Achievement share text copied to clipboard: {text}");
        }
    }
}
