// VRCockpitInteraction.cs — Phase 112: VR/XR Flight Experience
// Grab, push, pull, twist interactions for physical cockpit controls.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Handles physical VR interaction with a single cockpit control element.
    /// Supports grab, push, pull, and twist interaction types.
    /// Attach to any interactive cockpit object (lever, button, knob, etc.).
    /// </summary>
    public class VRCockpitInteraction : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Interaction Config")]
        [SerializeField] private CockpitInteractionType interactionType = CockpitInteractionType.Grab;
        [SerializeField] private string controlId = "unnamed_control";

        [Header("Grab")]
        [SerializeField] private float grabRadius = 0.08f;

        [Header("Push/Pull (linear)")]
        [SerializeField] private float linearMinMetres = -0.1f;
        [SerializeField] private float linearMaxMetres =  0.1f;

        [Header("Twist (rotary)")]
        [SerializeField] private float twistMinDeg = -150f;
        [SerializeField] private float twistMaxDeg =  150f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether this control is currently being held by the player.</summary>
        public bool IsGrabbed { get; private set; }

        /// <summary>Current normalised value [0..1] of this control.</summary>
        public float NormalizedValue { get; private set; }

        /// <summary>Interaction type of this control.</summary>
        public CockpitInteractionType InteractionType => interactionType;

        /// <summary>Logical identifier of this control.</summary>
        public string ControlId => controlId;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the control is grabbed by the player.</summary>
        public event Action<VRCockpitInteraction> OnGrabbed;

        /// <summary>Fired when the control is released.</summary>
        public event Action<VRCockpitInteraction> OnReleased;

        /// <summary>Fired when the control value changes. Args: controlId, normalizedValue.</summary>
        public event Action<string, float> OnValueChanged;

        // ── Private state ─────────────────────────────────────────────────────────
        private Vector3    _grabStartWorldPos;
        private float      _valueAtGrabStart;
        private Quaternion _rotationAtGrabStart;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns true when the given world position is within grab radius.</summary>
        public bool IsWithinGrabRange(Vector3 worldPos) =>
            Vector3.Distance(transform.position, worldPos) <= grabRadius;

        /// <summary>Begin grab interaction from the given world-space hand position.</summary>
        public void BeginGrab(Vector3 handWorldPos)
        {
            if (IsGrabbed) return;
            IsGrabbed          = true;
            _grabStartWorldPos = handWorldPos;
            _valueAtGrabStart  = NormalizedValue;
            OnGrabbed?.Invoke(this);
        }

        /// <summary>Update the control based on current hand world position during grab.</summary>
        public void UpdateGrab(Vector3 handWorldPos)
        {
            if (!IsGrabbed) return;

            float newValue = NormalizedValue;

            switch (interactionType)
            {
                case CockpitInteractionType.Grab:
                case CockpitInteractionType.Pull:
                case CockpitInteractionType.Push:
                {
                    Vector3 delta   = handWorldPos - _grabStartWorldPos;
                    float   localDY = transform.InverseTransformDirection(delta).y;
                    float   range   = linearMaxMetres - linearMinMetres;
                    float   deltaV  = range > 0f ? localDY / range : 0f;
                    newValue = Mathf.Clamp01(_valueAtGrabStart + deltaV);
                    break;
                }
                case CockpitInteractionType.Twist:
                {
                    Vector3 fromDir = (_grabStartWorldPos - transform.position).normalized;
                    Vector3 toDir   = (handWorldPos        - transform.position).normalized;
                    float   angle   = Vector3.SignedAngle(fromDir, toDir, transform.forward);
                    float   range   = twistMaxDeg - twistMinDeg;
                    float   deltaV  = range > 0f ? angle / range : 0f;
                    newValue = Mathf.Clamp01(_valueAtGrabStart + deltaV);
                    break;
                }
            }

            SetNormalizedValue(newValue);
        }

        /// <summary>End the current grab interaction.</summary>
        public void EndGrab()
        {
            if (!IsGrabbed) return;
            IsGrabbed = false;
            OnReleased?.Invoke(this);
        }

        /// <summary>Force-set a normalised control value [0..1].</summary>
        public void SetNormalizedValue(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(NormalizedValue, clamped)) return;
            NormalizedValue = clamped;
            OnValueChanged?.Invoke(controlId, clamped);
        }
    }
}
