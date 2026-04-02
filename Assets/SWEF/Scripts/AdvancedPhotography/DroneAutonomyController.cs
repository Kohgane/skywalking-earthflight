// DroneAutonomyController.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_PHOTOMODE_AVAILABLE
using SWEF.PhotoMode;
#endif

namespace SWEF.AdvancedPhotography
{
    /// <summary>
    /// Phase 89 — Singleton MonoBehaviour that manages autonomous drone camera flight.
    /// Supports Orbit, Flyby, Follow, Waypoint, Tracking, Cinematic, FreeRoam, and ReturnHome modes.
    /// Includes a battery system with auto-return and collision avoidance via raycasts.
    ///
    /// <para>Attach to a persistent scene object — uses DontDestroyOnLoad.</para>
    /// </summary>
    public sealed class DroneAutonomyController : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static DroneAutonomyController Instance { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired when the active flight mode changes.</summary>
        public event Action<DroneFlightMode> OnFlightModeChanged;

        /// <summary>Fired when battery drops below <see cref="AdvancedPhotographyConfig.DroneLowBatteryThreshold"/>.</summary>
        public event Action<float> OnBatteryLow;

        /// <summary>Fired when battery reaches zero.</summary>
        public event Action OnBatteryDepleted;

        /// <summary>Fired when the drone reaches a waypoint index.</summary>
        public event Action<int> OnWaypointReached;

        /// <summary>Fired when collision avoidance changes the drone's trajectory.</summary>
        public event Action OnCollisionAvoided;

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Transform used as the drone camera pivot.")]
        [SerializeField] private Transform _droneTransform;

        [Tooltip("Player / subject transform for Follow and Tracking modes.")]
        [SerializeField] private Transform _playerTransform;

        [Header("Battery")]
        [Tooltip("Battery drain rate multiplier (1 = normal).")]
        [SerializeField] [Min(0.01f)] private float _batteryDrainMultiplier = 1f;

        [Header("Orbit Mode")]
        [Tooltip("Current orbit radius in metres.")]
        [SerializeField] [Min(1f)] private float _orbitRadius = AdvancedPhotographyConfig.DroneOrbitDefaultRadius;

        [Tooltip("Orbit angular speed (degrees/second).")]
        [SerializeField] private float _orbitSpeed = AdvancedPhotographyConfig.DroneOrbitDefaultSpeed;

        [Header("Follow Mode")]
        [Tooltip("Offset from the player transform in Follow mode.")]
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 10f, -30f);

        [Header("Collision Avoidance")]
        [Tooltip("Layer mask for collision avoidance raycasts.")]
        [SerializeField] private LayerMask _collisionMask = Physics.DefaultRaycastLayers;

        #endregion

        #region Private State

        private DroneFlightMode _currentMode = DroneFlightMode.FreeRoam;
        private float _batteryRemaining;
        private bool _lowBatteryFired = false;

        // Orbit
        private Vector3 _orbitCenter;
        private float _orbitAngle = 0f;

        // Waypoint
        private DroneFlightPath _activePath;
        private int _currentWaypointIndex = 0;
        private Coroutine _waypointCoroutine;

        // Tracking
        private Transform _trackingSubject;

