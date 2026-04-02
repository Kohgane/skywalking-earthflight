// FuelCalculator.cs — SWEF Advanced Navigation & Flight Plan System (Phase 87)
using UnityEngine;

namespace SWEF.FlightPlan
{
    /// <summary>
    /// Phase 87 — Static utility class for all fuel-related calculations.
    ///
    /// <para>Uses altitude-banded fuel flow tables and a wind-component adjustment to
    /// estimate fuel burn, range, endurance, and optimal cruise altitude.</para>
    ///
    /// <para>Distances are in nautical miles, altitudes in feet, speeds in knots,
    /// fuel quantities in kilograms, and time in hours.</para>
    /// </summary>
    public static class FuelCalculator
    {
        // ── Altitude Band Boundaries (ft) ─────────────────────────────────────────
        private const float BandFL100 = 10000f;
        private const float BandFL200 = 20000f;
        private const float BandFL300 = 30000f;
        private const float BandFL350 = 35000f;

        // ── Fuel Flow Speed Adjustment Factor ─────────────────────────────────────
        // Each 1 kt above 250 kts (reference) increases fuel flow by this fraction.
        private const float SpeedFuelFactor = 0.001f;

        // ── Reference Speed for Flow Calculation (kts) ───────────────────────────
        private const float ReferenceSpeedKts = 250f;

        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Calculates the total fuel burned for a flight of <paramref name="distanceNm"/>
        /// nautical miles at <paramref name="altitudeFt"/> feet, <paramref name="speedKts"/>
        /// knots, and a head/tail <paramref name="windComponentKts"/> (positive = headwind,
        /// negative = tailwind).
        /// </summary>
        /// <returns>Fuel consumed in kg.</returns>
        public static float CalculateFuelBurn(float distanceNm, float altitudeFt,
                                              float speedKts, float windComponentKts)
        {
            if (distanceNm <= 0f) return 0f;

            float flowRate   = GetFuelFlowRate(altitudeFt, speedKts);
            float groundSpeed = Mathf.Max(1f, speedKts - windComponentKts);
            float timeHours  = distanceNm / groundSpeed;
            return flowRate * timeHours;
        }

        /// <summary>
        /// Calculates the maximum range in nautical miles achievable with
        /// <paramref name="fuelKg"/> kilograms of fuel at the given altitude and speed.
        /// </summary>
        public static float CalculateRange(float fuelKg, float altitudeFt, float speedKts)
        {
            if (fuelKg <= 0f || speedKts < 1f) return 0f;
            float flowRate = GetFuelFlowRate(altitudeFt, speedKts);
            if (flowRate < 0.001f) return float.MaxValue;
            float enduranceHours = fuelKg / flowRate;
            return enduranceHours * speedKts; // nm = hrs × kts
        }

        /// <summary>
        /// Calculates the endurance in hours for <paramref name="fuelKg"/> kg of fuel
        /// at <paramref name="altitudeFt"/> feet and <paramref name="speedKts"/> knots.
        /// </summary>
        public static float CalculateEndurance(float fuelKg, float altitudeFt, float speedKts)
        {
            if (fuelKg <= 0f) return 0f;
            float flowRate = GetFuelFlowRate(altitudeFt, speedKts);
            if (flowRate < 0.001f) return float.MaxValue;
            return fuelKg / flowRate;
        }

        /// <summary>
        /// Returns the optimal cruise altitude in feet for a given route distance and
        /// aircraft weight.  Longer, heavier flights benefit from higher altitudes.
        /// </summary>
        public static float CalculateOptimalAltitude(float distanceNm, float weight)
        {
            // Simplified stepped model:
            // Short routes (<300 nm): FL150–FL200
            // Medium routes (300–800 nm): FL250–FL310
            // Long routes (>800 nm): FL350+
            if (distanceNm < 300f)
                return Mathf.Lerp(15000f, 20000f, Mathf.InverseLerp(0f, 300f, distanceNm));
            if (distanceNm < 800f)
                return Mathf.Lerp(25000f, 31000f, Mathf.InverseLerp(300f, 800f, distanceNm));

            // Heavy aircraft need slightly lower ceiling
            float weightFactor = Mathf.Clamp01((weight - 50000f) / 250000f);
            return Mathf.Lerp(41000f, 35000f, weightFactor);
        }

        /// <summary>
        /// Returns the instantaneous fuel flow rate in kg/hour for the given altitude
        /// and indicated airspeed.
        ///
        /// <para>Uses altitude-banded constants from <see cref="FlightPlanConfig"/> and
        /// applies a linear speed-based correction above the reference speed.</para>
        /// </summary>
        public static float GetFuelFlowRate(float altitudeFt, float speedKts)
        {
            float baseFlowKgPerNm = GetBandedBurnRate(altitudeFt);
            float speedMultiplier = 1f + SpeedFuelFactor * Mathf.Max(0f, speedKts - ReferenceSpeedKts);

            // Flow rate (kg/hr) = burn per nm × speed (nm/hr) × speed-correction
            float groundSpeedNmPerHr = Mathf.Max(1f, speedKts);
            return baseFlowKgPerNm * groundSpeedNmPerHr * speedMultiplier;
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        /// <summary>Returns the base fuel burn (kg/nm) for the altitude band.</summary>
        private static float GetBandedBurnRate(float altitudeFt)
        {
            if (altitudeFt >= BandFL350)  return FlightPlanConfig.FuelBurnFL350KgPerNm;
            if (altitudeFt >= BandFL300)
                return Mathf.Lerp(FlightPlanConfig.FuelBurnFL300KgPerNm,
                                  FlightPlanConfig.FuelBurnFL350KgPerNm,
                                  Mathf.InverseLerp(BandFL300, BandFL350, altitudeFt));
            if (altitudeFt >= BandFL200)
                return Mathf.Lerp(FlightPlanConfig.FuelBurnFL200KgPerNm,
                                  FlightPlanConfig.FuelBurnFL300KgPerNm,
                                  Mathf.InverseLerp(BandFL200, BandFL300, altitudeFt));
            if (altitudeFt >= BandFL100)
                return Mathf.Lerp(FlightPlanConfig.FuelBurnFL100KgPerNm,
                                  FlightPlanConfig.FuelBurnFL200KgPerNm,
                                  Mathf.InverseLerp(BandFL100, BandFL200, altitudeFt));

            return Mathf.Lerp(FlightPlanConfig.FuelBurnSeaLevelKgPerNm,
                              FlightPlanConfig.FuelBurnFL100KgPerNm,
                              Mathf.InverseLerp(0f, BandFL100, altitudeFt));
        }
    }
}
