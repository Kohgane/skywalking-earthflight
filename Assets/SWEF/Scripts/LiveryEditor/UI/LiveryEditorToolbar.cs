// LiveryEditorToolbar.cs — Phase 115: Advanced Aircraft Livery Editor
// Toolbar: brush tools, selection, transform, text, decal, undo/redo, save/load.
// Namespace: SWEF.LiveryEditor

using System;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Controls the tool selection toolbar at the top of the livery editor.
    /// Dispatches tool-change notifications and delegates undo/redo/save/load actions.
    /// </summary>
    public class LiveryEditorToolbar : MonoBehaviour
    {
        // ── Inspector references ──────────────────────────────────────────────────
        [Header("Sub-Systems")]
        [SerializeField] private LiveryEditorManager  manager;
        [SerializeField] private LayerHistoryController history;
        [SerializeField] private BrushEngine          brushEngine;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the active tool changes.</summary>
        public event Action<EditorTool> OnToolChanged;

        // ── Tool enum ─────────────────────────────────────────────────────────────
        /// <summary>Available editor tools.</summary>
        public enum EditorTool
        {
            /// <summary>Paint brush tool.</summary>
            Brush,
            /// <summary>Selection / marquee tool.</summary>
            Select,
            /// <summary>Move / transform tool.</summary>
            Transform,
            /// <summary>Text overlay tool.</summary>
            Text,
            /// <summary>Decal placement tool.</summary>
            Decal,
            /// <summary>Fill / bucket tool.</summary>
            Fill,
            /// <summary>Eyedropper / colour sampler.</summary>
            Eyedropper
        }

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Currently active editor tool.</summary>
        public EditorTool ActiveTool { get; private set; } = EditorTool.Brush;

        // ── Tool selection ────────────────────────────────────────────────────────

        /// <summary>Activates the given tool.</summary>
        public void SelectTool(EditorTool tool)
        {
            if (ActiveTool == tool) return;
            ActiveTool = tool;
            OnToolChanged?.Invoke(tool);
        }

        // ── History actions ───────────────────────────────────────────────────────

        /// <summary>Performs an undo step.</summary>
        public void Undo()
        {
            if (history != null && history.CanUndo)
                history.Undo();
        }

        /// <summary>Performs a redo step.</summary>
        public void Redo()
        {
            if (history != null && history.CanRedo)
                history.Redo();
        }

        /// <summary>Whether undo is currently available.</summary>
        public bool CanUndo => history != null && history.CanUndo;

        /// <summary>Whether redo is currently available.</summary>
        public bool CanRedo => history != null && history.CanRedo;

        // ── Save / Load ───────────────────────────────────────────────────────────

        /// <summary>Saves the active livery via the manager.</summary>
        public void Save() => manager?.SaveActiveLivery();
    }
}