        // Cinematic
        private Vector3 _cinematicVelocity;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _batteryRemaining = AdvancedPhotographyConfig.DroneBatteryDuration;
        }

        private void Update()
        {
            DrainBattery();
            AvoidCollisions();
            ExecuteFlightMode();
        }

        #endregion

        #region Public API

        /// <summary>Sets the active autonomous flight mode.</summary>
        public void SetFlightMode(DroneFlightMode mode)
        {
            if (_currentMode == mode) return;

            _currentMode = mode;
            OnFlightModeChanged?.Invoke(mode);
            Debug.Log($"[SWEF] DroneAutonomyController: mode → {mode}");

            if (_waypointCoroutine != null)
            {
                StopCoroutine(_waypointCoroutine);
                _waypointCoroutine = null;
            }

            if (mode == DroneFlightMode.Waypoint && _activePath != null)
                _waypointCoroutine = StartCoroutine(WaypointFlightCoroutine());

            AdvancedPhotographyAnalytics.RecordDroneFlightStarted(mode);
        }

        /// <summary>Configures Orbit mode with a centre point and radius.</summary>
        public void SetOrbitTarget(Vector3 centre, float radius)
        {
            _orbitCenter = centre;
            _orbitRadius = Mathf.Max(1f, radius);
            _orbitAngle  = 0f;
        }

        /// <summary>Loads a <see cref="DroneFlightPath"/> and, if in Waypoint mode, starts flying it.</summary>
        public void StartWaypointPath(DroneFlightPath path)
        {
            _activePath            = path ?? throw new ArgumentNullException(nameof(path));
            _currentWaypointIndex  = 0;

            if (_currentMode == DroneFlightMode.Waypoint)
            {
                if (_waypointCoroutine != null) StopCoroutine(_waypointCoroutine);
                _waypointCoroutine = StartCoroutine(WaypointFlightCoroutine());
            }
        }

        /// <summary>Commands the drone to return to the player's position.</summary>
        public void ReturnToPlayer()
        {
            SetFlightMode(DroneFlightMode.ReturnHome);
        }

        /// <summary>Returns the current battery level as a fraction in [0, 1].</summary>
        public float GetBatteryPercent()
        {
            return _batteryRemaining / AdvancedPhotographyConfig.DroneBatteryDuration;
        }

        /// <summary>Sets the subject transform for Tracking mode.</summary>
        public void SetTrackingSubject(Transform subject)
        {
            _trackingSubject = subject;
        }

        #endregion

        #region Private — Battery

        private void DrainBattery()
        {
            if (_batteryRemaining <= 0f) return;

            _batteryRemaining -= Time.deltaTime * _batteryDrainMultiplier;
            _batteryRemaining  = Mathf.Max(0f, _batteryRemaining);

            float pct = GetBatteryPercent();

            if (!_lowBatteryFired && pct <= AdvancedPhotographyConfig.DroneLowBatteryThreshold)
            {
                _lowBatteryFired = true;
                OnBatteryLow?.Invoke(pct);
                Debug.Log($"[SWEF] DroneAutonomyController: battery low ({pct * 100f:0}%)");
                ReturnToPlayer();
            }

            if (_batteryRemaining <= 0f)
            {
                OnBatteryDepleted?.Invoke();
                AdvancedPhotographyAnalytics.RecordDroneBatteryDepleted();
                Debug.Log("[SWEF] DroneAutonomyController: battery depleted");
            }
        }

        #endregion

        #region Private — Collision Avoidance

        private void AvoidCollisions()
        {
            if (_droneTransform == null) return;

            Vector3 forward = _droneTransform.forward;
            float   dist    = AdvancedPhotographyConfig.DroneCollisionLookahead;

            if (Physics.Raycast(_droneTransform.position, forward, dist, _collisionMask))
            {
                _droneTransform.Translate(Vector3.up * (Time.deltaTime * 3f), Space.World);
                OnCollisionAvoided?.Invoke();
            }
        }

        #endregion

        #region Private — Flight Execution

        private void ExecuteFlightMode()
        {
            if (_droneTransform == null) return;

            switch (_currentMode)
            {
                case DroneFlightMode.Orbit:      UpdateOrbit();       break;
                case DroneFlightMode.Follow:      UpdateFollow();      break;
                case DroneFlightMode.Tracking:    UpdateTracking();    break;
                case DroneFlightMode.Cinematic:   UpdateCinematic();   break;
                case DroneFlightMode.ReturnHome:  UpdateReturnHome();  break;
                case DroneFlightMode.FreeRoam:
                case DroneFlightMode.Waypoint:
                case DroneFlightMode.Flyby:
                default:
                    break;
            }
        }

        private void UpdateOrbit()
        {
            _orbitAngle += _orbitSpeed * Time.deltaTime;
            float rad = _orbitAngle * Mathf.Deg2Rad;

            Vector3 target = _orbitCenter + new Vector3(
                Mathf.Cos(rad) * _orbitRadius,
                _droneTransform.position.y - _orbitCenter.y,
                Mathf.Sin(rad) * _orbitRadius);

            _droneTransform.position = Vector3.MoveTowards(
                _droneTransform.position, target, 30f * Time.deltaTime);

            _droneTransform.LookAt(_orbitCenter);
        }

        private void UpdateFollow()
        {
            if (_playerTransform == null) return;

            Vector3 desired = _playerTransform.position + _playerTransform.TransformDirection(_followOffset);
            _droneTransform.position = Vector3.SmoothDamp(
                _droneTransform.position, desired, ref _cinematicVelocity, 0.5f);

            _droneTransform.LookAt(_playerTransform.position);
        }

        private void UpdateTracking()
        {
            if (_trackingSubject == null) return;
            _droneTransform.LookAt(_trackingSubject.position);
        }

        private void UpdateCinematic()
        {
            // Slow upward dolly to emulate a cinematic crane move
            _droneTransform.Translate(Vector3.up * (0.2f * Time.deltaTime), Space.World);
        }

        private void UpdateReturnHome()
        {
            if (_playerTransform == null) return;

            Vector3 home = _playerTransform.position + Vector3.up * 5f;
            _droneTransform.position = Vector3.MoveTowards(
                _droneTransform.position, home, 20f * Time.deltaTime);

            if (Vector3.Distance(_droneTransform.position, home) < 1f)
                SetFlightMode(DroneFlightMode.FreeRoam);
        }

        private IEnumerator WaypointFlightCoroutine()
        {
            if (_activePath == null || _activePath.waypoints == null || _activePath.waypoints.Count == 0)
                yield break;

            List<DroneWaypoint> wps = _activePath.waypoints;
            int idx = 0;

            while (true)
            {
                DroneWaypoint wp = wps[idx];

                // Travel to waypoint
                while (Vector3.Distance(_droneTransform.position, wp.position) > 0.5f)
                {
                    _droneTransform.position = Vector3.MoveTowards(
                        _droneTransform.position, wp.position, wp.speed * Time.deltaTime);

                    if (wp.lookAtTarget && _playerTransform != null)
                        _droneTransform.LookAt(_playerTransform.position);
                    else
                        _droneTransform.rotation = Quaternion.Slerp(
                            _droneTransform.rotation, wp.rotation, 2f * Time.deltaTime);

                    yield return null;
                }

                OnWaypointReached?.Invoke(idx);

                if (wp.holdTime > 0f)
                    yield return new WaitForSeconds(wp.holdTime);

                idx++;

                if (idx >= wps.Count)
                {
                    if (_activePath.loop)
                        idx = 0;
                    else
                    {
                        SetFlightMode(DroneFlightMode.FreeRoam);
                        yield break;
                    }
                }
            }
        }

        #endregion
    }
}
