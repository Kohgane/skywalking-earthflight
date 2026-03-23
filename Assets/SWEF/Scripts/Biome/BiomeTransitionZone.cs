// BiomeTransitionZone.cs — SWEF Terrain Detail & Biome System
using UnityEngine;

namespace SWEF.Biome
{
    /// <summary>
    /// MonoBehaviour that detects and manages transition zones between adjacent biomes,
    /// applying noise-based boundary wobble for natural-looking edges.
    /// </summary>
    public class BiomeTransitionZone : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Transition Settings")]
        [Tooltip("Width of the default transition band in metres.")]
        [SerializeField] private float transitionWidthMeters = 5000f;

        [Tooltip("Noise frequency for boundary wobble.")]
        [SerializeField] private float noiseFrequency = 0.0003f;

        [Tooltip("Noise amplitude (metres) for boundary wobble.")]
        [SerializeField] private float noiseAmplitude = 1500f;

        [Tooltip("Scale factor applied when building approximate transition bounds.")]
        [SerializeField] private float boundsPaddingMeters = 10000f;

        #endregion

        #region Public API

        /// <summary>
        /// Returns the 1 or 2 biomes present at the given coordinate.
        /// If the coordinate falls within a transition zone, both adjacent biomes are returned.
        /// </summary>
        /// <param name="lat">Latitude in decimal degrees.</param>
        /// <param name="lon">Longitude in decimal degrees.</param>
        /// <returns>Array of 1–2 biomes at the position.</returns>
        public BiomeType[] GetBiomesAtPosition(double lat, double lon)
        {
            var primary   = BiomeClassifier.ClassifyBiome(lat, lon, 0f);
            var neighbors = BiomeClassifier.GetNeighborBiomes(lat, lon, transitionWidthMeters / 1000f);

            foreach (var neighbor in neighbors)
            {
                if (neighbor == primary) continue;
                float progress = GetTransitionProgress(lat, lon, primary, neighbor);
                if (progress > 0f && progress < 1f)
                    return new[] { primary, neighbor };
            }

            return new[] { primary };
        }

        /// <summary>
        /// Returns how far along the transition the coordinate is from
        /// <paramref name="from"/> to <paramref name="to"/> (0 = fully from, 1 = fully to).
        /// </summary>
        /// <param name="lat">Latitude in decimal degrees.</param>
        /// <param name="lon">Longitude in decimal degrees.</param>
        /// <param name="from">Source biome.</param>
        /// <param name="to">Destination biome.</param>
        /// <returns>Progress 0–1.</returns>
        public float GetTransitionProgress(double lat, double lon, BiomeType from, BiomeType to)
        {
            // Apply noise-based boundary wobble
            float noise = Mathf.PerlinNoise(
                (float)(lon * noiseFrequency * 111000.0),
                (float)(lat * noiseFrequency * 111000.0));
            float wobbleMeters = (noise - 0.5f) * 2f * noiseAmplitude;

            // Compute blend factor, then offset by wobble
            float blend = BiomeClassifier.GetBiomeBlendFactor(lat, lon, from, to);

            // Convert blend to a signed distance and apply wobble shift
            float halfWidth  = transitionWidthMeters * 0.5f;
            float distMeters = (blend - 0.5f) * transitionWidthMeters + wobbleMeters;
            distMeters       = Mathf.Clamp(distMeters, -halfWidth, halfWidth);

            return Mathf.InverseLerp(-halfWidth, halfWidth, distMeters);
        }

        /// <summary>Sets the transition band width in metres.</summary>
        /// <param name="meters">Width in metres (must be &gt; 0).</param>
        public void SetTransitionWidth(float meters)
        {
            transitionWidthMeters = Mathf.Max(1f, meters);
        }

        /// <summary>
        /// Returns an approximate world-space bounding box that encompasses
        /// the transition zone between two biomes.
        /// The returned <see cref="Bounds"/> uses a simplified flat-world model
        /// centred at the origin.
        /// </summary>
        /// <param name="biomeA">First biome.</param>
        /// <param name="biomeB">Second biome.</param>
        /// <returns>Approximate world-space bounds of the transition zone.</returns>
        public Bounds GetTransitionBounds(BiomeType biomeA, BiomeType biomeB)
        {
            // In a full implementation this would query tile metadata.
            // Here we return a centred box scaled by the transition width + padding.
            float halfExtent = (transitionWidthMeters + boundsPaddingMeters) * 0.5f;
            return new Bounds(Vector3.zero, new Vector3(halfExtent * 2f, 5000f, halfExtent * 2f));
        }

        #endregion
    }
}
