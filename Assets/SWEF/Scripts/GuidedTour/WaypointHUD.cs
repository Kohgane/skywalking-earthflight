using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.GuidedTour
{
    /// <summary>
    /// HUD overlay that renders on-screen waypoint markers, distance labels, direction arrows
    /// for off-screen waypoints, a tour progress bar, and a waypoint counter.
    /// Uses <see cref="Camera.WorldToScreenPoint"/> for marker positioning.
    /// </summary>
    public class WaypointHUD : MonoBehaviour
    {
        // ── UI Prefabs ────────────────────────────────────────────────────────────
        [Header("Prefabs")]
        [SerializeField] private GameObject waypointMarkerPrefab;
        [SerializeField] private GameObject offScreenArrowPrefab;

        // ── Root canvases ─────────────────────────────────────────────────────────
        [Header("Root Containers")]
        [SerializeField] private RectTransform markerContainer;
        [SerializeField] private RectTransform arrowContainer;

        // ── Progress UI ───────────────────────────────────────────────────────────
        [Header("Progress")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text   waypointCounterText;

        // ── Colors ────────────────────────────────────────────────────────────────
        [Header("Colors")]
        [SerializeField] private Color colorActive   = Color.yellow;
        [SerializeField] private Color colorVisited  = Color.green;
        [SerializeField] private Color colorUpcoming = Color.white;

        // ── State ─────────────────────────────────────────────────────────────────
        private TourManager         _tourManager;
        private Camera              _cam;
        private readonly List<MarkerEntry> _markers = new List<MarkerEntry>();
        private readonly List<GameObject>  _arrows  = new List<GameObject>();

        private struct MarkerEntry
        {
            public GameObject root;
            public Text       distanceLabel;
            public Text       nameLabel;
            public Image      icon;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _cam = Camera.main;
            _tourManager = FindFirstObjectByType<TourManager>();

            if (_tourManager != null)
            {
                _tourManager.OnTourStarted   += OnTourStarted;
                _tourManager.OnTourCompleted += _ => ClearMarkers();
                _tourManager.OnTourCancelled += _ => ClearMarkers();
            }
        }

        private void OnDestroy()
        {
            if (_tourManager != null)
            {
                _tourManager.OnTourStarted   -= OnTourStarted;
            }
        }

        private void LateUpdate()
        {
            if (_tourManager == null || _tourManager.ActiveTour == null) return;

            UpdateMarkers();
            UpdateProgress();
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private void OnTourStarted(TourData tour)
        {
            ClearMarkers();
            BuildMarkers(tour);
        }

        private void BuildMarkers(TourData tour)
        {
            if (waypointMarkerPrefab == null || markerContainer == null) return;

            for (int i = 0; i < tour.waypoints.Count; i++)
            {
                var go     = Instantiate(waypointMarkerPrefab, markerContainer);
                var entry  = new MarkerEntry
                {
                    root          = go,
                    distanceLabel = go.transform.Find("Distance")?.GetComponent<Text>(),
                    nameLabel     = go.transform.Find("Name")?.GetComponent<Text>(),
                    icon          = go.transform.Find("Icon")?.GetComponent<Image>(),
                };

                if (entry.nameLabel != null)
                    entry.nameLabel.text = tour.waypoints[i].waypointName;

                _markers.Add(entry);

                // Create one off-screen arrow per waypoint.
                if (offScreenArrowPrefab != null && arrowContainer != null)
                {
                    var arrow = Instantiate(offScreenArrowPrefab, arrowContainer);
                    arrow.SetActive(false);
                    _arrows.Add(arrow);
                }
                else
                {
                    _arrows.Add(null);
                }
            }
        }

        private void UpdateMarkers()
        {
            if (_cam == null || _tourManager.ActiveTour == null) return;

            var tour        = _tourManager.ActiveTour;
            int currentIdx  = _tourManager.CurrentWaypointIndex;
            var waypoints   = tour.waypoints;
            var screenSize  = new Vector2(Screen.width, Screen.height);
            var camTransform = _cam.transform;

            for (int i = 0; i < _markers.Count && i < waypoints.Count; i++)
            {
                var entry = _markers[i];
                if (entry.root == null) continue;

                var wp = waypoints[i];

                // Determine state color.
                Color stateColor;
                if (i < currentIdx)
                    stateColor = colorVisited;
                else if (i == currentIdx)
                    stateColor = colorActive;
                else
                    stateColor = colorUpcoming;

                if (entry.icon != null)
                    entry.icon.color = stateColor;

                // World → Screen position.
                Vector3 screenPos = _cam.WorldToScreenPoint(wp.position);
                bool    inFront   = screenPos.z > 0f;
                bool    onScreen  = inFront
                    && screenPos.x >= 0 && screenPos.x <= screenSize.x
                    && screenPos.y >= 0 && screenPos.y <= screenSize.y;

                entry.root.SetActive(onScreen);

                if (onScreen)
                {
                    // Position the RectTransform.
                    RectTransform rt = entry.root.GetComponent<RectTransform>();
                    if (rt != null)
                        rt.anchoredPosition = ScreenToCanvasPos(screenPos, screenSize);

                    // Update distance label.
                    float dist = Vector3.Distance(camTransform.position, wp.position);
                    if (entry.distanceLabel != null)
                        entry.distanceLabel.text = dist < 1000f
                            ? $"{dist:F0} m"
                            : $"{dist / 1000f:F1} km";
                }

                // Off-screen arrow.
                if (i < _arrows.Count && _arrows[i] != null)
                {
                    var arrow = _arrows[i];
                    arrow.SetActive(!onScreen && i == currentIdx);
                    if (!onScreen && i == currentIdx)
                    {
                        // Point arrow toward the off-screen waypoint.
                        Vector3 dir = (wp.position - camTransform.position).normalized;
                        Vector3 proj = _cam.WorldToScreenPoint(camTransform.position + dir);
                        Vector2 edgePos = ClampToScreen(proj, screenSize, 60f);
                        RectTransform art = arrow.GetComponent<RectTransform>();
                        if (art != null)
                            art.anchoredPosition = ScreenToCanvasPos(edgePos, screenSize);

                        float halfW = screenSize.x * 0.5f;
                        float halfH = screenSize.y * 0.5f;
                        float angle = Mathf.Atan2(proj.y - halfH,
                                                   proj.x - halfW) * Mathf.Rad2Deg;
                        arrow.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                    }
                }
            }
        }

        private void UpdateProgress()
        {
            if (progressBar != null)
                progressBar.value = _tourManager.Progress;

            if (waypointCounterText != null && _tourManager.ActiveTour != null)
            {
                int total   = _tourManager.ActiveTour.waypoints.Count;
                int current = Mathf.Min(_tourManager.CurrentWaypointIndex + 1, total);
                waypointCounterText.text = $"{current}/{total}";
            }
        }

        private void ClearMarkers()
        {
            foreach (var entry in _markers)
                if (entry.root != null) Destroy(entry.root);
            _markers.Clear();

            foreach (var arrow in _arrows)
                if (arrow != null) Destroy(arrow);
            _arrows.Clear();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private static Vector2 ScreenToCanvasPos(Vector3 screenPos, Vector2 screenSize)
        {
            return new Vector2(screenPos.x - screenSize.x * 0.5f,
                               screenPos.y - screenSize.y * 0.5f);
        }

        private static Vector2 ClampToScreen(Vector3 screenPos, Vector2 screenSize, float padding)
        {
            float x = Mathf.Clamp(screenPos.x, padding, screenSize.x - padding);
            float y = Mathf.Clamp(screenPos.y, padding, screenSize.y - padding);
            return new Vector2(x, y);
        }
    }
}
