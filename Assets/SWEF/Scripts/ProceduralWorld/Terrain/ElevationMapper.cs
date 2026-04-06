// ElevationMapper.cs — Phase 113: Procedural City & Airport Generation
// Height map analysis for building height limits, no-fly zone generation.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Samples terrain elevation data to produce height maps, enforce building
    /// height limits, and identify no-fly zones above terrain obstacles.
    /// </summary>
    public class ElevationMapper : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Mapping")]
        [Tooltip("Resolution of the elevation sample grid (N × N samples).")]
        [SerializeField] private int resolution = 32;

        [Tooltip("Minimum clearance above terrain for no-fly zone ceiling in metres.")]
        [SerializeField] private float noFlyZoneClearance = 300f;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Samples elevation in a square grid centred at <paramref name="centre"/>.
        /// Returns a 2-D array [resolution × resolution] of elevation values in metres.
        /// </summary>
        public float[,] BuildElevationMap(Vector3 centre, float extentMetres)
        {
            var map = new float[resolution, resolution];
            float step = extentMetres * 2f / (resolution - 1);

            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    float x = centre.x - extentMetres + i * step;
                    float z = centre.z - extentMetres + j * step;
                    map[i, j] = SampleElevation(new Vector3(x, centre.y + 5000f, z));
                }
            }
            return map;
        }

        /// <summary>
        /// Computes the maximum permitted building height for a footprint at
        /// <paramref name="position"/> based on surrounding terrain elevation.
        /// </summary>
        public float MaxBuildingHeight(Vector3 position, float footprintRadius, float absoluteMaxMetres)
        {
            float baseElev = SampleElevation(new Vector3(position.x, position.y + 5000f, position.z));
            // Higher base elevation → lower maximum building height
            float fraction = Mathf.Clamp01(1f - baseElev / 3000f);
            return fraction * absoluteMaxMetres;
        }

        /// <summary>
        /// Returns the no-fly zone ceiling elevation above <paramref name="position"/>.
        /// </summary>
        public float NoFlyZoneCeiling(Vector3 position)
        {
            float terrain = SampleElevation(new Vector3(position.x, position.y + 5000f, position.z));
            return terrain + noFlyZoneClearance;
        }

        /// <summary>
        /// Finds the minimum elevation value within the given elevation map.
        /// </summary>
        public static float MinElevation(float[,] map)
        {
            float min = float.MaxValue;
            int r = map.GetLength(0);
            int c = map.GetLength(1);
            for (int i = 0; i < r; i++)
                for (int j = 0; j < c; j++)
                    if (map[i, j] < min) min = map[i, j];
            return min == float.MaxValue ? 0f : min;
        }

        /// <summary>
        /// Finds the maximum elevation value within the given elevation map.
        /// </summary>
        public static float MaxElevation(float[,] map)
        {
            float max = float.MinValue;
            int r = map.GetLength(0);
            int c = map.GetLength(1);
            for (int i = 0; i < r; i++)
                for (int j = 0; j < c; j++)
                    if (map[i, j] > max) max = map[i, j];
            return max == float.MinValue ? 0f : max;
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private static float SampleElevation(Vector3 origin)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain != null) return terrain.SampleHeight(origin);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10000f))
                return hit.point.y;
            return 0f;
        }
    }
}
