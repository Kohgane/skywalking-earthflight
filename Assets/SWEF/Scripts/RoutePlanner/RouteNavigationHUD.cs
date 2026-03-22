using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — In-flight navigation HUD overlay that displays next-waypoint guidance,
    /// overall route progress, per-waypoint constraints (altitude/speed), off-path warnings,
    /// and a completion celebration screen.
    /// Integrates with <c>SWEF.UI.HudBinder</c>, <c>SWEF.GuidedTour.WaypointNavigator</c>,
    /// and <c>SWEF.Accessibility.AccessibilityController</c>.
    /// </summary>
    public class RouteNavigationHUD : MonoBehaviour
    {
        #region Inspector

        [Header("Next Waypoint Panel")]
        [SerializeField] private Text  _nextWaypointName;
        [SerializeField] private Text  _nextWaypointDistance;
        [SerializeField] private Text  _nextWaypointETA;
        [SerializeField] private Image _directionArrow;

        [Header("Progress")]
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private Text   _progressLabel;

        [Header("Route Stats")]
        [SerializeField] private Text _elapsedTimeLabel;
        [SerializeField] private Text _distanceTraveledLabel;
        [SerializeField] private Text _avgSpeedLabel;
        [SerializeField] private Text _deviationLabel;

        [Header("Waypoint List")]
        [SerializeField] private Transform _waypointListContainer;
        [SerializeField] private GameObject _waypointEntryPrefab;

        [Header("Constraint Hints")]
        [SerializeField] private GameObject _altitudeHintPanel;
        [SerializeField] private Text       _altitudeHintLabel;
        [SerializeField] private GameObject _speedHintPanel;
        [SerializeField] private Text       _speedHintLabel;

        [Header("Off-Path Warning")]
        [SerializeField] private GameObject _offPathPanel;
        [SerializeField] private Text       _offPathLabel;

        [Header("Completion")]
        [SerializeField] private GameObject _completionPanel;
        [SerializeField] private Text       _completionTimeLabel;
        [SerializeField] private Text       _completionDistanceLabel;
        [SerializeField] private Slider     _ratingSlider;

        [Header("Turn-by-Turn")]
        [SerializeField] private Text       _turnByTurnLabel;
        [SerializeField] private float      _turnByTurnDisplayDist = 2000f;

        #endregion

        #region Private State

        private RoutePlannerManager _mgr;
        private readonly List<GameObject> _waypointEntries = new List<GameObject>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetPanel(_completionPanel, false);
            SetPanel(_offPathPanel,    false);
            SetPanel(_altitudeHintPanel, false);
            SetPanel(_speedHintPanel,    false);
        }

        private void OnEnable()
        {
            _mgr = RoutePlannerManager.Instance;
            if (_mgr == null) return;

            _mgr.OnNavigationStarted  += HandleNavigationStarted;
            _mgr.OnWaypointReached    += HandleWaypointReached;
            _mgr.OnRouteCompleted     += HandleRouteCompleted;
            _mgr.OnOffPath            += HandleOffPath;
            _mgr.OnBackOnPath         += HandleBackOnPath;
            _mgr.OnNavigationPaused   += HandlePaused;
            _mgr.OnNavigationResumed  += HandleResumed;
        }

        private void OnDisable()
        {
            if (_mgr == null) return;

            _mgr.OnNavigationStarted  -= HandleNavigationStarted;
            _mgr.OnWaypointReached    -= HandleWaypointReached;
            _mgr.OnRouteCompleted     -= HandleRouteCompleted;
            _mgr.OnOffPath            -= HandleOffPath;
            _mgr.OnBackOnPath         -= HandleBackOnPath;
            _mgr.OnNavigationPaused   -= HandlePaused;
            _mgr.OnNavigationResumed  -= HandleResumed;
        }

        private void Update()
        {
            if (_mgr == null || !_mgr.IsNavigating) return;
            RefreshHUD();
        }

        #endregion

        #region HUD Refresh

        private void RefreshHUD()
        {
            var progress = _mgr.RouteProgress;
            var route    = _mgr.ActiveRoute;
            var next     = _mgr.NextWaypoint;

            if (progress == null || route == null) return;

            // -- Next waypoint panel
            if (_nextWaypointName     != null) _nextWaypointName.text     = next != null ? next.name : "—";
            if (_nextWaypointDistance != null) _nextWaypointDistance.text  = FormatDistance(_mgr.DistanceToNext);
            if (_nextWaypointETA      != null) _nextWaypointETA.text       = FormatETA(_mgr.ETA);

            // -- Progress
            int total    = route.waypoints.Count;
            int reached  = progress.waypointsReached.Count;
            float pct    = total > 0 ? (float)reached / total : 0f;
            if (_progressSlider != null) _progressSlider.value = pct;
            if (_progressLabel  != null) _progressLabel.text   = $"{reached}/{total}";

            // -- Route stats
            if (_elapsedTimeLabel      != null) _elapsedTimeLabel.text      = FormatTime(progress.elapsedTime);
            if (_distanceTraveledLabel != null) _distanceTraveledLabel.text = $"{progress.distanceTraveled:F1} km";
            if (_deviationLabel        != null) _deviationLabel.text        = $"{progress.deviations}";

            // -- Constraint hints
            RefreshConstraintHints(next);

            // -- Turn-by-turn
            RefreshTurnByTurn(next);
        }

        private void RefreshConstraintHints(RouteWaypoint next)
        {
            if (next == null)
            {
                SetPanel(_altitudeHintPanel, false);
                SetPanel(_speedHintPanel,    false);
                return;
            }

            bool showAlt   = next.waypointType == WaypointType.Altitude   && next.requiredAltitude >= 0f;
            bool showSpeed = next.waypointType == WaypointType.SpeedGate  && next.requiredSpeed    >= 0f;

            SetPanel(_altitudeHintPanel, showAlt);
            SetPanel(_speedHintPanel,    showSpeed);

            if (showAlt && _altitudeHintLabel != null)
                _altitudeHintLabel.text = $"Reach {next.requiredAltitude:F0} m altitude";

            if (showSpeed && _speedHintLabel != null)
                _speedHintLabel.text = $"Speed up to {next.requiredSpeed:F0} km/h";
        }

        private void RefreshTurnByTurn(RouteWaypoint next)
        {
            if (_turnByTurnLabel == null || next == null) return;

            if (_mgr.DistanceToNext <= _turnByTurnDisplayDist && !string.IsNullOrEmpty(next.name))
                _turnByTurnLabel.text = $"In {FormatDistance(_mgr.DistanceToNext)}, fly towards {next.name}";
            else
                _turnByTurnLabel.text = string.Empty;
        }

        #endregion

        #region Event Handlers

        private void HandleNavigationStarted(FlightRoute route)
        {
            SetPanel(_completionPanel, false);
            RebuildWaypointList(route);
        }

        private void HandleWaypointReached(RouteWaypoint wp, int index)
        {
            UpdateWaypointEntryReached(index);
            RoutePathRenderer.Instance?.MarkWaypointReached(index);
            RoutePathRenderer.Instance?.HighlightWaypoint(index + 1);
        }

        private void HandleRouteCompleted(FlightRoute route, RouteProgress progress)
        {
            SetPanel(_completionPanel, true);
            if (_completionTimeLabel     != null) _completionTimeLabel.text     = FormatTime(progress.elapsedTime);
            if (_completionDistanceLabel != null) _completionDistanceLabel.text = $"{progress.distanceTraveled:F1} km";
        }

        private void HandleOffPath()     => SetPanel(_offPathPanel, true);
        private void HandleBackOnPath()  => SetPanel(_offPathPanel, false);
        private void HandlePaused()      { /* optionally show pause overlay */ }
        private void HandleResumed()     { /* hide pause overlay */ }

        #endregion

        #region Waypoint List

        private void RebuildWaypointList(FlightRoute route)
        {
            foreach (var e in _waypointEntries)
                if (e != null) Destroy(e);
            _waypointEntries.Clear();

            if (_waypointListContainer == null || _waypointEntryPrefab == null) return;

            foreach (var wp in route.waypoints)
            {
                var entry = Instantiate(_waypointEntryPrefab, _waypointListContainer);
                var label = entry.GetComponentInChildren<Text>();
                if (label != null) label.text = string.IsNullOrEmpty(wp.name)
                    ? $"Waypoint {wp.index + 1}" : wp.name;
                _waypointEntries.Add(entry);
            }
        }

        private void UpdateWaypointEntryReached(int index)
        {
            if (index < 0 || index >= _waypointEntries.Count) return;
            var entry = _waypointEntries[index];
            if (entry == null) return;

            var label = entry.GetComponentInChildren<Text>();
            if (label != null) label.color = Color.grey;
        }

        #endregion

        #region Rating

        /// <summary>
        /// Called by the UI when the player submits a route rating after completion.
        /// </summary>
        public void SubmitRating()
        {
            if (_ratingSlider == null || _mgr?.ActiveRoute == null) return;
            float rating = _ratingSlider.value;
            RouteShareManager.Instance?.RateRoute(_mgr.ActiveRoute.routeId, rating);
            SetPanel(_completionPanel, false);
        }

        #endregion

        #region Formatting Helpers

        private static string FormatDistance(float metres)
        {
            return metres >= 1000f
                ? $"{metres / 1000f:F1} km"
                : $"{metres:F0} m";
        }

        private static string FormatETA(float seconds)
        {
            if (seconds >= float.MaxValue || seconds < 0f) return "—";
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return m > 0 ? $"{m}m {s:D2}s" : $"{s}s";
        }

        private static string FormatTime(float seconds)
        {
            int h = Mathf.FloorToInt(seconds / 3600f);
            int m = Mathf.FloorToInt((seconds % 3600f) / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return h > 0 ? $"{h}:{m:D2}:{s:D2}" : $"{m}:{s:D2}";
        }

        private static void SetPanel(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        #endregion
    }
}
