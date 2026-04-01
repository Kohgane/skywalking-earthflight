// TerrainSurveyData.cs — SWEF Terrain Scanning & Geological Survey System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TerrainSurvey
{
    // ── Enums ─────────────────────────────────────────────────────────────────────

    /// <summary>Classifies the dominant geological/biome character of a surveyed area.</summary>
    public enum GeologicalFeatureType
    {
        Mountain,
        Desert,
        Volcano,
        Plains,
        Forest,
        Glacier,
        Coastline,
        Canyon,
        Wetland,
        Tundra,
        Plateau,
        RiftValley
    }

    /// <summary>Active visualization mode for the heatmap overlay.</summary>
    public enum SurveyMode
    {
        Altitude,
        Slope,
        Biome,
        Temperature,
        Mineral
    }

    // ── Structs ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single terrain sample collected by the scanner during one scan cycle.
    /// Blittable value-type for performance-friendly collection into arrays.
    /// </summary>
    [Serializable]
    public struct SurveySample
    {
        /// <summary>World-space position of the sample point.</summary>
        public Vector3 position;

        /// <summary>Classified geological feature at this sample point.</summary>
        public GeologicalFeatureType featureType;

        /// <summary>Terrain altitude above sea level in metres.</summary>
        public float altitude;

        /// <summary>Terrain slope angle in degrees (0 = flat, 90 = vertical).</summary>
        public float slope;

        /// <summary>Biome identifier returned by <c>BiomeClassifier</c>.</summary>
        public int biomeId;

        /// <summary>UTC timestamp when this sample was collected.</summary>
        public long timestamp;
    }

    // ── Classes ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// A discovered Point of Interest from the geological survey.
    /// Serialized to JSON for persistence.
    /// </summary>
    [Serializable]
    public class SurveyPOI
    {
        /// <summary>Unique identifier (GUID string).</summary>
        public string id;

        /// <summary>World-space position of the discovery.</summary>
        public Vector3 position;

        /// <summary>Dominant geological feature at this location.</summary>
        public GeologicalFeatureType featureType;

        /// <summary>Localization key for the display name.</summary>
        public string nameLocKey;

        /// <summary>UTC timestamp when the POI was first discovered.</summary>
        public long discoveredTimestamp;

        /// <summary>True until the player has acknowledged the discovery toast.</summary>
        public bool isNew;

        public SurveyPOI() { }

        public SurveyPOI(Vector3 pos, GeologicalFeatureType type, string locKey)
        {
            id                  = Guid.NewGuid().ToString();
            position            = pos;
            featureType         = type;
            nameLocKey          = locKey;
            discoveredTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            isNew               = true;
        }
    }

    // ── ScriptableObject ──────────────────────────────────────────────────────────

    /// <summary>
    /// Project-wide configuration asset for the Terrain Survey system.
    /// Create via <c>Assets → Create → SWEF → Terrain Survey Config</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Terrain Survey Config", fileName = "TerrainSurveyConfig")]
    public class TerrainSurveyConfig : ScriptableObject
    {
        [Header("Scanner")]
        [Tooltip("Radius of the scan grid below the aircraft in metres.")]
        public float scanRadius = 500f;

        [Tooltip("Number of sample points along each axis of the scan grid.")]
        public int scanResolution = 10;

        [Tooltip("Minimum seconds between successive scan cycles.")]
        public float cooldown = 5f;

        [Header("POI")]
        [Tooltip("Maximum number of POIs stored in memory and persisted.")]
        public int maxPOIs = 500;

        [Tooltip("Minimum metres between two POIs before they are treated as duplicates.")]
        public float proximityThreshold = 500f;

        [Header("Heatmap")]
        [Tooltip("Colors applied to the heatmap gradient for Altitude mode (low → high).")]
        public Gradient altitudeGradient = new Gradient();

        [Tooltip("Colors applied to the heatmap gradient for Slope mode (flat → steep).")]
        public Gradient slopeGradient = new Gradient();

        [Header("Classification Thresholds")]
        [Tooltip("Altitude above which terrain is classified as Mountain (metres).")]
        public float mountainAltitudeMin  = 2500f;

        [Tooltip("Altitude above which terrain is classified as Plateau (metres, slope < slopeFlat).")]
        public float plateauAltitudeMin   = 1000f;

        [Tooltip("Altitude above which terrain is classified as Glacier (metres).")]
        public float glacierAltitudeMin   = 3000f;

        [Tooltip("Slope angle below which terrain is considered flat (degrees).")]
        public float slopeFlat            = 5f;

        [Tooltip("Slope angle above which terrain is classified as Canyon/Rift walls.")]
        public float slopeSteep           = 40f;

        [Tooltip("Slope angle above which terrain is classified as Volcano (high alt + steep).")]
        public float volcanoSlopeMin      = 25f;

        [Tooltip("Altitude below which terrain touching water is Coastline (metres).")]
        public float coastlineAltitudeMax = 50f;

        [Tooltip("Temperature estimate (°C) below which terrain is Tundra.")]
        public float tundraTemperatureMax = -5f;

        [Tooltip("Temperature estimate (°C) above which terrain is Desert (low rainfall biome).")]
        public float desertTemperatureMin = 35f;

        // ── Default values ────────────────────────────────────────────────────────

        private void Reset()
        {
            scanRadius         = 500f;
            scanResolution     = 10;
            cooldown           = 5f;
            maxPOIs            = 500;
            proximityThreshold = 500f;

            mountainAltitudeMin  = 2500f;
            plateauAltitudeMin   = 1000f;
            glacierAltitudeMin   = 3000f;
            slopeFlat            = 5f;
            slopeSteep           = 40f;
            volcanoSlopeMin      = 25f;
            coastlineAltitudeMax = 50f;
            tundraTemperatureMax = -5f;
            desertTemperatureMin = 35f;
        }
    }
}
