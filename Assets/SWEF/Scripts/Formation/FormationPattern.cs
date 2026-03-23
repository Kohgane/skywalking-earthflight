// FormationPattern.cs — SWEF Flight Formation & Wingman AI System (Phase 63)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Formation
{
    /// <summary>
    /// Identifies every supported formation shape.
    /// </summary>
    public enum FormationType
    {
        /// <summary>Classic V-shape with the leader at the apex.</summary>
        VShape,

        /// <summary>Staircase extending to the leader's left.</summary>
        EchelonLeft,

        /// <summary>Staircase extending to the leader's right.</summary>
        EchelonRight,

        /// <summary>Four-point diamond with one aircraft above and one below.</summary>
        Diamond,

        /// <summary>Single-file line directly behind the leader.</summary>
        Trail,

        /// <summary>All wingmen abreast (side-by-side) of the leader.</summary>
        LineAbreast,

        /// <summary>
        /// Asymmetric four-finger spread inspired by WWII fighter tactics.
        /// </summary>
        FingerFour,
    }

    /// <summary>
    /// ScriptableObject that describes one formation pattern: its type, per-slot
    /// local-space offsets, spacing, and optional dynamic-adjustment flag.
    /// <para>
    /// Create assets via <em>Assets → Create → SWEF → Formation → Formation Pattern</em>.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "SWEF/Formation/Formation Pattern",
        fileName = "FP_New",
        order    = 0)]
    public sealed class FormationPattern : ScriptableObject
    {
        #region Inspector

        [Header("Identity")]
        [Tooltip("Formation type this pattern represents.")]
        [SerializeField] private FormationType formationType = FormationType.VShape;

        [Header("Geometry")]
        [Tooltip("Local-space offsets (relative to the leader) for each wingman slot. " +
                 "Index 0 = slot 1, index 1 = slot 2, etc.")]
        [SerializeField] private List<Vector3> slotOffsets = new List<Vector3>();

        [Tooltip("Base spacing between slots in metres. Slot offsets are scaled by this value " +
                 "relative to FormationConfig.DefaultSpacing.")]
        [SerializeField] private float spacing = FormationConfig.DefaultSpacing;

        [Header("Behaviour")]
        [Tooltip("When true the formation compresses / expands dynamically based on " +
                 "the group's current speed and surrounding terrain clearance.")]
        [SerializeField] private bool isDynamic = false;

        #endregion

        #region Public Properties

        /// <summary>Formation type this asset describes.</summary>
        public FormationType FormationType => formationType;

        /// <summary>Read-only view of the per-slot local-space offset list.</summary>
        public IReadOnlyList<Vector3> SlotOffsets => slotOffsets;

        /// <summary>Base spacing in metres used when building slot positions.</summary>
        public float Spacing => spacing;

        /// <summary>
        /// <see langword="true"/> if the pattern may adjust slot distances at
        /// runtime (speed / terrain).
        /// </summary>
        public bool IsDynamic => isDynamic;

        #endregion

        #region Public API

        /// <summary>
        /// Returns the world-space position of the requested slot given the
        /// current leader transform.
        /// </summary>
        /// <param name="slotIndex">Zero-based slot index.</param>
        /// <param name="leaderTransform">The leader aircraft's <see cref="Transform"/>.</param>
        /// <returns>
        /// World position for <paramref name="slotIndex"/>, or the leader's
        /// position when the index is out of range.
        /// </returns>
        public Vector3 GetSlotWorldPosition(int slotIndex, Transform leaderTransform)
        {
            if (leaderTransform == null)
                return Vector3.zero;

            if (slotIndex < 0 || slotIndex >= slotOffsets.Count)
                return leaderTransform.position;

            float scale = spacing / Mathf.Max(FormationConfig.DefaultSpacing, 0.001f);
            Vector3 scaledOffset = slotOffsets[slotIndex] * scale;
            return leaderTransform.TransformPoint(scaledOffset);
        }

        /// <summary>
        /// Rebuilds <see cref="SlotOffsets"/> from the preset tables in
        /// <see cref="FormationConfig"/> for the given <see cref="FormationType"/>
        /// and wingman count.
        /// </summary>
        /// <param name="type">Formation type to generate offsets for.</param>
        /// <param name="wingmanCount">
        /// Number of wingmen to generate offsets for (clamped to
        /// [1, <see cref="FormationConfig.MaxWingmen"/>]).
        /// </param>
        public void RebuildFromConfig(FormationType type, int wingmanCount)
        {
            formationType = type;
            slotOffsets.Clear();
            Vector3[] offsets = FormationConfig.GetOffsets(type, wingmanCount);
            slotOffsets.AddRange(offsets);
        }

        /// <summary>
        /// Rebuilds <see cref="SlotOffsets"/> from the preset tables in
        /// <see cref="FormationConfig"/> for the current <see cref="FormationType"/>
        /// and the given wingman count.
        /// </summary>
        /// <param name="wingmanCount">
        /// Number of wingmen to generate offsets for (clamped to
        /// [1, <see cref="FormationConfig.MaxWingmen"/>]).
        /// </param>
        public void RebuildFromConfig(int wingmanCount)
        {
            RebuildFromConfig(formationType, wingmanCount);
        }

        #endregion

        #region Unity Lifecycle

        private void OnValidate()
        {
            spacing = Mathf.Max(1f, spacing);
        }

        #endregion
    }
}
