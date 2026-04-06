// LiveryLayer.cs — Phase 115: Advanced Aircraft Livery Editor
// Individual paint layer: texture data, opacity, blend mode, visibility, lock, name, mask.
// Namespace: SWEF.LiveryEditor

using System;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Represents a single paint layer within a livery stack.
    /// Holds all per-layer properties including texture data, opacity,
    /// blend mode, visibility, lock state, and an optional mask.
    /// </summary>
    [Serializable]
    public class LiveryLayer
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        /// <summary>Unique identifier for this layer.</summary>
        public string LayerId { get; private set; }

        /// <summary>Human-readable display name shown in the layer panel.</summary>
        public string Name;

        /// <summary>Type of this layer (BaseColor, Decal, Pattern, etc.).</summary>
        public LiveryLayerType LayerType;

        // ── Visual properties ─────────────────────────────────────────────────────
        /// <summary>Layer opacity in the range 0 (transparent) to 1 (fully opaque).</summary>
        [Range(0f, 1f)]
        public float Opacity = 1f;

        /// <summary>Blend mode used when compositing this layer onto the layers below.</summary>
        public BlendMode BlendMode = BlendMode.Normal;

        /// <summary>Whether this layer is visible in the preview and final export.</summary>
        public bool IsVisible = true;

        /// <summary>When <c>true</c> the layer cannot be modified.</summary>
        public bool IsLocked;

        // ── Texture data ──────────────────────────────────────────────────────────
        /// <summary>Paint texture for this layer; may be <c>null</c> for placeholder layers.</summary>
        public Texture2D LayerTexture;

        /// <summary>Optional mask texture; white = show, black = hide.</summary>
        public Texture2D MaskTexture;

        /// <summary>Whether the mask is active and applied during compositing.</summary>
        public bool MaskEnabled;

        // ── Grouping ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Optional group identifier; layers sharing the same group id are collapsed
        /// together in the layer panel.
        /// </summary>
        public string GroupId;

        // ── Constructor ───────────────────────────────────────────────────────────
        /// <summary>Creates a new layer with a generated unique id.</summary>
        public LiveryLayer(string name, LiveryLayerType type)
        {
            LayerId   = Guid.NewGuid().ToString();
            Name      = name;
            LayerType = type;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises <see cref="LayerTexture"/> to a blank transparent texture
        /// of the given resolution.
        /// </summary>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        public void InitializeTexture(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException("Texture dimensions must be positive.");
            LayerTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name    = $"Layer_{Name}",
                wrapMode = TextureWrapMode.Clamp
            };
            ClearTexture();
        }

        /// <summary>
        /// Clears all pixels in <see cref="LayerTexture"/> to fully transparent.
        /// </summary>
        public void ClearTexture()
        {
            if (LayerTexture == null) return;
            var pixels = new Color32[LayerTexture.width * LayerTexture.height];
            LayerTexture.SetPixels32(pixels);
            LayerTexture.Apply();
        }

        /// <summary>
        /// Creates a shallow copy of this layer with a new unique identifier.
        /// </summary>
        public LiveryLayer Duplicate()
        {
            var copy = new LiveryLayer(Name + " (copy)", LayerType)
            {
                Opacity       = Opacity,
                BlendMode     = BlendMode,
                IsVisible     = IsVisible,
                IsLocked      = false,
                MaskEnabled   = MaskEnabled,
                GroupId       = GroupId
            };

            if (LayerTexture != null)
            {
                copy.LayerTexture = Instantiate(LayerTexture);
                copy.LayerTexture.name = $"Layer_{copy.Name}";
            }

            if (MaskTexture != null)
                copy.MaskTexture = Instantiate(MaskTexture);

            return copy;
        }

        // ── Helpers (Unity object) ────────────────────────────────────────────────
        private static T Instantiate<T>(T obj) where T : UnityEngine.Object
            => UnityEngine.Object.Instantiate(obj);
    }
}
