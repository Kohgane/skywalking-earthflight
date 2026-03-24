// FuelConsumptionModel.cs — SWEF Fuel & Energy Management System (Phase 69)
using UnityEngine;

namespace SWEF.Fuel
{
    /// <summary>
    /// Phase 69 — ScriptableObject that defines how quickly fuel is consumed based on
    /// throttle position, altitude, and airspeed.
    ///
    /// <para>Create instances via <c>Assets → Create → SWEF → Fuel → Fuel Consumption Model</c>
    /// and assign them to <see cref="FuelManager"/>.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Fuel/Fuel Consumption Model", fileName = "NewFuelConsumptionModel")]
    public class FuelConsumptionModel : ScriptableObject
    {
        // ── Consumption Curves ────────────────────────────────────────────────

        [Header("Consumption Curves")]
        [Tooltip("Fuel consumption rate (litres/s) as a multiplier of baseConsumptionRate, " +
                 "sampled by throttle position (0 = idle, 1 = full throttle).")]
        public AnimationCurve throttleConsumptionCurve = AnimationCurve.Linear(0f, 0.15f, 1f, 1f);

        [Tooltip("Efficiency multiplier sampled by altitude in metres. " +
                 "Higher altitude → thinner air → lower fuel burn (values < 1 reduce consumption).")]
        public AnimationCurve altitudeEfficiencyCurve = new AnimationCurve(
            new Keyframe(0f,      1.00f),
            new Keyframe(5000f,   0.90f),
            new Keyframe(10000f,  0.75f),
            new Keyframe(15000f,  0.60f),
            new Keyframe(20000f,  0.45f));

        [Tooltip("Efficiency multiplier sampled by airspeed in m/s. " +
                 "Cruise speed has the best efficiency (value = 1); very high or very low speeds cost more.")]
        public AnimationCurve speedEfficiencyCurve = new AnimationCurve(
            new Keyframe(0f,   1.10f),
            new Keyframe(100f, 0.95f),
            new Keyframe(200f, 0.90f),
            new Keyframe(300f, 1.00f),
            new Keyframe(500f, 1.30f));

        // ── Base Rates ────────────────────────────────────────────────────────

        [Header("Base Rates")]
        [Tooltip("Litres per second consumed at full throttle, sea level, optimal cruise speed.")]
        public float baseConsumptionRate = FuelConfig.BaseConsumptionRate;

        [Tooltip("Minimum litres per second consumed when the engine is at idle throttle.")]
        public float idleConsumptionRate = FuelConfig.IdleConsumptionRate;

        // ── Modifiers ─────────────────────────────────────────────────────────

        [Header("Modifiers")]
        [Tooltip("Additional consumption multiplier applied on top of the throttle curve when " +
                 "afterburner or boost is active.")]
        public float afterburnerMultiplier = FuelConfig.AfterburnerMultiplier;

        [Tooltip("Consumption multiplier applied during engine warm-up (cold-start penalty). " +
                 "Values > 1 increase fuel burn until the engine reaches operating temperature.")]
        public float coldStartPenalty = 1.5f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Calculates the instantaneous fuel consumption rate in litres per second.
        /// </summary>
        /// <param name="throttle">Throttle position in [0, 1].</param>
        /// <param name="altitude">Aircraft altitude in metres above sea level.</param>
        /// <param name="speed">Airspeed in metres per second.</param>
        /// <param name="afterburner"><c>true</c> if afterburner or boost is currently active.</param>
        /// <returns>Litres per second to deduct from the active tank.</returns>
        public float CalculateConsumption(float throttle, float altitude, float speed, bool afterburner)
        {
            // Sample the throttle curve; ensure idle minimum is always respected.
            float throttleFactor = throttleConsumptionCurve.Evaluate(Mathf.Clamp01(throttle));
            float baseRate = Mathf.Max(idleConsumptionRate,
                                       baseConsumptionRate * throttleFactor);

            // Apply altitude and speed efficiency multipliers.
            float altMult   = altitudeEfficiencyCurve.Evaluate(Mathf.Max(0f, altitude));
            float speedMult = speedEfficiencyCurve.Evaluate(Mathf.Max(0f, speed));

            float rate = baseRate * altMult * speedMult;

            // Apply afterburner surcharge.
            if (afterburner)
                rate *= afterburnerMultiplier;

            return Mathf.Max(0f, rate);
        }
    }
}
