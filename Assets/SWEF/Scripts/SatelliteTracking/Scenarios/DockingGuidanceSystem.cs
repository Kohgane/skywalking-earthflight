// DockingGuidanceSystem.cs — Phase 114: Satellite & Space Debris Tracking
// Docking HUD: approach vector, relative velocity, distance, alignment indicators, go/no-go status.
// Namespace: SWEF.SatelliteTracking

using System;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Computes go/no-go guidance data for the docking HUD based on real-time
    /// measurements from the <see cref="DockingScenarioController"/>.
    /// </summary>
    public class DockingGuidanceSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Go / No-Go Limits")]
        [Tooltip("Maximum allowed closing velocity at given range (AnimationCurve).")]
        [SerializeField] private AnimationCurve maxVelocityVsRange =
            AnimationCurve.Linear(0f, 0.03f, 1000f, 0.5f);

        [Tooltip("Maximum allowed lateral offset at given range (m).")]
        [SerializeField] private float maxLateralOffsetFraction = 0.1f;

        [Tooltip("Maximum allowed alignment error for final approach (degrees).")]
        [SerializeField] private float maxAlignmentErrorDeg = 5f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when go/no-go status changes.</summary>
        public event Action<bool> OnGoNoGoChanged;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether all parameters are within go limits.</summary>
        public bool IsGo { get; private set; }

        /// <summary>Human-readable reason for no-go status (empty if go).</summary>
        public string NoGoReason { get; private set; } = string.Empty;

        /// <summary>Approach corridor compliance 0–1 (1 = perfectly centred).</summary>
        public float CorridorCompliance { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────
        private DockingScenarioController _scenario;
        private bool _prevIsGo;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _scenario = FindObjectOfType<DockingScenarioController>();
        }

        private void Update()
        {
            if (_scenario == null) return;
            EvaluateGoNoGo();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void EvaluateGoNoGo()
        {
            float range   = _scenario.RangeToPortM;
            float velMs   = _scenario.ClosingVelocityMs;
            float offset  = _scenario.LateralOffsetM;
            float align   = _scenario.AlignmentAngleDeg;

            bool go = true;
            NoGoReason = string.Empty;

            // Velocity check
            float maxVel = maxVelocityVsRange.Evaluate(range);
            if (velMs > maxVel)
            {
                go = false;
                NoGoReason = $"Closing velocity {velMs:F2} m/s exceeds {maxVel:F2} m/s limit";
            }

            // Lateral offset check
            float maxOffset = range * maxLateralOffsetFraction;
            if (offset > maxOffset)
            {
                go = false;
                if (string.IsNullOrEmpty(NoGoReason))
                    NoGoReason = $"Lateral offset {offset:F2} m exceeds {maxOffset:F2} m limit";
            }

            // Alignment check (final approach only)
            if (_scenario.CurrentState == DockingState.FinalApproach &&
                align > maxAlignmentErrorDeg)
            {
                go = false;
                if (string.IsNullOrEmpty(NoGoReason))
                    NoGoReason = $"Misalignment {align:F1}° exceeds {maxAlignmentErrorDeg}° limit";
            }

            // Corridor compliance
            CorridorCompliance = go ? 1f : Mathf.Clamp01(1f - offset / Mathf.Max(1f, maxOffset));

            IsGo = go;
            if (IsGo != _prevIsGo)
            {
                _prevIsGo = IsGo;
                OnGoNoGoChanged?.Invoke(IsGo);
            }
        }
    }
}
