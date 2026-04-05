// UGCPlacementController.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — MonoBehaviour that handles tap/click-to-place of waypoints, triggers,
    /// and zones onto the terrain in the UGC editor.
    ///
    /// <para>Features: ghost preview object, snap-to-grid, altitude mode selection,
    /// drag-to-move, and multi-select for batch operations.</para>
    /// </summary>
    public sealed class UGCPlacementController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Placement")]
        [Tooltip("Layer mask used for terrain raycasts.")]
        [SerializeField] private LayerMask _terrainLayerMask = ~0;

        [Tooltip("Camera used for screen-to-world raycasts.")]
        [SerializeField] private Camera _editorCamera;

        [Header("Grid Snap")]
        [Tooltip("Snap placed objects to a grid of this size in metres. Set to 0 to disable.")]
        [SerializeField] private float _snapGridSize = 0f;

        [Header("Altitude")]
        [Tooltip("Default altitude mode for newly placed objects.")]
        [SerializeField] private AltitudeMode _altitudeMode = AltitudeMode.GroundLevel;

        [Tooltip("Fixed altitude in metres (used when AltitudeMode is FixedAltitude).")]
        [SerializeField] private float _fixedAltitude = 500f;

        [Tooltip("Offset above ground in metres (used when AltitudeMode is RelativeToGround).")]
        [SerializeField] private float _groundOffset = 10f;

        [Header("Ghost Preview")]
        [Tooltip("Prefab shown as a semi-transparent preview before placement is confirmed.")]
        [SerializeField] private GameObject _ghostPrefab;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a waypoint is placed. Argument is the new waypoint.</summary>
        public event Action<UGCWaypoint> OnWaypointPlaced;

        /// <summary>Raised when a trigger is placed. Argument is the new trigger.</summary>
        public event Action<UGCTrigger> OnTriggerPlaced;

        /// <summary>Raised when a zone is placed. Argument is the new zone.</summary>
        public event Action<UGCZone> OnZonePlaced;

        /// <summary>Raised when the selected objects change.</summary>
        public event Action<IReadOnlyList<string>> OnSelectionChanged;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>Currently active placement tool.</summary>
        public EditorTool ActiveTool { get; set; } = EditorTool.Select;

        /// <summary>Whether snap-to-grid is enabled.</summary>
        public bool SnapToGrid
        {
            get => _snapGridSize > 0f;
            set => _snapGridSize = value ? Mathf.Max(1f, _snapGridSize) : 0f;
        }

        /// <summary>Current altitude resolution mode.</summary>
        public AltitudeMode CurrentAltitudeMode
        {
            get => _altitudeMode;
            set => _altitudeMode = value;
        }

        /// <summary>Read-only list of selected object IDs.</summary>
        public IReadOnlyList<string> SelectedIds => _selectedIds;

        // ── Internal state ─────────────────────────────────────────────────────

        private readonly List<string> _selectedIds = new List<string>();
        private GameObject _ghostInstance;
        private bool _isDragging;
        private string _draggingId;
        private Vector3 _dragStartWorld;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (_editorCamera == null)
                _editorCamera = Camera.main;
        }

        private void Update()
        {
            if (!IsEditorActive()) return;
            UpdateGhostPreview();
            HandleInput();
        }

        private void OnDestroy()
        {
            DestroyGhost();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public void ClearSelection()
        {
            _selectedIds.Clear();
            OnSelectionChanged?.Invoke(_selectedIds);
        }

        /// <summary>
        /// Adds or removes <paramref name="id"/> from the selection set.
        /// </summary>
        public void ToggleSelection(string id)
        {
            if (_selectedIds.Contains(id))
                _selectedIds.Remove(id);
            else
                _selectedIds.Add(id);
            OnSelectionChanged?.Invoke(_selectedIds);
        }

        /// <summary>
        /// Sets snap grid size in metres. Pass ≤0 to disable snapping.
        /// </summary>
        public void SetSnapGridSize(float sizeMetres)
        {
            _snapGridSize = Mathf.Max(0f, sizeMetres);
        }

        /// <summary>
        /// Sets the fixed altitude used when <see cref="AltitudeMode.FixedAltitude"/> is active.
        /// </summary>
        public void SetFixedAltitude(float altitudeMetres)
        {
            _fixedAltitude = altitudeMetres;
        }

        /// <summary>
        /// Sets the above-ground offset used when <see cref="AltitudeMode.RelativeToGround"/> is active.
        /// </summary>
        public void SetGroundOffset(float offsetMetres)
        {
            _groundOffset = offsetMetres;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private bool IsEditorActive()
        {
            return UGCEditorManager.Instance != null && UGCEditorManager.Instance.IsEditorModeActive;
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
                HandlePrimaryClick();

            if (Input.GetMouseButtonUp(0) && _isDragging)
                EndDrag();
        }

        private void HandlePrimaryClick()
        {
            if (!Raycast(out Vector3 hitPoint)) return;
            hitPoint = ApplyAltitudeMode(hitPoint);
            if (_snapGridSize > 0f) hitPoint = SnapToGridPoint(hitPoint);

            switch (ActiveTool)
            {
                case EditorTool.Place:
                    PlaceCurrentObject(hitPoint);
                    break;
                case EditorTool.Select:
                    // Selection handled by individual object colliders
                    break;
            }
        }

        private void PlaceCurrentObject(Vector3 worldPoint)
        {
            var manager = UGCEditorManager.Instance;
            if (manager?.CurrentProject == null) return;

            // NOTE: In a real Cesium integration, worldPoint would be converted to
            // geographic coordinates via CesiumGeoreference.TransformUnityPositionToEarthCenteredEarthFixed.
            // Here we store world-space y as altitude and x/z as lat/lon placeholders.
            var waypoint = new UGCWaypoint
            {
                waypointId = Guid.NewGuid().ToString(),
                latitude   = worldPoint.x,   // placeholder; replace with geo conversion in production
                longitude  = worldPoint.z,   // placeholder; replace with geo conversion in production
                altitude   = worldPoint.y,
                order      = manager.CurrentProject.waypoints.Count,
                isRequired = true,
            };

            var cmd = new AddWaypointCommand(manager.CurrentProject, waypoint);
            manager.ExecuteCommand(cmd);
            OnWaypointPlaced?.Invoke(waypoint);
        }

        private void EndDrag()
        {
            _isDragging  = false;
            _draggingId  = null;
        }

        private void UpdateGhostPreview()
        {
            if (ActiveTool != EditorTool.Place) { DestroyGhost(); return; }
            if (_ghostPrefab == null) return;

            if (_ghostInstance == null)
                _ghostInstance = Instantiate(_ghostPrefab);

            if (Raycast(out Vector3 hitPoint))
            {
                hitPoint = ApplyAltitudeMode(hitPoint);
                if (_snapGridSize > 0f) hitPoint = SnapToGridPoint(hitPoint);
                _ghostInstance.transform.position = hitPoint;
                _ghostInstance.SetActive(true);
            }
            else
            {
                _ghostInstance.SetActive(false);
            }
        }

        private bool Raycast(out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            if (_editorCamera == null) return false;
            Ray ray = _editorCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _terrainLayerMask))
            {
                hitPoint = hit.point;
                return true;
            }
            return false;
        }

        private Vector3 ApplyAltitudeMode(Vector3 point)
        {
            switch (_altitudeMode)
            {
                case AltitudeMode.FixedAltitude:
                    point.y = _fixedAltitude;
                    break;
                case AltitudeMode.RelativeToGround:
                    point.y += _groundOffset;
                    break;
                case AltitudeMode.GroundLevel:
                default:
                    break;
            }
            return point;
        }

        private Vector3 SnapToGridPoint(Vector3 point)
        {
            float s = _snapGridSize;
            return new Vector3(
                Mathf.Round(point.x / s) * s,
                point.y,
                Mathf.Round(point.z / s) * s);
        }

        private void DestroyGhost()
        {
            if (_ghostInstance != null)
            {
                Destroy(_ghostInstance);
                _ghostInstance = null;
            }
        }
    }
}
