// LiveryEditorData.cs — Phase 115: Advanced Aircraft Livery Editor
// Enums and data models for the livery editor system.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    // ── Layer Type ────────────────────────────────────────────────────────────────

    /// <summary>Classification of a livery layer.</summary>
    public enum LiveryLayerType
    {
        /// <summary>Solid base colour fill for the aircraft surface.</summary>
        BaseColor,
        /// <summary>Repeating pattern (stripes, chevrons, camo, etc.).</summary>
        Pattern,
        /// <summary>Image decal placed at a specific UV position.</summary>
        Decal,
        /// <summary>Text rendered directly onto the surface.</summary>
        Text,
        /// <summary>Linear, radial, or angular colour gradient.</summary>
        Gradient,
        /// <summary>Mask that controls the visibility of layers below.</summary>
        Mask,
        /// <summary>Post-processing visual effect (gloss, matte, metallic).</summary>
        Effect
    }

    // ── Blend Mode ────────────────────────────────────────────────────────────────

    /// <summary>Compositing blend mode applied to a livery layer.</summary>
    public enum BlendMode
    {
        /// <summary>Standard alpha compositing.</summary>
        Normal,
        /// <summary>Multiplies source and destination colours.</summary>
        Multiply,
        /// <summary>Inverts, multiplies, then inverts the result.</summary>
        Screen,
        /// <summary>Combines Multiply and Screen based on destination brightness.</summary>
        Overlay,
        /// <summary>Softer version of Overlay with less contrast.</summary>
        SoftLight,
        /// <summary>Selects the darker of source or destination.</summary>
        Darken,
        /// <summary>Selects the lighter of source or destination.</summary>
        Lighten,
        /// <summary>Adds source to destination (saturates at white).</summary>
        Add
    }

    // ── Decal Category ────────────────────────────────────────────────────────────

    /// <summary>Thematic category for a built-in decal asset.</summary>
    public enum DecalCategory
    {
        /// <summary>Commercial airline logos and livery marks.</summary>
        Airline,
        /// <summary>Military insignia, roundels, and markings.</summary>
        Military,
        /// <summary>Motorsport team, sponsor, and racing numbers.</summary>
        Racing,
        /// <summary>National flags and civil aviation markings.</summary>
        National,
        /// <summary>User-uploaded custom decal.</summary>
        Custom
    }

    // ── Livery Export Format ──────────────────────────────────────────────────────

    /// <summary>File format for livery texture export.</summary>
    public enum LiveryExportFormat
    {
        /// <summary>PNG texture sheet (lossless).</summary>
        PNG,
        /// <summary>JPEG texture sheet (lossy, smaller file size).</summary>
        JPEG,
        /// <summary>SWEF proprietary livery package (.sweflivery).</summary>
        SWEFLivery
    }

    // ── Brush Type ────────────────────────────────────────────────────────────────

    /// <summary>Brush shape and behaviour for the paint engine.</summary>
    public enum BrushType
    {
        /// <summary>Circular brush with hard edge.</summary>
        Round,
        /// <summary>Square brush with hard edge.</summary>
        Square,
        /// <summary>Circular brush with soft, feathered edge.</summary>
        Soft,
        /// <summary>Low-opacity spray brush simulating an airbrush.</summary>
        Airbrush,
        /// <summary>Removes paint; writes transparent pixels.</summary>
        Eraser
    }

    // ── Mirror Mode ───────────────────────────────────────────────────────────────

    /// <summary>Axis of symmetry for mirrored painting.</summary>
    public enum MirrorMode
    {
        /// <summary>No mirroring applied.</summary>
        None,
        /// <summary>Mirror paint strokes across the aircraft's vertical axis.</summary>
        Horizontal,
        /// <summary>Mirror paint strokes across the aircraft's horizontal axis.</summary>
        Vertical,
        /// <summary>Mirror across both axes simultaneously.</summary>
        Both
    }

    // ── Gradient Type ─────────────────────────────────────────────────────────────

    /// <summary>Shape of a gradient fill.</summary>
    public enum GradientType
    {
        /// <summary>Straight linear gradient between two points.</summary>
        Linear,
        /// <summary>Circular gradient radiating from a centre point.</summary>
        Radial,
        /// <summary>Gradient sweeping around an angle.</summary>
        Angular,
        /// <summary>Reflected/symmetric linear gradient.</summary>
        Reflected
    }

    // ── Pattern Type ──────────────────────────────────────────────────────────────

    /// <summary>Type of procedurally generated pattern.</summary>
    public enum PatternType
    {
        /// <summary>Parallel horizontal or vertical stripes.</summary>
        Stripes,
        /// <summary>V-shaped chevron marks.</summary>
        Chevrons,
        /// <summary>Military-style disruptive camouflage.</summary>
        Camouflage,
        /// <summary>Classic racing livery chequered pattern.</summary>
        Chequered,
        /// <summary>Regular polygon grid (hexagons, triangles, etc.).</summary>
        Geometric,
        /// <summary>Smooth noise-based organic pattern.</summary>
        Noise
    }

    // ── UV Zone ───────────────────────────────────────────────────────────────────

    /// <summary>Named UV region of the aircraft model.</summary>
    public enum UVZone
    {
        /// <summary>Main fuselage body.</summary>
        Fuselage,
        /// <summary>Left and right wing surfaces.</summary>
        Wings,
        /// <summary>Horizontal and vertical stabilisers.</summary>
        Tail,
        /// <summary>Engine nacelles and cowlings.</summary>
        Engines,
        /// <summary>Landing gear doors and bays.</summary>
        LandingGear,
        /// <summary>Cockpit and nose section.</summary>
        Nose,
        /// <summary>Entire aircraft (all zones).</summary>
        All
    }

    // ── Livery Template Category ──────────────────────────────────────────────────

    /// <summary>Thematic category for a livery template.</summary>
    public enum LiveryTemplateCategory
    {
        /// <summary>Real-world inspired commercial airline schemes.</summary>
        Commercial,
        /// <summary>Military camouflage and tactical liveries.</summary>
        Military,
        /// <summary>Motorsport and air racing liveries.</summary>
        Racing,
        /// <summary>Historic aircraft paint schemes.</summary>
        Historic,
        /// <summary>Fictional / fantasy liveries.</summary>
        Fantasy,
        /// <summary>Team or squadron shared liveries.</summary>
        Team
    }

    // ── Data Models ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot of a single layer's data used for undo/redo history entries.
    /// </summary>
    [Serializable]
    public class LayerSnapshot
    {
        /// <summary>Unique identifier of the layer this snapshot belongs to.</summary>
        public string LayerId;
        /// <summary>Serialised layer state as a JSON string.</summary>
        public string SerializedState;
        /// <summary>UTC timestamp when the snapshot was taken.</summary>
        public long TimestampUtc;

        /// <summary>Creates a new snapshot.</summary>
        public static LayerSnapshot Create(string layerId, string state) =>
            new LayerSnapshot
            {
                LayerId       = layerId,
                SerializedState = state,
                TimestampUtc  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
    }

    /// <summary>
    /// Metadata attached to a community-shared or exported livery package.
    /// </summary>
    [Serializable]
    public class LiveryMetadata
    {
        /// <summary>Globally unique livery identifier.</summary>
        public string LiveryId;
        /// <summary>Human-readable display name.</summary>
        public string Name;
        /// <summary>Author / creator username.</summary>
        public string Author;
        /// <summary>Short description shown in the gallery.</summary>
        public string Description;
        /// <summary>Aircraft type(s) this livery is compatible with.</summary>
        public List<string> CompatibleAircraftIds = new List<string>();
        /// <summary>Schema version of the livery format.</summary>
        public int FormatVersion = 1;
        /// <summary>UTC creation timestamp.</summary>
        public long CreatedAtUtc;
        /// <summary>UTC last-modified timestamp.</summary>
        public long ModifiedAtUtc;
        /// <summary>Community star rating (0–5).</summary>
        public float Rating;
        /// <summary>Number of times the livery has been downloaded.</summary>
        public int DownloadCount;
        /// <summary>Tags for searching and filtering.</summary>
        public List<string> Tags = new List<string>();
    }

    /// <summary>
    /// Colour stop within a multi-stop gradient definition.
    /// </summary>
    [Serializable]
    public class GradientStop
    {
        /// <summary>Normalised position along the gradient (0–1).</summary>
        [Range(0f, 1f)]
        public float Position;
        /// <summary>Colour value at this stop.</summary>
        public Color Color = Color.white;

        /// <summary>Creates a gradient stop at the given position with the given colour.</summary>
        public static GradientStop Create(float position, Color color) =>
            new GradientStop { Position = position, Color = color };
    }

    /// <summary>
    /// Placement transform for a decal on the UV canvas.
    /// </summary>
    [Serializable]
    public class DecalTransform
    {
        /// <summary>Centre position in UV space (0–1 on each axis).</summary>
        public Vector2 UVPosition;
        /// <summary>Rotation in degrees.</summary>
        public float Rotation;
        /// <summary>Scale multiplier relative to the canvas size.</summary>
        public Vector2 Scale = Vector2.one;
        /// <summary>Whether the decal is flipped horizontally.</summary>
        public bool FlipX;
        /// <summary>Whether the decal is flipped vertically.</summary>
        public bool FlipY;
    }

    /// <summary>
    /// Brush settings used by a single paint stroke.
    /// </summary>
    [Serializable]
    public class BrushSettings
    {
        /// <summary>Brush shape and behaviour.</summary>
        public BrushType Type = BrushType.Round;
        /// <summary>Brush diameter in pixels.</summary>
        [Range(1, 512)]
        public int SizePx = 20;
        /// <summary>Brush opacity (0–1).</summary>
        [Range(0f, 1f)]
        public float Opacity = 1f;
        /// <summary>Brush hardness; 0 = feathered, 1 = hard edge.</summary>
        [Range(0f, 1f)]
        public float Hardness = 1f;
        /// <summary>Stroke spacing as a fraction of the brush diameter.</summary>
        [Range(0.01f, 2f)]
        public float Spacing = 0.25f;
        /// <summary>Paint colour.</summary>
        public Color Color = Color.white;
        /// <summary>Active mirror mode for this stroke.</summary>
        public MirrorMode Mirror = MirrorMode.None;
    }

    /// <summary>
    /// Runtime record of a decal asset in the built-in library.
    /// </summary>
    [Serializable]
    public class DecalAssetRecord
    {
        /// <summary>Unique identifier of the decal asset.</summary>
        public string DecalId;
        /// <summary>Display name shown in the decal picker.</summary>
        public string DisplayName;
        /// <summary>Thematic category.</summary>
        public DecalCategory Category;
        /// <summary>Source texture for the decal.</summary>
        public Texture2D Texture;
        /// <summary>Optional short description / attribution.</summary>
        public string Description;
    }

    /// <summary>
    /// Represents a complete livery save file combining all layer data and metadata.
    /// </summary>
    [Serializable]
    public class LiverySaveData
    {
        /// <summary>Livery metadata (name, author, compatibility).</summary>
        public LiveryMetadata Metadata = new LiveryMetadata();
        /// <summary>Ordered list of serialised layer records.</summary>
        public List<string> SerializedLayers = new List<string>();
        /// <summary>Export format used when this save was last exported.</summary>
        public LiveryExportFormat LastExportFormat = LiveryExportFormat.SWEFLivery;
        /// <summary>Target texture resolution (width, clamped to power-of-two).</summary>
        public int TextureResolution = 2048;
    }
}
