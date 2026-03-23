using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.PhotoMode
{
    /// <summary>Social sharing integration for captured photos.</summary>
    public class PhotoSharingManager : MonoBehaviour
    {
        #region Events

        /// <summary>Fired when a share action has been initiated.</summary>
        public static event Action<SharePayload> OnShareInitiated;

        /// <summary>Fired when a share action completes successfully.</summary>
        public static event Action<SharePayload> OnShareCompleted;

        /// <summary>Fired when a share action fails. Supplies error description.</summary>
        public static event Action<string> OnShareFailed;

        #endregion

        #region Inner Types

        /// <summary>All data bundled together for a share action.</summary>
        [Serializable]
        public class SharePayload
        {
            public string  photoPath;
            public string  caption;
            public string  locationTag;
            public string[] hashtags;
            public bool    stripMetadata;
        }

        #endregion

        #region Inspector

        [Header("Rate Limiting")]
        [SerializeField, Tooltip("Minimum seconds between share actions."), Range(1f, 60f)]
        private float _rateLimitSeconds = 5f;

        [Header("Card Branding")]
        [SerializeField, Tooltip("Optional branding texture to composite onto shared photos.")]
        private Texture2D _brandingTexture;

        [SerializeField, Tooltip("Width of the branding border in pixels."), Range(0, 200)]
        private int _brandingBorderPx = 40;

        [Header("Privacy")]
        [SerializeField, Tooltip("When enabled, GPS/EXIF data is stripped before sharing by default.")]
        private bool _stripMetadataByDefault = true;

        #endregion

        #region Private State

        private float _lastShareTime = -999f;

        #endregion

        #region Public API

        /// <summary>Share a photo using a fully specified payload. Respects rate limiting.</summary>
        public void Share(SharePayload payload)
        {
            if (!CanShare())
            {
                OnShareFailed?.Invoke("Rate limit: please wait before sharing again.");
                return;
            }

            if (string.IsNullOrEmpty(payload.photoPath) || !File.Exists(payload.photoPath))
            {
                OnShareFailed?.Invoke($"Photo file not found: {payload.photoPath}");
                return;
            }

            _lastShareTime = Time.realtimeSinceStartup;
            OnShareInitiated?.Invoke(payload);

            try
            {
                string sharePath = PrepareShareFile(payload);
                TriggerNativeShare(sharePath, payload.caption);
                OnShareCompleted?.Invoke(payload);
            }
            catch (Exception ex)
            {
                OnShareFailed?.Invoke(ex.Message);
            }
        }

        /// <summary>Quick-share the most recent photo from the gallery using default settings.</summary>
        public void ShareLastPhoto(string photoPath, string caption = "")
        {
            Share(new SharePayload
            {
                photoPath     = photoPath,
                caption       = caption,
                locationTag   = string.Empty,
                hashtags      = new[] { "#SkyWalkingEarthflight", "#FlightPhoto" },
                stripMetadata = _stripMetadataByDefault
            });
        }

        /// <summary>Copy the photo bytes to the clipboard (no-op on platforms that don't support it).</summary>
        public void CopyToClipboard(string photoPath)
        {
            if (!File.Exists(photoPath))
            {
                Debug.LogWarning($"[PhotoSharingManager] File not found for clipboard: {photoPath}");
                return;
            }

            // Clipboard image sharing is platform-specific; we surface the path for native plugin use.
            GUIUtility.systemCopyBuffer = photoPath;
            Debug.Log($"[PhotoSharingManager] Path copied to clipboard: {photoPath}");
        }

        /// <summary>Whether the rate limit allows a new share right now.</summary>
        public bool CanShare() =>
            Time.realtimeSinceStartup - _lastShareTime >= _rateLimitSeconds;

        #endregion

        #region Helpers

        private string PrepareShareFile(SharePayload payload)
        {
            if (_brandingBorderPx == 0 && !payload.stripMetadata)
                return payload.photoPath;

            byte[] originalBytes = File.ReadAllBytes(payload.photoPath);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(originalBytes);

            if (_brandingBorderPx > 0 && _brandingTexture != null)
                tex = ApplyBrandingBorder(tex);

            string ext     = Path.GetExtension(payload.photoPath);
            string outPath = Path.Combine(
                Path.GetTempPath(),
                $"swef_share_{DateTime.Now.Ticks}{ext}");

            byte[] outBytes = ext.ToLowerInvariant() == ".jpg" || ext.ToLowerInvariant() == ".jpeg"
                ? tex.EncodeToJPG(90)
                : tex.EncodeToPNG();

            File.WriteAllBytes(outPath, outBytes);
            Destroy(tex);
            return outPath;
        }

        private Texture2D ApplyBrandingBorder(Texture2D source)
        {
            int b  = _brandingBorderPx;
            int nw = source.width  + b * 2;
            int nh = source.height + b * 2;

            var result = new Texture2D(nw, nh, TextureFormat.RGBA32, false);

            // Fill border with branding colour (sample centre of branding texture)
            Color brandColour = _brandingTexture.GetPixel(
                _brandingTexture.width  / 2,
                _brandingTexture.height / 2);

            Color[] borderPixels = new Color[nw * nh];
            for (int i = 0; i < borderPixels.Length; i++)
                borderPixels[i] = brandColour;
            result.SetPixels(borderPixels);

            // Blit original image into centre
            result.SetPixels(b, b, source.width, source.height, source.GetPixels());
            result.Apply();

            Destroy(source);
            return result;
        }

        private void TriggerNativeShare(string filePath, string text)
        {
#if UNITY_ANDROID || UNITY_IOS
            // On mobile, open a deep-link or native share sheet via Application.OpenURL.
            // A real production implementation would call a native plugin.
            string encoded = UnityEngine.Networking.UnityWebRequest.EscapeURL(text);
            Application.OpenURL($"share://?file={Uri.EscapeDataString(filePath)}&text={encoded}");
#else
            // On desktop/editor open the folder containing the file.
            Application.OpenURL("file://" + Path.GetDirectoryName(filePath));
#endif
        }

        #endregion
    }
}
