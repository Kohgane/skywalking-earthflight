// BiomeClassifier.cs — SWEF Terrain Detail & Biome System
using System;
using UnityEngine;

namespace SWEF.Biome
{
    /// <summary>
    /// Static utility that classifies world coordinates into <see cref="BiomeType"/> values
    /// using latitude bands, altitude modifiers, and simplified climate estimates.
    /// No UnityEngine dependency beyond <c>Mathf</c> and <c>Vector2</c>.
    /// </summary>
    public static class BiomeClassifier
    {
        #region Latitude / Altitude Constants

        // Latitude band edges (absolute degrees)
        private const double ArcticLatitude    = 66.0;
        private const double BorealLatitude    = 55.0;
        private const double TemperateLatitude = 35.0;
        private const double SubtropicLatitude = 23.0;
        // Below SubtropicLatitude → Tropical

        // Altitude thresholds (metres)
        private const float TreelineAltitude = 2800f;
        private const float SnowlineAltitude = 3500f;

        // Coastal proximity threshold (degrees, rough approximation)
        private const double CoastalLatDegThreshold = 0.5;

        #endregion

        #region Primary Classification

        /// <summary>
        /// Classifies the given world coordinate into a <see cref="BiomeType"/>.
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees (−90 to +90).</param>
        /// <param name="longitude">Longitude in decimal degrees (−180 to +180).</param>
        /// <param name="altitudeM">Altitude above sea level in metres.</param>
        /// <returns>Most appropriate <see cref="BiomeType"/> for the position.</returns>
        public static BiomeType ClassifyBiome(double latitude, double longitude, float altitudeM)
        {
            double absLat = Math.Abs(latitude);

            // Altitude overrides take priority
            if (altitudeM >= SnowlineAltitude)
                return absLat >= ArcticLatitude ? BiomeType.Arctic : BiomeType.Mountain;

            if (altitudeM >= TreelineAltitude)
                return BiomeType.Mountain;

            // Ocean / coastal (very simplified: altitude < 0 = ocean)
            if (altitudeM < 0f)
                return BiomeType.Ocean;

            if (altitudeM < 20f && IsNearCoast(latitude, longitude))
                return BiomeType.Coastal;

            // Latitude bands
            if (absLat >= ArcticLatitude)
                return BiomeType.Arctic;

            if (absLat >= BorealLatitude)
            {
                float humidity = EstimateHumidity(latitude, longitude);
                return humidity > 0.4f ? BiomeType.Boreal : BiomeType.Tundra;
            }

            if (absLat >= TemperateLatitude)
            {
                float humidity = EstimateHumidity(latitude, longitude);
                if (humidity < 0.25f) return BiomeType.Steppe;
                return BiomeType.Temperate;
            }

            if (absLat >= SubtropicLatitude)
            {
                float humidity = EstimateHumidity(latitude, longitude);
                if (humidity < 0.15f) return BiomeType.Desert;
                if (humidity > 0.65f) return BiomeType.Savanna;
                return BiomeType.Steppe;
            }

            // Tropical band
            {
                float humidity = EstimateHumidity(latitude, longitude);
                if (humidity < 0.20f) return BiomeType.Desert;
                if (humidity > 0.70f) return BiomeType.Rainforest;
                return BiomeType.Tropical;
            }
        }

        #endregion

        #region Neighbour Biomes

        /// <summary>
        /// Returns the distinct biome types found within a radius around the given coordinate.
        /// Samples eight cardinal/intercardinal points at the specified radius.
        /// </summary>
        /// <param name="latitude">Centre latitude in decimal degrees.</param>
        /// <param name="longitude">Centre longitude in decimal degrees.</param>
        /// <param name="radiusKm">Sampling radius in kilometres.</param>
        /// <returns>Array of unique <see cref="BiomeType"/> values found in the neighbourhood.</returns>
        public static BiomeType[] GetNeighborBiomes(double latitude, double longitude, float radiusKm)
        {
            // 1 degree of latitude ≈ 111 km
            double latDelta = radiusKm / 111.0;
            double lonDelta = radiusKm / (111.0 * Math.Cos(latitude * Math.PI / 180.0));

            double[] latOffsets = { latDelta, -latDelta, 0,        0,        latDelta,  latDelta, -latDelta, -latDelta };
            double[] lonOffsets = { 0,        0,         lonDelta, -lonDelta, lonDelta, -lonDelta, lonDelta, -lonDelta };

            // Use a simple bit-field to track which BiomeType values have been seen
            System.Collections.Generic.HashSet<BiomeType> found = new System.Collections.Generic.HashSet<BiomeType>();
            for (int i = 0; i < latOffsets.Length; i++)
            {
                var b = ClassifyBiome(latitude + latOffsets[i], longitude + lonOffsets[i], 0f);
                found.Add(b);
            }

            BiomeType[] result = new BiomeType[found.Count];
            found.CopyTo(result);
            return result;
        }

        #endregion

