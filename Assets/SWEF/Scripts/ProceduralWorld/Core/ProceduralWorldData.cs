// ProceduralWorldData.cs — Phase 113: Procedural City & Airport Generation
// Enums and data models for the procedural world system.
// Namespace: SWEF.ProceduralWorld

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    // ── City Types ────────────────────────────────────────────────────────────────

    /// <summary>High-level classification of a procedurally generated city.</summary>
    public enum CityType
    {
        /// <summary>Large dense urban area with skyscrapers and heavy traffic.</summary>
        Metropolis,
        /// <summary>Mid-sized settlement with mixed commercial and residential zones.</summary>
        Town,
        /// <summary>Small rural community with low building density.</summary>
        Village,
        /// <summary>Industrial complex with warehouses and factories.</summary>
        Industrial,
        /// <summary>Settlement adjacent to a coast with harbours and beaches.</summary>
        Coastal
    }

    // ── Building Types ────────────────────────────────────────────────────────────

    /// <summary>Functional type of a procedural building.</summary>
    public enum BuildingType
    {
        /// <summary>Apartment blocks and houses.</summary>
        Residential,
        /// <summary>Shops, offices, and retail towers.</summary>
        Commercial,
        /// <summary>Factories, warehouses, and logistics hubs.</summary>
        Industrial,
        /// <summary>Notable single buildings such as stadiums or monuments.</summary>
        Landmark,
        /// <summary>Government offices, city halls, and civic buildings.</summary>
        Government
    }

    // ── Airport Types ─────────────────────────────────────────────────────────────

    /// <summary>Classification of a procedurally generated airport.</summary>
    public enum AirportType
    {
        /// <summary>Major airport with long runways, multiple terminals.</summary>
        International,
        /// <summary>Smaller airport serving regional routes.</summary>
        Regional,
        /// <summary>Military airfield with restricted access.</summary>
        Military,
        /// <summary>Single-pad heliport for rotorcraft only.</summary>
        Helipad,
        /// <summary>Water-based facility for seaplanes and floatplanes.</summary>
        Seaplane
    }

    // ── Road Types ────────────────────────────────────────────────────────────────

    /// <summary>Classification of road segments in the city grid.</summary>
    public enum RoadType
    {
        /// <summary>High-speed limited-access highway.</summary>
        Highway,
        /// <summary>Primary arterial road connecting districts.</summary>
        MainRoad,
        /// <summary>Secondary residential or commercial street.</summary>
        SideStreet,
        /// <summary>Narrow lane or alley between buildings.</summary>
        Lane,
        /// <summary>Circular junction managing traffic flow.</summary>
        Roundabout
    }

    // ── Generation State ──────────────────────────────────────────────────────────

    /// <summary>Lifecycle state of a generation request.</summary>
    public enum GenerationState
    {
        /// <summary>Generation has not started.</summary>
        Idle,
        /// <summary>Terrain analysis in progress.</summary>
        AnalyzingTerrain,
        /// <summary>City or airport layout is being computed.</summary>
        GeneratingLayout,
        /// <summary>Meshes and prefabs are being placed.</summary>
        PlacingObjects,
        /// <summary>LOD levels are being configured.</summary>
        ConfiguringLOD,
        /// <summary>Generation complete and objects are active.</summary>
        Complete,
        /// <summary>Generation failed due to an error.</summary>
        Failed
    }

    // ── LOD Levels ────────────────────────────────────────────────────────────────

    /// <summary>Detail level for buildings and city chunks.</summary>
    public enum LODLevel
    {
        /// <summary>Full geometric detail with textures and normal maps.</summary>
        LOD0,
        /// <summary>Simplified mesh — roughly half the polygon count.</summary>
        LOD1,
        /// <summary>Billboard / impostor representation.</summary>
        LOD2,
        /// <summary>GPU-instanced batch or distant point sprite.</summary>
        LOD3
    }

    // ── Data Models ───────────────────────────────────────────────────────────────

    /// <summary>Blueprint describing a single procedural building instance.</summary>
    [Serializable]
    public class BuildingInstance
    {
        /// <summary>World-space footprint centre.</summary>
        public Vector3 position;
        /// <summary>Euler rotation of the building.</summary>
        public Vector3 rotation;
        /// <summary>Non-uniform scale applied to the mesh.</summary>
        public Vector3 scale;
        /// <summary>Functional type determining mesh style and material.</summary>
        public BuildingType buildingType;
        /// <summary>Floor count driving height.</summary>
        public int floorCount;
        /// <summary>Current active LOD level.</summary>
        public LODLevel currentLOD;

        /// <summary>Initialises a building instance with required parameters.</summary>
        public BuildingInstance(Vector3 position, BuildingType type, int floors)
        {
            this.position = position;
            this.rotation = Vector3.zero;
            this.scale = Vector3.one;
            this.buildingType = type;
            this.floorCount = Mathf.Max(1, floors);
            this.currentLOD = LODLevel.LOD0;
        }
    }

    /// <summary>Describes a single runway within an airport.</summary>
    [Serializable]
    public class RunwayData
    {
        /// <summary>ICAO runway designator (e.g. "09L").</summary>
        public string designator;
        /// <summary>True heading in degrees.</summary>
        public float heading;
        /// <summary>Runway length in metres.</summary>
        public float lengthMetres;
        /// <summary>Runway width in metres.</summary>
        public float widthMetres;
        /// <summary>World-space threshold position.</summary>
        public Vector3 thresholdPosition;
        /// <summary>Whether ILS is fitted on this end.</summary>
        public bool hasILS;

        /// <summary>Returns the reciprocal runway heading.</summary>
        public float ReciprocalHeading => (heading + 180f) % 360f;
    }

    /// <summary>Complete description of a procedurally generated airport.</summary>
    [Serializable]
    public class AirportLayout
    {
        /// <summary>Generated ICAO-style identifier (e.g. "PW01").</summary>
        public string icaoCode;
        /// <summary>Human-readable airport name.</summary>
        public string airportName;
        /// <summary>Airport classification.</summary>
        public AirportType airportType;
        /// <summary>World-space airport reference point.</summary>
        public Vector3 referencePoint;
        /// <summary>Elevation above sea level in metres.</summary>
        public float elevationMetres;
        /// <summary>All runways at this airport.</summary>
        public List<RunwayData> runways = new List<RunwayData>();
        /// <summary>Number of passenger gates.</summary>
        public int gateCount;
        /// <summary>Whether a control tower is present.</summary>
        public bool hasControlTower;
    }

    /// <summary>Complete description of a procedurally generated city.</summary>
    [Serializable]
    public class CityDescription
    {
        /// <summary>Seed used for deterministic generation.</summary>
        public int seed;
        /// <summary>Human-readable city name.</summary>
        public string cityName;
        /// <summary>Classification driving density and style.</summary>
        public CityType cityType;
        /// <summary>Centre of the city in world space.</summary>
        public Vector3 centre;
        /// <summary>Approximate radius covering built-up area.</summary>
        public float radiusMetres;
        /// <summary>Estimated population count.</summary>
        public int population;
        /// <summary>All building instances placed in this city.</summary>
        public List<BuildingInstance> buildings = new List<BuildingInstance>();
    }

    /// <summary>Result bundle returned after terrain analysis.</summary>
    [Serializable]
    public class TerrainAnalysisResult
    {
        /// <summary>Whether the area is flat enough for city or airport placement.</summary>
        public bool isSuitable;
        /// <summary>Average elevation in metres.</summary>
        public float averageElevation;
        /// <summary>Maximum slope angle found in the analysis area in degrees.</summary>
        public float maxSlopeDegrees;
        /// <summary>Whether a coastline was detected nearby.</summary>
        public bool hasCoastline;
        /// <summary>Recommended city type based on terrain analysis.</summary>
        public CityType recommendedCityType;
        /// <summary>Recommended airport type based on terrain analysis.</summary>
        public AirportType recommendedAirportType;
    }

    /// <summary>Chunk identifier for the world streaming system.</summary>
    [Serializable]
    public class ChunkCoord
    {
        /// <summary>Chunk X index.</summary>
        public int x;
        /// <summary>Chunk Z index.</summary>
        public int z;

        public ChunkCoord(int x, int z) { this.x = x; this.z = z; }

        public override bool Equals(object obj) =>
            obj is ChunkCoord other && other.x == x && other.z == z;

        public override int GetHashCode() => x * 397 ^ z;

        public override string ToString() => $"Chunk({x},{z})";
    }
}
