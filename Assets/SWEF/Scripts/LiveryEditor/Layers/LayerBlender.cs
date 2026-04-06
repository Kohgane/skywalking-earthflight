// LayerBlender.cs — Phase 115: Advanced Aircraft Livery Editor
// Real-time layer compositing: blend mode calculations, opacity mixing, mask application.
// Namespace: SWEF.LiveryEditor

using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Static utility class for compositing one <see cref="Texture2D"/>
    /// layer onto another using standard blend modes and per-pixel opacity mixing.
    /// Supports an optional mask texture for non-destructive masking.
    /// </summary>
    public static class LayerBlender
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Composites <paramref name="source"/> onto <paramref name="destination"/>
        /// using the specified blend mode and opacity.  Modifies destination in place
        /// and calls <c>Apply()</c>.
        /// </summary>
        /// <param name="destination">Base texture that will be modified.</param>
        /// <param name="source">Overlay texture composited on top.</param>
        /// <param name="opacity">Layer opacity (0–1).</param>
        /// <param name="mode">Blend mode.</param>
        /// <param name="mask">Optional mask; white = show source, black = hide source.</param>
        public static void BlendOnto(Texture2D destination, Texture2D source, float opacity, BlendMode mode, Texture2D mask = null)
        {
            if (destination == null || source == null) return;
            if (destination.width != source.width || destination.height != source.height)
            {
                Debug.LogWarning("[SWEF] LayerBlender: texture size mismatch — blending skipped.");
                return;
            }

            Color[] dst  = destination.GetPixels();
            Color[] src  = source.GetPixels();
            Color[] msk  = mask != null ? mask.GetPixels() : null;

            for (int i = 0; i < dst.Length; i++)
            {
                float maskAlpha = msk != null ? msk[i].r : 1f;
                float alpha     = src[i].a * opacity * maskAlpha;
                Color blended   = Blend(dst[i], src[i], mode);
                dst[i]          = Color.Lerp(dst[i], blended, alpha);
            }

            destination.SetPixels(dst);
            destination.Apply();
        }

        /// <summary>
        /// Applies a mask to a texture in-place: multiplies each pixel's alpha
        /// by the mask's red channel.
        /// </summary>
        public static void ApplyMask(Texture2D texture, Texture2D mask)
        {
            if (texture == null || mask == null) return;
            Color[] pixels = texture.GetPixels();
            Color[] mskPx  = mask.GetPixels();
            int len = Mathf.Min(pixels.Length, mskPx.Length);
            for (int i = 0; i < len; i++)
            {
                Color p = pixels[i];
                p.a      *= mskPx[i].r;
                pixels[i] = p;
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }

        // ── Blend mode implementations ────────────────────────────────────────────

        /// <summary>Applies the requested blend formula to a single pixel pair.</summary>
        public static Color Blend(Color dst, Color src, BlendMode mode)
        {
            switch (mode)
            {
                case BlendMode.Multiply:  return BlendMultiply(dst, src);
                case BlendMode.Screen:    return BlendScreen(dst, src);
                case BlendMode.Overlay:   return BlendOverlay(dst, src);
                case BlendMode.SoftLight: return BlendSoftLight(dst, src);
                case BlendMode.Darken:    return BlendDarken(dst, src);
                case BlendMode.Lighten:   return BlendLighten(dst, src);
                case BlendMode.Add:       return BlendAdd(dst, src);
                default:                  return src; // Normal
            }
        }

        private static Color BlendMultiply(Color dst, Color src) =>
            new Color(dst.r * src.r, dst.g * src.g, dst.b * src.b, dst.a);

        private static Color BlendScreen(Color dst, Color src) =>
            new Color(
                1f - (1f - dst.r) * (1f - src.r),
                1f - (1f - dst.g) * (1f - src.g),
                1f - (1f - dst.b) * (1f - src.b),
                dst.a);

        private static Color BlendOverlay(Color dst, Color src)
        {
            float r = dst.r < 0.5f ? 2f * dst.r * src.r : 1f - 2f * (1f - dst.r) * (1f - src.r);
            float g = dst.g < 0.5f ? 2f * dst.g * src.g : 1f - 2f * (1f - dst.g) * (1f - src.g);
            float b = dst.b < 0.5f ? 2f * dst.b * src.b : 1f - 2f * (1f - dst.b) * (1f - src.b);
            return new Color(r, g, b, dst.a);
        }

        private static Color BlendSoftLight(Color dst, Color src)
        {
            float r = (1f - 2f * src.r) * dst.r * dst.r + 2f * src.r * dst.r;
            float g = (1f - 2f * src.g) * dst.g * dst.g + 2f * src.g * dst.g;
            float b = (1f - 2f * src.b) * dst.b * dst.b + 2f * src.b * dst.b;
            return new Color(r, g, b, dst.a);
        }

        private static Color BlendDarken(Color dst, Color src) =>
            new Color(Mathf.Min(dst.r, src.r), Mathf.Min(dst.g, src.g), Mathf.Min(dst.b, src.b), dst.a);

        private static Color BlendLighten(Color dst, Color src) =>
            new Color(Mathf.Max(dst.r, src.r), Mathf.Max(dst.g, src.g), Mathf.Max(dst.b, src.b), dst.a);

        private static Color BlendAdd(Color dst, Color src) =>
            new Color(
                Mathf.Clamp01(dst.r + src.r),
                Mathf.Clamp01(dst.g + src.g),
                Mathf.Clamp01(dst.b + src.b),
                dst.a);
    }
}
