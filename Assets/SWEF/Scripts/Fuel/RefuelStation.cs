// RefuelStation.cs — SWEF Fuel & Energy Management System (Phase 69)
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Fuel
{
    /// <summary>
    /// Phase 69 — MonoBehaviour placed at airports and landing zones to refuel an aircraft.
    ///
    /// <para>Integrates with the Landing system (Phase 68) fuel stations.
    /// Call <see cref="BeginRefuel"/> when the aircraft is within range, and
    /// <see cref="StopRefuel"/> to disconnect manually.  The coroutine
    /// progressively fills all compatible tanks at <see cref="refuelRate"/>
    /// litres per second.</para>
    /// </summary>
    public class RefuelStation : MonoBehaviour
    {
        #region Inspector

        [Header("Station Identity")]
        [Tooltip("Unique identifier for this refuel station.")]
        [SerializeField] private string stationId = "Station_01";

        [Tooltip("Fuel type dispensed by this station.")]
        [SerializeField] private FuelType availableFuelType = FuelType.Standard;

        [Header("Capacity & Rate")]
        [Tooltip("Fuel flow rate in litres per second.")]
        [SerializeField] private float refuelRate = FuelConfig.DefaultRefuelRate;

        [Tooltip("Total fuel available at this station in litres.")]
        [SerializeField] private float maxFuelAvailable = 10000f;

        [Header("Economics")]
        [Tooltip("Cost per litre in in-game currency.")]
        [SerializeField] private float costPerLiter = 1f;

        [Header("Range")]
        [Tooltip("Maximum distance in metres from this station to the aircraft for refuelling to start.")]
        [SerializeField] private float refuelRange = 30f;

        [Header("Visuals")]
        [Tooltip("Animator on the fuel-hose prop (optional).  Trigger parameter names: " +
                 "\"Connect\" and \"Disconnect\" are fired automatically.")]
        [SerializeField] private Animator hoseAnimator;

        #endregion

        #region Public State

        /// <summary>Unique identifier for this station.</summary>
        public string StationId => stationId;

        /// <summary>Fuel type this station can dispense.</summary>
        public FuelType AvailableFuelType => availableFuelType;

        /// <summary>Litres per second delivered to the aircraft.</summary>
        public float RefuelRate => refuelRate;

        /// <summary>Fuel currently remaining at this station in litres.</summary>
        public float CurrentFuelAvailable { get; private set; }

        /// <summary>Cost per litre in in-game currency.</summary>
        public float CostPerLiter => costPerLiter;

        /// <summary>Maximum range in metres for refuelling.</summary>
        public float RefuelRange => refuelRange;

        /// <summary>Current refuelling operation state.</summary>
        public RefuelState State { get; private set; } = RefuelState.Idle;

        /// <summary>Aircraft currently being refuelled, or <c>null</c> when idle.</summary>
        public FuelManager ConnectedAircraft { get; private set; }

        #endregion

        #region Events

        /// <summary>Raised whenever the refuel <see cref="State"/> changes.</summary>
        public event Action<RefuelState> OnRefuelStateChanged;

        /// <summary>Raised each second with the cumulative litres delivered this session.</summary>
        public event Action<float> OnFuelDelivered;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CurrentFuelAvailable = maxFuelAvailable;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Checks whether refuelling can begin for the given aircraft.
        /// </summary>
        /// <param name="aircraft">The aircraft's <see cref="FuelManager"/>.</param>
        /// <returns>
        /// <c>true</c> when the station is idle, has fuel, the aircraft is in range,
        /// and the fuel type is compatible with at least one tank.
        /// </returns>
        public bool CanRefuel(FuelManager aircraft)
        {
            if (aircraft == null)                          return false;
            if (State != RefuelState.Idle)                 return false;
            if (CurrentFuelAvailable <= 0f)                return false;
            if (!IsInRange(aircraft))                      return false;
            if (!HasCompatibleTank(aircraft))              return false;
            return true;
        }

        /// <summary>
        /// Begins the refuelling process for <paramref name="aircraft"/>.
        /// Does nothing if <see cref="CanRefuel"/> returns <c>false</c>.
        /// </summary>
        /// <param name="aircraft">The aircraft's <see cref="FuelManager"/>.</param>
        public void BeginRefuel(FuelManager aircraft)
        {
            if (!CanRefuel(aircraft)) return;

            ConnectedAircraft = aircraft;
            SetState(RefuelState.Connecting);
            StartCoroutine(RefuelCoroutine());
        }

        /// <summary>
        /// Disconnects the fuel hose and stops refuelling.
        /// </summary>
        public void StopRefuel()
        {
            StopAllCoroutines();
            TriggerHoseAnimation("Disconnect");
            SetState(State == RefuelState.Complete
                ? RefuelState.Complete
                : RefuelState.Disconnected);
            ConnectedAircraft = null;
        }

        #endregion

        #region Refuel Coroutine

        private IEnumerator RefuelCoroutine()
        {
            // Brief connection delay (hose animation).
            TriggerHoseAnimation("Connect");
            yield return new WaitForSeconds(1.5f);

            SetState(RefuelState.Refueling);
            float delivered = 0f;

            while (ConnectedAircraft != null && CurrentFuelAvailable > 0f)
            {
                // Stop if aircraft moved out of range.
                if (!IsInRange(ConnectedAircraft))
                {
                    StopRefuel();
                    yield break;
                }

                // Fill compatible tanks that still need fuel.
                bool allFull = true;
                foreach (var tank in ConnectedAircraft.Tanks)
                {
                    if (tank.fuelType != availableFuelType) continue;
                    if (tank.fuelPercent >= 1f)             continue;

                    float needed    = tank.capacity - tank.currentFuel;
                    float available = Mathf.Min(needed,
                                               CurrentFuelAvailable,
                                               refuelRate * Time.deltaTime);
                    if (available <= 0f) continue;

                    tank.Refuel(available);
                    CurrentFuelAvailable -= available;
                    delivered            += available;
                    allFull               = false;
                }

                OnFuelDelivered?.Invoke(delivered);

                if (allFull)
                {
                    // All compatible tanks are full.
                    TriggerHoseAnimation("Disconnect");
                    SetState(RefuelState.Complete);
                    ConnectedAircraft = null;
                    yield break;
                }

                yield return null;
            }

            // Station ran out of fuel.
            TriggerHoseAnimation("Disconnect");
            SetState(RefuelState.Complete);
            ConnectedAircraft = null;
        }

        #endregion

        #region Private Helpers

        private bool IsInRange(FuelManager aircraft)
        {
            return Vector3.Distance(transform.position, aircraft.transform.position) <= refuelRange;
        }

        private bool HasCompatibleTank(FuelManager aircraft)
        {
            foreach (var tank in aircraft.Tanks)
                if (tank.fuelType == availableFuelType) return true;
            return false;
        }

        private void SetState(RefuelState newState)
        {
            if (State == newState) return;
            State = newState;
            OnRefuelStateChanged?.Invoke(newState);
        }

        private void TriggerHoseAnimation(string triggerName)
        {
            if (hoseAnimator != null)
                hoseAnimator.SetTrigger(triggerName);
        }

        #endregion
    }
}
