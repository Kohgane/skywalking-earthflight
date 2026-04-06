// TerrainFlattener.cs — Phase 113: Procedural City & Airport Generation
// Local terrain modification for building/runway placement,
// smooth blending with surrounding terrain.
// Namespace: SWEF.ProceduralWorld

using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Modifies Unity <see cref="Terrain"/> height data to flatten an area suitable
    /// for city or runway placement, with smooth edge blending.
    /// </summary>
    public class TerrainFlattener : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Blending")]
        [Tooltip("Width of the blending border around the flat area as a fraction of radius.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float blendFraction = 0.15f;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Flattens the terrain within <paramref name="radiusMetres"/> of
        /// <paramref name="worldCentre"/> to <paramref name="targetElevation"/>.
        /// Areas outside the radius are blended smoothly.
        /// </summary>
        public void Flatten(Vector3 worldCentre, float radiusMetres, float targetElevation)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) return;

            var data = terrain.terrainData;
            int resolution = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, resolution, resolution);
            float targetNorm = targetElevation / data.size.y;
            float blendDist = radiusMetres * blendFraction;

            for (int hy = 0; hy < resolution; hy++)
            {
                for (int hx = 0; hx < resolution; hx++)
                {
                    // Convert heightmap index to world position
                    float wx = terrain.transform.position.x + ((float)hx / (resolution - 1)) * data.size.x;
                    float wz = terrain.transform.position.z + ((float)hy / (resolution - 1)) * data.size.z;
                    float dist = Vector2.Distance(new Vector2(wx, wz), new Vector2(worldCentre.x, worldCentre.z));

                    if (dist <= radiusMetres - blendDist)
                    {
                        heights[hy, hx] = targetNorm;
                    }
                    else if (dist <= radiusMetres)
                    {
                        float t = 1f - (dist - (radiusMetres - blendDist)) / blendDist;
                        t = Mathf.SmoothStep(0f, 1f, t);
                        heights[hy, hx] = Mathf.Lerp(heights[hy, hx], targetNorm, t);
                    }
                }
            }

            data.SetHeights(0, 0, heights);
        }

        /// <summary>
        /// Returns the normalised height at world position for use in flattening calculations.
        /// </summary>
        public static float SampleNormalisedHeight(Vector3 worldPos)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) return 0f;
            return terrain.SampleHeight(worldPos) / terrain.terrainData.size.y;
        }
    }
}
