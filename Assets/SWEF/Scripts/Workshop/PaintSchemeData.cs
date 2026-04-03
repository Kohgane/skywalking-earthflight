// PaintSchemeData.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using System;
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>Predefined livery pattern applied over the base colour channels.</summary>
    public enum PaintPattern
    {
        /// <summary>Single solid colour — no secondary pattern.</summary>
        Solid,
        /// <summary>Horizontal racing stripe along the fuselage.</summary>
        RacingStripe,
        /// <summary>Military-inspired disruptive camouflage.</summary>
        Camo,
        /// <summary>Digital / pixelated low-poly style.</summary>
        Digital,
        /// <summary>Diagonal checkerboard pattern.</summary>
        Checkerboard,
        /// <summary>Diagonal striped lines.</summary>
        DiagonalStripes,
        /// <summary>Flames motif on the nose section.</summary>
        Flames,
        /// <summary>Carbon-fibre weave texture overlay.</summary>
        CarbonFibre,
        /// <summary>Retro airline livery with cheatline.</summary>
        RetroAirline,
        /// <summary>Custom / user-imported pattern.</summary>
        Custom
    }

    /// <summary>
    /// Serialisable data class describing a complete paint / livery scheme
    /// for an aircraft build.  Stored inside <see cref="AircraftBuildData"/> and
    /// applied by <see cref="PaintEditorController"/>.
    /// </summary>
    [Serializable]
    public class PaintSchemeData
    {
        /// <summary>Primary body colour.</summary>
        [Tooltip("Primary body colour.")]
        public Color primaryColor = Color.white;

        /// <summary>Secondary accent colour (stripes, trim, etc.).</summary>
        [Tooltip("Secondary accent colour.")]
        public Color secondaryColor = Color.gray;

        /// <summary>Tertiary accent colour used for small details.</summary>
        [Tooltip("Tertiary accent colour used for fine details.")]
        public Color accentColor = Color.black;

        /// <summary>PBR metallic value in the range [0, 1] (0 = matte, 1 = mirror).</summary>
        [Tooltip("PBR metallic value [0–1].")]
        [Range(0f, 1f)]
        public float metallic = 0.2f;

        /// <summary>PBR roughness value in the range [0, 1] (0 = mirror-smooth, 1 = fully rough).</summary>
        [Tooltip("PBR roughness value [0–1].")]
        [Range(0f, 1f)]
        public float roughness = 0.5f;

        /// <summary>Livery pattern to overlay on top of the colour channels.</summary>
        [Tooltip("Livery pattern overlaid on top of the colours.")]
        public PaintPattern pattern = PaintPattern.Solid;

        /// <summary>
        /// Optional resource path for a custom pattern texture (used when
        /// <see cref="pattern"/> is <see cref="PaintPattern.Custom"/>).
        /// </summary>
        [Tooltip("Resources/ path for a custom pattern texture.")]
        public string customPatternPath = string.Empty;
    }
}
