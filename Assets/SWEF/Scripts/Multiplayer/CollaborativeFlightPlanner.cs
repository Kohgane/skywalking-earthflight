using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>Editing role a player holds in a collaborative flight plan.</summary>
    public enum PlannerRole
    {
        /// <summary>Can add, remove, and reorder waypoints.</summary>
        Planner,
        /// <summary>Read-only access to the shared plan.</summary>
        Follower
    }

    /// <summary>
    /// MonoBehaviour that enables multi-player collaborative flight plan creation.
    /// Integrates with the Phase 87 Navigation system (<c>SWEF.Navigation</c>) and
    /// supports real-time waypoint editing with role-based access control.
    /// </summary>
    public class CollaborativeFlightPlanner : MonoBehaviour
    {
        #region Events
        /// <summary>Fired whenever the shared plan is modified.</summary>
        public event Action<List<SharedWaypointData>> OnPlanUpdated;
        /// <summary>Fired when a new waypoint is added to the plan.</summary>
        public event Action<SharedWaypointData> OnWaypointAdded;
        /// <summary>Fired when the local player's role changes.</summary>
        public event Action<PlannerRole> OnRoleChanged;
        #endregion

        #region Inspector
        [Header("Planner Settings")]
        [SerializeField, Tooltip("Maximum waypoints allowed in a collaborative plan.")]
        private int maxWaypoints = 50;
        #endregion

        #region Public Properties
        /// <summary>The current editing role of the local player.</summary>
        public PlannerRole LocalRole { get; private set; } = PlannerRole.Planner;

        /// <summary>Read-only ordered list of waypoints in the shared plan.</summary>
        public IReadOnlyList<SharedWaypointData> Waypoints => _waypoints.AsReadOnly();
        #endregion

        #region Private State
        private readonly List<SharedWaypointData> _waypoints = new List<SharedWaypointData>();
        #endregion

        #region Role Management
        /// <summary>
        /// Sets the local player's role in this planning session.
        /// </summary>
        /// <param name="role">New role to assign.</param>
        public void SetLocalRole(PlannerRole role)
        {
            if (LocalRole == role) return;
            LocalRole = role;
            OnRoleChanged?.Invoke(role);
            Debug.Log($"[SWEF] Multiplayer: Planner role changed to {role}");
        }
        #endregion

        #region Waypoint Editing
        /// <summary>
        /// Adds a waypoint to the end of the plan. Requires <see cref="PlannerRole.Planner"/> role.
        /// </summary>
        /// <param name="waypoint">Waypoint to append.</param>
        public void AddWaypoint(SharedWaypointData waypoint)
        {
            if (LocalRole != PlannerRole.Planner)
            {
                Debug.LogWarning("[SWEF] Multiplayer: AddWaypoint — local player is a Follower (read-only).");
                return;
            }
            if (waypoint == null)
            {
                Debug.LogWarning("[SWEF] Multiplayer: AddWaypoint called with null waypoint.");
                return;
            }
            if (_waypoints.Count >= maxWaypoints)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: AddWaypoint — plan is full ({maxWaypoints} waypoints).");
                return;
            }

            _waypoints.Add(waypoint);

            MultiplayerAnalytics.RecordFlightPlanWaypointAdded();
            OnWaypointAdded?.Invoke(waypoint);
            OnPlanUpdated?.Invoke(new List<SharedWaypointData>(_waypoints));
        }

        /// <summary>
        /// Removes a waypoint from the plan by its ID. Requires <see cref="PlannerRole.Planner"/> role.
        /// </summary>
        /// <param name="waypointId">ID of the waypoint to remove.</param>
        public void RemoveWaypoint(string waypointId)
        {
            if (LocalRole != PlannerRole.Planner)
            {
                Debug.LogWarning("[SWEF] Multiplayer: RemoveWaypoint — local player is a Follower (read-only).");
                return;
            }
            int removed = _waypoints.RemoveAll(w => w.waypointId == waypointId);
            if (removed > 0)
                OnPlanUpdated?.Invoke(new List<SharedWaypointData>(_waypoints));
        }

        /// <summary>
        /// Reorders a waypoint from <paramref name="fromIndex"/> to <paramref name="toIndex"/>.
        /// Requires <see cref="PlannerRole.Planner"/> role.
        /// </summary>
        /// <param name="fromIndex">Current index of the waypoint.</param>
        /// <param name="toIndex">Target index.</param>
        public void ReorderWaypoint(int fromIndex, int toIndex)
        {
            if (LocalRole != PlannerRole.Planner)
            {
                Debug.LogWarning("[SWEF] Multiplayer: ReorderWaypoint — local player is a Follower (read-only).");
                return;
            }
            if (fromIndex < 0 || fromIndex >= _waypoints.Count ||
                toIndex < 0 || toIndex >= _waypoints.Count)
            {
                Debug.LogWarning("[SWEF] Multiplayer: ReorderWaypoint — index out of range.");
                return;
            }

            var wp = _waypoints[fromIndex];
            _waypoints.RemoveAt(fromIndex);
            _waypoints.Insert(toIndex, wp);
            OnPlanUpdated?.Invoke(new List<SharedWaypointData>(_waypoints));
        }

        /// <summary>
        /// Clears all waypoints from the plan. Requires <see cref="PlannerRole.Planner"/> role.
        /// </summary>
        public void ClearPlan()
        {
            if (LocalRole != PlannerRole.Planner)
            {
                Debug.LogWarning("[SWEF] Multiplayer: ClearPlan — local player is a Follower (read-only).");
                return;
            }
            _waypoints.Clear();
            OnPlanUpdated?.Invoke(new List<SharedWaypointData>(_waypoints));
        }
        #endregion

        #region Export & Navigation Integration
        /// <summary>
        /// Exports the current plan as a JSON string suitable for sharing.
        /// </summary>
        /// <returns>JSON representation of the shared plan, or null on failure.</returns>
        public string ExportPlanAsJson()
        {
            try
            {
                var wrapper = new PlanWrapper { waypoints = _waypoints };
                string json = JsonUtility.ToJson(wrapper, true);
                MultiplayerBridge.OnFlightPlanShared();
                MultiplayerAnalytics.RecordFlightPlanShared();
                return json;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: ExportPlanAsJson failed — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Imports a previously exported JSON plan, replacing the current plan.
        /// Requires <see cref="PlannerRole.Planner"/> role.
        /// </summary>
        /// <param name="json">JSON string produced by <see cref="ExportPlanAsJson"/>.</param>
        public void ImportPlanFromJson(string json)
        {
            if (LocalRole != PlannerRole.Planner)
            {
                Debug.LogWarning("[SWEF] Multiplayer: ImportPlanFromJson — local player is a Follower (read-only).");
                return;
            }
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[SWEF] Multiplayer: ImportPlanFromJson called with null/empty JSON.");
                return;
            }
            try
            {
                var wrapper = JsonUtility.FromJson<PlanWrapper>(json);
                if (wrapper?.waypoints == null) return;
                _waypoints.Clear();
                _waypoints.AddRange(wrapper.waypoints);
                OnPlanUpdated?.Invoke(new List<SharedWaypointData>(_waypoints));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: ImportPlanFromJson failed — {ex.Message}");
            }
        }

        /// <summary>
        /// Sends the current waypoints to the Phase 87 Navigation system (if available).
        /// </summary>
        public void SendToNavigationSystem()
        {
#if SWEF_NAVIGATION_AVAILABLE
            if (SWEF.Navigation.FlightPlanManager.Instance == null)
            {
                Debug.LogWarning("[SWEF] Multiplayer: FlightPlanManager not available.");
                return;
            }
            // Hand waypoints off to the Navigation flight plan manager.
            foreach (var wp in _waypoints)
            {
                SWEF.Navigation.FlightPlanManager.Instance.AddWaypointFromMultiplayer(
                    wp.waypointId, wp.name, wp.latitude, wp.longitude, wp.altitude);
            }
#endif
        }

        [Serializable]
        private class PlanWrapper
        {
            public List<SharedWaypointData> waypoints;
        }
        #endregion
    }
}
