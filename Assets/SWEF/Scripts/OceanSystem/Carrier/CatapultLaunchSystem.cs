// CatapultLaunchSystem.cs — Phase 117: Advanced Ocean & Maritime System
// Catapult launch: holdback, steam/EMALS, acceleration profile.
// Namespace: SWEF.OceanSystem

#if SWEF_CARRIER_AVAILABLE || !UNITY_EDITOR
using System;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Simulates carrier catapult launch.
    /// Supports both steam (CATOBAR) and electromagnetic (EMALS) catapult types.
    /// Applies an acceleration force profile to the attached aircraft Rigidbody.
    /// </summary>
    public class CatapultLaunchSystem : MonoBehaviour
    {
        // ── Launch State ──────────────────────────────────────────────────────────

        /// <summary>Phases of a catapult launch cycle.</summary>
        public enum LaunchState
        {
            /// <summary>Catapult idle, ready for next aircraft.</summary>
            Idle,
            /// <summary>Aircraft attached to holdback bar, engines at full power.</summary>
            HoldbackAttached,
            /// <summary>Catapult firing — acceleration phase.</summary>
            Firing,
            /// <summary>Aircraft clear of deck end.</summary>
            Complete
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Catapult")]
        [SerializeField] private CatapultType catapultType = CatapultType.Steam;
        [SerializeField] private float strokeLengthMetres = 95f;
        [SerializeField] private Transform launchDirection;

        [Header("Audio")]
        [SerializeField] private AudioSource launchAudio;

        // ── Private state ─────────────────────────────────────────────────────────

        private LaunchState _state = LaunchState.Idle;
        private Rigidbody   _targetRigidbody;
        private float       _launchProgress;      // 0..1 over the stroke
        private float       _peakAcceleration;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when the catapult begins its firing stroke.</summary>
        public event Action OnLaunchStarted;

        /// <summary>Raised when the aircraft clears the deck end.</summary>
        public event Action OnLaunchComplete;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Current catapult state.</summary>
        public LaunchState State => _state;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Attaches an aircraft Rigidbody to the catapult holdback bar.
        /// Call this when the aircraft is positioned on the catapult track.
        /// </summary>
        public void AttachAircraft(Rigidbody aircraftRb)
        {
            if (_state != LaunchState.Idle) return;
            _targetRigidbody = aircraftRb;
            _state           = LaunchState.HoldbackAttached;
            _launchProgress  = 0f;
            _peakAcceleration = config != null ? config.catapultAcceleration : 120f;
        }

        /// <summary>Initiates the catapult firing stroke.</summary>
        public void Fire()
        {
            if (_state != LaunchState.HoldbackAttached) return;
            _state = LaunchState.Firing;
            OnLaunchStarted?.Invoke();
            if (launchAudio != null) launchAudio.Play();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void FixedUpdate()
        {
            if (_state != LaunchState.Firing || _targetRigidbody == null) return;
            ApplyLaunchForce();
        }

        private void ApplyLaunchForce()
        {
            // Bell-curve acceleration profile: peak at mid-stroke
            float t           = _launchProgress;
            float accelFactor = Mathf.Sin(t * Mathf.PI); // 0→1→0
            float accelMs2    = _peakAcceleration * accelFactor;

            var dir = launchDirection != null ? launchDirection.forward : transform.forward;
            _targetRigidbody.AddForce(dir * accelMs2 * _targetRigidbody.mass, ForceMode.Force);

            // Advance progress based on approximate stroke speed
            float speed      = _targetRigidbody.linearVelocity.magnitude;
            _launchProgress += (speed + 10f) * Time.fixedDeltaTime / strokeLengthMetres;

            if (_launchProgress >= 1f)
            {
                _state           = LaunchState.Complete;
                _targetRigidbody = null;
                OnLaunchComplete?.Invoke();
            }
        }

        /// <summary>Resets the catapult to idle after the aircraft has cleared.</summary>
        public void Reset()
        {
            _state          = LaunchState.Idle;
            _targetRigidbody = null;
            _launchProgress  = 0f;
        }
    }
}
#endif
