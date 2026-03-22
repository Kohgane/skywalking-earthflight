using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Interactive visual tool for creating and editing custom flight routes.
    /// Supports tap-to-place waypoints, drag repositioning, undo/redo, spline path
    /// estimation, terrain-collision warnings, landmark snapping, and route preview.
    /// </summary>
    public class RouteBuilderController : MonoBehaviour
    {
        #region Events

        /// <summary>Fired when a waypoint is added to the route under construction.</summary>
        public event Action<RouteWaypoint> OnWaypointAdded;

        /// <summary>Fired when a waypoint is removed; carries its former index.</summary>
        public event Action<int> OnWaypointRemoved;

        /// <summary>Fired when a waypoint is repositioned; carries the updated index.</summary>
        public event Action<int> OnWaypointMoved;

        /// <summary>Fired after <see cref="ValidateRoute"/> runs; carries validity flag.</summary>
        public event Action<bool> OnRouteValidated;

        #endregion

        #region Inspector

        [Header("Snapping")]
        [Tooltip("Maximum world-space metres to snap a placed waypoint onto a landmark.")]
        [SerializeField] private float _snapToLandmarkRadius = 200f;

        [Tooltip("Maximum world-space metres to snap onto a favourite location.")]
        [SerializeField] private float _snapToFavoriteRadius = 200f;

        [Header("Estimation")]
        [Tooltip("Assumed average speed in km/h used for time estimates.")]
        [SerializeField] private float _estimatedSpeedKmh = 300f;

        [Header("Validation")]
        [Tooltip("Minimum number of waypoints required for a valid route.")]
        [SerializeField] private int _minWaypoints = 2;

        #endregion

        #region Public State

        /// <summary>The route being actively built.</summary>
        public FlightRoute ActiveRoute { get; private set; }

        /// <summary>Whether there are undoable operations on the stack.</summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>Whether there are redoable operations on the stack.</summary>
        public bool CanRedo => _redoStack.Count > 0;

        #endregion

        #region Private State

        // Undo / Redo stacks – each entry is a deep-copy snapshot of the waypoint list
        private readonly Stack<List<RouteWaypoint>> _undoStack = new Stack<List<RouteWaypoint>>();
        private readonly Stack<List<RouteWaypoint>> _redoStack = new Stack<List<RouteWaypoint>>();

        private bool _isPreviewing;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ActiveRoute = new FlightRoute
            {
                routeId = Guid.NewGuid().ToString(),
                name    = "New Route"
            };
        }

        #endregion

        #region Public API — Waypoint Editing

        /// <summary>
        /// Appends a new waypoint at the end of the route.
        /// Snapping against landmarks and favourites is attempted first.
        /// </summary>
        /// <param name="lat">Latitude in decimal degrees.</param>
        /// <param name="lon">Longitude in decimal degrees.</param>
        /// <param name="alt">Altitude above sea level in metres.</param>
        /// <returns>The newly created waypoint.</returns>
        public RouteWaypoint AddWaypoint(double lat, double lon, float alt)
        {
            PushUndoSnapshot();

            var wp = CreateWaypoint(lat, lon, alt);
            wp.index = ActiveRoute.waypoints.Count;

            TrySnapToLandmark(wp);
            TrySnapToFavorite(wp);

            ActiveRoute.waypoints.Add(wp);
            RebuildIndices();
            RecalculateStats();

            OnWaypointAdded?.Invoke(wp);
            return wp;
        }

        /// <summary>
        /// Inserts a waypoint at the specified position in the route, shifting later waypoints down.
        /// </summary>
        /// <param name="index">Zero-based insertion position.</param>
        /// <param name="waypoint">Pre-configured waypoint to insert.</param>
        public void InsertWaypoint(int index, RouteWaypoint waypoint)
        {
            if (waypoint == null) return;
            PushUndoSnapshot();

            index = Mathf.Clamp(index, 0, ActiveRoute.waypoints.Count);
            ActiveRoute.waypoints.Insert(index, waypoint);
            RebuildIndices();
            RecalculateStats();

            OnWaypointAdded?.Invoke(waypoint);
        }

        /// <summary>Removes the waypoint at <paramref name="index"/>.</summary>
        public void RemoveWaypoint(int index)
        {
            if (!IsValidIndex(index)) return;
            PushUndoSnapshot();

            ActiveRoute.waypoints.RemoveAt(index);
            RebuildIndices();
            RecalculateStats();

            OnWaypointRemoved?.Invoke(index);
        }

        /// <summary>
        /// Repositions the waypoint at <paramref name="index"/> to new geographic coordinates.
        /// </summary>
        public void MoveWaypoint(int index, double lat, double lon)
        {
            if (!IsValidIndex(index)) return;
            PushUndoSnapshot();

            ActiveRoute.waypoints[index].latitude  = lat;
            ActiveRoute.waypoints[index].longitude = lon;
            RecalculateStats();

            OnWaypointMoved?.Invoke(index);
        }

        /// <summary>Changes the altitude of an existing waypoint.</summary>
        public void SetWaypointAltitude(int index, float altitude)
        {
            if (!IsValidIndex(index)) return;
            PushUndoSnapshot();
            ActiveRoute.waypoints[index].altitude = altitude;
            RecalculateStats();
            OnWaypointMoved?.Invoke(index);
        }

        /// <summary>Updates the type, name and description of an existing waypoint.</summary>
        public void EditWaypointProperties(int index, WaypointType type, string name, string description)
        {
            if (!IsValidIndex(index)) return;
            PushUndoSnapshot();
            ActiveRoute.waypoints[index].waypointType  = type;
            ActiveRoute.waypoints[index].name        = name ?? string.Empty;
            ActiveRoute.waypoints[index].description = description ?? string.Empty;
            OnWaypointMoved?.Invoke(index);
        }

        /// <summary>Moves a waypoint from <paramref name="fromIndex"/> to <paramref name="toIndex"/>.</summary>
        public void ReorderWaypoint(int fromIndex, int toIndex)
        {
            if (!IsValidIndex(fromIndex) || toIndex < 0 || toIndex >= ActiveRoute.waypoints.Count) return;
            PushUndoSnapshot();

            var wp = ActiveRoute.waypoints[fromIndex];
            ActiveRoute.waypoints.RemoveAt(fromIndex);
            ActiveRoute.waypoints.Insert(toIndex, wp);
            RebuildIndices();
            RecalculateStats();

            OnWaypointMoved?.Invoke(toIndex);
        }

        #endregion

        #region Public API — Undo / Redo

        /// <summary>Reverts the most recent editing operation.</summary>
        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            // Push current state to redo before restoring
            _redoStack.Push(DeepCopyWaypoints(ActiveRoute.waypoints));
            ActiveRoute.waypoints = _undoStack.Pop();
            RebuildIndices();
            RecalculateStats();
        }

        /// <summary>Re-applies the most recently undone operation.</summary>
        public void Redo()
        {
            if (_redoStack.Count == 0) return;

            _undoStack.Push(DeepCopyWaypoints(ActiveRoute.waypoints));
            ActiveRoute.waypoints = _redoStack.Pop();
            RebuildIndices();
            RecalculateStats();
        }

        #endregion

        #region Public API — Route Operations

        /// <summary>
        /// Initiates a preview flight of the route in fast-forward mode.
        /// Currently signals the renderer to animate the path.
        /// </summary>
        public void PreviewRoute()
        {
            if (_isPreviewing) return;
            _isPreviewing = true;
            RoutePathRenderer.Instance?.StartPreview(ActiveRoute);
        }

        /// <summary>Stops an active preview.</summary>
        public void StopPreview()
        {
            _isPreviewing = false;
            RoutePathRenderer.Instance?.StopPreview();
        }

        /// <summary>Recalculates distance, duration, and altitude stats for the current route.</summary>
        public void CalculatePathStats()
        {
            RecalculateStats();
        }

        /// <summary>
        /// Validates the route against all rules and returns a list of human-readable warnings.
        /// An empty list means the route is valid.
        /// </summary>
        /// <returns>List of warning strings; empty when no issues found.</returns>
        public List<string> ValidateRoute()
        {
            var warnings = new List<string>();

            if (ActiveRoute.waypoints.Count < _minWaypoints)
                warnings.Add($"Route needs at least {_minWaypoints} waypoints.");

            bool hasStart  = ActiveRoute.waypoints.Exists(w => w.waypointType == WaypointType.Start);
            bool hasFinish = ActiveRoute.waypoints.Exists(w => w.waypointType == WaypointType.Finish);

            if (!hasStart)
                warnings.Add("No Start waypoint defined. First waypoint will be used.");
            if (!hasFinish)
                warnings.Add("No Finish waypoint defined. Last waypoint will be used.");

            // Speed gate validation
            foreach (var wp in ActiveRoute.waypoints)
            {
                if (wp.waypointType == WaypointType.SpeedGate && wp.requiredSpeed < 0f)
                    warnings.Add($"Speed Gate '{wp.name}' has no required speed set.");
                if (wp.waypointType == WaypointType.Altitude && wp.requiredAltitude < 0f)
                    warnings.Add($"Altitude Checkpoint '{wp.name}' has no required altitude set.");
            }

            bool isValid = warnings.Count == 0;
            OnRouteValidated?.Invoke(isValid);
            return warnings;
        }

        /// <summary>
        /// Saves the route being built into the storage system and returns it.
        /// </summary>
        public FlightRoute FinalizeRoute()
        {
            ValidateRoute();
            RecalculateStats();

            if (ActiveRoute.waypoints.Count > 0)
            {
                var first = ActiveRoute.waypoints[0];
                var last  = ActiveRoute.waypoints[ActiveRoute.waypoints.Count - 1];

                ActiveRoute.startLatitude  = first.latitude;
                ActiveRoute.startLongitude = first.longitude;
                ActiveRoute.startAltitude  = first.altitude;
                ActiveRoute.endLatitude    = last.latitude;
                ActiveRoute.endLongitude   = last.longitude;
                ActiveRoute.endAltitude    = last.altitude;
                const double loopToleranceDeg = 0.001; // ~111 m
                ActiveRoute.isLoop = Math.Abs(first.latitude  - last.latitude)  < loopToleranceDeg
                                  && Math.Abs(first.longitude - last.longitude) < loopToleranceDeg;
            }

            RoutePlannerManager.Instance?.UpdateRoute(ActiveRoute);
            return ActiveRoute;
        }

        /// <summary>
        /// Attempts to snap the waypoint at <paramref name="index"/> to the nearest landmark
        /// within the snap radius.
        /// </summary>
        public bool TrySnapIndexToLandmark(int index)
        {
            if (!IsValidIndex(index)) return false;
            return TrySnapToLandmark(ActiveRoute.waypoints[index]);
        }

        /// <summary>
        /// Returns a list of automatically suggested waypoints for interesting landmarks
        /// along the approximate path of the current route.
        /// </summary>
        public List<RouteWaypoint> AutoSuggestWaypoints()
        {
            // Placeholder — in production this queries LandmarkDatabase for points
            // that lie within a configurable corridor around the current path segments.
            return new List<RouteWaypoint>();
        }

        #endregion

        #region Private Helpers

        private RouteWaypoint CreateWaypoint(double lat, double lon, float alt)
        {
            return new RouteWaypoint
            {
                waypointId = Guid.NewGuid().ToString(),
                latitude   = lat,
                longitude  = lon,
                altitude   = alt
            };
        }

        private bool TrySnapToLandmark(RouteWaypoint wp)
        {
            // Delegates to LandmarkDatabase when available (soft dependency)
            var dbType = Type.GetType("SWEF.Narration.LandmarkDatabase, Assembly-CSharp");
            if (dbType == null) return false;

            var instance = FindObjectOfType(dbType) as MonoBehaviour;
            if (instance == null) return false;

            var method = dbType.GetMethod("GetNearestLandmark");
            if (method == null) return false;

            var result = method.Invoke(instance, new object[] { wp.latitude, wp.longitude, (double)_snapToLandmarkRadius });
            if (result == null) return false;

            // Extract Id and name via reflection
            var idProp   = result.GetType().GetProperty("landmarkId") ?? result.GetType().GetField("landmarkId");
            var nameProp = result.GetType().GetProperty("displayName") ?? result.GetType().GetField("displayName");

            if (idProp != null)
            {
                wp.landmarkId    = idProp.GetValue(result)?.ToString();
                wp.waypointType  = WaypointType.Landmark;
                if (nameProp != null) wp.name = nameProp.GetValue(result)?.ToString() ?? wp.name;
                return true;
            }
            return false;
        }

        private bool TrySnapToFavorite(RouteWaypoint wp)
        {
            // Delegates to FavoritesManager when available
            var managerType = Type.GetType("SWEF.Favorites.FavoritesManager, Assembly-CSharp");
            if (managerType == null) return false;

            var instanceProp = managerType.GetProperty("Instance");
            if (instanceProp == null) return false;

            var instance = instanceProp.GetValue(null) as MonoBehaviour;
            if (instance == null) return false;

            // Further snapping would compare favourite locations against wp coordinates
            return false;
        }

        private void RebuildIndices()
        {
            for (int i = 0; i < ActiveRoute.waypoints.Count; i++)
                ActiveRoute.waypoints[i].index = i;
        }

        private void RecalculateStats()
        {
            float totalDist = 0f;
            float maxAlt    = float.MinValue;
            float minAlt    = float.MaxValue;

            var wps = ActiveRoute.waypoints;
            for (int i = 1; i < wps.Count; i++)
            {
                totalDist += (float)HaversineKm(
                    wps[i - 1].latitude, wps[i - 1].longitude,
                    wps[i].latitude,     wps[i].longitude);
            }
            foreach (var wp in wps)
            {
                if (wp.altitude > maxAlt) maxAlt = wp.altitude;
                if (wp.altitude < minAlt) minAlt = wp.altitude;
            }

            ActiveRoute.estimatedDistance = totalDist;
            ActiveRoute.estimatedDuration  = _estimatedSpeedKmh > 0f
                ? (totalDist / _estimatedSpeedKmh) * 60f : 0f;
            ActiveRoute.maxAltitude = wps.Count > 0 ? maxAlt : 0f;
            ActiveRoute.minAltitude = wps.Count > 0 ? minAlt : 0f;
        }

        private void PushUndoSnapshot()
        {
            _undoStack.Push(DeepCopyWaypoints(ActiveRoute.waypoints));
            _redoStack.Clear(); // A new edit invalidates the redo history
        }

        private static List<RouteWaypoint> DeepCopyWaypoints(List<RouteWaypoint> source)
        {
            // Serialise via JsonUtility for a simple, allocation-efficient deep copy
            var copy = new List<RouteWaypoint>(source.Count);
            foreach (var wp in source)
            {
                string json = JsonUtility.ToJson(wp);
                copy.Add(JsonUtility.FromJson<RouteWaypoint>(json));
            }
            return copy;
        }

        private bool IsValidIndex(int index) =>
            ActiveRoute != null && index >= 0 && index < ActiveRoute.waypoints.Count;

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        #endregion
    }
}
