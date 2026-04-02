// CourseVisualizerRenderer.cs — SWEF Competitive Racing & Time Trial System (Phase 88)
using System;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_MINIMAP_AVAILABLE
using SWEF.Minimap;
#endif

namespace SWEF.CompetitiveRacing
{
    /// <summary>
    /// Phase 88 — MonoBehaviour that renders the race course in 3D world-space:
    /// a Catmull-Rom spline path via <see cref="LineRenderer"/>, gate prefab instances,
    /// direction arrows, distance labels, and minimap checkpoint blips.
    ///
    /// <para>Call <see cref="BuildCourse"/> after a course is loaded.
    /// Call <see cref="UpdateGateState"/> from <see cref="RaceManager"/> event
    /// handlers to colour-code captured / active / missed gates.</para>
    /// </summary>
    public class CourseVisualizerRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Prefabs")]
        [Tooltip("Gate ring/arch prefab (CheckpointGateController).")]
        [SerializeField] private GameObject _gatePrefab;

        [Tooltip("Direction arrow prefab placed along the route.")]
        [SerializeField] private GameObject _arrowPrefab;

        [Tooltip("Start/finish banner prefab (overrides gate at index 0 / last).")]
        [SerializeField] private GameObject _startFinishBannerPrefab;

        [Header("Path")]
        [Tooltip("LineRenderer used for the Catmull-Rom spline.")]
        [SerializeField] private LineRenderer _lineRenderer;

        [Tooltip("Number of interpolated segments between each pair of checkpoints.")]
        [SerializeField] [Min(2)] private int _splineSegmentsPerGate = 10;

        [Tooltip("Number of direction arrows placed between each pair of checkpoints.")]
        [SerializeField] [Min(0)] private int _arrowsPerSegment = 2;

        [Header("Colours")]
        [SerializeField] private Color _pathUpcomingColor = new Color(1f, 0.85f, 0f, 0.7f);
        [SerializeField] private Color _pathCapturedColor = new Color(0.2f, 0.5f, 1f, 0.4f);

        [Header("Labels")]
        [Tooltip("Prefab with a TextMesh for distance labels (world-space).")]
        [SerializeField] private GameObject _labelPrefab;

        // ── Private State ─────────────────────────────────────────────────────────

        private readonly List<CheckpointGateController> _gates   = new List<CheckpointGateController>();
        private readonly List<GameObject>               _arrows  = new List<GameObject>();
        private readonly List<GameObject>               _labels  = new List<GameObject>();
        private RaceCourse _course;

        // ── Minimap ───────────────────────────────────────────────────────────────

        private readonly List<string> _minimapBlipIds = new List<string>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Builds the full 3D visualisation for <paramref name="course"/>.</summary>
        public void BuildCourse(RaceCourse course)
        {
            if (course == null) return;
            _course = course;

            Clear();
            SpawnGates(course);
            DrawSplinePath(course);
            PlaceArrows(course);
            PlaceDistanceLabels(course);
            RegisterMinimapBlips(course);
        }

        /// <summary>
        /// Updates the visual state of gate <paramref name="index"/> to captured,
        /// missed, or active-next based on the current race progress.
        /// </summary>
        public void UpdateGateState(int index, bool captured, bool missed, bool isNext)
        {
            if (index < 0 || index >= _gates.Count) return;
            var gate = _gates[index];
            if (gate == null) return;

            if (captured)
                gate.MarkCaptured(0f, false);
            else if (missed)
                gate.MarkMissed();
            else
                gate.SetAsActiveNext(isNext);
        }