        #region Blend Factor

        /// <summary>
        /// Returns a 0–1 blend weight indicating proximity to the boundary between
        /// <paramref name="biomeA"/> and <paramref name="biomeB"/> at the given coordinate.
        /// Returns 0 if neither biome is dominant, 1 if fully inside biomeA.
        /// </summary>
        /// <param name="lat">Latitude in decimal degrees.</param>
        /// <param name="lon">Longitude in decimal degrees.</param>
        /// <param name="biomeA">First biome.</param>
        /// <param name="biomeB">Second biome.</param>
        /// <returns>0–1 blend weight (0 = fully biomeB, 1 = fully biomeA).</returns>
        public static float GetBiomeBlendFactor(double lat, double lon, BiomeType biomeA, BiomeType biomeB)
        {
            BiomeType current = ClassifyBiome(lat, lon, 0f);
            if (current == biomeA) return 1f;
            if (current == biomeB) return 0f;

            // Evaluate neighbours to compute a distance-based blend
            float aCount = 0, bCount = 0;
            double delta = 0.05; // ~5.5 km per step
            double[] latOffsets = { delta, -delta, 0,     0 };
            double[] lonOffsets = { 0,     0,      delta, -delta };

            for (int i = 0; i < latOffsets.Length; i++)
            {
                var b = ClassifyBiome(lat + latOffsets[i], lon + lonOffsets[i], 0f);
                if (b == biomeA) aCount++;
                else if (b == biomeB) bCount++;
            }

            float total = aCount + bCount;
            if (total <= 0f) return 0.5f;
            return aCount / total;
        }

        #endregion

        #region Climate Estimation

        /// <summary>
        /// Estimates the surface temperature at the given latitude and altitude.
        /// Uses a simplified lapse-rate model (6.5 °C / 1000 m) applied on top of
        /// a latitude-based baseline.
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees.</param>
        /// <param name="altitudeM">Altitude above sea level in metres.</param>
        /// <param name="monthOfYear">Month 1–12 (used for seasonal variation).</param>
        /// <returns>Estimated temperature in degrees Celsius.</returns>
        public static float EstimateTemperature(double latitude, float altitudeM, float monthOfYear)
        {
            double absLat = Math.Abs(latitude);

            // Baseline sea-level temperature by latitude
            double baseTempC;
            if (absLat >= ArcticLatitude)
                baseTempC = -20.0;
            else if (absLat >= BorealLatitude)
                baseTempC = -5.0;
            else if (absLat >= TemperateLatitude)
                baseTempC = 10.0;
            else if (absLat >= SubtropicLatitude)
                baseTempC = 22.0;
            else
                baseTempC = 27.0;

            // Seasonal offset: ±8 °C based on month (northern hemisphere convention)
            float seasonalFactor = Mathf.Sin((monthOfYear - 3.5f) * Mathf.PI / 6f); // peaks July
            if (latitude < 0) seasonalFactor = -seasonalFactor; // flip for southern hemisphere
            baseTempC += seasonalFactor * 8.0;

            // Altitude lapse rate: 6.5 °C per 1000 m
            baseTempC -= altitudeM * 0.0065;

            return (float)baseTempC;
        }

        /// <summary>
        /// Estimates relative humidity (0–1) at the given coordinate using a
        /// simplified latitude + coastal-proximity model.
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees.</param>
        /// <param name="longitude">Longitude in decimal degrees.</param>
        /// <returns>Estimated humidity 0–1.</returns>
        public static float EstimateHumidity(double latitude, double longitude)
        {
            double absLat = Math.Abs(latitude);

            // Base humidity from latitude band
            float baseHumidity;
            if (absLat >= ArcticLatitude)       baseHumidity = 0.30f;
            else if (absLat >= BorealLatitude)  baseHumidity = 0.55f;
            else if (absLat >= TemperateLatitude) baseHumidity = 0.50f;
            else if (absLat >= SubtropicLatitude) baseHumidity = 0.25f;
            else                                baseHumidity = 0.65f;

            // Continental dryness: longitudes far from oceans tend to be drier.
            // Simplification: add variation using a sine wave on longitude.
            float lonVariation = (float)(Math.Sin(longitude * Math.PI / 60.0) * 0.1);

            return Mathf.Clamp01(baseHumidity + lonVariation);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Very coarse coastal detection: returns <c>true</c> if the coordinate
        /// falls within a narrow latitude band of a grid-aligned ocean cell.
        /// In a real implementation this would query ocean tile metadata.
        /// </summary>
        private static bool IsNearCoast(double latitude, double longitude)
        {
            // Use a rough heuristic: abs(sin(lat)*cos(lon)) < threshold → coastal noise pattern.
            // A production implementation would query Cesium tile metadata for ocean proximity.
            double noise = Math.Abs(Math.Sin(latitude * 0.3) * Math.Cos(longitude * 0.3));
            return noise < 0.12;
        }

        #endregion
    }
}
