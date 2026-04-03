// PerformanceSimulator.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// Stat delta returned by <see cref="PerformanceSimulator.CompareBuilds"/>.
    /// Each field is <c>buildB value − buildA value</c> (positive = B is better).
    /// </summary>
    public struct BuildComparison
    {
        /// <summary>Speed delta in km/h (positive means B is faster).</summary>
        public float maxSpeedDelta;
        /// <summary>Climb-rate delta in m/s.</summary>
        public float climbRateDelta;
        /// <summary>Maneuverability score delta [−1, 1].</summary>
        public float maneuverabilityDelta;
        /// <summary>Fuel-efficiency score delta [−1, 1].</summary>
        public float fuelEfficiencyDelta;
        /// <summary>Structural integrity delta (durability units).</summary>
        public float structuralIntegrityDelta;
        /// <summary>Weight-balance score delta [−1, 1].</summary>
        public float weightBalanceDelta;
    }

    /// <summary>
    /// Static utility that computes aggregate flight performance characteristics
    /// from an <see cref="AircraftBuildData"/> and its constituent
    /// <see cref="AircraftPartData"/> records.
    ///
    /// <para>
    /// All formulas are intentionally lightweight arcade approximations rather than
    /// full aerodynamic simulations.  They are designed to produce meaningful relative
    /// comparisons between builds while remaining efficient inside the Workshop UI.
    /// </para>
    /// </summary>
    public static class PerformanceSimulator
    {
        // ── Baseline constants ─────────────────────────────────────────────────
        private const float BaseSpeed          = 200f;   // km/h
        private const float BaseClimbRate      =   5f;   // m/s
        private const float BaseDrag           =   0.3f;
        private const float BaseWeight         = 500f;   // kg
        private const float BaseThrust         = 800f;   // N·s (arbitrary)
        private const float MaxManeuver        =   1f;
        private const float MaxFuelEfficiency  =   1f;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the maximum achievable speed (km/h) for the supplied build.
        /// </summary>
        /// <remarks>
        /// Formula: <c>BaseSpeed × (totalThrust / totalDrag) × weightPenalty</c>
        /// where <c>weightPenalty = BaseWeight / totalWeight</c>.
        /// </remarks>
        /// <param name="build">The build to evaluate.</param>
        /// <param name="inventory">Part inventory used to look up equipped parts.</param>
        /// <returns>Maximum speed in km/h.</returns>
        public static float ComputeMaxSpeed(AircraftBuildData build, PartInventoryController inventory)
        {
            if (build == null || inventory == null) return BaseSpeed;

            var parts      = GetEquippedParts(build, inventory);
            float thrust   = AggregateThrust(parts);
            float drag     = AggregateDrag(parts);
            float weight   = AggregateWeight(parts);
            float penalty  = weight > 0f ? BaseWeight / weight : 1f;

            return BaseSpeed * (thrust / Mathf.Max(drag, 0.01f)) * penalty;
        }

        /// <summary>
        /// Computes the maximum climb rate (m/s) for the supplied build.
        /// </summary>
        /// <remarks>
        /// Formula: <c>BaseClimbRate × (thrust / weight) × liftBonus</c>
        /// </remarks>
        /// <param name="build">The build to evaluate.</param>
        /// <param name="inventory">Part inventory used to look up equipped parts.</param>
        /// <returns>Climb rate in m/s.</returns>
        public static float ComputeClimbRate(AircraftBuildData build, PartInventoryController inventory)
        {
            if (build == null || inventory == null) return BaseClimbRate;

            var parts     = GetEquippedParts(build, inventory);
            float thrust  = AggregateThrust(parts);
            float weight  = AggregateWeight(parts);
            float lift    = AggregateLift(parts);

            return BaseClimbRate * (thrust / Mathf.Max(weight, 1f)) * lift;
        }

        /// <summary>
        /// Computes a normalised maneuverability score [0, 1].
        /// </summary>
        /// <remarks>
        /// Derived from wing, aileron, and rudder/elevator contributions relative
        /// to total aircraft weight.
        /// </remarks>
        /// <param name="build">The build to evaluate.</param>
        /// <param name="inventory">Part inventory used to look up equipped parts.</param>
        /// <returns>Maneuverability score in the range [0, 1].</returns>
        public static float ComputeManeuverability(AircraftBuildData build, PartInventoryController inventory)
        {
            if (build == null || inventory == null) return 0.5f;

            var parts           = GetEquippedParts(build, inventory);
            float controlScore  = 0f;
            float weight        = AggregateWeight(parts);

            foreach (var p in parts)
            {
                switch (p.partType)
                {
                    case AircraftPartType.Wing:
                    case AircraftPartType.Aileron:
                    case AircraftPartType.Rudder:
                    case AircraftPartType.Elevator:
                        controlScore += p.liftModifier * (1f - p.dragCoefficient);
                        break;
                }
            }

            return Mathf.Clamp01(controlScore / Mathf.Max(weight / BaseWeight, 0.01f) * 0.5f);
        }

        /// <summary>
        /// Computes a normalised fuel-efficiency score [0, 1].
        /// </summary>
        /// <remarks>
        /// Engines with lower thrust modifiers burn less fuel per unit thrust;
        /// additional FuelTank parts extend total capacity.
        /// Score = <c>tankCapacityRatio / thrustRatio</c> clamped to [0, 1].
        /// </remarks>
        /// <param name="build">The build to evaluate.</param>
        /// <param name="inventory">Part inventory used to look up equipped parts.</param>
        /// <returns>Fuel-efficiency score in [0, 1].</returns>
        public static float ComputeFuelEfficiency(AircraftBuildData build, PartInventoryController inventory)
        {
            if (build == null || inventory == null) return 0.5f;

            var parts         = GetEquippedParts(build, inventory);
            float tankBonus   = 1f;
            float thrustRatio = 1f;

            foreach (var p in parts)
            {
                if (p.partType == AircraftPartType.FuelTank)
                    tankBonus += 0.2f;

                if (p.partType == AircraftPartType.Engine)
                    thrustRatio *= Mathf.Max(p.thrustModifier, 0.1f);
            }

            return Mathf.Clamp01(tankBonus / thrustRatio);
        }

        /// <summary>
        /// Computes the total structural-integrity score (sum of all part durability values).
        /// </summary>
        /// <param name="build">The build to evaluate.</param>
        /// <param name="inventory">Part inventory used to look up equipped parts.</param>
        /// <returns>Total durability in arbitrary units.</returns>
        public static float ComputeStructuralIntegrity(AircraftBuildData build, PartInventoryController inventory)
        {
            if (build == null || inventory == null) return 0f;

            float total = 0f;
            foreach (var p in GetEquippedParts(build, inventory))
                total += p.durability;

            return total;
        }

        /// <summary>
        /// Computes a centre-of-gravity balance score [0, 1].
        /// </summary>
        /// <remarks>
        /// Estimates the CG position as a ratio of total nose-section weight
        /// (Engine + Cockpit + Intake) to tail-section weight
        /// (Tail + Elevator + Rudder + Exhaust).  A balanced ratio → score 1.
        /// </remarks>
        /// <param name="build">The build to evaluate.</param>
        /// <param name="inventory">Part inventory used to look up equipped parts.</param>
        /// <returns>Balance score in [0, 1]; 1 = perfect balance.</returns>
        public static float ComputeWeightBalance(AircraftBuildData build, PartInventoryController inventory)
        {
            if (build == null || inventory == null) return 0.5f;

            var parts       = GetEquippedParts(build, inventory);
            float noseWeight = 0f;
            float tailWeight = 0f;

            foreach (var p in parts)
            {
                switch (p.partType)
                {
                    case AircraftPartType.Engine:
                    case AircraftPartType.Cockpit:
                    case AircraftPartType.Intake:
                    case AircraftPartType.Propeller:
                        noseWeight += p.weight;
                        break;

                    case AircraftPartType.Tail:
                    case AircraftPartType.Elevator:
                    case AircraftPartType.Rudder:
                    case AircraftPartType.Exhaust:
                        tailWeight += p.weight;
                        break;
                }
            }

            float total = noseWeight + tailWeight;
            if (total < 0.01f) return 0.5f;

            float ratio = noseWeight / total;
            // Ideal nose ratio ~0.4–0.6 maps to score 1; deviations taper to 0.
            return 1f - Mathf.Clamp01(Mathf.Abs(ratio - 0.5f) * 4f);
        }

        /// <summary>
        /// Returns a <see cref="BuildComparison"/> struct containing the stat
        /// delta between two builds (<c>buildB − buildA</c>).
        /// Positive values indicate that <paramref name="buildB"/> is superior.
        /// </summary>
        /// <param name="buildA">Baseline build.</param>
        /// <param name="buildB">Comparison build.</param>
        /// <param name="inventory">Shared part inventory.</param>
        /// <returns>Per-stat deltas for UI display.</returns>
        public static BuildComparison CompareBuilds(
            AircraftBuildData buildA,
            AircraftBuildData buildB,
            PartInventoryController inventory)
        {
            return new BuildComparison
            {
                maxSpeedDelta          = ComputeMaxSpeed(buildB, inventory)           - ComputeMaxSpeed(buildA, inventory),
                climbRateDelta         = ComputeClimbRate(buildB, inventory)          - ComputeClimbRate(buildA, inventory),
                maneuverabilityDelta   = ComputeManeuverability(buildB, inventory)    - ComputeManeuverability(buildA, inventory),
                fuelEfficiencyDelta    = ComputeFuelEfficiency(buildB, inventory)     - ComputeFuelEfficiency(buildA, inventory),
                structuralIntegrityDelta = ComputeStructuralIntegrity(buildB, inventory) - ComputeStructuralIntegrity(buildA, inventory),
                weightBalanceDelta     = ComputeWeightBalance(buildB, inventory)      - ComputeWeightBalance(buildA, inventory)
            };
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static List<AircraftPartData> GetEquippedParts(
            AircraftBuildData build,
            PartInventoryController inventory)
        {
            var result = new List<AircraftPartData>();
            if (build?.equippedPartIds == null) return result;

            foreach (var id in build.equippedPartIds)
            {
                var part = inventory.GetPartById(id);
                if (part != null) result.Add(part);
            }
            return result;
        }

        private static float AggregateThrust(List<AircraftPartData> parts)
        {
            float t = 1f;
            foreach (var p in parts)
                if (p.partType == AircraftPartType.Engine) t *= p.thrustModifier;
            return t * BaseThrust;
        }

        private static float AggregateDrag(List<AircraftPartData> parts)
        {
            float d = BaseDrag;
            foreach (var p in parts) d += p.dragCoefficient;
            return d;
        }

        private static float AggregateWeight(List<AircraftPartData> parts)
        {
            float w = BaseWeight;
            foreach (var p in parts) w += p.weight;
            return w;
        }

        private static float AggregateLift(List<AircraftPartData> parts)
        {
            float l = 1f;
            foreach (var p in parts)
                if (p.partType == AircraftPartType.Wing) l *= p.liftModifier;
            return l;
        }
    }
}
