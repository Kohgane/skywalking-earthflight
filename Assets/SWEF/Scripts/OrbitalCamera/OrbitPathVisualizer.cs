// OrbitPathVisualizer.cs — SWEF Satellite View & Orbital Camera System
using UnityEngine;

namespace SWEF.OrbitalCamera
{
    /// <summary>
    /// Renders the predicted orbital path as a <see cref="LineRenderer"/> trail,
    /// marks apoapsis / periapsis, and displays an orbital period indicator.
    /// </summary>
    public class OrbitPathVisualizer : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Line Renderer")]
        [Tooltip("LineRenderer used to draw the orbit path.")]
        [SerializeField] private LineRenderer orbitLine;

        [Tooltip("LineRenderer used to draw the future-prediction segment.")]
        [SerializeField] private LineRenderer predictionLine;

        [Header("Markers")]
        [Tooltip("Transform placed at the apoapsis (highest) point.")]
        [SerializeField] private Transform apoapsisMarker;

        [Tooltip("Transform placed at the periapsis (lowest) point.")]
        [SerializeField] private Transform periapsisMarker;

        [Header("Path Settings")]
        [Tooltip("Number of line segments in the full orbit path.")]
        [SerializeField, Range(32, 512)] private int orbitSegments = 128;

        [Tooltip("How many seconds ahead to draw the prediction line.")]
        [SerializeField] private float predictionTimeSec = 600f;

        [Tooltip("Colour of the orbit path line.")]
        [SerializeField] private Color pathColor = new Color(0.2f, 0.8f, 1f, 0.8f);

        [Tooltip("Width of the orbit path line (world units).")]
        [SerializeField] private float pathWidth = 5f;

        [Header("Visibility")]
        [Tooltip("Whether the orbit path is visible on start.")]
        [SerializeField] private bool showOnStart = true;

        #endregion

        #region Private State

        private bool _visible;
        private OrbitalMechanicsSimulator _mechanics;

        // Earth mean radius (km → world units; assume 1 km = 1 unit here)
        private const float EarthRadiusKm = 6371f;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _mechanics = FindFirstObjectByType<OrbitalMechanicsSimulator>();
            ShowOrbitPath(showOnStart);
            ApplyLineSettings();
        }

        private void LateUpdate()
        {
            if (!_visible) return;
            RefreshOrbitPath();
            RefreshPredictionPath();
        }

        #endregion

        #region Public API

        /// <summary>Shows or hides the orbit path renderers.</summary>
        /// <param name="show">Visibility state.</param>
        public void ShowOrbitPath(bool show)
        {
            _visible = show;
            if (orbitLine      != null) orbitLine.enabled      = show;
            if (predictionLine != null) predictionLine.enabled = show;
            if (apoapsisMarker  != null) apoapsisMarker.gameObject.SetActive(show);
            if (periapsisMarker != null) periapsisMarker.gameObject.SetActive(show);
        }

        /// <summary>Sets how many seconds ahead the prediction line extends.</summary>
        /// <param name="seconds">Prediction horizon in seconds.</param>
        public void SetPathPredictionTime(float seconds)
        {
            predictionTimeSec = Mathf.Max(0f, seconds);
        }

        /// <summary>Changes the colour of the orbit path line.</summary>
        /// <param name="color">Desired colour.</param>
        public void SetPathColor(Color color)
        {
            pathColor = color;
            ApplyLineSettings();
        }

        /// <summary>Changes the width of the orbit path line.</summary>
        /// <param name="width">Desired width in world units.</param>
        public void SetPathWidth(float width)
        {
            pathWidth = Mathf.Max(0f, width);
            ApplyLineSettings();
        }

        #endregion

        #region Private Helpers

        private void RefreshOrbitPath()
        {
            if (orbitLine == null || _mechanics == null) return;

            var period   = _mechanics.GetOrbitalPeriodSeconds();
            var altKm    = OrbitalCameraController.Instance != null
                ? OrbitalCameraController.Instance.GetCurrentAltitudeKm()
                : 400f;
            var radiusKm = EarthRadiusKm + altKm;

            orbitLine.positionCount = orbitSegments + 1;
            var apsisAlt = float.MinValue;
            var periAlt  = float.MaxValue;
            Vector3 apoPos = Vector3.zero, periPos = Vector3.zero;

            for (var i = 0; i <= orbitSegments; i++)
            {
                var angle = 2f * Mathf.PI * i / orbitSegments;
                var pos   = GroundTrackToWorld(angle, radiusKm);
                orbitLine.SetPosition(i, pos);

                var alt = pos.magnitude;
                if (alt > apsisAlt) { apsisAlt = alt; apoPos  = pos; }
                if (alt < periAlt)  { periAlt  = alt; periPos = pos; }
            }

            if (apoapsisMarker  != null) apoapsisMarker.position  = apoPos;
            if (periapsisMarker != null) periapsisMarker.position = periPos;
        }

        private void RefreshPredictionPath()
        {
            if (predictionLine == null || _mechanics == null) return;

            var period   = _mechanics.GetOrbitalPeriodSeconds();
            var altKm    = OrbitalCameraController.Instance != null
                ? OrbitalCameraController.Instance.GetCurrentAltitudeKm()
                : 400f;
            var radiusKm = EarthRadiusKm + altKm;

            var segments = Mathf.Max(8, orbitSegments / 4);
            predictionLine.positionCount = segments + 1;

            for (var i = 0; i <= segments; i++)
            {
                var t     = predictionTimeSec * i / segments;
                var angle = 2f * Mathf.PI * t / Mathf.Max(period, 1f);
                predictionLine.SetPosition(i, GroundTrackToWorld(angle, radiusKm));
            }
        }

        private static Vector3 GroundTrackToWorld(float angle, float radiusKm)
        {
            return new Vector3(
                Mathf.Cos(angle) * radiusKm,
                0f,
                Mathf.Sin(angle) * radiusKm);
        }

        private void ApplyLineSettings()
        {
            if (orbitLine != null)
            {
                orbitLine.startColor = orbitLine.endColor = pathColor;
                orbitLine.startWidth = orbitLine.endWidth = pathWidth;
            }
            if (predictionLine != null)
            {
                var dim = new Color(pathColor.r * 0.5f, pathColor.g * 0.5f,
                                    pathColor.b * 0.5f, pathColor.a * 0.5f);
                predictionLine.startColor = predictionLine.endColor = dim;
                predictionLine.startWidth = predictionLine.endWidth = pathWidth * 0.6f;
            }
        }

        #endregion
    }
}
