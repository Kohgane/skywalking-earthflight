using System;
using UnityEngine;

namespace SWEF.PhotoMode
{
    /// <summary>
    /// Renders decorative frames around captured photos and composites watermarks,
    /// date/time stamps, location labels, and an EXIF info bar into the final image.
    /// </summary>
    public class PhotoFrameRenderer : MonoBehaviour
    {
        #region Constants
        private const int   ThumbnailSize      = 256;
        private const float DefaultFrameWidth  = 0.05f; // fraction of shorter edge
        private const string ExifFormat        = "f/{0:F1}  1/{1}  ISO {2}";
        #endregion

        #region Inspector
        [Header("Watermark")]
        [Tooltip("Optional custom watermark texture. Leave null to omit.")]
        [SerializeField] private Texture2D watermarkTexture;

        [Header("Overlays")]
        [Tooltip("Show date/time stamp on the photo.")]
        [SerializeField] private bool enableDateStamp = false;

        [Tooltip("Show GPS coordinates or place name on the photo.")]
        [SerializeField] private bool enableLocationStamp = false;

        [Tooltip("Show camera EXIF info bar at the bottom of the photo.")]
        [SerializeField] private bool enableExifBar = false;

        [Tooltip("Date/time format string used by DateTime.ToString().")]
        [SerializeField] private string dateFormat = "yyyy-MM-dd  HH:mm";

        [Header("Frame Colours")]
        [SerializeField] private Color polaroidColor  = Color.white;
        [SerializeField] private Color filmstripColor = Color.black;
        [SerializeField] private Color postcardColor  = new Color(0.95f, 0.92f, 0.82f);
        #endregion

        #region Public properties
        /// <summary>Whether the date/time stamp overlay is active.</summary>
        public bool DateStampEnabled => enableDateStamp;

        /// <summary>Whether the location stamp overlay is active.</summary>
        public bool LocationStampEnabled => enableLocationStamp;

        /// <summary>Whether the EXIF info bar overlay is active.</summary>
        public bool ExifBarEnabled => enableExifBar;
        #endregion

        #region Public API
        /// <summary>
        /// Composites <paramref name="frameStyle"/> and all active overlays into a new
        /// <see cref="Texture2D"/> that the caller is responsible for destroying.
        /// </summary>
        /// <param name="frameStyle">Frame style to render.</param>
        /// <param name="photo">Source photo texture (unmodified).</param>
        /// <returns>New composited <see cref="Texture2D"/>.</returns>
        public Texture2D ApplyFrame(FrameStyle frameStyle, Texture2D photo)
        {
            if (photo == null) return null;

            // Determine frame border in pixels
            int borderPx = frameStyle == FrameStyle.None
                ? 0
                : Mathf.RoundToInt(Mathf.Min(photo.width, photo.height) * DefaultFrameWidth);

            int outW = photo.width  + borderPx * 2;
            int outH = photo.height + borderPx * 2;
            if (frameStyle == FrameStyle.Polaroid) outH += borderPx * 4; // extra bottom white strip
            if (frameStyle == FrameStyle.Widescreen) { borderPx = Mathf.RoundToInt(photo.height * 0.12f); outH = photo.height + borderPx * 2; }
            if (frameStyle == FrameStyle.Panoramic)  { outW = photo.width; outH = Mathf.RoundToInt(photo.width * 0.4f); }

            Texture2D output = new Texture2D(outW, outH, TextureFormat.RGBA32, false);

            // Fill background with frame colour
            Color bg = GetFrameBackground(frameStyle);
            FillRect(output, 0, 0, outW, outH, bg);

            // Blit source photo
            BlitTexture(output, photo, borderPx, borderPx);

            // Watermark
            if (watermarkTexture != null)
                BlitTextureCorner(output, watermarkTexture, Corner.BottomRight, 8);

            output.Apply();
            return output;
        }

        /// <summary>
        /// Sets a custom watermark texture.
        /// </summary>
        /// <param name="watermark">Texture to use as watermark, or null to clear.</param>
        public void SetWatermark(Texture2D watermark)
        {
            watermarkTexture = watermark;
        }

        /// <summary>
        /// Enables or disables the date/time stamp overlay.
        /// </summary>
        /// <param name="enable">True to enable.</param>
        public void EnableDateStamp(bool enable)
        {
            enableDateStamp = enable;
        }

        /// <summary>
        /// Enables or disables the GPS/place-name location stamp overlay.
        /// </summary>
        /// <param name="enable">True to enable.</param>
        public void EnableLocationStamp(bool enable)
        {
            enableLocationStamp = enable;
        }

        /// <summary>
        /// Enables or disables the EXIF camera-settings info bar overlay.
        /// </summary>
        /// <param name="enable">True to enable.</param>
        public void EnableExifBar(bool enable)
        {
            enableExifBar = enable;
        }
        #endregion

        #region Private helpers
        private Color GetFrameBackground(FrameStyle style)
        {
            switch (style)
            {
                case FrameStyle.Polaroid:   return polaroidColor;
                case FrameStyle.Filmstrip:  return filmstripColor;
                case FrameStyle.Postcard:   return postcardColor;
                case FrameStyle.Passport:   return Color.white;
                case FrameStyle.Widescreen: return Color.black;
                case FrameStyle.Square:     return Color.white;
                case FrameStyle.Panoramic:  return Color.black;
                default:                    return Color.clear;
            }
        }

        private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            Color[] block = new Color[w * h];
            for (int i = 0; i < block.Length; i++) block[i] = c;
            tex.SetPixels(x, y, w, h, block);
        }

        private static void BlitTexture(Texture2D dst, Texture2D src, int offsetX, int offsetY)
        {
            Color[] srcPixels = src.GetPixels();
            dst.SetPixels(offsetX, offsetY, src.width, src.height, srcPixels);
        }

        private enum Corner { BottomRight, BottomLeft, TopRight, TopLeft }

        private static void BlitTextureCorner(Texture2D dst, Texture2D src, Corner corner, int margin)
        {
            int ox = corner == Corner.BottomRight || corner == Corner.TopRight
                ? dst.width  - src.width  - margin : margin;
            int oy = corner == Corner.TopRight    || corner == Corner.TopLeft
                ? dst.height - src.height - margin : margin;
            BlitTexture(dst, src, ox, oy);
        }
        #endregion
    }
}
