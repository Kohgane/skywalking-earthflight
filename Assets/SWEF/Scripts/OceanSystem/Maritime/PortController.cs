// PortController.cs — Phase 117: Advanced Ocean & Maritime System
// Port simulation: vessel docking, cargo operations, tugboat assistance.
// Namespace: SWEF.OceanSystem

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Simulates a port / harbour facility.
    /// Manages vessel docking berths, cargo loading/unloading timers,
    /// and tugboat assignment for larger vessels.
    /// </summary>
    public class PortController : MonoBehaviour
    {
        // ── Berth State ───────────────────────────────────────────────────────────

        [Serializable]
        private class Berth
        {
            public int        berthIndex;
            public Transform  berthTransform;
            public bool       isOccupied;
            public string     occupantVesselId;
            public float      cargoTimer;       // seconds remaining for cargo ops
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Port Settings")]
        [SerializeField] private string portName = "Harbour";
        [SerializeField] private int    maxBerths = 6;
        [SerializeField] private float  cargoOperationDuration = 120f;

        [Header("Tugboats")]
        [SerializeField] private float tugAssistSpeedThreshold = 10f; // vessel size proxy

        // ── Private state ─────────────────────────────────────────────────────────

        private readonly List<Berth> _berths = new List<Berth>();

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a vessel docks at a berth.</summary>
        public event Action<string, int> OnVesselDocked;    // (vesselId, berthIndex)

        /// <summary>Raised when a vessel completes cargo operations and departs.</summary>
        public event Action<string> OnVesselDeparted;       // (vesselId)

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Port display name.</summary>
        public string PortName => portName;

        /// <summary>Number of currently occupied berths.</summary>
        public int OccupiedBerths => _berths.FindAll(b => b.isOccupied).Count;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            for (int i = 0; i < maxBerths; i++)
                _berths.Add(new Berth { berthIndex = i });
        }

        private void Update()
        {
            TickCargo();
        }

        // ── Cargo Tick ────────────────────────────────────────────────────────────

        private void TickCargo()
        {
            foreach (var berth in _berths)
            {
                if (!berth.isOccupied || berth.cargoTimer <= 0f) continue;
                berth.cargoTimer -= Time.deltaTime;
                if (berth.cargoTimer <= 0f)
                {
                    string vesselId       = berth.occupantVesselId;
                    berth.isOccupied      = false;
                    berth.occupantVesselId = string.Empty;
                    OnVesselDeparted?.Invoke(vesselId);
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Requests a berth for <paramref name="vesselId"/>.
        /// Returns berth index or −1 if port is full.
        /// </summary>
        public int RequestBerth(string vesselId)
        {
            foreach (var berth in _berths)
            {
                if (!berth.isOccupied)
                {
                    berth.isOccupied       = true;
                    berth.occupantVesselId = vesselId;
                    berth.cargoTimer       = cargoOperationDuration;
                    OnVesselDocked?.Invoke(vesselId, berth.berthIndex);
                    return berth.berthIndex;
                }
            }
            return -1;
        }

        /// <summary>Returns whether the port has a free berth.</summary>
        public bool HasFreeBerth() => _berths.Exists(b => !b.isOccupied);
    }
}
