// DronePathEditor.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AdvancedPhotography
{
    /// <summary>
    /// Phase 89 — MonoBehaviour providing an interactive drone flight path builder.
    ///
    /// <para>Players tap-to-place waypoints in 3D space, drag to reposition them,
    /// configure per-waypoint speed and hold time, and preview the path via an
    /// animated Catmull-Rom spline rendered with a <see cref="LineRenderer"/>.</para>
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class DronePathEditor : MonoBehaviour
    {
        #region Events

        /// <summary>Fired when a waypoint is added, passing the new waypoint index.</summary>
        public event Action<int> OnWaypointAdded;

        /// <summary>Fired when a waypoint is removed, passing the removed index.</summary>
        public event Action<int> OnWaypointRemoved;

        /// <summary>Fired when path validation completes, passing whether it is valid.</summary>
        public event Action<bool> OnPathValidated;

        /// <summary>Fired when a path preview flythrough starts.</summary>
        public event Action OnPreviewStarted;

        /// <summary>Fired when a path preview flythrough finishes.</summary>
        public event Action OnPreviewCompleted;

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Drone camera transform used for preview playback.")]
        [SerializeField] private Transform _droneTransform;

        [Tooltip("Player transform for range validation.")]
        [SerializeField] private Transform _playerTransform;

        [Header("Waypoint Defaults")]
        [Tooltip("Default travel speed (m/s) applied to new waypoints.")]
        [SerializeField] [Min(0.1f)] private float _defaultWaypointSpeed = 10f;

        [Tooltip("Default hold time (seconds) at new waypoints.")]
        [SerializeField] [Min(0f)] private float _defaultHoldTime = 0f;

        [Header("Spline Preview")]
        [Tooltip("Number of line segments per waypoint-to-waypoint span.")]
        [SerializeField] [Min(4)] private int _splineSegmentsPerSpan = 20;

        #endregion

        #region Private State

        private LineRenderer _lineRenderer;
        private List<DroneWaypoint> _waypoints = new List<DroneWaypoint>();

        // Undo / redo stacks
        private Stack<List<DroneWaypoint>> _undoStack = new Stack<List<DroneWaypoint>>();
        private Stack<List<DroneWaypoint>> _redoStack = new Stack<List<DroneWaypoint>>();

        private bool _previewing = false;
        private Coroutine _previewCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;
        }

        #endregion

        #region Public API

        /// <summary>Adds a new waypoint at the given world-space position.</summary>
        public void AddWaypoint(Vector3 position)
        {
            PushUndo();

            _waypoints.Add(new DroneWaypoint
            {
                position = position,
                rotation = Quaternion.identity,
                speed    = _defaultWaypointSpeed,
                holdTime = _defaultHoldTime
            });

            _redoStack.Clear();
            int idx = _waypoints.Count - 1;
            UpdateSplineVisual();
            OnWaypointAdded?.Invoke(idx);
            AdvancedPhotographyAnalytics.RecordDronePathCreated();
            Debug.Log($"[SWEF] DronePathEditor: waypoint added at {position} (index {idx})");
        }

        /// <summary>Removes the waypoint at <paramref name="index"/>.</summary>
        public void RemoveWaypoint(int index)
        {
            if (!ValidIndex(index)) return;

            PushUndo();
            _waypoints.RemoveAt(index);
            _redoStack.Clear();
            UpdateSplineVisual();
            OnWaypointRemoved?.Invoke(index);
        }

        /// <summary>Moves the waypoint at <paramref name="index"/> to <paramref name="newPosition"/>.</summary>
        public void MoveWaypoint(int index, Vector3 newPosition)
        {
            if (!ValidIndex(index)) return;

            PushUndo();
            _waypoints[index].position = newPosition;
            _redoStack.Clear();
            UpdateSplineVisual();
        }

        /// <summary>Undoes the last add/remove/move operation.</summary>
        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            _redoStack.Push(CloneList(_waypoints));
            _waypoints = _undoStack.Pop();
            UpdateSplineVisual();
        }

        /// <summary>Redoes a previously undone operation.</summary>
        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            _undoStack.Push(CloneList(_waypoints));
            _waypoints = _redoStack.Pop();
            UpdateSplineVisual();
        }

        /// <summary>Validates the current path against drone constraints.</summary>
        /// <returns><c>true</c> if the path is valid.</returns>
        public bool ValidatePath()
        {
            bool valid = true;

            if (_waypoints.Count < 2)
            {
                Debug.LogWarning("[SWEF] DronePathEditor: path must have at least 2 waypoints.");
                valid = false;
            }

            if (valid && _playerTransform != null)
            {
                foreach (var wp in _waypoints)
                {
                    float dist = Vector3.Distance(wp.position, _playerTransform.position);
                    if (dist > AdvancedPhotographyConfig.DroneMaxRange)
                    {
                        Debug.LogWarning($"[SWEF] DronePathEditor: waypoint at {wp.position} exceeds max range.");
                        valid = false;
                        break;
                    }

                    float alt = wp.position.y;
                    if (alt < 0f)
                    {
                        Debug.LogWarning($"[SWEF] DronePathEditor: waypoint {wp.position} is underground.");
                        valid = false;
                        break;
                    }
                }
            }

            OnPathValidated?.Invoke(valid);
            return valid;
        }

        /// <summary>Begins an animated preview of the path using the drone transform.</summary>
        public void PreviewPath()
        {
            if (_previewing || _waypoints.Count < 2) return;
            _previewCoroutine = StartCoroutine(PreviewCoroutine());
        }

        /// <summary>Returns the current waypoints as a <see cref="DroneFlightPath"/>.</summary>
        public DroneFlightPath GetFlightPath()
        {
            var path = new DroneFlightPath { loop = false };
            path.waypoints.AddRange(_waypoints);

            float total = 0f;
            for (int i = 1; i < _waypoints.Count; i++)
            {
                float dist = Vector3.Distance(_waypoints[i - 1].position, _waypoints[i].position);
                total += dist / Mathf.Max(0.01f, _waypoints[i].speed) + _waypoints[i].holdTime;
            }
            path.totalDuration = total;
            return path;
        }

        /// <summary>Removes all waypoints and clears undo/redo history.</summary>
        public void ClearPath()
        {
            _waypoints.Clear();
            _undoStack.Clear();
            _redoStack.Clear();
            UpdateSplineVisual();
        }

        #endregion

        #region Private — Spline

        private void UpdateSplineVisual()
        {
            if (_waypoints.Count < 2)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            var points = new List<Vector3>();

            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                Vector3 p0 = _waypoints[Mathf.Max(0, i - 1)].position;
                Vector3 p1 = _waypoints[i].position;
                Vector3 p2 = _waypoints[i + 1].position;
                Vector3 p3 = _waypoints[Mathf.Min(_waypoints.Count - 1, i + 2)].position;

                for (int s = 0; s < _splineSegmentsPerSpan; s++)
                {
                    float t = (float)s / _splineSegmentsPerSpan;
                    points.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }

            points.Add(_waypoints[_waypoints.Count - 1].position);

            _lineRenderer.positionCount = points.Count;
            _lineRenderer.SetPositions(points.ToArray());
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        #endregion

        #region Private — Preview

        private IEnumerator PreviewCoroutine()
        {
            _previewing = true;
            OnPreviewStarted?.Invoke();

            if (_droneTransform != null)
            {
                for (int i = 0; i < _waypoints.Count - 1; i++)
                {
                    Vector3 from = _waypoints[i].position;
                    Vector3 to   = _waypoints[i + 1].position;
                    float   spd  = _waypoints[i + 1].speed;

                    while (Vector3.Distance(_droneTransform.position, to) > 0.5f)
                    {
                        _droneTransform.position = Vector3.MoveTowards(
                            _droneTransform.position, to, spd * Time.deltaTime);
                        yield return null;
                    }

                    if (_waypoints[i + 1].holdTime > 0f)
                        yield return new WaitForSeconds(_waypoints[i + 1].holdTime);
                }
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }

            _previewing = false;
            OnPreviewCompleted?.Invoke();
        }

        #endregion

        #region Private — Helpers

        private bool ValidIndex(int index) =>
            index >= 0 && index < _waypoints.Count;

        private void PushUndo()
        {
            _undoStack.Push(CloneList(_waypoints));
        }

        private static List<DroneWaypoint> CloneList(List<DroneWaypoint> source)
        {
            var clone = new List<DroneWaypoint>(source.Count);
            foreach (var wp in source)
                clone.Add(new DroneWaypoint
                {
                    position     = wp.position,
                    rotation     = wp.rotation,
                    speed        = wp.speed,
                    holdTime     = wp.holdTime,
                    lookAtTarget = wp.lookAtTarget
                });
            return clone;
        }

        #endregion
    }
}
