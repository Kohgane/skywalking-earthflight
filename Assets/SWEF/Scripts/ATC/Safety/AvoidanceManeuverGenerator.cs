// AvoidanceManeuverGenerator.cs — Phase 119: Advanced AI Traffic Control
// Avoidance maneuvers: climb/descend recommendations, turn advisories,
// speed adjustments.
// Namespace: SWEF.ATC

using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Generates recommended avoidance maneuvers for an aircraft
    /// faced with a TCAS RA or ATC-detected conflict.
    /// </summary>
    public class AvoidanceManeuverGenerator : MonoBehaviour
    {
        // ── Maneuver ──────────────────────────────────────────────────────────────

        /// <summary>A recommended avoidance maneuver.</summary>
        public class AvoidanceManeuver
        {
            /// <summary>Type of maneuver to execute.</summary>
            public ManeuverType type;
            /// <summary>Target altitude change (ft, positive = climb).</summary>
            public float altitudeChangeFt;
            /// <summary>Target heading change (degrees, positive = right).</summary>
            public float headingChangeDeg;
            /// <summary>Target speed change (knots, negative = slow).</summary>
            public float speedChangeKts;
            /// <summary>Urgency of the maneuver (0 = advisory, 1 = immediate).</summary>
            public float urgency;
        }

        /// <summary>Classification of avoidance maneuver.</summary>
        public enum ManeuverType
        {
            /// <summary>Climb to increase vertical separation.</summary>
            Climb,
            /// <summary>Descend to increase vertical separation.</summary>
            Descend,
            /// <summary>Turn to increase lateral separation.</summary>
            Turn,
            /// <summary>Reduce speed to increase longitudinal spacing.</summary>
            SpeedReduction,
            /// <summary>Increase speed to pass through the conflict point sooner.</summary>
            SpeedIncrease,
            /// <summary>Combined climb and turn.</summary>
            ClimbAndTurn,
            /// <summary>Combined descent and turn.</summary>
            DescendAndTurn
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates an avoidance maneuver for the ownship given a TCAS advisory.
        /// </summary>
        public AvoidanceManeuver GenerateFromTCAS(TCASAdvisory advisory, float currentAlt)
        {
            switch (advisory)
            {
                case TCASAdvisory.RA_Climb:
                    return new AvoidanceManeuver
                    {
                        type              = ManeuverType.Climb,
                        altitudeChangeFt  = 1500f,
                        urgency           = 1f
                    };
                case TCASAdvisory.RA_Descend:
                    return new AvoidanceManeuver
                    {
                        type              = ManeuverType.Descend,
                        altitudeChangeFt  = -1500f,
                        urgency           = 1f
                    };
                case TCASAdvisory.TA:
                    return new AvoidanceManeuver
                    {
                        type    = ManeuverType.Turn,
                        headingChangeDeg = 15f,
                        urgency = 0.5f
                    };
                default:
                    return null;
            }
        }

        /// <summary>
        /// Generates an avoidance maneuver based on a conflict alert.
        /// </summary>
        public AvoidanceManeuver GenerateFromConflict(ConflictAlert alert, float ownAlt, float intruderAlt)
        {
            if (alert == null) return null;

            float urgency = alert.severity switch
            {
                ConflictSeverity.Critical  => 1.0f,
                ConflictSeverity.Warning   => 0.8f,
                ConflictSeverity.Caution   => 0.5f,
                _                          => 0.2f
            };

            // If vertical separation is the better option
            if (ownAlt < intruderAlt)
                return new AvoidanceManeuver
                {
                    type = ManeuverType.Descend,
                    altitudeChangeFt = -1000f,
                    urgency = urgency
                };

            return new AvoidanceManeuver
            {
                type = ManeuverType.Climb,
                altitudeChangeFt = 1000f,
                urgency = urgency
            };
        }
    }
}
