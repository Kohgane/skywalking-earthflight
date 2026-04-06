// CargoDeliveryMission.cs — Phase 117: Advanced Ocean & Maritime System
// Cargo delivery: carrier resupply, offshore platform, island supply runs.
// Namespace: SWEF.OceanSystem

using System;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Represents a cargo delivery maritime mission.
    /// Tracks pickup and delivery waypoints, cargo weight, and delivery timer.
    /// </summary>
    public class CargoDeliveryMission : MonoBehaviour
    {
        // ── Delivery State ────────────────────────────────────────────────────────

        /// <summary>State of a cargo delivery mission.</summary>
        public enum DeliveryState { Inactive, PickingUp, EnRoute, Delivering, Complete, Failed }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Mission Settings")]
        [SerializeField] private string missionId = "CDM-001";
        [SerializeField] private string missionTitle = "Carrier Resupply";
        [SerializeField] private float  cargoWeightKg = 2000f;
        [SerializeField] private float  timeLimitSeconds = 900f;

        [Header("Waypoints")]
        [SerializeField] private Transform pickupPoint;
        [SerializeField] private Transform deliveryPoint;
        [SerializeField] private float     waypointRadius = 100f;

        // ── Private state ─────────────────────────────────────────────────────────

        private DeliveryState _state = DeliveryState.Inactive;
        private float         _remainingTime;
        private bool          _cargoLoaded;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when cargo is picked up at the source.</summary>
        public event Action OnCargoPickedUp;

        /// <summary>Raised when cargo is successfully delivered.</summary>
        public event Action<string> OnDeliveryComplete; // (missionId)

        /// <summary>Raised when the delivery timer expires.</summary>
        public event Action<string> OnDeliveryFailed;   // (missionId)

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Current delivery state.</summary>
        public DeliveryState State => _state;

        /// <summary>Whether cargo is currently loaded on the aircraft.</summary>
        public bool IsCargoLoaded => _cargoLoaded;

        /// <summary>Remaining time in seconds (0 = no limit or mission ended).</summary>
        public float RemainingTime => _remainingTime;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Starts the cargo delivery mission.</summary>
        public void StartMission()
        {
            _state          = DeliveryState.PickingUp;
            _remainingTime  = timeLimitSeconds;
            _cargoLoaded    = false;
        }

        /// <summary>Updates the mission. Call every frame with the aircraft position.</summary>
        public void Tick(Vector3 aircraftPosition)
        {
            if (_state == DeliveryState.Inactive || _state == DeliveryState.Complete || _state == DeliveryState.Failed)
                return;

            // Timer
            if (_remainingTime > 0f)
            {
                _remainingTime -= Time.deltaTime;
                if (_remainingTime <= 0f)
                {
                    _state = DeliveryState.Failed;
                    OnDeliveryFailed?.Invoke(missionId);
                    return;
                }
            }

            switch (_state)
            {
                case DeliveryState.PickingUp:
                    if (pickupPoint != null && Vector3.Distance(aircraftPosition, pickupPoint.position) <= waypointRadius)
                    {
                        _cargoLoaded = true;
                        _state       = DeliveryState.EnRoute;
                        OnCargoPickedUp?.Invoke();
                    }
                    break;

                case DeliveryState.EnRoute:
                    _state = DeliveryState.Delivering;
                    break;

                case DeliveryState.Delivering:
                    if (deliveryPoint != null && Vector3.Distance(aircraftPosition, deliveryPoint.position) <= waypointRadius)
                    {
                        _cargoLoaded = false;
                        _state       = DeliveryState.Complete;
                        OnDeliveryComplete?.Invoke(missionId);
                    }
                    break;
            }
        }
    }
}
