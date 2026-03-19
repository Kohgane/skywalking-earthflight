using UnityEngine;

namespace SWEF.Terrain
{
    /// <summary>
    /// Static utility class mapping altitude + latitude + moisture to a <see cref="BiomeType"/>
    /// and providing per-biome colors for vertex and texture coloring.
    /// </summary>
    public static class TerrainBiomeMapper
    {
        // ── Altitude thresholds (metres) ─────────────────────────────────────────
        private const float OceanMaxAlt    = -1f;
        private const float BeachMaxAlt    =  50f;
        private const float LowlandMaxAlt  = 500f;
        private const float MidlandMaxAlt  = 1500f;
        private const float HighlandMaxAlt = 3000f;
        private const float SnowMinAlt     = 3000f;

        // ── Latitude thresholds (absolute degrees) ───────────────────────────────
        private const float TropicalMaxLat  = 23.5f;
        private const float TemperateMaxLat = 55f;
        private const float BorealMaxLat    = 70f;

        // ── Moisture thresholds (0–1) ────────────────────────────────────────────
        private const float DesertMaxMoisture = 0.2f;

        /// <summary>
        /// Returns the most appropriate <see cref="BiomeType"/> for the given
        /// altitude (metres), latitude (degrees, signed) and moisture (0–1).
        /// </summary>
        public static BiomeType GetBiome(float altitude, float latitude, float moisture)
        {
            float absLat = Mathf.Abs(latitude);

            if (altitude < OceanMaxAlt) return BiomeType.Ocean;
            if (altitude < BeachMaxAlt) return BiomeType.Beach;
            if (altitude >= SnowMinAlt)
            {
                return absLat > BorealMaxLat ? BiomeType.Ice : BiomeType.Snow;
            }

            if (absLat > BorealMaxLat)  return BiomeType.Tundra;
            if (absLat > TemperateMaxLat) return BiomeType.Boreal;

            if (altitude >= MidlandMaxAlt) return BiomeType.Mountain;
            if (moisture < DesertMaxMoisture && absLat < TemperateMaxLat) return BiomeType.Desert;
            if (absLat < TropicalMaxLat) return BiomeType.Tropical;

            return BiomeType.Temperate;
        }

        /// <summary>Returns a representative <see cref="Color"/> for the given <see cref="BiomeType"/>.</summary>
        public static Color GetBiomeColor(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean      => new Color(0.05f, 0.25f, 0.65f),
                BiomeType.Beach      => new Color(0.85f, 0.82f, 0.55f),
                BiomeType.Tropical   => new Color(0.10f, 0.60f, 0.15f),
                BiomeType.Temperate  => new Color(0.25f, 0.55f, 0.20f),
                BiomeType.Boreal     => new Color(0.15f, 0.40f, 0.15f),
                BiomeType.Tundra     => new Color(0.60f, 0.65f, 0.55f),
                BiomeType.Desert     => new Color(0.85f, 0.72f, 0.38f),
                BiomeType.Mountain   => new Color(0.50f, 0.40f, 0.30f),
                BiomeType.Snow       => new Color(0.92f, 0.95f, 1.00f),
                BiomeType.Ice        => new Color(0.78f, 0.90f, 1.00f),
                _                    => Color.magenta
            };
        }

        /// <summary>
        /// Returns a vertex color blended purely from altitude, useful for quick
        /// height-based vertex coloring on procedural meshes.
        /// </summary>
        public static Color GetBiomeGradient(float altitude)
        {
            if (altitude < 0f)
                return Color.Lerp(new Color(0.02f, 0.15f, 0.50f), new Color(0.05f, 0.30f, 0.70f), Mathf.InverseLerp(-500f, 0f, altitude));

            if (altitude < 50f)
                return Color.Lerp(new Color(0.05f, 0.30f, 0.70f), new Color(0.85f, 0.82f, 0.55f), Mathf.InverseLerp(0f, 50f, altitude));

            if (altitude < 500f)
                return Color.Lerp(new Color(0.85f, 0.82f, 0.55f), new Color(0.25f, 0.55f, 0.20f), Mathf.InverseLerp(50f, 500f, altitude));

            if (altitude < 1500f)
                return Color.Lerp(new Color(0.25f, 0.55f, 0.20f), new Color(0.50f, 0.40f, 0.30f), Mathf.InverseLerp(500f, 1500f, altitude));

            if (altitude < 3000f)
                return Color.Lerp(new Color(0.50f, 0.40f, 0.30f), new Color(0.65f, 0.60f, 0.55f), Mathf.InverseLerp(1500f, 3000f, altitude));

            return Color.Lerp(new Color(0.65f, 0.60f, 0.55f), new Color(0.95f, 0.97f, 1.00f), Mathf.InverseLerp(3000f, 5000f, altitude));
        }
    }

    /// <summary>Terrain biome categories used by <see cref="TerrainBiomeMapper"/>.</summary>
    public enum BiomeType
    {
        Ocean,
        Beach,
        Tropical,
        Temperate,
        Boreal,
        Tundra,
        Desert,
        Mountain,
        Snow,
        Ice
    }
}
