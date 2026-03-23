// VegetationPlacementHints.cs — SWEF Terrain Detail & Biome System
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Biome
{
    /// <summary>
    /// MonoBehaviour that generates vegetation placement hints based on
    /// biome type, altitude, slope, and latitude.
    /// </summary>
    public class VegetationPlacementHints : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Treeline Settings")]
        [Tooltip("Altitude (metres) of the global treeline at the equator.")]
        [SerializeField] private float equatorialTreelineAltitude = 4000f;

        [Tooltip("Altitude (metres) of the treeline at high latitudes (≥ 60°).")]
        [SerializeField] private float highLatitudeTreelineAltitude = 800f;

        [Header("Density Defaults")]
        [Tooltip("Maximum number of scatter points returned by GeneratePlacementPoints.")]
        [SerializeField] private int maxDefaultPoints = 512;

        [Header("Slope Limits")]
        [Tooltip("Maximum slope angle (degrees) at which any vegetation can grow.")]
        [SerializeField] private float absoluteMaxSlopeDeg = 55f;

        #endregion

        #region Public API

        /// <summary>
        /// Returns vegetation placement hints for a specific world position.
        /// </summary>
        /// <param name="lat">Latitude in decimal degrees.</param>
        /// <param name="lon">Longitude in decimal degrees.</param>
        /// <param name="altitudeM">Altitude above sea level in metres.</param>
        /// <param name="slopeDeg">Terrain slope at the position in degrees.</param>
        /// <returns>Populated <see cref="VegetationHint"/> for this position.</returns>
        public VegetationHint GetHintsForPosition(double lat, double lon, float altitudeM, float slopeDeg)
        {
            var biome      = BiomeClassifier.ClassifyBiome(lat, lon, altitudeM);
            float treeline = GetTreelineAltitude(lat);
            bool canGrow   = ShouldPlaceVegetation(altitudeM, slopeDeg, biome);

            return new VegetationHint
            {
                density             = canGrow ? GetBiomeDensity(biome) : 0f,
                treeLineAltitude    = treeline,
                undergrowthDensity  = canGrow ? GetUndergrowthDensity(biome) : 0f,
                canopyHeightRange   = GetCanopyHeightRange(biome),
                dominantSpeciesTags = GetSpeciesTags(biome)
            };
        }

        /// <summary>
        /// Calculates the approximate treeline altitude (metres) for a given latitude.
        /// Treeline decreases linearly from equatorial to high-latitude values.
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees.</param>
        /// <returns>Treeline altitude in metres.</returns>
        public float GetTreelineAltitude(double latitude)
        {
            float absLat = (float)System.Math.Abs(latitude);
            float t      = Mathf.Clamp01(absLat / 60f);
            return Mathf.Lerp(equatorialTreelineAltitude, highLatitudeTreelineAltitude, t);
        }

        /// <summary>
        /// Generates a list of world-space vegetation placement points within an area.
        /// </summary>
        /// <param name="area">World-space bounding box to scatter within.</param>
        /// <param name="biome">Target biome type.</param>
        /// <param name="density">Normalised density 0–1.</param>
        /// <param name="maxPoints">Maximum number of points to generate.</param>
        /// <returns>List of placement positions.</returns>
        public List<Vector3> GeneratePlacementPoints(Bounds area, BiomeType biome, float density, int maxPoints)
        {
            int   count  = Mathf.Clamp(Mathf.RoundToInt(maxPoints * density), 0, maxDefaultPoints);
            var   points = new List<Vector3>(count);
            float jitter = area.extents.x * 0.1f; // small position jitter for natural look

            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(area.min.x, area.max.x);
                float z = Random.Range(area.min.z, area.max.z);
                float y = area.center.y;

                // Apply cluster or scatter pattern depending on biome
                if (IsClusteringBiome(biome))
                {
                    x += Random.Range(-jitter, jitter);
                    z += Random.Range(-jitter, jitter);
                }

                points.Add(new Vector3(x, y, z));
            }

            return points;
        }

        /// <summary>
        /// Quick check — returns whether vegetation should be placed at the given position.
        /// </summary>
        /// <param name="altitude">Altitude above sea level in metres.</param>
        /// <param name="slope">Terrain slope in degrees.</param>
        /// <param name="biome">Biome at the position.</param>
        /// <returns><c>true</c> if vegetation placement is appropriate.</returns>
        public bool ShouldPlaceVegetation(float altitude, float slope, BiomeType biome)
        {
            if (slope > absoluteMaxSlopeDeg) return false;

            switch (biome)
            {
                case BiomeType.Ocean:
                case BiomeType.Arctic:
                case BiomeType.Volcanic:
                    return false;
                case BiomeType.Desert:
                    return altitude < 1000f && slope < 20f;
                case BiomeType.Mountain:
                    // Use the equatorial treeline as a conservative upper bound
                    // since this method does not receive latitude. Callers that have
                    // latitude available should call GetHintsForPosition instead.
                    return altitude < equatorialTreelineAltitude && slope < 40f;
                default:
                    return true;
            }
        }

        #endregion

        #region Private Helpers

        private float GetBiomeDensity(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Rainforest => 1.0f,
                BiomeType.Tropical   => 0.85f,
                BiomeType.Temperate  => 0.75f,
                BiomeType.Boreal     => 0.70f,
                BiomeType.Wetland    => 0.65f,
                BiomeType.Savanna    => 0.40f,
                BiomeType.Steppe     => 0.25f,
                BiomeType.Tundra     => 0.15f,
                BiomeType.Desert     => 0.05f,
                BiomeType.Mountain   => 0.30f,
                BiomeType.Coastal    => 0.45f,
                _                    => 0.10f
            };
        }

        private float GetUndergrowthDensity(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Rainforest => 0.90f,
                BiomeType.Temperate  => 0.60f,
                BiomeType.Wetland    => 0.75f,
                BiomeType.Boreal     => 0.40f,
                BiomeType.Savanna    => 0.55f,
                BiomeType.Desert     => 0.03f,
                _                    => 0.30f
            };
        }

        private Vector2 GetCanopyHeightRange(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Rainforest => new Vector2(20f, 50f),
                BiomeType.Tropical   => new Vector2(10f, 35f),
                BiomeType.Temperate  => new Vector2(8f,  25f),
                BiomeType.Boreal     => new Vector2(5f,  20f),
                BiomeType.Savanna    => new Vector2(4f,  15f),
                BiomeType.Steppe     => new Vector2(0.5f, 3f),
                BiomeType.Tundra     => new Vector2(0.1f, 1f),
                _                    => new Vector2(1f,   5f)
            };
        }

        private string[] GetSpeciesTags(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Rainforest => new[] { "mahogany", "fern", "bromeliad", "palm" },
                BiomeType.Tropical   => new[] { "palm", "bamboo", "banana" },
                BiomeType.Temperate  => new[] { "oak", "beech", "maple", "fern" },
                BiomeType.Boreal     => new[] { "pine", "spruce", "birch" },
                BiomeType.Savanna    => new[] { "acacia", "baobab", "grass" },
                BiomeType.Desert     => new[] { "cactus", "scrub", "succulent" },
                BiomeType.Tundra     => new[] { "lichen", "moss", "sedge" },
                BiomeType.Wetland    => new[] { "reed", "willow", "cattail", "mangrove" },
                BiomeType.Mountain   => new[] { "alpine-grass", "dwarf-pine", "rock-plant" },
                _                    => new[] { "grass" }
            };
        }

        private static bool IsClusteringBiome(BiomeType biome)
        {
            return biome == BiomeType.Boreal
                || biome == BiomeType.Temperate
                || biome == BiomeType.Rainforest;
        }

        #endregion
    }
}
