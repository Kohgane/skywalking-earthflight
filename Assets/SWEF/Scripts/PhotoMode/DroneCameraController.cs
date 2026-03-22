using System;
using System.Collections;
using UnityEngine;
using SWEF.Flight;
using SWEF.Analytics;

namespace SWEF.PhotoMode
{
    /// <summary>
    /// Core drone camera controller that detaches from the player aircraft and flies
    /// independently.  Supports six <see cref="DroneMode"/> values and provides a
    /// battery system, collision avoidance, and a tether range limit.
    /// </summary>
    public class DroneCameraController : MonoBehaviour
    {
        #region Constants
        private const float LowBatteryThreshold  = 0.10f;  // 10 %
        private const float RangePulseInterval   = 1f;     // seconds between range-warning pulses
        private const float CollisionCheckRadius = 0.3f;   // metres
        private const float ReturnSpeedMultiplier = 2f;
        #endregion

        #region Inspector
        [Header("References (auto-found if null)")]
        [Tooltip("FlightController reference. Auto-found if null.")]
        [SerializeField] private FlightController flightController;

        [Tooltip("UserBehaviorTracker reference. Auto-found if null.")]
        [SerializeField] private UserBehaviorTracker behaviorTracker;

        [Header("Drone Configuration")]
        [Tooltip("Configurable drone parameters.")]
        [SerializeField] private DroneConfig config = new DroneConfig();

        [Header("Orbit / Follow Target")]
        [Tooltip("Transform to orbit or follow. Defaults to player aircraft if null.")]
        [SerializeField] private Transform orbitTarget;
        #endregion

        #region Events
        /// <summary>Fired after the drone is successfully deployed.</summary>
        public event Action OnDeployed;

        /// <summary>Fired after the drone returns to the player aircraft.</summary>
        public event Action OnRecalled;

        /// <summary>Fired once when battery drops to or below 10 %.</summary>
        public event Action OnBatteryLow;

        /// <summary>Fired once when the drone reaches its maximum tether range.</summary>
        public event Action OnMaxRangeReached;

        /// <summary>Fired whenever the active <see cref="DroneMode"/> changes.</summary>
        public event Action<DroneMode> OnModeChanged;
        #endregion

        #region Public properties
        /// <summary>Whether the drone is currently deployed.</summary>
        public bool IsDeployed { get; private set; }

        /// <summary>Battery charge remaining, normalised 0–1.</summary>
        public float BatteryNormalized => _battery / config.batteryDuration;

        /// <summary>Current drone mode.</summary>
        public DroneMode CurrentMode { get; private set; } = DroneMode.Free;

        /// <summary>Read-only reference to the active drone configuration.</summary>
        public DroneConfig Config => config;
        #endregion

        #region Private state
        private float     _battery;
        private bool      _batteryLowFired;
        private bool      _maxRangeFired;
        private bool      _isReturning;
        private float     _orbitAngle;
        private Vector3   _targetVelocity;
        private Vector3   _smoothedVelocity;
        private Transform _playerTransform;
        private Coroutine _dollyCoroutine;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (flightController == null)
                flightController = FindObjectOfType<FlightController>();
            if (behaviorTracker == null)
                behaviorTracker = FindObjectOfType<UserBehaviorTracker>();

            _playerTransform = flightController != null ? flightController.transform : null;
            _battery = config.batteryDuration;
        }

        private void OnEnable()  { }
        private void OnDisable() { }

