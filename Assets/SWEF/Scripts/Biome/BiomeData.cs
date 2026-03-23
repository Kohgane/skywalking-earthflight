// BiomeData.cs — SWEF Terrain Detail & Biome System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Biome
{
    #region Enumerations

    /// <summary>All supported terrain biome categories.</summary>
    public enum BiomeType
    {
        /// <summary>Hot, arid sandy/rocky terrain.</summary>
        Desert,
        /// <summary>Hot, humid equatorial forest.</summary>
        Tropical,
        /// <summary>Mild mid-latitude mixed forest.</summary>
        Temperate,
        /// <summary>Cold northern coniferous forest (taiga).</summary>
        Boreal,
        /// <summary>Treeless arctic/alpine plain.</summary>
        Tundra,
        /// <summary>High-altitude rocky terrain above treeline.</summary>
        Mountain,
        /// <summary>Open ocean environment.</summary>
        Ocean,
        /// <summary>Coastal beach and shoreline zone.</summary>
        Coastal,
        /// <summary>Marsh, swamp, or bog terrain.</summary>
        Wetland,
        /// <summary>Dense urban/metropolitan environment.</summary>
        Urban,
        /// <summary>Polar ice cap and frozen tundra.</summary>
        Arctic,
        /// <summary>Lava fields, calderas, and volcanic terrain.</summary>
        Volcanic,
        /// <summary>Tropical grassland with scattered trees.</summary>
        Savanna,
        /// <summary>Dense tropical rainforest with high canopy.</summary>
        Rainforest,
        /// <summary>Temperate grassland / semi-arid plain.</summary>
        Steppe
    }

    /// <summary>Blend style applied at biome boundary transitions.</summary>
    public enum BiomeTransitionType
    {
        /// <summary>Abrupt hard-edge boundary.</summary>
        Sharp,
        /// <summary>Narrow gradient blend (≤ 2 km wide).</summary>
        GradientSmall,
        /// <summary>Wide gradient blend (up to 20 km).</summary>
        GradientLarge,
        /// <summary>Noise-scattered interlocking patches.</summary>
        Scattered
    }

    /// <summary>Curve type applied when blending transition weights.</summary>
    public enum BlendCurveType
    {
        /// <summary>Linear interpolation across the transition band.</summary>
        Linear,
        /// <summary>Smooth-step (ease-in/ease-out) interpolation.</summary>
        Smooth,
        /// <summary>Hard step (threshold-based) transition.</summary>
        Step
    }

    #endregion

    #region Configuration Structs

    /// <summary>
    /// Per-biome visual, audio, and climate configuration.
    /// </summary>
    [Serializable]
    public struct BiomeConfig
    {
        /// <summary>Biome this configuration applies to.</summary>
        [Tooltip("Biome this configuration applies to.")]
        public BiomeType biomeType;

        /// <summary>Colour tint applied to the environment in this biome.</summary>
        [Tooltip("Colour tint applied to the environment.")]
        public Color colorTint;

        /// <summary>Atmospheric fog colour for this biome.</summary>
        [Tooltip("Atmospheric fog colour.")]
        public Color fogColor;

        /// <summary>Minimum and maximum fog density (x = min, y = max).</summary>
        [Tooltip("Fog density range (x = min, y = max).")]
        public Vector2 fogDensityRange;

        /// <summary>Identifier of the ambient sound asset associated with this biome.</summary>
        [Tooltip("Ambient sound asset ID.")]
        public string ambientSoundId;

        /// <summary>Minimum and maximum wind intensity (x = min, y = max), in m/s.</summary>
        [Tooltip("Wind intensity range in m/s (x = min, y = max).")]
        public Vector2 windIntensityRange;

        /// <summary>Minimum and maximum surface temperature in Celsius (x = min, y = max).</summary>
        [Tooltip("Surface temperature range in °C (x = min, y = max).")]
        public Vector2 temperatureRangeCelsius;

        /// <summary>Minimum and maximum relative humidity 0–1 (x = min, y = max).</summary>
        [Tooltip("Relative humidity range 0–1 (x = min, y = max).")]
        public Vector2 humidityRange;
    }

    /// <summary>
    /// Configuration for the transition zone between two adjacent biomes.
    /// </summary>
    [Serializable]
    public struct BiomeTransitionConfig
    {
        /// <summary>Width of the transition band in metres.</summary>
        [Tooltip("Width of the transition band in metres.")]
        public float transitionWidthMeters;

        /// <summary>Interpolation curve used inside the transition band.</summary>
        [Tooltip("Blend curve type for the transition.")]
        public BlendCurveType blendCurveType;

        /// <summary>Frequency of the boundary-wobble Perlin noise.</summary>
        [Tooltip("Noise frequency for boundary wobble.")]
        public float noiseFrequency;

        /// <summary>Amplitude of the boundary-wobble Perlin noise (metres).</summary>
        [Tooltip("Noise amplitude for boundary wobble (metres).")]
        public float noiseAmplitude;
    }

    /// <summary>
    /// Vegetation placement hints for a biome at a specific position.
    /// </summary>
    [Serializable]
    public struct VegetationHint
    {
        /// <summary>Overall vegetation density 0–1 (0 = bare, 1 = full coverage).</summary>
        [Tooltip("Overall vegetation density 0–1.")]
        public float density;

        /// <summary>Altitude in metres above which no trees appear.</summary>
        [Tooltip("Tree-line altitude in metres.")]
        public float treeLineAltitude;

        /// <summary>Ground-cover / undergrowth density 0–1.</summary>
        [Tooltip("Undergrowth density 0–1.")]
        public float undergrowthDensity;

        /// <summary>Minimum and maximum canopy height in metres (x = min, y = max).</summary>
        [Tooltip("Canopy height range in metres (x = min, y = max).")]
        public Vector2 canopyHeightRange;

        /// <summary>Species classification tags (e.g. "oak", "pine", "grass").</summary>
        [Tooltip("Dominant species tags for this position.")]
        public string[] dominantSpeciesTags;
    }

    /// <summary>
    /// Rule that maps an altitude/slope/moisture/biome combination to a terrain texture layer.
    /// </summary>
    [Serializable]
    public struct TerrainTextureRule
    {
        /// <summary>Altitude band this rule applies to (x = min metres, y = max metres).</summary>
        [Tooltip("Altitude range (x = min, y = max) in metres.")]
        public Vector2 altitudeRange;

        /// <summary>Slope band this rule applies to (x = min degrees, y = max degrees).</summary>
        [Tooltip("Slope range (x = min, y = max) in degrees.")]
        public Vector2 slopeRange;

        /// <summary>Minimum moisture level (0–1) required for this rule to apply.</summary>
        [Tooltip("Minimum moisture level 0–1 required.")]
        public float moistureLevel;

        /// <summary>Biome this rule is authored for.</summary>
        [Tooltip("Target biome for this rule.")]
        public BiomeType biomeType;

        /// <summary>Index into the terrain material's texture array.</summary>
        [Tooltip("Texture array index.")]
        public int textureIndex;

        /// <summary>Blend weight contribution 0–1 when this rule is active.</summary>
        [Tooltip("Blend weight 0–1 when this rule is active.")]
        public float blendWeight;
    }

    #endregion

    #region ScriptableObject

    /// <summary>
    /// A complete authored biome profile that combines all per-biome configuration
    /// and terrain texture rules into a single asset.
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeProfile", menuName = "SWEF/Biome/Biome Profile")]
    public class BiomeProfile : ScriptableObject
    {
        [Header("Biome Configurations")]
        [Tooltip("Per-biome configuration entries. One entry per BiomeType.")]
        public List<BiomeConfig> biomeConfigs = new List<BiomeConfig>();

        [Header("Terrain Texture Rules")]
        [Tooltip("Ordered list of terrain texture blending rules.")]
        public List<TerrainTextureRule> textureRules = new List<TerrainTextureRule>();

        [Header("Transition Settings")]
        [Tooltip("Default transition configuration used when no per-pair override is set.")]
        public BiomeTransitionConfig defaultTransitionConfig;

        /// <summary>
        /// Returns the <see cref="BiomeConfig"/> for the requested biome, or a
        /// default-initialised struct if no matching entry is found.
        /// </summary>
        /// <param name="biome">Biome type to look up.</param>
        public BiomeConfig GetConfig(BiomeType biome)
        {
            foreach (var cfg in biomeConfigs)
                if (cfg.biomeType == biome) return cfg;
            return default;
        }
    }

    #endregion
}
