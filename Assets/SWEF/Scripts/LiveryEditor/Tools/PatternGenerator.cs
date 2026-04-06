// PatternGenerator.cs — Phase 115: Advanced Aircraft Livery Editor
// Procedural pattern generation: stripes, chevrons, camo, racing livery templates, geometric patterns.
// Namespace: SWEF.LiveryEditor

using System;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Generates procedural pattern textures for livery layers.
    /// All methods return a newly created <see cref="Texture2D"/> that can be
    /// assigned to a <see cref="LiveryLayer"/>.
    /// </summary>
    public static class PatternGenerator
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a pattern texture for the given type.
        /// </summary>
        /// <param name="type">Pattern type to generate.</param>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <param name="primaryColor">Primary / foreground colour.</param>
        /// <param name="secondaryColor">Secondary / background colour.</param>
        /// <param name="scale">Pattern scale multiplier.</param>
        /// <param name="angle">Pattern rotation in degrees (where supported).</param>
        /// <returns>A new <see cref="Texture2D"/> with the generated pattern.</returns>
        public static Texture2D Generate(PatternType type, int width, int height,
            Color primaryColor, Color secondaryColor, float scale = 1f, float angle = 0f)
        {
            return type switch
            {
                PatternType.Stripes     => GenerateStripes(width, height, primaryColor, secondaryColor, scale, angle),
                PatternType.Chevrons    => GenerateChevrons(width, height, primaryColor, secondaryColor, scale),
                PatternType.Camouflage  => GenerateCamouflage(width, height, primaryColor, secondaryColor, scale),
                PatternType.Chequered   => GenerateChequered(width, height, primaryColor, secondaryColor, scale),
                PatternType.Geometric   => GenerateGeometric(width, height, primaryColor, secondaryColor, scale),
                PatternType.Noise       => GenerateNoise(width, height, primaryColor, secondaryColor, scale),
                _                       => GenerateSolid(width, height, primaryColor)
            };
        }

        // ── Stripe ────────────────────────────────────────────────────────────────

        private static Texture2D GenerateStripes(int w, int h, Color a, Color b, float scale, float angle)
        {
            var tex    = CreateTexture(w, h);
            float freq = Mathf.Max(0.001f, scale);
            float rad  = angle * Mathf.Deg2Rad;
            float cos  = Mathf.Cos(rad);
            float sin  = Mathf.Sin(rad);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float uvx  = (float)x / w;
                    float uvy  = (float)y / h;
                    float proj = cos * uvx + sin * uvy;
                    bool  odd  = Mathf.FloorToInt(proj / freq) % 2 == 0;
                    tex.SetPixel(x, y, odd ? a : b);
                }

            tex.Apply();
            return tex;
        }

        // ── Chevrons ──────────────────────────────────────────────────────────────

        private static Texture2D GenerateChevrons(int w, int h, Color a, Color b, float scale)
        {
            var tex = CreateTexture(w, h);
            float freq = Mathf.Max(0.001f, scale * 0.5f);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float uvx  = (float)x / w;
                    float uvy  = (float)y / h;
                    float v    = Mathf.Repeat(uvy / freq, 1f);
                    float dist = Mathf.Abs(uvx - 0.5f);
                    bool  fill = v + dist < 0.5f;
                    tex.SetPixel(x, y, fill ? a : b);
                }

            tex.Apply();
            return tex;
        }

        // ── Camouflage ────────────────────────────────────────────────────────────

        private static Texture2D GenerateCamouflage(int w, int h, Color a, Color b, float scale)
        {
            var tex  = CreateTexture(w, h);
            float f1 = 3.1f / scale, f2 = 5.7f / scale, f3 = 8.3f / scale;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float uvx = (float)x / w;
                    float uvy = (float)y / h;
                    float n   = Mathf.Sin(uvx * f1 + uvy * f2) * Mathf.Cos(uvx * f3 - uvy * f1);
                    tex.SetPixel(x, y, n > 0f ? a : b);
                }

            tex.Apply();
            return tex;
        }

        // ── Chequered ─────────────────────────────────────────────────────────────

        private static Texture2D GenerateChequered(int w, int h, Color a, Color b, float scale)
        {
            var tex  = CreateTexture(w, h);
            int cell = Mathf.Max(1, Mathf.RoundToInt(w * scale * 0.125f));

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool evenX = (x / cell) % 2 == 0;
                    bool evenY = (y / cell) % 2 == 0;
                    tex.SetPixel(x, y, (evenX == evenY) ? a : b);
                }

            tex.Apply();
            return tex;
        }

        // ── Geometric (hexagons) ──────────────────────────────────────────────────

        private static Texture2D GenerateGeometric(int w, int h, Color a, Color b, float scale)
        {
            var tex  = CreateTexture(w, h);
            float sz = Mathf.Max(0.001f, scale * 0.1f);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float uvx  = (float)x / w / sz;
                    float uvy  = (float)y / h / sz;
                    float qx   = Mathf.Repeat(uvx, 1f) - 0.5f;
                    float qy   = Mathf.Repeat(uvy, 1f) - 0.5f;
                    float dist = Mathf.Max(Mathf.Abs(qx), Mathf.Abs(qy));
                    tex.SetPixel(x, y, dist < 0.4f ? a : b);
                }

            tex.Apply();
            return tex;
        }

        // ── Noise ─────────────────────────────────────────────────────────────────

        private static Texture2D GenerateNoise(int w, int h, Color a, Color b, float scale)
        {
            var tex  = CreateTexture(w, h);
            float f  = scale * 8f;
            var rng  = new System.Random(42);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float uvx = (float)x / w;
                    float uvy = (float)y / h;
                    float n   = Mathf.PerlinNoise(uvx * f, uvy * f);
                    tex.SetPixel(x, y, Color.Lerp(b, a, n));
                }

            tex.Apply();
            return tex;
        }

        // ── Solid fallback ────────────────────────────────────────────────────────

        private static Texture2D GenerateSolid(int w, int h, Color color)
        {
            var tex = CreateTexture(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, color);
            tex.Apply();
            return tex;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static Texture2D CreateTexture(int w, int h) =>
            new Texture2D(Mathf.Max(1, w), Mathf.Max(1, h), TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat
            };
    }
}
