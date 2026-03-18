using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Replay
{
    /// <summary>
    /// Renders a flight path as a 3D world-space line using a <see cref="LineRenderer"/>.
    /// Supports both static replay visualization and live tracking of a moving transform.
    /// Applies altitude-based colour coding and Douglas–Peucker path simplification for
    /// long recordings.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class FlightPathRenderer : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Line Renderer")]
        [SerializeField] private LineRenderer pathLine;
        [SerializeField] private Material     pathMaterial;
        [SerializeField] private float        lineWidth            = 3.0f;
        [SerializeField] private int          maxPoints            = 2000;

        [Header("Altitude Colouring")]
        [SerializeField] private bool     useAltitudeColoring  = true;
        [SerializeField] private Gradient altitudeColorGradient;

        [Header("Simplification")]
        [SerializeField] private float simplificationTolerance = 10.0f;

        // ── Private state ─────────────────────────────────────────────────────────
        private Coroutine         _liveTrackingCoroutine;
        private List<Vector3>     _livePoints = new List<Vector3>();
        private bool              _isTracking;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (pathLine == null)
                pathLine = GetComponent<LineRenderer>();

            ConfigureLineRenderer();
            BuildDefaultGradient();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Renders the flight path from a <see cref="ReplayData"/> instance.</summary>
        public void RenderPath(ReplayData replay)
        {
            if (replay == null || replay.frames.Count == 0)
            {
                ClearPath();
                return;
            }

            var positions = new List<Vector3>(replay.frames.Count);
            foreach (var f in replay.frames)
                positions.Add(f.Position);

            var altitudes = new List<float>(replay.frames.Count);
            foreach (var f in replay.frames)
                altitudes.Add(f.altitude);

            ApplyPath(positions, altitudes);
        }

        /// <summary>Renders the flight path from an explicit list of world positions.</summary>
        public void RenderPath(List<Vector3> positions)
        {
            if (positions == null || positions.Count == 0)
            {
                ClearPath();
                return;
            }
            ApplyPath(positions, null);
        }

        /// <summary>Clears all points from the path line.</summary>
        public void ClearPath()
        {
            if (pathLine != null) pathLine.positionCount = 0;
        }

        /// <summary>
        /// Starts sampling <paramref name="target"/>'s position every
        /// <paramref name="sampleInterval"/> seconds and appending it to the live path.
        /// </summary>
        public void StartLiveTracking(Transform target, float sampleInterval = 0.5f)
        {
            if (target == null) return;
            StopLiveTracking();
            _livePoints.Clear();
            _isTracking            = true;
            _liveTrackingCoroutine = StartCoroutine(LiveTrackCoroutine(target, sampleInterval));
            Debug.Log("[SWEF] FlightPathRenderer: Live tracking started.");
        }

        /// <summary>Stops live tracking. The rendered path remains visible.</summary>
        public void StopLiveTracking()
        {
            _isTracking = false;
            if (_liveTrackingCoroutine != null)
            {
                StopCoroutine(_liveTrackingCoroutine);
                _liveTrackingCoroutine = null;
            }
            Debug.Log("[SWEF] FlightPathRenderer: Live tracking stopped.");
        }

        /// <summary>Shows or hides the <see cref="LineRenderer"/>.</summary>
        public void SetVisible(bool visible)
        {
            if (pathLine != null) pathLine.enabled = visible;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ConfigureLineRenderer()
        {
            if (pathLine == null) return;
            pathLine.useWorldSpace    = true;
            pathLine.startWidth       = lineWidth;
            pathLine.endWidth         = lineWidth;
            pathLine.numCapVertices   = 4;
            pathLine.numCornerVertices = 4;
            if (pathMaterial != null)
                pathLine.material = pathMaterial;
        }

        private void BuildDefaultGradient()
        {
            if (altitudeColorGradient != null && altitudeColorGradient.colorKeys.Length > 0)
                return;

            // Green → Yellow → Orange → Red → Purple → White
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.2f, 0.9f, 0.2f), 0.00f),   // 0 m     green
                    new GradientColorKey(new Color(1.0f, 0.9f, 0.0f), 0.15f),   // ~18 km  yellow
                    new GradientColorKey(new Color(1.0f, 0.5f, 0.0f), 0.35f),   // ~42 km  orange
                    new GradientColorKey(new Color(0.9f, 0.1f, 0.1f), 0.60f),   // ~72 km  red
                    new GradientColorKey(new Color(0.5f, 0.1f, 0.8f), 0.80f),   // ~96 km  purple
                    new GradientColorKey(new Color(1.0f, 1.0f, 1.0f), 1.00f),   // 120 km+ white
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                }
            );
            altitudeColorGradient = gradient;
        }

        private void ApplyPath(List<Vector3> positions, List<float> altitudes)
        {
            // Simplify if too many points
            List<Vector3> simplified  = positions;
            List<float>   simpAlt     = altitudes;

            if (positions.Count > maxPoints)
            {
                simplified = DouglasPeucker(positions, simplificationTolerance);
                simpAlt    = altitudes != null ? ResampleAltitudes(altitudes, positions, simplified) : null;
            }

            // Downsample if still over maxPoints
            if (simplified.Count > maxPoints)
            {
                int step = Mathf.CeilToInt((float)simplified.Count / maxPoints);
                var downsampled = new List<Vector3>();
                var downsampledAlt = simpAlt != null ? new List<float>() : null;
                for (int i = 0; i < simplified.Count; i += step)
                {
                    downsampled.Add(simplified[i]);
                    downsampledAlt?.Add(simpAlt[i]);
                }
                // Always include the final point
                if (downsampled[downsampled.Count - 1] != simplified[simplified.Count - 1])
                {
                    downsampled.Add(simplified[simplified.Count - 1]);
                    downsampledAlt?.Add(simpAlt[simpAlt.Count - 1]);
                }
                simplified = downsampled;
                simpAlt    = downsampledAlt;
            }

            pathLine.positionCount = simplified.Count;
            pathLine.SetPositions(simplified.ToArray());

            if (useAltitudeColoring && simpAlt != null && simpAlt.Count == simplified.Count)
                ApplyAltitudeColors(simplified, simpAlt);
        }

        private void ApplyAltitudeColors(List<Vector3> positions, List<float> altitudes)
        {
            const float maxAlt = 120000f;
            var gradient = new Gradient();
            var colorKeys = new GradientColorKey[Mathf.Min(positions.Count, 8)];
            var alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };

            int step = Mathf.Max(1, positions.Count / colorKeys.Length);
            for (int i = 0; i < colorKeys.Length; i++)
            {
                int idx   = Mathf.Min(i * step, positions.Count - 1);
                float t   = Mathf.Clamp01(altitudes[idx] / maxAlt);
                colorKeys[i] = new GradientColorKey(altitudeColorGradient.Evaluate(t), (float)i / (colorKeys.Length - 1));
            }

            gradient.SetKeys(colorKeys, alphaKeys);
            pathLine.colorGradient = gradient;
        }

        private IEnumerator LiveTrackCoroutine(Transform target, float interval)
        {
            while (_isTracking)
            {
                if (target == null)
                {
                    StopLiveTracking();
                    yield break;
                }

                _livePoints.Add(target.position);

                // Enforce circular buffer
                while (_livePoints.Count > maxPoints)
                    _livePoints.RemoveAt(0);

                RenderPath(_livePoints);
                yield return new WaitForSeconds(interval);
            }
        }

        // ── Douglas–Peucker simplification ────────────────────────────────────────

        private static List<Vector3> DouglasPeucker(List<Vector3> points, float tolerance)
        {
            if (points.Count < 3) return new List<Vector3>(points);
            var result = new List<Vector3>();
            DPRecurse(points, 0, points.Count - 1, tolerance * tolerance, result);
            result.Add(points[points.Count - 1]);
            return result;
        }

        private static void DPRecurse(List<Vector3> pts, int start, int end,
                                       float tolSq, List<Vector3> result)
        {
            if (end <= start + 1)
            {
                result.Add(pts[start]);
                return;
            }

            float maxDist = 0f;
            int   maxIdx  = start;
            Vector3 a     = pts[start];
            Vector3 b     = pts[end];

            for (int i = start + 1; i < end; i++)
            {
                float d = PerpendicularDistanceSq(pts[i], a, b);
                if (d > maxDist) { maxDist = d; maxIdx = i; }
            }

            if (maxDist > tolSq)
            {
                DPRecurse(pts, start, maxIdx, tolSq, result);
                DPRecurse(pts, maxIdx, end,   tolSq, result);
            }
            else
            {
                result.Add(pts[start]);
            }
        }

        private static float PerpendicularDistanceSq(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float abLenSq = ab.sqrMagnitude;
            if (abLenSq == 0f) return (p - a).sqrMagnitude;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / abLenSq);
            Vector3 proj = a + t * ab;
            return (p - proj).sqrMagnitude;
        }

        /// <summary>
        /// Resamples the <paramref name="originalAltitudes"/> list so it aligns with the
        /// <paramref name="simplified"/> point set that was derived from
        /// <paramref name="originalPositions"/>.
        /// </summary>
        private static List<float> ResampleAltitudes(List<float> originalAltitudes,
                                                      List<Vector3> originalPositions,
                                                      List<Vector3> simplified)
        {
            var result = new List<float>(simplified.Count);
            foreach (var sp in simplified)
            {
                int best = 0;
                float bestDist = float.MaxValue;
                for (int i = 0; i < originalPositions.Count; i++)
                {
                    float d = (originalPositions[i] - sp).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; best = i; }
                }
                result.Add(originalAltitudes[best]);
            }
            return result;
        }
    }
}
