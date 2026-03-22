using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Tracks Route Planner and Custom Route Builder usage through the
    /// SWEF analytics pipeline (<c>SWEF.Analytics.UserBehaviorTracker</c>).
    /// All events follow the existing <c>TrackFeatureDiscovery / TrackButtonClick</c>
    /// pattern used across the project.
    /// </summary>
    public class RoutePlannerAnalytics : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static RoutePlannerAnalytics Instance { get; private set; }

        #endregion

        #region Event Name Constants

        private const string EventRouteCreated      = "route_created";
        private const string EventRouteStarted      = "route_started";
        private const string EventRouteCompleted    = "route_completed";
        private const string EventRouteAbandoned    = "route_abandoned";
        private const string EventWaypointReached   = "waypoint_reached";
        private const string EventOffPath           = "off_path";
        private const string EventRouteShared       = "route_shared";
        private const string EventRouteImported     = "route_imported";
        private const string EventRouteRated        = "route_rated";
        private const string EventBuilderOpened     = "route_builder_used";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Public Tracking Methods

        /// <summary>Tracks creation of a new route.</summary>
        /// <param name="route">The newly created route.</param>
        public void TrackRouteCreated(FlightRoute route)
        {
            if (route == null) return;
            LogEvent(EventRouteCreated, new Dictionary<string, object>
            {
                { "route_id",       route.routeId },
                { "route_type",     route.routeType.ToString() },
                { "waypoint_count", route.waypoints.Count }
            });
        }

        /// <summary>Tracks when a player begins navigating a route.</summary>
        /// <param name="route">The route that was started.</param>
        public void TrackRouteStarted(FlightRoute route)
        {
            if (route == null) return;
            LogEvent(EventRouteStarted, new Dictionary<string, object>
            {
                { "route_id",          route.routeId },
                { "route_type",        route.routeType.ToString() },
                { "waypoint_count",    route.waypoints.Count },
                { "distance_km",       route.estimatedDistance },
                { "navigation_style",  route.navigationStyle.ToString() }
            });
        }

        /// <summary>Tracks successful route completion with final stats.</summary>
        /// <param name="route">The completed route.</param>
        /// <param name="progress">Progress data including elapsed time and deviations.</param>
        public void TrackRouteCompleted(FlightRoute route, RouteProgress progress)
        {
            if (route == null || progress == null) return;

            float completionPct = route.waypoints.Count > 0
                ? (float)progress.waypointsReached.Count / route.waypoints.Count
                : 0f;

            LogEvent(EventRouteCompleted, new Dictionary<string, object>
            {
                { "route_id",        route.routeId },
                { "route_type",      route.routeType.ToString() },
                { "duration_sec",    progress.elapsedTime },
                { "distance_km",     progress.distanceTraveled },
                { "completion_pct",  completionPct },
                { "deviation_count", progress.deviations }
            });
        }

        /// <summary>Tracks when a route navigation session is abandoned.</summary>
        /// <param name="route">The route that was abandoned.</param>
        /// <param name="progress">Progress at the time of abandonment.</param>
        public void TrackRouteAbandoned(FlightRoute route, RouteProgress progress)
        {
            if (route == null) return;

            float completionPct = (route.waypoints.Count > 0 && progress != null)
                ? (float)progress.waypointsReached.Count / route.waypoints.Count
                : 0f;

            LogEvent(EventRouteAbandoned, new Dictionary<string, object>
            {
                { "route_id",       route.routeId },
                { "route_type",     route.routeType.ToString() },
                { "completion_pct", completionPct }
            });
        }

        /// <summary>Tracks individual waypoint arrivals during navigation.</summary>
        /// <param name="route">Active route.</param>
        /// <param name="waypoint">The waypoint that was reached.</param>
        /// <param name="index">Zero-based index of the reached waypoint.</param>
        public void TrackWaypointReached(FlightRoute route, RouteWaypoint waypoint, int index)
        {
            if (route == null || waypoint == null) return;
            LogEvent(EventWaypointReached, new Dictionary<string, object>
            {
                { "route_id",       route.routeId },
                { "waypoint_index", index },
                { "waypoint_type",  waypoint.waypointType.ToString() }
            });
        }

        /// <summary>Tracks when the player deviates beyond the off-path threshold.</summary>
        /// <param name="route">Active route.</param>
        public void TrackOffPath(FlightRoute route)
        {
            if (route == null) return;
            LogEvent(EventOffPath, new Dictionary<string, object>
            {
                { "route_id",   route.routeId },
                { "route_type", route.routeType.ToString() }
            });
        }

        /// <summary>Tracks when a route is shared with others.</summary>
        /// <param name="route">The shared route.</param>
        public void TrackRouteShared(FlightRoute route)
        {
            if (route == null) return;
            LogEvent(EventRouteShared, new Dictionary<string, object>
            {
                { "route_id",   route.routeId },
                { "route_type", route.routeType.ToString() }
            });
        }

        /// <summary>Tracks when a route file is imported from an external source.</summary>
        /// <param name="route">The imported route.</param>
        public void TrackRouteImported(FlightRoute route)
        {
            if (route == null) return;
            LogEvent(EventRouteImported, new Dictionary<string, object>
            {
                { "route_id",       route.routeId },
                { "waypoint_count", route.waypoints.Count }
            });
        }

        /// <summary>Tracks a route rating submission.</summary>
        /// <param name="routeId">Id of the rated route.</param>
        /// <param name="rating">Rating value (0–5).</param>
        public void TrackRouteRated(string routeId, float rating)
        {
            LogEvent(EventRouteRated, new Dictionary<string, object>
            {
                { "route_id", routeId },
                { "rating",   rating  }
            });
        }

        /// <summary>Tracks when the Route Builder tool is opened.</summary>
        public void TrackBuilderOpened()
        {
            LogEvent(EventBuilderOpened, new Dictionary<string, object>
            {
                { "timestamp", System.DateTime.UtcNow.ToString("o") }
            });
        }

        #endregion

        #region Private — Analytics Bridge

        /// <summary>
        /// Forwards events to <c>SWEF.Analytics.UserBehaviorTracker</c> via reflection.
        /// Falls back to <c>Debug.Log</c> when the analytics system is unavailable.
        /// </summary>
        private void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            // Try UserBehaviorTracker.TrackFeatureDiscovery(string) as a lightweight bridge
            var trackerType = System.Type.GetType("SWEF.Analytics.UserBehaviorTracker, Assembly-CSharp");
            if (trackerType != null)
            {
                var instanceProp = trackerType.GetProperty("Instance");
                var instance     = instanceProp?.GetValue(null) as MonoBehaviour;
                if (instance != null)
                {
                    var method = trackerType.GetMethod("TrackFeatureDiscovery",
                        new[] { typeof(string) });
                    method?.Invoke(instance, new object[] { eventName });
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var sb = new System.Text.StringBuilder();
            sb.Append($"[SWEF Analytics] {eventName}");
            if (parameters != null && parameters.Count > 0)
            {
                sb.Append(" {");
                foreach (var kv in parameters)
                    sb.Append($" {kv.Key}={kv.Value},");
                sb.Append(" }");
            }
            Debug.Log(sb.ToString());
#endif
        }

        #endregion
    }
}
