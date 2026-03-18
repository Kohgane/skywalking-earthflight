using UnityEngine;
using SWEF.Core;
using SWEF.Flight;

namespace SWEF.Social
{
    /// <summary>
    /// Generates shareable content from the current flight session — deep link URLs,
    /// descriptive text, and screenshot attachments. Integrates with the native share
    /// sheet on mobile platforms and falls back to clipboard on unsupported platforms.
    /// Lives in the World scene (no DontDestroyOnLoad).
    /// </summary>
    public class ShareManager : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AltitudeController altitudeSource;
        [SerializeField] private Screenshot.ScreenshotController screenshotSource; // nullable

        /// <summary>Singleton instance — valid for the lifetime of the World scene.</summary>
        public static ShareManager Instance { get; private set; }

        /// <summary>Fired after a share action completes, passing the shared text.</summary>
        public event System.Action<string> OnShareCompleted;

        private void Awake()
        {
            Instance = this;

            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();

            if (screenshotSource == null)
                screenshotSource = FindFirstObjectByType<Screenshot.ScreenshotController>();
        }

        /// <summary>
        /// Builds a deep-link URL encoding the current GPS position.
        /// </summary>
        /// <returns>A <c>swef://teleport</c> URL with lat/lon from <see cref="SWEFSession"/>.</returns>
        public string GenerateDeepLink()
        {
            return $"swef://teleport?lat={SWEFSession.Lat:F6}&lon={SWEFSession.Lon:F6}&name=SharedLocation";
        }

        /// <summary>
        /// Composes a human-readable share message combining the current altitude
        /// and a deep link so recipients can jump straight to the same location.
        /// </summary>
        /// <returns>A share-ready string.</returns>
        public string GenerateShareText()
        {
            float altitude = altitudeSource != null ? altitudeSource.CurrentAltitudeMeters : 0f;
            string deepLink = GenerateDeepLink();
            return $"🚀 Flying at {altitude:N0}m above Earth! Come join me! {deepLink}";
        }

        /// <summary>
        /// Shares the given text using the native share sheet on mobile, or copies
        /// it to the clipboard as a fallback on unsupported platforms.
        /// </summary>
        /// <param name="text">The text to share.</param>
        public void ShareText(string text)
        {
#if UNITY_ANDROID
            try
            {
                using var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", "android.intent.action.SEND");
                intent.Call<AndroidJavaObject>("setType", "text/plain");
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text);

                using var chooser = new AndroidJavaClass("android.content.Intent")
                    .CallStatic<AndroidJavaObject>("createChooser", intent, "Share via");

                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("startActivity", chooser);

                Debug.Log("[SWEF] ShareManager: Android share intent fired.");
                OnShareCompleted?.Invoke(text);
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SWEF] ShareManager: Android share failed — {ex.Message}. Falling back to clipboard.");
            }
#elif UNITY_IOS
            // iOS share sheet would be implemented via a native plugin (e.g. NativeShare).
            // Placeholder for future integration:
            Debug.Log("[SWEF] ShareManager: iOS share not yet implemented — falling back to clipboard.");
#endif
            // Clipboard fallback for editor, standalone, and unsupported mobile platforms.
            GUIUtility.systemCopyBuffer = text;
            Debug.Log("[SWEF] Share text copied to clipboard.");
            OnShareCompleted?.Invoke(text);
        }

        /// <summary>
        /// Composes share text and includes the latest screenshot path if one is
        /// available, then calls <see cref="ShareText"/>.
        /// </summary>
        public void ShareWithScreenshot()
        {
            string text = GenerateShareText();

            if (screenshotSource != null)
                Debug.Log("[SWEF] ShareManager: sharing with screenshot.");
            else
                Debug.Log("[SWEF] ShareManager: no screenshot source — sharing text only.");

            ShareText(text);
        }

        // Phase 17 — Replay sharing
        /// <summary>
        /// Convenience wrapper for sharing replay content via the native share sheet.
        /// </summary>
        /// <param name="replayText">Pre-formatted replay share text or deep link.</param>
        public void ShareReplayText(string replayText)
        {
            ShareText(replayText);
            Debug.Log("[SWEF] Replay shared via ShareManager");
        }
    }
}
