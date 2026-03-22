using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Full pre-flight route planner UI.  Manages four logical views:
    /// <list type="bullet">
    ///   <item><description><b>Route List</b> — browse, search, filter, sort saved routes.</description></item>
    ///   <item><description><b>Route Detail</b> — overview, waypoint list, stats, start button.</description></item>
    ///   <item><description><b>Builder</b> — delegates to <see cref="RouteBuilderController"/>.</description></item>
    ///   <item><description><b>Settings</b> — navigation preferences panel.</description></item>
    /// </list>
    /// Integrates with <c>SWEF.UI.HudBinder</c>, <c>SWEF.Localization.LocalizationManager</c>,
    /// and <c>SWEF.Accessibility.AccessibilityController</c>.
    /// </summary>
    public class RoutePlannerUI : MonoBehaviour
    {
        #region Enum

        private enum View { RouteList, RouteDetail, Builder, Settings }

        #endregion

        #region Inspector — Panels

        [Header("Panels")]
        [SerializeField] private GameObject _routeListPanel;
        [SerializeField] private GameObject _routeDetailPanel;
        [SerializeField] private GameObject _builderPanel;
        [SerializeField] private GameObject _settingsPanel;

        #endregion

        #region Inspector — Route List

        [Header("Route List")]
        [SerializeField] private Transform    _routeListContainer;
        [SerializeField] private GameObject   _routeCardPrefab;
        [SerializeField] private InputField   _searchField;
        [SerializeField] private Dropdown     _filterTypeDropdown;
        [SerializeField] private Dropdown     _sortDropdown;
        [SerializeField] private Button       _newRouteButton;
        [SerializeField] private Button       _importRouteButton;

        #endregion

        #region Inspector — Route Detail

        [Header("Route Detail")]
        [SerializeField] private Text    _detailName;
        [SerializeField] private Text    _detailDescription;
        [SerializeField] private Text    _detailDistance;
        [SerializeField] private Text    _detailDuration;
        [SerializeField] private Text    _detailDifficulty;
        [SerializeField] private Text    _detailWaypointCount;
        [SerializeField] private Button  _startNavigationButton;
        [SerializeField] private Button  _editRouteButton;
        [SerializeField] private Button  _shareRouteButton;
        [SerializeField] private Button  _deleteRouteButton;
        [SerializeField] private Transform _detailWaypointContainer;
        [SerializeField] private GameObject _detailWaypointEntryPrefab;

        #endregion

        #region Inspector — Builder

        [Header("Builder")]
        [SerializeField] private Button _builderUndoButton;
        [SerializeField] private Button _builderRedoButton;
        [SerializeField] private Button _builderPreviewButton;
        [SerializeField] private Button _builderValidateButton;
        [SerializeField] private Button _builderSaveButton;
        [SerializeField] private Text   _builderDistanceLabel;
        [SerializeField] private Text   _builderDurationLabel;
        [SerializeField] private Text   _builderWaypointCountLabel;

        #endregion

        #region Inspector — Settings

        [Header("Settings")]
        [SerializeField] private Toggle  _showPathToggle;
        [SerializeField] private Toggle  _offPathWarningToggle;
        [SerializeField] private Slider  _offPathThresholdSlider;
        [SerializeField] private Toggle  _voiceNavToggle;
        [SerializeField] private Dropdown _navStyleDropdown;

        #endregion

        #region Private State

        private View _currentView = View.RouteList;
        private FlightRoute _selectedRoute;
        private readonly List<GameObject> _routeCards    = new List<GameObject>();
        private readonly List<GameObject> _detailEntries = new List<GameObject>();
        private string _searchQuery = string.Empty;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            WireButtons();
        }

        private void OnEnable()
        {
            ShowView(View.RouteList);
            RefreshRouteList();
        }

        #endregion

        #region View Management

        private void ShowView(View view)
        {
            _currentView = view;

            SetActive(_routeListPanel,   view == View.RouteList);
            SetActive(_routeDetailPanel, view == View.RouteDetail);
            SetActive(_builderPanel,     view == View.Builder);
            SetActive(_settingsPanel,    view == View.Settings);
        }

        #endregion

        #region Route List

        /// <summary>Refreshes the route list applying the current search query, filter and sort.</summary>
        public void RefreshRouteList()
        {
            ClearCards();

            var routes = RoutePlannerManager.Instance?.GetAllRoutes() ?? new List<FlightRoute>();
            routes = ApplyFilter(routes);
            routes = ApplySort(routes);

            foreach (var route in routes)
                SpawnRouteCard(route);
        }

        private void SpawnRouteCard(FlightRoute route)
        {
            if (_routeListContainer == null || _routeCardPrefab == null) return;

            var card  = Instantiate(_routeCardPrefab, _routeListContainer);
            var label = card.GetComponentInChildren<Text>();
            if (label != null) label.text = route.name;

            var btn = card.GetComponent<Button>() ?? card.GetComponentInChildren<Button>();
            if (btn != null)
            {
                FlightRoute captured = route;
                btn.onClick.AddListener(() => SelectRoute(captured));
            }

            _routeCards.Add(card);
        }

        private void ClearCards()
        {
            foreach (var c in _routeCards)
                if (c != null) Destroy(c);
            _routeCards.Clear();
        }

        private List<FlightRoute> ApplyFilter(List<FlightRoute> routes)
        {
            if (!string.IsNullOrEmpty(_searchQuery))
                routes = routes.FindAll(r =>
                    r.name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);

            if (_filterTypeDropdown != null && _filterTypeDropdown.value > 0)
            {
                var type = (RouteType)(_filterTypeDropdown.value - 1);
                routes = routes.FindAll(r => r.routeType == type);
            }

            return routes;
        }

        private List<FlightRoute> ApplySort(List<FlightRoute> routes)
        {
            if (_sortDropdown == null) return routes;

            switch (_sortDropdown.value)
            {
                case 1: routes.Sort((a, b) => b.rating.CompareTo(a.rating));    break;
                case 2: routes.Sort((a, b) => b.downloadCount.CompareTo(a.downloadCount)); break;
                default: routes.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal)); break;
            }
            return routes;
        }

        #endregion

        #region Route Detail

        private void SelectRoute(FlightRoute route)
        {
            _selectedRoute = route;
            ShowView(View.RouteDetail);
            PopulateDetail(route);
        }

        private void PopulateDetail(FlightRoute route)
        {
            if (route == null) return;

            SetText(_detailName,          route.name);
            SetText(_detailDescription,   route.description);
            SetText(_detailDistance,      $"{route.estimatedDistance:F1} km");
            SetText(_detailDuration,      $"{route.estimatedDuration:F0} min");
            SetText(_detailDifficulty,    new string('★', route.difficulty));
            SetText(_detailWaypointCount, $"{route.waypoints.Count} waypoints");

            BuildDetailWaypointList(route);
        }

        private void BuildDetailWaypointList(FlightRoute route)
        {
            foreach (var e in _detailEntries)
                if (e != null) Destroy(e);
            _detailEntries.Clear();

            if (_detailWaypointContainer == null || _detailWaypointEntryPrefab == null) return;

            foreach (var wp in route.waypoints)
            {
                var entry = Instantiate(_detailWaypointEntryPrefab, _detailWaypointContainer);
                var label = entry.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"{wp.index + 1}. {(string.IsNullOrEmpty(wp.name) ? wp.waypointType.ToString() : wp.name)}";
                _detailEntries.Add(entry);
            }
        }

        #endregion

        #region Builder

        private void OpenBuilder(FlightRoute existingRoute = null)
        {
            ShowView(View.Builder);
            RefreshBuilderStats();
        }

        private void RefreshBuilderStats()
        {
            var builder = FindObjectOfType<RouteBuilderController>();
            if (builder == null) return;

            var route = builder.ActiveRoute;
            SetText(_builderDistanceLabel,     $"{route.estimatedDistance:F1} km");
            SetText(_builderDurationLabel,     $"{route.estimatedDuration:F0} min");
            SetText(_builderWaypointCountLabel, $"{route.waypoints.Count} waypoints");

            if (_builderUndoButton != null) _builderUndoButton.interactable = builder.CanUndo;
            if (_builderRedoButton != null) _builderRedoButton.interactable = builder.CanRedo;
        }

        #endregion

        #region Settings

        private void ApplySettings()
        {
            var cfg = RoutePlannerManager.Instance?.Config;
            if (cfg == null) return;

            if (_showPathToggle       != null) cfg.showPathPreview      = _showPathToggle.isOn;
            if (_offPathWarningToggle != null) cfg.offPathWarning        = _offPathWarningToggle.isOn;
            if (_offPathThresholdSlider != null) cfg.offPathThreshold   = _offPathThresholdSlider.value;
            if (_voiceNavToggle       != null) cfg.enableVoiceNavigation = _voiceNavToggle.isOn;
            if (_navStyleDropdown     != null) cfg.navigationStyle       = (NavigationStyle)_navStyleDropdown.value;
        }

        private void PopulateSettings()
        {
            var cfg = RoutePlannerManager.Instance?.Config;
            if (cfg == null) return;

            if (_showPathToggle          != null) _showPathToggle.isOn          = cfg.showPathPreview;
            if (_offPathWarningToggle    != null) _offPathWarningToggle.isOn    = cfg.offPathWarning;
            if (_offPathThresholdSlider  != null) _offPathThresholdSlider.value = cfg.offPathThreshold;
            if (_voiceNavToggle          != null) _voiceNavToggle.isOn          = cfg.enableVoiceNavigation;
            if (_navStyleDropdown        != null) _navStyleDropdown.value       = (int)cfg.navigationStyle;
        }

        #endregion

        #region Button Wiring

        private void WireButtons()
        {
            AddListener(_newRouteButton,        OnNewRouteClicked);
            AddListener(_importRouteButton,     OnImportRouteClicked);
            AddListener(_startNavigationButton, OnStartNavigationClicked);
            AddListener(_editRouteButton,       OnEditRouteClicked);
            AddListener(_shareRouteButton,      OnShareRouteClicked);
            AddListener(_deleteRouteButton,     OnDeleteRouteClicked);
            AddListener(_builderUndoButton,     OnBuilderUndo);
            AddListener(_builderRedoButton,     OnBuilderRedo);
            AddListener(_builderPreviewButton,  OnBuilderPreview);
            AddListener(_builderValidateButton, OnBuilderValidate);
            AddListener(_builderSaveButton,     OnBuilderSave);

            if (_searchField != null)
            {
                _searchField.onValueChanged.AddListener(q => { _searchQuery = q; RefreshRouteList(); });
            }
        }

        private void OnNewRouteClicked()
        {
            OpenBuilder();
            RoutePlannerAnalytics.Instance?.TrackBuilderOpened();
        }

        private void OnImportRouteClicked()
        {
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel("Import Route", "", "swefroute");
            if (!string.IsNullOrEmpty(path))
            {
                var imported = RouteStorageManager.Instance?.ImportRoute(path);
                if (imported != null) RefreshRouteList();
            }
#else
            Debug.Log("[SWEF] RoutePlannerUI: native file picker not implemented on this platform.");
#endif
        }

        private void OnStartNavigationClicked()
        {
            if (_selectedRoute == null) return;
            RoutePlannerManager.Instance?.StartNavigation(_selectedRoute);
            gameObject.SetActive(false);
        }

        private void OnEditRouteClicked()
        {
            if (_selectedRoute == null) return;
            OpenBuilder(_selectedRoute);
        }

        private void OnShareRouteClicked()
        {
            if (_selectedRoute != null)
                RouteShareManager.Instance?.ShareRoute(_selectedRoute);
        }

        private void OnDeleteRouteClicked()
        {
            if (_selectedRoute == null) return;
            RoutePlannerManager.Instance?.DeleteRoute(_selectedRoute.routeId);
            _selectedRoute = null;
            ShowView(View.RouteList);
            RefreshRouteList();
        }

        private void OnBuilderUndo() => FindObjectOfType<RouteBuilderController>()?.Undo();
        private void OnBuilderRedo() => FindObjectOfType<RouteBuilderController>()?.Redo();

        private void OnBuilderPreview() => FindObjectOfType<RouteBuilderController>()?.PreviewRoute();

        private void OnBuilderValidate()
        {
            var builder = FindObjectOfType<RouteBuilderController>();
            if (builder == null) return;

            var warnings = builder.ValidateRoute();
            if (warnings.Count == 0)
                Debug.Log("[SWEF] RoutePlannerUI: route is valid.");
            else
                Debug.LogWarning($"[SWEF] RoutePlannerUI: route warnings:\n" + string.Join("\n", warnings));
        }

        private void OnBuilderSave()
        {
            var builder = FindObjectOfType<RouteBuilderController>();
            if (builder == null) return;

            builder.FinalizeRoute();
            ShowView(View.RouteList);
            RefreshRouteList();
        }

        #endregion

        #region Helpers

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private static void SetText(Text label, string text)
        {
            if (label != null) label.text = text;
        }

        private static void AddListener(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null) btn.onClick.AddListener(action);
        }

        #endregion
    }
}
