// DamageConfig.cs — SWEF Damage & Repair System (Phase 66)
using UnityEngine;

namespace SWEF.Damage
{
    /// <summary>
    /// Compile-time constants shared across the Damage &amp; Repair system.
    ///
    /// <para>Override runtime behaviour by editing <see cref="DamageModel"/> and
    /// <see cref="RepairSystem"/> inspector fields; these constants serve as
    /// sensible defaults.</para>
    /// </summary>
    public static class DamageConfig
    {
        // ── Health Thresholds ─────────────────────────────────────────────────

        /// <summary>Health percentage above which damage level is <see cref="DamageLevel.None"/>.</summary>
        public const float MinorThreshold    = 0.9f;

        /// <summary>Health percentage above which damage level is <see cref="DamageLevel.Minor"/>.</summary>
        public const float ModerateThreshold = 0.7f;

        /// <summary>Health percentage above which damage level is <see cref="DamageLevel.Moderate"/>.</summary>
        public const float SevereThreshold   = 0.5f;

        /// <summary>Health percentage above which damage level is <see cref="DamageLevel.Severe"/>.</summary>
        public const float CriticalThreshold = 0.25f;

        // ── Collision ─────────────────────────────────────────────────────────

        /// <summary>Multiplier applied to relative collision velocity to produce raw damage.</summary>
        public const float CollisionDamageScale = 0.5f;

        // ── Overspeed ─────────────────────────────────────────────────────────

        /// <summary>Damage per second inflicted on the airframe when the aircraft is overspeeding.</summary>
        public const float OverspeedDamageRate = 5f;

        // ── Over-G ────────────────────────────────────────────────────────────

        /// <summary>G-force magnitude above which structural damage begins.</summary>
        public const float OverGDamageThreshold = 8f;

        /// <summary>Damage per second inflicted on the airframe when G-force exceeds <see cref="OverGDamageThreshold"/>.</summary>
        public const float OverGDamageRate = 10f;

        // ── Repair ────────────────────────────────────────────────────────────

        /// <summary>Minimum seconds between successive emergency repairs.</summary>
        public const float EmergencyRepairCooldown = 60f;

        /// <summary>Health points restored to each part by a single emergency repair.</summary>
        public const float EmergencyRepairAmount = 30f;

        /// <summary>Maximum number of emergency repair uses available per flight.</summary>
        public const int MaxEmergencyCharges = 3;

        // ── Engine ────────────────────────────────────────────────────────────

        /// <summary>
        /// Engine health fraction below which the engine is considered stalled/failed.
        /// Expressed as a value in [0, 1].
        /// </summary>
        public const float EngineFailureThreshold = 0.1f;

        // ── Part Importance Weights ───────────────────────────────────────────

        /// <summary>
        /// Returns the relative importance weight for <paramref name="part"/> used when
        /// calculating the weighted overall aircraft health.
        ///
        /// <para>Weights do not need to sum to any particular value; they are
        /// normalised internally by <see cref="DamageModel"/>.</para>
        /// </summary>
        /// <param name="part">Aircraft part to query.</param>
        /// <returns>A positive float weight.</returns>
        public static float GetPartWeight(AircraftPart part)
        {
            switch (part)
            {
                case AircraftPart.Engine:       return 3.0f;
                case AircraftPart.Fuselage:     return 2.5f;
                case AircraftPart.Cockpit:      return 2.0f;
                case AircraftPart.LeftWing:     return 2.0f;
                case AircraftPart.RightWing:    return 2.0f;
                case AircraftPart.Tail:         return 1.5f;
                case AircraftPart.Elevator:     return 1.5f;
                case AircraftPart.Rudder:       return 1.0f;
                case AircraftPart.LeftAileron:  return 1.0f;
                case AircraftPart.RightAileron: return 1.0f;
                case AircraftPart.LandingGear:  return 0.5f;
                default:                        return 1.0f;
            }
        }

        // ── UI Colors ─────────────────────────────────────────────────────────

        /// <summary>Indicator color for <see cref="DamageLevel.None"/> or <see cref="DamageLevel.Minor"/>.</summary>
        public static readonly Color ColorHealthy  = new Color(0.20f, 0.85f, 0.20f, 1f);

        /// <summary>Indicator color for <see cref="DamageLevel.Moderate"/>.</summary>
        public static readonly Color ColorModerate = new Color(1.00f, 0.85f, 0.00f, 1f);

        /// <summary>Indicator color for <see cref="DamageLevel.Severe"/>.</summary>
        public static readonly Color ColorSevere   = new Color(1.00f, 0.50f, 0.00f, 1f);

        /// <summary>Indicator color for <see cref="DamageLevel.Critical"/>.</summary>
        public static readonly Color ColorCritical = new Color(1.00f, 0.10f, 0.10f, 1f);

        /// <summary>Indicator color for <see cref="DamageLevel.Destroyed"/>.</summary>
        public static readonly Color ColorDestroyed = new Color(0.05f, 0.05f, 0.05f, 1f);

        /// <summary>Returns the UI color that corresponds to <paramref name="level"/>.</summary>
        /// <param name="level">Damage level to look up.</param>
        /// <returns>A <see cref="Color"/> value.</returns>
        public static Color GetLevelColor(DamageLevel level)
        {
            switch (level)
            {
                case DamageLevel.None:
                case DamageLevel.Minor:     return ColorHealthy;
                case DamageLevel.Moderate:  return ColorModerate;
                case DamageLevel.Severe:    return ColorSevere;
                case DamageLevel.Critical:  return ColorCritical;
                case DamageLevel.Destroyed: return ColorDestroyed;
                default:                    return ColorHealthy;
            }
        }
    }
}
