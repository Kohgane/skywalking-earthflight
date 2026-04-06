// DebrisGenerator.cs — Phase 114: Satellite & Space Debris Tracking
// Procedural debris generation: size distribution, tumble animation, albedo variation.
// Namespace: SWEF.SatelliteTracking

using System;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Generates procedural <see cref="DebrisObject"/> instances using a probabilistic
    /// size distribution and random orbital parameters sampled from the debris density model.
    /// </summary>
    public class DebrisGenerator : MonoBehaviour
    {
        // ── Size distribution probabilities (must sum to 1) ───────────────────────
        [Header("Size Distribution")]
        [Tooltip("Probability of generating a Large debris fragment.")]
        [Range(0f, 1f)]
        [SerializeField] private float probLarge = 0.01f;

        [Tooltip("Probability of generating a Medium debris fragment.")]
        [Range(0f, 1f)]
        [SerializeField] private float probMedium = 0.09f;

        [Tooltip("Probability of generating a Small debris fragment.")]
        [Range(0f, 1f)]
        [SerializeField] private float probSmall = 0.30f;

        // Micro = 1 - others
        [Header("Physical Properties")]
        [Tooltip("Typical tumble rate range (degrees/second).")]
        [SerializeField] private Vector2 tumbleRateRange = new Vector2(0.5f, 20f);

        [Tooltip("Albedo range [0..1] for visual rendering.")]
        [SerializeField] private Vector2 albedoRange = new Vector2(0.02f, 0.3f);

        private static readonly string[] OriginEvents =
        {
            "Fengyun-1C breakup (2007)",
            "Cosmos 2251 collision (2009)",
            "Iridium 33 collision (2009)",
            "SL-16 rocket body",
            "ASAT test fragment",
            "Upper stage",
            "Solar panel fragment",
            "Unknown"
        };

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a single <see cref="DebrisObject"/> sampled from the density model.
        /// </summary>
        public DebrisObject GenerateOne(int id, SpaceDebrisManager densityModel)
        {
            // Sample altitude using density model (rejection sampling)
            float altKm = SampleAltitude(densityModel);

            // Sample size
            DebrisSize size = SampleSize();

            // Cross-section based on size
            float cs = size switch
            {
                DebrisSize.Large  => UnityEngine.Random.Range(0.05f, 1.0f),
                DebrisSize.Medium => UnityEngine.Random.Range(0.001f, 0.05f),
                DebrisSize.Small  => UnityEngine.Random.Range(1e-5f, 0.001f),
                _                 => UnityEngine.Random.Range(1e-8f, 1e-5f)
            };

            // Random ECI position on a sphere at altKm
            float r   = (6371f + altKm) ; // km from Earth centre
            var dir   = UnityEngine.Random.onUnitSphere;
            var posECI = dir * r;

            // Random velocity (approximate LEO orbital speed ~7.8 km/s)
            float v = 7.8f + UnityEngine.Random.Range(-0.5f, 0.5f);
            var perpDir = Vector3.Cross(dir, UnityEngine.Random.onUnitSphere).normalized;
            var velECI  = perpDir * v;

            return new DebrisObject
            {
                debrisId           = id,
                size               = size,
                noradId            = 0,
                crossSectionM2     = cs,
                tumbleRateDegPerSec = UnityEngine.Random.Range(tumbleRateRange.x, tumbleRateRange.y),
                albedo             = UnityEngine.Random.Range(albedoRange.x, albedoRange.y),
                positionECI        = posECI,
                velocityECI        = velECI,
                altitudeKm         = altKm,
                originEvent        = OriginEvents[UnityEngine.Random.Range(0, OriginEvents.Length)]
            };
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private float SampleAltitude(SpaceDebrisManager model)
        {
            // Simple rejection sampling in 200–2000 km band
            float maxDensity = 1.5f;
            for (int attempts = 0; attempts < 100; attempts++)
            {
                float alt = UnityEngine.Random.Range(200f, 2000f);
                float density = model != null ? model.GetDensityAtAltitude(alt) : 1f;
                if (UnityEngine.Random.value < density / maxDensity)
                    return alt;
            }
            return UnityEngine.Random.Range(200f, 2000f);
        }

        private DebrisSize SampleSize()
        {
            float r = UnityEngine.Random.value;
            if (r < probLarge)                          return DebrisSize.Large;
            if (r < probLarge + probMedium)             return DebrisSize.Medium;
            if (r < probLarge + probMedium + probSmall) return DebrisSize.Small;
            return DebrisSize.Micro;
        }
    }
}
