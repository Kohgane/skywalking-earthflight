using UnityEngine;
using SWEF.Flight;

namespace SWEF.GuidedTour
{
    /// <summary>
    /// Provides navigation assistance toward the next tour waypoint and offers
    /// an optional auto-pilot mode that smoothly steers the player via
    /// <see cref="FlightController"/>.
    /// </summary>
    public class WaypointNavigator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private FlightController flightController;
        [SerializeField] private Transform playerTransform;

        [Header("Auto-Pilot")]
        [SerializeField] private float autopilotSpeed         = 0.5f;  // throttle 0–1
        [SerializeField] private float autopilotTurnRate      = 2.0f;  // normalised units/s
        [SerializeField] private float autopilotPitchRate     = 1.0f;
        [SerializeField] private float autopilotAltitudeGain  = 50f;   // metres above WP

        // ── State ─────────────────────────────────────────────────────────────────
        /// <summary>Distance in metres to the next waypoint, or <c>float.MaxValue</c> if unavailable.</summary>
        public float DistanceToNextWaypoint { get; private set; } = float.MaxValue;

        /// <summary>Magnetic bearing in degrees [0, 360) to the next waypoint.</summary>
        public float BearingToNextWaypoint { get; private set; }

        /// <summary>Whether auto-pilot is currently engaged.</summary>
        public bool IsAutoPilotActive { get; private set; }

        private TourManager _tourManager;
        private Vector3     _targetPosition;
        private bool        _hasTarget;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (flightController == null)
                flightController = FindFirstObjectByType<FlightController>();
            if (playerTransform == null && flightController != null)
                playerTransform = flightController.transform;
        }

        private void OnEnable()
        {
            _tourManager = FindFirstObjectByType<TourManager>();
            if (_tourManager != null)
            {
                _tourManager.OnWaypointReached += HandleWaypointReached;
                _tourManager.OnTourCancelled   += _ => ClearTarget();
                _tourManager.OnTourCompleted   += _ => ClearTarget();
            }
        }

        private void OnDisable()
        {
            if (_tourManager != null)
            {
                _tourManager.OnWaypointReached -= HandleWaypointReached;
            }
        }

        private void Update()
        {
            UpdateNavigationData();
            if (IsAutoPilotActive) RunAutoPilot();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Engages auto-pilot, steering the player toward the current waypoint.</summary>
        public void EnableAutoPilot()
        {
            if (flightController == null)
            {
                Debug.LogWarning("[SWEF] WaypointNavigator.EnableAutoPilot: no FlightController found.");
                return;
            }
            IsAutoPilotActive = true;
            Debug.Log("[SWEF] WaypointNavigator: Auto-pilot engaged.");
        }

        /// <summary>Disengages auto-pilot and returns control to the player.</summary>
        public void DisableAutoPilot()
        {
            IsAutoPilotActive = false;
            Debug.Log("[SWEF] WaypointNavigator: Auto-pilot disengaged.");
        }

        /// <summary>Sets the normalised throttle (0–1) used while auto-pilot is active.</summary>
        /// <param name="speed">Normalised speed in [0, 1].</param>
        public void SetAutoPilotSpeed(float speed)
        {
            autopilotSpeed = Mathf.Clamp01(speed);
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private void HandleWaypointReached(int index, TourData.WaypointData data)
        {
            // Advance the target to the next waypoint, if one exists.
            if (_tourManager == null || _tourManager.ActiveTour == null) return;
            int next = index + 1;
            if (next < _tourManager.ActiveTour.waypoints.Count)
            {
                _targetPosition = _tourManager.ActiveTour.waypoints[next].position;
                _hasTarget      = true;
            }
            else
            {
                ClearTarget();
            }
        }

        private void ClearTarget()
        {
            _hasTarget            = false;
            DistanceToNextWaypoint = float.MaxValue;
            DisableAutoPilot();
        }

        private void UpdateNavigationData()
        {
            if (!_hasTarget || playerTransform == null)
            {
                // Derive target from TourManager current waypoint if possible.
                if (_tourManager != null && _tourManager.ActiveTour != null
                    && _tourManager.IsRunning)
                {
                    int idx = _tourManager.CurrentWaypointIndex;
                    if (idx < _tourManager.ActiveTour.waypoints.Count)
                    {
                        _targetPosition = _tourManager.ActiveTour.waypoints[idx].position;
                        _hasTarget      = true;
                    }
                }
                else return;
            }

            Vector3 toTarget     = _targetPosition - playerTransform.position;
            DistanceToNextWaypoint = toTarget.magnitude;

            // Bearing: angle from north (world +Z) projected onto horizontal plane.
            Vector3 horizontal    = new Vector3(toTarget.x, 0f, toTarget.z);
            BearingToNextWaypoint = horizontal.sqrMagnitude > 0.001f
                ? Vector3.SignedAngle(Vector3.forward, horizontal.normalized, Vector3.up)
                : 0f;
            if (BearingToNextWaypoint < 0f) BearingToNextWaypoint += 360f;
        }

        private void RunAutoPilot()
        {
            if (!_hasTarget || flightController == null || playerTransform == null) return;

            // Compute desired direction toward waypoint.
            Vector3 desiredDir = (_targetPosition - playerTransform.position).normalized;

            // Yaw control: project onto horizontal plane.
            Vector3 forward2D  = new Vector3(playerTransform.forward.x, 0f, playerTransform.forward.z).normalized;
            Vector3 desired2D  = new Vector3(desiredDir.x, 0f, desiredDir.z).normalized;
            float   yawError   = Vector3.SignedAngle(forward2D, desired2D, Vector3.up);
            float   yawInput   = Mathf.Clamp(yawError / 90f * autopilotTurnRate, -1f, 1f);

            // Pitch control: target a comfortable altitude above the waypoint.
            float   desiredAlt  = _targetPosition.y + autopilotAltitudeGain;
            float   altError    = desiredAlt - playerTransform.position.y;
            float   pitchInput  = Mathf.Clamp(-altError / 200f * autopilotPitchRate, -1f, 1f);

            flightController.SetThrottle(autopilotSpeed);
            flightController.Step(yawInput, pitchInput, 0f);
        }

    }
}
