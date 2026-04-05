// UGCEditorHUD.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — MonoBehaviour that drives the in-editor HUD overlay.
    ///
    /// <para>Manages the tool palette, selected-object properties panel, minimap
    /// indicator, undo/redo buttons, save/test/publish buttons, and grid/snap toggles.</para>
    ///
    /// <para>All <see cref="SerializeField"/> references are null-safe.</para>
    /// </summary>
    public sealed class UGCEditorHUD : MonoBehaviour
    {
        // ── Inspector — containers ─────────────────────────────────────────────

        [Header("Root")]
        [Tooltip("Root canvas group that shows/hides the entire HUD.")]
        [SerializeField] private CanvasGroup _hudRoot;

        [Header("Tool Palette Buttons")]
        [SerializeField] private Button _btnSelect;
        [SerializeField] private Button _btnPlace;
        [SerializeField] private Button _btnMove;
        [SerializeField] private Button _btnRotate;
        [SerializeField] private Button _btnScale;
        [SerializeField] private Button _btnDelete;
        [SerializeField] private Button _btnPath;
        [SerializeField] private Button _btnZone;
        [SerializeField] private Button _btnTrigger;
        [SerializeField] private Button _btnText;

        [Header("History Buttons")]
        [SerializeField] private Button _btnUndo;
        [SerializeField] private Button _btnRedo;

        [Header("Action Buttons")]
        [SerializeField] private Button _btnSave;
        [SerializeField] private Button _btnTest;
        [SerializeField] private Button _btnPublish;
        [SerializeField] private Button _btnExitEditor;

        [Header("Toggle Buttons")]
        [SerializeField] private Toggle _toggleGrid;
        [SerializeField] private Toggle _toggleSnap;

        [Header("Status")]
        [SerializeField] private Text _lblUnsavedChanges;
        [SerializeField] private Text _lblObjectCount;

        [Header("References")]
        [SerializeField] private UGCPlacementController _placement;
        [SerializeField] private UGCEditorUI            _editorUI;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            WireButtons();
            WireManagerEvents();
            SetHudVisible(false);
        }

        private void OnDestroy()
        {
            UnwireManagerEvents();
        }

        private void Update()
        {
            RefreshButtonStates();
            RefreshStatusLabels();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Shows or hides the editor HUD.</summary>
        public void SetHudVisible(bool visible)
        {
            if (_hudRoot == null) return;
            _hudRoot.alpha          = visible ? 1f : 0f;
            _hudRoot.interactable   = visible;
            _hudRoot.blocksRaycasts = visible;
        }

        // ── Private setup ──────────────────────────────────────────────────────

        private void WireButtons()
        {
            WireToolButton(_btnSelect,  EditorTool.Select);
            WireToolButton(_btnPlace,   EditorTool.Place);
            WireToolButton(_btnMove,    EditorTool.Move);
            WireToolButton(_btnRotate,  EditorTool.Rotate);
            WireToolButton(_btnScale,   EditorTool.Scale);
            WireToolButton(_btnDelete,  EditorTool.Delete);
            WireToolButton(_btnPath,    EditorTool.Path);
            WireToolButton(_btnZone,    EditorTool.Zone);
            WireToolButton(_btnTrigger, EditorTool.Trigger);
            WireToolButton(_btnText,    EditorTool.Text);

            _btnUndo?.onClick.AddListener(() => UGCEditorManager.Instance?.Undo());
            _btnRedo?.onClick.AddListener(() => UGCEditorManager.Instance?.Redo());

            _btnSave?.onClick.AddListener(() => UGCEditorManager.Instance?.SaveProject());
            _btnTest?.onClick.AddListener(OnTestClicked);
            _btnPublish?.onClick.AddListener(OnPublishClicked);
            _btnExitEditor?.onClick.AddListener(() => UGCEditorManager.Instance?.ExitEditorMode());

            _toggleGrid?.onValueChanged.AddListener(OnGridToggled);
            _toggleSnap?.onValueChanged.AddListener(OnSnapToggled);
        }

        private void WireToolButton(Button btn, EditorTool tool)
        {
            if (btn == null) return;
            btn.onClick.AddListener(() =>
            {
                if (_placement != null) _placement.ActiveTool = tool;
            });
        }

        private void WireManagerEvents()
        {
            if (UGCEditorManager.Instance == null) return;
            UGCEditorManager.Instance.OnEditorModeChanged += OnEditorModeChanged;
        }

        private void UnwireManagerEvents()
        {
            if (UGCEditorManager.Instance == null) return;
            UGCEditorManager.Instance.OnEditorModeChanged -= OnEditorModeChanged;
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void OnEditorModeChanged(bool active)
        {
            SetHudVisible(active);
        }

        private void OnGridToggled(bool on)
        {
            // Grid visualisation toggle — actual grid rendering handled by placement controller
            Debug.Log($"[UGCEditorHUD] Grid display: {on}");
        }

        private void OnSnapToggled(bool on)
        {
            if (_placement != null) _placement.SnapToGrid = on;
        }

        private void OnTestClicked()
        {
            var runner = FindFirstObjectByType<UGCTestRunner>();
            runner?.StartTest();
        }

        private void OnPublishClicked()
        {
            var mgr     = UGCEditorManager.Instance;
            var pubMgr  = UGCPublishManager.Instance;
            if (mgr?.CurrentProject == null || pubMgr == null) return;
            pubMgr.SubmitForReview(mgr.CurrentProject);
        }

        // ── Per-frame refresh ──────────────────────────────────────────────────

        private void RefreshButtonStates()
        {
            var mgr = UGCEditorManager.Instance;
            if (mgr == null) return;

            if (_btnUndo != null) _btnUndo.interactable = mgr.CanUndo;
            if (_btnRedo != null) _btnRedo.interactable = mgr.CanRedo;
        }

        private void RefreshStatusLabels()
        {
            var mgr = UGCEditorManager.Instance;
            if (mgr == null) return;

            if (_lblUnsavedChanges != null)
                _lblUnsavedChanges.text = mgr.HasUnsavedChanges ? "● Unsaved" : "✓ Saved";

            if (_lblObjectCount != null && mgr.CurrentProject != null)
            {
                var p = mgr.CurrentProject;
                _lblObjectCount.text = $"WP:{p.waypoints.Count}  TR:{p.triggers.Count}  ZN:{p.zones.Count}";
            }
        }
    }
}
