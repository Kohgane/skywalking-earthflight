// LiveryEditorConfig.cs — Phase 115: Advanced Aircraft Livery Editor
// ScriptableObject configuration for the livery editor system.
// Namespace: SWEF.LiveryEditor

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Editor-configurable settings for the Advanced Aircraft Livery Editor.
    /// Create an asset instance via <c>Assets → Create → SWEF → Livery Editor Config</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Livery Editor Config", fileName = "LiveryEditorConfig")]
    public class LiveryEditorConfig : ScriptableObject
    {
        // ── Layer limits ──────────────────────────────────────────────────────────

        [Header("Layer Settings")]
        [Tooltip("Maximum number of layers allowed in a single livery.")]
        [Range(1, 64)]
        public int MaxLayers = 32;

        [Tooltip("Maximum texture resolution (width and height) for the paint canvas.")]
        public int MaxTextureResolution = 4096;

        [Tooltip("Default paint canvas resolution used for new liveries.")]
        public int DefaultTextureResolution = 2048;

        // ── File formats ──────────────────────────────────────────────────────────

        [Header("Import / Export")]
        [Tooltip("Supported file extensions for custom decal import.")]
        public List<string> SupportedImportFormats = new List<string> { "png", "jpg", "jpeg" };

        [Tooltip("Default export format for livery texture sheets.")]
        public LiveryExportFormat DefaultExportFormat = LiveryExportFormat.SWEFLivery;

        // ── Undo history ──────────────────────────────────────────────────────────

        [Header("Undo / Redo")]
        [Tooltip("Maximum number of undo steps retained in memory.")]
        [Range(1, 200)]
        public int UndoHistoryDepth = 50;

        [Tooltip("Whether complex operations (merge, filter) use full-texture snapshots.")]
        public bool UseSnapshotUndoForComplexOps = true;

        // ── Colour palette ────────────────────────────────────────────────────────

        [Header("Colour Palettes")]
        [Tooltip("Preset named colour swatches available to all users.")]
        public List<ColorPreset> ColorPalettePresets = new List<ColorPreset>
        {
            new ColorPreset { Name = "White",      Value = Color.white           },
            new ColorPreset { Name = "Black",      Value = Color.black           },
            new ColorPreset { Name = "Red",        Value = Color.red             },
            new ColorPreset { Name = "Blue",       Value = Color.blue            },
            new ColorPreset { Name = "Yellow",     Value = Color.yellow          },
            new ColorPreset { Name = "Green",      Value = Color.green           },
            new ColorPreset { Name = "Silver",     Value = new Color(0.75f, 0.75f, 0.75f) },
            new ColorPreset { Name = "Gold",       Value = new Color(1f, 0.84f, 0f) }
        };

        [Tooltip("Number of recent colours to remember in the colour picker.")]
        [Range(1, 32)]
        public int RecentColorCount = 16;

        // ── Canvas behaviour ──────────────────────────────────────────────────────

        [Header("Canvas")]
        [Tooltip("Background colour displayed behind transparent canvas areas.")]
        public Color CanvasBackgroundColor = new Color(0.2f, 0.2f, 0.2f);

        [Tooltip("Enable UV grid overlay on the 2-D edit canvas by default.")]
        public bool ShowUVGridByDefault = true;

        [Tooltip("Grid line opacity (0 = invisible, 1 = fully opaque).")]
        [Range(0f, 1f)]
        public float UVGridOpacity = 0.25f;

        // ── Auto-save ─────────────────────────────────────────────────────────────

        [Header("Auto-Save")]
        [Tooltip("Automatically save the active livery at this interval (seconds). 0 = disabled.")]
        [Range(0, 600)]
        public float AutoSaveIntervalSeconds = 60f;

        // ── UGC upload ────────────────────────────────────────────────────────────

        [Header("UGC Decal Upload")]
        [Tooltip("Maximum file size (kilobytes) for a user-uploaded decal image.")]
        [Range(64, 8192)]
        public int MaxUploadFileSizeKB = 2048;

        [Tooltip("Maximum pixel dimension (width or height) for an imported decal texture.")]
        [Range(64, 4096)]
        public int MaxImportedDecalResolution = 1024;

        // ── Preview renderer ──────────────────────────────────────────────────────

        [Header("3-D Preview")]
        [Tooltip("Default lighting preset index for the livery preview renderer.")]
        [Range(0, 5)]
        public int DefaultLightingPreset = 0;

        [Tooltip("Rotation speed of the preview aircraft (degrees per second) when auto-rotating.")]
        [Range(1f, 180f)]
        public float PreviewAutoRotateSpeed = 20f;
    }

    // ── Nested helper ─────────────────────────────────────────────────────────────

    /// <summary>A named colour swatch for the palette preset list.</summary>
    [System.Serializable]
    public class ColorPreset
    {
        /// <summary>Display label in the colour picker palette.</summary>
        public string Name;
        /// <summary>RGBA colour value.</summary>
        public Color Value = Color.white;
    }
}
