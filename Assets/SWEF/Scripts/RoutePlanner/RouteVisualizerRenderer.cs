using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — 3D visualisation of a flight route inside the game world.
    /// <para>
    /// Features:
    /// <list type="bullet">
    ///   <item>LineRenderer-based path between waypoints.</item>
    ///   <item>Colour coding: completed (green), current (yellow), upcoming (blue).</item>
    ///   <item>Waypoint markers and world-space name labels.</item>
    ///   <item>Per-segment distance labels.</item>
    ///   <item>Animated flow effect on the active segment.</item>
    ///   <item>Transparency fade for waypoints far from the player.</item>
    ///   <item>Height offset to render path above terrain.</item>
    ///   <item>Visibility toggle.</item>
    /// </list>
    /// </para>
    /// Wire <see cref="NavigationController"/> in the inspector or let the component
    /// find it at runtime.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RouteVisualizerRenderer : MonoBehaviour
    {
        #region Constants

        private const float DefaultLineWidth      = 8f;
        private const float AnimationSpeed        = 1.5f;    // texture scroll speed
        private const float FadeStartDistance     = 2000f;   // metres — start fading
        private const float FadeEndDistance       = 5000f;   // metres — fully transparent
        private const float HeightOffsetDefault   = 10f;     // metres above waypoint y
        private const float MarkerScale           = 20f;

        #endregion

        #region Inspector

        [Header("Controller")]
        [Tooltip("NavigationController to source waypoints from. Auto-found if null.")]
        [SerializeField] private NavigationController navigationController;

        [Header("Player Reference")]
        [SerializeField] private Transform playerTransform;

        [Header("Line Appearance")]
        [SerializeField] private Color completedColor  = Color.green;
        [SerializeField] private Color currentColor    = Color.yellow;
        [SerializeField] private Color upcomingColor   = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private float lineWidth       = DefaultLineWidth;
        [SerializeField] private Material lineMaterial;
        [Tooltip("Metres above each waypoint's y-position to render the path.")]
        [SerializeField] private float heightOffset    = HeightOffsetDefault;

        [Header("Waypoint Markers")]
        [SerializeField] private GameObject waypointMarkerPrefab;
        [SerializeField] private float      markerScale = MarkerScale;

        [Header("Labels")]
        [Tooltip("Prefab with a TextMesh for world-space labels.")]
        [SerializeField] private GameObject labelPrefab;
        [SerializeField] private bool       showWaypointNames   = true;
        [SerializeField] private bool       showDistanceLabels  = true;

        [Header("Animation")]
        [SerializeField] private bool  animateCurrentSegment = true;
        [SerializeField] private float animationSpeed        = AnimationSpeed;

        [Header("Fade")]
        [SerializeField] private bool  fadeWithDistance    = true;
        [SerializeField] private float fadeStartDistance   = FadeStartDistance;
        [SerializeField] private float fadeEndDistance     = FadeEndDistance;

        #endregion

        #region Private State

        private LineRenderer          _lineRenderer;
        private List<Waypoint>        _waypoints;
        private int                   _currentIndex;
        private bool                  _visible = true;

        private readonly List<GameObject> _markers = new List<GameObject>();
        private readonly List<GameObject> _labels  = new List<GameObject>();

        private float _animOffset;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();

            if (navigationController == null)
                navigationController = FindFirstObjectByType<NavigationController>();

            if (playerTransform == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) playerTransform = go.transform;
            }
        }

        private void OnEnable()
        {
            if (navigationController == null) return;
            navigationController.OnNavigationStarted += HandleNavigationStarted;
            navigationController.OnNavigationEnded   += HandleNavigationEnded;
            navigationController.OnWaypointReached   += HandleWaypointReached;
        }

        private void OnDisable()
        {
            if (navigationController == null) return;
            navigationController.OnNavigationStarted -= HandleNavigationStarted;
            navigationController.OnNavigationEnded   -= HandleNavigationEnded;
            navigationController.OnWaypointReached   -= HandleWaypointReached;
        }

        private void Update()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;

            if (animateCurrentSegment && lineMaterial != null)
            {
                _animOffset -= Time.deltaTime * animationSpeed;
                lineMaterial.mainTextureOffset = new Vector2(_animOffset, 0f);
            }

            if (fadeWithDistance) UpdateFade();

            // Billboard labels toward camera
            if (Camera.main != null)
            {
                foreach (var lbl in _labels)
                {
                    if (lbl != null)
                        lbl.transform.LookAt(Camera.main.transform);
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>Renders the given waypoints as a visualised route.</summary>
        /// <param name="waypoints">Ordered waypoints.</param>
        /// <param name="currentIndex">Index of the waypoint being headed toward.</param>
        public void ShowRoute(List<Waypoint> waypoints, int currentIndex = 0)
        {
            ClearAll();
            _waypoints    = waypoints;
            _currentIndex = currentIndex;
            Rebuild();
        }

        /// <summary>Updates which segment is highlighted as "current".</summary>
        public void SetCurrentIndex(int index)
        {
            _currentIndex = index;
            Rebuild();
        }

        /// <summary>Clears all rendered geometry and labels.</summary>
        public void Hide()
        {
            _visible = false;
            _lineRenderer.enabled = false;
            SetMarkersActive(false);
            SetLabelsActive(false);
        }

        /// <summary>Shows the visualiser if a route is loaded.</summary>
        public void Show()
        {
            _visible = true;
            _lineRenderer.enabled = _waypoints != null && _waypoints.Count > 0;
            SetMarkersActive(true);
            SetLabelsActive(true);
        }

        /// <summary>Toggles visibility.</summary>
        public void ToggleVisibility() { if (_visible) Hide(); else Show(); }

        #endregion

        #region Event Handlers

        private void HandleNavigationStarted(List<Waypoint> waypoints)
        {
            ShowRoute(waypoints, 0);
        }

        private void HandleNavigationEnded()
        {
            ClearAll();
        }

        private void HandleWaypointReached(Waypoint wp, int index)
        {
            SetCurrentIndex(index + 1);
        }

        #endregion

        #region Build / Rebuild

        private void Rebuild()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;

            // Position points with height offset
            _lineRenderer.positionCount = _waypoints.Count;
            for (int i = 0; i < _waypoints.Count; i++)
            {
                Vector3 p = _waypoints[i].position;
                p.y += heightOffset;
                _lineRenderer.SetPosition(i, p);
            }

            // Colour gradient
            var grad = BuildGradient();
            _lineRenderer.colorGradient = grad;

            // Markers and labels
            CreateMarkers();
            CreateLabels();

            _lineRenderer.enabled = _visible;
        }

        private Gradient BuildGradient()
        {
            var grad = new Gradient();
            if (_waypoints == null || _waypoints.Count < 2)
            {
                grad.SetKeys(
                    new[] { new GradientColorKey(upcomingColor, 0f), new GradientColorKey(upcomingColor, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                return grad;
            }

            int   count    = _waypoints.Count;
            float compFrac = _currentIndex > 0 ? (float)(_currentIndex - 1) / (count - 1) : 0f;
            float currFrac = (float)_currentIndex / (count - 1);

            var colorKeys = new List<GradientColorKey>
            {
                new GradientColorKey(completedColor, 0f),
                new GradientColorKey(completedColor, Mathf.Max(compFrac - 0.001f, 0f)),
                new GradientColorKey(currentColor,   compFrac),
                new GradientColorKey(currentColor,   Mathf.Min(currFrac + 0.001f, 1f)),
                new GradientColorKey(upcomingColor,  currFrac),
                new GradientColorKey(upcomingColor,  1f),
            };

            grad.SetKeys(
                colorKeys.ToArray(),
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return grad;
        }

        private void CreateMarkers()
        {
            ClearMarkers();
            if (waypointMarkerPrefab == null || _waypoints == null) return;

            foreach (var wp in _waypoints)
            {
                Vector3 pos = wp.position + Vector3.up * heightOffset;
                var obj = Instantiate(waypointMarkerPrefab, pos, Quaternion.identity, transform);
                obj.transform.localScale = Vector3.one * markerScale;
                _markers.Add(obj);
            }
        }

        private void CreateLabels()
        {
            ClearLabels();
            if (labelPrefab == null || _waypoints == null) return;

            for (int i = 0; i < _waypoints.Count; i++)
            {
                var wp  = _waypoints[i];
                Vector3 pos = wp.position + Vector3.up * (heightOffset + markerScale);

                if (showWaypointNames)
                {
                    var lbl = Instantiate(labelPrefab, pos, Quaternion.identity, transform);
                    if (lbl.TryGetComponent<TextMesh>(out var tm)) tm.text = wp.name;
                    _labels.Add(lbl);
                }

                if (showDistanceLabels && i < _waypoints.Count - 1)
                {
                    float dist = Vector3.Distance(wp.position, _waypoints[i + 1].position);
                    Vector3 mid = (wp.position + _waypoints[i + 1].position) * 0.5f + Vector3.up * heightOffset;
                    var lbl = Instantiate(labelPrefab, mid, Quaternion.identity, transform);
                    if (lbl.TryGetComponent<TextMesh>(out var tm))
                        tm.text = dist >= 1000f ? $"{dist / 1000f:0.0} km" : $"{Mathf.RoundToInt(dist)} m";
                    _labels.Add(lbl);
                }
            }
        }

        #endregion

        #region Fade

        private void UpdateFade()
        {
            if (playerTransform == null || _waypoints == null) return;

            float nearest = float.MaxValue;
            foreach (var wp in _waypoints)
                nearest = Mathf.Min(nearest, Vector3.Distance(playerTransform.position, wp.position));

            float alpha = 1f - Mathf.InverseLerp(fadeStartDistance, fadeEndDistance, nearest);

            var grad = _lineRenderer.colorGradient;
            var alphaKeys = grad.alphaKeys;
            for (int i = 0; i < alphaKeys.Length; i++) alphaKeys[i].alpha = alpha;
            grad.alphaKeys = alphaKeys;
            _lineRenderer.colorGradient = grad;
        }

        #endregion

        #region Helpers

        private void ConfigureLineRenderer()
        {
            _lineRenderer.startWidth  = lineWidth;
            _lineRenderer.endWidth    = lineWidth;
            _lineRenderer.useWorldSpace = true;
            if (lineMaterial != null) _lineRenderer.material = lineMaterial;
        }

        private void ClearAll()
        {
            ClearMarkers();
            ClearLabels();
            _lineRenderer.positionCount = 0;
            _waypoints = null;
        }

        private void ClearMarkers()
        {
            foreach (var m in _markers) if (m != null) Destroy(m);
            _markers.Clear();
        }

        private void ClearLabels()
        {
            foreach (var l in _labels) if (l != null) Destroy(l);
            _labels.Clear();
        }

        private void SetMarkersActive(bool active)
        {
            foreach (var m in _markers) if (m != null) m.SetActive(active);
        }

        private void SetLabelsActive(bool active)
        {
            foreach (var l in _labels) if (l != null) l.SetActive(active);
        }

        #endregion
    }
}
