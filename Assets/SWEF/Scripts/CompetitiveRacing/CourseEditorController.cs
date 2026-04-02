// CourseEditorController.cs — SWEF Competitive Racing & Time Trial System (Phase 88)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CompetitiveRacing
{
    /// <summary>
    /// Phase 88 — Interactive MonoBehaviour that lets players build race courses
    /// by tapping/clicking to place checkpoint gates on the 3D world.
    ///
    /// <para>Mirrors the pattern of <c>SWEF.RoutePlanner.RouteBuilderController</c>:
    /// each tap ray-casts against the terrain layer to obtain a world position, which
    /// is then converted to lat/lon/alt and stored as a <see cref="RaceCheckpoint"/> in
    /// <see cref="editingCourse"/>.</para>
    /// </summary>
    public class CourseEditorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Placement")]
        [Tooltip("Layer mask used when ray-casting onto terrain for checkpoint placement.")]
        [SerializeField] private LayerMask _terrainMask = ~0;

        [Tooltip("Camera used for screen-to-world ray-casting.")]
        [SerializeField] private Camera _editorCamera;

        [Header("Gate Defaults")]
        [Tooltip("Default gate width in metres.")]
        [SerializeField] [Min(5f)] private float _defaultGateWidth  = 100f;

        [Tooltip("Default gate height in metres.")]
        [SerializeField] [Min(5f)] private float _defaultGateHeight = 60f;

        [Header("Preview")]
        [Tooltip("Fly-through preview speed (world units per second).")]
        [SerializeField] [Min(1f)] private float _previewSpeed = 200f;

        // ── Public State ──────────────────────────────────────────────────────────

        /// <summary>The course currently being built or edited.</summary>
        public RaceCourse editingCourse { get; private set; }

        /// <summary>True while the fly-through preview animation is playing.</summary>
        public bool isPreviewPlaying { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised whenever the editing course is modified.</summary>
        public event Action<RaceCourse>     OnCourseChanged;

        /// <summary>Raised when a checkpoint is added to the course.</summary>
        public event Action<RaceCheckpoint> OnCheckpointAdded;

        /// <summary>Raised when a checkpoint is removed from the course.</summary>
        public event Action<RaceCheckpoint> OnCheckpointRemoved;

        /// <summary>Raised with validation result: (isValid, errorMessage).</summary>
        public event Action<bool, string>   OnValidationResult;

        // ── Undo / Redo ───────────────────────────────────────────────────────────

        private readonly Stack<List<RaceCheckpoint>> _undoStack = new Stack<List<RaceCheckpoint>>();
        private readonly Stack<List<RaceCheckpoint>> _redoStack = new Stack<List<RaceCheckpoint>>();

        // ── Dragging ──────────────────────────────────────────────────────────────

        private int     _draggingIndex = -1;
        private bool    _isDragging;
        private Coroutine _previewCoroutine;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (_editorCamera == null)
                _editorCamera = Camera.main;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Initialises a new empty course ready for editing.</summary>
        public void CreateNewCourse()
        {
            editingCourse = new RaceCourse
            {
                courseId         = Guid.NewGuid().ToString(),
                courseName       = "New Course",
                createdDate      = DateTime.UtcNow,
                lastModifiedDate = DateTime.UtcNow,
                defaultMode      = RaceMode.TimeTrial,
                difficulty       = CourseDifficulty.Intermediate,
                environment      = CourseEnvironment.Mixed,
                lapCount         = 1
            };
            _undoStack.Clear();
            _redoStack.Clear();
            OnCourseChanged?.Invoke(editingCourse);
            Debug.Log("[SWEF] CourseEditorController: New course created.");
        }

        /// <summary>Loads an existing course for editing.</summary>
        public void LoadCourse(RaceCourse course)
        {
            if (course == null) return;
            editingCourse = course;
            _undoStack.Clear();
            _redoStack.Clear();
            RecalculateDistances();
            OnCourseChanged?.Invoke(editingCourse);
            Debug.Log($"[SWEF] CourseEditorController: Loaded course '{course.courseName}'.");
        }

        /// <summary>Finalises and saves the current course (fires <see cref="OnCourseChanged"/>).</summary>
        public void SaveCourse()
        {
            if (editingCourse == null) return;
            ValidateCourse();
            editingCourse.lastModifiedDate = DateTime.UtcNow;
            RecalculateDistances();
            CalculateMedalTimes();
            OnCourseChanged?.Invoke(editingCourse);
            Debug.Log($"[SWEF] CourseEditorController: Course '{editingCourse.courseName}' saved.");
        }

        /// <summary>Validates the current course and fires <see cref="OnValidationResult"/>.</summary>
        public void ValidateCourse()
        {
            if (editingCourse == null)
            {
                OnValidationResult?.Invoke(false, "No course loaded.");
                return;
            }

            int count = editingCourse.checkpoints.Count;
            if (count < CompetitiveRacingConfig.MinCheckpointsRequired)
            {
                OnValidationResult?.Invoke(false,
                    $"Course needs at least {CompetitiveRacingConfig.MinCheckpointsRequired} checkpoints (has {count}).");
                return;
            }

            if (editingCourse.totalDistanceMeters < CompetitiveRacingConfig.MinCourseDistanceMeters)
            {
                OnValidationResult?.Invoke(false,
                    $"Course distance {editingCourse.totalDistanceMeters:0}m is below minimum {CompetitiveRacingConfig.MinCourseDistanceMeters}m.");
                return;
            }

            if (editingCourse.totalDistanceMeters > CompetitiveRacingConfig.MaxCourseDistanceMeters)
            {
                OnValidationResult?.Invoke(false,
                    $"Course distance {editingCourse.totalDistanceMeters:0}m exceeds maximum {CompetitiveRacingConfig.MaxCourseDistanceMeters}m.");
                return;
            }

            // Overlapping gate check
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    float dist = HaversineMeters(
                        editingCourse.checkpoints[i].latitude,
                        editingCourse.checkpoints[i].longitude,
                        editingCourse.checkpoints[j].latitude,
                        editingCourse.checkpoints[j].longitude);
                    if (dist < CompetitiveRacingConfig.MinCheckpointSpacing)
                    {
                        OnValidationResult?.Invoke(false,
                            $"Checkpoints {i} and {j} are too close ({dist:0}m < {CompetitiveRacingConfig.MinCheckpointSpacing}m).");
                        return;
                    }
                }
            }

            OnValidationResult?.Invoke(true, "Course is valid.");
        }

        /// <summary>
        /// Handles a tap/click at <paramref name="screenPos"/> to place or drag a
        /// checkpoint gate.
        /// </summary>
        public void HandleTap(Vector2 screenPos)
        {
            if (editingCourse == null || _editorCamera == null) return;

            Ray ray = _editorCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _terrainMask))
                return;

            PushUndo();
            AddCheckpointAtWorld(hit.point);
        }

        /// <summary>Begins dragging the checkpoint nearest to <paramref name="screenPos"/>.</summary>
        public void BeginDrag(Vector2 screenPos)
        {
            if (editingCourse == null || _editorCamera == null) return;
            _draggingIndex = FindNearestCheckpointScreen(screenPos);
            _isDragging    = _draggingIndex >= 0;
        }

        /// <summary>Updates the dragged checkpoint position.</summary>
        public void UpdateDrag(Vector2 screenPos)
        {
            if (!_isDragging || _draggingIndex < 0) return;
            if (_editorCamera == null) return;

            Ray ray = _editorCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _terrainMask))
                return;

            var cp = editingCourse.checkpoints[_draggingIndex];
            WorldToLatLon(hit.point, out cp.latitude, out cp.longitude);
            cp.altitude = hit.point.y;
            AutoFacePathTangent(cp);
            RecalculateDistances();
            OnCourseChanged?.Invoke(editingCourse);
        }

        /// <summary>Ends a drag operation.</summary>
        public void EndDrag()
        {
            _isDragging    = false;
            _draggingIndex = -1;
        }

        /// <summary>Removes the checkpoint at <paramref name="index"/>.</summary>
        public void RemoveCheckpoint(int index)
        {
            if (editingCourse == null) return;
            if (index < 0 || index >= editingCourse.checkpoints.Count) return;

            PushUndo();
            var removed = editingCourse.checkpoints[index];
            editingCourse.checkpoints.RemoveAt(index);
            RebuildIndices();
            RecalculateDistances();
            OnCheckpointRemoved?.Invoke(removed);
            OnCourseChanged?.Invoke(editingCourse);
        }

        /// <summary>Updates the <see cref="CheckpointType"/> of a checkpoint.</summary>
        public void SetCheckpointType(int index, CheckpointType type)
        {
            if (editingCourse == null || index < 0 || index >= editingCourse.checkpoints.Count)
                return;
            editingCourse.checkpoints[index].type = type;
            OnCourseChanged?.Invoke(editingCourse);
        }

        /// <summary>Updates the trigger radius of a checkpoint.</summary>
        public void SetTriggerRadius(int index, float radius)
        {
            if (editingCourse == null || index < 0 || index >= editingCourse.checkpoints.Count)
                return;
            editingCourse.checkpoints[index].triggerRadius = Mathf.Max(10f, radius);
            OnCourseChanged?.Invoke(editingCourse);
        }

        /// <summary>Sets gate dimensions for a checkpoint.</summary>
        public void SetGateDimensions(int index, float width, float height)
        {
            if (editingCourse == null || index < 0 || index >= editingCourse.checkpoints.Count)
                return;
            editingCourse.checkpoints[index].gateWidth  = Mathf.Max(5f, width);
            editingCourse.checkpoints[index].gateHeight = Mathf.Max(5f, height);
            OnCourseChanged?.Invoke(editingCourse);
        }

        /// <summary>Reverts the last editing action.</summary>
        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            PushRedo();
            editingCourse.checkpoints = DeepCopyCheckpoints(_undoStack.Pop());
            RebuildIndices();
            RecalculateDistances();
            OnCourseChanged?.Invoke(editingCourse);
        }

        /// <summary>Re-applies the last undone action.</summary>
        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            PushUndo();
            editingCourse.checkpoints = DeepCopyCheckpoints(_redoStack.Pop());
            RebuildIndices();
            RecalculateDistances();
            OnCourseChanged?.Invoke(editingCourse);
        }

        /// <summary>Starts the fly-through preview animation.</summary>
        public void StartPreview()
        {
            if (editingCourse == null || editingCourse.checkpoints.Count < 2) return;
            if (_previewCoroutine != null) StopCoroutine(_previewCoroutine);
            isPreviewPlaying = true;
            _previewCoroutine = StartCoroutine(FlyThroughCoroutine());
        }

        /// <summary>Stops the fly-through preview.</summary>
        public void StopPreview()
        {
            if (_previewCoroutine != null) StopCoroutine(_previewCoroutine);
            isPreviewPlaying = false;
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private void AddCheckpointAtWorld(Vector3 worldPos)
        {
            WorldToLatLon(worldPos, out double lat, out double lon);

            var cp = new RaceCheckpoint
            {
                checkpointId  = Guid.NewGuid().ToString(),
                index         = editingCourse.checkpoints.Count,
                type          = editingCourse.checkpoints.Count == 0
                                    ? CheckpointType.Start : CheckpointType.Standard,
                latitude      = lat,
                longitude     = lon,
                altitude      = worldPos.y,
                triggerRadius = CompetitiveRacingConfig.DefaultCheckpointTriggerRadius,
                gateWidth     = _defaultGateWidth,
                gateHeight    = _defaultGateHeight
            };

            AutoFacePathTangent(cp);
            editingCourse.checkpoints.Add(cp);
            CheckAutoLoop();
            RecalculateDistances();

            OnCheckpointAdded?.Invoke(cp);
            OnCourseChanged?.Invoke(editingCourse);
        }

        private void AutoFacePathTangent(RaceCheckpoint cp)
        {
            int idx = cp.index;
            if (idx <= 0 || idx >= editingCourse.checkpoints.Count - 1) return;

            var prev = editingCourse.checkpoints[idx - 1];
            var next = editingCourse.checkpoints[idx + 1];

            Vector3 prevWorld = LatLonToWorld(prev.latitude, prev.longitude, prev.altitude);
            Vector3 nextWorld = LatLonToWorld(next.latitude, next.longitude, next.altitude);
            Vector3 tangent   = (nextWorld - prevWorld).normalized;

            if (tangent != Vector3.zero)
                cp.gateRotation = Quaternion.LookRotation(tangent, Vector3.up);
        }

        private void CheckAutoLoop()
        {
            var cps = editingCourse.checkpoints;
            if (cps.Count < 2) return;

            float dist = HaversineMeters(
                cps[0].latitude, cps[0].longitude,
                cps[cps.Count - 1].latitude, cps[cps.Count - 1].longitude);

            editingCourse.isLoop = dist <= CompetitiveRacingConfig.LoopDetectionThreshold;
        }

        private void RecalculateDistances()
        {
            var cps = editingCourse.checkpoints;
            float total = 0f;
            for (int i = 1; i < cps.Count; i++)
            {
                total += HaversineMeters(
                    cps[i - 1].latitude, cps[i - 1].longitude,
                    cps[i].latitude,     cps[i].longitude);
            }
            editingCourse.totalDistanceMeters = total;

            // Rough time estimate: assume 250 m/s average speed
            const float avgSpeed = 250f;
            editingCourse.estimatedTimeSeconds = total / avgSpeed;
        }

        private void CalculateMedalTimes()
        {
            float est = editingCourse.estimatedTimeSeconds;
            editingCourse.goldTime   = est * CompetitiveRacingConfig.GoldTimeMultiplier;
            editingCourse.silverTime = est * CompetitiveRacingConfig.SilverTimeMultiplier;
            editingCourse.bronzeTime = est * CompetitiveRacingConfig.BronzeTimeMultiplier;
        }

        private void RebuildIndices()
        {
            for (int i = 0; i < editingCourse.checkpoints.Count; i++)
                editingCourse.checkpoints[i].index = i;
        }

        private void PushUndo()
        {
            _undoStack.Push(DeepCopyCheckpoints(editingCourse.checkpoints));
            _redoStack.Clear();
        }

        private void PushRedo()
        {
            _redoStack.Push(DeepCopyCheckpoints(editingCourse.checkpoints));
        }

        private static List<RaceCheckpoint> DeepCopyCheckpoints(List<RaceCheckpoint> src)
        {
            // Deep-copy each checkpoint so that undo/redo state is fully independent.
            // RaceCheckpoint is a reference type, so a shallow list copy would share
            // instances and cause subsequent edits to corrupt the undo stack.
            var copy = new List<RaceCheckpoint>(src.Count);
            foreach (var cp in src)
            {
                copy.Add(new RaceCheckpoint
                {
                    checkpointId  = cp.checkpointId,
                    index         = cp.index,
                    type          = cp.type,
                    latitude      = cp.latitude,
                    longitude     = cp.longitude,
                    altitude      = cp.altitude,
                    triggerRadius = cp.triggerRadius,
                    gateWidth     = cp.gateWidth,
                    gateHeight    = cp.gateHeight,
                    gateRotation  = cp.gateRotation,
                    targetTime    = cp.targetTime,
                    bonusSeconds  = cp.bonusSeconds,
                    isOptional    = cp.isOptional
                });
            }
            return copy;
        }

        private int FindNearestCheckpointScreen(Vector2 screenPos)
        {
            if (_editorCamera == null || editingCourse == null) return -1;
            int   bestIdx  = -1;
            float bestDist = 80f; // pixels threshold
            for (int i = 0; i < editingCourse.checkpoints.Count; i++)
            {
                var cp = editingCourse.checkpoints[i];
                Vector3 world  = LatLonToWorld(cp.latitude, cp.longitude, cp.altitude);
                Vector3 screen = _editorCamera.WorldToScreenPoint(world);
                if (screen.z < 0) continue;
                float d = Vector2.Distance(screenPos, new Vector2(screen.x, screen.y));
                if (d < bestDist) { bestDist = d; bestIdx = i; }
            }
            return bestIdx;
        }

        private System.Collections.IEnumerator FlyThroughCoroutine()
        {
            var cps = editingCourse.checkpoints;
            for (int i = 0; i < cps.Count - 1 && isPreviewPlaying; i++)
            {
                Vector3 from = LatLonToWorld(cps[i].latitude,     cps[i].longitude,     cps[i].altitude);
                Vector3 to   = LatLonToWorld(cps[i+1].latitude, cps[i+1].longitude, cps[i+1].altitude);
                float   dist = Vector3.Distance(from, to);
                float   dur  = dist / _previewSpeed;
                float   t    = 0f;

                while (t < dur && isPreviewPlaying)
                {
                    t += Time.deltaTime;
                    if (_editorCamera != null)
                    {
                        _editorCamera.transform.position = Vector3.Lerp(from, to, t / dur);
                        if ((to - from).sqrMagnitude > 0.001f)
                            _editorCamera.transform.rotation = Quaternion.LookRotation(to - from);
                    }
                    yield return null;
                }
            }
            isPreviewPlaying = false;
        }

        // ── Geo Math ──────────────────────────────────────────────────────────────

        private static float HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0;
            double dLat = (lat2 - lat1) * Mathf.Deg2Rad;
            double dLon = (lon2 - lon1) * Mathf.Deg2Rad;
            double a    = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                        + Math.Cos(lat1 * Mathf.Deg2Rad) * Math.Cos(lat2 * Mathf.Deg2Rad)
                        * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return (float)(R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
        }

        /// <summary>Converts a Unity world-space position to latitude/longitude.</summary>
        private static void WorldToLatLon(Vector3 world, out double lat, out double lon)
        {
            // Assumes 1 Unity unit ≈ 1 metre on a flat-earth approximation.
            // Projects on the XZ plane centred at the world origin.
            lat = world.z / 111320.0;
            lon = world.x / (111320.0 * Math.Cos(lat * Mathf.Deg2Rad));
        }

        private static Vector3 LatLonToWorld(double lat, double lon, float altitude)
        {
            float x = (float)(lon * 111320.0 * Math.Cos(lat * Mathf.Deg2Rad));
            float z = (float)(lat * 111320.0);
            return new Vector3(x, altitude, z);
        }
    }
}
