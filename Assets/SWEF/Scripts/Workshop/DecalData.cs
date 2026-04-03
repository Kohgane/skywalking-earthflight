// DecalData.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using System;
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// Serialisable data class that describes a single decal placed on an
    /// aircraft surface.  Up to <see cref="DecalEditorController.MaxDecals"/>
    /// instances can be stored per <see cref="AircraftBuildData"/>.
    /// </summary>
    [Serializable]
    public class DecalData
    {
        /// <summary>
        /// Resource path (relative to a <c>Resources/</c> folder) for the decal
        /// texture (should be a transparent PNG sprite).
        /// </summary>
        [Tooltip("Resources/ path for the decal texture.")]
        public string texturePath = string.Empty;

        /// <summary>
        /// UV-space position on the aircraft mesh where the decal is anchored.
        /// X and Y are in the range [0, 1].
        /// </summary>
        [Tooltip("UV-space anchor position [0–1, 0–1] on the aircraft mesh.")]
        public Vector2 uvPosition = new Vector2(0.5f, 0.5f);

        /// <summary>Rotation of the decal around its anchor point in degrees.</summary>
        [Tooltip("Rotation around the UV anchor in degrees.")]
        public float rotation = 0f;

        /// <summary>Uniform scale of the decal relative to the UV tile size.</summary>
        [Tooltip("Uniform scale multiplier relative to the UV tile.")]
        public float scale = 0.1f;

        /// <summary>
        /// Layer index controlling draw order when decals overlap.
        /// Higher values are drawn on top.
        /// </summary>
        [Tooltip("Draw-order layer (higher = on top) when decals overlap.")]
        public int layerIndex = 0;
    }
}
