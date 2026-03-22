using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Central singleton that manages the full lifecycle of route planning
    /// and in-flight navigation: creating routes, tracking player progress waypoint-by-
    /// waypoint, detecting off-path deviations, computing ETAs, and surfacing route
    /// suggestions.
    /// </summary>
    public class RoutePlannerManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static RoutePlannerManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Navigation")]
        [Tooltip("Seconds between waypoint proximity checks.")]
        [SerializeField] private float _checkInterval = 0.2f;

        [Tooltip("Seconds between off-path distance checks.")]
        [SerializeField] private float _offPathCheckInterval = 1f;

        [Header("Config")]
        [SerializeField] private RoutePlannerConfig _config = new RoutePlannerConfig();

        #endregion

        #region Events

        /// <summary>Fired when a new route is created via <see cref="CreateRoute"/>.</summary>
        public event Action<FlightRoute> OnRouteCreated;

        /// <summary>Fired when navigation starts on a route.</summary>
        public event Action<FlightRoute> OnNavigationStarted;

        /// <summary>Fired each time a waypoint is triggered; carries the waypoint and its index.</summary>
        public event Action<RouteWaypoint, int> OnWaypointReached;

        /// <summary>Fired when all required waypoints have been reached.</summary>
        public event Action<FlightRoute, RouteProgress> OnRouteCompleted;

        /// <summary>Fired when the player exceeds the off-path threshold.</summary>
        public event Action OnOffPath;

        /// <summary>Fired when the player returns within the off-path threshold after a deviation.</summary>
        public event Action OnBackOnPath;

        /// <summary>Fired when navigation is paused.</summary>
        public event Action OnNavigationPaused;

        /// <summary>Fired when navigation is resumed after a pause.</summary>
        public event Action OnNavigationResumed;

        #endregion

        #region Public Properties

        /// <summary>The route currently loaded for navigation, or <c>null</c>.</summary>
        public FlightRoute ActiveRoute { get; private set; }

        /// <summary>Live progress data for the active navigation session.</summary>
        public RouteProgress RouteProgress { get; private set; }

        /// <summary>The next waypoint the player must reach, or <c>null</c> when none remain.</summary>
        public RouteWaypoint NextWaypoint
        {
            get
            {
                if (ActiveRoute == null || RouteProgress == null) return null;
                int idx = RouteProgress.currentWaypointIndex;
                return idx < ActiveRoute.waypoints.Count ? ActiveRoute.waypoints[idx] : null;
            }
        }

        /// <summary>Straight-line distance to the next waypoint in metres.</summary>
        public float DistanceToNext { get; private set; }

        /// <summary>Estimated seconds until the next waypoint is reached at current speed.</summary>
        public float ETA { get; private set; }

        /// <summary><c>true</c> while a navigation session is active and not paused.</summary>
        public bool IsNavigating => _isNavigating && !_isPaused;

        /// <summary>Exposes the current configuration.</summary>
        public RoutePlannerConfig Config => _config;

        #endregion

        #region Private State

        private bool _isNavigating;
        private bool _isPaused;
        private bool _isOffPath;
        private Coroutine _navCoroutine;
        private Coroutine _offPathCoroutine;

        // All in-memory routes (loaded from storage on Awake)
        private readonly List<FlightRoute> _allRoutes = new List<FlightRoute>();

        // Lazy reference helpers — resolved at runtime to avoid hard compile dependencies
        private Component _flightController;
        private Component _waypointNavigator;
        private Component _minimapManager;
        private Component _narrationManager;

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
        }

        private void Start()
        {
            ResolveReferences();
            LoadRoutesFromStorage();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        #endregion

        #region Public API — Route CRUD

        /// <summary>
        /// Creates a new empty route with the supplied name and adds it to the in-memory library.
        /// </summary>
        /// <param name="name">Display name for the new route.</param>
        /// <returns>The newly created <see cref="FlightRoute"/>.</returns>
        public FlightRoute CreateRoute(string name)
        {
            var route = new FlightRoute
            {
                name            = string.IsNullOrEmpty(name) ? "New Route" : name,
                routeId         = Guid.NewGuid().ToString(),
                createdAt       = DateTime.UtcNow.ToString("o"),
                updatedAt       = DateTime.UtcNow.ToString("o"),
                navigationStyle = _config.navigationStyle
            };

            _allRoutes.Add(route);
            OnRouteCreated?.Invoke(route);

            if (_config.autoSaveDrafts)
                RouteStorageManager.Instance?.SaveRoute(route);

            return route;
        }

        /// <summary>Updates an existing route's metadata and persists the change.</summary>
        /// <param name="updated">The modified route object.</param>
        public void UpdateRoute(FlightRoute updated)
        {
            if (updated == null) return;
            updated.updatedAt = DateTime.UtcNow.ToString("o");
            updated.version++;

            int idx = _allRoutes.FindIndex(r => r.routeId == updated.routeId);
            if (idx >= 0)
                _allRoutes[idx] = updated;
            else
                _allRoutes.Add(updated);

            RouteStorageManager.Instance?.SaveRoute(updated);
        }

        /// <summary>Removes a route from the in-memory library and deletes its saved file.</summary>
        /// <param name="routeId">Id of the route to remove.</param>
        public void DeleteRoute(string routeId)
        {
            _allRoutes.RemoveAll(r => r.routeId == routeId);
            RouteStorageManager.Instance?.DeleteRoute(routeId);
        }

        /// <summary>Returns all routes currently in the in-memory library.</summary>
        public List<FlightRoute> GetAllRoutes() => new List<FlightRoute>(_allRoutes);

        #endregion

        #region Public API — Navigation

        /// <summary>
        /// Begins navigating the specified route from its first waypoint.
        /// Any active session is abandoned first.
        /// </summary>
        /// <param name="route">Route to navigate.</param>
        public void StartNavigation(FlightRoute route)
        {
            if (route == null)
            {
                Debug.LogWarning("[SWEF] RoutePlannerManager.StartNavigation: route is null.");
                return;
            }

            if (_isNavigating) AbandonRoute();

            ActiveRoute  = route;
            RouteProgress = new RouteProgress
            {
                routeId   = route.routeId,
                status    = RouteStatus.InProgress,
                startTime = Time.time
            };

            _isNavigating = true;
            _isPaused     = false;
            _isOffPath    = false;

            NotifyMinimapShowRoute(route);
            NotifyWaypointNavigatorTarget();

            _navCoroutine      = StartCoroutine(NavigationLoop());
            _offPathCoroutine  = StartCoroutine(OffPathLoop());

            OnNavigationStarted?.Invoke(route);
            RoutePlannerAnalytics.Instance?.TrackRouteStarted(route);
        }

        /// <summary>Pauses the active navigation session without abandoning it.</summary>
        public void PauseNavigation()
        {
            if (!_isNavigating || _isPaused) return;
            _isPaused = true;
            OnNavigationPaused?.Invoke();
        }

        /// <summary>Resumes a previously paused navigation session.</summary>
        public void ResumeNavigation()
        {
            if (!_isNavigating || !_isPaused) return;
            _isPaused = false;
            OnNavigationResumed?.Invoke();
        }

        /// <summary>Abandons the current navigation session and resets state.</summary>
        public void AbandonRoute()
        {
            if (!_isNavigating) return;

            if (RouteProgress != null)
                RouteProgress.status = RouteStatus.Abandoned;

            RoutePlannerAnalytics.Instance?.TrackRouteAbandoned(ActiveRoute, RouteProgress);
            EndNavigation(completed: false);
        }

        /// <summary>Skips the current waypoint and advances to the next.</summary>
        public void SkipWaypoint()
        {
            if (!_isNavigating || ActiveRoute == null || RouteProgress == null) return;
            AdvanceWaypoint(skipped: true);
        }

        /// <summary>
        /// Returns a list of suggested routes ordered by relevance to the player's current
        /// position, preferences, and history.
        /// </summary>
        /// <param name="count">Maximum number of suggestions to return.</param>
        public List<FlightRoute> GetSuggestedRoutes(int count)
        {
            return RouteRecommendationEngine.Instance != null
                ? RouteRecommendationEngine.Instance.GetRecommendations(count)
                : new List<FlightRoute>(_allRoutes.Count > count ? _allRoutes.GetRange(0, count) : _allRoutes);
        }

        #endregion

        #region Navigation Loop

        private IEnumerator NavigationLoop()
        {
            var wait = new WaitForSeconds(_checkInterval);

            while (_isNavigating)
            {
                if (!_isPaused)
                {
                    RouteProgress.elapsedTime = Time.time - RouteProgress.startTime;
                    UpdateDistanceAndETA();
                    CheckWaypointProximity();
                }
                yield return wait;
            }
        }

        private IEnumerator OffPathLoop()
        {
            var wait = new WaitForSeconds(_offPathCheckInterval);

            while (_isNavigating)
            {
                if (!_isPaused && _config.offPathWarning)
                    CheckOffPath();
                yield return wait;
            }
        }

        private void UpdateDistanceAndETA()
        {
            var wp = NextWaypoint;
            if (wp == null) { DistanceToNext = 0f; ETA = 0f; return; }

            Vector3 playerPos = GetPlayerPosition();
            Vector3 wpPos     = LatLonAltToWorld(wp.latitude, wp.longitude, wp.altitude);

            DistanceToNext = Vector3.Distance(playerPos, wpPos);

            float speed = GetPlayerSpeed(); // m/s
            ETA = speed > 0.1f ? DistanceToNext / speed : float.MaxValue;
        }

        private void CheckWaypointProximity()
        {
            var wp = NextWaypoint;
            if (wp == null) return;

            Vector3 playerPos = GetPlayerPosition();
            Vector3 wpPos     = LatLonAltToWorld(wp.latitude, wp.longitude, wp.altitude);

            if (Vector3.Distance(playerPos, wpPos) <= wp.triggerRadius)
                TriggerWaypoint(wp, RouteProgress.currentWaypointIndex);
        }

        private void CheckOffPath()
        {
            var wp = NextWaypoint;
            // Only check when actively navigating toward a waypoint
            if (wp == null || RouteProgress == null) return;

            Vector3 playerPos = GetPlayerPosition();
            Vector3 wpPos     = LatLonAltToWorld(wp.latitude, wp.longitude, wp.altitude);

            bool tooFar = Vector3.Distance(playerPos, wpPos) > _config.offPathThreshold;

            if (tooFar && !_isOffPath)
            {
                _isOffPath = true;
                RouteProgress.deviations++;
                OnOffPath?.Invoke();
                RoutePlannerAnalytics.Instance?.TrackOffPath(ActiveRoute);
            }
            else if (!tooFar && _isOffPath)
            {
                _isOffPath = false;
                OnBackOnPath?.Invoke();
            }
        }

        private void TriggerWaypoint(RouteWaypoint wp, int idx)
        {
            if (RouteProgress.waypointsReached.Contains(idx)) return;

            RouteProgress.waypointsReached.Add(idx);
            OnWaypointReached?.Invoke(wp, idx);
            RoutePlannerAnalytics.Instance?.TrackWaypointReached(ActiveRoute, wp, idx);

            // Trigger narration if linked
            if (!string.IsNullOrEmpty(wp.narrationId))
                TriggerNarration(wp.narrationId);

            if (wp.stayDuration > 0f)
                StartCoroutine(DelayedAdvance(wp.stayDuration));
            else if (_config.autoAdvanceWaypoints)
                AdvanceWaypoint(skipped: false);
        }

        private IEnumerator DelayedAdvance(float delay)
        {
            yield return new WaitForSeconds(delay);
            AdvanceWaypoint(skipped: false);
        }

        private void AdvanceWaypoint(bool skipped)
        {
            if (ActiveRoute == null || RouteProgress == null) return;

            RouteProgress.currentWaypointIndex++;

            // Check for route completion
            if (RouteProgress.currentWaypointIndex >= ActiveRoute.waypoints.Count)
            {
                CompleteRoute();
                return;
            }

            NotifyWaypointNavigatorTarget();
        }

        private void CompleteRoute()
        {
            if (RouteProgress == null) return;
            RouteProgress.status    = RouteStatus.Completed;
            RouteProgress.elapsedTime = Time.time - RouteProgress.startTime;

            // Update personal best
            if (RouteProgress.bestTime < 0f || RouteProgress.elapsedTime < RouteProgress.bestTime)
                RouteProgress.bestTime = RouteProgress.elapsedTime;

            FlightRoute completed = ActiveRoute;
            RouteProgress prog    = RouteProgress;

            RoutePlannerAnalytics.Instance?.TrackRouteCompleted(completed, prog);
            OnRouteCompleted?.Invoke(completed, prog);

            EndNavigation(completed: true);
        }

        private void EndNavigation(bool completed)
        {
            _isNavigating = false;
            _isPaused     = false;

            if (_navCoroutine     != null) { StopCoroutine(_navCoroutine);     _navCoroutine     = null; }
            if (_offPathCoroutine != null) { StopCoroutine(_offPathCoroutine); _offPathCoroutine = null; }

            ActiveRoute   = null;
        }

        #endregion

        #region Route Suggestions

        /// <summary>
        /// Returns routes whose starting point is within <paramref name="radiusKm"/> kilometres
        /// of the provided coordinates.
        /// </summary>
        public List<FlightRoute> GetNearbyRoutes(double lat, double lon, float radiusKm)
        {
            var result = new List<FlightRoute>();
            foreach (var r in _allRoutes)
            {
                double dist = HaversineKm(lat, lon, r.startLatitude, r.startLongitude);
                if (dist <= radiusKm)
                    result.Add(r);
            }
            return result;
        }

        #endregion

        #region Internal Helpers

        private void ResolveReferences()
        {
            // Soft-resolve via string type names to avoid hard compile-time dependencies
            _flightController  = FindObjectOfType(Type.GetType("SWEF.Flight.FlightController, Assembly-CSharp") ?? typeof(MonoBehaviour)) as Component;
            _waypointNavigator = FindObjectOfType(Type.GetType("SWEF.GuidedTour.WaypointNavigator, Assembly-CSharp") ?? typeof(MonoBehaviour)) as Component;
            _minimapManager    = FindObjectOfType(Type.GetType("SWEF.Minimap.MinimapManager, Assembly-CSharp") ?? typeof(MonoBehaviour)) as Component;
            _narrationManager  = FindObjectOfType(Type.GetType("SWEF.Narration.NarrationManager, Assembly-CSharp") ?? typeof(MonoBehaviour)) as Component;
        }

        private void LoadRoutesFromStorage()
        {
            if (RouteStorageManager.Instance == null) return;
            var loaded = RouteStorageManager.Instance.GetAllRoutes();
            _allRoutes.Clear();
            _allRoutes.AddRange(loaded);
        }

        private Vector3 GetPlayerPosition()
        {
            // Prefer Camera.main position as a reliable fallback
            if (Camera.main != null) return Camera.main.transform.position;
            return Vector3.zero;
        }

        private float GetPlayerSpeed()
        {
            // Return a default cruising speed when FlightController is unavailable
            return 100f; // m/s placeholder
        }

        private Vector3 LatLonAltToWorld(double lat, double lon, float alt)
        {
            // Simplified flat-earth conversion used for proximity detection.
            // In the full game this delegates to the terrain coordinate system.
            const float metersPerDegree = 111_320f;
            float x = (float)(lon * metersPerDegree);
            float z = (float)(lat * metersPerDegree);
            return new Vector3(x, alt, z);
        }

        private void NotifyMinimapShowRoute(FlightRoute route)
        {
            // Minimap integration is handled through RoutePathRenderer
        }

        private void NotifyWaypointNavigatorTarget()
        {
            // WaypointNavigator integration: update the current target
            if (_waypointNavigator == null) return;
            var wp = NextWaypoint;
            if (wp == null) return;

            var method = _waypointNavigator.GetType().GetMethod("SetManualTarget");
            if (method != null)
            {
                Vector3 pos = LatLonAltToWorld(wp.latitude, wp.longitude, wp.altitude);
                method.Invoke(_waypointNavigator, new object[] { pos });
            }
        }

        private void TriggerNarration(string narrationId)
        {
            if (_narrationManager == null || string.IsNullOrEmpty(narrationId)) return;
            var method = _narrationManager.GetType().GetMethod("PlayNarration",
                new[] { typeof(string) });
            method?.Invoke(_narrationManager, new object[] { narrationId });
        }

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
