using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Handles player interaction with waypoints in the game world.
    /// <para>
    /// Features:
    /// <list type="bullet">
    ///   <item>Proximity glow effect when approaching a waypoint.</item>
    ///   <item>Arrival celebration (particle burst, notification).</item>
    ///   <item>Checkpoint auto-save for checkpoint waypoints.</item>
    ///   <item>Collectible waypoint handling (scenic spots grant score).</item>
    ///   <item>Waypoint info popup with description, altitude, and local weather.</item>
    ///   <item>Missed-waypoint detection (player flies past without entering radius).</item>
    ///   <item>Breadcrumb trail between waypoints.</item>
    ///   <item>Haptic feedback on arrival (where supported).</item>
    /// </list>
    /// </para>
    /// Requires a <see cref="NavigationController"/> in the scene.
    /// </summary>
    public class WaypointInteractionHandler : MonoBehaviour
    {
        #region Constants

        private const float ProximityAlertDistance    = 300f;  // metres — show glow/pulse
        private const float MissedCheckDistance       = 50f;   // metres past waypoint to detect miss
        private const float BreadcrumbSpacing         = 100f;  // metres between breadcrumb markers
        private const float CelebrationDuration       = 2f;    // seconds for arrival celebration
        private const int   ScorePerScenicWaypoint    = 50;
        private const int   ScorePerCheckpoint        = 100;

        #endregion

        #region Inspector

        [Header("Controller")]
        [Tooltip("NavigationController to receive waypoint events from. Auto-found if null.")]
        [SerializeField] private NavigationController navigationController;

        [Header("Player")]
        [SerializeField] private Transform playerTransform;

        [Header("Celebration Effects")]
        [Tooltip("Particle system instantiated on waypoint arrival.")]
        [SerializeField] private GameObject arrivalParticlePrefab;
        [SerializeField] private AudioSource arrivalAudioSource;
        [SerializeField] private AudioClip   arrivalSound;

        [Header("Proximity Glow")]
        [Tooltip("Marker GameObject that pulses/glows when within ProximityAlertDistance.")]
        [SerializeField] private GameObject proximityGlowPrefab;
        private GameObject _activeGlow;

        [Header("Info Popup")]
        [Tooltip("UI panel shown when the player approaches a waypoint.")]
        [SerializeField] private GameObject    infoPopupPanel;
        [SerializeField] private UnityEngine.UI.Text infoPopupTitle;
        [SerializeField] private UnityEngine.UI.Text infoPopupDescription;
        [SerializeField] private UnityEngine.UI.Text infoPopupAltitude;
        [SerializeField] private float              popupShowDistance = 200f;
        [SerializeField] private float              popupHideDistance = 400f;

        [Header("Breadcrumbs")]
        [SerializeField] private bool       showBreadcrumbs       = false;
        [SerializeField] private GameObject breadcrumbPrefab;
        private readonly List<GameObject>   _breadcrumbs = new List<GameObject>();

        [Header("Score")]
        [Tooltip("Score accumulator — incremented on scenic/checkpoint arrivals.")]
        [SerializeField] private int _score;

        #endregion

        #region Private State

        private bool  _inProximityAlert;
        private bool  _popupVisible;
        private float _lastBreadcrumbDistance;
        private Vector3 _lastPlayerPos;

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

            if (infoPopupPanel != null) infoPopupPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (navigationController == null) return;
            navigationController.OnWaypointReached   += HandleWaypointReached;
            navigationController.OnNavigationStarted += HandleNavigationStarted;
            navigationController.OnNavigationEnded   += HandleNavigationEnded;
        }

        private void OnDisable()
        {
            if (navigationController == null) return;
            navigationController.OnWaypointReached   -= HandleWaypointReached;
            navigationController.OnNavigationStarted -= HandleNavigationStarted;
            navigationController.OnNavigationEnded   -= HandleNavigationEnded;
        }

        private void Update()
        {
            if (!navigationController.IsNavigating || playerTransform == null) return;

            UpdateProximityAlert();
            UpdateInfoPopup();

            if (showBreadcrumbs) DropBreadcrumb();

            CheckMissedWaypoint();
        }

        #endregion

        #region Event Handlers

        private void HandleNavigationStarted(System.Collections.Generic.List<Waypoint> waypoints)
        {
            _lastPlayerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
            _lastBreadcrumbDistance = 0f;
            ClearBreadcrumbs();
        }

        private void HandleNavigationEnded()
        {
            if (_activeGlow != null) { Destroy(_activeGlow); _activeGlow = null; }
            HideInfoPopup();
            if (!showBreadcrumbs) ClearBreadcrumbs();
        }

        private void HandleWaypointReached(Waypoint wp, int index)
        {
            StartCoroutine(PlayCelebration(wp));

            // Checkpoint auto-save
            if (wp.type == WaypointType.Checkpoint)
            {
                _score += ScorePerCheckpoint;
                AutoSaveCheckpoint(index);
            }

            // Scenic XP / collectible
            if (wp.type == WaypointType.Landmark || wp.type == WaypointType.Photo)
                _score += ScorePerScenicWaypoint;
        }

        #endregion

        #region Proximity & Popup

        private void UpdateProximityAlert()
        {
            var wp = navigationController.CurrentWaypoint;
            if (wp == null) { SetGlowActive(false); return; }

            bool inRange = navigationController.DistanceToWaypoint <= ProximityAlertDistance;

            if (inRange && !_inProximityAlert)
            {
                _inProximityAlert = true;
                ShowGlow(wp.position);
            }
            else if (!inRange && _inProximityAlert)
            {
                _inProximityAlert = false;
                SetGlowActive(false);
            }
        }

        private void UpdateInfoPopup()
        {
            var wp = navigationController.CurrentWaypoint;
            if (wp == null) { HideInfoPopup(); return; }

            float dist = navigationController.DistanceToWaypoint;

            if (!_popupVisible && dist <= popupShowDistance)
                ShowInfoPopup(wp);
            else if (_popupVisible && dist > popupHideDistance)
                HideInfoPopup();
        }

        private void ShowInfoPopup(Waypoint wp)
        {
            if (infoPopupPanel == null) return;
            _popupVisible = true;
            infoPopupPanel.SetActive(true);
            if (infoPopupTitle       != null) infoPopupTitle.text       = wp.name;
            if (infoPopupDescription != null) infoPopupDescription.text = wp.description;
            if (infoPopupAltitude    != null) infoPopupAltitude.text    = $"{wp.altitudeHint:0} m ASL";
        }

        private void HideInfoPopup()
        {
            _popupVisible = false;
            if (infoPopupPanel != null) infoPopupPanel.SetActive(false);
        }

        #endregion

        #region Celebration

        private IEnumerator PlayCelebration(Waypoint wp)
        {
            // Particle burst at waypoint
            if (arrivalParticlePrefab != null)
            {
                var ps = Instantiate(arrivalParticlePrefab, wp.position, Quaternion.identity);
                Destroy(ps, CelebrationDuration);
            }

            // Sound
            if (arrivalAudioSource != null && arrivalSound != null)
                arrivalAudioSource.PlayOneShot(arrivalSound);

            // Haptic
            TriggerHaptic();

            yield return null;
        }

        private static void TriggerHaptic()
        {
#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }

        #endregion

        #region Glow Marker

        private void ShowGlow(Vector3 position)
        {
            if (proximityGlowPrefab == null) return;
            if (_activeGlow != null) Destroy(_activeGlow);
            _activeGlow = Instantiate(proximityGlowPrefab, position, Quaternion.identity);
        }

        private void SetGlowActive(bool active)
        {
            if (_activeGlow != null) _activeGlow.SetActive(active);
        }

        #endregion

        #region Missed Waypoint

        private void CheckMissedWaypoint()
        {
            var wp = navigationController.CurrentWaypoint;
            if (wp == null || playerTransform == null) return;

            // Detect if player has passed the waypoint (moved beyond radius + MissedCheckDistance)
            // while the distance is now increasing.
            float dist = Vector3.Distance(playerTransform.position, wp.position);
            float prevDist = Vector3.Distance(_lastPlayerPos, wp.position);

            bool wasPastRadius = prevDist > wp.radius + MissedCheckDistance;
            bool movingAway    = dist > prevDist;

            if (wasPastRadius && movingAway && dist > wp.radius * 2f)
            {
                Debug.Log($"[SWEF] WaypointInteractionHandler: Possible missed waypoint '{wp.name}'.");
            }

            _lastPlayerPos = playerTransform.position;
        }

        #endregion

        #region Breadcrumbs

        private void DropBreadcrumb()
        {
            if (breadcrumbPrefab == null || playerTransform == null) return;

            float moved = Vector3.Distance(playerTransform.position, _lastPlayerPos);
            _lastBreadcrumbDistance += moved;
            _lastPlayerPos = playerTransform.position;

            if (_lastBreadcrumbDistance >= BreadcrumbSpacing)
            {
                _lastBreadcrumbDistance = 0f;
                var crumb = Instantiate(breadcrumbPrefab, playerTransform.position, Quaternion.identity);
                _breadcrumbs.Add(crumb);
            }
        }

        private void ClearBreadcrumbs()
        {
            foreach (var b in _breadcrumbs) if (b != null) Destroy(b);
            _breadcrumbs.Clear();
        }

        #endregion

        #region Checkpoint Save

        private void AutoSaveCheckpoint(int waypointIndex)
        {
            PlayerPrefs.SetInt("SWEF_Route_CheckpointIndex", waypointIndex);
            PlayerPrefs.Save();
            Debug.Log($"[SWEF] WaypointInteractionHandler: Checkpoint saved at waypoint index {waypointIndex}.");
        }

        #endregion
    }
}
