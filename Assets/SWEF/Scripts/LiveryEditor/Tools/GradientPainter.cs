// GradientPainter.cs — Phase 115: Advanced Aircraft Livery Editor
// Gradient tool: linear, radial, angular gradients with multi-stop color support.
// Namespace: SWEF.LiveryEditor

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Generates gradient-filled textures for livery layers.
    /// Supports linear, radial, angular, and reflected gradient types with
    /// arbitrary multi-stop colour definitions.
    /// </summary>
    public static class GradientPainter
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Renders a gradient into the given texture.
        /// </summary>
        /// <param name="canvas">Target texture (modified in place; Apply() is called).</param>
        /// <param name="type">Gradient shape.</param>
        /// <param name="stops">Ordered colour stops (sorted by Position).</param>
        /// <param name="startUV">Start UV coordinate (for linear / angular).</param>
        /// <param name="endUV">End UV coordinate (for linear).</param>
        public static void Paint(Texture2D canvas, GradientType type,
            IList<GradientStop> stops, Vector2 startUV, Vector2 endUV)
        {
            if (canvas == null || stops == null || stops.Count == 0) return;

            var sorted = stops.OrderBy(s => s.Position).ToList();

            for (int y = 0; y < canvas.height; y++)
            {
                for (int x = 0; x < canvas.width; x++)
                {
                    float uvx = (float)x / (canvas.width  - 1);
                    float uvy = (float)y / (canvas.height - 1);

                    float t = SampleT(type, new Vector2(uvx, uvy), startUV, endUV);
                    canvas.SetPixel(x, y, SampleGradient(sorted, t));
                }
            }

            canvas.Apply();
        }

        /// <summary>
        /// Convenience overload: two-colour linear gradient from top to bottom.
        /// </summary>
        public static void PaintLinear(Texture2D canvas, Color from, Color to)
        {
            var stops = new List<GradientStop>
            {
                GradientStop.Create(0f, from),
                GradientStop.Create(1f, to)
            };
            Paint(canvas, GradientType.Linear, stops, Vector2.zero, Vector2.up);
        }

        // ── T calculation ─────────────────────────────────────────────────────────

        private static float SampleT(GradientType type, Vector2 uv, Vector2 startUV, Vector2 endUV)
        {
            switch (type)
            {
                case GradientType.Radial:
                {
                    Vector2 centre = startUV;
                    float radius   = Vector2.Distance(startUV, endUV);
                    float dist     = Vector2.Distance(uv, centre);
                    return radius > 0f ? Mathf.Clamp01(dist / radius) : 0f;
                }

                case GradientType.Angular:
                {
                    Vector2 dir  = uv - startUV;
                    float angle  = Mathf.Atan2(dir.y, dir.x);
                    return Mathf.Repeat(angle / (Mathf.PI * 2f), 1f);
                }

                case GradientType.Reflected:
                {
                    float t = ProjectLinear(uv, startUV, endUV);
                    return 1f - Mathf.Abs(Mathf.Clamp01(t) * 2f - 1f);
                }

                default: // Linear
                    return Mathf.Clamp01(ProjectLinear(uv, startUV, endUV));
            }
        }

        private static float ProjectLinear(Vector2 uv, Vector2 start, Vector2 end)
        {
            Vector2 dir   = end - start;
            float   len   = dir.sqrMagnitude;
            if (len < 1e-6f) return 0f;
            return Vector2.Dot(uv - start, dir) / len;
        }

        // ── Colour sampling ───────────────────────────────────────────────────────

        private static Color SampleGradient(List<GradientStop> stops, float t)
        {
            if (stops.Count == 1) return stops[0].Color;

            if (t <= stops[0].Position) return stops[0].Color;
            if (t >= stops[^1].Position) return stops[^1].Color;

            for (int i = 0; i < stops.Count - 1; i++)
            {
                var a = stops[i];
                var b = stops[i + 1];
                if (t >= a.Position && t <= b.Position)
                {
                    float span = b.Position - a.Position;
                    float local = span > 0f ? (t - a.Position) / span : 0f;
                    return Color.Lerp(a.Color, b.Color, local);
                }
            }

            return stops[^1].Color;
        }
    }
}
