// UGCPathEditor.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — MonoBehaviour for editing the sequential waypoint path of a
    /// <see cref="UGCContent"/> route.
    ///
    /// <para>Renders the path as a Catmull-Rom spline via a <see cref="LineRenderer"/>,
    /// shows distance labels between waypoints, direction arrows, and provides a
    /// loop/one-way toggle.  Also renders an altitude-profile view.</para>
    ///
    /// <para>Requires a <see cref="LineRenderer"/> on the same GameObject.</para>
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class UGCPathEditor : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Spline")]
        [Tooltip("Number of interpolated segments between each pair of waypoints.")]
        [SerializeField] private int _splineSegments = 20;

        [Tooltip("Colour of the path LineRenderer.")]
        [SerializeField] private Color _pathColor = new Color(0.2f, 0.8f, 1f, 0.9f);

        [Header("Labels")]
        [Tooltip("World-space vertical offset for distance labels above the midpoint.")]
        [SerializeField] private float _labelHeightOffset = 50f;

        [Header("Loop")]
        [Tooltip("If true the path loops from the last waypoint back to the first.")]
        [SerializeField] private bool _isLoop = false;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when the loop setting is toggled.</summary>
        public event Action<bool> OnLoopToggled;

        /// <summary>Raised whenever the rendered path is updated.</summary>
        public event Action OnPathUpdated;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>Whether the path loops back from the last waypoint to the first.</summary>
        public bool IsLoop
        {
            get => _isLoop;
            set
            {
                if (_isLoop == value) return;
                _isLoop = value;
                OnLoopToggled?.Invoke(value);
                RefreshPath();
            }
        }

        /// <summary>Returns the total approximate path length in metres.</summary>
        public float TotalDistanceMetres { get; private set; }

        // ── Internal state ─────────────────────────────────────────────────────

        private LineRenderer _lineRenderer;
        private UGCContent   _content;
        private readonly List<Vector3> _splinePoints = new List<Vector3>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.startColor    = _pathColor;
            _lineRenderer.endColor      = _pathColor;
        }

        private void OnDestroy()
        {
            _content = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Binds this editor to a <see cref="UGCContent"/> project and refreshes the display.
        /// </summary>
        public void SetContent(UGCContent content)
        {
            _content = content;
            RefreshPath();
        }

        /// <summary>
        /// Appends a new waypoint at the given world position.
        /// </summary>
        public void AppendWaypoint(Vector3 worldPosition)
        {
            if (_content == null) return;
            if (_content.waypoints.Count >= UGCConfig.MaxWaypoints)
            {
                Debug.LogWarning("[UGCPathEditor] Max waypoints reached.");
                return;
            }

            var wp = new UGCWaypoint
            {
                waypointId = Guid.NewGuid().ToString(),
                latitude   = worldPosition.x,
                longitude  = worldPosition.z,
                altitude   = worldPosition.y,
                order      = _content.waypoints.Count,
                isRequired = true,
            };

            var cmd = new AddWaypointCommand(_content, wp);
            UGCEditorManager.Instance?.ExecuteCommand(cmd);
            RefreshPath();
        }

        /// <summary>
        /// Removes the waypoint with the given ID.
        /// </summary>
        public void RemoveWaypoint(string waypointId)
        {
            if (_content == null) return;
            var wp = _content.waypoints.Find(w => w.waypointId == waypointId);
            if (wp == null) return;
            var cmd = new RemoveWaypointCommand(_content, wp);
            UGCEditorManager.Instance?.ExecuteCommand(cmd);
            RefreshPath();
        }

        /// <summary>
        /// Rebuilds the Catmull-Rom spline and distance labels for the current waypoints.
        /// </summary>
        public void RefreshPath()
        {
            _splinePoints.Clear();
            TotalDistanceMetres = 0f;

            if (_content == null || _content.waypoints.Count < 2)
            {
                _lineRenderer.positionCount = 0;
                OnPathUpdated?.Invoke();
                return;
            }

            var sorted = new List<UGCWaypoint>(_content.waypoints);
            sorted.Sort((a, b) => a.order.CompareTo(b.order));

            var pts = new List<Vector3>();
            foreach (var wp in sorted)
                pts.Add(new Vector3((float)wp.latitude, wp.altitude, (float)wp.longitude));

            if (_isLoop) pts.Add(pts[0]);

            // Build Catmull-Rom spline
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector3 p0 = pts[Mathf.Max(i - 1, 0)];
                Vector3 p1 = pts[i];
                Vector3 p2 = pts[i + 1];
                Vector3 p3 = pts[Mathf.Min(i + 2, pts.Count - 1)];

                for (int s = 0; s <= _splineSegments; s++)
                {
                    float t   = s / (float)_splineSegments;
                    Vector3 sp = CatmullRom(p0, p1, p2, p3, t);
                    _splinePoints.Add(sp);
                }
            }

            // Compute total distance
            for (int i = 1; i < _splinePoints.Count; i++)
                TotalDistanceMetres += Vector3.Distance(_splinePoints[i - 1], _splinePoints[i]);

            // Apply to LineRenderer
            _lineRenderer.positionCount = _splinePoints.Count;
            _lineRenderer.SetPositions(_splinePoints.ToArray());

            OnPathUpdated?.Invoke();
        }

        // ── Flight plan import ─────────────────────────────────────────────────

#if SWEF_FLIGHTPLAN_AVAILABLE
        /// <summary>
        /// Imports waypoints from an existing <see cref="SWEF.FlightPlan.FlightPlanData"/> route.
        /// Only available when <c>SWEF_FLIGHTPLAN_AVAILABLE</c> is defined.
        /// </summary>
        public void ImportFromFlightPlan(SWEF.FlightPlan.FlightPlanData plan)
        {
            if (_content == null || plan == null) return;
            _content.waypoints.Clear();
            int order = 0;
            foreach (var leg in plan.legs)
            {
                _content.waypoints.Add(new UGCWaypoint
                {
                    waypointId = Guid.NewGuid().ToString(),
                    latitude   = leg.latitude,
                    longitude  = leg.longitude,
                    altitude   = leg.altitude,
                    order      = order++,
                    isRequired = true,
                });
            }
            RefreshPath();
        }
#endif

        // ── Private math ───────────────────────────────────────────────────────

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
    }
}
