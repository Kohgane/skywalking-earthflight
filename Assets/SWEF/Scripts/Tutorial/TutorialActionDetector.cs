using System;
using UnityEngine;
using SWEF.Flight;
using SWEF.Screenshot;
using SWEF.Teleport;

namespace SWEF.Tutorial
{
    /// <summary>
    /// Monitors player input and gameplay events to detect when the player has
    /// performed a specific action required by the current tutorial step.
    /// Subscribe to <see cref="OnActionDetected"/> to receive the action identifier.
    /// </summary>
    public class TutorialActionDetector : MonoBehaviour
    {
        // ── Serialized references (auto-found when null) ──────────────────────
        [SerializeField] private FlightController    flightController;
        [SerializeField] private AltitudeController  altitudeController;
        [SerializeField] private TouchInputRouter    touchInputRouter;
        [SerializeField] private ScreenshotController screenshotController;
        [SerializeField] private TeleportController  teleportController;

        // ── Sensitivity thresholds ────────────────────────────────────────────
        [Header("Detection Thresholds")]
        [SerializeField] private float throttleChangeDelta  = 0.05f;
        [SerializeField] private float altitudeChangeDelta  = 2f;
        [SerializeField] private float lookAroundDragPixels = 20f;

        /// <summary>
        /// Raised when the player performs a recognised action.
        /// The string argument is the action identifier (e.g. <c>"throttle_change"</c>).
        /// </summary>
        public event Action<string> OnActionDetected;

        // ── Internal tracking state ───────────────────────────────────────────
        private float   _lastThrottle;
        private float   _lastAltitude;
        private Vector2 _dragStart;
        private bool    _dragging;
        private bool    _active;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Enables or disables action detection.</summary>
        public void SetActive(bool active) => _active = active;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            AutoWireReferences();
            SubscribeEvents();
            CaptureBaseline();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            if (!_active) return;
            DetectThrottleChange();
            DetectAltitudeChange();
            DetectLookAround();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void AutoWireReferences()
        {
            if (flightController   == null) flightController   = FindFirstObjectByType<FlightController>();
            if (altitudeController == null) altitudeController = FindFirstObjectByType<AltitudeController>();
            if (touchInputRouter   == null) touchInputRouter   = FindFirstObjectByType<TouchInputRouter>();
            if (screenshotController == null) screenshotController = FindFirstObjectByType<ScreenshotController>();
            if (teleportController == null) teleportController = FindFirstObjectByType<TeleportController>();
        }

        private void SubscribeEvents()
        {
            if (screenshotController != null)
                screenshotController.OnScreenshotCaptured += OnScreenshotCaptured;

            if (teleportController != null)
                teleportController.OnTeleportStarted += OnTeleportOpened;
        }

        private void UnsubscribeEvents()
        {
            if (screenshotController != null)
                screenshotController.OnScreenshotCaptured -= OnScreenshotCaptured;

            if (teleportController != null)
                teleportController.OnTeleportStarted -= OnTeleportOpened;
        }

        private void CaptureBaseline()
        {
            _lastThrottle = flightController != null ? flightController.Throttle01 : 0f;
            _lastAltitude = altitudeController != null ? altitudeController.TargetAltitudeMeters : 0f;
        }

        // ── Per-frame detectors ───────────────────────────────────────────────

        private void DetectThrottleChange()
        {
            if (flightController == null) return;
            float current = flightController.Throttle01;
            if (Mathf.Abs(current - _lastThrottle) >= throttleChangeDelta)
            {
                _lastThrottle = current;
                Fire("throttle_change");
            }
        }

        private void DetectAltitudeChange()
        {
            if (altitudeController == null) return;
            float current = altitudeController.TargetAltitudeMeters;
            if (Mathf.Abs(current - _lastAltitude) >= altitudeChangeDelta)
            {
                _lastAltitude = current;
                Fire("altitude_change");
            }
        }

        private void DetectLookAround()
        {
            // Touch input
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    _dragging  = true;
                    _dragStart = t.position;
                }
                else if (_dragging && t.phase == TouchPhase.Moved)
                {
                    if (Vector2.Distance(t.position, _dragStart) >= lookAroundDragPixels)
                    {
                        _dragging = false;
                        Fire("look_around");
                    }
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    _dragging = false;
                }
            }

            // Mouse input (Editor / desktop)
            if (Input.GetMouseButtonDown(0))
            {
                _dragging  = true;
                _dragStart = Input.mousePosition;
            }
            else if (_dragging && Input.GetMouseButton(0))
            {
                if (Vector2.Distance(Input.mousePosition, _dragStart) >= lookAroundDragPixels)
                {
                    _dragging = false;
                    Fire("look_around");
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _dragging = false;
            }
        }

        // ── Event callbacks ───────────────────────────────────────────────────

        /// <summary>Called by roll-left UI button via InteractiveTutorialManager.</summary>
        public void NotifyRollLeft() => Fire("roll_left");

        /// <summary>Called by roll-right UI button via InteractiveTutorialManager.</summary>
        public void NotifyRollRight() => Fire("roll_right");

        /// <summary>Called when the comfort mode toggle is changed.</summary>
        public void NotifyComfortToggle() => Fire("comfort_toggle");

        /// <summary>Called when the settings panel is opened.</summary>
        public void NotifySettingsOpen() => Fire("settings_open");

        private void OnScreenshotCaptured(string _) => Fire("screenshot_take");

        private void OnTeleportOpened() => Fire("teleport_open");

        // ── Internal fire ─────────────────────────────────────────────────────

        private void Fire(string actionId)
        {
            if (!_active) return;
            OnActionDetected?.Invoke(actionId);
        }
    }
}
