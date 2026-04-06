// CityLayout.cs — Phase 113: Procedural City & Airport Generation
// City blueprint: road grid, block sizes, zoning map, population density map.
// Namespace: SWEF.ProceduralWorld

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Stores the complete blueprint of a procedurally generated city including
    /// the road grid layout, zoning map, and population density distribution.
    /// </summary>
    [Serializable]
    public class CityLayout
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        /// <summary>Deterministic seed used to produce this layout.</summary>
        public int seed;

        /// <summary>World-space centre of the city.</summary>
        public Vector3 centre;

        /// <summary>Overall city radius in metres.</summary>
        public float radiusMetres;

        /// <summary>City classification.</summary>
        public CityType cityType;

        // ── Road Grid ─────────────────────────────────────────────────────────────
        /// <summary>All road segments forming the street network.</summary>
        public List<RoadSegmentData> roadSegments = new List<RoadSegmentData>();

        /// <summary>Size of a single city block in metres (width × depth).</summary>
        public Vector2 blockSize = new Vector2(80f, 60f);

        /// <summary>Number of blocks along the X axis.</summary>
        public int blocksX;

        /// <summary>Number of blocks along the Z axis.</summary>
        public int blocksZ;

        // ── Zoning ────────────────────────────────────────────────────────────────
        /// <summary>
        /// 2-D zone map indexed [x, z]. Each cell contains a <see cref="BuildingType"/>
        /// cast to <c>int</c> indicating the dominant land use in that block.
        /// </summary>
        public int[,] zoneMap;

        // ── Population Density ────────────────────────────────────────────────────
        /// <summary>
        /// Normalised population density map [0..1] indexed [x, z].
        /// Higher values indicate denser building placement.
        /// </summary>
        public float[,] densityMap;

        // ── Statistics ────────────────────────────────────────────────────────────
        /// <summary>Total number of building plots in this layout.</summary>
        public int TotalPlots => blocksX * blocksZ;

        /// <summary>Total road length across all segments in metres.</summary>
        public float TotalRoadLengthMetres
        {
            get
            {
                float total = 0f;
                foreach (var seg in roadSegments)
                    total += seg.lengthMetres;
                return total;
            }
        }

        /// <summary>
        /// Returns the <see cref="BuildingType"/> assigned to the block at grid coordinates.
        /// </summary>
        public BuildingType GetZoneAt(int bx, int bz)
        {
            if (zoneMap == null || bx < 0 || bz < 0 || bx >= blocksX || bz >= blocksZ)
                return BuildingType.Residential;
            return (BuildingType)zoneMap[bx, bz];
        }

        /// <summary>Returns the density value [0..1] at the given grid position.</summary>
        public float GetDensityAt(int bx, int bz)
        {
            if (densityMap == null || bx < 0 || bz < 0 || bx >= blocksX || bz >= blocksZ)
                return 0f;
            return densityMap[bx, bz];
        }
    }

    /// <summary>Single road segment in the city grid.</summary>
    [Serializable]
    public class RoadSegmentData
    {
        /// <summary>Start point of the segment in world space.</summary>
        public Vector3 start;
        /// <summary>End point of the segment in world space.</summary>
        public Vector3 end;
        /// <summary>Functional road classification.</summary>
        public RoadType roadType;
        /// <summary>Road width in metres.</summary>
        public float widthMetres;

        /// <summary>Computed length of this segment in metres.</summary>
        public float lengthMetres => Vector3.Distance(start, end);
    }
}
