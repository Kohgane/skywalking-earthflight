// FuelTank.cs — SWEF Fuel & Energy Management System (Phase 69)
using System;
using UnityEngine;

namespace SWEF.Fuel
{
    /// <summary>
    /// Phase 69 — Serializable data model representing a single fuel tank on an aircraft.
    ///
    /// <para>Tanks track current fuel level, leak state, and weight contribution.
    /// The Damage system (Phase 66) calls <see cref="SetLeaking"/> when a tank is
    /// punctured; <see cref="SealLeak"/> provides a partial emergency fix.</para>
    /// </summary>
    [Serializable]
    public class FuelTank
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Unique identifier for this tank, e.g. \"MainLeft\", \"MainRight\", \"Reserve\".")]
        public string tankId = "Tank";

        [Tooltip("Type of fuel this tank holds.")]
        public FuelType fuelType = FuelType.Standard;

        // ── Capacity ──────────────────────────────────────────────────────────

        [Header("Capacity")]
        [Tooltip("Maximum fuel volume in litres.")]
        public float capacity = 500f;

        [Tooltip("Current fuel volume in litres.")]
        public float currentFuel = 500f;

        // ── State ─────────────────────────────────────────────────────────────

        [Header("State")]
        [Tooltip("Current operational state of this tank.")]
        public TankState state = TankState.Normal;

        [Tooltip("Litres per second leaking from this tank (0 when not leaking).")]
        public float leakRate = 0f;

        // ── Computed Properties ───────────────────────────────────────────────

        /// <summary>Fuel level as a fraction of total capacity (0–1).</summary>
        public float fuelPercent => capacity > 0f ? currentFuel / capacity : 0f;

        /// <summary><c>true</c> when the tank contains no remaining fuel.</summary>
        public bool isEmpty => currentFuel <= 0f;

        /// <summary>
        /// Mass of the fuel currently in this tank in kilograms.
        /// Uses <see cref="FuelConfig.FuelDensity"/> (kg per litre).
        /// </summary>
        public float weight => currentFuel * FuelConfig.FuelDensity;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised once when this tank transitions to empty.</summary>
        public event Action<FuelTank> OnTankEmpty;

        /// <summary>Raised whenever this tank's <see cref="state"/> changes.</summary>
        public event Action<FuelTank, TankState> OnTankStateChanged;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Removes <paramref name="amount"/> litres from the tank, clamped to zero.
        /// Raises <see cref="OnTankEmpty"/> on the first transition to empty.
        /// </summary>
        /// <param name="amount">Litres to consume (must be positive).</param>
        public void Consume(float amount)
        {
            if (amount <= 0f) return;

            bool wasEmpty = isEmpty;
            currentFuel = Mathf.Max(0f, currentFuel - amount);

            if (!wasEmpty && isEmpty)
            {
                SetState(TankState.Empty);
                OnTankEmpty?.Invoke(this);
            }
        }

        /// <summary>
        /// Adds <paramref name="amount"/> litres to the tank, clamped to <see cref="capacity"/>.
        /// </summary>
        /// <param name="amount">Litres to add (must be positive).</param>
        public void Refuel(float amount)
        {
            if (amount <= 0f) return;

            currentFuel = Mathf.Min(capacity, currentFuel + amount);

            // Recover from Empty state when fuel is restored.
            if (state == TankState.Empty && currentFuel > 0f)
                SetState(TankState.Normal);
        }

        /// <summary>
        /// Marks this tank as leaking at the specified rate.
        /// Called by the Damage system (Phase 66) when a tank is punctured.
        /// </summary>
        /// <param name="rate">Litres per second to lose through the leak.</param>
        public void SetLeaking(float rate)
        {
            leakRate = Mathf.Max(0f, rate);
            if (leakRate > 0f)
                SetState(TankState.Leaking);
        }

        /// <summary>
        /// Emergency-seals the leak, stopping further fuel loss.
        /// The tank transitions to <see cref="TankState.Sealed"/> rather than
        /// <see cref="TankState.Normal"/> to indicate it still requires proper repair.
        /// </summary>
        public void SealLeak()
        {
            leakRate = 0f;
            if (state == TankState.Leaking)
                SetState(TankState.Sealed);
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void SetState(TankState newState)
        {
            if (state == newState) return;
            state = newState;
            OnTankStateChanged?.Invoke(this, newState);
        }
    }
}
