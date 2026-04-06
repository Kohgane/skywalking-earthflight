// HeatmapRenderer.cs — Phase 116: Flight Analytics Dashboard
// Heatmap visualization: colour gradient overlay, adjustable opacity and resolution.
// Namespace: SWEF.FlightAnalytics

using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Renders a <see cref="HeatmapData"/> onto a <see cref="Texture2D"/>
    /// using a configurable colour gradient, suitable for UI RawImage or material overlay.
    /// </summary>
    public class HeatmapRenderer : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Colour Gradient")]
        [Tooltip("Low-density colour (cool end of the gradient).")]
        [SerializeField] private Color lowColor  = new Color(0f, 0f, 1f, 0f);    // transparent blue
        [Tooltip("Mid-density colour.")]
        [SerializeField] private Color midColor  = new Color(0f, 1f, 0f, 0.6f);  // green
        [Tooltip("High-density colour (hot end of the gradient).")]
        [SerializeField] private Color highColor = new Color(1f, 0f, 0f, 1f);    // red

        [Header("Rendering")]
        [Tooltip("Overall opacity multiplier applied to the rendered texture.")]
        [Range(0f, 1f)]
        [SerializeField] private float opacity = 0.8f;

        // ── Cached texture ────────────────────────────────────────────────────────

        private Texture2D _lastTexture;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Convert <paramref name="data"/> into a <see cref="Texture2D"/>.
        /// Caller is responsible for destroying the texture when no longer needed.
        /// </summary>
        public Texture2D Render(HeatmapData data)
        {
            if (data == null || data.width <= 0 || data.height <= 0) return null;

            var tex = new Texture2D(data.width, data.height, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp
            };

            // Initialise all pixels to transparent
            Color[] pixels = new Color[data.width * data.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            foreach (var cell in data.cells)
            {
                float t = cell.normalised;
                Color c = t < 0.5f
                    ? Color.Lerp(lowColor, midColor, t * 2f)
                    : Color.Lerp(midColor, highColor, (t - 0.5f) * 2f);
                c.a *= opacity;
                pixels[cell.y * data.width + cell.x] = c;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _lastTexture = tex;
            return tex;
        }

        /// <summary>Adjust opacity and re-render with the last data.</summary>
        public void SetOpacity(float newOpacity)
        {
            opacity = Mathf.Clamp01(newOpacity);
        }
    }
}
