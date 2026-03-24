// FuelConfig.cs — SWEF Fuel & Energy Management System (Phase 69)
using UnityEngine;

namespace SWEF.Fuel
{
    /// <summary>
    /// Phase 69 — Compile-time constants shared across the Fuel &amp; Energy Management system.
    ///
    /// <para>Override runtime behaviour by editing <see cref="FuelConsumptionModel"/> and
    /// <see cref="FuelManager"/> inspector fields; these constants serve as sensible
    /// defaults.</para>
    /// </summary>
    public static class FuelConfig
    {
        // ── Physics ───────────────────────────────────────────────────────────

        /// <summary>Fuel density in kilograms per litre. Used to compute tank weight.</summary>
        public const float FuelDensity = 0.8f;

        // ── Consumption ───────────────────────────────────────────────────────

        /// <summary>Default fuel consumption in litres per second at full throttle, sea level.</summary>
        public const float BaseConsumptionRate = 2f;

        /// <summary>Default fuel consumption in litres per second at idle throttle.</summary>
        public const float IdleConsumptionRate = 0.3f;

        /// <summary>Default multiplier applied to the consumption rate during afterburner/boost.</summary>
        public const float AfterburnerMultiplier = 3f;

        // ── Warning Thresholds ────────────────────────────────────────────────

        /// <summary>
        /// Total fuel percentage below which the warning level becomes
        /// <see cref="FuelWarningLevel.Low"/>.
        /// </summary>
        public const float LowFuelThreshold = 0.25f;

        /// <summary>
        /// Total fuel percentage below which the warning level becomes
        /// <see cref="FuelWarningLevel.Critical"/>.
        /// </summary>
        public const float CriticalFuelThreshold = 0.10f;

        // ── Refuelling ────────────────────────────────────────────────────────

        /// <summary>Default fuel flow rate at a refuel station in litres per second.</summary>
        public const float DefaultRefuelRate = 50f;

        // ── Emergency ─────────────────────────────────────────────────────────

        /// <summary>Litres per second dumped during an emergency fuel-dump operation.</summary>
        public const float FuelDumpRate = 20f;

        /// <summary>
        /// Litres held in reserve by <see cref="EmergencyFuelProtocol"/>;
        /// this fuel is not drawn on during normal consumption.
        /// </summary>
        public const float EmergencyReserve = 50f;

        /// <summary>
        /// Throttle ceiling (0–1) applied to the engine when the emergency protocol
        /// is active; limits maximum power to conserve remaining fuel.
        /// </summary>
        public const float EmergencyPowerReduction = 0.5f;

        // ── Glide ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Default glide ratio (distance/altitude) used by
        /// <see cref="EmergencyFuelProtocol.CalculateGlideRange"/> when no aircraft-specific
        /// value is supplied.
        /// </summary>
        public const float GlideRatioDefault = 15f;

        // ── Tanks ─────────────────────────────────────────────────────────────

        /// <summary>Maximum number of fuel tanks supported on a single aircraft.</summary>
        public const int MaxTanks = 6;

        // ── UI Colors ─────────────────────────────────────────────────────────

        /// <summary>Fuel-level indicator color when warning is <see cref="FuelWarningLevel.Normal"/>.</summary>
        public static readonly Color ColorNormal   = new Color(0.20f, 0.85f, 0.20f, 1f);

        /// <summary>Fuel-level indicator color when warning is <see cref="FuelWarningLevel.Low"/>.</summary>
        public static readonly Color ColorLow      = new Color(1.00f, 0.85f, 0.00f, 1f);

        /// <summary>Fuel-level indicator color when warning is <see cref="FuelWarningLevel.Critical"/>.</summary>
        public static readonly Color ColorCritical = new Color(1.00f, 0.35f, 0.00f, 1f);

        /// <summary>Fuel-level indicator color when warning is <see cref="FuelWarningLevel.Empty"/>.</summary>
        public static readonly Color ColorEmpty    = new Color(1.00f, 0.10f, 0.10f, 1f);

        /// <summary>Returns the UI color that corresponds to <paramref name="level"/>.</summary>
        /// <param name="level">Warning level to look up.</param>
        /// <returns>A <see cref="Color"/> value.</returns>
        public static Color GetWarningColor(FuelWarningLevel level)
        {
            switch (level)
            {
                case FuelWarningLevel.Normal:   return ColorNormal;
                case FuelWarningLevel.Low:      return ColorLow;
                case FuelWarningLevel.Critical: return ColorCritical;
                case FuelWarningLevel.Empty:    return ColorEmpty;
                default:                        return ColorNormal;
            }
        }
    }
}
