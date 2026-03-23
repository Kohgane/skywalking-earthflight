// FormationManager.cs — SWEF Flight Formation & Wingman AI System (Phase 63)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Formation
{
    /// <summary>
    /// Singleton MonoBehaviour that orchestrates the active formation:
    /// it holds the current <see cref="FormationPattern"/>, maintains the
    /// list of active <see cref="WingmanAI"/> agents, and assigns slot
    /// world-positions to each wingman every frame.
    /// <para>
    /// The manager emits events whenever the formation type changes or
    /// the wingman roster is modified.
    /// </para>
    /// </summary>
    public sealed class FormationManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Single active instance of <see cref="FormationManager"/>.</summary>
        public static FormationManager Instance { get; private set; }

        #endregion

        #region Events

        /// <summary>Raised whenever the active <see cref="FormationType"/> changes.</summary>
        public event Action<FormationType> OnFormationChanged;

        /// <summary>Raised when a wingman is successfully added to the roster.</summary>
        public event Action<WingmanAI> OnWingmanAdded;

        /// <summary>Raised when a wingman is removed from the roster.</summary>
        public event Action<WingmanAI> OnWingmanRemoved;

        #endregion

        #region Inspector

        [Header("Formation")]
        [Tooltip("Active formation pattern asset.  If null a default V-Shape is generated.")]
        [SerializeField] private FormationPattern currentPattern;

        [Header("Leader")]
        [Tooltip("The player / leader transform that the formation centres on.")]
        [SerializeField] private Transform leaderTransform;

        [Header("Limits")]
        [Tooltip("Maximum number of wingmen permitted in the formation. " +
                 "Capped at FormationConfig.MaxWingmen (5).")]
        [SerializeField] private int maxWingmen = FormationConfig.MaxWingmen;

        #endregion

        #region Runtime State

        /// <summary>List of all active wingman agents in the current formation.</summary>
        public List<WingmanAI> wingmen { get; } = new List<WingmanAI>();

        /// <summary>Hard cap on the wingman roster.</summary>
        public int MaxWingmen => Mathf.Clamp(maxWingmen, 1, FormationConfig.MaxWingmen);

        /// <summary>The active <see cref="FormationPattern"/> asset.</summary>
        public FormationPattern CurrentPattern => currentPattern;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsurePattern();
        }

        private void Update()
        {
            if (currentPattern == null || leaderTransform == null) return;
            AssignSlotPositions();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Switches the entire group to the requested formation type.
        /// Rebuilds the active <see cref="FormationPattern"/> offsets and
        /// commands all wingmen back to their slots.
        /// </summary>
        /// <param name="type">The new formation shape.</param>
        public void SetFormation(FormationType type)
        {
            // Replace the current pattern with a fresh runtime instance pre-built
            // for the requested type and current wingman count.
            FormationPattern rt = ScriptableObject.CreateInstance<FormationPattern>();
            rt.RebuildFromConfig(type, Mathf.Max(1, wingmen.Count));
            currentPattern = rt;

            foreach (WingmanAI w in wingmen)
                w.CommandReturn();

            OnFormationChanged?.Invoke(type);
        }

        /// <summary>
        /// Adds a wingman to the formation roster and assigns it the next
        /// available slot index.
        /// </summary>
        /// <param name="wingman">The <see cref="WingmanAI"/> to add.</param>
        public void AddWingman(WingmanAI wingman)
        {
            if (wingman == null) return;
            if (wingmen.Contains(wingman)) return;
            if (wingmen.Count >= MaxWingmen) return;

            wingman.assignedSlot = wingmen.Count;
            wingmen.Add(wingman);

            OnWingmanAdded?.Invoke(wingman);
        }

        /// <summary>
        /// Removes a wingman from the roster and re-packs slot indices.
        /// </summary>
        /// <param name="wingman">The <see cref="WingmanAI"/> to remove.</param>
        public void RemoveWingman(WingmanAI wingman)
        {
            if (wingman == null) return;
            if (!wingmen.Remove(wingman)) return;

            // Re-pack slot indices.
            for (int i = 0; i < wingmen.Count; i++)
                wingmen[i].assignedSlot = i;

            OnWingmanRemoved?.Invoke(wingman);
        }

        /// <summary>
        /// Commands all wingmen to return to their assigned formation slots.
        /// </summary>
        public void RecallAll()
        {
            foreach (WingmanAI w in wingmen)
                w.CommandReturn();
        }

        /// <summary>
        /// Commands all wingmen to break formation and fly freely.
        /// </summary>
        public void BreakFormation()
        {
            foreach (WingmanAI w in wingmen)
                w.CommandBreak();
        }

        /// <summary>
        /// Assigns or reassigns the leader transform the formation follows.
        /// </summary>
        /// <param name="leader">New leader <see cref="Transform"/>.</param>
        public void SetLeader(Transform leader)
        {
            leaderTransform = leader;
            foreach (WingmanAI w in wingmen)
                w.leader = leader;
        }

        #endregion

        #region Private Helpers

        private void AssignSlotPositions()
        {
            for (int i = 0; i < wingmen.Count; i++)
            {
                WingmanAI w = wingmen[i];
                if (w == null) continue;

                Vector3 slotPos = currentPattern.GetSlotWorldPosition(
                    w.assignedSlot, leaderTransform);

                w.SetSlotPosition(slotPos);
            }
        }

        private void EnsurePattern()
        {
            if (currentPattern != null) return;

            currentPattern = ScriptableObject.CreateInstance<FormationPattern>();
            currentPattern.RebuildFromConfig(Mathf.Max(1, wingmen.Count));
        }

        #endregion
    }
}
