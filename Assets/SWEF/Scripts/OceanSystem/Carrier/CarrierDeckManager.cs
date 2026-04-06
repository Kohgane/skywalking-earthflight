// CarrierDeckManager.cs — Phase 117: Advanced Ocean & Maritime System
// Deck operations: parking, elevator, catapult queue, landing pattern.
// Namespace: SWEF.OceanSystem

#if SWEF_CARRIER_AVAILABLE || !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Manages aircraft carrier deck operations including parking slot
    /// assignments, elevator cycling, catapult launch queues, and landing pattern
    /// calls (bolter, wave-off, trap).
    /// </summary>
    public class CarrierDeckManager : MonoBehaviour
    {
        // ── Landing Pattern Result ────────────────────────────────────────────────

        /// <summary>Outcome of a carrier landing approach.</summary>
        public enum LandingResult { Trap, Bolter, WaveOff }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Deck Layout")]
        [SerializeField] private DeckSlotState[] deckSlots;

        // ── Private state ─────────────────────────────────────────────────────────

        private readonly Queue<string> _catapultQueue = new Queue<string>();

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when an aircraft receives a trap, bolter, or wave-off call.</summary>
        public event Action<string, LandingResult> OnLandingResult; // (aircraftId, result)

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Number of aircraft awaiting catapult launch.</summary>
        public int CatapultQueueLength => _catapultQueue.Count;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            InitialiseSlots();
        }

        private void InitialiseSlots()
        {
            if (deckSlots == null) deckSlots = new DeckSlotState[0];
            for (int i = 0; i < deckSlots.Length; i++)
            {
                if (deckSlots[i] == null) deckSlots[i] = new DeckSlotState { slotIndex = i };
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Assigns an aircraft to the first available deck slot.</summary>
        /// <returns>The assigned slot index, or −1 if deck is full.</returns>
        public int AssignParkingSlot(string aircraftId)
        {
            foreach (var slot in deckSlots)
            {
                if (!slot.isOccupied)
                {
                    slot.isOccupied  = true;
                    slot.aircraftId  = aircraftId;
                    return slot.slotIndex;
                }
            }
            return -1;
        }

        /// <summary>Releases the parking slot occupied by <paramref name="aircraftId"/>.</summary>
        public void ReleaseParkingSlot(string aircraftId)
        {
            foreach (var slot in deckSlots)
            {
                if (slot.aircraftId == aircraftId)
                {
                    slot.isOccupied = false;
                    slot.aircraftId = string.Empty;
                }
            }
        }

        /// <summary>Queues an aircraft for catapult launch.</summary>
        public void EnqueueForLaunch(string aircraftId)
        {
            if (!_catapultQueue.Contains(aircraftId))
                _catapultQueue.Enqueue(aircraftId);
        }

        /// <summary>Dequeues the next aircraft for catapult launch.</summary>
        /// <returns>Aircraft ID or empty string if queue is empty.</returns>
        public string DequeueForLaunch()
        {
            return _catapultQueue.Count > 0 ? _catapultQueue.Dequeue() : string.Empty;
        }

        /// <summary>Records and broadcasts the result of a landing attempt.</summary>
        public void RecordLandingResult(string aircraftId, LandingResult result)
        {
            OnLandingResult?.Invoke(aircraftId, result);
        }

        /// <summary>Returns the deck slot state for a given aircraft ID, or null.</summary>
        public DeckSlotState GetSlotForAircraft(string aircraftId)
        {
            if (deckSlots == null) return null;
            foreach (var slot in deckSlots)
                if (slot.aircraftId == aircraftId) return slot;
            return null;
        }
    }
}
#endif
