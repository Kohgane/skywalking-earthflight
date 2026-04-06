// ApproachBriefing.cs — Phase 120: Precision Landing Challenge System
// Pre-challenge briefing: airport diagram, approach plate, weather brief, recommended speeds.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Pre-challenge approach briefing data provider.
    /// Assembles briefing information including airport diagram references,
    /// approach plate data, weather summary, recommended speeds, and key callouts.
    /// </summary>
    public class ApproachBriefing : MonoBehaviour
    {
        // ── Briefing Data ─────────────────────────────────────────────────────

        [System.Serializable]
        public class BriefingData
        {
            /// <summary>Airport display name.</summary>
            public string AirportName;

            /// <summary>ICAO code.</summary>
            public string ICAO;

            /// <summary>Active runway identifier.</summary>
            public string RunwayId;

            /// <summary>Runway heading (magnetic degrees).</summary>
            public float RunwayHeadingDeg;

            /// <summary>Airport elevation (feet MSL).</summary>
            public float ElevationFeet;

            /// <summary>Glideslope angle (degrees).</summary>
            public float GlideSlopeAngleDeg;

            /// <summary>Recommended Vapp (knots).</summary>
            public float RecommendedVappKnots;

            /// <summary>Recommended Vref (knots).</summary>
            public float RecommendedVrefKnots;

            /// <summary>Weather preset summary string.</summary>
            public string WeatherSummary;

            /// <summary>Wind description.</summary>
            public string WindDescription;

            /// <summary>Notable hazards or key callout notes.</summary>
            public string[] KeyCallouts;

            /// <summary>Path to airport diagram sprite resource (optional).</summary>
            public string DiagramResourcePath;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Weather")]
        [SerializeField] private ChallengeWeatherController weatherController;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Build a briefing for the given challenge definition.
        /// </summary>
        public BriefingData BuildBriefing(ChallengeDefinition challenge)
        {
            if (challenge == null) return null;

            var bd = new BriefingData
            {
                ICAO               = challenge.AirportICAO,
                RunwayId           = challenge.RunwayId,
                WeatherSummary     = challenge.Weather.ToString(),
                GlideSlopeAngleDeg = GetGlideSlopeAngle(challenge.Type),
                KeyCallouts        = GetCallouts(challenge)
            };

            // Speeds: rough defaults (real aircraft integration would supply exact values)
            bd.RecommendedVappKnots = challenge.Type == ChallengeType.CarrierLanding ? 130f : 145f;
            bd.RecommendedVrefKnots = challenge.Type == ChallengeType.CarrierLanding ? 125f : 137f;

            if (weatherController != null)
                bd.WindDescription = $"Weather severity: {weatherController.WeatherSeverity:P0}";

            bd.DiagramResourcePath = $"AirportDiagrams/{challenge.AirportICAO}";

            return bd;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private float GetGlideSlopeAngle(ChallengeType type)
        {
            switch (type)
            {
                case ChallengeType.CarrierLanding:  return 3.5f;
                case ChallengeType.MountainApproach: return 6f;
                default:                            return 3f;
            }
        }

        private string[] GetCallouts(ChallengeDefinition challenge)
        {
            switch (challenge.Type)
            {
                case ChallengeType.CarrierLanding:
                    return new[] { "500 ft — call the ball", "Call the wire after trap", "LSO gives grade on pass" };
                case ChallengeType.MountainApproach:
                    return new[] { "One-way approach — no go-around", "Terrain MSA 10,500 ft", "Speed critical — high-altitude stall margin reduced" };
                case ChallengeType.CrosswindLanding:
                    return new[] { "Use crab-and-kick technique", "Be ready for wind shear below 300 ft", "Hold corrections through roll-out" };
                default:
                    return new[] { "Gear down by 2,000 ft", "Flaps full for landing", "Aim for 1,000 ft touchdown zone" };
            }
        }
    }
}
