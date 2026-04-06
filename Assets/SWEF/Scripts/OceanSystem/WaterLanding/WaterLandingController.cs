// WaterLandingController.cs — Phase 117: Advanced Ocean & Maritime System
// Seaplane / float plane water landing: touchdown detection, deceleration, taxi.
// Namespace: SWEF.OceanSystem

using System;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Manages a seaplane or float-plane water landing sequence.
    /// Detects water touchdown, applies deceleration, and transitions to taxi mode.
    /// </summary>
    public class WaterLandingController : MonoBehaviour
    {
        // ── Landing State ─────────────────────────────────────────────────────────

        /// <summary>Phases of a water landing sequence.</summary>
        public enum LandingPhase
        {
            /// <summary>Airborne approach.</summary>
            Approach,
            /// <summary>Touching the water surface.</summary>
            Touchdown,
            /// <summary>Decelerating on the water surface.</summary>
            Deceleration,
            /// <summary>Taxiing at low speed on water.</summary>
            Taxi,
            /// <summary>Landed and stationary.</summary>
            Stopped
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Physics")]
        [SerializeField] private Rigidbody aircraftRigidbody;
        [SerializeField] private float waterBrakingForce = 15000f;
        [SerializeField] private float taxiMaxSpeedKnots = 20f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem sprayEffect;
        [SerializeField] private AudioSource splashAudio;

        // ── Private state ─────────────────────────────────────────────────────────

        private LandingPhase _phase = LandingPhase.Approach;
        private float _waterLevel;
        private float _touchdownVerticalSpeed;
        private float _touchdownHorizontalSpeed;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when the aircraft touches the water surface.</summary>
        public event Action<float, float> OnTouchdown; // (vertSpeed, horizSpeed)

        /// <summary>Raised when the aircraft transitions to taxi phase.</summary>
        public event Action OnTaxiStarted;

        /// <summary>Raised when the aircraft comes to a stop on water.</summary>
        public event Action OnStopped;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Current phase of the water landing sequence.</summary>
        public LandingPhase Phase => _phase;

        /// <summary>Whether the aircraft is currently on water (not airborne).</summary>
        public bool IsOnWater => _phase != LandingPhase.Approach;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (aircraftRigidbody == null) aircraftRigidbody = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            UpdateWaterLevel();
            ProcessLandingPhase();
        }

        // ── Phase Logic ───────────────────────────────────────────────────────────

        private void UpdateWaterLevel()
        {
            var mgr = OceanSystemManager.Instance;
            _waterLevel = mgr != null
                ? mgr.GetSurfaceHeight(new Vector2(transform.position.x, transform.position.z))
                : 0f;
        }

        private void ProcessLandingPhase()
        {
            if (aircraftRigidbody == null) return;
            switch (_phase)
            {
                case LandingPhase.Approach:
                    CheckForTouchdown();
                    break;
                case LandingPhase.Touchdown:
                case LandingPhase.Deceleration:
                    ApplyWaterBraking();
                    CheckTransitions();
                    break;
                case LandingPhase.Taxi:
                    LimitTaxiSpeed();
                    break;
            }
        }

        private void CheckForTouchdown()
        {
            if (transform.position.y <= _waterLevel + 0.1f && aircraftRigidbody.linearVelocity.y < 0f)
            {
                _touchdownVerticalSpeed   = Mathf.Abs(aircraftRigidbody.linearVelocity.y);
                _touchdownHorizontalSpeed = new Vector2(aircraftRigidbody.linearVelocity.x, aircraftRigidbody.linearVelocity.z).magnitude;

                _phase = LandingPhase.Touchdown;
                OnTouchdown?.Invoke(_touchdownVerticalSpeed, _touchdownHorizontalSpeed);
                TriggerTouchdownEffects();

                // Record landing
                var mgr = OceanSystemManager.Instance;
                mgr?.RecordWaterLanding(new WaterLandingRecord
                {
                    timestamp                 = System.DateTime.UtcNow,
                    landingType               = WaterLandingType.Seaplane,
                    touchdownVerticalSpeed    = _touchdownVerticalSpeed,
                    touchdownHorizontalSpeed  = _touchdownHorizontalSpeed,
                    seaState                  = mgr.CurrentSeaState,
                    success                   = _touchdownVerticalSpeed <= (config != null ? config.maxSafeTouchdownSpeed : 3f)
                });
            }
        }

        private void ApplyWaterBraking()
        {
            if (transform.position.y > _waterLevel + 0.5f) return;
            var vel    = aircraftRigidbody.linearVelocity;
            var flatVel = new Vector3(vel.x, 0f, vel.z);
            if (flatVel.magnitude < 0.1f) return;
            aircraftRigidbody.AddForce(-flatVel.normalized * waterBrakingForce * Time.fixedDeltaTime, ForceMode.Force);
            if (_phase == LandingPhase.Touchdown) _phase = LandingPhase.Deceleration;
        }

        private void CheckTransitions()
        {
            float speed = new Vector2(aircraftRigidbody.linearVelocity.x, aircraftRigidbody.linearVelocity.z).magnitude;
            float taxiSpeed = taxiMaxSpeedKnots * 0.5144f; // knots → m/s

            if (speed < taxiSpeed && _phase == LandingPhase.Deceleration)
            {
                _phase = LandingPhase.Taxi;
                OnTaxiStarted?.Invoke();
            }
            else if (speed < 0.5f && _phase == LandingPhase.Taxi)
            {
                _phase = LandingPhase.Stopped;
                OnStopped?.Invoke();
            }
        }

        private void LimitTaxiSpeed()
        {
            float max = taxiMaxSpeedKnots * 0.5144f;
            var vel = aircraftRigidbody.linearVelocity;
            var flat = new Vector3(vel.x, 0f, vel.z);
            if (flat.magnitude > max)
            {
                var clamped = flat.normalized * max;
                aircraftRigidbody.linearVelocity = new Vector3(clamped.x, vel.y, clamped.z);
            }
        }

        private void TriggerTouchdownEffects()
        {
            if (sprayEffect != null) sprayEffect.Play();
            if (splashAudio != null) splashAudio.Play();
        }
    }
}
