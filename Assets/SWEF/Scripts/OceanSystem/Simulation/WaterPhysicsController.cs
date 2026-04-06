// WaterPhysicsController.cs — Phase 117: Advanced Ocean & Maritime System
// Water interaction physics: buoyancy, drag, splash, wake generation.
// Namespace: SWEF.OceanSystem

using System;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Applies buoyancy, drag and splash forces to a Rigidbody
    /// when it intersects the ocean surface. Attach alongside a Rigidbody.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class WaterPhysicsController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Buoyancy")]
        [SerializeField] private float volume = 1f;            // displaced volume (m³)
        [SerializeField] private int   buoyancyPoints = 4;     // number of sample points

        [Header("Splash")]
        [SerializeField] private ParticleSystem splashEffect;
        [SerializeField] private float splashVelocityThreshold = 2f;

        [Header("Wake")]
        [SerializeField] private TrailRenderer wakeTrail;

        // ── Private state ─────────────────────────────────────────────────────────

        private Rigidbody _rb;
        private bool      _wasInWater;
        private float     _waterLevel;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised on water entry above the velocity threshold.</summary>
        public event Action OnWaterEntry;

        /// <summary>Raised on water exit.</summary>
        public event Action OnWaterExit;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            UpdateWaterLevel();
            ApplyBuoyancy();
            ApplyWaterDrag();
            HandleSplash();
            UpdateWake();
        }

        // ── Physics ───────────────────────────────────────────────────────────────

        private void UpdateWaterLevel()
        {
            var mgr = OceanSystemManager.Instance;
            if (mgr == null) { _waterLevel = 0f; return; }

            float tideOffset = FindFirstObjectByType<TideController>()?.CurrentWaterLevel ?? 0f;
            _waterLevel = mgr.GetSurfaceHeight(new Vector2(transform.position.x, transform.position.z)) + tideOffset;
        }

        private void ApplyBuoyancy()
        {
            if (config == null) return;
            float submergedFraction = CalculateSubmergedFraction();
            if (submergedFraction <= 0f) return;

            float buoyancyForce = config.waterDensity * Mathf.Abs(Physics.gravity.y) * volume * submergedFraction;
            _rb.AddForce(Vector3.up * buoyancyForce, ForceMode.Force);
        }

        private void ApplyWaterDrag()
        {
            if (config == null) return;
            float submergedFraction = CalculateSubmergedFraction();
            if (submergedFraction <= 0f) return;

            _rb.linearDamping  = Mathf.Lerp(0f, config.waterLinearDrag,  submergedFraction);
            _rb.angularDamping = Mathf.Lerp(0f, config.waterAngularDrag, submergedFraction);
        }

        private float CalculateSubmergedFraction()
        {
            float objectHeight = GetComponent<Collider>()?.bounds.size.y ?? 1f;
            float bottomY      = transform.position.y - objectHeight * 0.5f;
            float topY         = transform.position.y + objectHeight * 0.5f;
            if (topY <= _waterLevel && bottomY < _waterLevel) return 1f;
            if (bottomY >= _waterLevel) return 0f;
            return Mathf.Clamp01((_waterLevel - bottomY) / objectHeight);
        }

        private void HandleSplash()
        {
            bool inWater = transform.position.y < _waterLevel;
            if (inWater && !_wasInWater)
            {
                _wasInWater = true;
                OnWaterEntry?.Invoke();
                if (_rb.linearVelocity.magnitude >= splashVelocityThreshold && splashEffect != null)
                {
                    splashEffect.transform.position = new Vector3(transform.position.x, _waterLevel, transform.position.z);
                    splashEffect.Play();
                }
            }
            else if (!inWater && _wasInWater)
            {
                _wasInWater = false;
                OnWaterExit?.Invoke();
            }
        }

        private void UpdateWake()
        {
            if (wakeTrail == null) return;
            bool inWater = transform.position.y < _waterLevel + 0.2f;
            wakeTrail.emitting = inWater && _rb.linearVelocity.magnitude > 0.5f;
        }
    }
}
