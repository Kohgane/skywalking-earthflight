using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.WeatherChallenge
{
    /// <summary>
    /// Phase 53 — Renders the weather challenge route in 3D world space using a
    /// <see cref="LineRenderer"/> for the path and instantiated prefab markers for each waypoint.
    /// Supports colour-coded progress (completed / active / pending) and particle feedback
    /// when a waypoint is reached.
    /// </summary>
    public class RouteVisualizationController : MonoBehaviour
    {
        #region Serialized Fields

        /// <summary>LineRenderer component used to draw the route path between waypoints.</summary>
        [SerializeField] private LineRenderer routeLineRenderer;

        /// <summary>Prefab spawned at each standard (pending) waypoint position.</summary>
        [SerializeField] private GameObject waypointMarkerPrefab;

        /// <summary>Prefab spawned at the currently active (next target) waypoint position.</summary>
        [SerializeField] private GameObject activeWaypointMarkerPrefab;

        /// <summary>Optional particle system prefab triggered when a waypoint is reached.</summary>
        [SerializeField] private GameObject waypointReachedParticlePrefab;

        /// <summary>Colour used for pending / unvisited route segments.</summary>
        [SerializeField] private Color routeColor    = new Color(0.2f, 0.6f, 1f, 0.85f);

        /// <summary>Colour used for completed (already-flown) route segments.</summary>
        [SerializeField] private Color completedColor = new Color(0.3f, 1f, 0.3f, 0.85f);

        /// <summary>Colour used to highlight the currently active waypoint segment.</summary>
        [SerializeField] private Color activeColor   = new Color(1f, 0.85f, 0f, 1f);

        /// <summary>World-space Y offset applied to all markers so they float above terrain.</summary>
        [SerializeField] private float markerHeightOffset = 50f;

        /// <summary>Scale factor used to convert geo-coordinates to Unity world units.</summary>
        [SerializeField] private float geoToWorldScale = 1f;

        #endregion

        #region Private State

        /// <summary>Live instances of waypoint markers keyed by <see cref="RouteWaypoint.waypointId"/>.</summary>
        private Dictionary<string, GameObject> waypointMarkers = new Dictionary<string, GameObject>();

        private List<RouteWaypoint> _currentRoute;
        private int                  _currentWaypointIndex;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (WeatherChallengeManager.Instance != null)
            {
                WeatherChallengeManager.Instance.OnChallengeStarted   += HandleChallengeStarted;
                WeatherChallengeManager.Instance.OnWaypointReached    += HandleWaypointReached;
                WeatherChallengeManager.Instance.OnChallengeCompleted += HandleChallengeEnded;
                WeatherChallengeManager.Instance.OnChallengeFailed    += HandleChallengeEnded;
            }
        }

        private void OnDisable()
        {
            if (WeatherChallengeManager.Instance != null)
            {
                WeatherChallengeManager.Instance.OnChallengeStarted   -= HandleChallengeStarted;
                WeatherChallengeManager.Instance.OnWaypointReached    -= HandleWaypointReached;
                WeatherChallengeManager.Instance.OnChallengeCompleted -= HandleChallengeEnded;
                WeatherChallengeManager.Instance.OnChallengeFailed    -= HandleChallengeEnded;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Renders the full route by placing markers at each waypoint position and drawing the
        /// path line through them. Any previous visualisation is cleared first.
        /// </summary>
        /// <param name="waypoints">The ordered list of waypoints to visualise.</param>
        public void RenderRoute(List<RouteWaypoint> waypoints)
        {
            ClearRoute();
            if (waypoints == null || waypoints.Count == 0) return;

            _currentRoute         = waypoints;
            _currentWaypointIndex = 0;

            // Place markers
            foreach (RouteWaypoint wp in waypoints)
            {
                Vector3 pos = GeoToWorld(wp.latitude, wp.longitude, (float)wp.altitude);
                GameObject prefab = waypointMarkerPrefab;
                if (prefab == null) continue;

                GameObject marker = Instantiate(prefab, pos, Quaternion.identity, transform);
                waypointMarkers[wp.waypointId] = marker;
            }

            DrawLine(waypoints, 0);
        }

        /// <summary>
        /// Highlights <paramref name="waypointId"/> as the next target by swapping its marker
        /// prefab to <see cref="activeWaypointMarkerPrefab"/> and updating the line colour.
        /// </summary>
        /// <param name="waypointId">The <see cref="RouteWaypoint.waypointId"/> to highlight.</param>
        public void UpdateActiveWaypoint(string waypointId)
        {
            if (_currentRoute == null) return;

            for (int i = 0; i < _currentRoute.Count; i++)
            {
                if (_currentRoute[i].waypointId != waypointId) continue;

                _currentWaypointIndex = i;

                // Swap marker to active prefab
                if (waypointMarkers.TryGetValue(waypointId, out GameObject old) && old != null)
                {
                    Vector3 pos = old.transform.position;
                    Destroy(old);

                    if (activeWaypointMarkerPrefab != null)
                    {
                        GameObject active = Instantiate(activeWaypointMarkerPrefab, pos, Quaternion.identity, transform);
                        waypointMarkers[waypointId] = active;
                    }
                }

                UpdateRouteProgress(i);
                break;
            }
        }

        /// <summary>
        /// Provides visual feedback when a waypoint is reached: changes the marker colour to
        /// <see cref="completedColor"/> and optionally spawns a particle burst.
        /// </summary>
        /// <param name="waypointId">The <see cref="RouteWaypoint.waypointId"/> that was reached.</param>
        public void MarkWaypointReached(string waypointId)
        {
            if (waypointMarkers.TryGetValue(waypointId, out GameObject marker) && marker != null)
            {
                // Use MaterialPropertyBlock to avoid creating new material instances
                var propBlock = new MaterialPropertyBlock();
                propBlock.SetColor("_Color", completedColor);
                foreach (Renderer r in marker.GetComponentsInChildren<Renderer>())
                {
                    r.SetPropertyBlock(propBlock);
                }

                // Particle burst
                if (waypointReachedParticlePrefab != null)
                {
                    GameObject burst = Instantiate(waypointReachedParticlePrefab,
                        marker.transform.position, Quaternion.identity);
                    Destroy(burst, 3f);
                }
            }

            // Advance the active waypoint to the next unreached one
            if (_currentRoute != null)
            {
                for (int i = 0; i < _currentRoute.Count; i++)
                {
                    if (_currentRoute[i].waypointId == waypointId && i + 1 < _currentRoute.Count)
                    {
                        UpdateActiveWaypoint(_currentRoute[i + 1].waypointId);
                        break;
                    }
                }
            }
        }

        /// <summary>Removes all route markers and clears the <see cref="LineRenderer"/>.</summary>
        public void ClearRoute()
        {
            foreach (var kvp in waypointMarkers)
                if (kvp.Value != null) Destroy(kvp.Value);

            waypointMarkers.Clear();
            _currentRoute = null;
            _currentWaypointIndex = 0;

            if (routeLineRenderer != null)
            {
                routeLineRenderer.positionCount = 0;
            }
        }

        /// <summary>Toggles visibility of the route line renderer and all waypoint markers.</summary>
        /// <param name="visible">Whether the route should be visible.</param>
        public void SetRouteVisible(bool visible)
        {
            if (routeLineRenderer != null)
                routeLineRenderer.enabled = visible;

            foreach (var kvp in waypointMarkers)
                if (kvp.Value != null) kvp.Value.SetActive(visible);
        }

        /// <summary>
        /// Colour-codes the line renderer so completed segments use <see cref="completedColor"/>,
        /// the active segment uses <see cref="activeColor"/>, and pending segments use
        /// <see cref="routeColor"/>.
        /// </summary>
        /// <param name="currentWaypointIndex">Index of the next (not yet reached) waypoint.</param>
        public void UpdateRouteProgress(int currentWaypointIndex)
        {
            if (_currentRoute == null || routeLineRenderer == null) return;

            _currentWaypointIndex = currentWaypointIndex;

            // Build per-vertex colour gradient
            var gradient = new Gradient();
            int total = _currentRoute.Count;
            if (total < 2) return;

            var colorKeys = new GradientColorKey[Mathf.Min(total, 8)];
            var alphaKeys = new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };

            for (int i = 0; i < colorKeys.Length; i++)
            {
                float t = (float)i / (colorKeys.Length - 1);
                int wi = Mathf.RoundToInt(t * (total - 1));
                Color c = wi < currentWaypointIndex ? completedColor
                        : wi == currentWaypointIndex ? activeColor
                        : routeColor;
                colorKeys[i] = new GradientColorKey(c, t);
            }
            gradient.SetKeys(colorKeys, alphaKeys);
            routeLineRenderer.colorGradient = gradient;
        }

        #endregion

        #region Private Helpers

        private void DrawLine(List<RouteWaypoint> waypoints, int currentIndex)
        {
            if (routeLineRenderer == null || waypoints == null) return;

            routeLineRenderer.positionCount = waypoints.Count;
            for (int i = 0; i < waypoints.Count; i++)
            {
                routeLineRenderer.SetPosition(i,
                    GeoToWorld(waypoints[i].latitude, waypoints[i].longitude, (float)waypoints[i].altitude));
            }
            routeLineRenderer.startColor = routeColor;
            routeLineRenderer.endColor   = routeColor;
            UpdateRouteProgress(currentIndex);
        }

        private Vector3 GeoToWorld(double lat, double lon, float alt)
        {
            // Simple flat-Earth approximation scaled by geoToWorldScale.
            // Replace with a proper geo-projection when a world-space geo library is available.
            float x = (float)(lon * geoToWorldScale);
            float z = (float)(lat * geoToWorldScale);
            float y = alt * geoToWorldScale + markerHeightOffset;
            return new Vector3(x, y, z);
        }

        private void HandleChallengeStarted(WeatherChallenge c)
        {
            RenderRoute(c.waypoints);
            if (c.waypoints.Count > 0)
                UpdateActiveWaypoint(c.waypoints[0].waypointId);
        }

        private void HandleWaypointReached(RouteWaypoint wp) => MarkWaypointReached(wp.waypointId);
        private void HandleChallengeEnded(WeatherChallenge c) => StartCoroutine(DelayedClear(3f));

        private IEnumerator DelayedClear(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClearRoute();
        }

        #endregion
    }
}
