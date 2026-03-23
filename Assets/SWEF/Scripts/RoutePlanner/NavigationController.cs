using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Real-time navigation guidance during flight.
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Tracks the current waypoint index in the active route.</item>
    ///   <item>Detects arrival when the player enters a waypoint's radius.</item>
    ///   <item>Auto-advances to the next waypoint on arrival.</item>
    ///   <item>Calculates distance, bearing, and ETA to the next waypoint each frame.</item>
    ///   <item>Detects off-route deviations and suggests course corrections.</item>
    ///   <item>Supports looping routes (wraps back to waypoint 0 after the last).</item>
    /// </list>
    /// </para>
    /// Attach to the same persistent GameObject as <see cref="RoutePlannerManager"/>.
    /// </summary>
    public class NavigationController : MonoBehaviour
    {
        #region Constants

        private const float EtaUpdateInterval       = 1f;   // seconds between ETA recalculations
        private const float ArrivalCheckInterval    = 0.1f; // seconds between proximity checks
        private const float MinSpeedForEta          = 0.5f; // m/s — below this speed ETA is infinite
        private const float KmhToMs                = 1f / 3.6f;

        #endregion

        #region Inspector

        [Header("Player Reference")]
        [Tooltip("Transform of the player aircraft. Auto-found if null.")]
        [SerializeField] private Transform playerTransform;

        [Header("Navigation Settings")]
        [SerializeField] private NavigationSettings settings = new NavigationSettings();

        [Header("Route Loop")]
        [Tooltip("When true, reaching the last waypoint wraps back to the first.")]
        [SerializeField] private bool loopRoute;

        #endregion

        #region Events

        /// <summary>Fired when the player enters a waypoint's arrival radius.</summary>
        public event Action<Waypoint, int> OnWaypointReached;

        /// <summary>Fired when the player strays beyond the off-route threshold.</summary>
        public event Action<float> OnRouteDeviation;

        /// <summary>Fired each time the ETA estimate is refreshed.</summary>
        public event Action<float> OnETAUpdated;

        /// <summary>Fired when this controller begins guiding a route.</summary>
        public event Action<List<Waypoint>> OnNavigationStarted;

        /// <summary>Fired when navigation ends (route completed or cancelled).</summary>
        public event Action OnNavigationEnded;

        #endregion

        #region Public Properties

        /// <summary>The waypoints being navigated, or an empty list when idle.</summary>
        public IReadOnlyList<Waypoint> Waypoints => _waypoints;

        /// <summary>Index of the waypoint the player is currently heading toward.</summary>
        public int CurrentWaypointIndex { get; private set; }

        /// <summary>The next waypoint, or <c>null</c> when none remain.</summary>
        public Waypoint CurrentWaypoint =>
            _waypoints != null && CurrentWaypointIndex < _waypoints.Count
                ? _waypoints[CurrentWaypointIndex]
                : null;

        /// <summary>Distance in metres to <see cref="CurrentWaypoint"/> (updated every frame).</summary>
        public float DistanceToWaypoint { get; private set; }

        /// <summary>Horizontal bearing in degrees (0 = north, clockwise) toward the current waypoint.</summary>
        public float BearingToWaypoint { get; private set; }

        /// <summary>Estimated arrival time in seconds, or <c>float.PositiveInfinity</c> when speed is too low.</summary>
        public float ETA { get; private set; } = float.PositiveInfinity;

        /// <summary><c>true</c> when the player has deviated beyond the off-route threshold.</summary>
        public bool IsOffRoute { get; private set; }

        /// <summary><c>true</c> when navigation is active.</summary>
        public bool IsNavigating { get; private set; }

        /// <summary>Heading correction in degrees needed to point toward the current waypoint.</summary>
        public float HeadingCorrection { get; private set; }

        /// <summary>Completion fraction (0–1) based on waypoints reached.</summary>
        public float Progress =>
            _waypoints == null || _waypoints.Count == 0
                ? 0f
                : (float)CurrentWaypointIndex / _waypoints.Count;

        #endregion

        #region Private State

        private List<Waypoint> _waypoints;
        private float          _currentSpeed;   // m/s, set externally via SetCurrentSpeed()
        private float          _etaTimer;
        private bool           _wasOffRoute;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (playerTransform == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) playerTransform = go.transform;
            }
        }

        private void Update()
        {
            if (!IsNavigating || _waypoints == null || _waypoints.Count == 0) return;

            UpdateDistanceAndBearing();
            CheckArrival();
            CheckOffRoute();

            _etaTimer -= Time.deltaTime;
            if (_etaTimer <= 0f)
            {
                _etaTimer = EtaUpdateInterval;
                RefreshETA();
            }
        }

        #endregion

        #region Public API

        /// <summary>Starts guiding the player through <paramref name="waypoints"/>.</summary>
        /// <param name="waypoints">Ordered list of waypoints to navigate.</param>
        /// <param name="loop">If <c>true</c>, the route wraps back to the start after the last waypoint.</param>
        public void StartNavigation(List<Waypoint> waypoints, bool loop = false)
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                Debug.LogWarning("[SWEF] NavigationController.StartNavigation: waypoint list is null or empty.");
                return;
            }

            _waypoints           = waypoints;
            CurrentWaypointIndex = 0;
            loopRoute            = loop;
            IsNavigating         = true;
            IsOffRoute           = false;
            _wasOffRoute         = false;
            _etaTimer            = 0f;

            OnNavigationStarted?.Invoke(_waypoints);
        }

        /// <summary>Stops active navigation and resets state.</summary>
        public void StopNavigation()
        {
            IsNavigating         = false;
            IsOffRoute           = false;
            _waypoints           = null;
            CurrentWaypointIndex = 0;
            DistanceToWaypoint   = 0f;
            BearingToWaypoint    = 0f;
            ETA                  = float.PositiveInfinity;
            OnNavigationEnded?.Invoke();
        }

        /// <summary>Manually skips to the next waypoint.</summary>
        public void SkipWaypoint()
        {
            if (!IsNavigating) return;
            AdvanceWaypoint();
        }

        /// <summary>
        /// Updates the player's current speed used for ETA calculations.
        /// Call this every frame from the flight controller.
        /// </summary>
        /// <param name="speedMs">Speed in metres per second.</param>
        public void SetCurrentSpeed(float speedMs) => _currentSpeed = Mathf.Max(0f, speedMs);

        /// <summary>Updates the navigation settings at runtime.</summary>
        public void ApplySettings(NavigationSettings newSettings)
        {
            if (newSettings != null) settings = newSettings;
        }

        #endregion

        #region Private Helpers

        private void UpdateDistanceAndBearing()
        {
            if (playerTransform == null || CurrentWaypoint == null) return;

            Vector3 delta = CurrentWaypoint.position - playerTransform.position;
            DistanceToWaypoint = delta.magnitude;

            // Horizontal bearing (ignoring vertical component)
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            BearingToWaypoint = flat == Vector3.zero
                ? 0f
                : Vector3.SignedAngle(Vector3.forward, flat, Vector3.up);
            if (BearingToWaypoint < 0f) BearingToWaypoint += 360f;

            // Heading correction
            float currentHeading = playerTransform != null
                ? playerTransform.eulerAngles.y
                : 0f;
            float raw = BearingToWaypoint - currentHeading;
            HeadingCorrection = ((raw + 180f) % 360f) - 180f; // [-180, 180]
        }

        private void CheckArrival()
        {
            if (CurrentWaypoint == null || playerTransform == null) return;

            float radius = CurrentWaypoint.radius > 0f
                ? CurrentWaypoint.radius
                : settings.arrivalDetectionRadius;

            if (DistanceToWaypoint <= radius)
            {
                OnWaypointReached?.Invoke(CurrentWaypoint, CurrentWaypointIndex);

                if (settings.autoAdvanceWaypoints)
                    AdvanceWaypoint();
            }
        }

        private void CheckOffRoute()
        {
            if (CurrentWaypoint == null) return;

            bool offNow = DistanceToWaypoint > settings.offRouteWarningDistance;

            if (offNow && !_wasOffRoute)
            {
                IsOffRoute   = true;
                _wasOffRoute = true;
                OnRouteDeviation?.Invoke(DistanceToWaypoint);
            }
            else if (!offNow)
            {
                IsOffRoute   = false;
                _wasOffRoute = false;
            }
        }

        private void RefreshETA()
        {
            if (_currentSpeed < MinSpeedForEta)
            {
                ETA = float.PositiveInfinity;
            }
            else
            {
                // Remaining path distance
                float remaining = DistanceToWaypoint;
                if (_waypoints != null)
                {
                    for (int i = CurrentWaypointIndex + 1; i < _waypoints.Count; i++)
                        remaining += Vector3.Distance(_waypoints[i - 1].position, _waypoints[i].position);
                }
                ETA = remaining / _currentSpeed;
            }

            OnETAUpdated?.Invoke(ETA);
        }

        private void AdvanceWaypoint()
        {
            if (_waypoints == null) return;

            int next = CurrentWaypointIndex + 1;

            if (next >= _waypoints.Count)
            {
                if (loopRoute)
                {
                    CurrentWaypointIndex = 0;
                }
                else
                {
                    StopNavigation();
                }
                return;
            }

            CurrentWaypointIndex = next;
        }

        #endregion
    }
}
