using System;
using UnityEngine;

namespace SWEF.Social
{
    /// <summary>
    /// Static utility class for native social sharing.
    /// On iOS/Android it stubs out the native share-sheet interface;
    /// in the Unity Editor it copies the path to the system clipboard.
    /// </summary>
    public static class SocialShareController
    {
        /// <summary>Fired after a share action completes, passing the platform name.</summary>
        public static event Action<string> OnShareCompleted;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Shares an image file along with an optional caption using the native
        /// share-sheet on mobile, or falls back to clipboard copy in the Editor.
        /// </summary>
        public static void ShareImage(string imagePath, string caption)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                Debug.LogWarning("[SWEF] SocialShareController: no image path provided.");
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            ShareViaIOSNative(imagePath, caption);
#elif UNITY_ANDROID && !UNITY_EDITOR
            ShareViaAndroidNative(imagePath, caption);
#else
            ShareEditorFallback(imagePath, caption);
#endif
        }

        /// <summary>
        /// Generates and shares a deep-link URL for a flight replay.
        /// The link format matches <c>DeepLinkHandler</c>:
        /// <c>swef://replay?id={replayId}</c>.
        /// </summary>
        public static void ShareFlightReplay(string replayId, string caption)
        {
            string deepLink = $"swef://replay?id={replayId}";
            string shareText = string.IsNullOrEmpty(caption)
                ? deepLink
                : $"{caption}\n{deepLink}";
            CopyToClipboard(shareText);
            Debug.Log($"[SWEF] SocialShareController: replay deep link copied — {deepLink}");
            OnShareCompleted?.Invoke("Clipboard");
        }

        /// <summary>Copies <paramref name="text"/> to the system clipboard.</summary>
        public static void CopyToClipboard(string text)
        {
            GUIUtility.systemCopyBuffer = text;
            Debug.Log($"[SWEF] SocialShareController: copied to clipboard — {text}");
        }

        // ── Platform implementations ──────────────────────────────────────────

#if UNITY_IOS && !UNITY_EDITOR
        /// <summary>
        /// iOS stub — invokes <c>UIActivityViewController</c> via the native plugin
        /// interface. Replace with actual plugin call when integrating a native plugin.
        /// </summary>
        private static void ShareViaIOSNative(string imagePath, string caption)
        {
            // Stub: real implementation calls a native iOS plugin, e.g.:
            // NativeShare.ShareImage(imagePath, caption);
            Debug.Log($"[SWEF] SocialShareController [iOS stub]: sharing {imagePath}");
            OnShareCompleted?.Invoke("iOS");
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Android stub — triggers <c>Intent.ACTION_SEND</c> via the native plugin
        /// interface. Replace with actual plugin call when integrating a native plugin.
        /// </summary>
        private static void ShareViaAndroidNative(string imagePath, string caption)
        {
            // Stub: real implementation calls a native Android plugin, e.g.:
            // using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND")) { ... }
            Debug.Log($"[SWEF] SocialShareController [Android stub]: sharing {imagePath}");
            OnShareCompleted?.Invoke("Android");
        }
#endif

        /// <summary>Editor / unsupported-platform fallback: copies the path to clipboard.</summary>
        private static void ShareEditorFallback(string imagePath, string caption)
        {
            string shareText = string.IsNullOrEmpty(caption)
                ? imagePath
                : $"{caption}\n{imagePath}";
            CopyToClipboard(shareText);
            Debug.Log($"[SWEF] SocialShareController [Editor]: path copied — {imagePath}");
            OnShareCompleted?.Invoke("Editor");
        }
    }
}
