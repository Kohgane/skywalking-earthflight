using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CityGen
{
    #region Enumerations

    /// <summary>Broad classification of a settlement by size and function.</summary>
    public enum SettlementType
    {
        Megacity,
        City,
        Town,
        Village,
        Hamlet,
        Industrial,
        Resort,
        HistoricCenter
    }

    /// <summary>Primary function of a procedurally generated building.</summary>
    public enum BuildingType
    {
        Residential,
        Commercial,
        Industrial,
        Skyscraper,
        Church,
        Mosque,
        Temple,
        Stadium,
        Airport,
        Park,
        Monument,
        Bridge,
        Tower
    }

    /// <summary>Road classification used for width, material and marking selection.</summary>
    public enum RoadType
    {
        Highway,
        MainRoad,
        Street,
        Alley,
        Pedestrian,
        Bridge
    }

    /// <summary>Visual language applied to a settlement's architecture.</summary>
    public enum ArchitectureStyle
    {
        Modern,
        Classical,
        Asian,
        MiddleEastern,
        Tropical,
        Nordic,
        Mediterranean,
        Futuristic
    }

    /// <summary>High-level category of a world landmark.</summary>
    public enum LandmarkCategory
    {
        Natural,
        Historical,
        Architectural,
        Religious,
        Cultural,
        Engineering
    }

    /// <summary>Roof shape applied to the top of a procedural building.</summary>
    public enum RoofType
    {
        Flat,
        Pitched,
        Dome,
        Spire,
        Antenna
    }

    /// <summary>Street-grid strategy used when generating a settlement layout.</summary>
    public enum LayoutStyle
    {
        /// <summary>Uniform rectangular grid (Manhattan style).</summary>
        Grid,
        /// <summary>Irregular, radial streets growing from a historic core.</summary>
        Organic,
        /// <summary>Grid outskirts surrounding an organic historic center.</summary>
        Mixed
    }

    #endregion

    #region Building & Settlement Definitions

    /// <summary>
    /// Design-time specification for a single procedural building archetype.
    /// </summary>
    [Serializable]
    public class BuildingDefinition
    {
        [Tooltip("Functional category of this building.")]
        public BuildingType buildingType = BuildingType.Residential;

        [Tooltip("Minimum height in world units.")]
        public float minHeight = 6f;

        [Tooltip("Maximum height in world units.")]
        public float maxHeight = 20f;

        [Tooltip("Minimum footprint width in world units.")]
        public float minWidth = 8f;

        [Tooltip("Maximum footprint width in world units.")]
        public float maxWidth = 20f;

        [Tooltip("Minimum number of rendered floor levels.")]
        public int minFloors = 2;

        [Tooltip("Maximum number of rendered floor levels.")]
        public int maxFloors = 8;

        [Tooltip("Roof shape applied at the building apex.")]
        public RoofType roofType = RoofType.Flat;

        [Tooltip("Index into the shared material palette for this building type.")]
        public int materialIndex = 0;

        [Tooltip("Number of LOD levels generated for this building (1–4).")]
        [Range(1, 4)]
        public int lodLevels = 3;
    }

    /// <summary>
    /// Design-time specification for a settlement's size, density and style.
    /// </summary>
    [Serializable]
    public class SettlementDefinition
    {
        [Tooltip("Broad classification of this settlement.")]
        public SettlementType settlementType = SettlementType.City;

        [Tooltip("Approximate minimum population (display / seed weighting only).")]
        public int minPopulation = 50000;

        [Tooltip("Approximate maximum population (display / seed weighting only).")]
        public int maxPopulation = 500000;

        [Tooltip("Radius of the settlement in world units.")]
        public float areaRadius = 500f;

        [Tooltip("Average building footprint density (buildings per square unit × 1000).")]
        [Range(0f, 1f)]
        public float buildingDensity = 0.4f;

        [Tooltip("Average road density (road length per unit area × 1000).")]
        [Range(0f, 1f)]
        public float roadDensity = 0.3f;

        [Tooltip("Dominant architectural language of this settlement.")]
        public ArchitectureStyle architectureStyle = ArchitectureStyle.Modern;

        [Tooltip("Whether this settlement includes an airport footprint.")]
        public bool hasAirport = false;

        [Tooltip("Whether this settlement has a waterfront district.")]
        public bool hasWaterfront = false;
    }

    #endregion

    #region Landmark Definition

    /// <summary>
    /// Design-time definition for a notable real-world or procedural landmark.
    /// </summary>
    [Serializable]
    public class LandmarkDefinition
    {
        [Tooltip("Unique display name of the landmark.")]
        public string landmarkName = "Unknown Landmark";

        [Tooltip("High-level thematic category.")]
        public LandmarkCategory category = LandmarkCategory.Architectural;

        [Tooltip("World-space position of the landmark.")]
        public Vector3 worldPosition = Vector3.zero;

        [Tooltip("Short descriptive text shown in the info popup.")]
        [TextArea(2, 5)]
        public string description = string.Empty;

        [Tooltip("Optional prefab to instantiate for this landmark (null = procedural fallback).")]
        public GameObject prefab;

        [Tooltip("Relative importance rating from 1 (minor) to 10 (world-famous).")]
        [Range(1, 10)]
        public int importanceRating = 5;

        [Tooltip("ID string used to locate the NarrationData asset for this landmark.")]
        public string narrationTriggerId = string.Empty;

        [Tooltip("Maximum distance from which this landmark is visible / loaded (world units).")]
        public float visibilityDistance = 2000f;
    }

    #endregion

    #region Road Network

    /// <summary>A single directed road segment connecting two world-space points.</summary>
    [Serializable]
    public class RoadSegment
    {
        public Vector3   start;
        public Vector3   end;
        public float     width  = 6f;
        public RoadType  roadType = RoadType.Street;
    }

    /// <summary>
    /// Flat data container for the procedurally generated road graph of one settlement.
    /// </summary>
    [Serializable]
    public class RoadNetwork
    {
        public List<RoadSegment> segments          = new List<RoadSegment>();
        public List<Vector3>     intersectionPoints = new List<Vector3>();
    }

    #endregion

    #region City Block

    /// <summary>
    /// Rectangular parcel of land bounded by roads; filled with buildings or parks.
    /// </summary>
    [Serializable]
    public class CityBlock
    {
        public Bounds                    bounds        = new Bounds();
        public List<BuildingDefinition>  buildings     = new List<BuildingDefinition>();

        [Tooltip("Fraction of block area reserved for park/green space (0–1).")]
        [Range(0f, 1f)]
        public float parkPercentage  = 0f;

        [Tooltip("Length of this block's edge that faces a road.")]
        public float roadFrontage    = 20f;
    }

    #endregion

    #region City Gen Settings

    /// <summary>
    /// Runtime tuning knobs for the entire city generation system.
    /// Configure on the <see cref="CityManager"/> inspector.
    /// </summary>
    [Serializable]
    public class CityGenSettings
    {
        [Tooltip("Maximum number of settlements simultaneously loaded in the world.")]
        [Range(1, 32)]
        public int maxVisibleCities = 8;

        [Tooltip("Distance thresholds at which buildings switch LOD level (LOD0 → LOD3).")]
        public float[] buildingLodDistances = { 200f, 500f, 1000f, 2000f };

        [Tooltip("Maximum distance at which roads are rendered.")]
        public float roadRenderDistance = 1500f;

        [Tooltip("Maximum distance at which landmarks are rendered.")]
        public float landmarkRenderDistance = 5000f;

        [Tooltip("Integer seed that drives deterministic city placement and layout.")]
        public int generationSeed = 42;

        [Tooltip("Global multiplier on building and road density (0.1 = sparse, 2.0 = dense).")]
        [Range(0.1f, 2f)]
        public float densityMultiplier = 1f;
    }

    #endregion

    #region Runtime Layout Container

    /// <summary>
    /// Transient data produced by <see cref="CityLayoutGenerator"/> for one settlement.
    /// Consumed by <see cref="ProceduralBuildingGenerator"/> and <see cref="RoadNetworkRenderer"/>.
    /// </summary>
    public class CityLayout
    {
        public SettlementDefinition definition;
        public Vector3              center;
        public int                  seed;
        public RoadNetwork          roadNetwork   = new RoadNetwork();
        public List<CityBlock>      blocks        = new List<CityBlock>();
        public LayoutStyle          layoutStyle   = LayoutStyle.Grid;
    }

    #endregion
}
