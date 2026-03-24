// FuelManager.cs — SWEF Fuel & Energy Management System (Phase 69)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Fuel
{
    /// <summary>
    /// Phase 69 — Central singleton MonoBehaviour that manages all fuel tanks on the
    /// aircraft, drives consumption each <c>FixedUpdate</c>, and publishes warning events.
    ///
    /// <para>Call <see cref="ConsumeFuel"/> from your flight-physics update with current
    /// throttle, altitude, speed, and afterburner state each fixed time-step.</para>
    /// </summary>
    public class FuelManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static FuelManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitialiseTanks();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Inspector

        [Header("Tanks")]
        [Tooltip("All fuel tanks fitted to this aircraft. Maximum " + nameof(FuelConfig.MaxTanks) + " tanks.")]
        [SerializeField] private List<FuelTank> tanks = new List<FuelTank>();

        [Header("Consumption")]
        [Tooltip("ScriptableObject that defines fuel burn curves and multipliers.")]
        [SerializeField] private FuelConsumptionModel consumptionModel;

        [Header("Tank Management")]
        [Tooltip("Automatically switch to the next available tank when the active tank empties.")]
        [SerializeField] private bool autoSwitchTanks = true;

        #endregion

        #region Public State

        /// <summary>All fuel tanks on this aircraft.</summary>
        public IReadOnlyList<FuelTank> Tanks => tanks;

        /// <summary>The consumption model driving fuel burn calculations.</summary>
        public FuelConsumptionModel ConsumptionModel => consumptionModel;

        /// <summary>The tank currently supplying fuel to the engine.</summary>
        public FuelTank ActiveTank { get; private set; }

        /// <summary>Whether the fuel-dump system is currently active.</summary>
        public bool IsDumpingFuel { get; private set; }

        /// <summary>Total litres of fuel across all tanks.</summary>
        public float TotalFuel
        {
            get
            {
                float total = 0f;
                foreach (var t in tanks) total += t.currentFuel;
                return total;
            }
        }

        /// <summary>Total fuel capacity across all tanks in litres.</summary>
        public float TotalCapacity
        {
            get
            {
                float total = 0f;
                foreach (var t in tanks) total += t.capacity;
                return total;
            }
        }

        /// <summary>Total fuel as a fraction of total capacity (0–1).</summary>
        public float TotalFuelPercent =>
            TotalCapacity > 0f ? TotalFuel / TotalCapacity : 0f;

        /// <summary>Instantaneous fuel consumption rate in litres per second.</summary>
        public float CurrentConsumptionRate { get; private set; }

        /// <summary>
        /// Estimated seconds of flight remaining at the current consumption rate.
        /// Returns <c>float.PositiveInfinity</c> when consumption is zero.
        /// </summary>
        public float EstimatedFlightTime =>
            CurrentConsumptionRate > 0f
                ? TotalFuel / CurrentConsumptionRate
                : float.PositiveInfinity;

        /// <summary>Total litres consumed since the aircraft started flying.</summary>
        public float FuelUsedThisFlight { get; private set; }

        /// <summary>Current pilot-facing fuel warning level.</summary>
        public FuelWarningLevel WarningLevel { get; private set; } = FuelWarningLevel.Normal;

        #endregion

        #region Events

        /// <summary>Raised when the <see cref="WarningLevel"/> changes.</summary>
        public event Action<FuelWarningLevel> OnFuelWarningChanged;

        /// <summary>Raised once when all tanks are empty and fuel is fully depleted.</summary>
        public event Action OnFuelDepleted;

        /// <summary>Raised when the active tank switches (auto or manual).</summary>
        public event Action<FuelTank> OnTankSwitched;

        /// <summary>Raised each frame with the amount of fuel consumed in litres.</summary>
        public event Action<float> OnFuelConsumed;

        #endregion

        #region Private State

        private FuelWarningLevel _previousWarning = FuelWarningLevel.Normal;
        private float _dumpTimer;

        #endregion

        #region Unity Lifecycle

        private void FixedUpdate()
        {
            ProcessLeaks(Time.fixedDeltaTime);
            ProcessDump(Time.fixedDeltaTime);
        }

        #endregion

        #region Public API — Consumption

        /// <summary>
        /// Deducts fuel from the active tank based on current flight parameters.
        /// Call this once per <c>FixedUpdate</c> from the aircraft flight controller.
        /// </summary>
        /// <param name="deltaTime">Elapsed seconds since the last call (use <c>Time.fixedDeltaTime</c>).</param>
        /// <param name="throttle">Throttle position in [0, 1].</param>
        /// <param name="altitude">Altitude above sea level in metres.</param>
        /// <param name="speed">Airspeed in metres per second.</param>
        /// <param name="afterburner"><c>true</c> when afterburner or boost is active.</param>
        public void ConsumeFuel(float deltaTime, float throttle, float altitude,
                                float speed, bool afterburner)
        {
            if (consumptionModel == null || deltaTime <= 0f) return;

            CurrentConsumptionRate =
                consumptionModel.CalculateConsumption(throttle, altitude, speed, afterburner);

            float amount = CurrentConsumptionRate * deltaTime;
            if (amount <= 0f) return;

            DeductFuel(amount);
        }

        #endregion

        #region Public API — Tank Management

        /// <summary>
        /// Manually transfers fuel between two tanks on the same aircraft.
        /// The transfer is clamped so the source tank cannot go below zero and
        /// the destination cannot exceed its capacity.
        /// </summary>
        /// <param name="from">Source tank.</param>
        /// <param name="to">Destination tank.</param>
        /// <param name="amount">Litres to transfer.</param>
        public void TransferFuel(FuelTank from, FuelTank to, float amount)
        {
            if (from == null || to == null || amount <= 0f) return;

            float transferable = Mathf.Min(amount, from.currentFuel,
                                           to.capacity - to.currentFuel);
            if (transferable <= 0f) return;

            from.Consume(transferable);
            to.Refuel(transferable);
        }

        /// <summary>
        /// Begins or stops the emergency fuel-dump sequence.
        /// Fuel is dumped from all tanks at <see cref="FuelConfig.FuelDumpRate"/> litres/s
        /// to reduce aircraft weight.
        /// </summary>
        /// <param name="rate">Litres per second to dump (defaults to <see cref="FuelConfig.FuelDumpRate"/>).</param>
        public void DumpFuel(float rate = FuelConfig.FuelDumpRate)
        {
            IsDumpingFuel = true;
            _dumpTimer    = 0f;
            CurrentConsumptionRate = Mathf.Max(CurrentConsumptionRate, rate);
        }

        /// <summary>Stops an ongoing emergency fuel dump.</summary>
        public void StopDump()
        {
            IsDumpingFuel = false;
        }

        /// <summary>
        /// Returns the next non-empty, non-sealed tank that is ready to supply fuel,
        /// or <c>null</c> if no suitable tank exists.
        /// </summary>
        /// <returns>The next available <see cref="FuelTank"/>, or <c>null</c>.</returns>
        public FuelTank GetNextAvailableTank()
        {
            foreach (var tank in tanks)
            {
                if (tank == ActiveTank) continue;
                if (!tank.isEmpty && tank.state != TankState.Sealed)
                    return tank;
            }
            return null;
        }

        /// <summary>Manually switches the active (feeding) tank to <paramref name="tank"/>.</summary>
        /// <param name="tank">Tank to set as active.</param>
        public void SetActiveTank(FuelTank tank)
        {
            if (tank == null || !tanks.Contains(tank)) return;
            ActiveTank = tank;
            OnTankSwitched?.Invoke(ActiveTank);
        }

        #endregion

        #region Private Helpers

        private void InitialiseTanks()
        {
            // Subscribe to each tank's empty event.
            foreach (var tank in tanks)
                tank.OnTankEmpty += HandleTankEmpty;

            // Select first non-empty tank as active.
            ActiveTank = GetNextAvailableTank() ?? (tanks.Count > 0 ? tanks[0] : null);
        }

        private void DeductFuel(float amount)
        {
            if (ActiveTank == null) return;

            // Consume from active tank.
            float before = ActiveTank.currentFuel;
            ActiveTank.Consume(amount);
            float consumed = before - ActiveTank.currentFuel;

            FuelUsedThisFlight += consumed;
            OnFuelConsumed?.Invoke(consumed);

            UpdateWarningLevel();
        }

        private void HandleTankEmpty(FuelTank tank)
        {
            if (autoSwitchTanks && tank == ActiveTank)
            {
                FuelTank next = GetNextAvailableTank();
                if (next != null)
                {
                    ActiveTank = next;
                    OnTankSwitched?.Invoke(ActiveTank);
                }
                else
                {
                    // All tanks empty.
                    OnFuelDepleted?.Invoke();
                    UpdateWarningLevel();
                }
            }
        }

        private void ProcessLeaks(float deltaTime)
        {
            foreach (var tank in tanks)
            {
                if (tank.state == TankState.Leaking && tank.leakRate > 0f)
                {
                    float leaked = tank.leakRate * deltaTime;
                    tank.Consume(leaked);
                    FuelUsedThisFlight += leaked;
                    OnFuelConsumed?.Invoke(leaked);
                }
            }

            UpdateWarningLevel();
        }

        private void ProcessDump(float deltaTime)
        {
            if (!IsDumpingFuel) return;

            // FuelConfig.FuelDumpRate is the *total* aircraft dump rate in L/s.
            // Spread it evenly across all tanks so the aggregate equals FuelDumpRate.
            float dumpPerTank = FuelConfig.FuelDumpRate / Mathf.Max(1, tanks.Count);
            foreach (var tank in tanks)
                tank.Consume(dumpPerTank * deltaTime);

            if (TotalFuel <= 0f)
                IsDumpingFuel = false;

            UpdateWarningLevel();
        }

        private void UpdateWarningLevel()
        {
            float pct = TotalFuelPercent;
            FuelWarningLevel level;

            if (pct <= 0f)
                level = FuelWarningLevel.Empty;
            else if (pct < FuelConfig.CriticalFuelThreshold)
                level = FuelWarningLevel.Critical;
            else if (pct < FuelConfig.LowFuelThreshold)
                level = FuelWarningLevel.Low;
            else
                level = FuelWarningLevel.Normal;

            if (level != _previousWarning)
            {
                WarningLevel      = level;
                _previousWarning  = level;
                OnFuelWarningChanged?.Invoke(level);
            }
        }

        #endregion
    }
}
