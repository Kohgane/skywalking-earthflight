// TerrainTextureBlender.cs — SWEF Terrain Detail & Biome System
using System;
using UnityEngine;

namespace SWEF.Biome
{
    /// <summary>
    /// MonoBehaviour that selects and blends terrain texture layers based on
    /// altitude, slope, moisture, and biome type.
    /// </summary>
    public class TerrainTextureBlender : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Profile")]
        [Tooltip("BiomeProfile asset containing TerrainTextureRule entries.")]
        [SerializeField] private BiomeProfile biomeProfile;

        [Header("Texture Layers")]
        [Tooltip("Total number of texture layers in the terrain material.")]
        [SerializeField] private int textureLayerCount = 8;

        [Header("Snow Settings")]
        [Tooltip("Altitude (metres) above which full snow coverage begins at the equator.")]
        [SerializeField] private float equatorialSnowlineAltitude = 4500f;

        [Tooltip("Altitude (metres) above which full snow coverage begins at high latitudes.")]
        [SerializeField] private float highLatitudeSnowlineAltitude = 500f;

        [Header("Area Refresh")]
        [Tooltip("Target terrain component used for RefreshBlendForArea calls.")]
        [SerializeField] private Terrain targetTerrain;

        #endregion

        #region Public API

        /// <summary>
        /// Returns the index of the primary texture layer for the given conditions.
        /// </summary>
        /// <param name="altitude">Altitude above sea level in metres.</param>
        /// <param name="slope">Terrain slope in degrees.</param>
        /// <param name="biome">Current biome.</param>
        /// <returns>Texture array index (0-based).</returns>
        public int GetPrimaryTextureIndex(float altitude, float slope, BiomeType biome)
        {
            if (biomeProfile == null) return 0;

            TerrainTextureRule bestRule = default;
            float bestWeight = -1f;

            foreach (var rule in biomeProfile.textureRules)
            {
                if (rule.biomeType != biome) continue;
                if (altitude < rule.altitudeRange.x || altitude > rule.altitudeRange.y) continue;
                if (slope    < rule.slopeRange.x    || slope    > rule.slopeRange.y)    continue;
                if (rule.blendWeight > bestWeight)
                {
                    bestWeight = rule.blendWeight;
                    bestRule   = rule;
                }
            }

            return bestWeight >= 0f ? bestRule.textureIndex : 0;
        }

        /// <summary>
        /// Returns blend weights for each texture layer given the current conditions.
        /// The returned array has length equal to <c>textureLayerCount</c>.
        /// Weights are normalised so they sum to 1.
        /// </summary>
        /// <param name="altitude">Altitude in metres.</param>
        /// <param name="slope">Slope in degrees.</param>
        /// <param name="moisture">Moisture 0–1.</param>
        /// <param name="biome">Current biome.</param>
        /// <returns>Normalised blend weights array.</returns>
        public float[] GetTextureBlendWeights(float altitude, float slope, float moisture, BiomeType biome)
        {
            float[] weights = new float[textureLayerCount];

            if (biomeProfile == null)
            {
                if (weights.Length > 0) weights[0] = 1f;
                return weights;
            }

            foreach (var rule in biomeProfile.textureRules)
            {
                if (rule.biomeType != biome) continue;
                if (altitude < rule.altitudeRange.x || altitude > rule.altitudeRange.y) continue;
                if (slope    < rule.slopeRange.x    || slope    > rule.slopeRange.y)    continue;
                if (moisture < rule.moistureLevel)  continue;
                if (rule.textureIndex < 0 || rule.textureIndex >= textureLayerCount) continue;

                weights[rule.textureIndex] += rule.blendWeight;
            }

            // Apply snow overlay using altitude only; latitude is not available in this
            // signature. Callers with latitude should call GetSnowCoverage(lat, altitude, month)
            // and apply it to the returned weights array manually.
            float snowCoverage = GetSnowCoverage(0.0, altitude, 1f);
            if (snowCoverage > 0f && textureLayerCount > 0)
            {
                int snowIdx = textureLayerCount - 1;
                weights[snowIdx] = Mathf.Max(weights[snowIdx], snowCoverage);
            }

            // Normalise
            float sum = 0f;
            for (int i = 0; i < weights.Length; i++) sum += weights[i];
            if (sum > 0f)
                for (int i = 0; i < weights.Length; i++) weights[i] /= sum;
            else if (weights.Length > 0)
                weights[0] = 1f;

            return weights;
        }

        /// <summary>
        /// Returns the snow coverage factor (0–1) at the given latitude and altitude.
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees.</param>
        /// <param name="altitude">Altitude in metres.</param>
        /// <param name="monthOfYear">Month 1–12 (affects seasonal snowline).</param>
        /// <returns>Snow coverage 0–1.</returns>
        public float GetSnowCoverage(double latitude, float altitude, float monthOfYear)
        {
            float absLat    = (float)Math.Abs(latitude);
            float t         = Mathf.Clamp01(absLat / 60f);
            float snowline  = Mathf.Lerp(equatorialSnowlineAltitude, highLatitudeSnowlineAltitude, t);

            // Seasonal: snowline drops by up to 400 m in winter
            float seasonShift = Mathf.Sin((monthOfYear - 7f) * Mathf.PI / 6f) * 400f;
            if (latitude < 0) seasonShift = -seasonShift;
            snowline -= seasonShift;

            if (altitude < snowline)  return 0f;
            if (altitude > snowline + 500f) return 1f;
            return (altitude - snowline) / 500f;
        }

        /// <summary>
        /// Requests the terrain system to refresh texture blending for the given world-space area.
        /// No-op when no target terrain is assigned.
        /// </summary>
        /// <param name="area">World-space bounds to refresh.</param>
        public void RefreshBlendForArea(Bounds area)
        {
            if (targetTerrain == null) return;

            // Convert world bounds to terrain alphamap coordinates and mark dirty.
            var   td          = targetTerrain.terrainData;
            var   terrainPos  = targetTerrain.GetPosition();
            float normMinX    = (area.min.x - terrainPos.x) / td.size.x;
            float normMinZ    = (area.min.z - terrainPos.z) / td.size.z;
            float normWidth   = area.size.x / td.size.x;
            float normHeight  = area.size.z / td.size.z;

            int mapX   = Mathf.Clamp(Mathf.FloorToInt(normMinX  * td.alphamapWidth),  0, td.alphamapWidth  - 1);
            int mapZ   = Mathf.Clamp(Mathf.FloorToInt(normMinZ  * td.alphamapHeight), 0, td.alphamapHeight - 1);
            int mapW   = Mathf.Max(1, Mathf.CeilToInt(normWidth  * td.alphamapWidth));
            int mapH   = Mathf.Max(1, Mathf.CeilToInt(normHeight * td.alphamapHeight));

            mapW = Mathf.Clamp(mapW, 1, td.alphamapWidth  - mapX);
            mapH = Mathf.Clamp(mapH, 1, td.alphamapHeight - mapZ);

            // Re-write the alphamap for the dirty region.
            float[,,] maps = td.GetAlphamaps(mapX, mapZ, mapW, mapH);
            // In a full implementation, each alphamap texel would be recomputed
            // from altitude/slope/moisture data. Here we mark the region as touched
            // so downstream systems can react.
            td.SetAlphamaps(mapX, mapZ, maps);
        }

        #endregion
    }
}
