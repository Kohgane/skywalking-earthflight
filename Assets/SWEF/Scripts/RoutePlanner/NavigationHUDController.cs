using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Navigation HUD overlay displayed during active route navigation.
    /// <para>
    /// Drives all screen-space navigation elements:
    /// <list type="bullet">
    ///   <item>Direction arrow pointing toward the next waypoint.</item>
    ///   <item>Distance readout (formatted in metres or kilometres).</item>
    ///   <item>ETA display (mm:ss).</item>
    ///   <item>Current and next waypoint names.</item>
    ///   <item>Bearing compass strip.</item>
    ///   <item>Altitude guidance indicator.</item>
    ///   <item>Route completion progress bar.</item>
    ///   <item>Mini upcoming-waypoint list.</item>
    ///   <item>Off-route warning (flashing).</item>
    /// </list>
    /// </para>
    /// Requires a <see cref="NavigationController"/> in the scene.
    /// All UI element references are optional; missing references are silently skipped.
    /// </summary>
    public class NavigationHUDController : MonoBehaviour
    {
        #region Constants

        private const float KmThreshold      = 1000f;  // metres above which km is shown
        private const float OffRouteFlashHz  = 2f;     // flashes per second
        private const float TransitionTime   = 0.4f;   // seconds for waypoint-switch fade
        private const string InfiniteEta     = "--:--";

        #endregion

        #region Inspector — References

        [Header("Controller")]
        [Tooltip("NavigationController to read data from. Auto-found if null.")]
        [SerializeField] private NavigationController navigationController;

        [Header("Direction Arrow")]
        [Tooltip("RectTransform of the direction arrow image (rotated toward waypoint).")]
        [SerializeField] private RectTransform directionArrow;

        [Header("Distance & ETA")]
        [SerializeField] private Text distanceText;
        [SerializeField] private Text etaText;

        [Header("Waypoint Info")]
        [SerializeField] private Text  currentWaypointNameText;
        [SerializeField] private Image currentWaypointIcon;
        [SerializeField] private Text  nextWaypointNameText;
        [SerializeField] private Text  nextWaypointDistanceText;

        [Header("Compass")]
        [Tooltip("RectTransform of the compass strip image (scrolled horizontally).")]
        [SerializeField] private RectTransform compassStrip;
        [SerializeField] private float compassPixelsPerDegree = 4f;

        [Header("Altitude Guidance")]
        [SerializeField] private RectTransform altitudeIndicator;
        [Tooltip("Transform of the player aircraft (used for altitude reads).")]
        [SerializeField] private Transform playerTransform;

        [Header("Progress")]
        [SerializeField] private Slider   progressBar;
        [SerializeField] private Text     progressPercentText;

        [Header("Waypoint Mini-List")]
        [SerializeField] private Transform waypointListParent;
        [SerializeField] private Text      waypointListItemPrefab;
        private readonly List<Text>        _waypointListItems = new List<Text>();

        [Header("Off-Route Warning")]
        [SerializeField] private GameObject offRouteWarning;

        [Header("HUD Root")]
        [SerializeField] private GameObject hudRoot;

        [Header("Settings")]
        [SerializeField] private NavigationSettings settings = new NavigationSettings();

        #endregion

        #region Private State

        private bool  _isOffRoute;
        private float _offRouteFlashTimer;
        private bool  _hudVisible;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (navigationController == null)
                navigationController = FindFirstObjectByType<NavigationController>();

            if (playerTransform == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) playerTransform = go.transform;
            }

            SetHUDVisible(false);
        }

        private void OnEnable()
        {
            if (navigationController == null) return;
            navigationController.OnNavigationStarted += HandleNavigationStarted;
            navigationController.OnNavigationEnded   += HandleNavigationEnded;
            navigationController.OnWaypointReached   += HandleWaypointReached;
            navigationController.OnRouteDeviation    += HandleRouteDeviation;
            navigationController.OnETAUpdated        += HandleETAUpdated;
        }

        private void OnDisable()
        {
            if (navigationController == null) return;
            navigationController.OnNavigationStarted -= HandleNavigationStarted;
            navigationController.OnNavigationEnded   -= HandleNavigationEnded;
            navigationController.OnWaypointReached   -= HandleWaypointReached;
            navigationController.OnRouteDeviation    -= HandleRouteDeviation;
            navigationController.OnETAUpdated        -= HandleETAUpdated;
        }

        private void Update()
        {
            if (!_hudVisible || navigationController == null || !navigationController.IsNavigating) return;

            UpdateDirectionArrow();
            UpdateDistanceText();
            UpdateCompass();
            UpdateAltitudeGuidance();
            UpdateProgress();
            UpdateOffRouteFlash();
        }

        #endregion

        #region Public API

        /// <summary>Shows or hides the entire HUD.</summary>
        public void SetHUDVisible(bool visible)
        {
            _hudVisible = visible;
            if (hudRoot != null) hudRoot.SetActive(visible);
        }

        /// <summary>Applies updated navigation settings to the HUD.</summary>
        public void ApplySettings(NavigationSettings newSettings)
        {
            if (newSettings == null) return;
            settings = newSettings;
            RefreshVisibilityToggles();
        }

        #endregion

        #region Event Handlers

        private void HandleNavigationStarted(List<Waypoint> waypoints)
        {
            SetHUDVisible(true);
            _isOffRoute = false;
            if (offRouteWarning != null) offRouteWarning.SetActive(false);
            RefreshVisibilityToggles();
            BuildWaypointList(waypoints);
            UpdateWaypointInfo();
        }

        private void HandleNavigationEnded()
        {
            SetHUDVisible(false);
            _isOffRoute = false;
        }

        private void HandleWaypointReached(Waypoint wp, int index)
        {
            StartCoroutine(SmoothWaypointTransition());
            UpdateWaypointInfo();
            HighlightWaypointListItem(index);
        }

        private void HandleRouteDeviation(float distance)
        {
            _isOffRoute = true;
            _offRouteFlashTimer = 0f;
        }

        private void HandleETAUpdated(float eta)
        {
            if (etaText == null || !settings.showETA) return;
            etaText.text = FormatETA(eta);
        }

        #endregion

        #region HUD Update Methods

        private void UpdateDirectionArrow()
        {
            if (directionArrow == null) return;

            float bearing = navigationController.BearingToWaypoint;
            float camY    = Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;
            float angle   = bearing - camY;

            directionArrow.localEulerAngles = new Vector3(0f, 0f, -angle);

            if (directionArrow.TryGetComponent<CanvasGroup>(out var cg))
                cg.alpha = settings.guidanceArrowOpacity;
        }

        private void UpdateDistanceText()
        {
            if (distanceText == null || !settings.showDistanceReadout) return;
            distanceText.text = FormatDistance(navigationController.DistanceToWaypoint);
        }

        private void UpdateCompass()
        {
            if (compassStrip == null || !settings.showCompass) return;

            float heading = playerTransform != null ? playerTransform.eulerAngles.y : 0f;
            Vector2 pos   = compassStrip.anchoredPosition;
            pos.x         = -heading * compassPixelsPerDegree;
            compassStrip.anchoredPosition = pos;
        }

        private void UpdateAltitudeGuidance()
        {
            if (altitudeIndicator == null || !settings.showAltitudeGuidance) return;
            if (playerTransform == null || navigationController.CurrentWaypoint == null) return;

            float diff    = navigationController.CurrentWaypoint.altitudeHint - playerTransform.position.y;
            float clamped = Mathf.Clamp(diff, -200f, 200f);
            Vector2 pos   = altitudeIndicator.anchoredPosition;
            pos.y         = clamped * 0.25f; // scale to screen pixels
            altitudeIndicator.anchoredPosition = pos;
        }

        private void UpdateProgress()
        {
            if (!settings.showProgressBar) return;

            float progress = navigationController.Progress;

            if (progressBar != null) progressBar.value = progress;
            if (progressPercentText != null)
                progressPercentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        private void UpdateOffRouteFlash()
        {
            if (offRouteWarning == null) return;

            if (!_isOffRoute)
            {
                offRouteWarning.SetActive(false);
                return;
            }

            _offRouteFlashTimer += Time.deltaTime;
            float half = 1f / (OffRouteFlashHz * 2f);
            offRouteWarning.SetActive((_offRouteFlashTimer % (2f * half)) < half);
        }

        private void UpdateWaypointInfo()
        {
            if (navigationController == null) return;

            var  current = navigationController.CurrentWaypoint;
            var  all     = navigationController.Waypoints;
            int  idx     = navigationController.CurrentWaypointIndex;

            if (currentWaypointNameText != null && settings.showWaypointName)
                currentWaypointNameText.text = current != null ? current.name : string.Empty;

            // Next waypoint preview
            int nextIdx = idx + 1;
            if (all != null && nextIdx < all.Count)
            {
                Waypoint next = all[nextIdx];
                if (nextWaypointNameText != null)
                    nextWaypointNameText.text = next.name;
                if (nextWaypointDistanceText != null && current != null)
                    nextWaypointDistanceText.text = FormatDistance(
                        Vector3.Distance(current.position, next.position));
            }
            else
            {
                if (nextWaypointNameText != null)     nextWaypointNameText.text     = string.Empty;
                if (nextWaypointDistanceText != null) nextWaypointDistanceText.text = string.Empty;
            }
        }

        #endregion

        #region Waypoint Mini-List

        private void BuildWaypointList(IReadOnlyList<Waypoint> waypoints)
        {
            if (waypointListParent == null || waypointListItemPrefab == null || waypoints == null) return;

            // Remove old items
            foreach (var item in _waypointListItems)
                if (item != null) Destroy(item.gameObject);
            _waypointListItems.Clear();

            foreach (var wp in waypoints)
            {
                Text item = Instantiate(waypointListItemPrefab, waypointListParent);
                item.text = wp.name;
                _waypointListItems.Add(item);
            }
        }

        private void HighlightWaypointListItem(int reachedIndex)
        {
            for (int i = 0; i < _waypointListItems.Count; i++)
            {
                if (_waypointListItems[i] == null) continue;
                _waypointListItems[i].color = i <= reachedIndex ? Color.gray : Color.white;
            }
        }

        #endregion

        #region Transitions

        private IEnumerator SmoothWaypointTransition()
        {
            // Brief fade-out then fade-in of waypoint name labels
            CanvasGroup cg = currentWaypointNameText != null
                ? currentWaypointNameText.GetComponent<CanvasGroup>()
                : null;

            if (cg == null) yield break;

            float t = 0f;
            float halfTime = TransitionTime * 0.5f;
            while (t < halfTime)
            {
                t          += Time.deltaTime;
                cg.alpha    = 1f - t / halfTime;
                yield return null;
            }
            UpdateWaypointInfo();
            t = 0f;
            while (t < halfTime)
            {
                t          += Time.deltaTime;
                cg.alpha    = t / halfTime;
                yield return null;
            }
            cg.alpha = 1f;
        }

        #endregion

        #region Formatting

        private void RefreshVisibilityToggles()
        {
            SetOptional(distanceText,             settings.showDistanceReadout);
            SetOptional(etaText,                  settings.showETA);
            SetOptional(currentWaypointNameText,  settings.showWaypointName);
            SetOptional(currentWaypointIcon,      settings.showWaypointName);
            SetOptional(compassStrip,             settings.showCompass);
            SetOptional(altitudeIndicator,        settings.showAltitudeGuidance);
            SetOptional(progressBar,              settings.showProgressBar);
            SetOptional(waypointListParent,       settings.showWaypointList);
        }

        private static void SetOptional(Component c, bool active)
        {
            if (c != null) c.gameObject.SetActive(active);
        }

        private static string FormatDistance(float metres)
        {
            if (metres >= KmThreshold)
                return $"{metres / 1000f:0.0} km";
            return $"{Mathf.RoundToInt(metres)} m";
        }

        private static string FormatETA(float seconds)
        {
            if (float.IsInfinity(seconds) || float.IsNaN(seconds)) return InfiniteEta;
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{mins:D2}:{secs:D2}";
        }

        #endregion
    }
}
