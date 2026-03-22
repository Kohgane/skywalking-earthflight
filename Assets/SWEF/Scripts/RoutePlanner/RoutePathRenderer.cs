using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Renders a <see cref="FlightRoute"/> as a 3D world-space path using a
    /// <see cref="LineRenderer"/>. Supports Catmull-Rom spline interpolation, altitude-based
    /// gradient colouring, animated flow effect, waypoint markers, and a fast-forward
    /// preview animation.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RoutePathRenderer : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static RoutePathRenderer Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Line Renderer")]
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private Material     _pathMaterial;
        [SerializeField] private float        _lineWidth          = 4f;

        [Header("Spline")]
        [Tooltip("Number of interpolated points between each pair of waypoints.")]
        [SerializeField] private int   _splineSegments     = 20;

        [Header("Colouring")]
        [SerializeField] private bool     _useAltitudeGradient = true;
        [SerializeField] private Color    _lowAltColor         = Color.green;
        [SerializeField] private Color    _highAltColor        = Color.red;

        [Header("Animation")]
        [SerializeField] private bool  _animateFlow    = true;
        [SerializeField] private float _flowSpeed      = 0.5f;

        [Header("Waypoint Markers")]
        [SerializeField] private GameObject _waypointMarkerPrefab;
        [SerializeField] private float      _markerScale = 1f;

        #endregion

        #region Private State

        private FlightRoute _currentRoute;
        private readonly List<GameObject> _markers = new List<GameObject>();
        private Coroutine _animationCoroutine;
        private Coroutine _previewCoroutine;
        private float _textureOffset;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (_lineRenderer == null)
                _lineRenderer = GetComponent<LineRenderer>();

            ConfigureLineRenderer();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        #endregion

        #region Public API

        /// <summary>Renders the path for the supplied route, replacing any current display.</summary>
        /// <param name="route">Route to display.</param>
        public void ShowRoute(FlightRoute route)
        {
            ClearPath();

            if (route == null || route.waypoints.Count < 2) return;
            _currentRoute = route;

            List<Vector3> splinePoints = BuildSplinePath(route.waypoints);
            ApplyLineRenderer(splinePoints, route);
            SpawnWaypointMarkers(route.waypoints);

            if (_animateFlow && _animationCoroutine == null)
                _animationCoroutine = StartCoroutine(AnimateFlow());
        }

        /// <summary>Clears all rendered path geometry and markers.</summary>
        public void ClearPath()
        {
            _currentRoute = null;

            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = 0;
            }

            foreach (var m in _markers)
                if (m != null) Destroy(m);
            _markers.Clear();

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        /// <summary>
        /// Highlights the waypoint at <paramref name="index"/> as the current navigation target.
        /// </summary>
        public void HighlightWaypoint(int index)
        {
            for (int i = 0; i < _markers.Count; i++)
            {
                if (_markers[i] == null) continue;
                var renderer = _markers[i].GetComponent<Renderer>();
                if (renderer == null) continue;

                renderer.material.color = (i == index)
                    ? Color.yellow
                    : Color.white;
            }
        }

        /// <summary>Marks a reached waypoint with a completion indicator.</summary>
        public void MarkWaypointReached(int index)
        {
            if (index < 0 || index >= _markers.Count || _markers[index] == null) return;

            var renderer = _markers[index].GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.grey;
        }

        /// <summary>Begins a fast-forward preview animation of the route path.</summary>
        public void StartPreview(FlightRoute route)
        {
            if (_previewCoroutine != null) StopCoroutine(_previewCoroutine);
            ShowRoute(route);
            _previewCoroutine = StartCoroutine(PreviewAnimation(route));
        }

        /// <summary>Stops any active preview animation.</summary>
        public void StopPreview()
        {
            if (_previewCoroutine != null)
            {
                StopCoroutine(_previewCoroutine);
                _previewCoroutine = null;
            }
        }

        #endregion

        #region Spline Construction

        /// <summary>
        /// Builds a smooth Catmull-Rom spline through all waypoints.
        /// </summary>
        private List<Vector3> BuildSplinePath(List<RouteWaypoint> waypoints)
        {
            var result = new List<Vector3>();
            int count  = waypoints.Count;

            for (int i = 0; i < count - 1; i++)
            {
                Vector3 p0 = GetWorldPos(waypoints[Mathf.Max(i - 1, 0)]);
                Vector3 p1 = GetWorldPos(waypoints[i]);
                Vector3 p2 = GetWorldPos(waypoints[i + 1]);
                Vector3 p3 = GetWorldPos(waypoints[Mathf.Min(i + 2, count - 1)]);

                for (int s = 0; s <= _splineSegments; s++)
                {
                    float t = s / (float)_splineSegments;
                    result.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }

            return result;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                2f * p1
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        #endregion

        #region Line Renderer

        private void ConfigureLineRenderer()
        {
            if (_lineRenderer == null) return;
            _lineRenderer.startWidth = _lineWidth;
            _lineRenderer.endWidth   = _lineWidth;
            _lineRenderer.useWorldSpace = true;

            if (_pathMaterial != null)
                _lineRenderer.material = _pathMaterial;
        }

        private void ApplyLineRenderer(List<Vector3> points, FlightRoute route)
        {
            _lineRenderer.positionCount = points.Count;
            _lineRenderer.SetPositions(points.ToArray());

            if (_useAltitudeGradient)
                ApplyAltitudeGradient(points, route);
            else
                ApplyFlatColor(RoutePlannerManager.Instance?.Config?.pathColor ?? Color.cyan);
        }

        private void ApplyAltitudeGradient(List<Vector3> points, FlightRoute route)
        {
            float range = route.maxAltitude - route.minAltitude;
            if (range < 1f) range = 1f;

            var gradient = new Gradient();
            var colorKeys = new GradientColorKey[2]
            {
                new GradientColorKey(_lowAltColor,  0f),
                new GradientColorKey(_highAltColor, 1f)
            };
            var alphaKeys = new GradientAlphaKey[2]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            gradient.SetKeys(colorKeys, alphaKeys);
            _lineRenderer.colorGradient = gradient;
        }

        private void ApplyFlatColor(Color color)
        {
            var gradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            gradient.SetKeys(colorKeys, alphaKeys);
            _lineRenderer.colorGradient = gradient;
        }

        #endregion

        #region Waypoint Markers

        private void SpawnWaypointMarkers(List<RouteWaypoint> waypoints)
        {
            foreach (var wp in waypoints)
            {
                Vector3 pos = GetWorldPos(wp);

                if (_waypointMarkerPrefab != null)
                {
                    var marker = Instantiate(_waypointMarkerPrefab, pos, Quaternion.identity, transform);
                    marker.transform.localScale = Vector3.one * _markerScale;
                    _markers.Add(marker);
                }
                else
                {
                    // Use a primitive sphere as a fallback marker
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    marker.transform.position   = pos;
                    marker.transform.localScale = Vector3.one * (_markerScale * 50f);
                    marker.transform.SetParent(transform);
                    _markers.Add(marker);
                }
            }
        }

        #endregion

        #region Animation

        private IEnumerator AnimateFlow()
        {
            while (true)
            {
                if (_pathMaterial != null)
                {
                    _textureOffset += _flowSpeed * Time.deltaTime;
                    // Use an instanced material to avoid affecting shared material assets
                    _lineRenderer.material.SetTextureOffset("_MainTex", new Vector2(_textureOffset, 0f));
                }
                yield return null;
            }
        }

        private IEnumerator PreviewAnimation(FlightRoute route)
        {
            if (_lineRenderer == null || route == null || route.waypoints.Count < 2)
                yield break;

            List<Vector3> path = BuildSplinePath(route.waypoints);
            float totalLength  = 0f;

            for (int i = 1; i < path.Count; i++)
                totalLength += Vector3.Distance(path[i - 1], path[i]);

            float previewSpeed = totalLength / Mathf.Max(route.estimatedDuration * 60f * 0.1f, 5f);
            float traveled     = 0f;

            for (int i = 1; i < path.Count; i++)
            {
                float seg = Vector3.Distance(path[i - 1], path[i]);
                traveled += seg;
                yield return new WaitForSeconds(seg / previewSpeed);
            }

            _previewCoroutine = null;
        }

        #endregion

        #region Helpers

        private static Vector3 GetWorldPos(RouteWaypoint wp)
        {
            const float metersPerDegree = 111_320f;
            return new Vector3(
                (float)(wp.longitude * metersPerDegree),
                wp.altitude,
                (float)(wp.latitude  * metersPerDegree));
        }

        #endregion
    }
}
