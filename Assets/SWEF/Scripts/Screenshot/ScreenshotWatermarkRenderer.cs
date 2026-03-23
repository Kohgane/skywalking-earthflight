using System;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Screenshot
{
    /// <summary>
    /// Composites watermark, branding, and overlay text onto a screenshot texture before saving.
    /// Integrates with ScreenshotController or ScreenshotFormatManager.
    /// </summary>
    public class ScreenshotWatermarkRenderer : MonoBehaviour
    {
        #region Enums

        /// <summary>Preset anchor positions for the watermark image.</summary>
        public enum WatermarkPosition
        {
            BottomRight,
            BottomLeft,
            TopRight,
            TopLeft,
            Center,
            Custom
        }

        #endregion

        #region Inspector

        [Header("Watermark Image")]
        [SerializeField, Tooltip("Texture to use as the watermark image. Null = none.")]
        private Texture2D _watermarkTexture;

        [SerializeField]
        private WatermarkPosition _position = WatermarkPosition.BottomRight;

        [SerializeField, Tooltip("Custom normalised position (0-1) used when position is Custom.")]
        private Vector2 _customNormalisedPos = new Vector2(0.95f, 0.05f);

        [SerializeField, Range(0f, 1f), Tooltip("Watermark opacity.")]
        private float _opacity = 0.75f;

        [SerializeField, Tooltip("Watermark width as fraction of the screenshot width."), Range(0.02f, 0.5f)]
        private float _relativeSizeFraction = 0.12f;

        [Header("Overlays — Timestamp")]
        [SerializeField]
        private bool _showTimestamp = true;

        [SerializeField, Tooltip("Text format for the timestamp, e.g. 'yyyy-MM-dd HH:mm'.")]
        private string _timestampFormat = "yyyy-MM-dd HH:mm";

        [Header("Overlays — Location")]
        [SerializeField]
        private bool _showLocation;

        [SerializeField, Tooltip("Location name injected at runtime.")]
        private string _locationName = string.Empty;

        [Header("Overlays — Flight Info")]
        [SerializeField]
        private bool _showFlightInfo;

        [SerializeField, Tooltip("Altitude in metres, injected at runtime.")]
        private float _altitudeMetres;

        [SerializeField, Tooltip("Speed in kph, injected at runtime.")]
        private float _speedKph;

        [SerializeField, Tooltip("Heading in degrees 0-360, injected at runtime.")]
        private float _headingDegrees;

        [Header("User Preference")]
        [SerializeField, Tooltip("Master toggle — when disabled no overlay is applied.")]
        private bool _watermarkEnabled = true;

        #endregion

        #region Public API

        /// <summary>Apply all enabled watermark/overlay compositing onto a copy of
        /// <paramref name="source"/> and return the result.
        /// The original texture is not modified.</summary>
        public Texture2D Apply(Texture2D source)
        {
            if (!_watermarkEnabled || source == null)
                return source;

            // Work on a copy
            var result = DuplicateTexture(source);

            if (_watermarkTexture != null)
                BlitWatermark(result);

            if (_showTimestamp || _showLocation || _showFlightInfo)
                BurnTextOverlays(result);

            result.Apply();
            return result;
        }

        /// <summary>Update flight info used by the overlay at runtime.</summary>
        public void SetFlightInfo(float altitudeMetres, float speedKph, float headingDegrees)
        {
            _altitudeMetres  = altitudeMetres;
            _speedKph        = speedKph;
            _headingDegrees  = headingDegrees;
        }

        /// <summary>Set the current location name shown in the overlay.</summary>
        public void SetLocationName(string name) => _locationName = name;

        #endregion

        #region Watermark Blit

        private void BlitWatermark(Texture2D target)
        {
            int wmW = Mathf.RoundToInt(target.width * _relativeSizeFraction);
            // Maintain aspect ratio
            float aspect = _watermarkTexture.width / (float)_watermarkTexture.height;
            int   wmH    = Mathf.RoundToInt(wmW / aspect);

            // Resize watermark to target dimensions
            var scaled = ScaleTexture(_watermarkTexture, wmW, wmH);

            Vector2Int anchor = CalculateAnchor(target.width, target.height, wmW, wmH);

            // Per-pixel alpha blend
            for (int y = 0; y < wmH; y++)
            {
                for (int x = 0; x < wmW; x++)
                {
                    int tx = anchor.x + x;
                    int ty = anchor.y + y;

                    if (tx < 0 || tx >= target.width || ty < 0 || ty >= target.height)
                        continue;

                    Color wm  = scaled.GetPixel(x, y);
                    Color bg  = target.GetPixel(tx, ty);
                    float a   = wm.a * _opacity;
                    target.SetPixel(tx, ty, Color.Lerp(bg, wm, a));
                }
            }

            Destroy(scaled);
        }

        private Vector2Int CalculateAnchor(int tw, int th, int wmW, int wmH)
        {
            const int margin = 16;

            switch (_position)
            {
                case WatermarkPosition.BottomLeft:
                    return new Vector2Int(margin, margin);
                case WatermarkPosition.TopLeft:
                    return new Vector2Int(margin, th - wmH - margin);
                case WatermarkPosition.TopRight:
                    return new Vector2Int(tw - wmW - margin, th - wmH - margin);
                case WatermarkPosition.Center:
                    return new Vector2Int((tw - wmW) / 2, (th - wmH) / 2);
                case WatermarkPosition.Custom:
                    return new Vector2Int(
                        Mathf.RoundToInt(_customNormalisedPos.x * tw - wmW * 0.5f),
                        Mathf.RoundToInt(_customNormalisedPos.y * th - wmH * 0.5f));
                default: // BottomRight
                    return new Vector2Int(tw - wmW - margin, margin);
            }
        }

        #endregion

        #region Text Overlay

        private void BurnTextOverlays(Texture2D target)
        {
            // We render overlay text using a temporary UI Canvas approach via a RenderTexture
            // and blit it onto the target. For robustness without a font atlas dependency,
            // we write simple pixel-bar "text blocks" as placeholders when no font is available.
            // Real production code would use a TextMeshPro label rendered off-screen.
            string line1 = _showTimestamp
                ? DateTime.Now.ToString(_timestampFormat)
                : string.Empty;

            string line2 = _showLocation && !string.IsNullOrEmpty(_locationName)
                ? _locationName
                : string.Empty;

            string line3 = _showFlightInfo
                ? $"{_altitudeMetres:F0}m  {_speedKph:F0}km/h  {_headingDegrees:F0}°"
                : string.Empty;

            // Draw a semi-transparent strip at the bottom of the image
            int stripH = 28;
            int y0     = 4;

            DrawSemiTransparentStrip(target, y0, stripH);

            // Produce simple dot-matrix-style text marks (minimal, no font dependency)
            int lineIndex = 0;
            foreach (string line in new[] { line1, line2, line3 })
            {
                if (!string.IsNullOrEmpty(line))
                {
                    DrawTextMark(target, line, 8, y0 + 4 + lineIndex * 10);
                    lineIndex++;
                }
            }
        }

        private void DrawSemiTransparentStrip(Texture2D tex, int y0, int height)
        {
            Color overlay = new Color(0f, 0f, 0f, 0.45f);
            for (int y = y0; y < y0 + height && y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    Color bg = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, Color.Lerp(bg, Color.black, overlay.a));
                }
            }
        }

        private void DrawTextMark(Texture2D tex, string text, int x0, int y0)
        {
            // Each character becomes a 5×7 white block as a minimal text indicator.
            // Full font rendering would require a texture font atlas.
            int charW = 6;
            Color c   = Color.white;

            for (int i = 0; i < text.Length; i++)
            {
                int cx = x0 + i * charW;
                if (cx + charW >= tex.width) break;

                // Draw a minimal "bar" representing each character
                for (int py = y0; py < y0 + 6 && py < tex.height; py++)
                {
                    tex.SetPixel(cx,     py, c);
                    tex.SetPixel(cx + 4, py, c);
                }
                for (int px = cx; px < cx + 5 && px < tex.width; px++)
                {
                    if (y0 < tex.height)        tex.SetPixel(px, y0, c);
                    if (y0 + 3 < tex.height)    tex.SetPixel(px, y0 + 3, c);
                    if (y0 + 6 < tex.height)    tex.SetPixel(px, y0 + 6, c);
                }
            }
        }

        #endregion

        #region Helpers

        private Texture2D DuplicateTexture(Texture2D source)
        {
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.SetPixels(source.GetPixels());
            copy.Apply();
            return copy;
        }

        private Texture2D ScaleTexture(Texture2D src, int targetW, int targetH)
        {
            var rt = new RenderTexture(targetW, targetH, 0);
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var result = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
            result.Apply();
            RenderTexture.active = null;
            rt.Release();
            Destroy(rt);
            return result;
        }

        #endregion
    }
}