        /// <summary>Destroys all instantiated visualisation objects.</summary>
        public void Clear()
        {
            foreach (var g in _gates)  { if (g != null) Destroy(g.gameObject); }
            foreach (var a in _arrows) { if (a != null) Destroy(a); }
            foreach (var l in _labels) { if (l != null) Destroy(l); }
            _gates.Clear();
            _arrows.Clear();
            _labels.Clear();

            if (_lineRenderer != null)
                _lineRenderer.positionCount = 0;

            DeregisterMinimapBlips();
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private void SpawnGates(RaceCourse course)
        {
            for (int i = 0; i < course.checkpoints.Count; i++)
            {
                var cp = course.checkpoints[i];

                // Use start/finish banner for first and last checkpoint if loop or banner prefab available
                bool usesBanner = _startFinishBannerPrefab != null
                    && (i == 0 || (course.isLoop && i == course.checkpoints.Count - 1));

                GameObject prefab = usesBanner ? _startFinishBannerPrefab : _gatePrefab;
                if (prefab == null) continue;

                var go   = Instantiate(prefab, transform);
                var gate = go.GetComponent<CheckpointGateController>();
                if (gate == null)
                {
                    gate = go.AddComponent<CheckpointGateController>();
                }
                gate.Initialize(cp);
                gate.OnCheckpointCaptured += HandleGateCaptured;
                _gates.Add(gate);
            }
        }

        private void DrawSplinePath(RaceCourse course)
        {
            if (_lineRenderer == null) return;

            var points = BuildSplinePoints(course);
            _lineRenderer.positionCount = points.Count;
            _lineRenderer.SetPositions(points.ToArray());
            _lineRenderer.startColor = _pathUpcomingColor;
            _lineRenderer.endColor   = _pathUpcomingColor;
        }

        private List<Vector3> BuildSplinePoints(RaceCourse course)
        {
            var cps    = course.checkpoints;
            var result = new List<Vector3>();
            if (cps.Count < 2) return result;

            var worldPts = new List<Vector3>();
            foreach (var cp in cps)
                worldPts.Add(LatLonToWorld(cp.latitude, cp.longitude, cp.altitude));

            for (int i = 0; i < worldPts.Count - 1; i++)
            {
                Vector3 p0 = worldPts[Mathf.Max(0, i - 1)];
                Vector3 p1 = worldPts[i];
                Vector3 p2 = worldPts[Mathf.Min(worldPts.Count - 1, i + 1)];
                Vector3 p3 = worldPts[Mathf.Min(worldPts.Count - 1, i + 2)];

                for (int s = 0; s < _splineSegmentsPerGate; s++)
                {
                    float t = s / (float)_splineSegmentsPerGate;
                    result.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }
            result.Add(worldPts[worldPts.Count - 1]);
            return result;
        }

        private void PlaceArrows(RaceCourse course)
        {
            if (_arrowPrefab == null || _arrowsPerSegment <= 0) return;

            var cps = course.checkpoints;
            for (int i = 0; i < cps.Count - 1; i++)
            {
                Vector3 from = LatLonToWorld(cps[i].latitude,     cps[i].longitude,     cps[i].altitude);
                Vector3 to   = LatLonToWorld(cps[i+1].latitude, cps[i+1].longitude, cps[i+1].altitude);
                Vector3 dir  = (to - from).normalized;

                for (int a = 1; a <= _arrowsPerSegment; a++)
                {
                    float t = a / (float)(_arrowsPerSegment + 1);
                    Vector3 pos = Vector3.Lerp(from, to, t);
                    var arrow   = Instantiate(_arrowPrefab, pos, Quaternion.LookRotation(dir), transform);
                    _arrows.Add(arrow);
                }
            }
        }

        private void PlaceDistanceLabels(RaceCourse course)
        {
            if (_labelPrefab == null) return;

            var cps = course.checkpoints;
            for (int i = 0; i < cps.Count - 1; i++)
            {
                float dist = HaversineMeters(
                    cps[i].latitude, cps[i].longitude,
                    cps[i+1].latitude, cps[i+1].longitude);

                Vector3 from = LatLonToWorld(cps[i].latitude,     cps[i].longitude,     cps[i].altitude);
                Vector3 to   = LatLonToWorld(cps[i+1].latitude, cps[i+1].longitude, cps[i+1].altitude);
                Vector3 mid  = (from + to) * 0.5f + Vector3.up * 100f;

                var lbl = Instantiate(_labelPrefab, mid, Quaternion.identity, transform);
                var tm  = lbl.GetComponent<TextMesh>();
                if (tm != null)
                    tm.text = $"{dist / 1000f:0.0} km";
                _labels.Add(lbl);
            }
        }

        private void RegisterMinimapBlips(RaceCourse course)
        {
#if SWEF_MINIMAP_AVAILABLE
            if (MinimapManager.Instance == null) return;
            foreach (var cp in course.checkpoints)
            {
                Vector3 worldPos = LatLonToWorld(cp.latitude, cp.longitude, cp.altitude);
                string blipId = MinimapManager.Instance.RegisterBlip(worldPos, "checkpoint");
                _minimapBlipIds.Add(blipId);
            }
#endif
        }

        private void DeregisterMinimapBlips()
        {
#if SWEF_MINIMAP_AVAILABLE
            if (MinimapManager.Instance == null) return;
            foreach (var id in _minimapBlipIds)
                MinimapManager.Instance.UnregisterBlip(id);
#endif
            _minimapBlipIds.Clear();
        }

        private void HandleGateCaptured(RaceCheckpoint cp, float splitTime)
        {
            RaceManager.Instance?.CaptureCheckpoint(cp.index);
        }

        // ── Geo / Math Helpers ────────────────────────────────────────────────────

        private static Vector3 LatLonToWorld(double lat, double lon, float alt)
        {
            float x = (float)(lon * 111320.0 * Math.Cos(lat * Mathf.Deg2Rad));
            float z = (float)(lat * 111320.0);
            return new Vector3(x, alt, z);
        }

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

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return 0.5f * (
                  2f * p1
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
        }
    }
}
