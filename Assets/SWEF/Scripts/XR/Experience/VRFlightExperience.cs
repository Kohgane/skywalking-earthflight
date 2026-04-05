// VRFlightExperience.cs — Phase 112: VR/XR Flight Experience
// Orchestrates the full VR flight loop: preflight, takeoff, cruise, landing.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Top-level orchestrator for the immersive VR flight experience.
    /// Drives a <see cref="VRFlightPhase"/> state machine and fires
    /// per-phase events consumed by VR sub-systems (cockpit, HUD, weather, etc.).
    /// </summary>
    public class VRFlightExperience : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Sub-Systems")]
        [SerializeField] private VRCockpitController  cockpit;
        [SerializeField] private VRWeatherEffects     weatherEffects;
        [SerializeField] private VRSpatialAudio       spatialAudio;
        [SerializeField] private VRHUDRenderer        hud;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current flight phase.</summary>
        public VRFlightPhase CurrentPhase { get; private set; } = VRFlightPhase.Preflight;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired whenever the flight phase transitions.</summary>
        public event Action<VRFlightPhase> OnPhaseChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            TransitionToPhase(VRFlightPhase.Preflight);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Advances to the next flight phase in sequence.</summary>
        public void AdvancePhase()
        {
            var next = CurrentPhase switch
            {
                VRFlightPhase.Preflight => VRFlightPhase.Takeoff,
                VRFlightPhase.Takeoff   => VRFlightPhase.Cruise,
                VRFlightPhase.Cruise    => VRFlightPhase.Landing,
                VRFlightPhase.Landing   => VRFlightPhase.Debrief,
                _                       => VRFlightPhase.Debrief
            };
            TransitionToPhase(next);
        }

        /// <summary>Jumps directly to a specified flight phase.</summary>
        public void TransitionToPhase(VRFlightPhase phase)
        {
            if (CurrentPhase == phase) return;
            ExitPhase(CurrentPhase);
            CurrentPhase = phase;
            EnterPhase(CurrentPhase);
            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void EnterPhase(VRFlightPhase phase)
        {
            Debug.Log($"[SWEF] VRFlightExperience: Entering phase {phase}.");
            switch (phase)
            {
                case VRFlightPhase.Preflight:
                    cockpit?.EnterCockpit();
                    break;
                case VRFlightPhase.Takeoff:
                    weatherEffects?.SetTakeoffEffects(true);
                    spatialAudio?.SetEngineThrottleLevel(0.7f);
                    hud?.SetVisible(true);
                    break;
                case VRFlightPhase.Cruise:
                    weatherEffects?.SetCruiseEffects(true);
                    spatialAudio?.SetEngineThrottleLevel(0.5f);
                    break;
                case VRFlightPhase.Landing:
                    weatherEffects?.SetTakeoffEffects(false);
                    spatialAudio?.SetEngineThrottleLevel(0.3f);
                    break;
                case VRFlightPhase.Debrief:
                    cockpit?.ExitCockpit();
                    hud?.SetVisible(false);
                    spatialAudio?.SetEngineThrottleLevel(0f);
                    break;
            }
        }

        private void ExitPhase(VRFlightPhase phase)
        {
            // Per-phase teardown hooks (reserved for future use).
        }
    }
}
