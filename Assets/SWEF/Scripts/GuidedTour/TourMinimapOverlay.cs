using UnityEngine;

namespace SWEF.GuidedTour
{
    /// <summary>
    /// Renders the active tour path on a minimap/overview camera using a
    /// <see cref="LineRenderer"/>.  Highlights visited vs. remaining waypoints
    /// and shows the player's current position along the path.
    /// Visibility can be toggled at runtime.
    /// </summary>
    public class TourMinimapOverlay : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Rendering")]
        [SerializeField] private LineRenderer pathLineRenderer;
        [SerializeField] private LineRenderer visitedLineRenderer;
        [SerializeField] private Transform    playerMarker;
        [SerializeField] private float        lineWidth          = 2f;
        [SerializeField] private float        markerYOffset      = 5f;

        [Header("Colors")]
        [SerializeField] private Color colorRemaining = Color.white;
        [SerializeField] private Color colorVisited   = Color.green;
        [SerializeField] private Color colorPlayer    = Color.yellow;

        [Header("Waypoint Markers")]
        [SerializeField] private GameObject waypointMarkerPrefab;

        // ── State ─────────────────────────────────────────────────────────────────
        private TourManager _tourManager;
        private Transform   _playerTransform;
        private bool        _visible = true;

        private GameObject[] _wpMarkers = System.Array.Empty<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _tourManager = FindFirstObjectByType<TourManager>();

            var fc = FindFirstObjectByType<Flight.FlightController>();
            if (fc != null) _playerTransform = fc.transform;

            if (_tourManager != null)
            {
                _tourManager.OnTourStarted   += BuildPath;
                _tourManager.OnTourCompleted += _ => ClearPath();
                _tourManager.OnTourCancelled += _ => ClearPath();
            }

            InitLineRenderers();
        }

        private void OnDestroy()
        {
            if (_tourManager != null)
            {
                _tourManager.OnTourStarted   -= BuildPath;
            }
        }

        private void LateUpdate()
        {
            if (!_visible || _tourManager == null || _tourManager.ActiveTour == null) return;
            UpdateVisuals();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the minimap overlay.</summary>
        public void Show()
        {
            _visible = true;
            SetLineActive(true);
        }

        /// <summary>Hides the minimap overlay.</summary>
        public void Hide()
        {
            _visible = false;
            SetLineActive(false);
        }

        /// <summary>Toggles minimap overlay visibility.</summary>
        public void Toggle() { if (_visible) Hide(); else Show(); }

        // ── Internal ──────────────────────────────────────────────────────────────
        private void InitLineRenderers()
        {
            if (pathLineRenderer != null)
            {
                pathLineRenderer.startWidth = lineWidth;
                pathLineRenderer.endWidth   = lineWidth;
                pathLineRenderer.startColor = colorRemaining;
                pathLineRenderer.endColor   = colorRemaining;
                pathLineRenderer.useWorldSpace = true;
            }

            if (visitedLineRenderer != null)
            {
                visitedLineRenderer.startWidth = lineWidth;
                visitedLineRenderer.endWidth   = lineWidth;
                visitedLineRenderer.startColor = colorVisited;
                visitedLineRenderer.endColor   = colorVisited;
                visitedLineRenderer.useWorldSpace = true;
            }

            if (playerMarker != null)
            {
                var mr = playerMarker.GetComponent<MeshRenderer>() ?? playerMarker.GetComponentInChildren<MeshRenderer>();
                if (mr != null) mr.material.color = colorPlayer;
            }
        }

        private void BuildPath(TourData tour)
        {
            ClearPath();
            if (tour == null || tour.waypoints == null || tour.waypoints.Count == 0) return;

            // Remaining path — all waypoints.
            if (pathLineRenderer != null)
            {
                pathLineRenderer.positionCount = tour.waypoints.Count;
                for (int i = 0; i < tour.waypoints.Count; i++)
                    pathLineRenderer.SetPosition(i, tour.waypoints[i].position + Vector3.up * markerYOffset);
            }

            // Spawn waypoint markers.
            if (waypointMarkerPrefab != null)
            {
                _wpMarkers = new GameObject[tour.waypoints.Count];
                for (int i = 0; i < tour.waypoints.Count; i++)
                {
                    var pos = tour.waypoints[i].position + Vector3.up * markerYOffset;
                    _wpMarkers[i] = Instantiate(waypointMarkerPrefab, pos, Quaternion.identity, transform);
                }
            }
        }

        private void UpdateVisuals()
        {
            var tour       = _tourManager.ActiveTour;
            int currentIdx = _tourManager.CurrentWaypointIndex;

            // Update visited segment.
            if (visitedLineRenderer != null && currentIdx > 0)
            {
                visitedLineRenderer.positionCount = currentIdx + 1;
                for (int i = 0; i <= currentIdx && i < tour.waypoints.Count; i++)
                    visitedLineRenderer.SetPosition(i, tour.waypoints[i].position + Vector3.up * markerYOffset);
            }

            // Colour waypoint markers.
            for (int i = 0; i < _wpMarkers.Length && i < tour.waypoints.Count; i++)
            {
                if (_wpMarkers[i] == null) continue;
                var mr = _wpMarkers[i].GetComponent<Renderer>();
                if (mr == null) mr = _wpMarkers[i].GetComponentInChildren<Renderer>();
                if (mr != null)
                {
                    mr.material.color = i < currentIdx  ? colorVisited
                                      : i == currentIdx ? colorPlayer
                                      : colorRemaining;
                }
            }

            // Update player marker.
            if (playerMarker != null && _playerTransform != null)
                playerMarker.position = _playerTransform.position + Vector3.up * markerYOffset;
        }

        private void ClearPath()
        {
            if (pathLineRenderer    != null) pathLineRenderer.positionCount    = 0;
            if (visitedLineRenderer != null) visitedLineRenderer.positionCount = 0;

            foreach (var marker in _wpMarkers)
                if (marker != null) Destroy(marker);
            _wpMarkers = System.Array.Empty<GameObject>();
        }

        private void SetLineActive(bool active)
        {
            if (pathLineRenderer    != null) pathLineRenderer.enabled    = active;
            if (visitedLineRenderer != null) visitedLineRenderer.enabled = active;
            foreach (var marker in _wpMarkers)
                if (marker != null) marker.SetActive(active);
        }
    }
}
