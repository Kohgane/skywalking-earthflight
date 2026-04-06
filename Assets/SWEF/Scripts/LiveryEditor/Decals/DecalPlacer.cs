// DecalPlacer.cs — Phase 115: Advanced Aircraft Livery Editor
// Decal placement: position, rotation, scale on UV-mapped aircraft surface with real-time preview.
// Namespace: SWEF.LiveryEditor

using System;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Handles placement, transform editing, and baking of decals
    /// onto the livery canvas.  Maintains the current active placement transform
    /// and fires events for real-time preview updates.
    /// </summary>
    public class DecalPlacer : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised whenever the active decal transform changes (for live preview).</summary>
        public event Action<DecalTransform> OnTransformChanged;

        /// <summary>Raised when the decal is committed to the canvas.</summary>
        public event Action<DecalAssetRecord, DecalTransform> OnDecalPlaced;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>The decal currently selected for placement.</summary>
        public DecalAssetRecord ActiveDecal { get; private set; }

        /// <summary>Current placement transform for the active decal.</summary>
        public DecalTransform ActiveTransform { get; private set; } = new DecalTransform
        {
            UVPosition = new Vector2(0.5f, 0.5f),
            Scale      = Vector2.one * 0.1f
        };

        /// <summary>Whether a decal is staged for placement.</summary>
        public bool HasActiveDecal => ActiveDecal != null;

        // ── Select & configure ────────────────────────────────────────────────────

        /// <summary>Selects a decal for staging and resets the transform.</summary>
        public void SelectDecal(DecalAssetRecord record)
        {
            ActiveDecal = record;
            ActiveTransform = new DecalTransform
            {
                UVPosition = new Vector2(0.5f, 0.5f),
                Scale      = Vector2.one * 0.1f
            };
            OnTransformChanged?.Invoke(ActiveTransform);
        }

        /// <summary>Moves the staged decal to the given UV position.</summary>
        public void SetPosition(Vector2 uvPosition)
        {
            ActiveTransform.UVPosition = uvPosition;
            OnTransformChanged?.Invoke(ActiveTransform);
        }

        /// <summary>Sets the rotation of the staged decal.</summary>
        public void SetRotation(float degrees)
        {
            ActiveTransform.Rotation = degrees;
            OnTransformChanged?.Invoke(ActiveTransform);
        }

        /// <summary>Sets the scale of the staged decal.</summary>
        public void SetScale(Vector2 scale)
        {
            ActiveTransform.Scale = scale;
            OnTransformChanged?.Invoke(ActiveTransform);
        }

        /// <summary>Flips the staged decal horizontally.</summary>
        public void ToggleFlipX()
        {
            ActiveTransform.FlipX = !ActiveTransform.FlipX;
            OnTransformChanged?.Invoke(ActiveTransform);
        }

        /// <summary>Flips the staged decal vertically.</summary>
        public void ToggleFlipY()
        {
            ActiveTransform.FlipY = !ActiveTransform.FlipY;
            OnTransformChanged?.Invoke(ActiveTransform);
        }

        // ── Place (commit) ────────────────────────────────────────────────────────

        /// <summary>
        /// Bakes the staged decal onto the given canvas texture at the current transform
        /// and fires <see cref="OnDecalPlaced"/>.
        /// </summary>
        /// <param name="canvas">Target texture.</param>
        public void PlaceDecal(Texture2D canvas)
        {
            if (ActiveDecal == null || canvas == null) return;
            if (ActiveDecal.Texture != null)
                BakeDecal(canvas, ActiveDecal.Texture, ActiveTransform);

            OnDecalPlaced?.Invoke(ActiveDecal, ActiveTransform);
        }

        /// <summary>Cancels the current decal staging without committing.</summary>
        public void CancelPlacement()
        {
            ActiveDecal     = null;
            ActiveTransform = new DecalTransform { UVPosition = new Vector2(0.5f, 0.5f), Scale = Vector2.one * 0.1f };
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private static void BakeDecal(Texture2D canvas, Texture2D decal, DecalTransform t)
        {
            int cx = Mathf.RoundToInt(t.UVPosition.x * canvas.width);
            int cy = Mathf.RoundToInt(t.UVPosition.y * canvas.height);
            int dw = Mathf.RoundToInt(t.Scale.x * canvas.width);
            int dh = Mathf.RoundToInt(t.Scale.y * canvas.height);

            if (dw <= 0 || dh <= 0) return;

            for (int dy = 0; dy < dh; dy++)
            {
                for (int dx = 0; dx < dw; dx++)
                {
                    float srcU = t.FlipX ? 1f - (float)dx / dw : (float)dx / dw;
                    float srcV = t.FlipY ? 1f - (float)dy / dh : (float)dy / dh;

                    // Apply rotation around the decal centre.
                    if (!Mathf.Approximately(t.Rotation, 0f))
                    {
                        float rad = -t.Rotation * Mathf.Deg2Rad;
                        float ux  = srcU - 0.5f;
                        float uy  = srcV - 0.5f;
                        srcU = ux * Mathf.Cos(rad) - uy * Mathf.Sin(rad) + 0.5f;
                        srcV = ux * Mathf.Sin(rad) + uy * Mathf.Cos(rad) + 0.5f;
                    }

                    if (srcU < 0f || srcU > 1f || srcV < 0f || srcV > 1f) continue;

                    Color decalPx = decal.GetPixelBilinear(srcU, srcV);
                    if (decalPx.a <= 0f) continue;

                    int canvasX = cx - dw / 2 + dx;
                    int canvasY = cy - dh / 2 + dy;

                    if (canvasX < 0 || canvasX >= canvas.width)  continue;
                    if (canvasY < 0 || canvasY >= canvas.height) continue;

                    Color existing = canvas.GetPixel(canvasX, canvasY);
                    Color blended  = Color.Lerp(existing, decalPx, decalPx.a);
                    canvas.SetPixel(canvasX, canvasY, blended);
                }
            }

            canvas.Apply();
        }
    }
}
