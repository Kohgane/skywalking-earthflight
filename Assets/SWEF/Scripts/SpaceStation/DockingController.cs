// DockingController.cs — SWEF Space Station & Orbital Docking System
using System;
using UnityEngine;

namespace SWEF.SpaceStation
{
    /// <summary>
    /// Singleton MonoBehaviour that manages the 6-phase docking approach sequence.
    /// Phase transitions are driven by distance, closing speed, and alignment angle.
    /// </summary>
    public class DockingController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        public static DockingController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [SerializeField] private SpaceStationConfig _config;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired when the active phase changes.</summary>
        public event Action<DockingApproachPhase> OnPhaseChanged;

        /// <summary>Fired when a docking sequence is successfully completed (phase reaches Docked).</summary>
        public event Action OnDockingComplete;

        /// <summary>Fired when a docking sequence is aborted by the player or a fail condition.</summary>
        public event Action<string> OnDockingAborted;

        // ── Public read-only state ─────────────────────────────────────────────────

        public DockingApproachPhase CurrentPhase  { get; private set; } = DockingApproachPhase.FreeApproach;
        public string               ActiveStationId { get; private set; }
        public string               ActivePortId    { get; private set; }
        public bool                 IsActive        { get; private set; }

        // ── Phase constants ───────────────────────────────────────────────────────

        private const float DistInitialAlignment = 1000f;
        private const float DistFinalApproach    = 200f;
        private const float DistSoftCapture      = 10f;
        private const float DistHardDock         = 1f;

        private const float SpeedLimitInitial    = 50f;
        private const float SpeedLimitFinal      = 5f;
        private const float SpeedLimitSoftCapture = 0.5f;
        private const float SpeedAbortFinal      = 10f;
        private const float SpeedAbortCollision  = 2f;

        private const float AlignToleranceInitial = 15f;
        private const float AlignToleranceFinal   = 5f;
        private const float AlignToleranceSoft    = 2f;
        private const float AlignAbortFinal       = 15f;

        private const float HardDockAutoLockTime = 2f;

        // ── Private state ─────────────────────────────────────────────────────────

        private float _hardDockTimer;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Starts a new docking approach sequence to the specified port.</summary>
        public void BeginDockingApproach(string stationId, string portId)
        {
            ActiveStationId = stationId;
            ActivePortId    = portId;
            IsActive        = true;
            _hardDockTimer  = 0f;
            SetPhase(DockingApproachPhase.FreeApproach);
        }

        /// <summary>Aborts the current docking sequence.</summary>
        public void Abort(string reason = "manual")
        {
            if (!IsActive) return;
            IsActive = false;
            OnDockingAborted?.Invoke(reason);
        }

        /// <summary>Undocks from a Docked state (triggers abort with "undock" reason).</summary>
        public void Undock()
        {
            if (CurrentPhase == DockingApproachPhase.Docked)
                Abort("undock");
        }

        /// <summary>
        /// Must be called each physics frame with current approach telemetry.
        /// Drives phase transitions and enforces fail conditions.
        /// </summary>
        /// <param name="distance">Distance to port in metres.</param>
        /// <param name="closingSpeed">Positive = approaching (m/s).</param>
        /// <param name="alignmentDeg">Angular deviation from port axis in degrees (0 = perfect).</param>
        /// <param name="deltaTime">Elapsed time since last call (seconds).</param>
        public void Tick(float distance, float closingSpeed, float alignmentDeg, float deltaTime)
        {
            if (!IsActive) return;

            switch (CurrentPhase)
            {
                case DockingApproachPhase.FreeApproach:
                    if (distance <= DistInitialAlignment)
                        SetPhase(DockingApproachPhase.InitialAlignment);
                    break;

                case DockingApproachPhase.InitialAlignment:
                    if (closingSpeed > SpeedLimitInitial)
                    {
                        Abort("speed_exceeded_initial");
                        return;
                    }
                    if (distance <= DistFinalApproach && alignmentDeg <= AlignToleranceInitial)
                        SetPhase(DockingApproachPhase.FinalApproach);
                    break;

                case DockingApproachPhase.FinalApproach:
                    if (closingSpeed > SpeedAbortFinal || alignmentDeg > AlignAbortFinal)
                    {
                        Abort(closingSpeed > SpeedAbortFinal ? "speed_exceeded_final" : "alignment_lost");
                        return;
                    }
                    if (distance <= DistSoftCapture &&
                        closingSpeed <= SpeedLimitFinal &&
                        alignmentDeg <= AlignToleranceFinal)
                        SetPhase(DockingApproachPhase.SoftCapture);
                    break;

                case DockingApproachPhase.SoftCapture:
                    if (closingSpeed > SpeedAbortCollision)
                    {
                        Abort("collision");
                        return;
                    }
                    if (distance <= DistHardDock &&
                        closingSpeed <= SpeedLimitSoftCapture &&
                        alignmentDeg <= AlignToleranceSoft)
                        SetPhase(DockingApproachPhase.HardDock);
                    break;

                case DockingApproachPhase.HardDock:
                    _hardDockTimer += deltaTime;
                    if (_hardDockTimer >= HardDockAutoLockTime)
                    {
                        SetPhase(DockingApproachPhase.Docked);
                        IsActive = false;
                        OnDockingComplete?.Invoke();
                    }
                    break;

                case DockingApproachPhase.Docked:
                    // No automatic transitions; player must call Undock().
                    break;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SetPhase(DockingApproachPhase phase)
        {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }
    }
}
