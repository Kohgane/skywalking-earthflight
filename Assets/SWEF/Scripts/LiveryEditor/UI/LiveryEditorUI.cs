// LiveryEditorUI.cs — Phase 115: Advanced Aircraft Livery Editor
// Full editor interface: canvas, layer panel, tool palette, color picker, properties inspector.
// Namespace: SWEF.LiveryEditor

using System;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Root UI controller for the livery editor.
    /// Coordinates all editor panels (canvas, layers, tools, colour picker, inspector)
    /// and delegates user actions to the appropriate sub-systems.
    /// </summary>
    public class LiveryEditorUI : MonoBehaviour
    {
        // ── Inspector references ──────────────────────────────────────────────────
        [Header("Sub-System References")]
        [SerializeField] private LiveryEditorManager manager;
        [SerializeField] private LiveryLayerManager  layerManager;
        [SerializeField] private BrushEngine         brushEngine;
        [SerializeField] private ColorPickerController colorPicker;
        [SerializeField] private DecalPlacer          decalPlacer;

        [Header("UI Panels")]
        [SerializeField] private GameObject layerPanel;
        [SerializeField] private GameObject toolPalette;
        [SerializeField] private GameObject propertiesPanel;
        [SerializeField] private GameObject colorPickerPanel;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the user requests to save the active livery.</summary>
        public event Action OnSaveRequested;

        /// <summary>Raised when the user closes the editor.</summary>
        public event Action OnCloseRequested;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether the editor UI is currently visible.</summary>
        public bool IsVisible { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake() => SetVisible(false);

        // ── Show / Hide ───────────────────────────────────────────────────────────

        /// <summary>Shows or hides the full editor UI.</summary>
        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (layerPanel)        layerPanel.SetActive(visible);
            if (toolPalette)       toolPalette.SetActive(visible);
            if (propertiesPanel)   propertiesPanel.SetActive(visible);
            if (colorPickerPanel)  colorPickerPanel.SetActive(visible);

            manager?.SetEditorOpen(visible);
        }

        // ── Tool selection ────────────────────────────────────────────────────────

        /// <summary>Activates the brush tool.</summary>
        public void SelectBrushTool()
        {
            if (brushEngine != null) brushEngine.enabled = true;
            if (decalPlacer != null) decalPlacer.CancelPlacement();
        }

        /// <summary>Activates the decal placement tool.</summary>
        public void SelectDecalTool()
        {
            if (brushEngine != null) brushEngine.enabled = false;
        }

        // ── Save / Close ──────────────────────────────────────────────────────────

        /// <summary>Requests the manager to save the active livery.</summary>
        public void RequestSave()
        {
            manager?.SaveActiveLivery();
            OnSaveRequested?.Invoke();
        }

        /// <summary>Closes the editor UI.</summary>
        public void RequestClose()
        {
            SetVisible(false);
            OnCloseRequested?.Invoke();
        }
    }
}
