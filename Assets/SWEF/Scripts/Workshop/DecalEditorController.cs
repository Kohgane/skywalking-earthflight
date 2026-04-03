// DecalEditorController.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// MonoBehaviour that drives the decal-placement editor inside the Workshop UI.
    /// Supports adding, removing, moving, rotating, and scaling decals via UV
    /// projection, with layer-ordering and up to <see cref="MaxDecals"/> decals per
    /// aircraft build.
    /// </summary>
    public class DecalEditorController : MonoBehaviour
    {
        /// <summary>Maximum number of decals that can be placed on a single aircraft.</summary>
        public const int MaxDecals = 10;

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Snap Settings")]
        [Tooltip("Minimum position increment for UV drag snapping (0 = free movement).")]
        [SerializeField] private float _positionSnap = 0f;

        [Tooltip("Rotation snap angle in degrees (0 = free rotation).")]
        [SerializeField] private float _rotationSnap = 0f;

        // ── State ──────────────────────────────────────────────────────────────

        private List<DecalData> _decals = new List<DecalData>();
        private int _selectedIndex = -1;

        // ── Unity Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            var build = WorkshopManager.Instance?.ActiveBuild;
            if (build?.decals != null)
                _decals = new List<DecalData>(build.decals);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Currently selected decal index, or −1 if none selected.</summary>
        public int SelectedIndex => _selectedIndex;

        /// <summary>Read-only snapshot of the current decal list.</summary>
        public IReadOnlyList<DecalData> Decals => _decals;

        /// <summary>
        /// Adds a new decal to the aircraft.  No-ops with a warning if the
        /// <see cref="MaxDecals"/> limit has been reached.
        /// </summary>
        /// <param name="texturePath">Resource path for the decal texture.</param>
        /// <param name="uvPosition">Initial UV anchor position.</param>
        /// <returns>Index of the newly added decal, or −1 on failure.</returns>
        public int AddDecal(string texturePath, Vector2 uvPosition)
        {
            if (_decals.Count >= MaxDecals)
            {
                Debug.LogWarning($"[SWEF] Workshop: AddDecal — maximum of {MaxDecals} decals reached.");
                return -1;
            }
            if (string.IsNullOrEmpty(texturePath))
            {
                Debug.LogWarning("[SWEF] Workshop: AddDecal called with empty texturePath.");
                return -1;
            }

            var decal = new DecalData
            {
                texturePath = texturePath,
                uvPosition  = uvPosition,
                layerIndex  = _decals.Count
            };
            _decals.Add(decal);
            _selectedIndex = _decals.Count - 1;
            SyncToActiveBuild();
            WorkshopAnalytics.RecordDecalPlaced(texturePath);
            return _selectedIndex;
        }

        /// <summary>
        /// Removes the decal at the given index and reorders layer indices.
        /// </summary>
        /// <param name="index">Zero-based index of the decal to remove.</param>
        public void RemoveDecal(int index)
        {
            if (!IsValidIndex(index))
            {
                Debug.LogWarning($"[SWEF] Workshop: RemoveDecal — index {index} out of range.");
                return;
            }

            _decals.RemoveAt(index);
            RebuildLayerIndices();
            _selectedIndex = Mathf.Clamp(_selectedIndex, -1, _decals.Count - 1);
            SyncToActiveBuild();
        }

        /// <summary>
        /// Moves the selected decal to a new UV position.
        /// </summary>
        /// <param name="index">Index of the decal to move.</param>
        /// <param name="newUVPosition">New UV anchor position [0–1, 0–1].</param>
        public void MoveDecal(int index, Vector2 newUVPosition)
        {
            if (!IsValidIndex(index)) return;

            if (_positionSnap > 0f)
                newUVPosition = SnapVector2(newUVPosition, _positionSnap);

            _decals[index].uvPosition = newUVPosition;
            SyncToActiveBuild();
        }

        /// <summary>
        /// Rotates the specified decal to an absolute angle.
        /// </summary>
        /// <param name="index">Index of the decal to rotate.</param>
        /// <param name="degrees">Rotation in degrees (0–360).</param>
        public void RotateDecal(int index, float degrees)
        {
            if (!IsValidIndex(index)) return;

            if (_rotationSnap > 0f)
                degrees = Mathf.Round(degrees / _rotationSnap) * _rotationSnap;

            _decals[index].rotation = degrees % 360f;
            SyncToActiveBuild();
        }

        /// <summary>
        /// Sets the uniform scale of the specified decal.
        /// </summary>
        /// <param name="index">Index of the decal to scale.</param>
        /// <param name="scale">New scale value (must be &gt; 0).</param>
        public void ScaleDecal(int index, float scale)
        {
            if (!IsValidIndex(index)) return;

            if (scale <= 0f)
            {
                Debug.LogWarning("[SWEF] Workshop: ScaleDecal — scale must be greater than 0.");
                return;
            }

            _decals[index].scale = scale;
            SyncToActiveBuild();
        }

        /// <summary>
        /// Selects a decal by index for subsequent transform operations.
        /// Pass −1 to deselect all.
        /// </summary>
        /// <param name="index">Decal index to select.</param>
        public void SelectDecal(int index)
        {
            _selectedIndex = (index >= -1 && index < _decals.Count) ? index : -1;
        }

        /// <summary>
        /// Moves a decal to a different layer position, re-ordering the list.
        /// </summary>
        /// <param name="fromIndex">Current index of the decal.</param>
        /// <param name="toLayerIndex">Desired layer index.</param>
        public void SetDecalLayer(int fromIndex, int toLayerIndex)
        {
            if (!IsValidIndex(fromIndex)) return;

            toLayerIndex = Mathf.Clamp(toLayerIndex, 0, _decals.Count - 1);
            var decal = _decals[fromIndex];
            _decals.RemoveAt(fromIndex);
            _decals.Insert(toLayerIndex, decal);
            RebuildLayerIndices();
            SyncToActiveBuild();
        }

        // ── Private ────────────────────────────────────────────────────────────

        private bool IsValidIndex(int index)
        {
            if (index >= 0 && index < _decals.Count) return true;
            Debug.LogWarning($"[SWEF] Workshop: Decal index {index} is out of range (count={_decals.Count}).");
            return false;
        }

        private void RebuildLayerIndices()
        {
            for (int i = 0; i < _decals.Count; i++)
                _decals[i].layerIndex = i;
        }

        private void SyncToActiveBuild()
        {
            var build = WorkshopManager.Instance?.ActiveBuild;
            if (build == null) return;
            build.decals = new System.Collections.Generic.List<DecalData>(_decals);
        }

        private static Vector2 SnapVector2(Vector2 v, float snap)
        {
            return new Vector2(
                Mathf.Round(v.x / snap) * snap,
                Mathf.Round(v.y / snap) * snap);
        }
    }
}
