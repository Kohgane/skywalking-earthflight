// DockingScenarioController.cs — Phase 114: Satellite & Space Debris Tracking
// ISS docking scenario: approach corridor, docking port alignment, velocity matching, capture sequence.
// Namespace: SWEF.SatelliteTracking

using System;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Manages the full ISS docking scenario state machine: far approach → near approach →
    /// final approach → contact → capture → hard-dock → undocking.
    /// </summary>
    public class DockingScenarioController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static DockingScenarioController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Docking Corridor")]
        [SerializeField] private DockingCorridor targetCorridor;

        [Header("Approach Vehicle")]
        [Tooltip("Transform of the approaching vehicle (player spacecraft).")]
        [SerializeField] private Transform vehicleTransform;

        [Header("Thresholds")]
        [Tooltip("Range (m) at which Near Approach phase begins.")]
        [SerializeField] private float nearApproachRangeM = 1000f;

        [Tooltip("Range (m) at which Final Approach phase begins.")]
        [SerializeField] private float finalApproachRangeM = 100f;

        [Tooltip("Range (m) within which Contact can occur.")]
        [SerializeField] private float contactRangeM = 0.3f;

        [Tooltip("Maximum lateral offset allowed at capture (m).")]
        [SerializeField] private float captureOffsetM = 0.05f;

        [Tooltip("Maximum relative velocity at contact (m/s).")]
        [SerializeField] private float maxContactVelocityMs = 0.2f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the docking state transitions.</summary>
        public event Action<DockingState> OnStateChanged;

        /// <summary>Raised when hard-dock is achieved.</summary>
        public event Action OnHardDock;

        /// <summary>Raised when undocking is complete.</summary>
        public event Action OnUndockingComplete;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current docking phase.</summary>
        public DockingState CurrentState { get; private set; } = DockingState.Idle;

        /// <summary>Distance to docking port in metres.</summary>
        public float RangeToPortM { get; private set; }

        /// <summary>Lateral offset from approach axis (m).</summary>
        public float LateralOffsetM { get; private set; }

        /// <summary>Relative closing velocity (m/s, positive = closing).</summary>
        public float ClosingVelocityMs { get; private set; }

        /// <summary>Alignment angle between vehicle axis and approach axis (degrees).</summary>
        public float AlignmentAngleDeg { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────
        private Vector3 _prevVehiclePos;
        private bool _scenarioStarted;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (!_scenarioStarted || vehicleTransform == null || targetCorridor == null) return;
            UpdateMeasurements();
            EvaluateStateTransitions();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Starts the docking scenario from Far Approach.</summary>
        public void StartScenario(DockingCorridor corridor, Transform vehicle)
        {
            targetCorridor   = corridor;
            vehicleTransform = vehicle;
            _prevVehiclePos  = vehicle.position;
            _scenarioStarted = true;
            TransitionTo(DockingState.FarApproach);
        }

        /// <summary>Aborts the current docking scenario and returns to Idle.</summary>
        public void AbortScenario()
        {
            _scenarioStarted = false;
            TransitionTo(DockingState.Idle);
        }

        /// <summary>Initiates the undocking sequence.</summary>
        public void BeginUndocking()
        {
            if (CurrentState != DockingState.HardDock) return;
            TransitionTo(DockingState.Undocking);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void UpdateMeasurements()
        {
            var portWorldPos = transform.TransformPoint(targetCorridor.portPositionLocal);
            var toPort = portWorldPos - vehicleTransform.position;
            RangeToPortM = toPort.magnitude;

            // Lateral offset from approach axis
            var axisWorld = transform.TransformDirection(targetCorridor.approachAxisLocal);
            var axisComponent = Vector3.Dot(toPort, axisWorld) * axisWorld;
            LateralOffsetM = (toPort - axisComponent).magnitude;

            // Closing velocity
            var vel = (vehicleTransform.position - _prevVehiclePos) / Time.deltaTime;
            _prevVehiclePos = vehicleTransform.position;
            ClosingVelocityMs = -Vector3.Dot(vel, toPort.normalized) * 1000f; // Unity units → m/s

            // Alignment angle
            AlignmentAngleDeg = Vector3.Angle(-vehicleTransform.forward, axisWorld);
        }

        private void EvaluateStateTransitions()
        {
            switch (CurrentState)
            {
                case DockingState.FarApproach when RangeToPortM <= nearApproachRangeM:
                    TransitionTo(DockingState.NearApproach);
                    break;

                case DockingState.NearApproach when RangeToPortM <= finalApproachRangeM:
                    TransitionTo(DockingState.FinalApproach);
                    break;

                case DockingState.FinalApproach when RangeToPortM <= contactRangeM:
                    TransitionTo(DockingState.Contact);
                    break;

                case DockingState.Contact
                    when LateralOffsetM <= captureOffsetM &&
                         ClosingVelocityMs <= maxContactVelocityMs:
                    TransitionTo(DockingState.Capture);
                    break;

                case DockingState.Capture:
                    TransitionTo(DockingState.HardDock);
                    OnHardDock?.Invoke();
                    break;

                case DockingState.Undocking when ClosingVelocityMs < -0.5f:
                    TransitionTo(DockingState.Idle);
                    OnUndockingComplete?.Invoke();
                    _scenarioStarted = false;
                    break;
            }
        }

        private void TransitionTo(DockingState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
            Debug.Log($"[DockingScenario] State → {newState}");
        }
    }
}
