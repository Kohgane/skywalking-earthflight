// ATCBridge.cs — Phase 119: Advanced AI Traffic Control
// Integration with existing SWEF systems: Flight, NPC Traffic, Weather,
// Navigation, Achievement.
// Namespace: SWEF.ATC

using System;
using UnityEngine;

#if SWEF_ATC_AVAILABLE

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Cross-system bridge that connects the ATC module to other
    /// SWEF subsystems (Flight, NPC Traffic, Weather, Navigation, Achievement).
    /// All cross-system calls are guarded behind <c>#if SWEF_ATC_AVAILABLE</c>.
    /// </summary>
    public class ATCBridge : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared instance of <see cref="ATCBridge"/>.</summary>
        public static ATCBridge Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when the player's flight phase changes via ATC.</summary>
        public static event Action<FlightPhase> OnPlayerPhaseChanged;

        /// <summary>Raised when the player receives a new ATC instruction.</summary>
        public static event Action<ATCInstructionCode> OnPlayerInstructionReceived;

        /// <summary>Raised when a conflict alert involves the player.</summary>
        public static event Action<ConflictAlert> OnPlayerConflictAlert;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnsubscribeEvents();
        }

        // ── Subscriptions ─────────────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (ATCSystemManager.Instance == null) return;
            ATCSystemManager.Instance.OnInstructionIssued += HandleInstruction;
            ATCSystemManager.Instance.OnConflictAlert      += HandleConflict;
        }

        private void UnsubscribeEvents()
        {
            if (ATCSystemManager.Instance == null) return;
            ATCSystemManager.Instance.OnInstructionIssued -= HandleInstruction;
            ATCSystemManager.Instance.OnConflictAlert      -= HandleConflict;
        }

        // ── Handlers ──────────────────────────────────────────────────────────────

        private void HandleInstruction(string callsign, ATCInstructionCode instruction)
        {
            // Forward to player flight controller when callsign matches player
            OnPlayerInstructionReceived?.Invoke(instruction);
        }

        private void HandleConflict(ConflictAlert alert)
        {
            OnPlayerConflictAlert?.Invoke(alert);
        }

        // ── Public Bridge API ─────────────────────────────────────────────────────

        /// <summary>
        /// Notifies ATC of a player-initiated emergency.
        /// </summary>
        public void NotifyPlayerEmergency(string callsign, string type)
        {
            ATCSystemManager.Instance?.GetStrip(callsign)?.ToString();   // touch strip
            var handler = FindFirstObjectByType<EmergencyTrafficHandler>();
            handler?.DeclareEmergency(callsign, type);
        }

        /// <summary>
        /// Requests a clearance for the player callsign.
        /// </summary>
        public void RequestClearance(string callsign)
        {
            ATCSystemManager.Instance?.IssueInstruction(callsign, ATCInstructionCode.Cleared);
        }
    }
}

#endif // SWEF_ATC_AVAILABLE
