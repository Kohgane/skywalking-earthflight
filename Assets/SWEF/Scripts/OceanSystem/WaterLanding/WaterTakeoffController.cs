// WaterTakeoffController.cs — Phase 117: Advanced Ocean & Maritime System
// Water takeoff: step planing, rotation speed on water, spray effects.
// Namespace: SWEF.OceanSystem

using System;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Manages a seaplane water takeoff sequence.
    /// Tracks the step-planing run, rotation (unstick) point, and initial climb.
    /// </summary>
    public class WaterTakeoffController : MonoBehaviour
    {
        // ── Takeoff State ─────────────────────────────────────────────────────────

        /// <summary>Phases of a water takeoff run.</summary>
        public enum TakeoffPhase
        {
            /// <summary>Idle on water.</summary>
            Idle,
            /// <summary>Accelerating on water surface.</summary>
            Run,
            /// <summary>Hull is on the step — reducing drag.</summary>
            OnStep,
            /// <summary>Aircraft has rotated and left the water.</summary>
            Airborne
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Physics")]
        [SerializeField] private Rigidbody aircraftRigidbody;

        [Header("Speeds (knots)")]
        [SerializeField] private float stepSpeedKnots     = 25f;
        [SerializeField] private float rotationSpeedKnots = 45f;

        [Header("Forces")]
        [SerializeField] private float stepHullLiftForce = 4000f;
        [SerializeField] private float rotationPitchTorque = 3000f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem bowSprayEffect;
        [SerializeField] private ParticleSystem stepSprayEffect;

        // ── Private state ─────────────────────────────────────────────────────────

        private TakeoffPhase _phase = TakeoffPhase.Idle;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when the takeoff phase changes.</summary>
        public event Action<TakeoffPhase> OnPhaseChanged;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Current takeoff phase.</summary>
        public TakeoffPhase Phase => _phase;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (aircraftRigidbody == null) aircraftRigidbody = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (_phase == TakeoffPhase.Airborne) return;
            UpdateTakeoffPhase();
        }

        // ── Phase Logic ───────────────────────────────────────────────────────────

        private void UpdateTakeoffPhase()
        {
            if (aircraftRigidbody == null) return;
            float speedKnots = aircraftRigidbody.linearVelocity.magnitude / 0.5144f;

            switch (_phase)
            {
                case TakeoffPhase.Idle:
                    if (speedKnots > 1f) SetPhase(TakeoffPhase.Run);
                    break;

                case TakeoffPhase.Run:
                    UpdateBowSpray(speedKnots);
                    if (speedKnots >= stepSpeedKnots) SetPhase(TakeoffPhase.OnStep);
                    break;

                case TakeoffPhase.OnStep:
                    ApplyStepLift();
                    UpdateStepSpray(speedKnots);
                    if (speedKnots >= rotationSpeedKnots) Rotate();
                    break;
            }
        }

        private void ApplyStepLift()
        {
            if (aircraftRigidbody == null) return;
            aircraftRigidbody.AddRelativeForce(Vector3.up * stepHullLiftForce, ForceMode.Force);
        }

        private void Rotate()
        {
            if (aircraftRigidbody == null) return;
            aircraftRigidbody.AddRelativeTorque(new Vector3(-rotationPitchTorque, 0f, 0f), ForceMode.Force);

            // Check if we've left the water
            var mgr = OceanSystemManager.Instance;
            float waterY = mgr != null
                ? mgr.GetSurfaceHeight(new Vector2(transform.position.x, transform.position.z))
                : 0f;

            if (transform.position.y > waterY + 0.5f) SetPhase(TakeoffPhase.Airborne);
        }

        private void UpdateBowSpray(float speedKnots)
        {
            if (bowSprayEffect == null) return;
            float t = Mathf.Clamp01(speedKnots / stepSpeedKnots);
            var emission = bowSprayEffect.emission;
            emission.rateOverTime = t * 80f;
            if (!bowSprayEffect.isPlaying) bowSprayEffect.Play();
        }

        private void UpdateStepSpray(float speedKnots)
        {
            if (stepSprayEffect == null) return;
            if (!stepSprayEffect.isPlaying) stepSprayEffect.Play();
            if (bowSprayEffect != null) bowSprayEffect.Stop();
        }

        private void SetPhase(TakeoffPhase newPhase)
        {
            _phase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Resets the takeoff controller to idle state (back on water).</summary>
        public void Reset()
        {
            SetPhase(TakeoffPhase.Idle);
        }
    }
}
