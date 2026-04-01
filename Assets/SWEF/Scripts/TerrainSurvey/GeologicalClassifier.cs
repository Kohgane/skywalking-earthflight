// GeologicalClassifier.cs — SWEF Terrain Scanning & Geological Survey System
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TerrainSurvey
{
    /// <summary>
    /// Static utility class that converts terrain measurement values into a
    /// <see cref="GeologicalFeatureType"/> classification and provides display
    /// metadata (localization key, heatmap color) for each feature type.
    /// </summary>
    public static class GeologicalClassifier
    {
        // ── Default thresholds (used when no config is available) ─────────────────
        private const float DefaultMountainAlt  = 2500f;
        private const float DefaultPlateauAlt   = 1000f;
        private const float DefaultGlacierAlt   = 3000f;
        private const float DefaultSlopeFlat    =    5f;
        private const float DefaultSlopeSteep   =   40f;
        private const float DefaultVolcanoSlope =   25f;
        private const float DefaultCoastlineAlt =   50f;
        private const float DefaultTundraTmp    =   -5f;
        private const float DefaultDesertTmp    =   35f;

        // ── Localization key lookup ───────────────────────────────────────────────
        private static readonly Dictionary<GeologicalFeatureType, string> _locKeys =
            new Dictionary<GeologicalFeatureType, string>
        {
            { GeologicalFeatureType.Mountain,  "survey_feature_mountain"   },
            { GeologicalFeatureType.Desert,    "survey_feature_desert"     },
            { GeologicalFeatureType.Volcano,   "survey_feature_volcano"    },
            { GeologicalFeatureType.Plains,    "survey_feature_plains"     },
            { GeologicalFeatureType.Forest,    "survey_feature_forest"     },
            { GeologicalFeatureType.Glacier,   "survey_feature_glacier"    },
            { GeologicalFeatureType.Coastline, "survey_feature_coastline"  },
            { GeologicalFeatureType.Canyon,    "survey_feature_canyon"     },
            { GeologicalFeatureType.Wetland,   "survey_feature_wetland"    },
            { GeologicalFeatureType.Tundra,    "survey_feature_tundra"     },
            { GeologicalFeatureType.Plateau,   "survey_feature_plateau"    },
            { GeologicalFeatureType.RiftValley,"survey_feature_rift_valley"},
        };

        // ── Heatmap color lookup ──────────────────────────────────────────────────
        private static readonly Dictionary<GeologicalFeatureType, Color> _featureColors =
            new Dictionary<GeologicalFeatureType, Color>
        {
            { GeologicalFeatureType.Mountain,  new Color(0.55f, 0.55f, 0.55f) }, // grey
            { GeologicalFeatureType.Desert,    new Color(0.96f, 0.84f, 0.46f) }, // sand
            { GeologicalFeatureType.Volcano,   new Color(0.80f, 0.20f, 0.05f) }, // deep red
            { GeologicalFeatureType.Plains,    new Color(0.60f, 0.80f, 0.35f) }, // light green
            { GeologicalFeatureType.Forest,    new Color(0.13f, 0.55f, 0.13f) }, // dark green
            { GeologicalFeatureType.Glacier,   new Color(0.85f, 0.95f, 1.00f) }, // ice blue
            { GeologicalFeatureType.Coastline, new Color(0.25f, 0.65f, 0.90f) }, // ocean blue
            { GeologicalFeatureType.Canyon,    new Color(0.70f, 0.35f, 0.15f) }, // burnt orange
            { GeologicalFeatureType.Wetland,   new Color(0.30f, 0.60f, 0.45f) }, // teal-green
            { GeologicalFeatureType.Tundra,    new Color(0.75f, 0.80f, 0.75f) }, // pale sage
            { GeologicalFeatureType.Plateau,   new Color(0.78f, 0.70f, 0.50f) }, // khaki
            { GeologicalFeatureType.RiftValley,new Color(0.50f, 0.25f, 0.10f) }, // dark brown
        };

        // ── Classification ────────────────────────────────────────────────────────

        /// <summary>
        /// Classifies terrain measurements into a <see cref="GeologicalFeatureType"/>.
        /// An optional <paramref name="cfg"/> overrides the built-in thresholds.
        /// </summary>
        /// <param name="altitude">Terrain altitude above sea level (metres).</param>
        /// <param name="slope">Terrain slope angle (degrees, 0 = flat, 90 = vertical).</param>
        /// <param name="biomeId">Biome identifier from <c>BiomeClassifier</c>.</param>
        /// <param name="temperature">Estimated surface temperature (°C).</param>
        /// <param name="cfg">Optional config asset supplying custom thresholds.</param>
        public static GeologicalFeatureType Classify(
            float altitude,
            float slope,
            int   biomeId,
            float temperature,
            TerrainSurveyConfig cfg = null)
        {
            float mountainAlt  = cfg != null ? cfg.mountainAltitudeMin  : DefaultMountainAlt;
            float plateauAlt   = cfg != null ? cfg.plateauAltitudeMin   : DefaultPlateauAlt;
            float glacierAlt   = cfg != null ? cfg.glacierAltitudeMin   : DefaultGlacierAlt;
            float slopeFlat    = cfg != null ? cfg.slopeFlat            : DefaultSlopeFlat;
            float slopeSteep   = cfg != null ? cfg.slopeSteep           : DefaultSlopeSteep;
            float volcanoSlope = cfg != null ? cfg.volcanoSlopeMin      : DefaultVolcanoSlope;
            float coastlineAlt = cfg != null ? cfg.coastlineAltitudeMax : DefaultCoastlineAlt;
            float tundraTmp    = cfg != null ? cfg.tundraTemperatureMax : DefaultTundraTmp;
            float desertTmp    = cfg != null ? cfg.desertTemperatureMin : DefaultDesertTmp;

            // Glacier — very high altitude with cold temperatures
            if (altitude >= glacierAlt && temperature < 0f)
                return GeologicalFeatureType.Glacier;

            // Volcano — very steep slope at high altitude
            if (altitude >= mountainAlt && slope >= volcanoSlope)
                return GeologicalFeatureType.Volcano;

            // Mountain — high altitude, steep slope
            if (altitude >= mountainAlt && slope >= slopeFlat)
                return GeologicalFeatureType.Mountain;

            // Plateau — elevated, relatively flat
            if (altitude >= plateauAlt && slope < slopeFlat)
                return GeologicalFeatureType.Plateau;

            // Canyon / Rift Valley — steep slope at lower altitude
            if (slope >= slopeSteep)
            {
                return altitude < 300f
                    ? GeologicalFeatureType.RiftValley
                    : GeologicalFeatureType.Canyon;
            }

            // Tundra — cold temperature, low/mid altitude
            if (temperature <= tundraTmp)
                return GeologicalFeatureType.Tundra;

            // Coastline — very low altitude (near sea level)
            if (altitude <= coastlineAlt && altitude >= 0f)
                return GeologicalFeatureType.Coastline;

            // Desert — hot and dry
            if (temperature >= desertTmp)
                return GeologicalFeatureType.Desert;

            // Wetland — low, flat, temperate
            if (altitude < 100f && slope < slopeFlat)
                return GeologicalFeatureType.Wetland;

            // Forest — mid-altitude, mild temperature
            if (altitude < mountainAlt && temperature > tundraTmp && temperature < desertTmp)
                return GeologicalFeatureType.Forest;

            // Default — open plains
            return GeologicalFeatureType.Plains;
        }

        // ── Metadata helpers ──────────────────────────────────────────────────────

        /// <summary>Returns the localization key for the given feature type.</summary>
        public static string GetFeatureDisplayName(GeologicalFeatureType type)
        {
            return _locKeys.TryGetValue(type, out string key) ? key : "survey_feature_unknown";
        }

        /// <summary>Returns the heatmap display color for the given feature type.</summary>
        public static Color GetFeatureColor(GeologicalFeatureType type)
        {
            return _featureColors.TryGetValue(type, out Color c) ? c : Color.white;
        }
    }
}