        private void Update()
        {
            if (!IsDeployed) return;

            ConsumeBattery();
            EnforceTetherRange();

            if (_isReturning)
            {
                MoveTowardsPlayer();
                return;
            }

            switch (CurrentMode)
            {
                case DroneMode.Free:        break; // input handled externally
                case DroneMode.Orbit:       UpdateOrbit(); break;
                case DroneMode.FollowTarget: UpdateFollow(); break;
                case DroneMode.Static:      break; // position locked; rotation handled externally
                case DroneMode.Selfie:      UpdateSelfie(); break;
                case DroneMode.Dolly:       break; // driven by coroutine
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Deploys the drone at the current camera position and begins battery drain.
        /// </summary>
        public void Deploy()
        {
            if (IsDeployed) return;

            IsDeployed      = true;
            _isReturning    = false;
            _batteryLowFired = false;
            _maxRangeFired  = false;
            _battery        = config.batteryDuration;

            behaviorTracker?.TrackFeatureDiscovery("drone_deployed");
            OnDeployed?.Invoke();
        }

        /// <summary>
        /// Recalls the drone back to the player aircraft and disables it on arrival.
        /// </summary>
        public void Recall()
        {
            if (!IsDeployed) return;
            _isReturning = true;
            behaviorTracker?.TrackFeatureDiscovery("drone_recalled");
        }

        /// <summary>
        /// Changes the active drone mode.
        /// </summary>
        /// <param name="mode">Target <see cref="DroneMode"/>.</param>
        public void SetMode(DroneMode mode)
        {
            if (CurrentMode == mode) return;

            CurrentMode = mode;
            behaviorTracker?.TrackFeatureDiscovery($"drone_mode_{mode.ToString().ToLowerInvariant()}");
            OnModeChanged?.Invoke(mode);
        }

        /// <summary>
        /// Instantly teleports the drone to <paramref name="worldPosition"/>.
        /// </summary>
        /// <param name="worldPosition">Target world-space position.</param>
        public void TeleportTo(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        /// <summary>
        /// Rotates the drone so its forward axis points toward <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Transform to look at.</param>
        public void LookAt(Transform target)
        {
            if (target == null) return;
            transform.LookAt(target);
        }

        /// <summary>
        /// Applies a translational velocity impulse (world-space) to move the drone.
        /// Call from an external input handler each frame.
        /// </summary>
        /// <param name="worldVelocity">World-space velocity vector in m/s.</param>
        public void ApplyMovement(Vector3 worldVelocity)
        {
            if (!IsDeployed || _isReturning) return;
            _targetVelocity = worldVelocity;
            ApplySmoothMovement();
        }
        #endregion

        #region Private helpers
        private void ConsumeBattery()
        {
            if (_battery <= 0f) return;

            _battery -= Time.deltaTime;
            _battery  = Mathf.Max(0f, _battery);

            if (!_batteryLowFired && BatteryNormalized <= LowBatteryThreshold)
            {
                _batteryLowFired = true;
                OnBatteryLow?.Invoke();
                if (config.autoReturnOnLowBattery)
                    Recall();
            }

            if (_battery <= 0f && !_isReturning)
                Recall();
        }

        private void EnforceTetherRange()
        {
            if (_playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            if (dist >= config.maxRange)
            {
                if (!_maxRangeFired)
                {
                    _maxRangeFired = true;
                    OnMaxRangeReached?.Invoke();
                }
                // Push drone back inside tether
                Vector3 dir = (_playerTransform.position - transform.position).normalized;
                transform.position += dir * (dist - config.maxRange + 0.1f);
            }
            else
            {
                _maxRangeFired = false;
            }
        }

        private void UpdateOrbit()
        {
            if (_playerTransform == null) return;
            Transform target = orbitTarget != null ? orbitTarget : _playerTransform;

            _orbitAngle += config.orbitSpeed * Time.deltaTime;
            float rad = _orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * config.orbitRadius;
            transform.position = Vector3.Lerp(transform.position, target.position + offset,
                (1f - config.smoothing) * Time.deltaTime * 10f);
            transform.LookAt(target);
        }

        private void UpdateFollow()
        {
            if (_playerTransform == null) return;

            Vector3 targetPos = _playerTransform.position
                                - _playerTransform.forward * config.followDistance
                                + Vector3.up * config.followHeight;
            transform.position = Vector3.Lerp(transform.position, targetPos,
                (1f - config.smoothing) * Time.deltaTime * 10f);
            transform.LookAt(_playerTransform);
        }

        private void UpdateSelfie()
        {
            if (_playerTransform == null) return;

            Vector3 targetPos = _playerTransform.position
                                + _playerTransform.forward * (config.followDistance * 0.5f)
                                + Vector3.up * (config.followHeight * 0.5f);
            transform.position = Vector3.Lerp(transform.position, targetPos,
                (1f - config.smoothing) * Time.deltaTime * 10f);
            transform.LookAt(_playerTransform);
        }

        private void ApplySmoothMovement()
        {
            float smooth = Mathf.Clamp01(config.smoothing);
            _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, _targetVelocity, 1f - smooth);

            Vector3 desired = transform.position + _smoothedVelocity * Time.deltaTime;

            if (config.collisionAvoidance)
            {
                float speed = _smoothedVelocity.magnitude;
                if (speed > 0.001f &&
                    Physics.SphereCast(transform.position, CollisionCheckRadius,
                    _smoothedVelocity / speed, out RaycastHit hit,
                    speed * Time.deltaTime + CollisionCheckRadius))
                {
                    desired = transform.position + Vector3.ProjectOnPlane(_smoothedVelocity, hit.normal) * Time.deltaTime;
                }
            }

            transform.position = desired;
        }

        private void MoveTowardsPlayer()
        {
            if (_playerTransform == null)
            {
                CompleteRecall();
                return;
            }

            Vector3 dir  = (_playerTransform.position - transform.position).normalized;
            float   dist = Vector3.Distance(transform.position, _playerTransform.position);
            transform.position += dir * config.moveSpeed * ReturnSpeedMultiplier * Time.deltaTime;

            if (dist < 1f)
                CompleteRecall();
        }

        private void CompleteRecall()
        {
            IsDeployed   = false;
            _isReturning = false;
            OnRecalled?.Invoke();
        }
        #endregion
    }
}
