// ArrestorWireSystem.cs — Phase 117: Advanced Ocean & Maritime System
// Arrestor wire landing: wire engagement, deceleration, bolter detection, LSO.
// Namespace: SWEF.OceanSystem

#if SWEF_CARRIER_AVAILABLE || !UNITY_EDITOR
using System;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Simulates carrier arrestor wire engagement.
    /// Detects which wire (1–4) is engaged, applies deceleration, detects bolters,
    /// and records trap data via <see cref="OceanSystemManager"/>.
    /// </summary>
    public class ArrestorWireSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Wire Positions")]
        [Tooltip("Transform positions for wires 1 (aft) through 4 (forward).")]
        [SerializeField] private Transform[] wireTransforms;

        [Header("Detection")]
        [SerializeField] private float wireEngagementWidth = 18f;  // metres across deck
        [SerializeField] private float wireEngagementHeight = 0.5f; // hook height above deck

        // ── Private state ─────────────────────────────────────────────────────────

        private bool  _engaged;
        private int   _engagedWire;     // 1-based
        private float _decelerationForce;
        private Rigidbody _trackedRb;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when an arrestor wire is engaged.</summary>
        public event Action<int> OnWireEngaged;   // (wireNumber)

        /// <summary>Raised when the aircraft bolters (overruns all wires).</summary>
        public event Action OnBolter;

        /// <summary>Raised when the aircraft has come to a stop after a trap.</summary>
        public event Action OnTrapComplete;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Whether a wire is currently engaged.</summary>
        public bool IsEngaged => _engaged;

        /// <summary>The engaged wire number (1–4), or 0 if none.</summary>
        public int EngagedWireNumber => _engagedWire;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Called each FixedUpdate by the carrier deck to check for wire engagement
        /// from an aircraft at <paramref name="hookPosition"/> with the given Rigidbody.
        /// </summary>
        public void CheckEngagement(Vector3 hookPosition, Rigidbody aircraftRb, float approachSpeedKnots)
        {
            if (_engaged || wireTransforms == null) return;

            for (int i = 0; i < wireTransforms.Length; i++)
            {
                if (wireTransforms[i] == null) continue;
                var wire = wireTransforms[i];

                // Check hook within wire engagement zone
                float lateralDist = Mathf.Abs(Vector3.Dot(hookPosition - wire.position, wire.right));
                float verticalDiff = hookPosition.y - wire.position.y;

                if (lateralDist <= wireEngagementWidth * 0.5f &&
                    verticalDiff >= 0f && verticalDiff <= wireEngagementHeight)
                {
                    EngageWire(i + 1, aircraftRb, approachSpeedKnots);
                    return;
                }
            }

            // Check if aircraft has overrun all wires
            if (wireTransforms.Length > 0)
            {
                var lastWire = wireTransforms[wireTransforms.Length - 1];
                if (lastWire != null)
                {
                    float forwardPast = Vector3.Dot(hookPosition - lastWire.position, lastWire.forward);
                    if (forwardPast > wireEngagementHeight * 2f)
                    {
                        OnBolter?.Invoke();
                        RecordTrap(aircraftRb, approachSpeedKnots, true);
                    }
                }
            }
        }

        private void EngageWire(int wireNumber, Rigidbody rb, float approachSpeedKnots)
        {
            _engaged   = true;
            _engagedWire = wireNumber;
            _trackedRb  = rb;

            float maxDecel = config != null ? config.arrestorMaxDecelG * 9.81f : 40f;
            _decelerationForce = rb.mass * maxDecel;

            OnWireEngaged?.Invoke(wireNumber);
            RecordTrap(rb, approachSpeedKnots, false);
        }

        private void RecordTrap(Rigidbody rb, float approachSpeedKnots, bool bolter)
        {
            var mgr = OceanSystemManager.Instance;
            mgr?.RecordCarrierTrap(new CarrierTrapRecord
            {
                timestamp          = System.DateTime.UtcNow,
                wireNumber         = bolter ? 0 : _engagedWire,
                wasBolter          = bolter,
                approachSpeedKnots = approachSpeedKnots,
                glidepathState     = GlidepathState.OnGlidepath
            });
        }

        private void FixedUpdate()
        {
            if (!_engaged || _trackedRb == null) return;
            ApplyArrestorDeceleration();
        }

        private void ApplyArrestorDeceleration()
        {
            var vel = _trackedRb.linearVelocity;
            if (vel.magnitude < 0.5f)
            {
                _engaged   = false;
                _trackedRb = null;
                OnTrapComplete?.Invoke();
                return;
            }
            _trackedRb.AddForce(-vel.normalized * _decelerationForce, ForceMode.Force);
        }
    }
}
#endif
