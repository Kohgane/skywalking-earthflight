// BrushEngine.cs — Phase 115: Advanced Aircraft Livery Editor
// Paint brush system: round, square, soft, hard, airbrush, eraser with pressure sensitivity.
// Namespace: SWEF.LiveryEditor

using System;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Handles paint stroke application onto a <see cref="Texture2D"/>
    /// canvas.  Supports multiple brush types, pressure sensitivity, and mirroring.
    /// </summary>
    public class BrushEngine : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Default Brush")]
        [SerializeField] private BrushSettings defaultBrush = new BrushSettings();

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised at the start of each new paint stroke.</summary>
        public event Action<Vector2> OnStrokeBegin;

        /// <summary>Raised for each paint step applied along a stroke.</summary>
        public event Action<Vector2> OnStrokeStep;

        /// <summary>Raised when a paint stroke ends.</summary>
        public event Action OnStrokeEnd;

        // ── Public properties ─────────────────────────────────────────────────────
        /// <summary>Active brush settings for the current stroke.</summary>
        public BrushSettings ActiveBrush { get; set; }

        /// <summary>Whether a stroke is currently in progress.</summary>
        public bool IsStroking { get; private set; }

        // ── Internal state ────────────────────────────────────────────────────────
        private Texture2D _canvas;
        private Vector2   _lastStrokePos;

        // ── Initialise ────────────────────────────────────────────────────────────

        private void Awake()
        {
            ActiveBrush = defaultBrush ?? new BrushSettings();
        }

        /// <summary>Sets the target canvas texture for paint operations.</summary>
        public void SetCanvas(Texture2D canvas) => _canvas = canvas;

        // ── Stroke control ────────────────────────────────────────────────────────

        /// <summary>Begins a new paint stroke at the given UV position.</summary>
        /// <param name="uvPos">Normalised UV coordinate (0–1 on each axis).</param>
        /// <param name="pressure">Stylus or simulated pressure (0–1).</param>
        public void BeginStroke(Vector2 uvPos, float pressure = 1f)
        {
            IsStroking     = true;
            _lastStrokePos = uvPos;
            ApplyBrush(uvPos, pressure);
            OnStrokeBegin?.Invoke(uvPos);
        }

        /// <summary>Continues the stroke to the given UV position.</summary>
        public void ContinueStroke(Vector2 uvPos, float pressure = 1f)
        {
            if (!IsStroking || _canvas == null) return;

            // Interpolate along the stroke for smooth lines.
            float dist    = Vector2.Distance(_lastStrokePos, uvPos);
            float spacing = Mathf.Max(0.001f, ActiveBrush.Spacing * (ActiveBrush.SizePx / (float)_canvas.width));
            int   steps   = Mathf.Max(1, Mathf.FloorToInt(dist / spacing));

            for (int s = 1; s <= steps; s++)
            {
                float t   = (float)s / steps;
                Vector2 p = Vector2.Lerp(_lastStrokePos, uvPos, t);
                ApplyBrush(p, pressure);
                OnStrokeStep?.Invoke(p);
            }

            _lastStrokePos = uvPos;
        }

        /// <summary>Ends the current stroke.</summary>
        public void EndStroke()
        {
            IsStroking = false;
            OnStrokeEnd?.Invoke();
        }

        // ── Core paint ────────────────────────────────────────────────────────────

        /// <summary>
        /// Paints a single brush dab at the given UV position on the canvas.
        /// </summary>
        /// <param name="uvPos">Normalised UV position.</param>
        /// <param name="pressure">Pressure multiplier applied to opacity.</param>
        public void ApplyBrush(Vector2 uvPos, float pressure = 1f)
        {
            if (_canvas == null) return;

            int cx     = Mathf.RoundToInt(uvPos.x * (_canvas.width  - 1));
            int cy     = Mathf.RoundToInt(uvPos.y * (_canvas.height - 1));
            int radius = Mathf.Max(1, ActiveBrush.SizePx / 2);
            float effectiveOpacity = ActiveBrush.Opacity * Mathf.Clamp01(pressure);

            PaintDab(cx, cy, radius, effectiveOpacity);

            if (ActiveBrush.Mirror != MirrorMode.None)
                PaintMirroredDab(cx, cy, radius, effectiveOpacity);

            _canvas.Apply();
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void PaintDab(int cx, int cy, int radius, float opacity)
        {
            int x0 = Mathf.Max(0, cx - radius);
            int x1 = Mathf.Min(_canvas.width  - 1, cx + radius);
            int y0 = Mathf.Max(0, cy - radius);
            int y1 = Mathf.Min(_canvas.height - 1, cy + radius);

            for (int py = y0; py <= y1; py++)
            {
                for (int px = x0; px <= x1; px++)
                {
                    float alpha = ComputePixelAlpha(px, py, cx, cy, radius);
                    if (alpha <= 0f) continue;

                    Color existing = _canvas.GetPixel(px, py);
                    Color paint    = GetPaintColor(opacity * alpha);
                    _canvas.SetPixel(px, py, BlendPaintPixel(existing, paint));
                }
            }
        }

        private void PaintMirroredDab(int cx, int cy, int radius, float opacity)
        {
            bool mirrorX = ActiveBrush.Mirror == MirrorMode.Horizontal || ActiveBrush.Mirror == MirrorMode.Both;
            bool mirrorY = ActiveBrush.Mirror == MirrorMode.Vertical   || ActiveBrush.Mirror == MirrorMode.Both;

            int mcx = mirrorX ? _canvas.width  - 1 - cx : cx;
            int mcy = mirrorY ? _canvas.height - 1 - cy : cy;

            PaintDab(mcx, mcy, radius, opacity);
        }

        private float ComputePixelAlpha(int px, int py, int cx, int cy, int radius)
        {
            float dx   = px - cx;
            float dy   = py - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            switch (ActiveBrush.Type)
            {
                case BrushType.Square:
                    return (Mathf.Abs(dx) <= radius && Mathf.Abs(dy) <= radius) ? 1f : 0f;

                case BrushType.Soft:
                {
                    float t = dist / radius;
                    return t >= 1f ? 0f : 1f - t;
                }

                case BrushType.Airbrush:
                {
                    float t = dist / radius;
                    return t >= 1f ? 0f : Mathf.Clamp01(Mathf.Exp(-t * t * 4f));
                }

                default: // Round / Hard / Eraser
                    return dist <= radius ? 1f : 0f;
            }
        }

        private Color GetPaintColor(float alpha)
        {
            Color c = ActiveBrush.Type == BrushType.Eraser
                ? new Color(0, 0, 0, 0)
                : ActiveBrush.Color;
            c.a = alpha;
            return c;
        }

        private static Color BlendPaintPixel(Color dst, Color paint)
        {
            if (paint.a <= 0f) return dst;
            float a = paint.a + dst.a * (1f - paint.a);
            if (a <= 0f) return new Color(0, 0, 0, 0);
            return new Color(
                (paint.r * paint.a + dst.r * dst.a * (1f - paint.a)) / a,
                (paint.g * paint.a + dst.g * dst.a * (1f - paint.a)) / a,
                (paint.b * paint.a + dst.b * dst.a * (1f - paint.a)) / a,
                a);
        }
    }
}
